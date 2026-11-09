$ErrorActionPreference = 'Stop'
# Load only function definitions. Never execute the launcher's main block.
$tokens=$null; $parseErrors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile("$PSScriptRoot/VAC.ps1",[ref]$tokens,[ref]$parseErrors)
if ($parseErrors.Count) { throw 'PowerShell parse failed' }
$finalTokens=$null; $finalParseErrors=$null
$null=[Management.Automation.Language.Parser]::ParseFile("$PSScriptRoot/VAC-Finalize.ps1",[ref]$finalTokens,[ref]$finalParseErrors)
if ($finalParseErrors.Count) { throw 'VAC-Finalize PowerShell parse failed' }
foreach ($f in $ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst]},$false)) {
    . ([scriptblock]::Create($f.Extent.Text))
}
$passed=0
$ComposeProject='klcn-s0'
function Assert($value,$label) { if (-not $value) { throw "FAIL: $label" }; $script:passed++; Write-Host "PASS: $label" }
function Assert-Throws($body,$pattern,$label) {
    $caught=$false
    try { & $body } catch { $caught=$_.Exception.Message -match $pattern }
    Assert $caught $label
}
$fixture=Join-Path $env:TEMP ('VAC Launcher Test '+[guid]::NewGuid().ToString('N'))
$originalLocation=(Get-Location).Path
$own=$null; $other=$null; $listener=$null; $testProcesses=@(); $mailFixture=$null
try {
    Set-Location $PSScriptRoot
    $realRoot=Find-Repository
    Assert (Test-Path "$realRoot/infra/compose.yaml") 'repository discovered from scripts/devtools without machine configuration'
    $composeSource=Get-Content "$realRoot/infra/compose.yaml" -Raw
    foreach ($mapping in @(
        'Identity__AllowRealDevelopmentVerificationRecipients: ${IDENTITY_ALLOW_REAL_DEVELOPMENT_VERIFICATION_RECIPIENTS:-false}',
        'ReminderMail__Mode: ${REMINDER_MAIL_MODE:-Disabled}',
        'ReminderMail__Host: ${REMINDER_MAIL_HOST:-}',
        'ReminderMail__Port: ${REMINDER_MAIL_PORT:-}',
        'ReminderMail__Username: ${REMINDER_MAIL_USERNAME:-}',
        'ReminderMail__Password: ${REMINDER_MAIL_PASSWORD:-}',
        'ReminderMail__SenderAddress: ${REMINDER_MAIL_SENDER_ADDRESS:-}',
        'ReminderMail__SenderName: ${REMINDER_MAIL_SENDER_NAME:-}'
    )) { Assert ($composeSource.Contains($mapping)) "Compose mapping: $($mapping.Split(':')[0])" }
    Assert (-not ((Get-Content "$realRoot/scripts/devtools/VAC.ps1" -Raw).Contains('--no-recreate'))) 'Start lets Compose apply changed mail configuration'
    Assert ((State-Key $realRoot 'klcn-s0') -ne (State-Key $realRoot 'vac-test')) 'Compose projects have separate ownership state'
    Assert ((State-Key $realRoot 'klcn-s0') -eq (State-Key ($realRoot.ToUpperInvariant()) 'klcn-s0')) 'repository casing does not change ownership state'
    $null=New-Item -ItemType Directory -Force "$fixture/infra","$fixture/apps/windows"
    Set-Content "$fixture/infra/compose.yaml" ''
    Set-Content "$fixture/apps/windows/TiemChung.Desktop.csproj" ''
    $RepositoryRoot=$fixture
    Assert ((Find-Repository) -eq $fixture) 'repository path containing spaces'
    $script:Root=$fixture
    Assert-Throws { Start-Demo } 'Chưa có infra/.env' 'missing .env fails before Docker mutation'
    Assert-Throws { Require-Tool '__vac_nonexistent_tool__' 'Thiếu công cụ thử nghiệm' } '^Thiếu' 'missing prerequisite has Vietnamese error'
    $script:StatePath=Join-Path $fixture 'state.json'
    $script:State=[pscustomobject]@{Web=@();Windows=@();Containers=@()}
    $shell=(Get-Process -Id $PID).Path
    $own=Start-Process $shell -ArgumentList '-NoProfile -Command Start-Sleep -Seconds 90' -WindowStyle Hidden -PassThru
    $testProcesses += Process-Identity $own
    $other=Start-Process $shell -ArgumentList '-NoProfile -Command Start-Sleep -Seconds 90' -WindowStyle Hidden -PassThru
    $testProcesses += Process-Identity $other
    $identity=Process-Identity $own
    Assert ($null -ne (Owned-Process $identity)) 'owned PID and creation time match'
    $wrong=[pscustomobject]@{Id=$identity.Id;Ticks='0';Path=$identity.Path}
    Assert ($null -eq (Owned-Process $wrong)) 'stale/reused PID rejected'
    $partialProcess=[pscustomobject]@{Id=$PID;Path=$null;HasExited=$false}
    Assert ((Process-ImagePath $partialProcess) -eq (Get-Process -Id $PID).Path) 'CIM resolves image path when Process.Path is unavailable'
    $unknownProcess=[pscustomobject]@{Id=2147483000;Path=$null;HasExited=$false}
    Assert-Throws { Process-ImagePath $unknownProcess } 'Không xác minh được đường dẫn' 'unverifiable image path fails without dropping ownership'
    $exitedProcess=[pscustomobject]@{Id=2147483000;HasExited=$true;StartTime=$null}
    Assert ($null -eq (Process-StartTicks $exitedProcess)) 'process exiting during Stop does not cause a null method error'
    $script:State.Web=@($identity)
    $tree=@(Process-Tree $script:State.Web)
    Assert (@($tree | Where-Object Id -eq $other.Id).Count -eq 0) 'unrelated process excluded from owned tree'
    Stop-Demo
    Assert (-not (Get-Process -Id $own.Id -ErrorAction SilentlyContinue)) 'Stop terminates owned process'
    Assert ($null -ne (Get-Process -Id $other.Id -ErrorAction SilentlyContinue)) 'Stop preserves unrelated process'
    Stop-Demo
    Assert $true 'Stop is repeatable'
    $serverFile=Join-Path $fixture 'listener.ps1'
    $readyFile=Join-Path $fixture 'listener-ready.txt'
    @'
param($ReadyFile)
$server=New-Object Net.Sockets.TcpListener([Net.IPAddress]::Loopback,0)
$server.Start()
Set-Content -LiteralPath $ReadyFile -Value $server.LocalEndpoint.Port
while ($true) { Start-Sleep -Seconds 1 }
'@ | Set-Content -LiteralPath $serverFile -Encoding UTF8
    $serverProcess=Start-Process $shell -ArgumentList ('-NoProfile -ExecutionPolicy Bypass -File "{0}" "{1}"' -f $serverFile,$readyFile) -WindowStyle Hidden -PassThru
    $serverIdentity=Process-Identity $serverProcess
    $testProcesses += $serverIdentity
    $readyUntil=(Get-Date).AddSeconds(10)
    while (-not (Test-Path $readyFile) -and (Get-Date) -lt $readyUntil) { Start-Sleep -Milliseconds 100 }
    Assert (Test-Path $readyFile) 'regression listener starts'
    $serverPort=[int](Get-Content $readyFile -Raw)
    $script:State.Web=@($serverIdentity)
    try {
        function Stop-Process { param($InputObject,[switch]$Force) }
        Assert-Throws { Stop-Demo } 'chưa thoát' 'Stop refuses success when termination has not completed'
        $saved=Get-Content $script:StatePath -Raw | ConvertFrom-Json
        Assert (@($saved.Web | Where-Object Id -eq $serverIdentity.Id).Count -eq 1) 'failed Stop retains process ownership on disk'
        Assert (-not (Port-Free $serverPort)) 'failed Stop does not hide a live listener'
    } finally { Remove-Item Function:Stop-Process }
    Stop-Demo
    Assert (Port-Free $serverPort) 'successful Stop waits until owned listener releases its port'
    $listener=New-Object Net.Sockets.TcpListener([Net.IPAddress]::Loopback,0)
    $listener.Start()
    $port=$listener.LocalEndpoint.Port
    Assert (-not (Port-Free $port)) 'foreign listener detected without killing it'
    Assert $listener.Server.IsBound 'foreign listener remains bound'
    $listener.Stop(); $listener=$null
    Assert (Port-Free $port) 'released port available'
    $oldSql=$env:M1_SQL_TEST_CONNECTION
    try {
        Remove-Item Env:M1_SQL_TEST_CONNECTION -ErrorAction SilentlyContinue
        $TestLevel='Full'
        Assert-Throws { Run-Test } 'Full test CHƯA CHẠY.*M1_SQL_TEST_CONNECTION' 'full test missing SQL is explicit failure'
    } finally { $env:M1_SQL_TEST_CONNECTION=$oldSql }
    # Fault injection only: prove raw Docker output never escapes Compose.
    $originalDocker=${function:Docker-Result}
    try {
        $script:ComposeArgs=@()
        function Docker-Result { return @{Code=1;Text='registry TLS handshake timeout Password=CANARY_DO_NOT_PRINT'} }
        Assert-Throws { $null=Compose @('build','api') } '^Docker registry tạm thời không truy cập được' 'registry fault uses safe Vietnamese message'
        function Docker-Result { return @{Code=1;Text='secret=CANARY_DO_NOT_PRINT'} }
        $message=''
        try { $null=Compose @('up') } catch { $message=$_.Exception.Message }
        Assert ($message -and $message -notmatch 'CANARY') 'generic Docker failure hides raw output'
    } finally { Set-Item Function:Docker-Result $originalDocker }
    # Test container ownership with fixtures; never stop or inspect the real Docker environment.
    function Docker-Ready { return $true }
    function Containers { return [pscustomobject]@{Id='fixture-container';Service='sqlserver';Running=$true} }
    function Compose { throw 'Unexpected Compose mutation during isolated self-test' }
    $script:State.Containers=@([pscustomobject]@{Id='not-the-real-container-id';Service='sqlserver'})
    Stop-Demo
    $after=Containers
    Assert ($after.Id -eq 'fixture-container' -and $after.Running) 'Stop ignores container whose ID does not match'
    # Rebuild regression uses fixtures only; it must not stop/delete any real container.
    Set-Content "$fixture/infra/.env" 'fixture-only'
    function Ensure-Docker {}
    $sqlFixture=[pscustomobject]@{Id='fixture-sql';Service='sqlserver';Running=$true}
    $apiFixture=[pscustomobject]@{Id='fixture-api';Service='api';Running=$true}
    $migrateFixture=[pscustomobject]@{Id='fixture-migrate';Service='migrate';Running=$false}
    $script:State.Containers=@($sqlFixture)
    $script:RebuildRows=@($sqlFixture,$apiFixture,$migrateFixture)
    $script:RebuildEvents=@(); $script:RebuildReply=''; $script:RebuildRmCode=0
    function Containers { return $script:RebuildRows }
    function Read-Host { param($Prompt) return $script:RebuildReply }
    function Compose { param([string[]]$Arguments) $script:RebuildEvents+=('compose:'+($Arguments -join '|')); return '' }
    function Docker-Result { param([string[]]$Arguments) $script:RebuildEvents+=('docker:'+($Arguments -join '|')); return @{Code=$script:RebuildRmCode;Text='fixture-only'} }
    Assert-Throws { Rebuild-Api } 'API/migrate còn chạy' 'Rebuild refuses running API outside launcher'
    Assert ($script:RebuildEvents.Count -eq 0) 'running API refusal performs no build, stop or removal'
    $apiFixture.Running=$false
    Rebuild-Api
    Assert ($script:RebuildEvents.Count -eq 0) 'cancelled Rebuild performs no Docker mutations'
    $script:RebuildReply='REBUILD'
    Rebuild-Api
    Assert ($script:RebuildEvents[0] -eq 'compose:build|api|migrate') 'confirmed Rebuild builds only application images'
    Assert ($script:RebuildEvents[1] -eq 'docker:container|rm|fixture-api|fixture-migrate') 'Rebuild removes only confirmed app IDs without force or volume flags'
    Assert ($script:State.Containers.Count -eq 1 -and $script:State.Containers[0].Id -eq 'fixture-sql') 'Rebuild preserves SQL ownership and container'
    $script:RebuildRmCode=1
    Assert-Throws { Rebuild-Api } 'không tự dừng container' 'Rebuild fails safely if confirmed container starts before removal'
    $script:Root=$realRoot
    $AccountsFile=$null
    Assert-Throws { Open-Accounts } 'Chưa cấu hình VAC_TEST_ACCOUNTS_FILE' 'account book requires local configuration'
    $AccountsFile=Join-Path $fixture 'missing-account-book.md'
    Assert-Throws { Open-Accounts } 'Không tìm thấy file sổ tài khoản' 'missing account book is explicit'
    $AccountsFile='https://example.invalid/account-book'
    Assert-Throws { Open-Accounts } 'đường dẫn tuyệt đối' 'account book cannot be a remote URL'
    $AccountsFile=Join-Path $script:Root 'account-book.md'
    Assert-Throws { Open-Accounts } 'phải nằm ngoài repository' 'account book must remain outside the repository'
    $mailFixture=Join-Path $env:TEMP ('VAC Mail Sink '+[guid]::NewGuid().ToString('N'))
    @(
        'MSSQL_SA_PASSWORD=PreserveSqlValue'
        'IDENTITY_SIGNING_KEY=PreserveSigningValue'
        'IDENTITY_MAIL_ENCRYPTION_KEY=PreserveEncryptionValue'
        "IDENTITY_MAIL_DIRECTORY=$($mailFixture.Replace('\','/'))"
        'CUSTOM_SETTING=preserve-me'
        'REMINDER_MAIL_MODE=Disabled'
    ) | Set-Content -LiteralPath "$fixture/infra/.env" -Encoding UTF8
    $mailSetup="$realRoot/scripts/devtools/VAC-MailSetup.ps1"
    $mailCheck="$realRoot/scripts/devtools/VAC-MailCheck.ps1"
    Assert-Throws { & $mailSetup -RepositoryRoot $fixture -RemoveCredentials } 'chỉ dùng cùng -Disable' 'credential removal requires explicit disable mode'
    Assert-Throws { & $mailSetup -RepositoryRoot $fixture -Disable -DisableVerificationRecipients } 'Chỉ chọn một chế độ' 'mail setup rejects conflicting disable modes'
    $canary=[guid]::NewGuid().ToString('N').Substring(0,16)
    $secure=ConvertTo-SecureString $canary -AsPlainText -Force
    $setupOutput=(& $mailSetup -RepositoryRoot $fixture -SenderAddress 'mail.tooling@example.invalid' -AppPassword $secure -VerificationRecipients On *>&1 | Out-String)
    $mailEnv=Get-Content "$fixture/infra/.env" -Raw
    Assert ($setupOutput -notmatch [regex]::Escape($canary)) 'mail setup never prints App Password'
    Assert (Test-Path -LiteralPath $mailFixture -PathType Container) 'mail setup creates local verification directory'
    Assert ($mailEnv -match '(?m)^MSSQL_SA_PASSWORD=PreserveSqlValue\r?$' -and $mailEnv -match '(?m)^IDENTITY_SIGNING_KEY=PreserveSigningValue\r?$' -and $mailEnv -match '(?m)^IDENTITY_MAIL_ENCRYPTION_KEY=PreserveEncryptionValue\r?$') 'mail setup preserves SQL and Identity keys'
    Assert ($mailEnv -match '(?m)^REMINDER_MAIL_MODE=Smtp\r?$' -and $mailEnv -match '(?m)^IDENTITY_ALLOW_REAL_DEVELOPMENT_VERIFICATION_RECIPIENTS=true\r?$') 'mail setup enables SMTP and explicit verification opt-in'
    $null=& $mailSetup -RepositoryRoot $fixture -DisableVerificationRecipients
    $mailEnv=Get-Content "$fixture/infra/.env" -Raw
    Assert ($mailEnv -match '(?m)^REMINDER_MAIL_MODE=Smtp\r?$' -and $mailEnv.Contains("REMINDER_MAIL_PASSWORD=$canary") -and $mailEnv -match '(?m)^IDENTITY_ALLOW_REAL_DEVELOPMENT_VERIFICATION_RECIPIENTS=false\r?$') 'verification opt-in closes without disabling SMTP'
    $nextCanary=[guid]::NewGuid().ToString('N').Substring(0,16)
    $nextSecure=ConvertTo-SecureString $nextCanary -AsPlainText -Force
    $null=& $mailSetup -RepositoryRoot $fixture -SenderAddress 'mail.tooling@example.invalid' -AppPassword $nextSecure -VerificationRecipients Off
    $mailEnv=Get-Content "$fixture/infra/.env" -Raw
    foreach ($key in @('REMINDER_MAIL_MODE','REMINDER_MAIL_HOST','REMINDER_MAIL_PORT','REMINDER_MAIL_USERNAME','REMINDER_MAIL_PASSWORD','REMINDER_MAIL_SENDER_ADDRESS','REMINDER_MAIL_SENDER_NAME','IDENTITY_ALLOW_REAL_DEVELOPMENT_VERIFICATION_RECIPIENTS')) {
        Assert (@([regex]::Matches($mailEnv,"(?m)^$key=")).Count -eq 1) "mail setup rerun has one $key"
    }
    Assert ($mailEnv.Contains("REMINDER_MAIL_PASSWORD=$nextCanary") -and -not $mailEnv.Contains($canary)) 'mail setup rerun replaces App Password'
    $null=& $mailSetup -RepositoryRoot $fixture -Disable
    $mailEnv=Get-Content "$fixture/infra/.env" -Raw
    Assert ($mailEnv -match '(?m)^REMINDER_MAIL_MODE=Disabled\r?$' -and $mailEnv -match '(?m)^IDENTITY_ALLOW_REAL_DEVELOPMENT_VERIFICATION_RECIPIENTS=false\r?$') 'mail disable closes delivery and real-recipient opt-in'
    Assert ($mailEnv.Contains("REMINDER_MAIL_PASSWORD=$nextCanary")) 'mail disable keeps local credential by default'
    $null=& $mailSetup -RepositoryRoot $fixture -Disable -RemoveCredentials
    $mailEnv=Get-Content "$fixture/infra/.env" -Raw
    Assert ($mailEnv -match '(?m)^REMINDER_MAIL_PASSWORD=\r?$' -and $mailEnv -match '(?m)^REMINDER_MAIL_USERNAME=\r?$' -and $mailEnv -match '(?m)^REMINDER_MAIL_SENDER_ADDRESS=\r?$') 'mail disable can remove local credential fields'
    Assert ($mailEnv -match '(?m)^CUSTOM_SETTING=preserve-me\r?$' -and $mailEnv -match '(?m)^MSSQL_SA_PASSWORD=PreserveSqlValue\r?$') 'mail disable preserves unrelated configuration'
    $checkOutput=(& $mailCheck -RepositoryRoot $fixture *>&1 | Out-String)
    Assert ($checkOutput -notmatch [regex]::Escape($nextCanary)) 'mail readiness never prints App Password'
    Write-Host "SELF-TEST: $passed assertions passed. Registry error was simulated, not a real outage."
} finally {
    foreach ($identity in $testProcesses) { $p=Owned-Process $identity; if ($p) { Stop-Process -InputObject $p -Force -ErrorAction SilentlyContinue } }
    if ($listener) { $listener.Stop() }
    if ($mailFixture -and (Test-Path -LiteralPath $mailFixture)) { Remove-Item -LiteralPath $mailFixture -Recurse -Force }
    # Only the unique test fixture created above is removed.
    $resolved=[IO.Path]::GetFullPath($fixture)
    if ($resolved.StartsWith([IO.Path]::GetFullPath($env:TEMP),[StringComparison]::OrdinalIgnoreCase) -and (Split-Path $resolved -Leaf) -like 'VAC Launcher Test *' -and (Test-Path -LiteralPath $resolved)) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
    Set-Location $originalLocation
}
