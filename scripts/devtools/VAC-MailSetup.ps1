[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$SenderAddress,
    [Security.SecureString]$AppPassword,
    [ValidateSet('Prompt','On','Off')][string]$VerificationRecipients = 'Prompt',
    [switch]$DisableVerificationRecipients,
    [switch]$Disable,
    [switch]$RemoveCredentials
)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding
if ($RemoveCredentials -and -not $Disable) { throw '-RemoveCredentials chỉ dùng cùng -Disable.' }
if ($Disable -and $DisableVerificationRecipients) { throw 'Chỉ chọn một chế độ disable trong mỗi lần chạy.' }

function Find-MailRepository {
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
    throw 'Không tìm thấy repository VAC có infra/.env. Thiết lập môi trường Development theo README trước.'
}

function Read-DotEnv([string]$Path) {
    $values = @{}
    foreach ($line in [IO.File]::ReadAllLines($Path, [Text.UTF8Encoding]::new($false))) {
        if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') {
            $values[$Matches[1]] = $Matches[2].Trim().Trim('"').Trim("'")
        }
    }
    return $values
}

function Set-DotEnv([string]$Path, [Collections.IDictionary]$Updates) {
    $result = [Collections.Generic.List[string]]::new()
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($line in [IO.File]::ReadAllLines($Path, [Text.UTF8Encoding]::new($false))) {
        if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=') {
            $key = $Matches[1]
            if ($Updates.Contains($key)) {
                if ($seen.Add($key)) { $result.Add("$key=$($Updates[$key])") }
                continue
            }
        }
        $result.Add($line)
    }
    foreach ($key in $Updates.Keys) {
        if ($seen.Add([string]$key)) { $result.Add("$key=$($Updates[$key])") }
    }
    [IO.File]::WriteAllLines($Path, $result, [Text.UTF8Encoding]::new($false))
}

function PlainText([Security.SecureString]$Value) {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
}

$root = Find-MailRepository
$envPath = Join-Path $root 'infra/.env'
$current = Read-DotEnv $envPath

if ($DisableVerificationRecipients) {
    Set-DotEnv $envPath ([ordered]@{ IDENTITY_ALLOW_REAL_DEVELOPMENT_VERIFICATION_RECIPIENTS = 'false' })
    Write-Host 'Đã tắt real Development verification recipient. Cấu hình SMTP reminder được giữ nguyên.'
    Write-Host 'Khởi động lại VAC để API nhận cấu hình mới; dữ liệu Customer đã xác minh được giữ nguyên.'
    return
}

if ($Disable) {
    $updates = [ordered]@{
        REMINDER_MAIL_MODE = 'Disabled'
        IDENTITY_ALLOW_REAL_DEVELOPMENT_VERIFICATION_RECIPIENTS = 'false'
    }
    if ($RemoveCredentials) {
        $updates.REMINDER_MAIL_USERNAME = ''
        $updates.REMINDER_MAIL_PASSWORD = ''
        $updates.REMINDER_MAIL_SENDER_ADDRESS = ''
    }
    Set-DotEnv $envPath $updates
    Write-Host "Đã tắt SMTP reminder và real-recipient verification trong infra/.env.$(if ($RemoveCredentials) { ' Đã xóa sender/App Password local.' })"
    Write-Host 'Khởi động lại VAC để API nhận cấu hình mới; dữ liệu Customer đã xác minh được giữ nguyên.'
    return
}

if (-not $SenderAddress) { $SenderAddress = Read-Host 'Gmail sender' }
$parsed = try { [Net.Mail.MailAddress]::new($SenderAddress) } catch { $null }
if (-not $parsed -or $parsed.Address -cne $SenderAddress) {
    throw 'Gmail sender không phải địa chỉ email hợp lệ.'
}
if (-not $AppPassword) { $AppPassword = Read-Host 'Gmail App Password (input được ẩn)' -AsSecureString }
$password = PlainText $AppPassword
try {
    $password = [regex]::Replace($password, '\s', '')
    if ($password -notmatch '^[A-Za-z0-9]{16}$') { throw 'Gmail App Password phải gồm đúng 16 ký tự chữ/số (có thể dán kèm khoảng trắng).' }

    if ($VerificationRecipients -eq 'Prompt') {
        $answer = Read-Host 'Bật real Development verification recipient để tạo Customer thật? [y/N]'
        $VerificationRecipients = if ($answer -match '^(?i:y|yes|c|co|có)$') { 'On' } else { 'Off' }
    }

    $mailDirectory = $current['IDENTITY_MAIL_DIRECTORY']
    if ([string]::IsNullOrWhiteSpace($mailDirectory)) {
        $mailDirectory = Join-Path $env:LOCALAPPDATA 'VAC-Demo/Mail'
    }
    if ($mailDirectory -notmatch '^[A-Za-z]:[\\/]') { throw 'IDENTITY_MAIL_DIRECTORY phải là đường dẫn local tuyệt đối.' }
    $mailDirectory = [IO.Path]::GetFullPath($mailDirectory)
    if ($mailDirectory.StartsWith("$root\", [StringComparison]::OrdinalIgnoreCase)) {
        throw 'IDENTITY_MAIL_DIRECTORY phải nằm ngoài repository để token không đi vào Git.'
    }
    $null = New-Item -ItemType Directory -Force -Path $mailDirectory

    Set-DotEnv $envPath ([ordered]@{
        IDENTITY_MAIL_DIRECTORY = $mailDirectory.Replace('\','/')
        REMINDER_MAIL_MODE = 'Smtp'
        REMINDER_MAIL_HOST = 'smtp.gmail.com'
        REMINDER_MAIL_PORT = '587'
        REMINDER_MAIL_USERNAME = $SenderAddress
        REMINDER_MAIL_PASSWORD = $password
        REMINDER_MAIL_SENDER_ADDRESS = $SenderAddress
        REMINDER_MAIL_SENDER_NAME = 'Trung tâm tiêm chủng VNVC - KLCN'
        IDENTITY_ALLOW_REAL_DEVELOPMENT_VERIFICATION_RECIPIENTS = $(if ($VerificationRecipients -eq 'On') { 'true' } else { 'false' })
    })
} finally {
    $password = $null
}

Write-Host 'Đã cấu hình Gmail SMTP local cho RF-05E và bảo đảm thư mục verification token tồn tại.'
Write-Host "Real Development verification recipient: $(if ($VerificationRecipients -eq 'On') { 'BẬT' } else { 'TẮT' })."
Write-Host 'Chạy VAC-MailCheck.ps1, rồi VAC-Start.cmd. Script không gửi email và không in App Password.'
