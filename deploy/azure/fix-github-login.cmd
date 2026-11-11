@echo off
rem Double-click this file to let GitHub deploy to Azure. It signs you in to Azure if needed.
cd /d "%~dp0..\.."
call az account show >nul 2>&1 || call az login
powershell -ExecutionPolicy Bypass -File deploy\azure\fix-github-login.ps1
echo.
pause
