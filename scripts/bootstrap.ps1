<#
.SYNOPSIS
    One-time environment setup for the DNA Platform.

.DESCRIPTION
    Installs everything needed before `run-all.bat` will work: .NET packages, a
    full solution build, web dependencies, the generated Prisma client and the
    SQLite database.

    Idempotent. Running it twice is safe and cheap: `dotnet restore`, `npm ci`
    style installs and Prisma generation all converge, and the database push is
    skipped when `prisma\dev.db` already exists.

    Design constraints (docs/refactor.md section 15, and AGENTS.md):
      * PowerShell 5.1 only. No `??`, no ternary, no `-Parallel`, no `Get-Error`.
      * Every external command is checked via $LASTEXITCODE. $ErrorActionPreference
        does not catch a native command that exits non-zero.
      * A failure names the step and prints the exact command to retry by hand.
      * `mise` is probed, never required. MiseService degrades to yellow when it
        is absent and so does this script.

.PARAMETER ForceDatabase
    Run `npx prisma db push` even when `prisma\dev.db` already exists. Use after
    editing `schema.prisma`.

.PARAMETER SkipBuild
    Skip `dotnet build` (restore only). Useful when iterating on the web project.

.EXAMPLE
    .\scripts\bootstrap.ps1
    .\scripts\bootstrap.ps1 -ForceDatabase
#>
[CmdletBinding()]
param(
    [switch]$ForceDatabase,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

# scripts/ is one level below the repo root; anchor every path from here so the
# script works regardless of the caller's current directory.
$RepoRoot = Split-Path -Parent $PSScriptRoot
$WebDir = Join-Path $RepoRoot 'src\05_DevUI\DNAPlatform.DevUI.Web'
$Solution = Join-Path $RepoRoot 'DNAPlatform.sln'
$PrismaDb = Join-Path $WebDir 'prisma\dev.db'

$script:Step = 'startup'
$script:Warnings = New-Object System.Collections.ArrayList
$script:StartedAt = Get-Date

function Write-Step {
    param([string]$Message)
    Write-Host ''
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Ok {
    param([string]$Message)
    Write-Host "    [ok] $Message" -ForegroundColor Green
}

function Write-Note {
    param([string]$Message)
    Write-Host "    [..] $Message" -ForegroundColor DarkGray
}

function Write-Warn {
    param([string]$Message)
    Write-Host "    [!!] $Message" -ForegroundColor Yellow
    [void]$script:Warnings.Add($Message)
}

function Write-Fail {
    param([string]$Message)
    Write-Host "    [XX] $Message" -ForegroundColor Red
}

# Runs a native command and throws a message that names the step and gives the
# exact command to paste. `dotnet` and `npm` exit non-zero without raising a
# PowerShell error, so nothing else would notice.
function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)][string]$File,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [string]$RetryHint
    )

    Write-Host "    $ $File $($Arguments -join ' ')" -ForegroundColor DarkGray

    & $File @Arguments
    $code = $LASTEXITCODE

    if ($code -ne 0) {
        $retry = if ($RetryHint) { $RetryHint } else { "$File $($Arguments -join ' ')" }
        throw "'$File $($Arguments -join ' ')' exited $code. Retry by hand: $retry"
    }
}

# Prerequisite versions. Mismatches warn, they do not fail: a 10.x SDK builds
# net8.0 targets fine, and a Node 20 patch release is not a reason to stop.
function Test-Toolchain {
    Write-Step 'Checking the toolchain'

    $dotnetVersion = & dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $dotnetVersion) {
        throw "'dotnet --version' failed. Install the .NET 8 SDK from https://dotnet.microsoft.com/download and put it on PATH."
    }

    $dotnetVersion = $dotnetVersion.Trim()
    Write-Ok "dotnet $dotnetVersion"

    # The target framework is net8.0. A 9/10 SDK still builds it, so only a
    # pre-3.x SDK is genuinely unusable.
    $major = [int]($dotnetVersion -split '\.')[0]
    if ($major -lt 8) {
        Write-Warn "dotnet $dotnetVersion is older than the net8.0 target framework. Install the .NET 8 SDK."
    }

    $nodeVersion = & node --version 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $nodeVersion) {
        throw "'node --version' failed. Install Node.js 20 or newer from https://nodejs.org and put it on PATH."
    }

    $nodeVersion = $nodeVersion.Trim()
    $nodeMajor = [int]($nodeVersion.TrimStart('v') -split '\.')[0]
    Write-Ok "node $nodeVersion"

    if ($nodeMajor -lt 20) {
        Write-Warn "node $nodeMajor is below the supported floor of 20. SvelteKit 2 and Vite will misbehave."
    }

    $npmVersion = & npm --version 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $npmVersion) {
        throw "'npm --version' failed. npm ships with Node.js; reinstall Node.js so npm is on PATH."
    }

    Write-Ok "npm $($npmVersion.Trim())"

    # mise is optional. When it is on PATH, install the pinned tools so the
    # project resolves the same versions CI does. When it is absent, the
    # platform defaults in mise.toml (.NET 8, Node LTS) apply instead and
    # EnvironmentController reports yellow.
    $mise = Get-Command mise -ErrorAction SilentlyContinue
    if ($mise) {
        $miseVersion = & mise --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Ok "mise $($miseVersion.Trim())"

            $script:Step = 'mise install'
            Invoke-Checked -File 'mise' -Arguments @('install') `
                -RetryHint "mise --cd `"$RepoRoot`" install"

            $script:Step = 'mise tasks ls'
            Write-Host '    declared tasks:' -ForegroundColor DarkGray
            & mise --cd $RepoRoot tasks ls
            if ($LASTEXITCODE -ne 0) {
                Write-Warn '`mise tasks ls` failed. mise.toml may be malformed; the platform still builds without it.'
            }
        }
        else {
            Write-Warn 'mise is on PATH but `mise --version` failed. Continuing with platform defaults.'
        }
    }
    else {
        Write-Note 'mise not found on PATH; using the platform defaults (.NET 8, Node LTS) from mise.toml.'
    }
}

try {
    Write-Host '================================================' -ForegroundColor White
    Write-Host ' DNA Platform - bootstrap' -ForegroundColor White
    Write-Host " $RepoRoot" -ForegroundColor DarkGray
    Write-Host '================================================' -ForegroundColor White

    if (-not (Test-Path -LiteralPath $Solution)) {
        throw "DNAPlatform.sln not found at $Solution."
    }

    # --- 1. .NET restore ---
    $script:Step = 'dotnet restore'
    Write-Step 'Restoring .NET packages'
    Push-Location $RepoRoot
    try {
        Invoke-Checked -File 'dotnet' -Arguments @('restore', 'DNAPlatform.sln') `
            -RetryHint "dotnet restore `"$Solution`""
    }
    finally {
        Pop-Location
    }
    Write-Ok 'solution restored'

    # --- 2. .NET build ---
    if ($SkipBuild) {
        Write-Step 'Skipping the build (-SkipBuild)'
        Write-Note 'run-all.bat builds on demand, so this is only unsafe if you skip the build there too.'
    }
    else {
        $script:Step = 'dotnet build'
        Write-Step 'Building DNAPlatform.sln'

        Push-Location $RepoRoot
        try {
            # AGENTS.md: a stale process on :5254 locks DNAPlatform.AgentFramework.dll
            # and the build fails with MSB3021. Say so instead of emitting the retry noise.
            $holding = netstat -ano | Select-String ':5254' | Select-String 'LISTENING'
            if ($holding) {
                Write-Note 'something is listening on :5254; if the copy step fails, stop it first (run-all.bat does this).'
            }

            Invoke-Checked -File 'dotnet' -Arguments @('build', 'DNAPlatform.sln', '--nologo') `
                -RetryHint "dotnet build `"$Solution`" --nologo"
        }
        finally {
            Pop-Location
        }

        Write-Ok 'solution built'
    }

    # --- 3. Web dependencies ---
    $script:Step = 'npm install'
    Write-Step 'Installing web dependencies'
    Push-Location $WebDir
    try {
        Invoke-Checked -File 'npm' -Arguments @('install') `
            -RetryHint "npm --prefix `"$WebDir`" install"
    }
    finally {
        Pop-Location
    }
    Write-Ok 'node_modules present'

    # --- 4. Prisma client + database ---
    # DATABASE_URL lives in the web project's .env and points at SQLite
    # (file:./dev.db). Both prisma commands read it from there.
    $envFile = Join-Path $WebDir '.env'
    if (-not (Test-Path -LiteralPath $envFile)) {
        Write-Warn ".env is missing from the web project. Prisma needs DATABASE_URL; copy .env.example and set it."
    }

    $script:Step = 'npx prisma generate'
    Write-Step 'Generating the Prisma client'
    Push-Location $WebDir
    try {
        # A running dev server has query_engine-windows.dll.node loaded, and Prisma
        # cannot rename over it (EPERM). Detect that before it fails confusingly.
        if (netstat -ano | Select-String ':5173' | Select-String 'LISTENING') {
            Write-Warn 'the web dev server is running on :5173. `prisma generate` will fail with EPERM until it is stopped.'
        }

        Invoke-Checked -File 'npx' -Arguments @('prisma', 'generate') `
            -RetryHint "npm --prefix `"$WebDir`" run prisma:generate"

        # --- 5. Database ---
        if ((Test-Path -LiteralPath $PrismaDb) -and -not $ForceDatabase) {
            Write-Note 'prisma\dev.db already exists; skipping `prisma db push`.'
            Write-Note 'rerun with -ForceDatabase after editing schema.prisma.'
        }
        else {
            $script:Step = 'npx prisma db push'
            Write-Step 'Pushing the Prisma schema to SQLite'

            if ($ForceDatabase) {
                Write-Note '-ForceDatabase: pushing even though dev.db exists.'
            }

            Invoke-Checked -File 'npx' -Arguments @('prisma', 'db', 'push') `
                -RetryHint "npm --prefix `"$WebDir`" run prisma:db:push"

            Write-Ok 'database schema up to date'
        }
    }
    finally {
        Pop-Location
    }

    # --- 6. Summary ---
    $elapsed = (Get-Date) - $script:StartedAt

    Write-Host ''
    Write-Host '================================================' -ForegroundColor White
    Write-Host ' Bootstrap complete' -ForegroundColor Green
    Write-Host '================================================' -ForegroundColor White

    Write-Host ''
    Write-Host '  Frontend:     http://localhost:5173'
    Write-Host '  Backend API:  http://localhost:5254'
    Write-Host '  Swagger Docs: http://localhost:5254/swagger'
    Write-Host '  Health Check: http://localhost:5254/health'
    Write-Host ''
    Write-Host '  Now run:      .\run-all.bat' -ForegroundColor Cyan
    Write-Host "  (took $([int]$elapsed.TotalSeconds)s)"

    if ($script:Warnings.Count -gt 0) {
        Write-Host ''
        Write-Host "  $($script:Warnings.Count) warning(s):" -ForegroundColor Yellow
        foreach ($warning in $script:Warnings) {
            Write-Host "   - $warning" -ForegroundColor Yellow
        }
    }

    exit 0
}
catch {
    Write-Host ''
    Write-Host '================================================' -ForegroundColor White
    Write-Host ' Bootstrap failed' -ForegroundColor Red
    Write-Host '================================================' -ForegroundColor White
    Write-Fail "step: $($script:Step)"
    Write-Fail $_.Exception.Message

    Write-Host ''
    Write-Host '  Fix the cause, then re-run:' -ForegroundColor Yellow
    Write-Host "    .\scripts\bootstrap.ps1" -ForegroundColor Yellow
    Write-Host ''
    Write-Host '  If the failure is a locked DLL, stop the stale server first:' -ForegroundColor Yellow
    Write-Host '    netstat -ano | findstr :5254' -ForegroundColor Yellow
    Write-Host '    taskkill /F /PID <pid>' -ForegroundColor Yellow

    exit 1
}
