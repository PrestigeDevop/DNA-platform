# Builds the whole solution. Paths are anchored with $PSScriptRoot so the script
# works from any working directory and from any checkout location.
#
# Equivalent to `mise run build`, `dotnet build DNAPlatform.sln`, or `build.bat`.
# For new users prefer .\scripts\bootstrap.ps1, which also restores and installs
# the frontend dependencies.

$ErrorActionPreference = 'Stop'

$solution = Join-Path $PSScriptRoot 'DNAPlatform.sln'
if (-not (Test-Path -LiteralPath $solution)) {
    throw "Solution not found: $solution"
}

Push-Location $PSScriptRoot
try {
    dotnet build $solution --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}
