# Capture one native viewport per image; never stitch, crop or normalize evidence.
[CmdletBinding()]
param(
    [string]$OutputDirectory = '.review',
    [ValidateRange(1,14)][int[]]$ScreenIds = @(1,2,3,4,5,6,7,8,9,10,11,12,13,14),
    [int]$TargetProcessId = 0,
    [ValidateRange(640,7680)][int]$WidthPixels = 1366,
    [ValidateRange(480,4320)][int]$HeightPixels = 768,
    [switch]$IncludeLogin,
    [switch]$CaptureGallery,
    [switch]$ValidateOnly
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
# Every invocation owns a fresh directory. No Remove-Item or overwrite of prior runs.
$parent = [IO.Path]::GetFullPath($OutputDirectory, $repo)
$run = Join-Path $parent ('run-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ') + '-' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($run)
$sourceSha = (& git -C $repo rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Không đọc được SHA nguồn.' }
$manifest = [ordered]@{
    schema = 1; sourceSha = $sourceSha; sourceDirty = @(& git -C $repo status --porcelain --untracked-files=no)
    startedUtc = [DateTime]::UtcNow.ToString('o'); command = $MyInvocation.Line
    kind = 'Native window viewport, physical pixels; no crop, normalization or stitching'
    requestedPixels = @{ width = $WidthPixels; height = $HeightPixels }
    captureScriptSha256 = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash
    results = [Collections.Generic.List[object]]::new(); status = 'started'; error = $null
    limits = @('No automatic error/empty states; states require separate reviewed fixtures.',
        'Recorded window DPI is metadata, not a 100/125/150 percent QA sweep.',
        'Source SHA describes checkout, not proof that the running executable was built from it.',
        'A13/A14 require the companion UI package. Gallery requires an already open gallery host.',
        'Local .review output is not reviewer-accessible until curated evidence is committed or attached.')
}
try {
    if ($ValidateOnly) { $manifest.status = 'validated-output-only'; return }
    if ($CaptureGallery -and $IncludeLogin) { throw 'Chọn Gallery hoặc Login, không chọn đồng thời.' }
    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes
    Add-Type -AssemblyName System.Drawing
    if (-not ('UiReviewNative' -as [type])) {
        Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class UiReviewNative {
 [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left,Top,Right,Bottom; }
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out RECT r);
 [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int hgt,uint flags);
 [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h,IntPtr dc,uint flags);
 [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr value);
}
'@
    }
    # Use physical coordinates for GetWindowRect/SetWindowPos; restore caller context in finally.
    $oldDpiContext = [UiReviewNative]::SetThreadDpiAwarenessContext([IntPtr](-4))
    if ($oldDpiContext -eq [IntPtr]::Zero) { throw 'Không đặt được DPI context cho capture.' }
    if ($TargetProcessId) { $candidates = @(Get-Process -Id $TargetProcessId | Where-Object MainWindowHandle) }
    else { $candidates = @(Get-Process TiemChung.Desktop -ErrorAction SilentlyContinue | Where-Object MainWindowHandle) }
    if ($candidates.Count -ne 1) { throw 'Cần đúng một cửa sổ ứng dụng; dùng -TargetProcessId khi có nhiều host.' }
    $process = $candidates[0]
    $manifest.process = @{ id=$process.Id; title=$process.MainWindowTitle; path=$process.Path; executableSha256=(Get-FileHash -LiteralPath $process.Path).Hash }
    $root = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    function Find-Button([string]$Name) {
        $condition = [System.Windows.Automation.AndCondition]::new(
            [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::Button),
            [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,$Name))
        @($root.FindAll([System.Windows.Automation.TreeScope]::Descendants,$condition)) |
            Where-Object { -not $_.Current.IsOffscreen -and $_.Current.IsEnabled } | Select-Object -First 1
    }
    function Invoke-Button([string]$Name) {
        $button = Find-Button $Name
        if (-not $button) { throw "Không tìm được nút đang hiển thị: $Name" }
        ([System.Windows.Automation.InvokePattern]$button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)).Invoke()
        Start-Sleep -Milliseconds 300
    }
    function Save-Viewport([string]$Name) {
        $handle = $process.MainWindowHandle
        $rect = [UiReviewNative+RECT]::new()
        if (-not [UiReviewNative]::GetWindowRect($handle,[ref]$rect)) { throw 'GetWindowRect thất bại.' }
        $width = $rect.Right-$rect.Left; $height = $rect.Bottom-$rect.Top
        $dpi = [UiReviewNative]::GetDpiForWindow($handle)
        if ($width -le 0 -or $height -le 0 -or $dpi -eq 0) { throw 'Kích thước/DPI cửa sổ không hợp lệ.' }
        $path = Join-Path $run ($Name + '.png')
        if (Test-Path -LiteralPath $path) { throw "Không ghi đè ảnh: $path" }
        $bitmap = [Drawing.Bitmap]::new($width,$height)
        $graphics = [Drawing.Graphics]::FromImage($bitmap)
        $dc = $graphics.GetHdc()
        try { if (-not [UiReviewNative]::PrintWindow($handle,$dc,2)) { throw 'PrintWindow thất bại.' } }
        finally { $graphics.ReleaseHdc($dc); $graphics.Dispose() }
        try { $bitmap.Save($path,[Drawing.Imaging.ImageFormat]::Png) } finally { $bitmap.Dispose() }
        $item = Get-Item -LiteralPath $path
        $manifest.results.Add(@{file=$item.Name; bytes=$item.Length; widthPixels=$width; heightPixels=$height; dpi=$dpi;
            inferredWidthDip=$width*96.0/$dpi; inferredHeightDip=$height*96.0/$dpi;
            sha256=(Get-FileHash -LiteralPath $path).Hash; utc=[DateTime]::UtcNow.ToString('o')})
        if ($item.Length -ge 500KB) { throw "Ảnh vượt 500 KB, cần curator xử lý riêng: $path" }
    }
    $rect = [UiReviewNative+RECT]::new()
    [void][UiReviewNative]::GetWindowRect($process.MainWindowHandle,[ref]$rect)
    if (-not [UiReviewNative]::SetWindowPos($process.MainWindowHandle,[IntPtr]::Zero,$rect.Left,$rect.Top,$WidthPixels,$HeightPixels,0x0014)) { throw 'Không đổi được kích thước cửa sổ.' }
    Start-Sleep -Milliseconds 400
    if ($CaptureGallery) {
        if ($process.MainWindowTitle -notmatch 'Gallery|Thư viện') { throw 'Cần mở Gallery bằng host WPF riêng, rồi truyền -TargetProcessId.' }
        Save-Viewport 'Gallery-current-viewport'; $manifest.status='completed'; return
    }
    if ($IncludeLogin) {
        if ($process.MainWindowTitle -notlike '*Đăng nhập*') { throw 'IncludeLogin yêu cầu cửa sổ Login đang mở.' }
        Save-Viewport 'Login-current-viewport'; $manifest.status='completed'; return
    }
    if ($process.MainWindowTitle -like '*Đăng nhập*') { throw 'Đăng nhập UI mẫu thủ công trước khi chụp shell; script không nhập mật khẩu.' }
    $screens = @{
        1=@('','Tổng quan'); 2=@('Chuyên môn','Khám sàng lọc'); 3=@('Tiếp nhận','Hồ sơ khách hàng')
        4=@('Danh mục & kho','Danh mục'); 5=@('Danh mục & kho','Lô vắc xin & kho'); 6=@('Chuyên môn','Tiêm & theo dõi')
        7=@('Tiếp nhận','Đăng ký dịch vụ'); 8=@('Tiếp nhận','Thanh toán'); 9=@('Quản trị','Lịch hẹn & lịch sử')
        10=@('Danh mục & kho','Ưu đãi'); 11=@('Quản trị','Báo cáo'); 12=@('Quản trị','Nhân viên & tài khoản')
    }
    foreach ($id in ($ScreenIds | Select-Object -Unique)) {
        if ($id -in 13,14) {
            Invoke-Button 'Mở menu tài khoản'
            if ($id -eq 14) { Invoke-Button 'Đổi mật khẩu' }
        } else {
            $screen = $screens[$id]
            # The function search avoids drawer scrolling and hidden duplicate button names.
            Invoke-Button 'Tìm chức năng'
            $condition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Tên hoặc mã chức năng')
            $search = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,$condition)
            if (-not $search) { throw 'Không tìm được ô tìm chức năng.' }
            ([System.Windows.Automation.ValuePattern]$search.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)).SetValue(('A{0:00}' -f $id))
            Start-Sleep -Milliseconds 200
            # Locate the exact screen ID text inside a search result, then its button ancestor.
            $idCondition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,('A{0:00}' -f $id))
            $idText = @($root.FindAll([System.Windows.Automation.TreeScope]::Descendants,$idCondition)) |
                Where-Object { -not $_.Current.IsOffscreen } | Select-Object -First 1
            $match = $idText
            for ($level=0; $match -and $match.Current.ControlType -ne [System.Windows.Automation.ControlType]::Button -and $level -lt 8; $level++) {
                $match = [System.Windows.Automation.TreeWalker]::ControlViewWalker.GetParent($match)
            }
            if (-not $match -or $match.Current.ControlType -ne [System.Windows.Automation.ControlType]::Button) { throw "Không tìm được kết quả A$id." }
            ([System.Windows.Automation.InvokePattern]$match.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)).Invoke()
            Start-Sleep -Milliseconds 300
        }
        Save-Viewport ('A{0:00}-current-viewport' -f $id)
        if ($id -eq 13) { Invoke-Button 'Đóng menu tài khoản' }
    }
    $manifest.status = 'completed'
} catch {
    $manifest.status = 'failed'; $manifest.error = $_.Exception.Message; throw
} finally {
    if ($oldDpiContext -and $oldDpiContext -ne [IntPtr]::Zero) { [void][UiReviewNative]::SetThreadDpiAwarenessContext($oldDpiContext) }
    $manifest.finishedUtc = [DateTime]::UtcNow.ToString('o')
    $manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $run 'manifest.json') -Encoding UTF8
    Write-Output "Evidence: $run"
}
