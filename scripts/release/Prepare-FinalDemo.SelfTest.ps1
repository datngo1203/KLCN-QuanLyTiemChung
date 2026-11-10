#requires -Version 5.1
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$target = Join-Path $PSScriptRoot 'Prepare-FinalDemo.ps1'
$tokens = $null; $errors = $null
$null = [Management.Automation.Language.Parser]::ParseFile($target,[ref]$tokens,[ref]$errors)
if ($errors.Count -gt 0) { throw "Script parser errors: $($errors -join '; ')" }
. $target -SkipAdmin -SkipLaunch

$script:passes = 0
function Pass([string]$label) { $script:passes++; Write-Host "PASS $label" }
function Reject([scriptblock]$action,[string]$pattern) {
  try { $null = & $action } catch {
    if ($_.Exception.Message -notlike "*$pattern*") { throw "Unexpected error: $($_.Exception.Message)" }
    return
  }
  throw "Expected error containing: $pattern"
}

$script:dockerMode = 'clean'
$script:dockerWrites = 0
function docker {
  $cmd = $args -join ' '
  $global:LASTEXITCODE = 0
  if ($cmd -like 'volume ls *') {
    if ($script:dockerMode -eq 'volume') { 'vac-final_sqlserver-data' }
    return
  }
  if ($cmd -like '* ps -a -q') {
    if ($script:dockerMode -eq 'containers') { 'c0ffee012345' }
    return
  }
  if ($cmd -like '*--seed-catalog*' -or $cmd -like '*BACKUP DATABASE*') {
    $script:dockerWrites++
    $global:LASTEXITCODE = 9
    'private sensitive output not to be shown'
    return
  }
  throw "Unexpected Docker command in mock: $cmd"
}
$script:busyPort = 0
function RequireFreePort([int]$port) {
  if ($script:busyPort -eq $port) { throw "Port $port is in use" }
}

$compose = @('compose','--env-file','infra/.env','-p','vac-final','-f','infra/compose.yaml')
Check-FreshProject $compose 'vac-final'
Pass 'unused project accepted'
$script:dockerMode = 'volume'
Reject { Check-FreshProject $compose 'vac-final' } 'Existing SQL volume'
Pass 'existing SQL volume rejected'
$script:dockerMode = 'containers'
Reject { Check-FreshProject $compose 'vac-final' } 'Existing project containers'
Pass 'existing containers rejected'
$script:dockerMode = 'clean'
$script:busyPort = 5080
Reject { Check-FreshProject $compose 'vac-final' } 'Port 5080'
$script:busyPort = 0
Pass 'port conflict rejected'

$null = Check-Audit '{"candidateCount":0,"candidates":[]}'
Reject { Check-Audit '{"candidateCount":2}' } 'Synthetic'
Reject { Check-Audit 'no json' } 'No audit'
Reject { Check-Audit '{"candidateCount":"0"}' } 'invalid'
Pass 'audit fail-closed'
Check-CatalogSql 'FINAL_CATALOG_OK'
Reject { Check-CatalogSql 'SQL failed' } 'validation failed'
$codes = @([regex]::Matches((CatalogQuery),"N'VX-[A-Z0-9-]+'") | ForEach-Object { $_.Value })
if ($codes.Count -ne 15 -or @($codes | Sort-Object -Unique).Count -ne 15) { throw 'SQL Catalog expectation must include 15 distinct codes.' }
Pass 'SQL check and marker validated'

Reject { Call ($compose + @('run','--rm','-T','api','--seed-catalog')) 'Seed' } 'Seed failed'
Reject { Call @('exec','c0ffee012345','sh','-c','BACKUP DATABASE') 'Backup' } 'Backup failed'
if ($script:dockerWrites -ne 2) { throw 'Expected two simulated failed Docker mutations.' }
Pass 'seed/backup failures sanitized'

$temp = Join-Path ([IO.Path]::GetTempPath()) ('vac-' + [guid]::NewGuid().ToString('N') + '.bak')
try {
  Reject { Check-Backup $temp } 'missing or empty'
  [IO.File]::WriteAllBytes($temp,[byte[]]@())
  Reject { Check-Backup $temp } 'missing or empty'
  [IO.File]::WriteAllBytes($temp,[byte[]]@(1,2,3))
  Check-Backup $temp
} finally {
  Remove-Item -LiteralPath $temp -Force -ErrorAction SilentlyContinue
}
Pass 'backup presence validated'

if (Create-Admin $compose) { throw 'SkipAdmin must not create accounts.' }
if (ShouldLaunch) { throw 'SkipLaunch must not start UI.' }
if ($script:dockerWrites -ne 2) { throw 'Unexpected Docker write attempted.' }
Pass 'Admin and UI can be skipped'
Write-Host "Release helper self-test: $script:passes PASS, no real Docker or SQL used."
