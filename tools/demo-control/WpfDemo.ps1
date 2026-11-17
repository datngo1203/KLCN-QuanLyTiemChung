param([int]$OwnerPid, [switch]$Rehearse, [switch]$EscapeTest, [string]$Scene, [int]$RegistrationId)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
try {
  $spec = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot '01_DATA_DEMO.json') | ConvertFrom-Json
  $roles = @{ profiles='reception'; payment='cashier'; appointment='reception'; clinical='doctor'; nurse='nurse'; warehouse='warehouse'; catalog='admin'; reports='admin'; accounts='admin' }
  $user = $spec.auth.employeeOnlyForWpfReadOnly
  if ($Scene) {
    if (-not $roles.ContainsKey($Scene)) { throw 'Unknown WPF scene' }
    $user = $spec.auth.employeeUsernames.($roles[$Scene])
  }
  $pattern = '^\|\s*[^|]*\|\s*\x60' + [regex]::Escape($user) + '\x60\s*\|\s*\x60([^\x60]+)\x60'
  $accountFile = $spec.auth.credentialsSourceRelativePath
  if (-not [IO.Path]::IsPathRooted($accountFile)) {
    $accountFile = Join-Path $spec.runtime.repository $accountFile
  }
  $matchesFound = @(Get-Content -Encoding UTF8 -LiteralPath $accountFile |
    ForEach-Object { if ($_ -match $pattern) { $Matches[1] } })
  if ($matchesFound.Count -ne 1) { throw 'Không tìm được tài khoản Lễ tân' }
  Add-Type -Path (Join-Path $PSScriptRoot 'WpfDemo.cs') -ReferencedAssemblies @(
    'System.Core','System.Drawing','System.Windows.Forms','WindowsBase','UIAutomationClient','UIAutomationTypes')
  exit [DemoWpf]::Run($OwnerPid, (Join-Path $spec.runtime.repository 'apps\windows\'), $user, $matchesFound[0],
    $spec.personas.customer.fullName, $spec.personas.recipient.fullName, $spec.interaction.typeIntervalMs.min,
    $spec.interaction.typeIntervalMs.max, $Rehearse.IsPresent, $EscapeTest.IsPresent, $Scene, $RegistrationId, $spec.wpf.lotNumber, $spec.auth.employeeUsernames.reception)
} catch {
  [Console]::WriteLine('BLOCKED WPF — kiểm tra cấu hình, thông tin tài khoản và cửa sổ WPF')
  exit 1
}
