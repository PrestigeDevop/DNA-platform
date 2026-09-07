@echo off
echo ================================================
echo    DNA Platform - Starting All Services
echo ================================================
echo.

:: Kill any stale process holding port 5254 (prevents locked-DLL build failures)
for /f "tokens=5" %%p in ('netstat -aon ^| findstr :5254 ^| findstr LISTENING') do taskkill /f /pid %%p >nul 2>&1

echo Starting Backend API on http://localhost:5254 ...
echo Starting Frontend on http://localhost:5173 ...
echo.

:: Start Backend in a new window
start "DNA Platform Backend" cmd /k "cd /d %~dp0src\05_DevUI\DNAPlatform.DevUI.API\DNAPlatform.DevUI.API && dotnet run --urls http://localhost:5254"

:: Wait until the backend health endpoint responds (max ~30s)
set /a tries=0
:waitbackend
set /a tries+=1
curl -s -m 2 http://127.0.0.1:5254/health >nul 2>&1
if %errorlevel%==0 goto backendok
if %tries% geq 30 (
    echo WARNING: Backend did not respond within 30 seconds.
    echo Check the "DNA Platform Backend" window for errors.
    goto startfrontend
)
timeout /t 1 /nobreak >nul
goto waitbackend
:backendok
echo Backend is up and healthy!
echo.

:startfrontend
:: Start Frontend in a new window (creates SQLite DB on first run)
start "DNA Platform Frontend" cmd /k "cd /d %~dp0src\05_DevUI\DNAPlatform.DevUI.Web && npm install && (if not exist prisma\dev.db npx prisma db push) & npm run dev"

echo.
echo ================================================
echo    Services Started!
echo ================================================
echo.
echo  - Frontend:     http://localhost:5173
echo  - Backend API:  http://localhost:5254
echo  - Swagger Docs: http://localhost:5254/swagger
echo  - Health Check: http://localhost:5254/health
echo.
echo  Keep BOTH console windows open while using the app.
echo  Closing a window stops that service.
echo.
echo Press any key to close this window...
pause >nul
