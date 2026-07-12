@echo off
setlocal
cd /d "%~dp0"

set "IMAGE_NAME=ghcr.io/radixal-sro/bcg-hub-api"
set "NAMESPACE=bcg-hub"
set "DEPLOYMENT_NAME=bcg-hub-api"

if "%RADIXAL_CLUSTER_CONFIG%"=="" (
    echo ERROR: RADIXAL_CLUSTER_CONFIG environment variable is not set.
    goto fail
)

echo Publishing API locally...
if exist publish rd /s /q publish
dotnet publish BcgHub.Api.csproj -c Release -o ./publish /p:UseAppHost=false
if errorlevel 1 goto fail

echo Building API Docker image...
docker build --platform linux/amd64 -t %IMAGE_NAME%:latest .
if errorlevel 1 goto fail

echo Pushing API image to GHCR...
docker push %IMAGE_NAME%:latest
if errorlevel 1 goto fail

set "KUBECONFIG=%RADIXAL_CLUSTER_CONFIG%"
kubectl apply -f k8s/ -n %NAMESPACE%
if errorlevel 1 goto fail
kubectl rollout restart deployment %DEPLOYMENT_NAME% -n %NAMESPACE%
if errorlevel 1 goto fail
kubectl rollout status deployment %DEPLOYMENT_NAME% -n %NAMESPACE%
if errorlevel 1 goto fail

echo API deployment complete.
endlocal
exit /b 0

:fail
echo API deployment failed.
pause
endlocal
exit /b 1
