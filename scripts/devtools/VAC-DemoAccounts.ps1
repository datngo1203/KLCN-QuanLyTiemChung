[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [ValidatePattern('^[a-z0-9][a-z0-9_-]*$')][string]$ComposeProject = $(if ($env:VAC_COMPOSE_PROJECT) { $env:VAC_COMPOSE_PROJECT } else { 'klcn-s0' })
)

$ErrorActionPreference = 'Stop'

function Find-Repository {
    if ($RepositoryRoot) {
        $candidates = @($RepositoryRoot)
    }
    else {
        $candidates = @()
        foreach ($base in @($PSScriptRoot, (Get-Location).Path)) {
            if (-not $base) { continue }
            $dir = [IO.DirectoryInfo]$base
            while ($dir) {
                $candidates += $dir.FullName
                $dir = $dir.Parent
            }
        }
    }

    foreach ($candidate in $candidates) {
        if ((Test-Path "$candidate/infra/compose.yaml") -and (Test-Path "$candidate/infra/.env")) {
            return (Resolve-Path $candidate).Path.TrimEnd('\')
        }
    }

    throw 'VAC repository with infra/.env was not found.'
}

function Role-Label([string]$Role) {
    switch ($Role) {
        'Admin' { 'Admin' }
        'LeTan' { 'Receptionist' }
        'BacSi' { 'Doctor' }
        'DieuDuong' { 'Nurse' }
        'ThuNgan' { 'Cashier' }
        'ThuKho' { 'Warehouse' }
        default { $Role }
    }
}

$root = Find-Repository
Set-Location $root

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker command was not found.'
}

$containerId = @(
    & docker ps --filter "label=com.docker.compose.project=$ComposeProject" --filter "label=com.docker.compose.service=sqlserver" --filter "status=running" --format '{{.ID}}' 2>$null
) | Select-Object -First 1

if ($LASTEXITCODE -ne 0 -or -not $containerId) {
    throw "Running SQL Server container for Compose project '$ComposeProject' was not found."
}

$query = @"
SET NOCOUNT ON;
SELECT
    e.ChucDanh,
    ISNULL(a.TenDangNhap, N''),
    e.TrangThai,
    ISNULL(a.TrangThai, N'')
FROM NhanVien e
LEFT JOIN TaiKhoan a
    ON a.MaNhanVien = e.MaNhanVien
    AND a.AccountType = N'Employee'
WHERE e.ChucDanh IN (N'Admin',N'LeTan',N'BacSi',N'DieuDuong',N'ThuNgan',N'ThuKho')
ORDER BY CASE e.ChucDanh
    WHEN N'Admin' THEN 1
    WHEN N'LeTan' THEN 2
    WHEN N'BacSi' THEN 3
    WHEN N'DieuDuong' THEN 4
    WHEN N'ThuNgan' THEN 5
    WHEN N'ThuKho' THEN 6
    ELSE 99 END, e.MaNhanVien;

SELECT COUNT(*)
FROM TaiKhoan
WHERE AccountType = N'Customer'
  AND TrangThai = N'Active'
  AND EmailVerifiedAt IS NOT NULL;
"@

$raw = $query | & docker exec -i $containerId sh -c '/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -d TiemChung -h -1 -W -s "|"'
if ($LASTEXITCODE -ne 0) {
    throw 'Could not read demo account readiness from SQL. No data was changed.'
}

$roles = @('Admin','LeTan','BacSi','DieuDuong','ThuNgan','ThuKho')
$rows = @()
$verifiedCustomerCount = $null

foreach ($line in @($raw)) {
    $text = [string]$line
    if ([string]::IsNullOrWhiteSpace($text)) { continue }

    if ($text -match '^\s*\d+\s*$' -and $text -notmatch '\|') {
        $verifiedCustomerCount = [int]$text.Trim()
        continue
    }

    $parts = $text -split '\|', 4
    if ($parts.Count -ne 4) { continue }

    $rows += [pscustomobject]@{
        Role = $parts[0].Trim()
        Username = $parts[1].Trim()
        EmployeeStatus = $parts[2].Trim()
        AccountStatus = $parts[3].Trim()
    }
}

Write-Host ''
Write-Host '=== VAC DEMO ACCOUNT READINESS ===' -ForegroundColor Cyan
Write-Host "Compose project: $ComposeProject"
Write-Host 'READ-ONLY: no account/password/data is created, reset, or deleted.'
Write-Host ''

foreach ($role in $roles) {
    $items = @($rows | Where-Object Role -eq $role)
    $active = @($items | Where-Object {
        $_.Username -and
        $_.EmployeeStatus -eq 'Active' -and
        $_.AccountStatus -eq 'Active'
    })

    Write-Host ("[{0}] {1}" -f $role, (Role-Label $role)) -ForegroundColor White

    if (-not $items.Count) {
        Write-Host '  MISSING: no employee exists for this role.' -ForegroundColor Red
        continue
    }

    foreach ($item in $items) {
        $username = if ($item.Username) { $item.Username } else { '(no account)' }
        $accountState = if ($item.AccountStatus) { $item.AccountStatus } else { '-' }
        $state = "Employee=$($item.EmployeeStatus); Account=$accountState"
        $marker = if (
            $item.Username -and
            $item.EmployeeStatus -eq 'Active' -and
            $item.AccountStatus -eq 'Active'
        ) { 'OK' } else { 'NOT_READY' }

        Write-Host ("  {0} | {1} | {2}" -f $marker, $username, $state)
    }

    if (-not $active.Count) {
        Write-Host '  => No Active account is ready for this role.' -ForegroundColor Yellow
    }
}

Write-Host ''
$customerText = if ($null -ne $verifiedCustomerCount) { $verifiedCustomerCount } else { 'unknown' }
Write-Host ("Verified Active Customer accounts: {0}" -f $customerText)
Write-Host ''
Write-Host 'Demo rule: reuse an existing OK username. Do not create another account unless a role is truly missing.'
Write-Host 'Passwords remain in your local VAC_TEST_ACCOUNTS_FILE outside Git. This script never reads credentials.'
