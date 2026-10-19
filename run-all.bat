@echo off
REM ===========================================================================
REM DNA Platform — DeNovoAutomata Multi-Window System Launcher & TUI Entry Point
REM ===========================================================================
title DNA Platform Studio Launcher
cls

echo ===========================================================================
echo   DNA Platform - Autonomous Polyglot Workflow Engine
echo   Starting Multi-Process Environment (API :5254, DevUI :3000, Console TUI)
echo ===========================================================================

REM Ensure environment variables & logs folder are ready
if not exist "%~dp0logs" mkdir "%~dp0logs"
if not exist "%~dp0.env" (
    if exist "%~dp0.env.example" copy "%~dp0.env.example" "%~dp0.env" >nul
)

REM 1. Start ASP.NET Core Backend API in separate window
echo [1/3] Starting Backend API service on port 5254...
start "DNA Platform - Backend API (:5254)" cmd /k "cd /d %~dp0 && (dotnet run --project src\04_Hosting\DNAPlatform.DevUI.API\DNAPlatform.DevUI.API.csproj --urls http://localhost:5254 || (echo [ERROR] Backend failed to start. >> logs\platform_errors.log))"

REM 2. Start SvelteKit Web Studio in separate window
echo [2/3] Starting SvelteKit Web Studio on port 3000...
start "DNA Platform - SvelteKit Web Studio (:3000)" cmd /k "cd /d %~dp0 && (npm run dev || (echo [ERROR] Frontend failed to start. >> logs\platform_errors.log))"

REM 3. Launch DeNovoAutomata Console Studio / TUI in main window as entry point
echo [3/3] Launching Console Studio TUI...
timeout /t 2 >nul
node "%~dp0scripts\3-denovo-automata.mjs" %*
