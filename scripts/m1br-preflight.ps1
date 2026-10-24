$ErrorActionPreference = 'Stop'
if ((Get-Service -Name 'MSSQL$SQLEXPRESS').Status -ne 'Running') {
    throw 'SQLEXPRESS is not running. No SQL connection or provisioning attempted.'
}
$sqlcmd = Get-Command sqlcmd -ErrorAction Stop
# Existing Windows credential; -N encryption + certificate validation, deliberately no -C.
# No connection password, secrets, config changes or SQL writes.
& $sqlcmd.Source -S '.\SQLEXPRESS' -d master -E -N -l 5 -b -i (Join-Path $PSScriptRoot 'm1br-preflight.sql')
if ($LASTEXITCODE -ne 0) { throw 'Read-only preflight blocked. Do not provision or retry with relaxed TLS/configuration.' }
