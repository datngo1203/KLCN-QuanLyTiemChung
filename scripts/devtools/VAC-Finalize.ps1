[CmdletBinding()]
param(
    [ValidatePattern('^[a-z0-9][a-z0-9_-]*$')]
    [string]$ComposeProject = ("vac-final-" + (Get-Date -Format 'yyyyMMdd-HHmm')),
    [switch]$SkipInventory,
    [switch]$SkipBackup
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding

function Find-Root {
    $dir = [IO.DirectoryInfo]$PSScriptRoot
    while ($dir) {
        if ((Test-Path (Join-Path $dir.FullName 'infra/compose.yaml')) -and
            (Test-Path (Join-Path $dir.FullName 'apps/windows/TiemChung.Desktop.csproj'))) {
            return $dir.FullName
        }
        $dir = $dir.Parent
    }
    throw 'Không tìm thấy repository VAC.'
}

function Run-Docker([string[]]$Arguments, [switch]$Capture) {
    if ($Capture) {
        $lines = @(& docker @Arguments 2>&1)
        if ($LASTEXITCODE -ne 0) { throw 'Docker command thất bại. Kiểm tra Docker Desktop, infra/.env và port đang dùng.' }
        return ($lines -join [Environment]::NewLine)
    }
    & docker @Arguments
    if ($LASTEXITCODE -ne 0) { throw 'Docker command thất bại. Kiểm tra Docker Desktop, infra/.env và port đang dùng.' }
}

function ComposeArgs([string[]]$Tail) {
    return @('compose','--env-file',"$Root/infra/.env",'-p',$ComposeProject,'-f',"$Root/infra/compose.yaml") + $Tail
}

function Confirm-Yes([string]$Prompt) {
    return ((Read-Host "$Prompt [y/N]").Trim().ToLowerInvariant() -eq 'y')
}

function Require-CleanMain {
    $branch = (& git -C $Root branch --show-current).Trim()
    if ($LASTEXITCODE -ne 0 -or $branch -ne 'main') {
        throw "Hãy chạy script từ branch main. Hiện tại: '$branch'."
    }
    $dirty = @(& git -C $Root status --porcelain)
    if ($LASTEXITCODE -ne 0) { throw 'Không đọc được git status.' }
    if ($dirty.Count -gt 0) {
        throw 'Worktree đang có thay đổi local. Commit/stash trước khi tạo bản final để tránh đóng gói nhầm.'
    }
}

function Port-InUse([int]$Port) {
    try { return $null -ne (Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue | Select-Object -First 1) }
    catch { return $false }
}

function Try-StopDefaultDemo {
    if (-not ((Port-InUse 14333) -or (Port-InUse 5080) -or (Port-InUse 5173))) { return }
    Write-Host ''
    Write-Host 'Phát hiện port demo đang được sử dụng.' -ForegroundColor Yellow
    if (-not (Confirm-Yes 'Thử dừng launcher mặc định klcn-s0? Script chỉ gọi VAC stop, không xóa volume/DB')) {
        throw 'Hãy tự đóng môi trường đang dùng các port 14333/5080/5173 rồi chạy lại.'
    }
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$Root/scripts/devtools/VAC.ps1" -Mode stop -ComposeProject klcn-s0
    Start-Sleep -Seconds 2
    if ((Port-InUse 14333) -or (Port-InUse 5080) -or (Port-InUse 5173)) {
        throw 'Vẫn còn port bị chiếm. Hãy tự đóng tiến trình/container ngoài launcher rồi chạy lại.'
    }
}

function Extract-Json([string]$Text) {
    $first = $Text.IndexOf('{')
    $last = $Text.LastIndexOf('}')
    if ($first -lt 0 -or $last -le $first) { throw 'Không tìm thấy JSON audit trong output.' }
    return $Text.Substring($first, $last - $first + 1)
}

function Backup-Database {
    $container = (& docker ps --filter "label=com.docker.compose.project=$ComposeProject" --filter 'label=com.docker.compose.service=sqlserver' --format '{{.ID}}' | Select-Object -First 1).Trim()
    if (-not $container) { throw 'Không tìm thấy SQL container của môi trường final.' }

    $outDir = Join-Path $Root 'dist/final-demo/Database'
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    $hostBak = Join-Path $outDir 'FinalDemo.bak'
    if (Test-Path $hostBak) { Remove-Item $hostBak -Force }

    & docker exec $container mkdir -p /var/opt/mssql/backup
    if ($LASTEXITCODE -ne 0) { throw 'Không tạo được thư mục backup trong SQL container.' }

    $backupSql = "BACKUP DATABASE [TiemChung] TO DISK = N'/var/opt/mssql/backup/FinalDemo.bak' WITH INIT, COPY_ONLY, CHECKSUM, STATS = 5;"
    $backupSql | & docker exec -i $container /bin/bash -lc '/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -b'
    if ($LASTEXITCODE -ne 0) { throw 'BACKUP DATABASE thất bại.' }

    $verifySql = "RESTORE VERIFYONLY FROM DISK = N'/var/opt/mssql/backup/FinalDemo.bak' WITH CHECKSUM;"
    $verifySql | & docker exec -i $container /bin/bash -lc '/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -b'
    if ($LASTEXITCODE -ne 0) { throw 'RESTORE VERIFYONLY thất bại; không dùng file backup này.' }

    $containerBak = $container + ':/var/opt/mssql/backup/FinalDemo.bak'
    & docker cp $containerBak $hostBak
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $hostBak)) { throw 'Không copy được FinalDemo.bak ra máy host.' }

    return $hostBak
}

$Root = Find-Root
Set-Location $Root

Write-Host '=== VAC FINAL DEMO BUILDER ===' -ForegroundColor Cyan
Write-Host "Repository : $Root"
Write-Host "Compose     : $ComposeProject"
Write-Host 'Mục tiêu: DB mới độc lập -> curated seed -> audit -> mở app -> bạn smoke -> backup .bak.'
Write-Host 'Script KHÔNG xóa/reset DB development cũ và KHÔNG in secret.'
Write-Host ''

if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw 'Thiếu git.' }
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { throw 'Thiếu Docker Desktop/CLI.' }
if (-not (Test-Path "$Root/infra/.env")) { throw 'Thiếu infra/.env local. Không tạo secret tự động.' }

Require-CleanMain
Try-StopDefaultDemo

$head = (& git -C $Root rev-parse HEAD).Trim()
Write-Host "Git HEAD: $head"

Write-Host ''
Write-Host '[1/6] Build + tạo SQL/API final mới...' -ForegroundColor Cyan
Run-Docker (ComposeArgs @('up','-d','--build','api'))

Write-Host ''
Write-Host '[2/6] Seed curated Catalog...' -ForegroundColor Cyan
Run-Docker (ComposeArgs @('run','--rm','api','--seed-catalog'))

if (-not $SkipInventory) {
    Write-Host ''
    if (Confirm-Yes 'Seed inventory demo cho Avaxim/Havax để có dữ liệu kho khi trình diễn?') {
        Write-Host '[3/6] Seed inventory demo...'
        Run-Docker (ComposeArgs @('run','--rm','api','--seed-inventory'))
    } else {
        Write-Host '[3/6] Bỏ qua inventory seed theo lựa chọn của bạn.'
    }
} else {
    Write-Host '[3/6] Bỏ qua inventory seed (-SkipInventory).'
}

Write-Host ''
Write-Host '[4/6] Audit Catalog final (read-only)...' -ForegroundColor Cyan
$auditRaw = Run-Docker (ComposeArgs @('run','--rm','api','--audit-catalog')) -Capture
$auditJson = Extract-Json $auditRaw
$audit = $auditJson | ConvertFrom-Json
$auditDir = Join-Path $Root 'dist/final-demo'
New-Item -ItemType Directory -Force -Path $auditDir | Out-Null
$auditPath = Join-Path $auditDir 'catalog-audit.json'
$auditJson | Set-Content -LiteralPath $auditPath -Encoding UTF8
Write-Host "Audit: candidateCount=$($audit.candidateCount), businessReferencedCount=$($audit.businessReferencedCount), graphOnlyCount=$($audit.graphOnlyCount)"
if ([int]$audit.candidateCount -ne 0) {
    Write-Host "Audit còn synthetic/placeholder. Xem: $auditPath" -ForegroundColor Red
    throw 'Không tiếp tục tạo final backup khi Catalog audit chưa sạch.'
}
Write-Host 'Catalog audit sạch.' -ForegroundColor Green

Write-Host ''
Write-Host '[5/6] Mở Web + Windows để bạn smoke thủ công...' -ForegroundColor Cyan
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$Root/scripts/devtools/VAC.ps1" -Mode start -ComposeProject $ComposeProject
if ($LASTEXITCODE -ne 0) { throw 'VAC launcher không mở được final demo.' }

Write-Host ''
Write-Host 'CHECK THỦ CÔNG:' -ForegroundColor Yellow
Write-Host '  - Web Catalog mở được, không có Vắc xin A/B hoặc gói synthetic.'
Write-Host '  - Windows Catalog mở được.'
Write-Host '  - Thấy vaccine curated như Gardasil 9, Qdenga, Prevenar 13, Hexaxim.'
Write-Host '  - Không có lỗi API/SQL rõ ràng.'
Write-Host ''
if (-not (Confirm-Yes 'Bạn đã kiểm Web + Windows và xác nhận PASS?')) {
    Write-Host 'Dừng trước backup. Môi trường final vẫn giữ nguyên để bạn kiểm/sửa.' -ForegroundColor Yellow
    exit 2
}

if ($SkipBackup) {
    Write-Host '[6/6] Bỏ qua backup (-SkipBackup).' -ForegroundColor Yellow
    exit 0
}

Write-Host ''
Write-Host '[6/6] Backup DB final + RESTORE VERIFYONLY...' -ForegroundColor Cyan
$bak = Backup-Database

$manifest = @"
VAC KLCN FINAL DEMO
GeneratedAt: $([DateTimeOffset]::Now.ToString('o'))
GitHead: $head
ComposeProject: $ComposeProject
CatalogAudit: PASS (candidateCount=0)
DatabaseBackup: dist/final-demo/Database/FinalDemo.bak
SecretsIncluded: NO
Notes:
- FinalDemo.bak was created with COPY_ONLY + CHECKSUM and RESTORE VERIFYONLY passed.
- infra/.env is local-only and must not be copied into the submission archive.
- Keep the exact source commit above together with this backup.
"@
$manifestPath = Join-Path $auditDir 'MANIFEST.txt'
$manifest | Set-Content -LiteralPath $manifestPath -Encoding UTF8

$restore = @"
# FinalDemo.bak

Database: TiemChung

This backup belongs to Git commit $head and Compose project $ComposeProject.

- Created with SQL Server BACKUP DATABASE using COPY_ONLY + CHECKSUM.
- RESTORE VERIFYONLY with CHECKSUM passed at packaging time.
- The submission package must not include infra/.env, passwords, Gmail App Passwords, payOS keys or tokens.
- Use the repository migrations/README for a reproducible clean setup when a direct restore is not desired.
"@
$restore | Set-Content -LiteralPath (Join-Path (Split-Path $bak -Parent) 'README-RESTORE.md') -Encoding UTF8

Write-Host ''
Write-Host 'FINAL DEMO READY' -ForegroundColor Green
Write-Host "Backup   : $bak"
Write-Host "Audit    : $auditPath"
Write-Host "Manifest : $manifestPath"
Write-Host "Project  : $ComposeProject"
Write-Host 'Giữ môi trường đang chạy nếu còn cần chụp hình/demo; khi xong dùng VAC.ps1 -Mode stop -ComposeProject <project>.'
