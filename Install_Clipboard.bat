@echo off
:: Request Admin Privileges
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    echo Requesting administrative privileges...
    goto UACPrompt
) else ( goto gotAdmin )

:UACPrompt
    echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    echo UAC.ShellExecute "%~s0", "", "", "runas", 1 >> "%temp%\getadmin.vbs"
    "%temp%\getadmin.vbs"
    del "%temp%\getadmin.vbs"
    exit /B

:gotAdmin
    pushd "%CD%"
    CD /D "%~dp0"

echo Closing existing application instance if running...
taskkill /F /IM CustomClipboardManager.exe 2>nul
sc stop CustomClipboardService >nul 2>&1
sc delete CustomClipboardService >nul 2>&1
timeout /t 1 /nobreak >nul

echo Creating destination directory...
mkdir "C:\Program Files\Clipboard" 2>nul

echo Detecting and copying program executable and dependencies...
if exist "%~dp0bin\Release\net10.0-windows\win-x64\publish\CustomClipboardManager.exe" (
    xcopy /s /e /y /q "%~dp0bin\Release\net10.0-windows\win-x64\publish\*" "C:\Program Files\Clipboard\"
) else if exist "%~dp0bin\Release\net10.0-windows\CustomClipboardManager.exe" (
    xcopy /s /e /y /q "%~dp0bin\Release\net10.0-windows\*" "C:\Program Files\Clipboard\"
) else if exist "%~dp0bin\Debug\net10.0-windows\CustomClipboardManager.exe" (
    xcopy /s /e /y /q "%~dp0bin\Debug\net10.0-windows\*" "C:\Program Files\Clipboard\"
) else (
    echo No pre-built executable found. Building project...
    dotnet publish -c Release -r win-x64 --self-contained false
    xcopy /s /e /y /q "%~dp0bin\Release\net10.0-windows\win-x64\publish\*" "C:\Program Files\Clipboard\"
)

echo Setting up auto-start on Windows boot...
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v "CustomClipboardManager" /t REG_SZ /d "\"C:\Program Files\Clipboard\CustomClipboardManager.exe\"" /f

echo Starting Custom Clipboard Manager...
start "" "C:\Program Files\Clipboard\CustomClipboardManager.exe"

echo Installation Complete!
pause
