# Trạng thái Themes — 2026-10-22

Nhánh `ui/themes` từ `main` sau `git fetch origin`, `git switch main`, `git pull --ff-only`. Phạm vi code: `apps/windows/Themes/` và `apps/windows/App.xaml` để nạp resource. `_Gallery.xaml` là cửa sổ thử độc lập, không có mục trong menu chính. Không sửa màn hình, ViewModel, service, API, contract hoặc schema.

## Đã làm

- Token Clinical Emerald, chữ và các kiểu ô nhập, lựa chọn, nút, icon, bảng, danh sách, badge, alert, menu hai cấp, tab, card, segmented button và toast.
- `TextBox` dùng `Tag` làm placeholder; biểu mẫu dùng `Validation.ErrorTemplate` để hiện nội dung lỗi bên dưới khi binding tạo lỗi. Các điều khiển có style vô hiệu, hover và focus; trạng thái lỗi có viền đỏ nhạt.
- Gallery có dữ liệu minh họa giả, các trạng thái chính và ảnh chụp [phần trên](ui-review/ui-themes/gallery.png), [phần dưới](ui-review/ui-themes/gallery-bottom.png). Không đưa gallery vào menu.

## Kiểm chứng

- `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release`: **đạt, 0 warning, 0 error**.
- Nạp gallery bằng WPF `XamlReader` với resource từ các tệp theme, render bằng `RenderTargetBitmap`, xem lại hai PNG. Kích thước ảnh dưới 500 KB/tệp.
- `git diff --check`: đạt.

## Chưa kiểm chứng / giới hạn

- Chưa chạy quy trình nghiệp vụ ở các màn hiện có, chưa kiểm tra DPI/kích thước nhỏ hoặc thao tác bằng bàn phím với tất cả component.
- Gallery cho thấy chú thích lỗi tĩnh; chưa có ViewModel/binding phát sinh lỗi thật. `Validation.ErrorTemplate` cần được xác nhận khi nối từng màn.
- Resource định danh `CE.*` được cung cấp để màn áp dụng dần; không thay đổi style mặc định của màn hiện tại ngoài việc nạp dictionary.
- Màu nút Primary `#047857` với chữ trắng có tỷ lệ tương phản theo Design Spec khoảng 5,5:1; chưa đo render tự động các trạng thái hover/pressed/disabled.
