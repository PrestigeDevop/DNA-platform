@echo off
echo Fixing duplicate Program.cs issue...

set "OLD_FILE=src\05_DevUI\DNAPlatform.DevUI.API\DNAPlatform.DevUI.API\Program.cs"

if exist "%OLD_FILE%" (
    echo Deleting duplicate: %OLD_FILE%
    del "%OLD_FILE%"
    echo Done!
) else (
    echo File not found: %OLD_FILE%
)

echo.
echo Cleaning up build artifacts...
if exist "src\05_DevUI\DNAPlatform.DevUI.API\DNAPlatform.DevUI.API\bin" (
    rmdir /s /q "src\05_DevUI\DNAPlatform.DevUI.API\DNAPlatform.DevUI.API\bin"
    echo Deleted bin folder
)

if exist "src\05_DevUI\DNAPlatform.DevUI.API\DNAPlatform.DevUI.API\obj" (
    rmdir /s /q "src\05_DevUI\DNAPlatform.DevUI.API\DNAPlatform.DevUI.API\obj"
    echo Deleted obj folder
)

echo.
echo Cleanup complete!
pause