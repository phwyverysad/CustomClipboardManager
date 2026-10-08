$ErrorActionPreference = 'Stop'

# Stop and remove legacy Windows Service if installed
try {
    Stop-Service -Name "CustomClipboardService" -Force -ErrorAction SilentlyContinue
    & sc.exe delete CustomClipboardService 2>$null
} catch {}

# Stop user session instances
Get-Process -Name "CustomClipboardManager" -ErrorAction SilentlyContinue | Where-Object { $_.SessionId -ne 0 } | Stop-Process -Force
Start-Sleep -Milliseconds 500

$candidates = @(
    (Join-Path $PSScriptRoot "bin\Release\net10.0-windows\win-x64\publish\CustomClipboardManager.exe"),
    (Join-Path $PSScriptRoot "bin\Release\net10.0-windows\CustomClipboardManager.exe"),
    (Join-Path $PSScriptRoot "bin\Debug\net10.0-windows\CustomClipboardManager.exe")
)

$sourceFile = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $sourceFile) {
    Write-Host "No built executable found. Building project in Release mode..." -ForegroundColor Cyan
    Push-Location $PSScriptRoot
    try {
        dotnet publish -c Release -r win-x64 --self-contained false
    } finally {
        Pop-Location
    }
    $sourceFile = Join-Path $PSScriptRoot "bin\Release\net10.0-windows\win-x64\publish\CustomClipboardManager.exe"
}

if (-not (Test-Path $sourceFile)) {
    throw "Executable could not be found or built at $sourceFile"
}

$destDir = "C:\Program Files\Clipboard"
$destFile = Join-Path $destDir "CustomClipboardManager.exe"

# Create directory if it doesn't exist
if (-not (Test-Path $destDir)) {
    New-Item -Path $destDir -ItemType Directory -Force | Out-Null
}

# Copy executable and dependencies
$publishDir = Join-Path $PSScriptRoot "bin\Release\net10.0-windows\win-x64\publish"
if (Test-Path $publishDir) {
    Copy-Item -Path "$publishDir\*" -Destination $destDir -Recurse -Force
} else {
    Copy-Item -Path $sourceFile -Destination $destFile -Force
}
Write-Host "Copied application binaries and dependencies to $destDir" -ForegroundColor Green

# Configure auto-start on boot
Set-ItemProperty -Path "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" -Name "CustomClipboardManager" -Value "`"$destFile`"" -Force
Write-Host "Registered startup entry in HKCU Run registry." -ForegroundColor Green

# Start the application
Start-Process -FilePath $destFile
Write-Host "Custom Clipboard Manager has been installed and started successfully!" -ForegroundColor Green
