# Development-only final-demo password loader. Secrets must remain outside Git.
function Get-FinalDemoAccountPasswords([string]$file) {
  if ([string]::IsNullOrWhiteSpace($file) -or -not [IO.Path]::IsPathRooted($file)) {
    throw 'Demo credentials require an absolute local file path outside Git.'
  }
  if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
    throw "Missing local demo credentials file: $file. Create it outside the repository before running this script."
  }
  try {
    $config = ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($file)) -ErrorAction Stop
  } catch {
    throw 'Invalid local demo credentials JSON.'
  }
  if ($null -eq $config -or $config -isnot [pscustomobject]) {
    throw 'Local demo credentials must be a JSON object.'
  }
  $expected = @('demo.admin','demo.reception','demo.doctor','demo.nurse','demo.cashier','demo.warehouse','demo.customer@example.invalid')
  $actual = @($config.PSObject.Properties | ForEach-Object { $_.Name })
  if ($actual.Count -ne $expected.Count) {
    throw 'Local demo credentials must contain exactly seven fixed demo usernames.'
  }
  $passwords = @{}
  $distinct = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
  foreach ($username in $expected) {
    if ($actual -cnotcontains $username) { throw "Missing fixed username: $username" }
    $property = $config.PSObject.Properties[$username]
    if ($property.Value -isnot [string] -or $property.Value.Length -lt 12 -or $property.Value.Length -gt 128) {
      throw "Password for $username must have 12-128 characters."
    }
    if (-not $distinct.Add($property.Value)) { throw 'Demo account passwords must be unique per role.' }
    $passwords[$username] = $property.Value
  }
  return $passwords
}
