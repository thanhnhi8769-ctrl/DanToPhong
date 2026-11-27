$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $root

$previousSaPassword = $env:DANTOPHONG_TEST_SA_PASSWORD
$previousConnection = $env:DANTOPHONG_DB_CONNECTION
$previousServerDll = $env:DANTOPHONG_TEST_SERVER_DLL
$exitCode = 0

try {
    if ([string]::IsNullOrWhiteSpace($env:DANTOPHONG_TEST_SA_PASSWORD)) {
        $env:DANTOPHONG_TEST_SA_PASSWORD = [Environment]::GetEnvironmentVariable(
            'DANTOPHONG_TEST_SA_PASSWORD',
            [EnvironmentVariableTarget]::User)
    }

    if ([string]::IsNullOrWhiteSpace($env:DANTOPHONG_TEST_SA_PASSWORD)) {
        Write-Host '[BLOCKED] DANTOPHONG_TEST_SA_PASSWORD is unavailable; no DB or server actions were run.'
        $exitCode = 2
    }
    else {
        $env:DANTOPHONG_DB_CONNECTION = "Server=localhost,1433;Database=DanToPhongDB_Test;User Id=sa;Password=$env:DANTOPHONG_TEST_SA_PASSWORD;Encrypt=True;TrustServerCertificate=True"
        $buildOutput = 'bin\t2t8-protocol\'
        $project = '.\tools\T2T8ProtocolTest\T2T8ProtocolTest.csproj'

        dotnet build $project -c Release "-p:BaseOutputPath=$buildOutput"
        if ($LASTEXITCODE -ne 0) {
            $exitCode = $LASTEXITCODE
        }
        else {
            $env:DANTOPHONG_TEST_SERVER_DLL = Join-Path $root 'UI\DanToPhong.Server\bin\t2t8-protocol\Release\net10.0\DanToPhong.Server.dll'
            $harness = Join-Path $root 'tools\T2T8ProtocolTest\bin\t2t8-protocol\Release\net10.0\T2T8ProtocolTest.dll'
            dotnet $harness
            $exitCode = $LASTEXITCODE
        }
    }
}
finally {
    if ($null -eq $previousSaPassword) {
        Remove-Item Env:DANTOPHONG_TEST_SA_PASSWORD -ErrorAction SilentlyContinue
    }
    else {
        $env:DANTOPHONG_TEST_SA_PASSWORD = $previousSaPassword
    }
    if ($null -eq $previousConnection) {
        Remove-Item Env:DANTOPHONG_DB_CONNECTION -ErrorAction SilentlyContinue
    }
    else {
        $env:DANTOPHONG_DB_CONNECTION = $previousConnection
    }
    if ($null -eq $previousServerDll) {
        Remove-Item Env:DANTOPHONG_TEST_SERVER_DLL -ErrorAction SilentlyContinue
    }
    else {
        $env:DANTOPHONG_TEST_SERVER_DLL = $previousServerDll
    }
}

exit $exitCode
