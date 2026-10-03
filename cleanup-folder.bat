@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

echo === Testing backend startup from parent folder ===

:: Kill anything on port 5254
for /f "tokens=5" %%p in ('netstat -aon ^| findstr :5254 ^| findstr LISTENING') do taskkill /f /pid %%p >nul 2>&1

:: Start backend minimized, log output to file
start "backend-test" /min cmd /c "cd /d %~dp0src\05_DevUI\DNAPlatform.DevUI.API && dotnet run --urls http://localhost:5254 > "%~dp0backend_test.log" 2>&1"

:: Poll health up to 30s
set /a tries=0
:wait
set /a tries+=1
curl -s -m 2 http://127.0.0.1:5254/health >nul 2>&1
if not errorlevel 1 goto healthy
if !tries! geq 30 goto failed
timeout /t 1 /nobreak >nul
goto wait

:healthy
echo.
echo [OK] Backend is healthy!
echo --- /health response: ---
curl -s http://127.0.0.1:5254/health
echo.
echo.
echo --- Skills check (msgbox-alert present?): ---
curl -s http://127.0.0.1:5254/api/skills | findstr /c:"msgbox-alert" >nul 2>&1
if not errorlevel 1 (
    echo [OK] msgbox-alert skill is registered!
) else (
    echo [FAIL] msgbox-alert skill NOT found in /api/skills
)
echo.
echo --- Executing msgbox-alert skill: ---
curl -s -X POST http://127.0.0.1:5254/api/skills/msgbox-alert/execute -H "Content-Type: application/json" -d "{\"message\":\"Hello from test 123\",\"msgType\":\"success\",\"title\":\"Greeting\"}"
echo.
goto cleanup

:failed
echo [FAIL] Backend did not respond in 30 seconds. Log tail:
powershell -NoProfile -Command "Get-Content '%~dp0backend_test.log' -Tail 20"
goto cleanup

:cleanup
for /f "tokens=5" %%p in ('netstat -aon ^| findstr :5254 ^| findstr LISTENING') do taskkill /f /pid %%p >nul 2>&1
echo.
echo Backend stopped. Test complete!
pause