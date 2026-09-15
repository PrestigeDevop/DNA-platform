$file = 'src\05_DevUI\DNAPlatform.DevUI.API\DNAPlatform.DevUI.API\Program.cs'
if (Test-Path $file) {
    Remove-Item $file -Force
    Write-Host "Deleted duplicate: $file"
} else {
    Write-Host "File not found: $file"
}

# Also check for other files in the subfolder
$folder = 'src\05_DevUI\DNAPlatform.DevUI.API\DNAPlatform.DevUI.API'
if (Test-Path $folder) {
    $files = Get-ChildItem $folder -Recurse
    if ($files.Count -eq 0) {
        Remove-Item $folder -Recurse -Force
        Write-Host "Deleted empty folder: $folder"
    } else {
        Write-Host "Folder contains files:"
        $files | ForEach-Object { Write-Host "  $($_.Name)" }
    }
}