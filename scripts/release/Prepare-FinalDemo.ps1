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

function Check-Audit([string]$raw,[switch]$AllowCandidates) {
  $begin = $raw.IndexOf('{'); $end = $raw.LastIndexOf('}')
  if ($begin -lt 0 -or $end -le $begin) { throw 'No audit JSON found.' }
  $json = $raw.Substring($begin,$end-$begin+1)
  try { $audit = $json | ConvertFrom-Json } catch { throw 'Invalid audit JSON.' }
  if ($null -eq $audit.candidateCount -or $audit.candidateCount -isnot [int]) { throw 'Missing or invalid candidateCount.' }
  if (-not $AllowCandidates -and $audit.candidateCount -ne 0) { throw 'Synthetic Catalog candidates found; no backup.' }
  return $json
}
function Check-CatalogSql([string]$output) {
  if (-not (@($output -split '[\r\n]+' | Where-Object { $_.Trim() -ceq 'FINAL_CATALOG_OK' }).Count)) {
    throw 'SQL Catalog validation failed; no backup.'
  }
}
function Check-Backup([string]$file) {
  if (-not (Test-Path -LiteralPath $file -PathType Leaf) -or (Get-Item -LiteralPath $file).Length -eq 0) {
    throw 'Backup missing or empty.'
  }
}
function Create-Admin([string[]]$compose) {
  if ($SkipAdmin -or (Read-Host 'Type ADMIN to create a Development Admin account, otherwise skip').Trim() -cne 'ADMIN') { return $false }
  $username = (Read-Host 'Admin username').Trim()
  if ($username -notmatch '^[A-Za-z0-9._-]{3,100}$' -or $username.Contains('@')) { throw 'Invalid Admin username.' }
  $secure = Read-Host 'Admin password (12-128 characters)' -AsSecureString
  $pointer = [IntPtr]::Zero
  try {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    if ($password.Length -lt 12 -or $password.Length -gt 128) { throw 'Admin password must have 12-128 characters.' }
    $env:DevelopmentSeed__Username = $username
    $env:DevelopmentSeed__Password = $password
    Call ($compose + @('run','--rm','-T','-e','DevelopmentSeed__Username','-e','DevelopmentSeed__Password','api','--seed-admin')) 'Create Development Admin'
    return $true
  } finally {
    Remove-Item Env:DevelopmentSeed__Username, Env:DevelopmentSeed__Password -ErrorAction SilentlyContinue
    if ($pointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
    $password = $null
  }
}
function CatalogQuery {
  return @'
SET NOCOUNT ON;
DECLARE @expected TABLE(Code nvarchar(50) PRIMARY KEY);
INSERT INTO @expected(Code) VALUES
(N'VX-AVAXIM-80U'),(N'VX-BEXSERO'),(N'VX-GARDASIL-9'),(N'VX-HAVAX-05'),(N'VX-HEXAXIM'),
(N'VX-INFANRIX-HEXA'),(N'VX-MMR-II'),(N'VX-NIMENRIX'),(N'VX-PREVENAR-13'),(N'VX-QDENGA'),
(N'VX-ROTARIX'),(N'VX-ROTATEQ'),(N'VX-SHINGRIX'),(N'VX-VARIVAX'),(N'VX-VAXIGRIP');
IF (SELECT COUNT(*) FROM dbo.VacXin) <> 15 THROW 51001, 'Incorrect vaccine count', 1;
IF EXISTS (SELECT Code FROM @expected EXCEPT SELECT Code FROM dbo.VacXin) THROW 51002, 'Missing vaccine', 1;
IF EXISTS (SELECT Code FROM dbo.VacXin EXCEPT SELECT Code FROM @expected) THROW 51003, 'Unexpected vaccine', 1;
IF EXISTS (SELECT 1 FROM dbo.GoiVacXin) THROW 51004, 'Unexpected package', 1;
IF EXISTS (SELECT 1 FROM dbo.VacXin WHERE Gia <= 0 OR TrangThai <> 'Active'
    OR DuongDanNguon IS NULL OR DuongDanNguon NOT LIKE 'https://%'
    OR NguonThongTin IS NULL OR NguonThongTin NOT LIKE '%09/11/2026%'
    OR NguonThongTin NOT LIKE '%https://vnvc.vn/gia-tiem-chung-vac-xin/%'
    OR NgayCapNhatThongTin IS NULL OR NULLIF(LTRIM(RTRIM(DoiTuongSuDung)), '') IS NULL
    OR NULLIF(LTRIM(RTRIM(DuongDung)), '') IS NULL OR NULLIF(LTRIM(RTRIM(LieuLuong)), '') IS NULL)
    THROW 51005, 'Incomplete vaccine provenance or price', 1;
IF EXISTS (SELECT 1 FROM dbo.VacXin AS v WHERE NOT EXISTS
    (SELECT 1 FROM dbo.VacXin_Benh AS vb WHERE vb.MaVacXin = v.MaVacXin))
    THROW 51006, 'Missing vaccine disease link', 1;
IF EXISTS (SELECT 1 FROM dbo.VacXin AS v WHERE NOT EXISTS
    (SELECT 1 FROM dbo.VaccineRegimen AS r JOIN dbo.VaccineRegimenDose AS d ON d.RegimenId = r.Id
     WHERE r.VaccineId = v.MaVacXin AND r.SourceUrl LIKE 'https://%'))
    THROW 51007, 'Missing sourced regimen', 1;
PRINT 'FINAL_CATALOG_OK';
'@
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
$json = Check-Audit $raw -AllowCandidates
$out = Join-Path $env:LOCALAPPDATA ("VAC-Demo/Submission/" + (Get-Date -Format yyyyMMdd-HHmmss) + '-' + [guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $out -Force | Out-Null
$report = Join-Path $out 'catalog-audit.json'
[IO.File]::WriteAllText($report,$json,(New-Object Text.UTF8Encoding($false)))
$null = Check-Audit $raw
Write-Host "Audit: $report" -ForegroundColor Green
$container = (Capture ($compose + @('ps','-q','sqlserver')) 'Find SQL container').Trim()
if ($container -notmatch '^[0-9a-f]{12,64}$') { throw 'SQL container ID missing or invalid.' }
$query = (CatalogQuery) -replace '[\r\n]+', ' '
$sqlValidation = '/opt/mssql-tools18/bin/sqlcmd -C -S localhost -d TiemChung -U sa -P "$MSSQL_SA_PASSWORD" -b -Q "' + $query.Trim() + '"'
$validation = Capture @('exec',$container,'sh','-c',$sqlValidation) 'Validate 15 vaccine SQL records'
Check-CatalogSql $validation
$adminReady = Create-Admin $compose
if (-not $adminReady) { Write-Host 'Authenticated Windows Catalog smoke: UNVERIFIED (no Admin created).' }
if (-not $SkipBackup) {
  # Password expands within SQL container, never in local PowerShell.
  $remoteBak = '/var/opt/mssql/backup/FinalDemo-' + [guid]::NewGuid().ToString('N') + '.bak'
  $sql = 'mkdir -p /var/opt/mssql/backup && /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -b -Q "BACKUP DATABASE [TiemChung] TO DISK = N''' + $remoteBak + ''' WITH CHECKSUM; RESTORE VERIFYONLY FROM DISK = N''' + $remoteBak + ''' WITH CHECKSUM;"'
  $null = Capture @('exec',$container,'sh','-c',$sql) 'BACKUP and RESTORE VERIFYONLY'
  $bak = Join-Path $out 'FinalDemo.bak'
  $dockerPath = $container + ':' + $remoteBak
  Call @('cp',$dockerPath,$bak) 'Copy verified DB backup outside Git'
  Check-Backup $bak
  Write-Host "Backup: $bak" -ForegroundColor Green
} else { Write-Host 'Backup SKIPPED; final database backup: UNVERIFIED.' }
Write-Host 'MANUAL: inspect Web and Windows Catalog; check 15 real vaccine records, no fake package.' -ForegroundColor Yellow
if (ShouldLaunch) {
  $previous = $env:VAC_COMPOSE_PROJECT
  try {
    $env:VAC_COMPOSE_PROJECT = $ComposeProject
    & (Join-Path $root 'scripts/devtools/VAC.ps1') -Mode start -ComposeProject $ComposeProject
    if ($LASTEXITCODE -ne 0) { throw 'VAC launcher failed; UI smoke is UNVERIFIED.' }
  } finally { $env:VAC_COMPOSE_PROJECT = $previous }
  Write-Host 'Web/Windows launched; manual UI smoke remains UNVERIFIED.'
} else { Write-Host 'Web/Windows smoke: UNVERIFIED.' }
Write-Host "Prepared files in: $out" -ForegroundColor Green
Write-Host 'Not a submission archive yet. Check UI, privacy, signatures/Turnitin and restore on a disposable DB.'
Write-Host 'Never submit infra/.env, credentials, tokens, or real personal/health data.'

}
if ($MyInvocation.InvocationName -ne '.') { Invoke-FinalDemo }
