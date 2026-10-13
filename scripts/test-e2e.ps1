<#
.SYNOPSIS
    Three-stage end-to-end verification for the DNA Platform stack.

.DESCRIPTION
    Stage A  `dotnet build DNAPlatform.sln`, then `npm run check` in
             DNAPlatform.DevUI.Web compared against a recorded
             svelte-check baseline. Fails only on a NEW diagnostic.
    Stage B  Starts the backend on :5254 and asserts /health,
             /api/info, /api/node-types, /api/status-values,
             /api/environment and POST /api/skills/msgbox-alert/execute,
             then runs `dnaplatform env`.
    Stage C  `npm run build`, starts the dev server on :5173 and asserts
             GET /api/workflows (Prisma), GET /health (proxied) and
             GET /api/skills (proxied), then runs `dnaplatform validate`
             and `dnaplatform status` against a stored workflow.

    The backend stays up through Stage C so the :5173 Vite proxy has a
    target; both listeners are stopped in the finally block so the run
    never leaks a port (a leaked :5254 breaks the next build, a leaked
    :5173 breaks the next `prisma generate`).

    PowerShell 5.1 only: no `??`, no ternary, no -Parallel.
    Exit code: 0 on pass, 1 on fail.

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-e2e.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# Debian often lacks ICU; the invariant mode keeps dotnet working there
# (mise.toml sets this for its own tasks, direct dotnet calls need it too).
$env:DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = '1'

# scripts/ is one level below the repo root; anchor every path here so the
# script works regardless of the caller's current directory.
$RepoRoot = Split-Path -Parent $PSScriptRoot
$Solution = Join-Path $RepoRoot 'DNAPlatform.sln'
$WebDir = Join-Path $RepoRoot 'src\05_DevUI\DNAPlatform.DevUI.Web'
$ApiProject = Join-Path $RepoRoot 'src\05_DevUI\DNAPlatform.DevUI.API\DNAPlatform.DevUI.API.csproj'
$CliProject = Join-Path $RepoRoot 'src\07_CLI\DNAPlatform.Cli\DNAPlatform.Cli.csproj'

$ApiUrl = 'http://127.0.0.1:5254'
$WebUrl = 'http://127.0.0.1:5173'

# Recorded `npm run check` baseline for DNAPlatform.DevUI.Web.
# Errors: zero. Warnings: 39 pre-existing a11y issues (docs/TODO.md).
# The gate fails only on a *new* diagnostic, so counts at or under
# these numbers pass.
$BaselineSvelteErrors = 0
$BaselineSvelteWarnings = 39

$script:Step = 'startup'
$script:ApiProc = $null
$script:WebProc = $null

function Write-Step { param([string]$Message) Write-Host "==> $Message" -ForegroundColor Cyan }
function Write-Ok { param([string]$Message) Write-Host "    [ok] $Message" -ForegroundColor Green }

# dotnet and npm exit non-zero without raising a PowerShell error, so
# every native call is checked through $LASTEXITCODE.
function Invoke-Checked {
    param([Parameter(Mandatory = $true)][string]$File, [Parameter(Mandatory = $true)][string[]]$Arguments)
    Write-Host "    $ $File $($Arguments -join ' ')" -ForegroundColor DarkGray
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "'$File $($Arguments -join ' ')' exited $LASTEXITCODE"
    }
}

# dnaplatform is not installed on PATH, so run it from source. Stage A
# built it as part of the solution, which makes --no-build safe and fast.
function Invoke-DnaPlatform {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)
    Write-Host "    $ dnaplatform $($Arguments -join ' ')" -ForegroundColor DarkGray
    & dotnet run --project $CliProject --no-build -- @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dnaplatform $($Arguments -join ' ') exited $LASTEXITCODE"
    }
}

# Polls until the URL answers 200 or the timeout elapses. Vite cold
# starts can take ~30 s and `dotnet run` builds on first launch.
function Wait-ForUrl {
    param([Parameter(Mandatory = $true)][string]$Url, [int]$TimeoutSeconds = 90)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3
            if ($response.StatusCode -eq 200) { return $true }
        }
        catch { }
        Start-Sleep -Seconds 2
    }
    return $false
}

# Kills every process LISTENING on $Port (netstat + taskkill /T /F).
# `dotnet run` and `npm run dev` both spawn children that survive the
# parent's death and keep holding the port, so the port is the truth.
function Stop-ListenerOnPort {
    param([Parameter(Mandatory = $true)][int]$Port)
    $listeners = netstat -ano | Select-String (":$Port ") | Select-String 'LISTENING'
    foreach ($line in $listeners) {
        $procId = ($line -split '\s+')[-1]
        if ($procId -match '^\d+$') {
            Write-Host "    killing PID $procId (port $Port)" -ForegroundColor DarkGray
            & taskkill /T /F /PID $procId 2>$null | Out-Null
        }
    }
}

# ---------------------------------------------------------------- stages

function Run-StageA {
    Write-Step 'Stage A: dotnet build + npm run check'

    # A stale server locks the output DLLs and the build fails with
    # MSB3021, so clear both ports before touching the toolchain.
    Stop-ListenerOnPort 5254
    Stop-ListenerOnPort 5173

    Invoke-Checked -File 'dotnet' -Arguments @('build', $Solution, '--nologo')
    Write-Ok 'solution built'

    $checkOutput = $null
    Push-Location $WebDir
    try {
        # svelte-check writes diagnostics to stderr; under
        # $ErrorActionPreference = 'Stop' the 2>&1 merge would throw on
        # every line, so relax it for the duration of the call.
        $previous = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        $checkOutput = npm run check 2>&1
        $ErrorActionPreference = $previous
    }
    finally { Pop-Location }

    $text = $checkOutput | Out-String
    if ([string]::IsNullOrWhiteSpace($text)) {
        throw "'npm run check' produced no output (is npm on PATH?)"
    }
    # 0 = clean, 1 = diagnostics found; anything else is a crash.
    if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 1) {
        throw "'npm run check' exited $LASTEXITCODE (expected 0 or 1)"
    }

    $errorCount = 0
    $warningCount = 0
    $summary = [regex]::Match($text, 'found\s+(\d+)\s+errors?\s+and\s+(\d+)\s+warnings?')
    if ($summary.Success) {
        $errorCount = [int]$summary.Groups[1].Value
        $warningCount = [int]$summary.Groups[2].Value
    }
    else {
        # No summary line: count the per-file diagnostic lines instead.
        $errorCount = @($checkOutput | Select-String -SimpleMatch ': Error:').Count
        $warningCount = @($checkOutput | Select-String -SimpleMatch ': Warning:').Count
    }

    Write-Host "    svelte-check: $errorCount errors, $warningCount warnings (baseline: $BaselineSvelteErrors / $BaselineSvelteWarnings)"
    if ($errorCount -gt $BaselineSvelteErrors -or $warningCount -gt $BaselineSvelteWarnings) {
        throw "svelte-check exceeded the baseline: $errorCount errors / $warningCount warnings"
    }
    Write-Ok 'svelte-check within baseline'
}

function Run-StageB {
    Write-Step 'Stage B: backend API on :5254'

    $logDir = Join-Path $env:TEMP 'dna-platform-e2e'
    if (-not (Test-Path -LiteralPath $logDir)) { New-Item -ItemType Directory -Path $logDir | Out-Null }
    $apiOut = Join-Path $logDir 'api-stdout.log'
    $apiErr = Join-Path $logDir 'api-stderr.log'

    $script:Step = 'Stage B: backend startup'
    $script:ApiProc = Start-Process -FilePath 'dotnet' `
        -ArgumentList @('run', '--project', "`"$ApiProject`"", '--no-launch-profile', '--urls', $ApiUrl) `
        -PassThru -WindowStyle Hidden -RedirectStandardOutput $apiOut -RedirectStandardError $apiErr

    if (-not (Wait-ForUrl "$ApiUrl/health")) {
        throw "backend did not answer $ApiUrl/health within 90 s (see $apiErr)"
    }

    $health = Invoke-RestMethod -Uri "$ApiUrl/health"
    if ($health.Status -ne 'Healthy') { throw "/health reported status '$($health.Status)'" }
    Write-Ok "/health: $($health.Status) (v$($health.Version))"

    $info = Invoke-RestMethod -Uri "$ApiUrl/api/info"
    if ([string]::IsNullOrEmpty($info.Version)) { throw '/api/info returned no version' }
    Write-Ok "/api/info: $($info.Name) v$($info.Version)"

    $nodeTypes = Invoke-RestMethod -Uri "$ApiUrl/api/node-types"
    if ($nodeTypes.NodeTypes -notcontains 'Input') { throw '/api/node-types does not list "Input"' }
    Write-Ok "/api/node-types: $($nodeTypes.NodeTypes.Count) node types"

    $statusValues = Invoke-RestMethod -Uri "$ApiUrl/api/status-values"
    if ($statusValues.WorkflowStatus -notcontains 'Completed') { throw '/api/status-values does not list "Completed"' }
    Write-Ok "/api/status-values: $($statusValues.WorkflowStatus.Count) workflow statuses"

    # MiseService degrades gracefully, so this is always 200 with a
    # green/yellow/red overall status even when mise is absent.
    $environment = Invoke-RestMethod -Uri "$ApiUrl/api/environment"
    if ($environment.OverallStatus -notin @('green', 'yellow', 'red')) {
        throw "/api/environment reported overall status '$($environment.OverallStatus)'"
    }
    Write-Ok "/api/environment: overall=$($environment.OverallStatus), mise installed=$($environment.MiseInstalled)"

    # msgbox-alert requires a 'message' input; without it the skill
    # answers 400 { Success = false }.
    $skill = Invoke-RestMethod -Method Post -Uri "$ApiUrl/api/skills/msgbox-alert/execute" `
        -Body '{"message": "e2e smoke test"}' -ContentType 'application/json'
    if ($skill.Success -ne $true -or $skill.SkillId -ne 'msgbox-alert') {
        throw "POST /api/skills/msgbox-alert/execute failed: $($skill | ConvertTo-Json -Compress)"
    }
    Write-Ok "skill execute: msgbox-alert (type=$($skill.Data.type))"

    Invoke-DnaPlatform -Arguments @('env', '--url', $ApiUrl)
    Write-Ok 'dnaplatform env'
}

function Run-StageC {
    Write-Step 'Stage C: dev server on :5173'

    $script:Step = 'Stage C: vite build'
    Push-Location $WebDir
    try {
        Invoke-Checked -File 'npm' -Arguments @('run', 'build')
    }
    finally { Pop-Location }
    Write-Ok 'frontend built'

    $logDir = Join-Path $env:TEMP 'dna-platform-e2e'
    $webOut = Join-Path $logDir 'web-stdout.log'
    $webErr = Join-Path $logDir 'web-stderr.log'

    $script:Step = 'Stage C: dev server startup'
    $script:WebProc = Start-Process -FilePath 'npm' `
        -ArgumentList @('run', 'dev', '--', '--port', '5173') `
        -PassThru -WindowStyle Hidden -WorkingDirectory $WebDir `
        -RedirectStandardOutput $webOut -RedirectStandardError $webErr

    # The Prisma route is the strongest readiness signal: Vite, the
    # generated client and SQLite are all up when it answers 200.
    if (-not (Wait-ForUrl "$WebUrl/api/workflows")) {
        throw "dev server did not answer $WebUrl/api/workflows within 90 s (see $webErr)"
    }

    # Prisma/SQLite through SvelteKit: lowercase envelope, pre-parsed
    # JSON blob columns.
    $workflows = Invoke-RestMethod -Uri "$WebUrl/api/workflows"
    if ($null -eq $workflows.workflows) { throw '/api/workflows did not return a workflows array' }
    Write-Ok "/api/workflows (Prisma): $($workflows.workflows.Count) stored"

    # Proxied straight to :5254 by vite.config.ts.
    $proxiedHealth = Invoke-RestMethod -Uri "$WebUrl/health"
    if ($proxiedHealth.Status -ne 'Healthy') { throw "/health via :5173 reported status '$($proxiedHealth.Status)'" }
    Write-Ok '/health proxied to :5254'

    $proxiedSkills = Invoke-RestMethod -Uri "$WebUrl/api/skills"
    if ($proxiedSkills.Skills.Count -lt 1) { throw '/api/skills via :5173 returned no skills' }
    Write-Ok "/api/skills proxied: $($proxiedSkills.Skills.Count) skills"

    # A stored workflow to validate. A single Input node carrying a
    # 'source' config is the smallest definition the structural
    # validator (POST /api/workflows/validate) accepts.
    $definition = @{
        name = 'e2e-smoke'
        description = 'Created by scripts/test-e2e.ps1'
        nodes = @(@{ id = 'n1'; name = 'Test Input'; nodeType = 'Input'; config = @{ source = 'e2e-test' } })
        connections = @()
    }
    $created = Invoke-RestMethod -Method Post -Uri "$WebUrl/api/workflows" `
        -Body ($definition | ConvertTo-Json -Depth 5) -ContentType 'application/json'
    $workflowId = $created.workflow.id
    Write-Ok "stored smoke workflow $workflowId"

    Invoke-DnaPlatform -Arguments @('validate', $workflowId, '--url', $WebUrl)
    Write-Ok "dnaplatform validate $workflowId"

    Invoke-DnaPlatform -Arguments @('status', '--url', $WebUrl)
    Write-Ok 'dnaplatform status'

    # Best effort: leave the database as it was found.
    Invoke-RestMethod -Method Delete -Uri "$WebUrl/api/workflows/$workflowId" -ErrorAction SilentlyContinue
}

# ---------------------------------------------------------------- run

try {
    Run-StageA
    Run-StageB   # backend stays up: Stage C asserts the :5173 proxy
    Run-StageC

    Write-Host ''
    Write-Host 'All stages passed' -ForegroundColor Green
    exit 0
}
catch {
    Write-Host ''
    Write-Host "E2E FAILED at $($script:Step): $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    foreach ($proc in @($script:WebProc, $script:ApiProc)) {
        if ($null -ne $proc) {
            try {
                if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
            }
            catch { }
        }
    }
    Start-Sleep -Milliseconds 500
    Stop-ListenerOnPort 5173
    Stop-ListenerOnPort 5254
}
