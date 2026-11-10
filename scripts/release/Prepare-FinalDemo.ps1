# DATA-R2 final demo helper. Windows PowerShell 5.1+, run after pulling main.
# NEVER deletes an existing SQL volume, database, containers or secrets.
[CmdletBinding()]
param(
  [ValidatePattern('^[a-z0-9][a-z0-9_-]*$')][string]$ComposeProject = 'vac-final',
  [switch]$SkipBackup,
  [switch]$SkipLaunch,
  [switch]$SkipAdmin
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
function MustHave([string]$name) {
  if (-not (Get-Command $name -ErrorAction SilentlyContinue)) { throw "Missing $name." }
}
function Capture([string[]]$arguments, [string]$label) {
  Write-Host "[STEP] $label" -ForegroundColor Cyan
  $prev = $ErrorActionPreference
  try {
    $ErrorActionPreference = 'Continue'
    $lines = @(& docker @arguments 2>&1)
    $code = $LASTEXITCODE
  } finally { $ErrorActionPreference = $prev }
  if ($code -ne 0) { throw "$label failed. Review privately; do not share logs or credentials." }
  return ($lines | Out-String)
}
function Call([string[]]$arguments, [string]$label) {
  $null = Capture $arguments $label
}
function RequireFreePort([int]$port) {
  if (@(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue).Count -gt 0) {
    throw "Port $port is in use. Stop OLD VAC app using VAC-Stop.cmd and rerun; this script never stops it."
  }
}
function Check-FreshProject([string[]]$compose, [string]$project) {
  $volumes = Capture @('volume','ls','--format','{{.Name}}') 'Check SQL volumes'
  if (@($volumes -split '[\r\n]+' | Where-Object { $_.Trim() -eq ($project + '_sqlserver-data') }).Count -gt 0) {
    throw 'Existing SQL volume. Use a different Compose project; existing data is untouched.'
  }
  $containers = Capture ($compose + @('ps','-a','-q')) 'Check project containers'
  if ($containers.Trim()) { throw 'Existing project containers. This helper prepares new SQL only.' }
  foreach ($port in @(14333,5080,5173)) { RequireFreePort $port }
}
function ShouldLaunch {
  if ($SkipLaunch) { return $false }
  return ((Read-Host 'Type OPEN to launch Web and Windows, otherwise skip').Trim() -ceq 'OPEN')
}
function Invoke-FinalDemo {
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location $root
if (-not (Test-Path 'infra/compose.yaml') -or -not (Test-Path 'infra/.env')) {
  throw 'Missing infra/compose.yaml or local infra/.env.'
}
MustHave docker; MustHave git
$branch = (& git branch --show-current).Trim()
if ($LASTEXITCODE -ne 0 -or $branch -ne 'main') {
  throw 'Switch to main and run git pull --ff-only before using this release helper.'
}
$head = (& git rev-parse HEAD).Trim()
$origin = (& git rev-parse origin/main).Trim()
if ($LASTEXITCODE -ne 0 -or $head -ne $origin) { throw 'Local main must match origin/main.' }
$prev = $ErrorActionPreference
try {
  $ErrorActionPreference = 'Continue'
  $remote = (& git ls-remote origin refs/heads/main 2>&1 | Out-String).Trim()
  $remoteCode = $LASTEXITCODE
} finally { $ErrorActionPreference = $prev }
if ($remoteCode -ne 0 -or $remote -notmatch '^([0-9a-f]{40})\s+refs/heads/main$' -or $Matches[1] -cne $head) {
  throw 'Cannot verify exact remote main. Run git pull --ff-only before retrying.'
}
$dirty = @(& git status --porcelain)
if ($LASTEXITCODE -ne 0 -or $dirty.Count -gt 0) { throw 'Worktree is dirty; review local changes.' }
$engine = (& docker info --format '{{.OSType}}' 2>$null | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or $engine -ne 'linux') { throw 'Start Docker Desktop Linux engine.' }
$compose = @('compose','--env-file','infra/.env','-p',$ComposeProject,'-f','infra/compose.yaml')
Check-FreshProject $compose $ComposeProject
Write-Host "Project $ComposeProject at $head" -ForegroundColor Yellow
Write-Host 'This prepares an isolated volume and never runs down -v, prune or delete.'
if ((Read-Host "Type CREATE to proceed").Trim() -cne 'CREATE') { throw 'Cancelled.' }
Check-FreshProject $compose $ComposeProject
Call ($compose + @('up','-d','--build')) 'Build SQL/migrations/API'
Call ($compose + @('run','--rm','-T','api','--seed-catalog')) 'Seed 15 source-backed vaccines'
$raw = Capture ($compose + @('run','--rm','-T','api','--audit-catalog')) 'Audit catalog read-only'
$begin = $raw.IndexOf('{'); $end = $raw.LastIndexOf('}')
if ($begin -lt 0 -or $end -le $begin) { throw 'No audit JSON found.' }
$json = $raw.Substring($begin,$end-$begin+1)
try { $audit = $json | ConvertFrom-Json } catch { throw 'Invalid audit JSON.' }
if ($null -eq $audit.candidateCount) { throw 'Missing candidateCount.' }
$out = Join-Path $env:LOCALAPPDATA ("VAC-Demo/Submission/" + (Get-Date -Format yyyyMMdd-HHmmss) + '-' + [guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $out -Force | Out-Null
$report = Join-Path $out 'catalog-audit.json'
[IO.File]::WriteAllText($report,$json,(New-Object Text.UTF8Encoding($false)))
Write-Host "Audit $report — candidates: $($audit.candidateCount)" -ForegroundColor Yellow
if ([int]$audit.candidateCount -ne 0) { throw 'Synthetic catalog candidates found. STOP, review JSON; no backup.' }
if (-not $SkipBackup) {
  # Password expands within SQL container, never in local PowerShell.
  $sql = 'mkdir -p /var/opt/mssql/backup && /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -b -Q "BACKUP DATABASE [TiemChung] TO DISK = N''/var/opt/mssql/backup/FinalDemo.bak'' WITH INIT, CHECKSUM; RESTORE VERIFYONLY FROM DISK = N''/var/opt/mssql/backup/FinalDemo.bak'' WITH CHECKSUM;"'
  $container = (Capture ($compose + @('ps','-q','sqlserver')) 'Find SQL container').Trim()
  if (-not $container) { throw 'SQL container not found.' }
  Capture @('exec',$container,'sh','-c',$sql) 'BACKUP and RESTORE VERIFYONLY' | Out-Null
  $bak = Join-Path $out 'FinalDemo.bak'
  Call @('cp',"${container}:/var/opt/mssql/backup/FinalDemo.bak",$bak) 'Copy verified DB backup outside Git'
  if (-not (Test-Path $bak) -or (Get-Item $bak).Length -eq 0) { throw 'Backup missing or empty.' }
  Write-Host "Backup: $bak" -ForegroundColor Green
}
Write-Host 'MANUAL: inspect Web and Windows Catalog; check 15 real vaccine records, no fake package.' -ForegroundColor Yellow
if (-not $SkipLaunch -and (Read-Host 'Type OPEN to launch Web + WPF, or anything else to skip').Trim() -ceq 'OPEN') {
  $previous = $env:VAC_COMPOSE_PROJECT
  try {
    $env:VAC_COMPOSE_PROJECT = $ComposeProject
    & (Join-Path $root 'scripts/devtools/VAC.ps1') -Mode start -ComposeProject $ComposeProject
  } finally { $env:VAC_COMPOSE_PROJECT = $previous }
}
Write-Host "Prepared files in: $out" -ForegroundColor Green
Write-Host 'Not a submission archive yet. Check UI, privacy, signatures/Turnitin and restore on a disposable DB.'
Write-Host 'Never submit infra/.env, credentials, tokens, or real personal/health data.'

}
if ($MyInvocation.InvocationName -ne '.') { Invoke-FinalDemo }
