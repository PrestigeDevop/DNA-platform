@echo off
echo ===========================================
echo  DNA Platform - Test Skills API
echo ===========================================
echo.

REM Change to script directory
cd /d "%~dp0"

echo Starting API server...
echo.

REM Start API in background
start "DNA Platform API" cmd /c "cd src\05_DevUI\DNAPlatform.DevUI.API && dotnet run --urls http://localhost:5254"

REM Wait for server to start
echo Waiting for server to start...
timeout /t 5 /nobreak > nul

echo.
echo ===========================================
echo  Testing Skills API
echo ===========================================
echo.

echo [Test 1] List all skills:
echo -------------------------------------------
curl http://localhost:5254/api/skills
echo.
echo.

echo [Test 2] Execute MsgBoxAlertSkill (success):
echo -------------------------------------------
curl -X POST http://localhost:5254/api/skills/msgbox-alert/execute ^
  -H "Content-Type: application/json" ^
  -d "{\"message\": \"Hello from DNA Platform!\", \"msgType\": \"success\", \"title\": \"Test\"}"
echo.
echo.

echo [Test 3] Execute MsgBoxAlertSkill (warning):
echo -------------------------------------------
curl -X POST http://localhost:5254/api/skills/msgbox-alert/execute ^
  -H "Content-Type: application/json" ^
  -d "{\"message\": \"This is a warning!\", \"msgType\": \"warning\", \"title\": \"Warning\"}"
echo.
echo.

echo [Test 4] Execute ValidationSkill:
echo -------------------------------------------
curl -X POST http://localhost:5254/api/skills/validation/execute ^
  -H "Content-Type: application/json" ^
  -d "{\"data\": \"ATCGATCG\", \"validationType\": \"fasta\"}"
echo.
echo.

echo ===========================================
echo  Tests Complete!
echo ===========================================
echo.
echo Press any key to stop the server...
pause > nul

REM Kill the server taskkill /FI "WINDOWTITLE eq DNA Platform API*" /F 2>nul

echo.
echo Server stopped.
pause