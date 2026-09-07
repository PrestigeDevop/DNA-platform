@echo off
echo ===========================================
echo  DNA Platform - Build Skills System
echo ===========================================
echo.

REM Change to script directory
cd /d "%~dp0"

echo [1/3] Building Skills Library...
dotnet build "src\06_Skills\DNAPlatform.Skills.csproj" -v minimal
if %ERRORLEVEL% neq 0 (
    echo ERROR: Skills build failed!
    pause
    exit /b 1
)
echo.

echo [2/3] Building API...
dotnet build "src\05_DevUI\DNAPlatform.DevUI.API\DNAPlatform.DevUI.API.csproj" -v minimal
if %ERRORLEVEL% neq 0 (
    echo ERROR: API build failed!
    pause
    exit /b 1
)
echo.

echo [3/3] Build Complete!
echo ===========================================
echo  SUCCESS: All projects built successfully
echo ===========================================
echo.
echo To run the API:
echo   cd src\05_DevUI\DNAPlatform.DevUI.API
echo   dotnet run --urls http://localhost:5254
echo.
pause