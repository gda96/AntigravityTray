@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
if %ERRORLEVEL% neq 0 (
    echo.
    echo Build failed!
    if "%~1"=="" pause
    exit /b %ERRORLEVEL%
)
echo.
echo Build completed successfully!
if "%~1"=="" pause
