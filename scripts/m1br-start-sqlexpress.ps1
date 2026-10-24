#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'
# User-approved run m1br_20261024_01. Start only; no config, restart, grants or stop.
$service = Get-Service -Name 'MSSQL$SQLEXPRESS'
if ($service.Status -ne 'Stopped') {
    throw 'Expected original state Stopped; inspect before changing service state.'
}
Start-Service -Name 'MSSQL$SQLEXPRESS'
$service.WaitForStatus('Running', [TimeSpan]::FromSeconds(30))
Get-Service -Name 'MSSQL$SQLEXPRESS' | Select-Object Name, Status
Write-Output 'Started only. No SQL objects or configuration changed. Do not stop shared work automatically.'
