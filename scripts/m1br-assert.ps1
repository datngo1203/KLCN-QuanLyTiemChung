$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$output=[ordered]@{RunId='m1br_20261024_01';RecordedUtc=[DateTime]::UtcNow.ToString('o');CertificateValidation='Local test exception only';Databases=@();Principals=@()}
foreach($name in @('Boundary','Legacy')){
 . (Join-Path $PSScriptRoot 'm1br-runtime.ps1') -Database $name
 $catalog=if($name -eq 'Boundary'){'KLCN_M1_Test_Boundary_m1br_20261024_01'}else{'KLCN_M1_Test_Legacy_m1br_20261024_01'}
 foreach($key in @('M1_BOUNDARY_APP_CONNECTION','M1_BOUNDARY_OPERATOR_CONNECTION')){
  $c=[System.Data.SqlClient.SqlConnection]::new([Environment]::GetEnvironmentVariable($key))
  try{
   $c.Open();$q=$c.CreateCommand();$q.CommandText=@'
SELECT DB_NAME() AS DatabaseName,ORIGINAL_LOGIN() AS SqlLogin,USER_NAME() AS DbUser,
 IS_SRVROLEMEMBER('sysadmin') AS Sysadmin,IS_ROLEMEMBER('db_owner') AS DbOwner,
 IS_ROLEMEMBER('m1_app') AS AppRole,IS_ROLEMEMBER('m1_operator') AS OperatorRole,
 HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','CONTROL') AS DbControl,
 HAS_PERMS_BY_NAME('dbo','SCHEMA','ALTER') AS SchemaAlter,
 HAS_PERMS_BY_NAME('dbo','USER','IMPERSONATE') AS ImpersonateDbo,
 HAS_PERMS_BY_NAME('dbo.SecurityAudit','OBJECT','SELECT') AS AuditRead,
 (SELECT permission_name FROM sys.fn_my_permissions(NULL,'SERVER') FOR JSON PATH) AS ServerPermissions,
 (SELECT permission_name FROM sys.fn_my_permissions(NULL,'DATABASE') FOR JSON PATH) AS DatabasePermissions
FOR JSON PATH,WITHOUT_ARRAY_WRAPPER;
'@
   $state=$q.ExecuteScalar()|ConvertFrom-Json
   if($state.Sysadmin -ne 0 -or $state.DbOwner -ne 0 -or $state.DbControl -ne 0 -or $state.SchemaAlter -ne 0 -or $state.ImpersonateDbo -ne 0 -or $state.AuditRead -ne 0){throw 'Unexpected runtime effective privileges.'}
   $output.Principals+= $state
  }finally{$c.Dispose()}
 }
 $c=[System.Data.SqlClient.SqlConnection]::new("Server=.\SQLEXPRESS;Database=$catalog;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Application Name=M1BRAssert")
 try{
  $c.Open();$q=$c.CreateCommand();$q.CommandText=@'
SELECT d.name,d.database_id,d.create_date,CONVERT(varchar(172),d.owner_sid,1) AS OwnerSid,
 CONVERT(nvarchar(64),(SELECT value FROM sys.extended_properties WHERE class=0 AND name='KLCN.TestRun')) AS TestRun,
 (SELECT name,is_disabled,execute_as_principal_id FROM sys.triggers t JOIN sys.sql_modules m ON t.object_id=m.object_id WHERE t.name IN ('M1GuardAccount','M1GuardAudit') FOR JSON PATH,INCLUDE_NULL_VALUES) AS Guards,
 (SELECT r.name AS RoleName,u.name AS MemberName FROM sys.database_role_members rm JOIN sys.database_principals r ON rm.role_principal_id=r.principal_id JOIN sys.database_principals u ON rm.member_principal_id=u.principal_id FOR JSON PATH) AS Memberships,
 (SELECT p.name AS PrincipalName,x.state_desc,x.permission_name,x.class_desc,OBJECT_NAME(x.major_id) AS ObjectName,x.minor_id FROM sys.database_permissions x JOIN sys.database_principals p ON x.grantee_principal_id=p.principal_id WHERE p.name IN ('m1_app','m1_operator','m1br_20261024_01_app','m1br_20261024_01_operator') FOR JSON PATH) AS Grants,
 (SELECT COUNT(*) FROM dbo.SecurityAudit) AS AuditCount,
 (SELECT COUNT(*) FROM dbo.IdentityRecoveryOperation) AS LedgerCount,
 (SELECT COUNT(*) FROM dbo.IdentityMailOutbox WHERE SentAt IS NOT NULL) AS SentOutboxCount,
 (SELECT COUNT(*) FROM dbo.EmailVerification WHERE UsedAt IS NOT NULL) AS UsedVerificationCount,
 (SELECT COUNT(*) FROM dbo.SecurityAudit WHERE TraceId='synthetic-append-only') AS RolledBackAppendCount,
 (SELECT Action,Outcome,OperatorIdentity FROM dbo.SecurityAudit WHERE OperatorIdentity IS NOT NULL FOR JSON PATH) AS OperatorAudit
FROM sys.databases d WHERE d.database_id=DB_ID() FOR JSON PATH,WITHOUT_ARRAY_WRAPPER;
'@
  # FOR JSON may stream multiple rows/chunks; concatenate before parsing.
  $reader=$q.ExecuteReader();$json=[Text.StringBuilder]::new()
  while($reader.Read()){[void]$json.Append($reader.GetString(0))};$reader.Dispose()
  $state=$json.ToString()|ConvertFrom-Json
  if($state.TestRun -ne 'm1br_20261024_01'){throw 'DB test marker mismatch.'}
  if($state.RolledBackAppendCount -ne 0){throw 'Audit rollback assertion failed.'}
  $output.Databases+=$state
 }finally{$c.Dispose()}
}
$c=[System.Data.SqlClient.SqlConnection]::new('Server=.\SQLEXPRESS;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Application Name=M1BRAssert')
try{
 $c.Open();$q=$c.CreateCommand();$q.CommandText="SELECT name,CONVERT(varchar(172),sid,1) AS Sid,create_date,is_disabled FROM sys.server_principals WHERE name IN ('m1br_20261024_01_app','m1br_20261024_01_operator') FOR JSON PATH";
 $output.Logins=@($q.ExecuteScalar()|ConvertFrom-Json)
 $q.CommandText="SELECT CONVERT(int,SERVERPROPERTY('IsIntegratedSecurityOnly')) AS IntegratedOnly,(SELECT CONVERT(int,is_disabled) FROM sys.server_principals WHERE sid=0x01) AS SaDisabled FOR JSON PATH,WITHOUT_ARRAY_WRAPPER";$output.Instance=$q.ExecuteScalar()|ConvertFrom-Json
}finally{$c.Dispose()}
$output|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $root 'docs/m1br_20261024_01-sql-evidence.json')
Remove-Item Env:M1_OPERATOR_CONNECTION,Env:M1_BOUNDARY_APP_CONNECTION,Env:M1_BOUNDARY_OPERATOR_CONNECTION -ErrorAction SilentlyContinue
Write-Output 'Read-only actual-login assertions recorded; no secret/password/token output.'
