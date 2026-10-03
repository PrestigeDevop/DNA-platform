@echo off
setlocal
cd /d "%~dp0src\05_DevUI\DNAPlatform.DevUI.Web"

echo ================================================
echo  DNA Platform - Setup Prisma Database
echo ================================================
echo.

:: Recreate .env cleanly (rules out encoding/BOM issues from OneDrive)
echo Writing .env ...
> .env echo DATABASE_URL="file:./dev.db"
>> .env echo BACKEND_URL="http://127.0.0.1:5254"
echo Done.
echo.

:: Generate the Prisma client (required for @prisma/client import)
echo [1/3] prisma generate ...
call npx prisma generate
if errorlevel 1 (
    echo [FAIL] prisma generate failed!
    pause
    exit /b 1
)
echo.

:: Push schema to SQLite - creates prisma\dev.db
echo [2/3] prisma db push ...
call npx prisma db push
if errorlevel 1 (
    echo [FAIL] prisma db push failed!
    pause
    exit /b 1
)
echo.

:: Verify
echo [3/3] Verifying ...
if exist prisma\dev.db (
    echo [OK] prisma\dev.db ready!
) else (
    echo [FAIL] prisma\dev.db still missing!
    pause
    exit /b 1
)

echo.
echo ================================================
echo  Database setup complete!
echo ================================================
pause