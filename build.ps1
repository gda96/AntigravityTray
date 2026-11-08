# Build script for Antigravity Tray Companion & Setup Installer
$ErrorActionPreference = "Stop"

$ProjectDir = $PSScriptRoot
$CscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$IconPath = Join-Path $ProjectDir "icon.ico"
$AppManifest = Join-Path $ProjectDir "app.manifest"
$InstallerManifest = Join-Path $ProjectDir "installer.manifest"
$AssemblyPath = Join-Path $ProjectDir "Properties\AssemblyInfo.cs"

$TraySource = Join-Path $ProjectDir "AntigravityTray.cs"
$TrayOut = Join-Path $ProjectDir "AntigravityTray.exe"

$InstallerSource = Join-Path $ProjectDir "Installer.cs"
$InstallerOut = Join-Path $ProjectDir "AntigravityTray_Setup.exe"

Write-Host "1. Compiling AntigravityTray.exe (High-DPI PerMonitorV2, asInvoker)..." -ForegroundColor Cyan
& $CscPath /nologo /target:winexe /optimize+ /platform:anycpu `
    /win32icon:"$IconPath" `
    /win32manifest:"$AppManifest" `
    /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,Microsoft.CSharp.dll `
    /out:"$TrayOut" "$AssemblyPath" "$TraySource"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to compile AntigravityTray.exe" -ForegroundColor Red
    exit 1
}

$fileInfoTray = Get-Item $TrayOut
Write-Host "   -> Success: $([math]::Round($fileInfoTray.Length / 1KB, 2)) KB" -ForegroundColor Green

Write-Host "2. Compiling AntigravityTray_Setup.exe (Program Files Installer, requireAdministrator)..." -ForegroundColor Cyan
& $CscPath /nologo /target:winexe /optimize+ /platform:anycpu `
    /win32icon:"$IconPath" `
    /win32manifest:"$InstallerManifest" `
    /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,Microsoft.CSharp.dll `
    /out:"$InstallerOut" "$AssemblyPath" "$InstallerSource"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to compile AntigravityTray_Setup.exe" -ForegroundColor Red
    exit 1
}

$fileInfoSetup = Get-Item $InstallerOut
Write-Host "   -> Success: $([math]::Round($fileInfoSetup.Length / 1KB, 2)) KB" -ForegroundColor Green

$ver = (Get-Content "$PSScriptRoot\VERSION").Trim()
Write-Host "`nAll builds v$ver completed successfully!" -ForegroundColor Green
