[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [ValidatePattern('^[a-z0-9][a-z0-9_-]*$')][string]$ComposeProject = $(if ($env:VAC_COMPOSE_PROJECT) { $env:VAC_COMPOSE_PROJECT } else { 'klcn-s0' })
)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding

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
    throw 'Không tìm thấy repository VAC có infra/.env.'
}

function Read-DotEnv([string]$Path) {
    $values = @{}
    foreach ($line in [IO.File]::ReadAllLines($Path, [Text.UTF8Encoding]::new($false))) {
        if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') { $values[$Matches[1]] = $Matches[2].Trim().Trim('"').Trim("'") }
    }
    return $values
}

function Mask-Email([string]$Value) {
    if ($Value -notmatch '^(.)([^@]*)@(.)(.*)$') { return '(chưa cấu hình)' }
    return "$($Matches[1])***@$($Matches[3])***"
}

function Docker-Lines([string[]]$Arguments) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $lines = @(& docker @Arguments 2>&1)
        return @{ Code = $LASTEXITCODE; Lines = $lines }
    } finally { $ErrorActionPreference = $previous }
}

$root = Find-MailRepository
$values = Read-DotEnv (Join-Path $root 'infra/.env')
$mode = if ($values['REMINDER_MAIL_MODE']) { $values['REMINDER_MAIL_MODE'] } else { 'Disabled' }
$flag = $values['IDENTITY_ALLOW_REAL_DEVELOPMENT_VERIFICATION_RECIPIENTS'] -eq 'true'
$directory = $values['IDENTITY_MAIL_DIRECTORY']
Write-Host "SMTP mode: $mode"
Write-Host "Host: $(if ($values['REMINDER_MAIL_HOST']) { $values['REMINDER_MAIL_HOST'] } else { '(chưa cấu hình)' })"
Write-Host "Port: $(if ($values['REMINDER_MAIL_PORT']) { $values['REMINDER_MAIL_PORT'] } else { '(chưa cấu hình)' })"
Write-Host "Sender: $(Mask-Email $values['REMINDER_MAIL_SENDER_ADDRESS'])"
Write-Host "Real Development verification recipient: $(if ($flag) { 'BẬT' } else { 'TẮT' })"
Write-Host "Local verification directory: $(if ($directory -and (Test-Path -LiteralPath $directory -PathType Container)) { 'TỒN TẠI' } else { 'THIẾU' })"

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host 'API container: CHƯA KIỂM (không tìm thấy Docker).'
    return
}
$compose = @('compose','--env-file',"$root/infra/.env",'-p',$ComposeProject,'-f',"$root/infra/compose.yaml")
$container = Docker-Lines ($compose + @('ps','-q','api'))
$containerId = $container.Lines | Where-Object { $_ -is [string] } | Select-Object -First 1
if ($container.Code -ne 0 -or -not $containerId) {
    Write-Host 'API container: CHƯA CHẠY hoặc Docker chưa sẵn sàng.'
    return
}
$inspection = Docker-Lines @('inspect','--format','{{range .Config.Env}}{{println .}}{{end}}',$containerId)
if ($inspection.Code -ne 0) {
    Write-Host 'API container: KHÔNG ĐỌC ĐƯỢC cấu hình.'
    return
}
$containerEnvironment = $inspection.Lines
$expected = [ordered]@{
    ReminderMail__Mode = $mode
    ReminderMail__Host = $values['REMINDER_MAIL_HOST']
    ReminderMail__Port = $values['REMINDER_MAIL_PORT']
    ReminderMail__Username = $values['REMINDER_MAIL_USERNAME']
    ReminderMail__Password = $values['REMINDER_MAIL_PASSWORD']
    ReminderMail__SenderAddress = $values['REMINDER_MAIL_SENDER_ADDRESS']
    ReminderMail__SenderName = $values['REMINDER_MAIL_SENDER_NAME']
    Identity__AllowRealDevelopmentVerificationRecipients = $(if ($flag) { 'true' } else { 'false' })
}
$actual = @{}
foreach ($line in $containerEnvironment) {
    if ($line -match '^([^=]+)=(.*)$' -and $expected.Contains($Matches[1])) { $actual[$Matches[1]] = $Matches[2] }
}
$matches = @($expected.Keys | Where-Object { -not $actual.ContainsKey($_) -or $actual[$_] -cne [string]$expected[$_] }).Count -eq 0
Write-Host "API container nhận cấu hình mail: $(if ($matches) { 'ĐÚNG' } else { 'CHƯA KHỚP — chạy lại VAC-Start' })"
