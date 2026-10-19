param([switch]$IncludeMobile)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
dotnet test backend/TiemChung.slnx
if ($LASTEXITCODE) { throw 'Backend tests failed' }
npm.cmd --prefix apps/web run lint
if ($LASTEXITCODE) { throw 'Web lint failed' }
npm.cmd --prefix apps/web run build
if ($LASTEXITCODE) { throw 'Web build failed' }
if ($IsWindows -or $env:OS -eq 'Windows_NT') {
  dotnet build apps/windows/TiemChung.Desktop.csproj
  if ($LASTEXITCODE) { throw 'Windows build failed' }
}
if ($IncludeMobile) {
  Push-Location apps/mobile
  try {
    flutter analyze
    if ($LASTEXITCODE) { throw 'Flutter analyze failed' }
    flutter test
    if ($LASTEXITCODE) { throw 'Flutter tests failed' }
  } finally { Pop-Location }
} else { Write-Warning 'Chưa chạy kiểm tra mobile; dùng -IncludeMobile nếu có Flutter.' }
