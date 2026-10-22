# Khắc phục review PR #22 — Windows phase3

Nhánh `ui/phase3-review-fixes`, tách từ `main` sau khi PR #23 đã merge.

## Các sửa đổi

- Khởi động: bỏ `StartupUri="MainWindow.xaml"`; `App.OnStartup` là luồng duy nhất mở login mô phỏng rồi tạo shell V2.
- A02 responsive: khi hẹp chuyển toàn bộ vùng kết luận và chỉ định xuống hàng dưới form khám; không ẩn cột cha của các điều khiển đó.
- Điều hướng: chọn nhóm một màn như “Tổng quan” điều hướng luôn đến màn đơn của nhóm.
- A01 theo khoảng ngày: `IDashboardService.GetSnapshot(from,to)` trả danh sách, thẻ luồng và chỉ số trong cùng snapshot; mỗi lần đổi ngày/làm mới tải lại cả ba. Ngày ngoài phạm vi fixture hiện có hiển thị bảng rỗng, số lượng 0, doanh thu 0 đ và thông báo không có dữ liệu.
- A02 nhãn nguồn: badge bind `ScreeningViewModel.DataSourceLabel`.

## Kiểm tra

- `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release` — pass, 0 warning/error.
- Harness WPF STA tạm ngoài repo có 6 kiểm tra liên quan và đều PASS: `StartupUri` không đặt; A01 ngày không có dữ liệu xóa các hàng/thẻ cũ và hiện số 0; chọn lại hôm nay nạp snapshot; nhóm đơn điều hướng về A01; A02 có nhãn không có nguồn khi service tắt dữ liệu; bố cục A02 hẹp giữ vùng kết luận/chỉ định có chiều rộng hiển thị.
- Ảnh kiểm tra trực quan 980×1050 DIP cho A02 compact ở `docs/ui-review/ui-phase3-review-fixes/a02-compact-form.png`; A02 no-source ở `a02-no-source.png`; A01 khoảng ngày rỗng ở `a01-empty-period.png`.
- Chưa kiểm tra thao tác đăng nhập/đăng xuất thủ công trên máy người dùng, DPI 125/150%, nhiều màn hình hoặc backend thật. Nguồn dữ liệu dashboard/sàng lọc vẫn là Sample.
