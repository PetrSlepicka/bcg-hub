@echo off
setlocal

if /I "%~1"=="__run" goto run_deploy

set "ROOT=%~dp0"
set "API_DIR=%ROOT%BcgHub.Api"
set "UI_DIR=%ROOT%BcgHub.UI"
set "API_LOG=%TEMP%\bcg-hub-api-deploy.log"
set "UI_LOG=%TEMP%\bcg-hub-ui-deploy.log"
set "API_EXIT=%TEMP%\bcg-hub-api-deploy.exit"
set "UI_EXIT=%TEMP%\bcg-hub-ui-deploy.exit"

del "%API_LOG%" "%UI_LOG%" "%API_EXIT%" "%UI_EXIT%" 2>nul

echo Starting API and UI deployments in parallel...
echo API log: %API_LOG%
echo UI log:  %UI_LOG%
echo.

start "BCG HUB API Deploy" /B cmd /C call "%~f0" __run API "%API_DIR%" "%API_LOG%" "%API_EXIT%"
start "BCG HUB UI Deploy" /B cmd /C call "%~f0" __run UI "%UI_DIR%" "%UI_LOG%" "%UI_EXIT%"

:wait
if not exist "%API_EXIT%" goto still_running
if not exist "%UI_EXIT%" goto still_running
goto done

:still_running
ping -n 3 127.0.0.1 >nul
goto wait

:done
set /p API_CODE=<"%API_EXIT%"
set /p UI_CODE=<"%UI_EXIT%"

echo.
echo API deployment exit code: %API_CODE%
echo UI deployment exit code:  %UI_CODE%

if not "%API_CODE%"=="0" (
    echo.
    echo API deployment failed. See log:
    echo %API_LOG%
)

if not "%UI_CODE%"=="0" (
    echo.
    echo UI deployment failed. See log:
    echo %UI_LOG%
)

if not "%API_CODE%"=="0" exit /b %API_CODE%
if not "%UI_CODE%"=="0" exit /b %UI_CODE%

echo.
echo API and UI deployments completed successfully.
exit /b 0

:run_deploy
set "DEPLOY_NAME=%~2"
set "DEPLOY_DIR=%~3"
set "LOG_FILE=%~4"
set "EXIT_FILE=%~5"

cd /d "%DEPLOY_DIR%"
if %ERRORLEVEL% neq 0 (
    echo [%DEPLOY_NAME%] Failed to change directory to "%DEPLOY_DIR%". > "%LOG_FILE%"
    > "%EXIT_FILE%" echo 1
    exit /b 1
)

echo [%DEPLOY_NAME%] Starting deployment in "%DEPLOY_DIR%"... > "%LOG_FILE%"
call Deploy.bat <nul >> "%LOG_FILE%" 2>&1
set "DEPLOY_CODE=%ERRORLEVEL%"
echo [%DEPLOY_NAME%] Deployment finished with exit code %DEPLOY_CODE%. >> "%LOG_FILE%"
> "%EXIT_FILE%" echo %DEPLOY_CODE%
exit /b %DEPLOY_CODE%
