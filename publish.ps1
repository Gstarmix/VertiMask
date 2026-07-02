param([switch]$SelfContained)
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
Stop-Process -Name "VertiMask" -Force -ErrorAction SilentlyContinue
$sc = if ($SelfContained) { "true" } else { "false" }
dotnet publish -c Release -r win-x64 --self-contained $sc `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -o $PSScriptRoot
Write-Host ""
Write-Host "OK -> $PSScriptRoot\VertiMask.exe" -ForegroundColor Green