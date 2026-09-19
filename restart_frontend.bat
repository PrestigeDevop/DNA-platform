@echo off
REM Dev helper: start backend + frontend detached (same as run-all.bat, without pausing)
for /f "tokens=5" %%p in ('netstat -aon ^| findstr :5254 ^| findstr LISTENING') do taskkill /f /pid %%p >nul 2>&1
cd /d "%~dp0src\05_DevUI\DNAPlatform.DevUI.API\DNAPlatform.DevUI.API"
start "DNA-Backend" /min cmd /c "dotnet run --urls http://localhost:5254 > backend_run.log 2>&1"
cd /d "%~dp0src\05_DevUI\DNAPlatform.DevUI.Web"
start "DNA-Frontend" /min cmd /c "npm run dev > frontend_run.log 2>&1"
exit /b 0
