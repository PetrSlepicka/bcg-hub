@echo off
setlocal
cd /d "%~dp0"

set "IMAGE_NAME=ghcr.io/radixal-sro/bcg-hub-ui"
set "NAMESPACE=bcg-hub"
set "DEPLOYMENT_NAME=bcg-hub-ui"

if "%RADIXAL_CLUSTER_CONFIG%"=="" (
    echo ERROR: RADIXAL_CLUSTER_CONFIG environment variable is not set.
    goto fail
)

echo Building UI locally...
call npm run build
if errorlevel 1 goto fail

echo Building UI Docker image...
docker build --no-cache --platform linux/amd64 -t %IMAGE_NAME%:latest .
if errorlevel 1 goto fail

echo Pushing UI image to GHCR...
docker push %IMAGE_NAME%:latest
if errorlevel 1 goto fail

set "KUBECONFIG=%RADIXAL_CLUSTER_CONFIG%"
kubectl apply -f k8s/ -n %NAMESPACE%
if errorlevel 1 goto fail
kubectl rollout restart deployment %DEPLOYMENT_NAME% -n %NAMESPACE%
if errorlevel 1 goto fail
kubectl rollout status deployment %DEPLOYMENT_NAME% -n %NAMESPACE%
if errorlevel 1 goto fail

echo UI deployment complete.
endlocal
exit /b 0

:fail
echo UI deployment failed.
pause
endlocal
exit /b 1
