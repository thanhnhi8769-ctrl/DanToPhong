$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $root

$names = @('DANTOPHONG_TEST_SA_PASSWORD', 'DANTOPHONG_DB_CONNECTION', 'DANTOPHONG_TEST_SERVER_DLL')
$previous = @{}
foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
$exitCode = 0

try {
    if ([string]::IsNullOrWhiteSpace($env:DANTOPHONG_TEST_SA_PASSWORD)) {
        $env:DANTOPHONG_TEST_SA_PASSWORD = [Environment]::GetEnvironmentVariable(
            'DANTOPHONG_TEST_SA_PASSWORD', [EnvironmentVariableTarget]::User)
    }
    if ([string]::IsNullOrWhiteSpace($env:DANTOPHONG_TEST_SA_PASSWORD)) {
        Write-Host '[BLOCKED] DANTOPHONG_TEST_SA_PASSWORD is unavailable; no DB/server action was run.'
        $exitCode = 2
    }
    else {
        $listeners = Get-NetTCPConnection -State Listen -LocalPort 19000,19001 -ErrorAction SilentlyContinue
        if ($listeners) {
            Write-Host '[BLOCKED] A dedicated TC05 test port is already occupied; no listener was stopped.'
            $exitCode = 2
        }
        else {
            $connectionBuilder = New-Object System.Data.Common.DbConnectionStringBuilder
            $connectionBuilder['Server'] = 'localhost,1433'
            $connectionBuilder['Database'] = 'DanToPhongDB_Test'
            $connectionBuilder['User Id'] = 'sa'
            $connectionBuilder['Password'] = $env:DANTOPHONG_TEST_SA_PASSWORD
            $connectionBuilder['Encrypt'] = 'True'
            $connectionBuilder['TrustServerCertificate'] = 'True'
            $env:DANTOPHONG_DB_CONNECTION = $connectionBuilder.ConnectionString
            $buildOutput = 'bin\tc05-pinned\'
            $project = '.\tools\T2T8ProtocolTest\T2T8ProtocolTest.csproj'

            dotnet build $project -c Release "-p:BaseOutputPath=$buildOutput"
            if ($LASTEXITCODE -ne 0) {
                $exitCode = $LASTEXITCODE
            }
            else {
                $env:DANTOPHONG_TEST_SERVER_DLL = Join-Path $root 'UI\DanToPhong.Server\bin\tc05-pinned\Release\net10.0\DanToPhong.Server.dll'
                $harness = Join-Path $root 'tools\T2T8ProtocolTest\bin\tc05-pinned\Release\net10.0\T2T8ProtocolTest.dll'
                dotnet $harness --tc05-pinned
                $exitCode = $LASTEXITCODE
            }
        }
    }
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