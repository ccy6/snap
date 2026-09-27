#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$DotNet = 'dotnet',
    [switch]$SmokeTest
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifacts = Join-Path $repo 'artifacts'
$staging = Join-Path $artifacts 'staging'
$release = Join-Path $artifacts 'release'
$publish = Join-Path $staging 'app'
$uninstaller = Join-Path $staging 'uninstaller'
$setup = Join-Path $staging 'setup'
$payload = Join-Path $staging 'payload.zip'
$version = ([xml](Get-Content (Join-Path $repo 'Directory.Build.props') -Raw)).Project.PropertyGroup.Version

function Invoke-DotNet {
    & $DotNet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" }
}

# Only disposable build outputs under this checkout may be removed.
foreach ($path in @($staging, $release)) {
    $full = [IO.Path]::GetFullPath($path)
    if (!$full.StartsWith($artifacts + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean outside artifacts: $full"
    }
    if (Test-Path -LiteralPath $full) { Remove-Item -LiteralPath $full -Recurse -Force }
    New-Item -ItemType Directory -Path $full -Force | Out-Null
}

Invoke-DotNet test (Join-Path $repo 'tests/Snap.Core.Tests/Snap.Core.Tests.csproj') -c Release --nologo
if ($SmokeTest) {
    Invoke-DotNet run --project (Join-Path $repo 'tests/Snap.App.SmokeTests/Snap.App.SmokeTests.csproj') -c Release
}
Invoke-DotNet publish (Join-Path $repo 'src/Snap.App/Snap.App.csproj') -c Release -r win-x64 --self-contained true -o $publish
Invoke-DotNet publish (Join-Path $repo 'packaging/Snap.Setup/Snap.Setup.csproj') -c Release -r win-x64 --self-contained true "-p:BuildUninstaller=true" -o $uninstaller

# Both applications target the same Windows Desktop runtime; share it in the payload.
Get-ChildItem -LiteralPath $uninstaller -Filter 'SnapUninstall.*' -File | Copy-Item -Destination $publish
[IO.Compression.ZipFile]::CreateFromDirectory($publish, $payload, [IO.Compression.CompressionLevel]::Optimal, $false)
Invoke-DotNet publish (Join-Path $repo 'packaging/Snap.Setup/Snap.Setup.csproj') -c Release -r win-x64 --self-contained true "-p:PayloadPath=$payload" "-p:PublishSingleFile=true" "-p:IncludeNativeLibrariesForSelfExtract=true" "-p:EnableCompressionInSingleFile=true" -o $setup

$installer = Join-Path $release "Snap-Setup-$version-win-x64.exe"
Copy-Item -LiteralPath (Join-Path $setup 'SnapSetup.exe') -Destination $installer
$hash = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath (Join-Path $release 'SHA256SUMS.txt') -Value "$hash  $([IO.Path]::GetFileName($installer))" -Encoding utf8
Write-Host "Release ready: $installer"
