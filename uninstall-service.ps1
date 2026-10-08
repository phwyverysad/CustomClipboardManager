# Requires Administrator
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "Elevating to Administrator..." -ForegroundColor Yellow
    Start-Process powershell.exe "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
    exit
}

$serviceName = "CustomClipboardService"
$existing = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if (-not $existing) {
    Write-Host "$serviceName is not installed." -ForegroundColor Yellow
    Start-Sleep -Seconds 2
    exit
}

Write-Host "Stopping $serviceName..." -ForegroundColor Cyan
Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

Write-Host "Deleting $serviceName..." -ForegroundColor Cyan
sc.exe delete $serviceName

Write-Host "[SUCCESS] $serviceName deleted from services.msc." -ForegroundColor Green
Start-Sleep -Seconds 2
