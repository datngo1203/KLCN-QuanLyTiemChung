#requires -Version 5.1
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$target = Join-Path $PSScriptRoot 'Prepare-FinalDemo.ps1'
$tokens = $null; $errors = $null
$null = [Management.Automation.Language.Parser]::ParseFile($target,[ref]$tokens,[ref]$errors)
if ($errors.Count -gt 0) { throw "Script parser errors: $($errors -join '; ')" }
$finish = Join-Path $PSScriptRoot 'Finish-FinalDemo.ps1'
$tokens = $null; $errors = $null
$null = [Management.Automation.Language.Parser]::ParseFile($finish,[ref]$tokens,[ref]$errors)
if ($errors.Count -gt 0) { throw "Finish script parser errors: $($errors -join '; ')" }
$finishScript = [IO.File]::ReadAllText($finish)
foreach ($role in @('demo.admin','demo.reception','demo.doctor','demo.nurse','demo.cashier','demo.warehouse','demo.customer@example.invalid')) {
  if (-not $finishScript.Contains("'$role'")) { throw "Missing fixed demo account: $role" }
}
if ($finishScript.Contains('down -v') -or $finishScript.Contains('volume rm')) { throw 'Unsafe Docker cleanup in final script.' }

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
  if ($cmd -like '* exec -T sqlserver sh -c *') {
    if ($cmd -match ' -Q ') { throw 'sqlcmd must read SQL from stdin, not -Q.' }
    $statement = ($input | Out-String)
    if ($statement -notmatch 'PRINT .FINAL_CATALOG_OK.') { throw 'SQL text was not piped to docker.' }
    if ($script:dockerMode -eq 'sql-fail') {
      $global:LASTEXITCODE = 9
      'Private mock SQL error'
      return
    }
    'FINAL_CATALOG_OK'
    return
  }
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
$script:dockerMode = 'sql-pass'
$valid = SqlInput $compose (CatalogQuery) 'Validate'
Check-CatalogSql $valid
$script:dockerMode = 'sql-fail'
Reject { SqlInput $compose (CatalogQuery) 'Validate' } 'Validate failed'
$script:dockerMode = 'clean'
Pass 'SQL stdin execution and failure handling'
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

$accountReader = Join-Path $PSScriptRoot 'Read-FinalDemoAccounts.ps1'
$tokens = $null; $errors = $null
$null = [Management.Automation.Language.Parser]::ParseFile($accountReader,[ref]$tokens,[ref]$errors)
if ($errors.Count -gt 0) { throw "Local account reader parser errors: $($errors -join '; ')" }
. $accountReader
$testDir = Join-Path ([IO.Path]::GetTempPath()) ('vac-demo-account-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDir | Out-Null
$configFile = Join-Path $testDir 'demo-accounts.local.json'
$keys = @('demo.admin','demo.reception','demo.doctor','demo.nurse','demo.cashier','demo.warehouse','demo.customer@example.invalid')
$fixture = @{}
foreach ($key in $keys) { $fixture[$key] = 'TEST-ONLY-' + $key + '-2026' }
try {
  [IO.File]::WriteAllText($configFile,($fixture | ConvertTo-Json))
  $loaded = Get-FinalDemoAccountPasswords $configFile
  if ($loaded.Count -ne 7) { throw 'Seven demo credentials expected.' }
  foreach ($key in $keys) { if ($loaded[$key] -cne $fixture[$key]) { throw 'Local password mismatch.' } }
  $loaded.Clear()
  Pass 'Local config loads seven unique role/customer passwords'

  $fixture['demo.admin'] = 'short'
  [IO.File]::WriteAllText($configFile,($fixture | ConvertTo-Json))
  Reject { Get-FinalDemoAccountPasswords $configFile } '12-128'
  $fixture['demo.admin'] = $fixture['demo.nurse']
  [IO.File]::WriteAllText($configFile,($fixture | ConvertTo-Json))
  Reject { Get-FinalDemoAccountPasswords $configFile } 'unique'
  $fixture.Remove('demo.admin')
  [IO.File]::WriteAllText($configFile,($fixture | ConvertTo-Json))
  Reject { Get-FinalDemoAccountPasswords $configFile } 'exactly seven'
  $fixture['unknown.account'] = 'TEST-ONLY-UNEXPECTED-2026'
  [IO.File]::WriteAllText($configFile,($fixture | ConvertTo-Json))
  Reject { Get-FinalDemoAccountPasswords $configFile } 'Missing fixed username'
  [IO.File]::WriteAllText($configFile,'not json')
  Reject { Get-FinalDemoAccountPasswords $configFile } 'Invalid local'
  Remove-Item -LiteralPath $configFile
  Reject { Get-FinalDemoAccountPasswords $configFile } 'Missing local'
  Pass 'Invalid, missing, duplicate or short local secrets rejected'
} finally {
  Remove-Item -LiteralPath $testDir -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "Release helper self-test: $script:passes PASS, no real Docker or SQL used."
