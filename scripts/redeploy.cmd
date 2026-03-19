@echo off
setlocal

set TARGET=%~1
if "%TARGET%"=="" set TARGET=both

powershell -ExecutionPolicy Bypass -File "%~dp0redeploy-azure-changes.ps1" -Target "%TARGET%"

endlocal
