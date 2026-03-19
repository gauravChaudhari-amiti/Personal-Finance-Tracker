@echo off
setlocal EnableDelayedExpansion

set "TARGET=%~1"
if "%TARGET%"=="" (
  powershell -ExecutionPolicy Bypass -File "%~dp0redeploy-azure-changes.ps1" -Target "both"
  endlocal
  exit /b %errorlevel%
)

if /I not "%TARGET%"=="backend" if /I not "%TARGET%"=="frontend" if /I not "%TARGET%"=="both" (
  powershell -ExecutionPolicy Bypass -File "%~dp0redeploy-azure-changes.ps1" %*
  endlocal
  exit /b %errorlevel%
)

shift
set "ARGS="

:collect_args
if "%~1"=="" goto run_command
set "ARGS=!ARGS! %1"
shift
goto collect_args

:run_command
powershell -ExecutionPolicy Bypass -File "%~dp0redeploy-azure-changes.ps1" -Target "%TARGET%" !ARGS!

endlocal
