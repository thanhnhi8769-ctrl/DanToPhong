[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Status', 'Start', 'Stop', 'Pin')]
    [string]$Action,

    [string]$RdsInstanceId,
    [string]$Ec2InstanceId,
    [string]$Region,
    [string]$Profile,

    [string]$ClientHostName,
    [int]$Port = 9000,
    [string]$ExpectedThumbprint,

    [string]$SshHost,
    [string]$SshUser = 'ubuntu',
    [string]$SshKeyPath,

    [ValidateRange(1, 60)]
    [int]$TimeoutMinutes = 30
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
    throw 'AWS CLI is unavailable. No AWS request was sent.'
}

if ([string]::IsNullOrWhiteSpace($Profile)) {
    if ($env:AWS_PROFILE) { $Profile = $env:AWS_PROFILE }
    elseif ($env:AWS_DEFAULT_PROFILE) { $Profile = $env:AWS_DEFAULT_PROFILE }
}
if ([string]::IsNullOrWhiteSpace($Region)) {
    if ($env:AWS_REGION) { $Region = $env:AWS_REGION }
    elseif ($env:AWS_DEFAULT_REGION) { $Region = $env:AWS_DEFAULT_REGION }
    else {
        $regionArgs = @('configure', 'get', 'region')
        if ($Profile) { $regionArgs += @('--profile', $Profile) }
        $Region = (& aws @regionArgs 2>$null | Out-String).Trim()
    }
}
if ([string]::IsNullOrWhiteSpace($Region)) {
    throw 'AWS region is not configured. Supply -Region; no AWS resource action was taken.'
}

$script:AwsBaseArgs = @('--region', $Region, '--no-cli-pager')
if ($Profile) { $script:AwsBaseArgs += @('--profile', $Profile) }

function Invoke-AwsJson {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $callArgs = @($script:AwsBaseArgs) + @($Arguments) + @('--output', 'json')
    $output = & aws @callArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "AWS CLI request failed ($($Arguments[0..1] -join ' ')): $($output -join ' ')"
    }
    $text = $output -join [Environment]::NewLine
    if ([string]::IsNullOrWhiteSpace($text)) { return $null }
    return ($text | ConvertFrom-Json)
}

function Get-Rds {
    param([string]$Id)
    Invoke-AwsJson @(
        'rds', 'describe-db-instances',
        '--db-instance-identifier', $Id,
        '--query', 'DBInstances[0].{Id:DBInstanceIdentifier,Status:DBInstanceStatus,Engine:Engine,PubliclyAccessible:PubliclyAccessible,Groups:VpcSecurityGroups[].VpcSecurityGroupId}'
    )
}

function Get-Ec2 {
    param([string]$Id)
    Invoke-AwsJson @(
        'ec2', 'describe-instances',
        '--instance-ids', $Id,
        '--query', 'Reservations[0].Instances[0].{Id:InstanceId,State:State.Name,Type:InstanceType,PublicDnsName:PublicDnsName,PublicIpAddress:PublicIpAddress,Groups:SecurityGroups[].GroupId}'
    )
}

function Confirm-ResourceAction {
    param([ValidateSet('START', 'STOP')][string]$Verb)

    $expected = "$Verb $RdsInstanceId $Ec2InstanceId"
    $answer = Read-Host "This may incur AWS charges. Type exactly '$expected' to continue"
    if ($answer -cne $expected) {
        throw "Confirmation did not match; no $Verb action was requested."
    }
}

function Wait-RdsState {
    param([string]$Id, [string]$DesiredState)

    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    do {
        $item = Get-Rds $Id
        Write-Host "RDS $Id state: $($item.Status)"
        if ($item.Status -eq $DesiredState) { return $item }
        Start-Sleep -Seconds 15
    } while ((Get-Date) -lt $deadline)
    throw "RDS did not reach '$DesiredState' within $TimeoutMinutes minutes."
}

function Wait-Ec2State {
    param([string]$Id, [string]$DesiredState)

    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    do {
        $item = Get-Ec2 $Id
        Write-Host "EC2 $Id state: $($item.State)"
        if ($item.State -eq $DesiredState) { return $item }
        Start-Sleep -Seconds 15
    } while ((Get-Date) -lt $deadline)
    throw "EC2 did not reach '$DesiredState' within $TimeoutMinutes minutes."
}

function Wait-Ec2Health {
    param([string]$Id)

    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    do {
        $status = Invoke-AwsJson @(
            'ec2', 'describe-instance-status',
            '--instance-ids', $Id,
            '--include-all-instances',
            '--query', 'InstanceStatuses[0].{Instance:InstanceStatus.Status,System:SystemStatus.Status}'
        )
        if ($status -and $status.Instance -eq 'ok' -and $status.System -eq 'ok') { return }
        Write-Host 'Waiting for EC2 instance and system health checks.'
        Start-Sleep -Seconds 15
    } while ((Get-Date) -lt $deadline)
    throw "EC2 health checks did not pass within $TimeoutMinutes minutes."
}

function Assert-Ids {
    if ([string]::IsNullOrWhiteSpace($RdsInstanceId) -or [string]::IsNullOrWhiteSpace($Ec2InstanceId)) {
        throw "Action '$Action' requires both -RdsInstanceId and -Ec2InstanceId."
    }
}

function Write-PortIngressSummary {
    param([string[]]$GroupIds)

    foreach ($groupId in @($GroupIds | Where-Object { $_ } | Sort-Object -Unique)) {
        $groups = Invoke-AwsJson @(
            'ec2', 'describe-security-groups',
            '--group-ids', $groupId,
            '--query', 'SecurityGroups[0].{Id:GroupId,Ingress:IpPermissions}'
        )
        Write-Host "Security group $($groups.Id):"
        foreach ($portNumber in @(9000, 1433)) {
            $broad = $false
            $sourceCount = 0
            foreach ($rule in @($groups.Ingress)) {
                $isTcpOrAll = $rule.IpProtocol -eq '-1' -or $rule.IpProtocol -eq 'tcp' -or $rule.IpProtocol -eq '6'
                $coversPort = $rule.IpProtocol -eq '-1' -or
                    ($isTcpOrAll -and $null -ne $rule.FromPort -and $null -ne $rule.ToPort -and
                    $rule.FromPort -le $portNumber -and $rule.ToPort -ge $portNumber)
                if (-not $coversPort) { continue }
                foreach ($range in $rule.IpRanges) {
                    $sourceCount++
                    if ($range.CidrIp -eq '0.0.0.0/0') { $broad = $true }
                }
                foreach ($range in $rule.Ipv6Ranges) {
                    $sourceCount++
                    if ($range.CidrIpv6 -eq '::/0') { $broad = $true }
                }
            }
            $summary = if ($broad) { 'BROAD CIDR DETECTED' } else { 'no 0.0.0.0/0 or ::/0 CIDR detected' }
            Write-Host "  TCP $portNumber`: $summary; source range count=$sourceCount (CIDRs withheld)"
        }
    }
}

function Invoke-Status {
    if ($Ec2InstanceId) {
        $ec2Items = @(Get-Ec2 $Ec2InstanceId)
    }
    else {
        $ec2Items = @(Invoke-AwsJson @(
            'ec2', 'describe-instances',
            '--query', 'Reservations[].Instances[].{Id:InstanceId,State:State.Name,Type:InstanceType,PublicDnsName:PublicDnsName,PublicIpAddress:PublicIpAddress,Groups:SecurityGroups[].GroupId}'
        ))
    }
    if ($RdsInstanceId) {
        $rdsItems = @(Get-Rds $RdsInstanceId)
    }
    else {
        $rdsItems = @(Invoke-AwsJson @(
            'rds', 'describe-db-instances',
            '--query', 'DBInstances[].{Id:DBInstanceIdentifier,Status:DBInstanceStatus,Engine:Engine,PubliclyAccessible:PubliclyAccessible,Groups:VpcSecurityGroups[].VpcSecurityGroupId}'
        ))
    }
    Write-Host "Region: $Region"
    if (-not $ec2Items) { Write-Host 'No EC2 instances found in this region.' }
    foreach ($ec2 in $ec2Items) {
        if (-not $ec2) { continue }
        Write-Host "EC2 $($ec2.Id): $($ec2.State); type=$($ec2.Type)"
        if ($ec2.PublicDnsName) { Write-Host "  public DNS: $($ec2.PublicDnsName)" }
        elseif ($ec2.PublicIpAddress) { Write-Host "  public IP: $($ec2.PublicIpAddress)" }
        if ($ec2.State -ne 'terminated') {
            $health = Invoke-AwsJson @(
                'ec2', 'describe-instance-status',
                '--instance-ids', $ec2.Id,
                '--include-all-instances',
                '--query', 'InstanceStatuses[0].{Instance:InstanceStatus.Status,System:SystemStatus.Status}'
            )
            if ($health) { Write-Host "  health checks: instance=$($health.Instance), system=$($health.System)" }
        }
    }
    if (-not $rdsItems) { Write-Host 'No RDS DB instances found in this region.' }
    foreach ($rds in $rdsItems) {
        if ($rds) {
            Write-Host "RDS $($rds.Id): $($rds.Status); publicly accessible=$($rds.PubliclyAccessible)"
        }
    }

    $addresses = Invoke-AwsJson @(
        'ec2', 'describe-addresses',
        '--query', 'Addresses[].{AllocationId:AllocationId,AssociationId:AssociationId,InstanceId:InstanceId,NetworkInterfaceId:NetworkInterfaceId}'
    )
    Write-Host 'Elastic IP allocations (address values withheld):'
    if (-not $addresses) { Write-Host '  none found in this region' }
    foreach ($address in @($addresses)) {
        $association = if ($address.AssociationId) { 'associated' } else { 'unassociated' }
        Write-Host "  $($address.AllocationId): $association; instance=$($address.InstanceId); interface=$($address.NetworkInterfaceId)"
    }

    $groupIds = @(
        foreach ($ec2 in $ec2Items) { if ($ec2) { $ec2.Groups } }
        foreach ($rds in $rdsItems) { if ($rds) { $rds.Groups } }
    )
    Write-PortIngressSummary $groupIds
}

function Invoke-Start {
    Assert-Ids
    if ([string]::IsNullOrWhiteSpace($ClientHostName) -or [string]::IsNullOrWhiteSpace($SshKeyPath)) {
        throw "Start requires -ClientHostName (SAN hostname) and -SshKeyPath (outside the repository)."
    }
    Confirm-ResourceAction 'START'

    $rds = Get-Rds $RdsInstanceId
    switch ($rds.Status) {
        'available' { Write-Host 'RDS is already available.' }
        'stopped' {
            $null = Invoke-AwsJson @('rds', 'start-db-instance', '--db-instance-identifier', $RdsInstanceId)
            $rds = Wait-RdsState $RdsInstanceId 'available'
        }
        'starting' { $rds = Wait-RdsState $RdsInstanceId 'available' }
        default { throw "RDS state '$($rds.Status)' is not safe for this start sequence; no EC2 start was issued." }
    }

    $ec2 = Get-Ec2 $Ec2InstanceId
    switch ($ec2.State) {
        'running' { Write-Host 'EC2 is already running.' }
        'stopped' {
            $null = Invoke-AwsJson @('ec2', 'start-instances', '--instance-ids', $Ec2InstanceId)
            $ec2 = Wait-Ec2State $Ec2InstanceId 'running'
        }
        'pending' { $ec2 = Wait-Ec2State $Ec2InstanceId 'running' }
        default { throw "EC2 state '$($ec2.State)' is not safe for this start sequence." }
    }
    Wait-Ec2Health $Ec2InstanceId

    if (-not $SshHost) { $SshHost = $ec2.PublicDnsName }
    if (-not $SshHost) { $SshHost = $ec2.PublicIpAddress }
    if (-not $SshHost) { throw 'EC2 has no public SSH endpoint; inspect the approved private access path.' }
    if (-not (Get-Command ssh -ErrorAction SilentlyContinue)) { throw 'OpenSSH client is unavailable; AWS instances are running, but service verification was not performed.' }
    if (-not (Test-Path -LiteralPath $SshKeyPath -PathType Leaf)) { throw 'SSH key path is missing; AWS instances are running, but service verification was not performed.' }

    $sshKey = (Resolve-Path -LiteralPath $SshKeyPath).Path
    $remoteCommand = 'systemctl is-active --quiet dantophong && systemctl is-enabled --quiet dantophong && ss -ltnH "sport = :9000" | grep -q .'
    $sshArgs = @(
        '-i', $sshKey,
        '-o', 'BatchMode=yes',
        '-o', 'StrictHostKeyChecking=yes',
        '-o', 'ConnectTimeout=10',
        "$SshUser@$SshHost",
        $remoteCommand
    )
    $sshOutput = & ssh @sshArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "AWS instances are running, but systemd/listener verification failed; check SSH trust, security rules, and the service journal. $($sshOutput -join ' ')"
    }
    $tcp = Test-NetConnection -ComputerName $ClientHostName -Port $Port -WarningAction SilentlyContinue
    if (-not $tcp.TcpTestSucceeded) {
        throw "Server service is active, but TCP $Port is not reachable from this client network; inspect the approved /32 security-group rules."
    }
    Write-Host "RDS available; EC2 running with passing health checks."
    Write-Host "systemd service active/enabled, TCP 9000 listening, and endpoint $ClientHostName`:$Port reachable from this client."
}

function Invoke-Stop {
    Assert-Ids
    Confirm-ResourceAction 'STOP'

    $ec2 = Get-Ec2 $Ec2InstanceId
    switch ($ec2.State) {
        'stopped' { Write-Host 'EC2 is already stopped.' }
        'running' {
            $null = Invoke-AwsJson @('ec2', 'stop-instances', '--instance-ids', $Ec2InstanceId)
            $ec2 = Wait-Ec2State $Ec2InstanceId 'stopped'
        }
        'stopping' { $ec2 = Wait-Ec2State $Ec2InstanceId 'stopped' }
        default { throw "EC2 state '$($ec2.State)' is not safe for shutdown; RDS was not stopped." }
    }

    $rds = Get-Rds $RdsInstanceId
    switch ($rds.Status) {
        'stopped' { Write-Host 'RDS is already stopped.' }
        'available' {
            $null = Invoke-AwsJson @('rds', 'stop-db-instance', '--db-instance-identifier', $RdsInstanceId)
            $rds = Wait-RdsState $RdsInstanceId 'stopped'
        }
        'stopping' { $rds = Wait-RdsState $RdsInstanceId 'stopped' }
        default { throw "RDS state '$($rds.Status)' is not safe for shutdown; EC2 is already stopped." }
    }
    Write-Host 'EC2 is stopped before RDS; shutdown sequence complete.'
}

function Invoke-Pin {
    if ([string]::IsNullOrWhiteSpace($ClientHostName) -or [string]::IsNullOrWhiteSpace($ExpectedThumbprint)) {
        throw "Pin requires -ClientHostName and -ExpectedThumbprint copied from setup-ec2.sh output."
    }
    if ($Port -lt 1 -or $Port -gt 65535) { throw 'Port must be from 1 through 65535.' }
    $expected = ($ExpectedThumbprint -replace '[^a-fA-F0-9]', '').ToUpperInvariant()
    if ($expected -notmatch '^[A-F0-9]{64}$') { throw 'Expected thumbprint must be a SHA-256 hex value.' }

    $callback = [System.Net.Security.RemoteCertificateValidationCallback]{
        param($sender, $certificate, $chain, $policyErrors)
        if ($null -eq $certificate) { return $false }
        if (($policyErrors -band [System.Net.Security.SslPolicyErrors]::RemoteCertificateNameMismatch) -ne 0 -or
            ($policyErrors -band [System.Net.Security.SslPolicyErrors]::RemoteCertificateNotAvailable) -ne 0) {
            return $false
        }
        $remoteCert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($certificate)
        try {
            $now = [DateTime]::UtcNow
            if ($remoteCert.Subject -ne $remoteCert.Issuer -or
                $now -lt $remoteCert.NotBefore.ToUniversalTime() -or
                $now -ge $remoteCert.NotAfter.ToUniversalTime()) {
                return $false
            }
            if (($policyErrors -band [System.Net.Security.SslPolicyErrors]::RemoteCertificateChainErrors) -eq 0) {
                return $policyErrors -eq [System.Net.Security.SslPolicyErrors]::None
            }
            if ($null -eq $chain -or $chain.ChainStatus.Count -eq 0) { return $false }
            foreach ($status in $chain.ChainStatus) {
                if ($status.Status -ne [System.Security.Cryptography.X509Certificates.X509ChainStatusFlags]::UntrustedRoot -and
                    $status.Status -ne [System.Security.Cryptography.X509Certificates.X509ChainStatusFlags]::PartialChain) {
                    return $false
                }
            }
            return $true
        }
        finally {
            $remoteCert.Dispose()
        }
    }

    $tcpClient = [System.Net.Sockets.TcpClient]::new()
    $sslStream = $null
    $certificate = $null
    $sha = $null
    try {
        $tcpClient.ReceiveTimeout = 10000
        $tcpClient.SendTimeout = 10000
        $tcpClient.Connect($ClientHostName, $Port)
        $networkStream = $tcpClient.GetStream()
        $networkStream.ReadTimeout = 10000
        $networkStream.WriteTimeout = 10000
        $sslStream = [System.Net.Security.SslStream]::new($networkStream, $false, $callback)
        $sslStream.AuthenticateAsClient($ClientHostName)
        $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($sslStream.RemoteCertificate)
        $sha = [System.Security.Cryptography.SHA256]::Create()
        $actual = ([BitConverter]::ToString($sha.ComputeHash($certificate.RawData))).Replace('-', '')
        if ($actual -ne $expected) {
            throw 'Live endpoint certificate does not match the thumbprint verified on EC2; client config was not emitted.'
        }
        [ordered]@{
            host = $ClientHostName
            port = $Port
            useTls = $true
            certificateThumbprintSha256 = $actual
        } | ConvertTo-Json
    }
    finally {
        if ($sha) { $sha.Dispose() }
        if ($certificate) { $certificate.Dispose() }
        if ($sslStream) { $sslStream.Dispose() }
        $tcpClient.Dispose()
    }
}

switch ($Action) {
    'Status' { Invoke-Status }
    'Start' { Invoke-Start }
    'Stop' { Invoke-Stop }
    'Pin' { Invoke-Pin }
}
