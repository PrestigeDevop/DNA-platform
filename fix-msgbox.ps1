$file = 'src\06_Skills\BuiltIn\MsgBoxAlertSkill.cs'
$lines = Get-Content $file
$goodLines = $lines[0..97]  # Keep only first 98 lines (0-indexed)
Set-Content $file $goodLines
Write-Host "Fixed! Kept first 98 lines."