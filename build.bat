@echo off
setlocal

REM Change to script directory
cd /d "%~dp0"

echo ===========================================
echo  DNA Platform - Build
echo ===========================================
echo.

echo Building Skills Library...
dotnet build src\06_Skills\DNAPlatform.Skills.csproj -v minimal
if %ERRORLEVEL% neq 0 (
    echo.
    echo BUILD FAILED!
    pause
    exit /b 1
)

echo.
echo Building API...
dotnet build src\05_DevUI\DNAPlatform.DevUI.API\DNAPlatform.DevUI.API.csproj -v minimal
if %ERRORLEVEL% neq 0 (
    echo.
    echo BUILD FAILED!
    pause
    exit /b 1
)

echo.
echo ===========================================
echo  BUILD SUCCESSFUL!
echo ===========================================
echo.
pause