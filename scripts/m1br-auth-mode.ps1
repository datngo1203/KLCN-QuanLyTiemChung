#Requires -RunAsAdministrator
[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Apply','Rollback')][string]$Action)
$ErrorActionPreference = 'Stop'
# Authorization: local SQLEXPRESS / m1br_20261024_01 only. No credential arguments.
$serviceName = 'MSSQL$SQLEXPRESS'
$recordPath = Join-Path $PSScriptRoot '../docs/m1br_20261024_01-auth-mode-evidence.json'
$mapping = Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL'
$instanceId = $mapping.SQLEXPRESS
if ($instanceId -notmatch '^MSSQL\d+\.SQLEXPRESS$') { throw 'Unexpected SQLEXPRESS registry mapping; stop.' }
$registryPath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$instanceId\MSSQLServer"
$service = Get-Service -Name $serviceName
if ($service.Status -ne 'Running') { throw 'Expected Running service. No mode change.' }
if (@($service.DependentServices | Where-Object Status -eq 'Running').Count) { throw 'Running dependent service; stop for review.' }

function Read-State {
    $connection = [System.Data.SqlClient.SqlConnection]::new('Server=.\SQLEXPRESS;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=5;Application Name=M1BRAuthMode')
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = @'
SET NOCOUNT ON;
SELECT CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')) AS ServerName,
 CONVERT(nvarchar(128),SERVERPROPERTY('InstanceName')) AS InstanceName,
 CONVERT(int,SERVERPROPERTY('IsIntegratedSecurityOnly')) AS IntegratedOnly,
 IS_SRVROLEMEMBER('sysadmin') AS DeploymentSysadmin,
 (SELECT COUNT(*) FROM sys.databases WHERE database_id>4) AS UserDatabaseCount,
 (SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process=1 AND session_id<>@@SPID) AS OtherUserSessionCount,
 (SELECT name FROM sys.server_principals WHERE sid=0x01) AS SaName,
 CONVERT(int,(SELECT is_disabled FROM sys.server_principals WHERE sid=0x01)) AS SaDisabled,
 (SELECT encrypt_option FROM sys.dm_exec_connections WHERE session_id=@@SPID) AS Encryption,
 (SELECT COUNT(*) FROM sys.server_principals WHERE name IN ('m1br_20261024_01_app','m1br_20261024_01_operator')) AS RunLoginCount
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
'@
        return ($command.ExecuteScalar() | ConvertFrom-Json)
    } finally { $connection.Dispose() }
}

$before = Read-State
$expectedServer = "$env:COMPUTERNAME\SQLEXPRESS"
if ($before.ServerName -ne $expectedServer -or $before.InstanceName -ne 'SQLEXPRESS') { throw 'SQL server identity mismatch.' }
if ($before.DeploymentSysadmin -ne 1 -or $before.Encryption -ne 'TRUE') { throw 'Required existing admin/encrypted connection missing.' }
if ($before.UserDatabaseCount -ne 0 -or $before.OtherUserSessionCount -ne 0) { throw 'User databases/sessions present; no mode change or restart. Ask for review.' }
$registryBefore = [int](Get-ItemPropertyValue -LiteralPath $registryPath -Name LoginMode)
if ($Action -eq 'Apply') {
    if (Test-Path -LiteralPath $recordPath) { throw 'Evidence already exists; do not overwrite/reapply. Review recorded state.' }
    if ($registryBefore -ne 1 -or $before.IntegratedOnly -ne 1 -or $before.SaDisabled -ne 1 -or $before.RunLoginCount -ne 0) { throw 'Unexpected baseline; no changes.' }
    $record = [ordered]@{
        RunId='m1br_20261024_01'; Instance='.\SQLEXPRESS'; RegistryPath=$registryPath
        OriginalLoginMode=$registryBefore; OriginalServiceState='Running'; Before=$before
        CertificateValidation='Bypassed only for this authorized local test; encryption required'
        ApplyStartedUtc=[DateTime]::UtcNow.ToString('o'); ApplySucceeded=$false
        RollbackSucceeded=$false
    }
    $targetMode = 2
} else {
    if (-not (Test-Path -LiteralPath $recordPath)) { throw 'Original evidence missing; no rollback.' }
    $saved = Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json
    $record = [ordered]@{}
    foreach ($property in $saved.PSObject.Properties) { $record[$property.Name] = $property.Value }
    if ($record.RunId -ne 'm1br_20261024_01' -or $record.RegistryPath -ne $registryPath -or $record.OriginalLoginMode -ne 1) { throw 'Baseline mismatch.' }
    if ($registryBefore -notin @(1,2) -or $before.IntegratedOnly -notin @(0,1) -or $before.RunLoginCount -ne 0) { throw 'Unexpected mode or run logins remain; finish/review cleanup first.' }
    if ($before.SaName -ne $record.Before.SaName -or $before.SaDisabled -ne $record.Before.SaDisabled) { throw 'sa state changed; stop for review.' }
    $record.RollbackBefore = $before
    $record.RollbackStartedUtc = [DateTime]::UtcNow.ToString('o')
    $targetMode = 1
}
# Persist baseline before the only configuration write. Contains no passwords/tokens.
$record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $recordPath -Encoding utf8
try {
    Set-ItemProperty -LiteralPath $registryPath -Name LoginMode -Type DWord -Value $targetMode
    # No Force: never stop dependent services or change service permissions.
    Restart-Service -Name $serviceName -ErrorAction Stop
    (Get-Service -Name $serviceName).WaitForStatus('Running', [TimeSpan]::FromSeconds(30))
    $after = Read-State
    if ($after.ServerName -ne $expectedServer -or $after.IntegratedOnly -ne (2-$targetMode) -or $after.Encryption -ne 'TRUE' -or $after.DeploymentSysadmin -ne 1 -or $after.SaName -ne $before.SaName -or $after.SaDisabled -ne $before.SaDisabled) { throw 'Post-restart assertions failed. Stop; review evidence, no further changes.' }
    if ($Action -eq 'Apply') { $record.ApplyAfter=$after; $record.ApplySucceeded=$true }
    else { $record.RollbackAfter=$after; $record.RollbackSucceeded=$true }
    $record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $recordPath -Encoding utf8
    Write-Output "$Action succeeded: LoginMode=$targetMode; Windows admin login works; encryption TRUE; sa unchanged."
    Write-Output "Evidence: $recordPath"
} catch {
    $record.LastFailure = $_.Exception.Message
    $record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $recordPath -Encoding utf8
    throw 'Mode/restart/postcheck failed. Stop and share non-secret evidence; do not change accounts, grants or other configuration. See runbook recovery.'
}
