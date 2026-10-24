[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Bootstrap','Recover','Reset')][string]$Command)
$ErrorActionPreference='Stop'
if([Console]::IsInputRedirected){throw 'A real interactive terminal is required; do not pipe secrets or redirect stdin.'}
$root=Split-Path $PSScriptRoot -Parent
$database=if($Command -eq 'Bootstrap'){'Boundary'}else{'Legacy'}
. (Join-Path $PSScriptRoot 'm1br-runtime.ps1') -Database $database
$dll=Join-Path $root 'artifacts/m1br-operator/TiemChung.IdentityOperator.dll'
if(-not (Test-Path -LiteralPath $dll)){throw 'Publish operator first.'}
if($Command -eq 'Recover'){
 $secretDir=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'KLCN-M1-Test/m1br_20261024_01'
 $env:M1_RECOVERY_MANIFEST=Join-Path $secretDir 'recovery.json'
 if(-not (Test-Path -LiteralPath $env:M1_RECOVERY_MANIFEST)){
  $c=[System.Data.SqlClient.SqlConnection]::new('Server=.\SQLEXPRESS;Database=KLCN_M1_Test_Legacy_m1br_20261024_01;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Application Name=M1BRManifest')
  try{
   $c.Open();$q=$c.CreateCommand();$q.CommandText="SELECT RecoveryDatabaseId,M1UpgradeEpoch,MaTaiKhoan,MaNhanVien,RowVersion FROM dbo.IdentityRecoveryDatabase CROSS JOIN dbo.TaiKhoan WHERE TenDangNhap='old-admin'";
   $reader=$q.ExecuteReader();if(-not $reader.Read()){throw 'Legacy target missing'}
   $manifest=[ordered]@{OperationId=[Guid]::NewGuid();RecoveryDatabaseId=$reader.GetGuid(0);M1UpgradeEpoch=$reader.GetString(1);MigrationId='20261023073816_M1Identity';TargetAccountId=$reader.GetInt32(2);EmployeeId=$reader.GetInt32(3);ExpectedRowVersion=[Convert]::ToBase64String($reader.GetValue(4));IssuedAt=[DateTimeOffset]::UtcNow;ExpiresAt=[DateTimeOffset]::UtcNow.AddMinutes(25);AssignedDuty='Technical'}
   $reader.Dispose();& (Join-Path $PSScriptRoot 'm1br-manifest-json.ps1') -Manifest $manifest | Set-Content -LiteralPath $env:M1_RECOVERY_MANIFEST -Encoding UTF8
  }finally{$c.Dispose()}
 }
}
if($Command -eq 'Bootstrap'){Write-Output 'Clean DB: employee ID 1; choose username test-admin. Enter a synthetic password at the hidden CLI prompt.';$operation='bootstrap-admin'}
elseif($Command -eq 'Recover'){Write-Output 'Legacy DB: recover old-admin. Enter a new synthetic password at the hidden CLI prompt; keep it for Reset actor authentication.';$operation='recover-upgraded-admin'}
else{
 $c=[System.Data.SqlClient.SqlConnection]::new('Server=.\SQLEXPRESS;Database=KLCN_M1_Test_Legacy_m1br_20261024_01;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Application Name=M1BRTarget')
 try{$c.Open();$q=$c.CreateCommand();$q.CommandText="SELECT MaTaiKhoan,MaNhanVien,RowVersion FROM dbo.TaiKhoan WHERE TenDangNhap='legacy-staff'";$r=$q.ExecuteReader();if(-not $r.Read()){throw 'Legacy staff missing'};Write-Output ('Target account ID: '+$r.GetInt32(0)+'; employee ID: '+$r.GetInt32(1)+'; RowVersion: '+[Convert]::ToBase64String($r.GetValue(2)));$r.Dispose()}finally{$c.Dispose()}
 Write-Output 'Actor old-admin: use the new recovery password, not any legacy credential. Employee is synthetic; use a different synthetic password at both hidden target prompts.';$operation='reset-employee-password'
}
$hash=(Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
try{
 & dotnet $dll $operation
 $exitCode=$LASTEXITCODE
 $record=[ordered]@{RunId='m1br_20261024_01';Command=$operation;Database=$database;BinarySHA256=$hash;ExitCode=$exitCode;AtUtc=[DateTime]::UtcNow.ToString('o');Interactive=$true}
 $record|ConvertTo-Json -Compress|Add-Content -LiteralPath (Join-Path $root 'docs/m1br_20261024_01-cli-evidence.jsonl')
 if($exitCode){throw 'CLI reported failure; inspect non-secret evidence. No success claimed.'}
}finally{
 Remove-Item Env:M1_OPERATOR_CONNECTION,Env:M1_BOUNDARY_APP_CONNECTION,Env:M1_BOUNDARY_OPERATOR_CONNECTION,Env:M1_RECOVERY_MANIFEST -ErrorAction SilentlyContinue
}
