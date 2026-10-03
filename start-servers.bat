@echo off
cd /d "%~dp0src\05_DevUI\DNAPlatform.DevUI.Web"

echo === Test 1: default PrismaClient (requires DATABASE_URL in env) ===
node test-prisma.cjs
echo exit code: %errorlevel%
echo.
echo === Test 2: explicit fallback URL (the db.ts fix) ===
node test-prisma.cjs "file:./dev.db"
echo exit code: %errorlevel%

