@echo off
:: Batch script to install and trust Custom Clipboard Manager Code Signing Certificate
:: Auto-elevate to Administrator

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [Custom Clipboard Manager] Requesting administrative privileges...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

title Custom Clipboard Manager - Trust Certificate Setup
echo ======================================================================
echo    Custom Clipboard Manager - Digital Signature Setup
echo    Installing Certificate into Windows Trusted Root and Publishers...
echo ======================================================================
echo.

set "CER_PATH=%~dp0ClipboardManager.cer"

if not exist "%CER_PATH%" (
    echo [ERROR] Certificate file not found: %CER_PATH%
    pause
    exit /b 1
)

echo [1/2] Installing into Trusted Root Certification Authorities...
certutil.exe -addstore -f "Root" "%CER_PATH%"
if %errorlevel% neq 0 (
    echo [ERROR] Failed to install into Root store.
) else (
    echo [OK] Successfully installed into Trusted Root.
)

echo.
echo [2/2] Installing into Trusted Publishers...
certutil.exe -addstore -f "TrustedPublisher" "%CER_PATH%"
if %errorlevel% neq 0 (
    echo [ERROR] Failed to install into TrustedPublisher store.
) else (
    echo [OK] Successfully installed into Trusted Publishers.
)

echo.
echo ======================================================================
echo    Setup completed! Windows now fully trusts Custom Clipboard Manager.
echo ======================================================================
echo.
pause
