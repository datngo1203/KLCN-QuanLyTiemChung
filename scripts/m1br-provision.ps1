$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$manifestPath=Join-Path $root 'docs/m1br_20261024_01-manifest.json'
$manifest=Get-Content $manifestPath -Raw | ConvertFrom-Json
$databases=@('KLCN_M1_Test_Boundary_m1br_20261024_01','KLCN_M1_Test_Legacy_m1br_20261024_01')
$logins=@('m1br_20261024_01_app','m1br_20261024_01_operator')
$secretDir=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'KLCN-M1-Test/m1br_20261024_01'
function Save-Manifest { $manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $manifestPath }
function Open-Deployment([string]$database='master') {
 $c=[System.Data.SqlClient.SqlConnection]::new("Server=.\SQLEXPRESS;Database=$database;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=5;Application Name=M1BRDeployment")
 $c.Open(); return $c
}
function Execute([System.Data.SqlClient.SqlConnection]$c,[string]$sql) {
 $cmd=$c.CreateCommand();$cmd.CommandTimeout=120;$cmd.CommandText=$sql;[void]$cmd.ExecuteNonQuery()
}
# Every run starts fresh; never overwrite an existing secret or object.
if(Test-Path -LiteralPath $secretDir){throw 'Run secret directory exists; inspect manifest, do not re-provision.'}
$c=Open-Deployment
try {
 $cmd=$c.CreateCommand();$cmd.CommandText=@'
SELECT CASE WHEN CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))=CONCAT(CONVERT(nvarchar(128),SERVERPROPERTY('MachineName')),'\SQLEXPRESS')
 AND CONVERT(int,SERVERPROPERTY('IsIntegratedSecurityOnly'))=0 AND IS_SRVROLEMEMBER('sysadmin')=1
 AND NOT EXISTS(SELECT 1 FROM sys.databases WHERE name IN ('KLCN_M1_Test_Boundary_m1br_20261024_01','KLCN_M1_Test_Legacy_m1br_20261024_01'))
 AND NOT EXISTS(SELECT 1 FROM sys.server_principals WHERE name IN ('m1br_20261024_01_app','m1br_20261024_01_operator'))
 AND EXISTS(SELECT 1 FROM sys.dm_exec_connections WHERE session_id=@@SPID AND encrypt_option='TRUE') THEN 1 ELSE 0 END
'@
 if($cmd.ExecuteScalar() -ne 1){throw 'Preflight failed; no provisioning.'}
 [void](New-Item -ItemType Directory -Path $secretDir)
 foreach($login in $logins){
  $password='M1!'+[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(36))
  ConvertTo-SecureString $password -AsPlainText -Force | Export-Clixml -LiteralPath (Join-Path $secretDir "$login.xml")
  # Generated base64 has no SQL quote. Never emit command text/secret or pass it to a child argv.
  try {Execute $c "CREATE LOGIN [$login] WITH PASSWORD=N'$password', CHECK_POLICY=ON, CHECK_EXPIRATION=OFF;"}
  catch {throw 'CREATE LOGIN failed; restricted secret remains DPAPI-protected. Inspect manifest; do not print SQL command.'}
  $password=$null
  $manifest.createdLogins=@($manifest.createdLogins)+$login;Save-Manifest
 }
 foreach($database in $databases){
  Execute $c "CREATE DATABASE [$database];"
  $manifest.createdDatabases=@($manifest.createdDatabases)+$database;Save-Manifest
 }
}finally{$c.Dispose()}
try {
 foreach($database in $databases){
  $env:ConnectionStrings__TiemChung="Server=.\SQLEXPRESS;Database=$database;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
  if($database -eq $databases[1]){
   & dotnet ef database update InitialCreate --project "$root/backend/src/TiemChung.Infrastructure" --startup-project "$root/backend/src/TiemChung.Api"
   if($LASTEXITCODE){throw 'Legacy initial migration failed.'}
   $c=Open-Deployment $database
   try{Execute $c @'
INSERT dbo.NhanVien(HoTen,CCCD,SoDienThoai,Email,BoPhan,ChucDanh,TrangThai) VALUES
(N'Synthetic Admin','000000000001','0900000000','admin@example.invalid','Test','Admin','Active'),
(N'Synthetic Legacy','000000000002','0900000001','legacy@example.invalid','Test','LeTan','Active'),
(N'Synthetic Unlinked','000000000003','0900000002','unlinked@example.invalid','Test','LeTan','Active');
INSERT dbo.TaiKhoan(MaNhanVien,TenDangNhap,MatKhauHash,TrangThai)
SELECT MaNhanVien,CASE WHEN ChucDanh='Admin' THEN 'old-admin' ELSE 'legacy-staff' END,'unknown-legacy-hash','Active' FROM dbo.NhanVien WHERE CCCD<>'000000000003';
'@}finally{$c.Dispose()}
  }
  & dotnet ef database update --project "$root/backend/src/TiemChung.Infrastructure" --startup-project "$root/backend/src/TiemChung.Api"
  if($LASTEXITCODE){throw 'M1 migration failed.'}
  $c=Open-Deployment $database
  try{
   if($database -eq $databases[0]){Execute $c @'
INSERT dbo.NhanVien(HoTen,CCCD,SoDienThoai,Email,BoPhan,ChucDanh,TrangThai) VALUES
(N'Synthetic Admin','000000000001','0900000000','admin@example.invalid','Test','Admin','Active'),
(N'Synthetic Unlinked','000000000003','0900000002','unlinked@example.invalid','Test','LeTan','Active');
'@}
   Execute $c ([IO.File]::ReadAllText((Join-Path $root 'database/m1-sql-boundary-up.sql')))
   Execute $c @'
CREATE USER [m1br_20261024_01_app] FOR LOGIN [m1br_20261024_01_app];
CREATE USER [m1br_20261024_01_operator] FOR LOGIN [m1br_20261024_01_operator];
ALTER ROLE m1_app ADD MEMBER [m1br_20261024_01_app];
ALTER ROLE m1_operator ADD MEMBER [m1br_20261024_01_operator];
DENY IMPERSONATE ON USER::m1br_20261024_01_app TO m1_operator;
DENY IMPERSONATE ON USER::m1br_20261024_01_operator TO m1_app;
CREATE TABLE dbo.FixtureOnly(Id int NOT NULL PRIMARY KEY CHECK(Id=1)); INSERT dbo.FixtureOnly VALUES(1);
GRANT SELECT ON dbo.FixtureOnly TO m1_app;
EXEC sys.sp_addextendedproperty @name=N'KLCN.TestRun',@value=N'm1br_20261024_01';
'@
   $manifest.createdUsersRolesGrantsGuardsFixtures=@($manifest.createdUsersRolesGrantsGuardsFixtures)+$database;Save-Manifest
  }finally{$c.Dispose()}
 }
 $manifest.blocker='Provisioned; acceptance and actual terminal CLI pending'
 $manifest.modeChangeExecuted=$true;$manifest.authenticationMode='Mixed';Save-Manifest
}finally{Remove-Item Env:ConnectionStrings__TiemChung -ErrorAction SilentlyContinue}
Write-Output 'Provisioned exact run DBs/logins; DPAPI secrets outside Git. No CLI acceptance claimed.'
