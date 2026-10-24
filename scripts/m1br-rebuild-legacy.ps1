[CmdletBinding()]
param([ValidateSet('Check','Apply','Rollback')][string]$Action='Check')
# Apply/Rollback require the user's separate approval of this exact fixture replacement.
# This is deployment-only test tooling, never an Identity recovery bypass or runtime grant.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$database='KLCN_M1_Test_Legacy_m1br_20261024_01'
$recordPath=Join-Path $root 'docs/m1br_20261024_01-legacy-rebuild.json'
$inventory=Get-Content (Join-Path $root 'docs/m1br_20261024_01-sql-evidence.json') -Raw|ConvertFrom-Json
$original=@($inventory.Databases|Where-Object name -eq $database)
if($original.Count -ne 1){throw 'Original run inventory required.'}
$c=[System.Data.SqlClient.SqlConnection]::new('Server=.\SQLEXPRESS;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Application Name=M1BRLegacyRebuild')
function Scalar([string]$sql){$q=$c.CreateCommand();$q.CommandText=$sql;return $q.ExecuteScalar()}
function Execute([string]$sql){$q=$c.CreateCommand();$q.CommandTimeout=180;$q.CommandText=$sql;[void]$q.ExecuteNonQuery()}
function Assert-Quiet {
 if((Scalar "SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process=1 AND session_id<>@@SPID") -ne 0){throw 'Other user sessions present; no kill/forced disconnect.'}
}
function Snapshot {
 $sql=@'
SELECT d.name,d.database_id,d.create_date,CONVERT(varchar(172),d.owner_sid,1) AS OwnerSid,
 CONVERT(nvarchar(64),(SELECT value FROM KLCN_M1_Test_Legacy_m1br_20261024_01.sys.extended_properties WHERE class=0 AND name='KLCN.TestRun')) AS TestRun,
 (SELECT RecoveryDatabaseId FROM KLCN_M1_Test_Legacy_m1br_20261024_01.dbo.IdentityRecoveryDatabase WHERE Id=1) AS RecoveryDatabaseId,
 (SELECT COUNT(*) FROM KLCN_M1_Test_Legacy_m1br_20261024_01.dbo.IdentityRecoveryOperation) AS LedgerCount,
 (SELECT COUNT(*) FROM KLCN_M1_Test_Legacy_m1br_20261024_01.dbo.SecurityAudit WHERE Action='employeePasswordReset') AS ResetAuditCount
FROM sys.databases d WHERE d.name='KLCN_M1_Test_Legacy_m1br_20261024_01'
FOR JSON PATH,WITHOUT_ARRAY_WRAPPER
'@
 return ((Scalar $sql)|ConvertFrom-Json)
}
function File-Command([string]$sql,[string]$path){
 $q=$c.CreateCommand();$q.CommandTimeout=180;$q.CommandText=$sql;[void]$q.Parameters.AddWithValue('@path',$path);[void]$q.ExecuteNonQuery()
}
function Save-Record { $record|ConvertTo-Json -Depth 12|Set-Content -LiteralPath $recordPath }
function Archive-LocalManifest {
 $directory=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'KLCN-M1-Test/m1br_20261024_01'
 $source=Join-Path $directory 'recovery.json'
 if(Test-Path -LiteralPath $source){
  $target=Join-Path $directory ('recovery.'+[Guid]::NewGuid().ToString('N')+'.json')
  if(-not ([IO.Path]::GetFullPath($target)).StartsWith(([IO.Path]::GetFullPath($directory)+[IO.Path]::DirectorySeparatorChar),[StringComparison]::OrdinalIgnoreCase)){throw 'Archive path escaped run directory.'}
  Move-Item -LiteralPath $source -Destination $target
 }
}
try{
 $c.Open()
 if((Scalar "SELECT CASE WHEN CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))=CONCAT(CONVERT(nvarchar(128),SERVERPROPERTY('MachineName')),'\SQLEXPRESS') AND IS_SRVROLEMEMBER('sysadmin')=1 AND CONVERT(int,SERVERPROPERTY('IsIntegratedSecurityOnly'))=0 THEN 1 ELSE 0 END") -ne 1){throw 'Wrong instance/mode/deployment principal.'}
 Assert-Quiet
 if((Scalar "SELECT COUNT(*) FROM sys.databases WHERE database_id>4 AND name NOT IN ('KLCN_M1_Test_Boundary_m1br_20261024_01','KLCN_M1_Test_Legacy_m1br_20261024_01')") -ne 0){throw 'Unrelated user DB found; stop for review.'}
 $state=Snapshot
 if($state.TestRun -ne 'm1br_20261024_01' -or $state.OwnerSid -ne $original[0].OwnerSid){throw 'DB ownership/run marker mismatch.'}
 $backupDirectory=[string](Scalar "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))")
 if([string]::IsNullOrWhiteSpace($backupDirectory)){throw 'Existing SQL backup directory unavailable; do not create/change paths or ACL.'}
 $backup=Join-Path $backupDirectory 'KLCN_M1_Test_Legacy_m1br_20261024_01_before_g2.bak'
 if($Action -ne 'Rollback'){
  $q=$c.CreateCommand();$q.CommandText='EXEC master.dbo.xp_fileexist @path';[void]$q.Parameters.AddWithValue('@path',$backup);$reader=$q.ExecuteReader();$exists=$reader.Read() -and [Convert]::ToInt32($reader.GetValue(0)) -ne 0;$reader.Dispose()
  if($exists){throw 'Backup target exists; do not overwrite or append.'}
 }
 if($Action -eq 'Check'){
  if($state.database_id -ne $original[0].database_id -or [DateTime]$state.create_date -ne [DateTime]$original[0].create_date){throw 'Original DB identity changed.'}
  Write-Output 'CHECK ONLY: exact legacy DB identity/owner/run marker verified; no other user sessions/DBs.'
  Write-Output ('Proposed COPY_ONLY CHECKSUM backup: '+$backup)
  Write-Output 'No backup, DROP, CREATE, RESTORE, migration or manifest move executed.'
  return
 }
 if($Action -eq 'Apply'){
  if(Test-Path -LiteralPath $recordPath){throw 'Rebuild record already exists; no repeat/overwrite.'}
  if($state.database_id -ne $original[0].database_id -or [DateTime]$state.create_date -ne [DateTime]$original[0].create_date -or $state.LedgerCount -ne 1 -or $state.ResetAuditCount -ne 0){throw 'Expected original recovered fixture changed.'}
  $record=[ordered]@{RunId='m1br_20261024_01';Generation=2;Before=$state;BackupPath=$backup;BackupVerified=$false;Applied=$false;RolledBack=$false;StartedUtc=[DateTime]::UtcNow.ToString('o')}
  Save-Record
  File-Command "BACKUP DATABASE [$database] TO DISK=@path WITH COPY_ONLY,CHECKSUM,NOINIT,NAME=N'm1br_20261024_01_before_g2';" $backup
  File-Command 'RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM;' $backup
  $record.BackupVerified=$true;Save-Record
  Assert-Quiet
  Execute "DROP DATABASE [$database]; CREATE DATABASE [$database];"
  $record.Recreated=$true;Save-Record
  $env:ConnectionStrings__TiemChung="Server=.\SQLEXPRESS;Database=$database;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
  & dotnet ef database update InitialCreate --project "$root/backend/src/TiemChung.Infrastructure" --startup-project "$root/backend/src/TiemChung.Api"
  if($LASTEXITCODE){throw 'Initial migration failed; keep backup and use reviewed rollback.'}
  Execute @'
USE [KLCN_M1_Test_Legacy_m1br_20261024_01];
INSERT dbo.NhanVien(HoTen,CCCD,SoDienThoai,Email,BoPhan,ChucDanh,TrangThai) VALUES
(N'Synthetic Admin','000000000001','0900000000','admin@example.invalid','Test','Admin','Active'),
(N'Synthetic Legacy','000000000002','0900000001','legacy@example.invalid','Test','LeTan','Active'),
(N'Synthetic Unlinked','000000000003','0900000002','unlinked@example.invalid','Test','LeTan','Active');
INSERT dbo.TaiKhoan(MaNhanVien,TenDangNhap,MatKhauHash,TrangThai)
SELECT MaNhanVien,CASE WHEN ChucDanh='Admin' THEN 'old-admin' ELSE 'legacy-staff' END,'unknown-legacy-hash','Active' FROM dbo.NhanVien WHERE CCCD<>'000000000003';
USE master;
'@
  & dotnet ef database update --project "$root/backend/src/TiemChung.Infrastructure" --startup-project "$root/backend/src/TiemChung.Api"
  if($LASTEXITCODE){throw 'M1 migration failed; keep backup and use reviewed rollback.'}
  Execute ("USE [$database];`n"+[IO.File]::ReadAllText((Join-Path $root 'database/m1-sql-boundary-up.sql'))+"`nUSE master;")
  Execute @'
USE [KLCN_M1_Test_Legacy_m1br_20261024_01];
CREATE USER [m1br_20261024_01_app] FOR LOGIN [m1br_20261024_01_app];
CREATE USER [m1br_20261024_01_operator] FOR LOGIN [m1br_20261024_01_operator];
ALTER ROLE m1_app ADD MEMBER [m1br_20261024_01_app]; ALTER ROLE m1_operator ADD MEMBER [m1br_20261024_01_operator];
DENY IMPERSONATE ON USER::m1br_20261024_01_app TO m1_operator;
DENY IMPERSONATE ON USER::m1br_20261024_01_operator TO m1_app;
CREATE TABLE dbo.FixtureOnly(Id int NOT NULL PRIMARY KEY CHECK(Id=1));INSERT dbo.FixtureOnly VALUES(1);GRANT SELECT ON dbo.FixtureOnly TO m1_app;
EXEC sys.sp_addextendedproperty @name=N'KLCN.TestRun',@value=N'm1br_20261024_01';
EXEC sys.sp_addextendedproperty @name=N'KLCN.TestGeneration',@value=N'2';
USE master;
'@
  $record.After=Snapshot
  if($record.After.RecoveryDatabaseId -eq $record.Before.RecoveryDatabaseId -or $record.After.LedgerCount -ne 0){throw 'New fixture identity assertion failed.'}
  Archive-LocalManifest
  $record.Applied=$true;Save-Record
  Write-Output 'Legacy generation2 fixture prepared; not recovery/reset acceptance. Original DB retained in verified backup.'
 }else{
  $saved=Get-Content -LiteralPath $recordPath -Raw|ConvertFrom-Json
  $record=[ordered]@{};foreach($property in $saved.PSObject.Properties){$record[$property.Name]=$property.Value}
  if($record.RunId -ne 'm1br_20261024_01' -or -not $record.BackupVerified -or $record.BackupPath -ne $backup -or -not $record.Applied){throw 'Verified completed rebuild record required; partial failure needs separate review.'}
  if($state.RecoveryDatabaseId.ToString() -ne $record.After.RecoveryDatabaseId.ToString() -or $state.database_id -ne $record.After.database_id -or [DateTime]$state.create_date -ne [DateTime]$record.After.create_date){throw 'Replacement DB identity changed.'}
  $record.BeforeRollback=$state;Save-Record
  Assert-Quiet
  File-Command 'RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM;' $backup
  File-Command "RESTORE DATABASE [$database] FROM DISK=@path WITH REPLACE,CHECKSUM;" $backup
  Archive-LocalManifest
  $record.AfterRollback=Snapshot
  if($record.AfterRollback.RecoveryDatabaseId.ToString() -ne $record.Before.RecoveryDatabaseId.ToString()){throw 'Restore identity assertion failed.'}
  $record.RolledBack=$true;Save-Record
  Write-Output 'Original legacy DB restored, including its original consumed recovery ledger. Password still unknown; no quota bypass.'
 }
}finally{Remove-Item Env:ConnectionStrings__TiemChung -ErrorAction SilentlyContinue;$c.Dispose()}
