param(
    [string]$OutputDirectory = ".review",
    [int[]]$ScreenIds = @(1,2,3,4,5,6,7,8,9,10,11,12,14),
    [switch]$IncludeLogin,
    [switch]$CaptureGallery,
    [switch]$CapturePhase3States,
    [switch]$Inspect
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName WindowsBase
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class UiReviewNative {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint flags);
    [DllImport("user32.dll")] public static extern bool ScreenToClient(IntPtr h, ref POINT p);
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
}
'@

$script:OutputPath = Join-Path (Get-Location) $OutputDirectory
New-Item -ItemType Directory -Force $script:OutputPath | Out-Null
$script:AllScreens = @(
    @{Id=1; Group=''; Name='Tổng quan'},
    @{Id=2; Group='Chuyên môn'; Name='Khám sàng lọc'},
    @{Id=3; Group='Tiếp nhận'; Name='Hồ sơ khách hàng'},
    @{Id=4; Group='Danh mục & kho'; Name='Danh mục'},
    @{Id=5; Group='Danh mục & kho'; Name='Lô vắc xin & kho'},
    @{Id=6; Group='Chuyên môn'; Name='Tiêm & theo dõi'},
    @{Id=7; Group='Tiếp nhận'; Name='Đăng ký dịch vụ'},
    @{Id=8; Group='Tiếp nhận'; Name='Thanh toán'},
    @{Id=9; Group='Quản trị'; Name='Lịch hẹn & lịch sử'},
    @{Id=10; Group='Danh mục & kho'; Name='Ưu đãi'},
    @{Id=11; Group='Quản trị'; Name='Báo cáo'},
    @{Id=12; Group='Quản trị'; Name='Nhân viên & tài khoản'},
    @{Id=14; Group='Quản trị'; Name='Đổi mật khẩu'}
)

function Get-AppRoot {
    $proc = Get-Process TiemChung.Desktop -ErrorAction Stop | Where-Object MainWindowHandle | Select-Object -First 1
    if (-not $proc) { throw 'Không tìm thấy cửa sổ TiemChung.Desktop đang chạy.' }
    [pscustomobject]@{ Process=$proc; Root=[System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle) }
}

function Get-Descendants($Root) {
    $Root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
}

function Find-Button($Root, [string]$Name) {
    $condition = [System.Windows.Automation.AndCondition]::new(
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button),
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, $Name))
    $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Find-Control($Root, [System.Windows.Automation.ControlType]$ControlType, [string]$Name) {
    $condition = [System.Windows.Automation.AndCondition]::new(
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty, $ControlType),
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, $Name))
    $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Invoke-Control($Root, [System.Windows.Automation.ControlType]$ControlType, [string]$Name) {
    $element = Find-Control $Root $ControlType $Name
    if (-not $element) { throw "Không tìm thấy điều khiển '$Name'." }
    $pattern = $null
    if ($element.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pattern)) {
        ([System.Windows.Automation.InvokePattern]$pattern).Invoke()
    } elseif ($element.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$pattern)) {
        ([System.Windows.Automation.SelectionItemPattern]$pattern).Select()
    } elseif ($element.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$pattern)) {
        ([System.Windows.Automation.TogglePattern]$pattern).Toggle()
    } else { Click-Element $Root $element }
    Start-Sleep -Milliseconds 350
}

function Toggle-Checkbox($Root, [string]$Name) {
    $checkbox = Find-Control $Root ([System.Windows.Automation.ControlType]::CheckBox) $Name
    if (-not $checkbox) { throw "Không tìm thấy ô chọn '$Name'." }
    $pattern = [System.Windows.Automation.TogglePattern]$checkbox.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    if ($pattern.Current.ToggleState -ne [System.Windows.Automation.ToggleState]::On) { $pattern.Toggle() }
    Start-Sleep -Milliseconds 350
}

function Move-MouseTo($Element) {
    $point = $Element.GetClickablePoint()
    [void][UiReviewNative]::SetCursorPos([int]$point.X, [int]$point.Y)
    Start-Sleep -Milliseconds 300
}

function Invoke-Button($Root, [string]$Name) {
    $button = Find-Button $Root $Name
    if (-not $button) { throw "Không tìm thấy nút '$Name'." }
    if (-not $button.Current.IsEnabled) { throw "Nút '$Name' đang bị khóa." }
    ([System.Windows.Automation.InvokePattern]$button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)).Invoke()
    Start-Sleep -Milliseconds 500
}

function Set-EditValue($Root, [string]$Name, [string]$Value) {
    $condition = [System.Windows.Automation.AndCondition]::new(
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit),
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, $Name))
    $edit = $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
    if (-not $edit) { throw "Không tìm thấy ô '$Name'." }
    ([System.Windows.Automation.ValuePattern]$edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)).SetValue($Value)
    Start-Sleep -Milliseconds 250
}

function Click-Element($Root, $Element) {
    $point = $Element.GetClickablePoint()
    $buttons = $Root.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button))
    $candidates = @()
    foreach ($button in $buttons) {
        $bounds = $button.Current.BoundingRectangle
        if ($button.Current.IsEnabled -and $point.X -ge $bounds.X -and $point.X -le $bounds.Right -and $point.Y -ge $bounds.Y -and $point.Y -le $bounds.Bottom) {
            $candidates += [pscustomobject]@{ Element=$button; Area=$bounds.Width * $bounds.Height }
        }
    }
    foreach ($candidate in ($candidates | Sort-Object Area)) {
        $invoke = $null
        if ($candidate.Element.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$invoke)) {
            ([System.Windows.Automation.InvokePattern]$invoke).Invoke()
            Start-Sleep -Milliseconds 400
            return
        }
    }
    $handle = [IntPtr]$Root.Current.NativeWindowHandle
    $clientPoint = [UiReviewNative+POINT]::new()
    $clientPoint.X = [int]$point.X; $clientPoint.Y = [int]$point.Y
    [void][UiReviewNative]::ScreenToClient($handle, [ref]$clientPoint)
    $packed = [IntPtr]([int64](($clientPoint.Y -band 0xffff) -shl 16) -bor ($clientPoint.X -band 0xffff))
    [void][UiReviewNative]::SendMessage($handle, 0x0200, [IntPtr]::Zero, $packed)
    [void][UiReviewNative]::SendMessage($handle, 0x0201, [IntPtr]1, $packed)
    [void][UiReviewNative]::SendMessage($handle, 0x0202, [IntPtr]::Zero, $packed)
    Start-Sleep -Milliseconds 400
}

function Send-MouseWheel($Root, [int]$Delta) {
    $rect = [UiReviewNative+RECT]::new()
    [void][UiReviewNative]::GetWindowRect([IntPtr]$Root.Current.NativeWindowHandle, [ref]$rect)
    $x = $rect.Left + [int](($rect.Right - $rect.Left) * .72)
    $y = $rect.Top + [int](($rect.Bottom - $rect.Top) * .72)
    $packedPoint = [IntPtr]([int64](($y -band 0xffff) -shl 16) -bor ($x -band 0xffff))
    $wheel = [IntPtr]([int64](($Delta -band 0xffff) -shl 16))
    [void][UiReviewNative]::SendMessage([IntPtr]$Root.Current.NativeWindowHandle, 0x020A, $wheel, $packedPoint)
    Start-Sleep -Milliseconds 300
}

function Save-WindowCapture($Root, [string]$FileName, [int]$Width, [int]$Height) {
    $handle = $Root.Current.NativeWindowHandle
    $rect = [UiReviewNative+RECT]::new()
    [void][UiReviewNative]::GetWindowRect([IntPtr]$handle, [ref]$rect)
    # Window frame metrics vary by theme; the PNG output is normalized to the requested review dimensions.
    $frameHeight = $rect.Bottom - $rect.Top
    $requestedFrameHeight = [Math]::Max(1, $Height - [Math]::Max(0, $frameHeight - [int]$Root.Current.BoundingRectangle.Height))
    [void][UiReviewNative]::SetWindowPos([IntPtr]$handle, [IntPtr]::Zero, $rect.Left, $rect.Top, $Width, $requestedFrameHeight, 0x0040)
    Start-Sleep -Milliseconds 350
    [void][UiReviewNative]::GetWindowRect([IntPtr]$handle, [ref]$rect)
    $sourceWidth = $rect.Right - $rect.Left
    $sourceHeight = $rect.Bottom - $rect.Top
    $bitmap = [System.Drawing.Bitmap]::new($sourceWidth, $sourceHeight)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $dc = $graphics.GetHdc()
    try {
        if (-not [UiReviewNative]::PrintWindow([IntPtr]$handle, $dc, 2)) { throw 'PrintWindow không chụp được cửa sổ.' }
    }
    finally { $graphics.ReleaseHdc($dc); $graphics.Dispose() }
    $normalized = [System.Drawing.Bitmap]::new($Width, $Height)
    $g2 = [System.Drawing.Graphics]::FromImage($normalized)
    $g2.DrawImage($bitmap, [System.Drawing.Rectangle]::new(0,0,$Width,$Height), [System.Drawing.Rectangle]::new(0,0,[Math]::Min($Width,$sourceWidth),[Math]::Min($Height,$sourceHeight)), [System.Drawing.GraphicsUnit]::Pixel)
    $path = Join-Path $script:OutputPath $FileName
    $normalized.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g2.Dispose(); $normalized.Dispose(); $bitmap.Dispose()
    $item = Get-Item $path
    if ($item.Length -ge 500KB) { throw "Ảnh vượt 500KB: $($item.Name) ($($item.Length) bytes)." }
    [pscustomobject]@{File=$item.Name; Width=$Width; Height=$Height; Bytes=$item.Length}
}

function Save-PageSlices($Root, [string]$BaseName, [int]$Width, [int]$Height) {
    $scrollTarget = $null
    $scrollBounds = $null
    foreach ($element in (Get-Descendants $Root)) {
        if ($element.Current.ControlType -eq [System.Windows.Automation.ControlType]::ScrollBar -and $element.Current.BoundingRectangle.Height -gt $element.Current.BoundingRectangle.Width) {
            $pattern = $null
            if ($element.TryGetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern, [ref]$pattern)) {
                $range = [System.Windows.Automation.RangeValuePattern]$pattern
                if ($range.Current.Maximum -gt $range.Current.Minimum) { $scrollTarget = $range; $scrollBounds = $element.Current.BoundingRectangle; break }
            }
        }
    }
    if (-not $scrollTarget) { return Save-WindowCapture $Root $BaseName $Width $Height }
    $minimum = $scrollTarget.Current.Minimum
    $maximum = $scrollTarget.Current.Maximum
    $range = [int][Math]::Ceiling($maximum - $minimum)
    $windowRect = [UiReviewNative+RECT]::new()
    [void][UiReviewNative]::GetWindowRect([IntPtr]$Root.Current.NativeWindowHandle, [ref]$windowRect)
    $scrollTop = [int][Math]::Round($scrollBounds.Y - $windowRect.Top)
    $viewport = [int][Math]::Round($scrollBounds.Height)
    $contentBottom = [Math]::Min($Height, $scrollTop + $viewport)
    $footerHeight = $Height - $contentBottom
    $step = [Math]::Max(1, [int][Math]::Floor($viewport * .82))
    $positions = [System.Collections.Generic.List[double]]::new()
    $positions.Add($minimum)
    $position = $minimum
    while ($position -lt $maximum) { $position = [Math]::Min($maximum, $position + $step); if ($position -gt $positions[$positions.Count - 1]) { $positions.Add($position) } }
    $tempFiles = [System.Collections.Generic.List[string]]::new()
    try {
        for ($index=0; $index -lt $positions.Count; $index++) {
            $scrollTarget.SetValue($positions[$index])
            Start-Sleep -Milliseconds 220
            $tempName = ".stitch-$([guid]::NewGuid().ToString('N')).png"
            [void](Save-WindowCapture $Root $tempName $Width $Height)
            $tempFiles.Add((Join-Path $script:OutputPath $tempName))
        }
        $canvasHeight = $Height + $range
        $canvas = [System.Drawing.Bitmap]::new($Width, $canvasHeight)
        $graphics = [System.Drawing.Graphics]::FromImage($canvas)
        $destinationY = 0
        $first = [System.Drawing.Bitmap]::new($tempFiles[0])
        $graphics.DrawImage($first, [System.Drawing.Rectangle]::new(0,0,$Width,$contentBottom), [System.Drawing.Rectangle]::new(0,0,$Width,$contentBottom), [System.Drawing.GraphicsUnit]::Pixel)
        $destinationY = $contentBottom
        $first.Dispose()
        for ($index=1; $index -lt $positions.Count; $index++) {
            $delta = [int][Math]::Round($positions[$index] - $positions[$index - 1])
            $sourceY = $scrollTop + $viewport - $delta
            $part = [System.Drawing.Bitmap]::new($tempFiles[$index])
            $graphics.DrawImage($part, [System.Drawing.Rectangle]::new(0,$destinationY,$Width,$delta), [System.Drawing.Rectangle]::new(0,$sourceY,$Width,$delta), [System.Drawing.GraphicsUnit]::Pixel)
            $destinationY += $delta
            $part.Dispose()
        }
        $last = [System.Drawing.Bitmap]::new($tempFiles[$tempFiles.Count - 1])
        if ($footerHeight -gt 0) {
            $graphics.DrawImage($last, [System.Drawing.Rectangle]::new(0,$destinationY,$Width,$footerHeight), [System.Drawing.Rectangle]::new(0,$contentBottom,$Width,$footerHeight), [System.Drawing.GraphicsUnit]::Pixel)
        }
        $last.Dispose(); $graphics.Dispose()
        $path = Join-Path $script:OutputPath $BaseName
        $canvas.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        $canvas.Dispose()
        $item = Get-Item $path
        if ($item.Length -ge 500KB) { throw "Ảnh toàn trang vượt 500KB: $($item.Name) ($($item.Length) bytes)." }
        $stem = [System.IO.Path]::GetFileNameWithoutExtension($BaseName)
        Remove-Item -LiteralPath (Join-Path $script:OutputPath "$stem-top.png") -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath (Join-Path $script:OutputPath "$stem-bottom.png") -Force -ErrorAction SilentlyContinue
        return [pscustomobject]@{File=$item.Name; Width=$Width; Height=$canvasHeight; Bytes=$item.Length}
    }
    finally {
        $scrollTarget.SetValue($minimum)
        foreach ($tempFile in $tempFiles) { Remove-Item -LiteralPath $tempFile -Force -ErrorAction SilentlyContinue }
    }
}

function Ensure-Shell {
    $p = Get-Process TiemChung.Desktop -ErrorAction Stop | Select-Object -First 1
    if ($p.MainWindowTitle -like '*Đăng nhập*') {
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
        Set-EditValue $root 'Tên đăng nhập' 'reviewer'
        Set-EditValue $root 'Mật khẩu' 'review-only'
        Invoke-Button $root 'Đăng nhập'
        Start-Sleep -Seconds 1
    }
}

function Select-Screen($Root, $Screen) {
    $screenButton = Find-Button $Root $Screen.Name
    if (-not $screenButton) {
        $drawer = Find-Button $Root 'Mở danh sách chức năng'
        if ($drawer) { Invoke-Button $Root 'Mở danh sách chức năng' }
        else { if ($Screen.Group -and (Find-Button $Root $Screen.Group)) { Invoke-Button $Root $Screen.Group } }
    } elseif ($Screen.Group -and (Find-Button $Root $Screen.Group)) {
        Invoke-Button $Root $Screen.Group
    }
    Start-Sleep -Milliseconds 200
    if (-not (Find-Button $Root $Screen.Name)) {
        if (Find-Button $Root 'Mở danh sách chức năng') { Invoke-Button $Root 'Mở danh sách chức năng' }
    }
    Invoke-Button $Root $Screen.Name
    Start-Sleep -Milliseconds 600
}

if ($IncludeLogin) {
    $p = Get-Process TiemChung.Desktop -ErrorAction Stop | Select-Object -First 1
    if ($p.MainWindowTitle -notlike '*Đăng nhập*') { throw 'Để chụp Login, cần mở lại ứng dụng ở màn đăng nhập.' }
    $root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
    Save-WindowCapture $root 'Login-binh-thuong-1366.png' 1366 768
    Set-EditValue $root 'Tên đăng nhập' 'invalid'
    Set-EditValue $root 'Mật khẩu' 'review-only'
    Invoke-Button $root 'Đăng nhập'
    Start-Sleep -Seconds 1
    Save-WindowCapture $root 'Login-loi-1366.png' 1366 768
    Set-EditValue $root 'Tên đăng nhập' 'locked'
    Invoke-Button $root 'Đăng nhập'
    Start-Sleep -Seconds 1
    Save-WindowCapture $root 'Login-bi-khoa-1366.png' 1366 768
}

Ensure-Shell
$root = (Get-AppRoot).Root
if ($Inspect) {
    foreach ($element in (Get-Descendants $root)) {
        $type = $element.Current.ControlType.ProgrammaticName
        $name = $element.Current.Name
        if ($name -or $element.Current.IsScrollPatternAvailable) {
            Write-Output "$type`t$name`tScroll=$($element.Current.IsScrollPatternAvailable)"
        }
    }
    exit
}
if ($CaptureGallery) {
    throw 'Gallery cần được mở bằng host WPF riêng; script hiện chưa tự khởi tạo gallery.'
}

# Capture the currently available grouped navigation. At high display scaling
# the shell uses its compact drawer instead of the horizontal two-level menu.
$navButton = Find-Button $root 'Mở danh sách chức năng'
if ($navButton) {
    Invoke-Button $root 'Mở danh sách chức năng'
    Save-WindowCapture $root 'Menu-nhom-cap-2-1366.png' 1366 768 | Out-Null
    Save-WindowCapture $root 'Menu-nhom-mot-muc-1366.png' 1366 768 | Out-Null
} else {
    Invoke-Button $root 'Tiếp nhận'
    Save-WindowCapture $root 'Menu-nhom-cap-2-1366.png' 1366 768 | Out-Null
    if (Find-Button $root 'Tổng quan') {
        Invoke-Button $root 'Tổng quan'
        Save-WindowCapture $root 'Menu-nhom-mot-muc-1366.png' 1366 768 | Out-Null
    }
}

$results = [System.Collections.Generic.List[object]]::new()
foreach ($screen in $script:AllScreens | Where-Object { $ScreenIds -contains $_.Id }) {
    Select-Screen $root $screen
    $id = 'A{0:d2}' -f $screen.Id
    if ($screen.Id -eq 2) {
        $queueItem = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
            [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, 'Người được tiêm 01'))
        if ($queueItem) { Click-Element $root $queueItem }
    }
    foreach ($capture in (Save-PageSlices $root "$id-binh-thuong-1366.png" 1366 768)) { $results.Add($capture) }
    if ($screen.Id -eq 2) {
        foreach ($capture in (Save-PageSlices $root 'A02-binh-thuong-980.png' 980 620)) { $results.Add($capture) }
        # Return to the standard size before navigating further.
        $restoreName = ".restore-$([guid]::NewGuid().ToString('N')).png"
        [void](Save-WindowCapture $root $restoreName 1366 768)
        Remove-Item -LiteralPath (Join-Path $script:OutputPath $restoreName) -Force
    }
}
$root = (Get-AppRoot).Root
if ($CapturePhase3States) {
    $overview = $script:AllScreens | Where-Object Id -eq 1 | Select-Object -First 1
    Select-Screen $root $overview
    Set-EditValue $root 'Ngày bắt đầu' '23/10/2026'
    Set-EditValue $root 'Ngày kết thúc' '22/10/2026'
    foreach ($capture in (Save-PageSlices $root 'A01-loi-nhap-1366.png' 1366 768)) { $results.Add($capture) }
    Set-EditValue $root 'Ngày bắt đầu' '01/01/2099'
    Set-EditValue $root 'Ngày kết thúc' '01/01/2099'
    foreach ($capture in (Save-PageSlices $root 'A01-rong-1366.png' 1366 768)) { $results.Add($capture) }
    Set-EditValue $root 'Ngày bắt đầu' '22/10/2026'
    Set-EditValue $root 'Ngày kết thúc' '22/10/2026'
    $hoverText = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, 'Chờ khám'))
    if ($hoverText) { Move-MouseTo $hoverText; foreach ($capture in (Save-PageSlices $root 'A01-hover-the-luong-1366.png' 1366 768)) { $results.Add($capture) } }

    $screening = $script:AllScreens | Where-Object Id -eq 2 | Select-Object -First 1
    Select-Screen $root $screening
    $queueItem = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, 'Người được tiêm 01'))
    if ($queueItem) { Click-Element $root $queueItem }
    Set-EditValue $root 'Mạch' '30'
    Set-EditValue $root 'Nhiệt độ' '43'
    Set-EditValue $root 'Huyết áp tâm thu' '70'
    Set-EditValue $root 'Huyết áp tâm trương' '90'
    foreach ($capture in (Save-PageSlices $root 'A02-loi-nhap-1366.png' 1366 768)) { $results.Add($capture) }

    Invoke-Button $root 'Hủy thay đổi'
    Set-EditValue $root 'Tìm lượt hoặc người được tiêm' 'khong-co-ket-qua'
    foreach ($capture in (Save-PageSlices $root 'A02-rong-1366.png' 1366 768)) { $results.Add($capture) }
    Set-EditValue $root 'Tìm lượt hoặc người được tiêm' ''
    $queueItem = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, 'Người được tiêm 01'))
    if ($queueItem) { Click-Element $root $queueItem }

    Set-EditValue $root 'Mạch' '72'
    Set-EditValue $root 'Nhiệt độ' '38'
    Set-EditValue $root 'Huyết áp tâm thu' '110'
    Set-EditValue $root 'Huyết áp tâm trương' '70'
    Set-EditValue $root 'Tiền sử dị ứng bắt buộc' 'Dữ liệu giả lập: tiền sử minh họa'
    Invoke-Control $root ([System.Windows.Automation.ControlType]::RadioButton) 'Tạm hoãn'
    foreach ($capture in (Save-PageSlices $root 'A02-tam-hoan-1366.png' 1366 768)) { $results.Add($capture) }

    Invoke-Button $root 'Hủy thay đổi'
    Set-EditValue $root 'Tiền sử dị ứng bắt buộc' 'Dữ liệu giả lập: thành phần X'
    Toggle-Checkbox $root 'Dị ứng nguy hiểm cần xác nhận'
    foreach ($capture in (Save-PageSlices $root 'A02-di-ung-xac-nhan-1-1366.png' 1366 768)) { $results.Add($capture) }
    Invoke-Button $root 'Xác nhận đã đọc cảnh báo'
    foreach ($capture in (Save-PageSlices $root 'A02-di-ung-xac-nhan-2-1366.png' 1366 768)) { $results.Add($capture) }
    Invoke-Button $root 'Hủy thay đổi'
}
$results | Format-Table -AutoSize
