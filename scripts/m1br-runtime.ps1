# Dot-source only into a test/terminal process; never print the connection values.
param([ValidateSet('Boundary','Legacy')][string]$Database='Legacy')
$ErrorActionPreference='Stop'
$catalog=if($Database -eq 'Boundary'){'KLCN_M1_Test_Boundary_m1br_20261024_01'}else{'KLCN_M1_Test_Legacy_m1br_20261024_01'}
$secretDir=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'KLCN-M1-Test/m1br_20261024_01'
function Get-RunConnection([string]$suffix){
 $login="m1br_20261024_01_$suffix"
 $secure=Import-Clixml -LiteralPath (Join-Path $secretDir "$login.xml")
 $password=[Net.NetworkCredential]::new('', $secure).Password
 $builder=[System.Data.SqlClient.SqlConnectionStringBuilder]::new()
 $builder['Data Source']='.\SQLEXPRESS';$builder['Initial Catalog']=$catalog;$builder['User ID']=$login;$builder['Password']=$password
 $builder['Encrypt']=$true;$builder['TrustServerCertificate']=$true;$builder['Persist Security Info']=$false
 $password=$null
 return $builder.ConnectionString
}
$env:M1_BOUNDARY_APP_CONNECTION=Get-RunConnection 'app'
$env:M1_BOUNDARY_OPERATOR_CONNECTION=Get-RunConnection 'operator'
$env:M1_OPERATOR_CONNECTION=$env:M1_BOUNDARY_OPERATOR_CONNECTION
# Do not export deployment Integrated credentials to the child API/operator.
