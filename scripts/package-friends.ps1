[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ServerHost,

    [Parameter(Mandatory = $true)]
    [string]$CertificateThumbprintSha256,

    [string]$GameBuildPath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'BuildGameRelease'),
    [string]$OutputZipPath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts\DanToPhong-friends-win-x64.zip'),
    [switch]$OverwriteExisting
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

if ([Uri]::CheckHostName($ServerHost) -notin @([UriHostNameType]::Dns, [UriHostNameType]::IPv4)) {
    throw 'ServerHost must be the DNS name or IPv4 address present in the TLS certificate SAN.'
}
$pin = ($CertificateThumbprintSha256 -replace '[^a-fA-F0-9]', '').ToUpperInvariant()
if ($pin -notmatch '^[A-F0-9]{64}$') {
    throw 'CertificateThumbprintSha256 must be a SHA-256 certificate thumbprint.'
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET SDK is unavailable; no package was created.'
}

$gameRoot = (Resolve-Path -LiteralPath $GameBuildPath).Path
$gameExe = Join-Path $gameRoot 'DanToPhong.exe'
if (-not (Test-Path -LiteralPath $gameExe -PathType Leaf)) {
    throw "Unity Windows x64 player is missing: $gameExe"
}
$outputZip = [System.IO.Path]::GetFullPath($OutputZipPath)
if ((Test-Path -LiteralPath $outputZip) -and -not $OverwriteExisting) {
    throw "Output already exists; choose a new OutputZipPath instead of overwriting: $outputZip"
}

$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$stage = Join-Path $tempRoot ("DanToPhong-friends-" + [Guid]::NewGuid().ToString('N'))
$publish = Join-Path $stage 'Launcher'
$package = Join-Path $stage 'Package'
$null = New-Item -ItemType Directory -Path $publish -Force
$null = New-Item -ItemType Directory -Path $package -Force

try {
    $project = Join-Path $root 'UI\UI\UI.csproj'
    & dotnet publish $project -c Release -r win-x64 --self-contained true -o $publish
    if ($LASTEXITCODE -ne 0) {
        throw "Launcher publish failed with exit code $LASTEXITCODE."
    }

    $launcherExe = Join-Path $publish 'DanToPhong.Launcher.exe'
    if (-not (Test-Path -LiteralPath $launcherExe -PathType Leaf)) {
        throw 'Published Launcher executable was not produced; no package was created.'
    }
    Get-ChildItem -LiteralPath $publish -Force |
        Copy-Item -Destination $package -Recurse -Force
    $gamePackage = Join-Path $package 'Game'
    $null = New-Item -ItemType Directory -Path $gamePackage
    Get-ChildItem -LiteralPath $gameRoot -Force |
        Copy-Item -Destination $gamePackage -Recurse -Force

    $clientConfig = [ordered]@{
        host = $ServerHost
        port = 9000
        udpPort = 9000
        useTls = $true
        certificateThumbprintSha256 = $pin
    } | ConvertTo-Json
    [System.IO.File]::WriteAllText(
        (Join-Path $package 'server.json'),
        $clientConfig,
        [System.Text.UTF8Encoding]::new($false))

    $readme = @"
Dan To Phong friend test package

1. Run DanToPhong.Launcher.exe.
2. Sign in with a test account approved for the demo.
3. Keep the network connected to $ServerHost.

The client uses TCP/TLS port 9000 for reliable operations and UDP port 9000
for authenticated movement input. The configured TLS pin is in server.json.
Do not edit the pin or disable TLS validation. If the certificate rotates,
obtain the newly verified pin from the project owner.
"@
    [System.IO.File]::WriteAllText(
        (Join-Path $package 'README.txt'),
        $readme,
        [System.Text.UTF8Encoding]::new($false))

    $outputDirectory = Split-Path -Parent $outputZip
    $null = New-Item -ItemType Directory -Path $outputDirectory -Force
    if ($OverwriteExisting) {
        Compress-Archive -Path (Join-Path $package '*') -DestinationPath $outputZip -CompressionLevel Optimal -Force
    }
    else {
        Compress-Archive -Path (Join-Path $package '*') -DestinationPath $outputZip -CompressionLevel Optimal
    }
    Write-Output "Package created: $outputZip"
}
finally {
    $resolvedStage = [System.IO.Path]::GetFullPath($stage)
    if ($resolvedStage.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedStage -PathType Container)) {
        Remove-Item -LiteralPath $resolvedStage -Recurse -Force
    }
}
