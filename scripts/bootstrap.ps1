param([switch]$IncludeMobile)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
foreach ($tool in @('dotnet', 'node', 'npm.cmd')) {
  if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "Thiếu $tool; xem docs/setup.md" }
}
dotnet tool restore
if ($LASTEXITCODE) { throw 'dotnet tool restore failed' }
dotnet restore backend/TiemChung.slnx
if ($LASTEXITCODE) { throw 'Backend restore failed' }
if ($IsWindows -or $env:OS -eq 'Windows_NT') {
  dotnet restore apps/windows/TiemChung.Desktop.csproj
  if ($LASTEXITCODE) { throw 'Windows restore failed' }
}
npm.cmd --prefix apps/web ci
if ($LASTEXITCODE) { throw 'Web restore failed' }
if ($IncludeMobile) {
  if (-not (Get-Command flutter -ErrorAction SilentlyContinue)) { throw 'Thiếu Flutter; xem docs/setup.md.' }
  Push-Location apps/mobile
  try { flutter pub get; if ($LASTEXITCODE) { throw 'Flutter restore failed' } }
  finally { Pop-Location }
} else { Write-Host 'Mobile đóng băng; dùng -IncludeMobile nếu cần restore Flutter.' }
Write-Host 'Hoàn tất dependencies đã có công cụ. Xem README để chạy từng ứng dụng.'
