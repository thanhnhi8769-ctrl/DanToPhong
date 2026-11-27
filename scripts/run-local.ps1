param(
    [switch]$EnableSecondaryListener
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $root

$names = @(
    'DANTOPHONG_TEST_SA_PASSWORD',
    'DANTOPHONG_TEST_SA_PASSWORD',
    'DANTOPHONG_DB_CONNECTION',
    'DANTOPHONG_PORT',
    'DANTOPHONG_USE_TLS',
    'DANTOPHONG_CERT_PATH',
    'DANTOPHONG_CERT_PASSWORD',
    'DANTOPHONG_SECONDARY_PORT'
)
$previous = @{}
foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
$exitCode = 0

try {
    if ([string]::IsNullOrWhiteSpace($env:DANTOPHONG_TEST_SA_PASSWORD)) {
        $env:DANTOPHONG_TEST_SA_PASSWORD = [Environment]::GetEnvironmentVariable(
            'DANTOPHONG_TEST_SA_PASSWORD', [EnvironmentVariableTarget]::User)
    }
    if ([string]::IsNullOrWhiteSpace($env:DANTOPHONG_TEST_SA_PASSWORD)) {
        throw 'BLOCKED: DANTOPHONG_TEST_SA_PASSWORD is unavailable; no server was started.'
    }
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'BLOCKED: Docker CLI is unavailable; only dtp-sql and dtp-smtp may be checked.'
    }
    foreach ($container in @('dtp-sql', 'dtp-smtp')) {
        $running = & docker inspect --format '{{.State.Running}}' $container 2>$null
        if ($LASTEXITCODE -ne 0 -or $running -ne 'true') {
            throw "BLOCKED: required local container $container is not running; this script will not start or modify containers."
        }
    }
    if (-not (Test-NetConnection -ComputerName 127.0.0.1 -Port 1433 -InformationLevel Quiet)) {
        throw 'BLOCKED: dtp-sql is running but localhost:1433 is not reachable.'
    }

    $connectionBuilder = New-Object System.Data.Common.DbConnectionStringBuilder
    $connectionBuilder['Server'] = 'localhost,1433'
    $connectionBuilder['Database'] = 'DanToPhongDB_Test'
    $connectionBuilder['User Id'] = 'sa'
    $connectionBuilder['Password'] = $env:DANTOPHONG_TEST_SA_PASSWORD
    $connectionBuilder['Encrypt'] = 'True'
    $connectionBuilder['TrustServerCertificate'] = 'True'
    $env:DANTOPHONG_DB_CONNECTION = $connectionBuilder.ConnectionString
    $env:DANTOPHONG_PORT = '19000'
    $env:DANTOPHONG_USE_TLS = 'true'
    $listeners = Get-NetTCPConnection -State Listen -LocalPort 19000 -ErrorAction SilentlyContinue
    if ($listeners) { throw 'BLOCKED: test port 19000 is occupied; no listener was stopped.' }
    if ($EnableSecondaryListener) {
        $secondaryListeners = Get-NetTCPConnection -State Listen -LocalPort 19001 -ErrorAction SilentlyContinue
        if ($secondaryListeners) { throw 'BLOCKED: optional test port 19001 is occupied; no listener was stopped.' }
        $env:DANTOPHONG_SECONDARY_PORT = '19001'
    }
    else { Remove-Item Env:DANTOPHONG_SECONDARY_PORT -ErrorAction SilentlyContinue }

    $rsa = [System.Security.Cryptography.RSA]::Create(2048)
    $request = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
        'CN=localhost', $rsa, [System.Security.Cryptography.HashAlgorithmName]::SHA256,
        [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
    $san = [System.Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder]::new()
    $san.AddDnsName('localhost')
    $san.AddIpAddress([System.Net.IPAddress]::Loopback)
    $request.CertificateExtensions.Add($san.Build())
    $now = [DateTimeOffset]::UtcNow
    $certificate = $request.CreateSelfSigned($now.AddMinutes(-5), $now.AddDays(1))
    try {
        $certificatePath = Join-Path ([System.IO.Path]::GetTempPath()) ('DanToPhong-local-' + [Guid]::NewGuid().ToString('N') + '.pfx')
        $random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
        $randomBytes = New-Object byte[] 32
        try {
            $random.GetBytes($randomBytes)
            $env:DANTOPHONG_CERT_PASSWORD = [Convert]::ToBase64String($randomBytes)
        }
        finally { $random.Dispose() }
        $pfx = $certificate.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pkcs12, $env:DANTOPHONG_CERT_PASSWORD)
        $certificateFile = [System.IO.FileStream]::new(
            $certificatePath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
        try { $certificateFile.Write($pfx, 0, $pfx.Length) }
        finally { $certificateFile.Dispose() }
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try { $pin = [BitConverter]::ToString($sha256.ComputeHash($certificate.GetRawCertData())).Replace('-', '') }
        finally { $sha256.Dispose() }
    }
    finally {
        $certificate.Dispose()
        $rsa.Dispose()
    }
    $env:DANTOPHONG_CERT_PATH = $certificatePath

    dotnet build .\UI\DanToPhong.Server\DanToPhong.Server.csproj -c Release --no-restore -p:BaseOutputPath=bin\local-run\
    if ($LASTEXITCODE -ne 0) { throw 'Server Release build failed; server was not started.' }

    Write-Host '[CONFIG] Copy these public endpoint values to an untracked server.json beside each Launcher:'
    [ordered]@{
        host = 'localhost'
        port = 19000
        useTls = $true
        certificateThumbprintSha256 = $pin
    } | ConvertTo-Json
    Write-Host '[TLS] Generated a unique self-signed localhost certificate outside the repository; no certificate-store import was performed.'
    Write-Host "[TLS] Temporary certificate path: $certificatePath"
    Write-Host '[DB] Server will verify access to DanToPhongDB_Test during startup. The connection string is not printed.'
    Write-Host '[RUN] Press Ctrl+C to stop the local server.'
    $serverDll = '.\UI\DanToPhong.Server\bin\local-run\Release\net10.0\DanToPhong.Server.dll'
    dotnet $serverDll
    $exitCode = $LASTEXITCODE
}
catch {
    Write-Error $_.Exception.Message
    $exitCode = 1
}
finally {
    foreach ($name in $names) {
        if ($null -eq $previous[$name]) { Remove-Item ("Env:" + $name) -ErrorAction SilentlyContinue }
        else { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
    }
}

exit $exitCode