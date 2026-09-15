$api='C:\Users\prest\OneDrive\Desktop\VibeCoding\DNA-platform Cat\src\05_DevUI\DNAPlatform.DevUI.API\DNAPlatform.DevUI.API.csproj' 
$skills='C:\Users\prest\OneDrive\Desktop\VibeCoding\DNA-platform Cat\src\06_Skills\DNAPlatform.Skills.csproj' 
dotnet build -LiteralPath:$api --nologo -v q 
dotnet build -LiteralPath:$skills --nologo -v q 
