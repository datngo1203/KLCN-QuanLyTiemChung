# Gói housekeeping thay thế #27 — 23/10/2026

Owner Windows/capture: Đạt; Bảo hoặc Quý review độc lập. Nhánh từ develop `f188084`, độc lập code UI trong [#29](https://github.com/datngo1203/KLCN-QuanLyTiemChung/pull/29). Giữ #27 Draft, không tích hợp cả gói cũ và thay thế.

## Phạm vi cuối

- Ignore chỉ `/.review/`, `/.capture/`, `/.tmp/`. Không ignore PNG toàn repo hoặc `docs/ui-review/`. Kiểm Android launcher, web/public, docs/diagrams và ảnh review mới không bị ignore; capture local bị ignore.
- Giữ đúng 56 ảnh lịch sử và các status/link ảnh cũ từ develop. Không lấy commit xóa ảnh, không chuyển bằng chứng cũ sang local hoặc sửa bảy status để làm mất link.
- Viết lại script capture đã có ở #27 theo phạm vi viewport: mỗi lần có thư mục GUID mới, không ghi đè/xóa ảnh, không ghép/crop/normalize. Kích thước đầu vào là pixel vật lý; manifest ghi kích thước thực, DPI cửa sổ, DIP suy ra, SHA checkout, tình trạng dirty, hash script/executable/ảnh và lỗi nếu dừng giữa chừng. SHA checkout không tự chứng minh binary đang chạy được build từ SHA đó.
- A13/A14 qua menu tài khoản; màn khác qua tìm mã chức năng, không phụ thuộc nhóm cũ hoặc cuộn drawer. Script không tự nhập mật khẩu. Gallery hỗ trợ viewport host đã mở với `-CaptureGallery -TargetProcessId`; không tự tạo host. `-IncludeLogin` chỉ chụp trạng thái Login đang mở rồi kết thúc. Các trạng thái error/empty cần fixture và lần chụp riêng; bỏ switch tự chạy trạng thái phase3 cũ có tên control sai.
- Quy trình vẫn yêu cầu ảnh đã tuyển chọn/manifest mà reviewer truy cập được trong `docs/ui-review/` hoặc attachment PR. Local `.review/` chỉ là staging, không là bàn giao.

## Kiểm tra

PowerShell parser đạt; `-ValidateOnly` chạy hai lần tạo hai thư mục/manifest độc lập; kiểm `git check-ignore --no-index` chỉ capture local được ignore; `git diff --check` sạch. So sánh tree/blob ảnh lịch sử với develop không có thay đổi. Không sửa source app nên Windows build/54 checks của #29 không được quy thành QA script.

Lệnh từ gốc repo (PowerShell 7):

```powershell
./scripts/ui-capture/Capture-UiReview.ps1 -ValidateOnly
# Build/chạy app phù hợp, đăng nhập UI mẫu thủ công trước:
./scripts/ui-capture/Capture-UiReview.ps1 -ScreenIds 2,12,13,14
# Cửa sổ hẹp; pixel thực có thể khác do min size/DPI, xem manifest:
./scripts/ui-capture/Capture-UiReview.ps1 -ScreenIds 12,13,14 -WidthPixels 980 -HeightPixels 620
```

Chưa kiểm chứng capture native/PrintWindow, UI Automation điều hướng trên app đang chạy, Gallery host, DPI 100/125/150%, nhiều màn hình, mọi trạng thái lỗi/rỗng. A13/A14 cần UI #29; trên develop nền cũ có thể dừng với manifest failed, không báo hoàn tất. Reviewer cần chạy script trên binary build từ head UI đã ghi, kiểm ảnh không đen và đúng màn; parser/ValidateOnly không thay được các kiểm tra này.
