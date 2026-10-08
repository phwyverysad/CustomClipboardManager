@echo off
setlocal enabledelayedexpansion

:: Check for Administrator privileges
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo =======================================================
    echo Requesting Administrator privileges...
    echo =======================================================
    powershell -Command "Start-Process cmd -ArgumentList '/c \"\"%~f0\"\"' -Verb runAs"
    exit /b
)

echo =======================================================
echo Uninstalling Custom Clipboard Service (services.msc)...
echo =======================================================

sc.exe query CustomClipboardService >nul 2>&1
if %errorlevel% neq 0 (
    echo [INFO] CustomClipboardService is not currently installed.
    timeout /t 3
    exit /b 0
)

echo Stopping CustomClipboardService...
sc.exe stop CustomClipboardService >nul 2>&1
timeout /t 2 /nobreak >nul

echo Deleting CustomClipboardService...
sc.exe delete CustomClipboardService
if %errorlevel% equ 0 (
    echo =======================================================
    echo [SUCCESS] CustomClipboardService removed successfully!
    echo =======================================================
) else (
    echo [ERROR] Failed to delete CustomClipboardService.
)

timeout /t 3
