param(
    [string]$ServerName = 'localhost',
    [int]$Port = 9000,
    [string]$TlsHost = 'localhost',
    [int]$LockMinutes = 15,
    [switch]$Plaintext
)

$ErrorActionPreference = 'Stop'
if ($ServerName.ToLowerInvariant() -notin @('localhost', '127.0.0.1', '::1')) {
    throw 'This account-lockout test is restricted to a loopback server.'
}
if ($TlsHost.ToLowerInvariant() -notin @('localhost', '127.0.0.1', '::1')) {
    throw 'TlsHost must be a loopback name for this local-only test.'
}
$username = [Environment]::GetEnvironmentVariable('DANTOPHONG_TEST_USERNAME')
$password = [Environment]::GetEnvironmentVariable('DANTOPHONG_TEST_PASSWORD')
$badPassword = [Environment]::GetEnvironmentVariable('DANTOPHONG_TEST_BAD_PASSWORD')
if ([string]::IsNullOrWhiteSpace($username) -or
    [string]::IsNullOrWhiteSpace($password) -or
    [string]::IsNullOrWhiteSpace($badPassword)) {
    throw 'Set DANTOPHONG_TEST_USERNAME, DANTOPHONG_TEST_PASSWORD and DANTOPHONG_TEST_BAD_PASSWORD outside the repository.'
}
if ($LockMinutes -ne 15) {
    throw 'The server lock duration is fixed at 15 minutes; use -LockMinutes 15.'
}

function Read-Exact {
    param([System.IO.Stream]$Stream, [int]$Length)
    $buffer = New-Object byte[] $Length
    $offset = 0
    while ($offset -lt $Length) {
        $read = $Stream.Read($buffer, $offset, $Length - $offset)
        if ($read -le 0) { throw 'Server closed before completing the response frame.' }
        $offset += $read
    }
    return ,$buffer
}

function Invoke-Login {
    param([string]$PasswordValue)
    $client = [System.Net.Sockets.TcpClient]::new()
    $client.ReceiveTimeout = 10000
    $client.SendTimeout = 10000
    try {
        $client.Connect($ServerName, $Port)
        $stream = $client.GetStream()
        if (-not $Plaintext) {
            $ssl = [System.Net.Security.SslStream]::new($stream, $false)
            $ssl.AuthenticateAsClient($TlsHost)
            $stream = $ssl
        }

        $json = @{ command = 'DANGNHAP'; username = $username; password = $PasswordValue } | ConvertTo-Json -Compress
        $body = [System.Text.Encoding]::UTF8.GetBytes($json)
        $header = [BitConverter]::GetBytes([System.Net.IPAddress]::HostToNetworkOrder([int]$body.Length))
        $stream.Write($header, 0, $header.Length)
        $stream.Write($body, 0, $body.Length)
        $stream.Flush()

        $responseHeader = Read-Exact -Stream $stream -Length 4
        $responseLength = [System.Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($responseHeader, 0))
        if ($responseLength -lt 1 -or $responseLength -gt 65536) {
            throw 'Server response length is invalid.'
        }
        $responseBytes = Read-Exact -Stream $stream -Length $responseLength
        $response = [System.Text.Encoding]::UTF8.GetString($responseBytes) | ConvertFrom-Json
        return [bool]$response.success
    }
    finally {
        if ($null -ne $ssl) { $ssl.Dispose() }
        $client.Dispose()
    }
}

for ($attempt = 1; $attempt -le 5; $attempt++) {
    if (Invoke-Login -PasswordValue $badPassword) {
        throw 'The configured bad test password authenticated; aborting without printing response data.'
    }
    Write-Host "Invalid login attempt $attempt rejected."
}

if (Invoke-Login -PasswordValue $password) {
    throw 'Expected temporary account/IP lockout, but the valid test login succeeded.'
}
Write-Host 'Valid login rejected during the expected lockout.'
Write-Host 'Waiting 15 minutes for lock expiry...'
Start-Sleep -Seconds 900
if (-not (Invoke-Login -PasswordValue $password)) {
    throw 'Valid test login was still rejected after lock expiry.'
}
Write-Host 'PASS: account/IP lockout and unlock verified. Token/response body was not printed.'
