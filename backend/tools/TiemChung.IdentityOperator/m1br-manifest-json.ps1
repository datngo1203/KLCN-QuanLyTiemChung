param([Parameter(Mandatory)][System.Collections.IDictionary]$Manifest)
$ErrorActionPreference='Stop'
$result=[ordered]@{}
foreach($key in $Manifest.Keys){$result[$key]=$Manifest[$key]}
# Windows PowerShell 5.1 otherwise emits legacy /Date(milliseconds)/ strings.
# Emit explicit ISO strings understood by System.Text.Json without changing validity times.
foreach($field in @('IssuedAt','ExpiresAt')){
 $result[$field]=([DateTimeOffset]$result[$field]).ToString('o',[Globalization.CultureInfo]::InvariantCulture)
}
$result|ConvertTo-Json
