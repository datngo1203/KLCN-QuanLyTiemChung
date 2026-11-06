[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [ValidatePattern('^[a-z0-9][a-z0-9_-]*$')][string]$ComposeProject = $(if ($env:VAC_COMPOSE_PROJECT) { $env:VAC_COMPOSE_PROJECT } else { 'klcn-s0' })
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding

function Find-Repository {
    if ($RepositoryRoot) { $candidates = @($RepositoryRoot) }
    else {
        $candidates = @()
        foreach ($base in @($PSScriptRoot, (Get-Location).Path)) {
            if (-not $base) { continue }
            $dir = [IO.DirectoryInfo]$base
            while ($dir) { $candidates += $dir.FullName; $dir = $dir.Parent }
        }
    }
    foreach ($candidate in $candidates) {
        if ((Test-Path "$candidate/infra/compose.yaml") -and (Test-Path "$candidate/infra/.env")) {
            return (Resolve-Path $candidate).Path.TrimEnd('\')
        }
    }
    throw 'Không tìm thấy repository VAC có infra/.env.'
}

function Role-Label([string]$Role) {
    switch ($Role) {
        'Admin' { 'Quản trị viên' }
        'LeTan' { 'Lễ tân' }
        'BacSi' { 'Bác sĩ' }
        'DieuDuong' { 'Điều dưỡng' }
        'ThuNgan' { 'Thu ngân' }
        'ThuKho' { 'Thủ kho' }
        default { $Role }
    }
}

$root = Find-Repository
Set-Location $root

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Không tìm thấy Docker.'
}

$compose = @('compose','--env-file',"$root/infra/.env",'-p',$ComposeProject,'-f',"$root/infra/compose.yaml")
$containerId = (& docker @compose ps -q sqlserver 2>$null | Select-Object -First 1)
if ($LASTEXITCODE -ne 0 -or -not $containerId) {
    throw "Không tìm thấy SQL Server của Compose project '$ComposeProject'. Chạy VAC-Start trước."
}

$query = @"
SET NOCOUNT ON;
SELECT
    e.ChucDanh,
    REPLACE(REPLACE(e.HoTen, CHAR(13), ' '), CHAR(10), ' '),
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

$raw = $query | & docker @compose exec -T sqlserver sh -c '/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -d TiemChung -h -1 -W -s "|"'
if ($LASTEXITCODE -ne 0) {
    throw 'Không đọc được account readiness từ SQL. Không có dữ liệu nào bị thay đổi.'
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

    $parts = $text -split '\|', 5
    if ($parts.Count -ne 5) { continue }

    $rows += [pscustomobject]@{
        Role = $parts[0].Trim()
        FullName = $parts[1].Trim()
        Username = $parts[2].Trim()
        EmployeeStatus = $parts[3].Trim()
        AccountStatus = $parts[4].Trim()
    }
}

Write-Host ''
Write-Host '=== VAC DEMO ACCOUNT READINESS ===' -ForegroundColor Cyan
Write-Host "Compose project: $ComposeProject"
Write-Host 'Chỉ đọc DB; script không tạo/reset tài khoản và không đọc mật khẩu.'
Write-Host ''

foreach ($role in $roles) {
    $items = @($rows | Where-Object Role -eq $role)
    $active = @($items | Where-Object { $_.Username -and $_.EmployeeStatus -eq 'Active' -and $_.AccountStatus -eq 'Active' })

    Write-Host ("[{0}] {1}" -f $role, (Role-Label $role)) -ForegroundColor White

    if (-not $items.Count) {
        Write-Host '  THIẾU: chưa có nhân viên cho role này.' -ForegroundColor Red
        continue
    }

    foreach ($item in $items) {
        $username = if ($item.Username) { $item.Username } else { '(chưa có tài khoản)' }
        $state = "NV=$($item.EmployeeStatus); TK=$(if ($item.AccountStatus) { $item.AccountStatus } else { '-' })"
        $marker = if ($item.Username -and $item.EmployeeStatus -eq 'Active' -and $item.AccountStatus -eq 'Active') { 'OK' } else { 'CHƯA SẴN SÀNG' }
        Write-Host ("  {0} | {1} | {2} | {3}" -f $marker,$username,$item.FullName,$state)
    }

    if (-not $active.Count) {
        Write-Host '  => Không có account Active sẵn để demo role này.' -ForegroundColor Yellow
    }
}

Write-Host ''
Write-Host ("Customer Active + email đã xác minh: {0}" -f $(if ($null -ne $verifiedCustomerCount) { $verifiedCustomerCount } else { 'không xác định' }))
Write-Host ''
Write-Host 'Khuyến nghị demo: reuse đúng username đang OK ở trên. Không tạo thêm account nếu không cần.'
Write-Host 'Mật khẩu tiếp tục giữ trong sổ local VAC_TEST_ACCOUNTS_FILE ngoài Git; script này không đọc hoặc ghi credential.'
