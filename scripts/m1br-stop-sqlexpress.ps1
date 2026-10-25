#Requires -RunAsAdministrator
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$manifestPath=Join-Path $root 'docs/m1br_20261024_01-manifest.json'
$manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
$mode=Get-Content (Join-Path $root 'docs/m1br_20261024_01-auth-mode-evidence.json') -Raw|ConvertFrom-Json
if($manifest.runId -ne 'm1br_20261024_01' -or $manifest.initialServiceState -ne 'Stopped' -or -not $manifest.cleanupPerformed -or -not $mode.RollbackSucceeded){throw 'Cleanup + confirmed LoginMode rollback required before restoring initial Stopped state.'}
$service=Get-Service -Name 'MSSQL$SQLEXPRESS'
if($service.Status -ne 'Running' -or @($service.DependentServices|Where-Object Status -eq 'Running').Count){throw 'Unexpected service/dependent state; do not stop.'}
$c=[System.Data.SqlClient.SqlConnection]::new('Server=.\SQLEXPRESS;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Pooling=False;Application Name=M1BRStopCheck')
try{
 $c.Open();$q=$c.CreateCommand();$q.CommandText=@'
SELECT CASE WHEN CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))=CONCAT(CONVERT(nvarchar(128),SERVERPROPERTY('MachineName')),'\SQLEXPRESS')
AND CONVERT(int,SERVERPROPERTY('IsIntegratedSecurityOnly'))=1
AND EXISTS(SELECT 1 FROM sys.server_principals WHERE sid=0x01 AND is_disabled=1)
AND NOT EXISTS(SELECT 1 FROM sys.databases WHERE database_id>4)
AND NOT EXISTS(SELECT 1 FROM sys.server_principals WHERE name IN('m1br_20261024_01_app','m1br_20261024_01_operator'))
AND NOT EXISTS(SELECT 1 FROM sys.dm_exec_sessions WHERE is_user_process=1 AND session_id<>@@SPID)
AND NOT EXISTS(SELECT 1 FROM msdb.dbo.sysjobactivity WHERE session_id=(SELECT MAX(session_id) FROM msdb.dbo.syssessions) AND start_execution_date IS NOT NULL AND stop_execution_date IS NULL)
THEN 1 ELSE 0 END
'@
 if($q.ExecuteScalar() -ne 1){throw 'Instance/mode/sa/DB/login/session/work check failed; no service stop.'}
}finally{$c.Dispose()}
# No Force/kill, no service permission or startup-mode change.
Stop-Service -Name 'MSSQL$SQLEXPRESS' -ErrorAction Stop
(Get-Service -Name 'MSSQL$SQLEXPRESS').WaitForStatus('Stopped',[TimeSpan]::FromSeconds(30))
$manifest.observedServiceState='Stopped'
$manifest|Add-Member -NotePropertyName initialServiceStateRestored -NotePropertyValue $true -Force
$manifest|ConvertTo-Json -Depth 12|Set-Content -LiteralPath $manifestPath
Write-Output 'MSSQL$SQLEXPRESS Stopped; initial service state restored after fresh workload checks.'
