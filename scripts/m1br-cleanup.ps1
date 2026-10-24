# Execute only after acceptance evidence has been reviewed and saved.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$manifestPath=Join-Path $root 'docs/m1br_20261024_01-manifest.json'
$manifest=Get-Content $manifestPath -Raw|ConvertFrom-Json
if(-not $manifest.acceptanceComplete){throw 'Acceptance gate remains open; do not destroy pending CLI fixtures.'}
$evidence=Get-Content (Join-Path $root 'docs/m1br_20261024_01-sql-evidence.json') -Raw|ConvertFrom-Json
$dbs=@('KLCN_M1_Test_Boundary_m1br_20261024_01','KLCN_M1_Test_Legacy_m1br_20261024_01')
$logins=@('m1br_20261024_01_app','m1br_20261024_01_operator')
if($manifest.runId -ne 'm1br_20261024_01' -or @($manifest.createdDatabases).Count -ne 2 -or @($manifest.createdLogins).Count -ne 2){throw 'Manifest mismatch.'}
$c=[System.Data.SqlClient.SqlConnection]::new('Server=.\SQLEXPRESS;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Application Name=M1BRCleanup')
try{
 $c.Open();$q=$c.CreateCommand();$q.CommandText="SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE session_id<>@@SPID AND is_user_process=1 AND (database_id IN(DB_ID('KLCN_M1_Test_Boundary_m1br_20261024_01'),DB_ID('KLCN_M1_Test_Legacy_m1br_20261024_01')) OR login_name IN('m1br_20261024_01_app','m1br_20261024_01_operator'))"
 if($q.ExecuteScalar() -ne 0){throw 'Run sessions still open; no disconnect/kill.'}
 # Validate every target before any DROP. Never use SINGLE_USER/ROLLBACK IMMEDIATE.
 foreach($db in $dbs){
  $expected=@($evidence.Databases|Where-Object name -eq $db)
  if($expected.Count -ne 1 -or $db -notin $manifest.createdDatabases){throw 'DB not recorded as created by run.'}
  $q.CommandText="SELECT CONVERT(varchar(172),owner_sid,1) FROM sys.databases WHERE name=@name AND database_id=@id AND create_date=@created"
  $q.Parameters.Clear();[void]$q.Parameters.AddWithValue('@name',$db);[void]$q.Parameters.AddWithValue('@id',$expected[0].database_id);[void]$q.Parameters.AddWithValue('@created',[DateTime]$expected[0].create_date)
  if($q.ExecuteScalar() -ne $expected[0].OwnerSid){throw 'DB identity/owner changed.'}
  $q.Parameters.Clear();$q.CommandText="SELECT CONVERT(nvarchar(64),value) FROM [$db].sys.extended_properties WHERE class=0 AND name='KLCN.TestRun'"
  if($q.ExecuteScalar() -ne 'm1br_20261024_01'){throw 'DB marker changed.'}
 }
 foreach($login in $logins){
  $expected=@($evidence.Logins|Where-Object name -eq $login)
  if($expected.Count -ne 1 -or $login -notin $manifest.createdLogins){throw 'Login not recorded as created by run.'}
  $q.CommandText="SELECT CONVERT(varchar(172),sid,1) FROM sys.server_principals WHERE name=@name AND create_date=@created"
  $q.Parameters.Clear();[void]$q.Parameters.AddWithValue('@name',$login);[void]$q.Parameters.AddWithValue('@created',[DateTime]$expected[0].create_date)
  if($q.ExecuteScalar() -ne $expected[0].Sid){throw 'Login identity changed.'}
 }
 $q.Parameters.Clear()
 foreach($db in $dbs){$q.CommandText="DROP DATABASE [$db]";[void]$q.ExecuteNonQuery();$manifest|Add-Member -NotePropertyName cleanedDatabases -NotePropertyValue @() -ErrorAction SilentlyContinue;$manifest.cleanedDatabases=@($manifest.cleanedDatabases)+$db;$manifest|ConvertTo-Json -Depth 12|Set-Content $manifestPath}
 foreach($login in $logins){$q.CommandText="DROP LOGIN [$login]";[void]$q.ExecuteNonQuery();$manifest|Add-Member -NotePropertyName cleanedLogins -NotePropertyValue @() -ErrorAction SilentlyContinue;$manifest.cleanedLogins=@($manifest.cleanedLogins)+$login;$manifest|ConvertTo-Json -Depth 12|Set-Content $manifestPath}
 $q.CommandText="SELECT (SELECT COUNT(*) FROM sys.databases WHERE name IN('KLCN_M1_Test_Boundary_m1br_20261024_01','KLCN_M1_Test_Legacy_m1br_20261024_01'))+(SELECT COUNT(*) FROM sys.server_principals WHERE name IN('m1br_20261024_01_app','m1br_20261024_01_operator'))"
 if($q.ExecuteScalar() -ne 0){throw 'Cleanup postcheck failed.'}
 $manifest.cleanupPerformed=$true;$manifest.cleanupReason='Exact recorded DB/login identities checked, dropped without forced disconnect; names absent after cleanup';$manifest|ConvertTo-Json -Depth 12|Set-Content $manifestPath
}finally{$c.Dispose()}
Write-Output 'SQL run objects cleaned. Keep evidence; mode rollback remains an elevated, separately checked step.'
