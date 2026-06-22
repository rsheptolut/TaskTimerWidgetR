<#
.SYNOPSIS
    Builds and/or publishes Task Timer Widget R using Visual Studio's MSBuild.

.DESCRIPTION
    WinUI 3 desktop apps require the Appx/PRI packaging tasks that ship only with
    Visual Studio's MSBuild (not the standalone 'dotnet' CLI). Running 'dotnet
    build/publish' fails with a missing 'Microsoft.Build.Packaging.Pri.Tasks.dll'
    error. This script locates VS MSBuild via vswhere and runs the build there.

.PARAMETER Publish
    Produce a self-contained, unpackaged publish in .\local-publish instead of a
    plain build.

.PARAMETER Configuration
    Build configuration (default: Release).

.EXAMPLE
    .\build.ps1                # Release build
    .\build.ps1 -Publish       # Self-contained publish to .\local-publish
    .\build.ps1 -Configuration Debug
#>
[CmdletBinding()]
param(
    [switch]$Publish,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$project = Join-Path $repoRoot 'src\TaskTimerWidget\TaskTimerWidget.csproj'
$publishDir = Join-Path $repoRoot 'local-publish'

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    throw "vswhere.exe not found. Visual Studio (with MSBuild) is required to build this WinUI 3 app."
}

$msbuild = & $vswhere -latest -prerelease -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) {
    throw "Could not locate MSBuild.exe via Visual Studio. Ensure the 'MSBuild' component is installed."
}

Write-Host "Using MSBuild: $msbuild" -ForegroundColor Cyan

& $msbuild $project /t:Restore /p:Configuration=$Configuration /p:Platform=x64 /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw "Restore failed (exit $LASTEXITCODE)." }

if ($Publish) {
    Write-Host "Publishing self-contained (unpackaged) to $publishDir ..." -ForegroundColor Cyan
    & $msbuild $project `
        /t:Publish `
        /p:Configuration=$Configuration `
        /p:Platform=x64 `
        /p:RuntimeIdentifier=win-x64 `
        /p:WindowsAppSDKSelfContained=true `
        /p:SelfContained=true `
        /p:PublishDir="$publishDir\" `
        /v:minimal /nologo
    if ($LASTEXITCODE -ne 0) { throw "Publish failed (exit $LASTEXITCODE)." }

    Write-Host "Published: $publishDir\TaskTimerWidgetR.exe" -ForegroundColor Green
} else {
    & $msbuild $project /t:Build /p:Configuration=$Configuration /p:Platform=x64 /v:minimal /nologo
    if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)." }

    Write-Host "Build succeeded ($Configuration)." -ForegroundColor Green
}
