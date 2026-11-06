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

WITH Roles(RoleCode, RoleLabel, SortOrder) AS (
    SELECT N'Admin', N'Admin', 1 UNION ALL
    SELECT N'LeTan', N'Receptionist', 2 UNION ALL
    SELECT N'BacSi', N'Doctor', 3 UNION ALL
    SELECT N'DieuDuong', N'Nurse', 4 UNION ALL
    SELECT N'ThuNgan', N'Cashier', 5 UNION ALL
    SELECT N'ThuKho', N'Warehouse', 6
)
SELECT
    N'[' + r.RoleCode + N'] ' + r.RoleLabel + N' | ' +
    CASE
        WHEN x.MaNhanVien IS NULL THEN N'MISSING_EMPLOYEE'
        WHEN x.TenDangNhap IS NULL THEN N'NO_ACCOUNT'
        WHEN x.EmployeeStatus = N'Active' AND x.AccountStatus = N'Active'
            THEN N'OK | ' + x.TenDangNhap + N' | Employee=Active; Account=Active'
        ELSE N'NOT_READY | ' + ISNULL(x.TenDangNhap, N'(no account)') +
             N' | Employee=' + ISNULL(x.EmployeeStatus, N'-') +
             N'; Account=' + ISNULL(x.AccountStatus, N'-')
    END
FROM Roles r
OUTER APPLY (
    SELECT TOP (1)
        e.MaNhanVien,
        a.TenDangNhap,
        e.TrangThai AS EmployeeStatus,
        a.TrangThai AS AccountStatus
    FROM NhanVien e
    LEFT JOIN TaiKhoan a
        ON a.MaNhanVien = e.MaNhanVien
        AND a.AccountType = N'Employee'
    WHERE e.ChucDanh = r.RoleCode
    ORDER BY
        CASE WHEN e.TrangThai = N'Active' AND a.TrangThai = N'Active' THEN 0 ELSE 1 END,
        CASE WHEN a.TenDangNhap IS NULL THEN 1 ELSE 0 END,
        e.MaNhanVien
) x
ORDER BY r.SortOrder;

SELECT
    N'Verified Active Customer accounts: ' +
    CONVERT(nvarchar(20), COUNT(*))
FROM TaiKhoan
WHERE AccountType = N'Customer'
  AND TrangThai = N'Active'
  AND EmailVerifiedAt IS NOT NULL;
"@

Write-Host ''
Write-Host '=== VAC DEMO ACCOUNT READINESS ===' -ForegroundColor Cyan
Write-Host "Compose project: $ComposeProject"
Write-Host 'READ-ONLY: no account/password/data is created, reset, or deleted.'
Write-Host ''

$query | & docker exec -i $containerId sh -c '/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -d TiemChung -h -1 -W'

if ($LASTEXITCODE -ne 0) {
    throw 'Could not read demo account readiness from SQL. No data was changed.'
}

Write-Host ''
Write-Host 'Demo rule: reuse an existing OK username. Do not create another account unless a role is truly missing.'
Write-Host 'This script never reads or writes passwords.'
