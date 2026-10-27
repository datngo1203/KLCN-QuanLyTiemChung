[CmdletBinding()]
param(
    [ValidateSet('start','stop','status','test','rebuild','accounts')][string]$Mode = 'status',
    [ValidateSet('Smoke','Full')][string]$TestLevel = 'Smoke',
    [string]$RepositoryRoot,
    [ValidatePattern('^[a-z0-9][a-z0-9_-]*$')][string]$ComposeProject = $(if ($env:VAC_COMPOSE_PROJECT) { $env:VAC_COMPOSE_PROJECT } else { 'klcn-s0' }),
    [string]$AccountsFile = $env:VAC_TEST_ACCOUNTS_FILE,
    [switch]$Interactive,
    [ValidateSet('Web','Windows')][string]$Worker
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
        if ((Test-Path "$candidate/infra/compose.yaml") -and (Test-Path "$candidate/apps/windows/TiemChung.Desktop.csproj")) {
            return (Resolve-Path $candidate).Path.TrimEnd('\')
        }
    }
    throw 'Không tìm thấy repository VAC. Giữ scripts/devtools trong repository hoặc dùng -RepositoryRoot.'
}
function State-Key($Root, $Project) {
    $identity = $Root.ToLowerInvariant()
    # Preserve the tested local launcher's state for the existing klcn-s0 demo.
    if ($Project -ne 'klcn-s0') { $identity += "|$Project" }
    $hash = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes($identity)))).Replace('-','').Substring(0,16) }
    finally { $hash.Dispose() }
}
function Require-Tool($name, $message) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) { throw $message }
}
function Docker-Result([string[]]$Arguments) {
    # Never echo Docker/Compose output: configuration errors can contain secrets.
    $ErrorActionPreference = 'Continue'
    $lines = @(& docker @Arguments 2>&1)
    return @{ Code = $LASTEXITCODE; Text = ($lines -join "`n") }
}
function Compose([string[]]$Arguments) {
    $r = Docker-Result ($script:ComposeArgs + $Arguments)
    if ($r.Code -ne 0) {
        if ($r.Text -match '(?i)registry|TLS handshake|proxyconnect|lookup .*no such host|failed to resolve|dial tcp|unexpected status.*(429|50[0234])') {
            throw 'Docker registry tạm thời không truy cập được. Kiểm tra mạng rồi thử lại; dữ liệu SQL được giữ nguyên.'
        }
        if ($r.Text -match '(?i)port is already allocated|address already in use|ports are not available') {
            throw 'Port Docker đang bị chiếm. Dùng Check để kiểm tra; launcher không dừng tiến trình khác.'
        }
        if ($r.Text -match '(?i)no such image|unable to find image|pull access denied') {
            throw 'Chưa có image cần thiết. Chọn Rebuild trong VAC-Check, rồi chạy Start lại.'
        }
        throw 'Lệnh Docker Compose không thành công. Kiểm tra Docker, infra/.env và image bằng công cụ Docker; launcher đã ẩn output để bảo vệ cấu hình.'
    }
    return $r.Text
}
function Docker-Ready {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { return $false }
    $r = Docker-Result @('info','--format','{{.OSType}}')
    return ($r.Code -eq 0 -and $r.Text.Trim() -eq 'linux')
}
function Ensure-Docker {
    Require-Tool docker 'Thiếu Docker Desktop. Cài Docker Desktop và bật Linux containers.'
    if (Docker-Ready) { return }
    Write-Host 'Docker chưa chạy. Đang mở Docker Desktop và chờ Engine (tối đa 180 giây)...'
    $programFiles = if ($env:ProgramW6432) { $env:ProgramW6432 } else { $env:ProgramFiles }
    $desktop = "$programFiles/Docker/Docker/Docker Desktop.exe"
    if (-not (Test-Path $desktop)) { throw 'Docker chưa chạy. Hãy mở Docker Desktop ở chế độ Linux containers rồi thử lại.' }
    $start = Docker-Result @('desktop','start','--detach')
    if ($start.Code -ne 0) { Start-Process -FilePath $desktop -WindowStyle Hidden | Out-Null }
    $until = (Get-Date).AddSeconds(180)
    do {
        if (Docker-Ready) { return }
        Start-Sleep -Seconds 3
    } while ((Get-Date) -lt $until)
    throw 'Docker chưa sẵn sàng sau 180 giây. Mở Docker Desktop, kiểm tra Linux Engine rồi chạy Start lại.'
}
function Containers {
    $r = Docker-Result @('ps','-a','--filter',"label=com.docker.compose.project=$ComposeProject",'--format','{{.ID}}|{{.Labels}}|{{.State}}|{{.Status}}')
    if ($r.Code -ne 0) { throw 'Không đọc được trạng thái container Docker.' }
    foreach ($line in ($r.Text -split "`n")) {
        if ($line.Trim()) {
            $p = $line.Trim() -split '\|',4
            if ($p[1] -match '(?:^|,)com.docker.compose.service=([^,]+)') {
                [pscustomobject]@{ Id=$p[0]; Service=$Matches[1]; Running=($p[2] -eq 'running'); Status=$p[3] }
            }
        }
    }
}
function Save-State {
    $script:State | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath "$script:StatePath.tmp" -Encoding UTF8
    Move-Item -LiteralPath "$script:StatePath.tmp" -Destination $script:StatePath -Force
}
function Process-ImagePath($process) {
    $path = $null
    try { $path = $process.Path } catch {}
    # 32-bit PowerShell returns null for .Path on 64-bit Node/WPF processes.
    if (-not $path) {
        $row = Get-CimInstance Win32_Process -Filter "ProcessId=$($process.Id)" -ErrorAction Stop
        $path = $row.ExecutablePath
    }
    if (-not $path -and -not $process.HasExited) {
        throw "Không xác minh được đường dẫn tiến trình PID $($process.Id). Giữ nguyên thông tin quản lý; chưa thể xác nhận tiến trình đã dừng."
    }
    return $path
}
function Process-StartTicks($process) {
    try {
        if ($process.HasExited) { return $null }
        $start = $process.StartTime
        if ($start) { return $start.ToUniversalTime().Ticks.ToString() }
    } catch {
        if ($process.HasExited) { return $null }
    }
    if ($process.HasExited) { return $null }
    throw "Không xác minh được thời điểm tạo tiến trình PID $($process.Id). Giữ nguyên thông tin quản lý."
}
function Process-Identity($process) {
    $ticks = Process-StartTicks $process
    if (-not $ticks) { return $null }
    $path = Process-ImagePath $process
    if (-not $path) { return $null }
    return [pscustomobject]@{ Id=$process.Id; Ticks=$ticks; Path=$path }
}
function Owned-Process($identity) {
    if (-not $identity) { return $null }
    $p = Get-Process -Id $identity.Id -ErrorAction SilentlyContinue
    if ($p -and (Process-StartTicks $p) -eq $identity.Ticks -and (Process-ImagePath $p) -eq $identity.Path) { return $p }
    return $null
}
function Process-Tree($identities) {
    $found = @{}
    foreach ($identity in $identities) {
        $p = Owned-Process $identity
        if ($p) {
            $current = Process-Identity $p
            if ($current) { $found[$p.Id] = $current }
        }
    }
    $all = @(Get-CimInstance Win32_Process | Select-Object ProcessId,ParentProcessId)
    do {
        $added = $false
        foreach ($child in $all) {
            if ($found.ContainsKey([int]$child.ParentProcessId) -and -not $found.ContainsKey([int]$child.ProcessId)) {
                $parent = Owned-Process $found[[int]$child.ParentProcessId]
                $p = Get-Process -Id $child.ProcessId -ErrorAction SilentlyContinue
                if ($parent -and $p) {
                    $current = Process-Identity $p
                    if ($current -and [long]$current.Ticks -ge [long]$found[$parent.Id].Ticks) {
                        $found[$p.Id] = $current; $added = $true
                    }
                }
            }
        }
    } while ($added)
    return @($found.Values)
}
function Port-Free([int]$Port) {
    $listener = New-Object Net.Sockets.TcpListener([Net.IPAddress]::Loopback,$Port)
    try { $listener.Server.ExclusiveAddressUse = $true; $listener.Start(); return $true }
    catch { return $false }
    finally { $listener.Stop() }
}
function Http-Ok($url, [switch]$Api) {
    try {
        $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 3
        if ($Api) { $data = $r.Content | ConvertFrom-Json; return ($r.StatusCode -eq 200 -and $data.status -eq 'ok' -and $data.service -eq 'TiemChung.Api') }
        return ($r.StatusCode -eq 200 -and $r.Content -match 'id="root"')
    } catch { return $false }
}
function Wait-Http($url, [switch]$Api, [int]$Seconds=120) {
    $until = (Get-Date).AddSeconds($Seconds)
    do {
        if (Http-Ok $url -Api:$Api) { return }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $until)
    if ($Api) { throw 'API chưa sẵn sàng tại http://127.0.0.1:5080/health. Dùng Check; nếu source đã đổi, chọn Rebuild.' }
    throw 'Web chưa sẵn sàng tại port 5173. Kiểm tra Node/dependencies hoặc port bị chiếm.'
}
function Catalog-Ok {
    try {
        $r = Invoke-RestMethod 'http://127.0.0.1:5080/api/v1/catalog/vaccines' -TimeoutSec 5
        return ($r.source -eq 'database' -and $null -ne $r.items)
    } catch { return $false }
}
function Start-Worker([string]$Name) {
    $live = @(Process-Tree @($script:State.$Name))
    if ($live.Count) { $script:State.$Name = $live; Save-State; return }
    if ($Name -eq 'Web' -and -not (Port-Free 5173)) { throw 'Port 5173 đang bị chiếm bởi tiến trình ngoài launcher. Hãy tự đóng tiến trình đó hoặc dùng môi trường riêng.' }
    if ($Name -eq 'Windows') {
        $external = @(Get-CimInstance Win32_Process | Where-Object {
            $_.Name -eq 'TiemChung.Desktop.exe' -and $_.ExecutablePath -and $_.ExecutablePath.StartsWith("$script:Root\apps\windows\",[StringComparison]::OrdinalIgnoreCase)
        })
        if ($external.Count) { throw 'Windows VAC đã mở ngoài launcher. Hãy đóng cửa sổ đó trước; launcher không tiếp quản tiến trình khác.' }
    }
    $shell = (Get-Process -Id $PID).Path
    $arguments = '-NoProfile -ExecutionPolicy Bypass -File "{0}" -RepositoryRoot "{1}" -Worker {2}' -f $PSCommandPath,$script:Root,$Name
    $p = Start-Process -FilePath $shell -ArgumentList $arguments -WorkingDirectory $script:Root -WindowStyle Hidden -PassThru
    $script:State.$Name = @((Process-Identity $p)); Save-State
}
function Show-Status {
    $healthy = $true
    if (Docker-Ready) {
        $containers = @(Containers)
        foreach ($name in @('sqlserver','api')) {
            $c = $containers | Where-Object Service -eq $name
            $owned = @($script:State.Containers | Where-Object { $_.Id -eq $c.Id -and $_.Service -eq $name }).Count -gt 0
            if ($c) { Write-Host ("{0}: {1} ({2})" -f $name,$c.Status,$(if ($owned) {'launcher quản lý'} else {'ngoài launcher'})) }
            else { Write-Host "${name}: chưa có container" }
            if (-not $c -or -not $c.Running -or ($name -eq 'sqlserver' -and $c.Status -notmatch '\(healthy\)')) { $healthy = $false }
        }
    } else { Write-Host 'Docker chưa chạy hoặc chưa ở chế độ Linux containers.'; $healthy = $false }
    $api = Http-Ok 'http://127.0.0.1:5080/health' -Api
    Write-Host "API /health: $(if ($api) {'OK'} else {'chưa sẵn sàng'})"
    $catalog = Catalog-Ok
    Write-Host "API đọc danh mục SQL: $(if ($catalog) {'OK (database)'} else {'chưa xác minh được'})"
    $web = Http-Ok 'http://127.0.0.1:5173'
    $webOwned = @(Process-Tree @($script:State.Web)).Count -gt 0
    Write-Host "Web 5173: $(if ($web) {'OK'} else {'chưa sẵn sàng'}); $(if ($webOwned) {'launcher quản lý'} else {'ngoài launcher hoặc chưa chạy'})"
    $windows = @(Process-Tree @($script:State.Windows) | ForEach-Object { Owned-Process $_ } | Where-Object { $_ -and $_.ProcessName -eq 'TiemChung.Desktop' -and $_.MainWindowHandle -ne 0 })
    $external = @(Get-Process -Name TiemChung.Desktop -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "$script:Root\apps\windows\*" -and $_.MainWindowHandle -ne 0 })
    Write-Host "Windows: $(if ($windows.Count) {'cửa sổ đang mở, launcher quản lý'} elseif ($external.Count) {'cửa sổ đang mở ngoài launcher'} else {'chưa mở'})"
    return ($healthy -and $api -and $catalog -and $web -and $windows.Count -gt 0 -and $webOwned)
}
function Start-Demo {
    Require-Tool dotnet 'Thiếu .NET SDK 10. Cài SDK theo README.md.'
    Require-Tool node 'Thiếu Node.js 24 LTS. Cài Node kèm npm theo README.md.'
    Require-Tool npm.cmd 'Thiếu npm. Cài Node.js kèm npm theo README.md.'
    $sdks = @(& dotnet --list-sdks 2>$null)
    if (-not ($sdks -match '^10\.')) { throw 'Thiếu .NET SDK 10. Chỉ có runtime là chưa đủ; cài SDK theo README.md.' }
    $nodeVersion = & node --version 2>$null
    if ($LASTEXITCODE -or $nodeVersion -notmatch '^v(\d+)\.' -or [int]$Matches[1] -lt 24) { throw 'Cần Node.js 24 LTS trở lên để chạy Web hiện tại.' }
    if (-not (Test-Path "$script:Root/infra/.env")) { throw 'Chưa có infra/.env. Thiết lập lần đầu theo README.md; launcher không tạo hoặc ghi đè cấu hình.' }
    Ensure-Docker
    $before = @(Containers)
    foreach ($entry in @(@{Port=14333;Service='sqlserver'},@{Port=5080;Service='api'})) {
        $existing = $before | Where-Object { $_.Service -eq $entry.Service -and $_.Running }
        if (-not $existing -and -not (Port-Free $entry.Port)) { throw "Port $($entry.Port) đang bị chiếm ngoài container $ComposeProject tương ứng. Launcher không dừng tiến trình đó." }
    }
    if (-not @(Process-Tree @($script:State.Web)).Count -and -not (Port-Free 5173)) { throw 'Port 5173 đang bị chiếm bởi tiến trình ngoài launcher. Hãy tự đóng tiến trình đó rồi thử lại.' }
    if (-not (Test-Path "$script:Root/apps/web/node_modules/vite/bin/vite.js")) {
        Write-Host 'Chưa có dependencies Web. Đang chạy npm ci...'
        & npm.cmd --prefix "$script:Root/apps/web" ci *> $null
        if ($LASTEXITCODE) { throw 'Không cài được dependencies Web. Kiểm tra mạng/npm registry rồi thử lại.' }
    }
    Write-Host 'Đang khởi động SQL/API bằng image có sẵn (không build, không tạo lại container)...'
    try { $null = Compose @('up','-d','--no-build','--pull','never','--no-recreate','api') }
    finally {
        # Record partially started containers too, so Stop can clean up after an error.
        foreach ($c in @(Containers)) {
            $wasRunning = @($before | Where-Object { $_.Id -eq $c.Id -and $_.Running }).Count -gt 0
            if ($c.Running -and -not $wasRunning -and $c.Service -in @('sqlserver','api','migrate')) {
                $script:State.Containers = @($script:State.Containers | Where-Object Service -ne $c.Service) + @($c)
            }
        }
        Save-State
    }
    Wait-Http 'http://127.0.0.1:5080/health' -Api
    Start-Worker Web
    Wait-Http 'http://127.0.0.1:5173'
    Start-Worker Windows
    Write-Host 'Đang chờ cửa sổ Windows (dotnet run có thể cần build/restore lần đầu)...'
    $until = (Get-Date).AddSeconds(150)
    do {
        $script:State.Windows = @(Process-Tree @($script:State.Windows)); Save-State
        $window = @($script:State.Windows | ForEach-Object { Owned-Process $_ } | Where-Object { $_ -and $_.ProcessName -eq 'TiemChung.Desktop' -and $_.MainWindowHandle -ne 0 })
        if ($window.Count) { break }
        if (-not $script:State.Windows.Count) { throw 'Windows không mở được. Kiểm tra .NET SDK 10 và chạy dotnet build apps/windows/TiemChung.Desktop.csproj để xem lỗi build.' }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $until)
    if (-not $window.Count) { throw 'Windows chưa mở cửa sổ sau 150 giây. Dùng Check; kiểm tra restore/build .NET.' }
    $script:State.Web = @(Process-Tree @($script:State.Web)); Save-State
    Start-Process 'http://127.0.0.1:5173'
    $null = Show-Status
    Write-Host 'VAC đã sẵn sàng. Không seed hoặc thay đổi tài khoản demo.'
}
function Stop-Demo {
    foreach ($name in @('Windows','Web')) {
        $tree = @(Process-Tree @($script:State.$name))
        # Keep every child identity until termination is confirmed, even if its parent exits first.
        $script:State.$name = $tree; Save-State
        foreach ($identity in $tree) {
            $p = Owned-Process $identity
            if ($p -and $p.ProcessName -eq 'TiemChung.Desktop' -and $p.MainWindowHandle -ne 0) { $null = $p.CloseMainWindow(); $null = $p.WaitForExit(5000) }
        }
        foreach ($identity in ($tree | Sort-Object { [long]$_.Ticks } -Descending)) {
            $p = Owned-Process $identity
            if ($p) {
                try { Stop-Process -InputObject $p -Force }
                catch { if (Owned-Process $identity) { throw "Không dừng được $name (PID $($identity.Id)). Trạng thái quản lý vẫn được giữ; hãy chạy VAC-Stop lại." } }
                if (-not $p.WaitForExit(10000)) { throw "$name (PID $($identity.Id)) chưa thoát. Trạng thái quản lý vẫn được giữ; hãy chạy VAC-Stop lại." }
            }
        }
        $script:State.$name = @(Process-Tree $tree); Save-State
        if ($script:State.$name.Count) { throw "$name còn tiến trình do launcher quản lý. Chưa hoàn tất Stop; hãy chạy VAC-Stop lại." }
        Write-Host "${name}: đã xác nhận tiến trình launcher quản lý thoát hoàn toàn."
    }
    if ($script:State.Containers.Count) {
        if (-not (Docker-Ready)) { throw 'Docker chưa chạy. Web/Windows đã dừng; trạng thái container được giữ để Stop lại khi Docker hoạt động.' }
        $current = @(Containers)
        foreach ($service in @('api','migrate','sqlserver')) {
            $owned = $script:State.Containers | Where-Object Service -eq $service
            $c = $current | Where-Object Service -eq $service
            if ($owned -and $c -and $owned.Id -eq $c.Id) { $null = Compose @('stop','--timeout','30',$service) }
            elseif ($owned -and $c) { Write-Host "Bỏ qua ${service}: ID container đã đổi ngoài launcher." }
        }
        $script:State.Containers = @(); Save-State
    }
    $webListeners = @(Get-NetTCPConnection -State Listen -LocalPort 5173 -ErrorAction SilentlyContinue)
    if ($webListeners.Count) {
        $owners = ($webListeners.OwningProcess | Sort-Object -Unique) -join ', '
        Write-Host "Port 5173 vẫn đang được tiến trình ngoài trạng thái quản lý sử dụng (PID: $owners). Launcher đã giữ nguyên tiến trình đó." -ForegroundColor Yellow
    } else { Write-Host 'Port 5173: không còn tiến trình lắng nghe.' }
    Write-Host 'Đã dừng các thành phần launcher quản lý. Container chạy từ trước và dữ liệu SQL được giữ nguyên; Docker Desktop vẫn mở.'
}
function Run-Test {
    if ($TestLevel -eq 'Smoke') {
        Write-Host 'Smoke chỉ đọc: trạng thái tiến trình/container và HTTP GET; không đăng nhập, seed hoặc ghi dữ liệu.'
        if (-not (Show-Status)) { throw 'Smoke chưa đạt: một hoặc nhiều thành phần chưa sẵn sàng. Dùng VAC-Start rồi kiểm tra lại.' }
        Write-Host 'Smoke PASS. Kết quả này không thay full test hoặc kiểm thử nghiệp vụ UI/API/SQL.'
        return
    }
    Require-Tool dotnet 'Thiếu .NET SDK 10.'
    Require-Tool node 'Thiếu Node.js 24 LTS.'
    Require-Tool npm.cmd 'Thiếu npm.'
    if (-not $env:M1_SQL_TEST_CONNECTION) { throw 'Full test CHƯA CHẠY: thiếu M1_SQL_TEST_CONNECTION trong môi trường. Cấu hình SQL development/test có quyền tạo DB test riêng theo README; không dùng tài khoản production. Smoke vẫn dùng được.' }
    Write-Host 'Full test: scripts/verify.ps1 (backend SQL test riêng, Web lint/test/build, Windows build). Không in log/credential.'
    $shell = (Get-Process -Id $PID).Path
    & $shell -NoProfile -ExecutionPolicy Bypass -File "$script:Root/scripts/verify.ps1" *> $null
    if ($LASTEXITCODE) { throw 'Full test FAIL trong scripts/verify.ps1. Output đã ẩn để không lộ credential; kiểm tra cấu hình SQL test/dependencies và chạy verify trong môi trường riêng để chẩn đoán.' }
    Write-Host 'Full test PASS: scripts/verify.ps1 hoàn tất với exit code 0.'
}
function Rebuild-Api {
    if (-not (Test-Path "$script:Root/infra/.env")) { throw 'Chưa có infra/.env. Thiết lập theo README trước.' }
    Ensure-Docker
    $running = @(Containers | Where-Object { $_.Service -in @('api','migrate') -and $_.Running })
    if ($running.Count) { throw 'API/migrate còn chạy. Dùng VAC-Stop trước; nếu chạy ngoài launcher, hãy tự dừng bằng Docker. Sau đó chọn Rebuild.' }
    Write-Host 'Đang build API/migrate từ source hiện tại. Chỉ dùng khi source backend hoặc Dockerfile đổi.'
    $null = Compose @('build','api','migrate')
    # Remove only stopped application containers; keep SQL container and volume intact.
    $null = Compose @('rm','-f','api','migrate')
    $script:State.Containers = @($script:State.Containers | Where-Object Service -eq 'sqlserver'); Save-State
    Write-Host 'Build xong. Chạy VAC-Start để dùng image mới; SQL và volume được giữ nguyên.'
}
function Open-Accounts {
    if (-not $AccountsFile) { throw 'Chưa cấu hình VAC_TEST_ACCOUNTS_FILE. Đặt đường dẫn sổ tài khoản local ngoài repository; xem README.md.' }
    $expanded = [Environment]::ExpandEnvironmentVariables($AccountsFile)
    if ($expanded -notmatch '^[a-zA-Z]:[\\/]') { throw 'Sổ tài khoản cần đường dẫn tuyệt đối trên ổ đĩa local, ngoài repository.' }
    try { $path = [IO.Path]::GetFullPath($expanded) }
    catch { throw 'Đường dẫn sổ tài khoản không hợp lệ. Kiểm tra VAC_TEST_ACCOUNTS_FILE.' }
    if ($path.StartsWith("$script:Root\", [StringComparison]::OrdinalIgnoreCase)) { throw 'Sổ tài khoản phải nằm ngoài repository; không đưa credential vào Git.' }
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw 'Không tìm thấy file sổ tài khoản. Kiểm tra VAC_TEST_ACCOUNTS_FILE; launcher không tạo hoặc sao chép sổ.' }
    Start-Process notepad.exe -ArgumentList ('"{0}"' -f $path)
    Write-Host 'Đã mở sổ tài khoản local. Launcher không đọc, sao chép hoặc in mật khẩu.'
}

$mutex = $null; $locked = $false; $exitCode = 0
try {
    $script:Root = Find-Repository
    Set-Location $script:Root
    if ($Worker) {
        # Worker shell owns the process tree; no application output is logged.
        if ($Worker -eq 'Web') { Set-Location "$script:Root/apps/web"; $env:VITE_API_BASE_URL='http://127.0.0.1:5080'; & node "$script:Root/apps/web/node_modules/vite/bin/vite.js" --host 127.0.0.1 --port 5173 --strictPort *> $null }
        else { $env:TIEMCHUNG_API_BASE_URL='http://127.0.0.1:5080'; & dotnet run --project "$script:Root/apps/windows/TiemChung.Desktop.csproj" *> $null }
        exit $LASTEXITCODE
    }
    $key = State-Key $script:Root $ComposeProject
    $stateDir = Join-Path $env:LOCALAPPDATA "VAC-DevTools/$key"
    $null = New-Item -ItemType Directory -Force $stateDir
    $script:StatePath = Join-Path $stateDir 'state.json'
    $script:ComposeArgs = @('compose','--env-file',"$script:Root/infra/.env",'-p',$ComposeProject,'-f',"$script:Root/infra/compose.yaml")
    $mutex = New-Object Threading.Mutex($false,"Local\VAC-DevTools-$key")
    try { $locked = $mutex.WaitOne(0) } catch [Threading.AbandonedMutexException] { $locked = $true }
    if (-not $locked) { throw 'Một thao tác VAC khác đang chạy. Chờ thao tác đó xong rồi thử lại.' }
    $script:State = [pscustomobject]@{ Web=@(); Windows=@(); Containers=@() }
    if (Test-Path $script:StatePath) { $script:State = Get-Content $script:StatePath -Raw | ConvertFrom-Json }
    do {
        try {
            switch ($Mode) {
                start { Start-Demo }
                stop { Stop-Demo }
                status { $null = Show-Status }
                test { Run-Test }
                rebuild { Rebuild-Api }
                accounts { Open-Accounts }
            }
            $exitCode = 0
        } catch { Write-Host "LỖI: $($_.Exception.Message)" -ForegroundColor Red; $exitCode = 1 }
        if (-not $Interactive) { break }
        Write-Host "`n[F] Full test   [A] Mở sổ tài khoản   [R] Rebuild API   [S] Status   [Enter] Thoát"
        $choice = Read-Host 'Chọn'
        switch ($choice.ToUpperInvariant()) {
            F { $Mode='test'; $TestLevel='Full' }
            A { $Mode='accounts' }
            R { $Mode='rebuild' }
            S { $Mode='status' }
            default { break }
        }
    } while ($choice.ToUpperInvariant() -in @('F','A','R','S'))
} catch { Write-Host "LỖI: $($_.Exception.Message)" -ForegroundColor Red; $exitCode = 1 }
finally { if ($locked) { $mutex.ReleaseMutex() }; if ($mutex) { $mutex.Dispose() } }
exit $exitCode
