#requires -Version 5.1
[CmdletBinding()]
param(
  [ValidatePattern('^[a-z0-9][a-z0-9_-]*$')][string]$ComposeProject = 'vac-final',
  [switch]$SkipBackup
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Prepare-FinalDemo.ps1') -ComposeProject $ComposeProject -SkipAdmin -SkipLaunch

function Seed-DemoAccount([string[]]$compose, [string]$username, [string]$role, [string]$command) {
  Write-Host "Prepare $username ($role)" -ForegroundColor Yellow
  $secure = Read-Host "Password for $username (12-128 chars)" -AsSecureString
  $ptr = [IntPtr]::Zero
  try {
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    if ($password.Length -lt 12 -or $password.Length -gt 128) {
      throw "Invalid password length for $username; account not changed."
    }
    $env:DevelopmentSeed__Username = $username
    $env:DevelopmentSeed__Password = $password
    $env:DevelopmentSeed__Role = $role
    Call ($compose + @('run','--rm','-T','-e','DevelopmentSeed__Username',
      '-e','DevelopmentSeed__Password','-e','DevelopmentSeed__Role','api',$command)) "Seed $username"
  } finally {
    Remove-Item Env:DevelopmentSeed__Username, Env:DevelopmentSeed__Password, Env:DevelopmentSeed__Role -ErrorAction SilentlyContinue
    if ($ptr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
    $password = $null
    $secure = $null
  }
}
function Check-DemoAccounts([string[]]$compose) {
  $sql = @'
SET NOCOUNT ON;
DECLARE @expected TABLE(Username nvarchar(100) PRIMARY KEY, RoleName nvarchar(30));
INSERT INTO @expected VALUES
(N'demo.admin',N'Admin'),(N'demo.reception',N'LeTan'),
(N'demo.doctor',N'BacSi'),(N'demo.nurse',N'DieuDuong'),
(N'demo.cashier',N'ThuNgan'),(N'demo.warehouse',N'ThuKho');
IF EXISTS (
  SELECT 1 FROM @expected x
  WHERE NOT EXISTS (
    SELECT 1 FROM dbo.TaiKhoan a JOIN dbo.NhanVien e ON a.MaNhanVien=e.MaNhanVien
    WHERE a.TenDangNhap=x.Username AND e.ChucDanh=x.RoleName
      AND a.AccountType='Employee' AND a.TrangThai='Active'
      AND e.TrangThai='Active'
  )
) THROW 51011, 'Missing or mismatched demo role', 1;
PRINT 'DEMO_ACCOUNTS_OK';
'@
  $result = SqlInput $compose $sql 'Verify six role-bound accounts'
  if (-not (@($result -split '[\r\n]+' | Where-Object { $_.Trim() -ceq 'DEMO_ACCOUNTS_OK' }).Count)) {
    throw 'Demo accounts verification failed.'
  }
}

$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location $root
MustHave git
MustHave docker
$branch = (& git branch --show-current | Out-String).Trim()
$head = (& git rev-parse HEAD | Out-String).Trim()
$origin = (& git rev-parse origin/main | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or $branch -ne 'main' -or $head -ne $origin) {
  throw 'Switch to updated main (git switch main; git pull --ff-only).'
}
$dirty = @(& git status --porcelain)
if ($LASTEXITCODE -ne 0 -or $dirty.Count -ne 0) { throw 'Worktree must be clean.' }
$compose = @('compose','--env-file','infra/.env','-p',$ComposeProject,'-f','infra/compose.yaml')
$container = (Capture ($compose + @('ps','-q','sqlserver')) 'Find existing SQL container').Trim()
if ($container -notmatch '^[0-9a-f]{12,64}$') { throw 'Existing SQL container required. No new database will be created.' }
Check-CatalogSql (SqlInput $compose (CatalogQuery) 'Verify existing curated Catalog')
$rawAudit = Capture ($compose + @('run','--rm','-T','api','--audit-catalog')) 'Audit existing Catalog'
$null = Check-Audit $rawAudit
Write-Host 'Curated Catalog verified; existing database will be preserved.' -ForegroundColor Green
if ((Read-Host 'Type DEMO to seed only six fixed-role accounts').Trim() -cne 'DEMO') { throw 'Cancelled; no accounts changed.' }

Call ($compose + @('build','api')) 'Build API containing role-aware seed'
$accounts = @(
  @('demo.admin','Admin','--seed-admin'),
  @('demo.reception','LeTan','--seed-reception'),
  @('demo.doctor','BacSi','--seed-demo-employee'),
  @('demo.nurse','DieuDuong','--seed-demo-employee'),
  @('demo.cashier','ThuNgan','--seed-demo-employee'),
  @('demo.warehouse','ThuKho','--seed-demo-employee')
)
foreach ($item in $accounts) { Seed-DemoAccount $compose $item[0] $item[1] $item[2] }
Check-DemoAccounts $compose
Check-CatalogSql (SqlInput $compose (CatalogQuery) 'Recheck curated Catalog after account seed')

$out = Join-Path $env:LOCALAPPDATA ("VAC-Demo/Submission/" + (Get-Date -Format yyyyMMdd-HHmmss) +
  '-' + [guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $out -Force | Out-Null
$report = Join-Path $out 'catalog-audit.json'
[IO.File]::WriteAllText($report,(Check-Audit $rawAudit),(New-Object Text.UTF8Encoding($false)))
if (-not $SkipBackup) {
  Call @('exec',$container,'sh','-c','mkdir -p /var/opt/mssql/backup') 'Prepare backup directory'
  $remoteBak = '/var/opt/mssql/backup/FinalDemo-' + [guid]::NewGuid().ToString('N') + '.bak'
  $backupSql = "BACKUP DATABASE [TiemChung] TO DISK = N'$remoteBak' WITH CHECKSUM;" +
    [Environment]::NewLine + 'GO' + [Environment]::NewLine +
    "RESTORE VERIFYONLY FROM DISK = N'$remoteBak' WITH CHECKSUM;"
  $null = SqlInput $compose $backupSql 'BACKUP and RESTORE VERIFYONLY'
  $bak = Join-Path $out 'FinalDemo.bak'
  Call @('cp',($container + ':' + $remoteBak),$bak) 'Copy verified backup'
  Check-Backup $bak
  Write-Host "Backup: $bak" -ForegroundColor Green
} else {
  Write-Host 'Backup SKIPPED; submission DB backup UNVERIFIED.' -ForegroundColor Yellow
}
Write-Host "Finished in $out" -ForegroundColor Green
Write-Host 'Manual: confirm six Windows logins/capabilities and Web Catalog; Customer Web account requires registration/email verification.'
Write-Host 'Disposable restore and final packaging still UNVERIFIED; do not submit infra/.env or private credentials.'
