$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
dotnet test backend/TiemChung.slnx
if ($LASTEXITCODE) { throw 'Backend tests failed' }
npm.cmd --prefix apps/web run lint
if ($LASTEXITCODE) { throw 'Web lint failed' }
node --test apps/web/src/api/*.test.js
if ($LASTEXITCODE) { throw 'Web tests failed' }
npm.cmd --prefix apps/web run build
if ($LASTEXITCODE) { throw 'Web build failed' }
if ($IsWindows -or $env:OS -eq 'Windows_NT') {
  dotnet build apps/windows/TiemChung.Desktop.csproj
  if ($LASTEXITCODE) { throw 'Windows build failed' }
}
