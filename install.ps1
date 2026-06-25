$ErrorActionPreference = 'Stop'

$sourceFile = "C:\Users\woran\OneDrive\เอกสาร\My_Project\C#\Clipboard\bin\Release\net10.0-windows\win-x64\publish\CustomClipboardManager.exe"
$destDir = "C:\Program Files\Clipboard"
$destFile = "$destDir\CustomClipboardManager.exe"

# Create directory if it doesn't exist
if (-not (Test-Path $destDir)) {
    New-Item -Path $destDir -ItemType Directory -Force
}

# Copy the executable
Copy-Item -Path $sourceFile -Destination $destFile -Force
