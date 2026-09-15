@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

echo ================================================
echo  Stack verification (backend + frontend)
echo ================================================

:: --- Poll backend (fast, usually under 12s) ---
set /a t=0
:bwait
set /a t+=1
curl -s -m 2 http://127.0.0.1:5254/health >nul 2>&1
if not errorlevel 1 goto bok
if !t! geq 15 goto bfail
timeout /t 1 /nobreak >nul
goto bwait
:bfail
echo [FAIL] Backend not responding. Log tail:
powershell -NoProfile -Command "Get-Content '%~dp0test_backend.log' -Tail 10"
goto done
:bok
echo [OK] Backend healthy on :5254

:: --- Poll frontend /api/workflows (Prisma-backed: the real test) ---
set /a t=0
:fwait
set /a t+=1
curl -s -m 2 http://127.0.0.1:5173/api/workflows >nul 2>&1
if not errorlevel 1 goto fok
if !t! geq 15 goto tryagain
timeout /t 1 /nobreak >nul
goto fwait

:tryagain
echo [..] Frontend still starting (Vite cold start can take ~30s).
echo [..] Run this script again in 15 seconds.
goto done

:fok
echo [OK] Frontend up on :5173
echo.
echo --- TEST 1: Prisma/SQLite via SvelteKit (/api/workflows) ---
curl -s http://127.0.0.1:5173/api/workflows
echo.
echo.
echo --- TEST 2: /health proxied to .NET ---
curl -s http://127.0.0.1:5173/health
echo.
echo.
echo --- TEST 3: /api/skills proxied to .NET ---
curl -s -m 5 http://127.0.0.1:5173/api/skills | findstr /c:"msgbox-alert" >nul 2>&1
if not errorlevel 1 (
    echo [OK] Skills listed through the 5173 proxy
) else (
    echo [FAIL] Skills check failed
)

:done
echo.
echo Verification pass complete (servers still running).
pause