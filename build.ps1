Set-Location 'C:\Users\prest\OneDrive\Desktop\VibeCoding\DNA-platform Cat\src\05_DevUI\DNAPlatform.DevUI.API'
dotnet build --nologo -v:q /p:FileLoggerMode=Standard /p:LogFile=../../../../out-api.txt
Get-Content 'C:\Users\prest\OneDrive\Desktop\VibeCoding\DNA-platform Cat\out-api.txt' | Select-String -Pattern 'error|Build succeeded' | Select-Object -First 8

Set-Location 'C:\Users\prest\OneDrive\Desktop\VibeCoding\DNA-platform Cat\src\06_Skills'
dotnet build --nologo -v:q /p:FileLoggerMode=Standard /p:LogFile=../../out-skills.txt
Get-Content 'C:\Users\prest\OneDrive\Desktop\VibeCoding\DNA-platform Cat\out-skills.txt' | Select-String -Pattern 'error|Build succeeded' | Select-Object -First 8
