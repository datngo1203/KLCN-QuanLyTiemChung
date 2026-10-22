# Windows UI A03/A07/A08 — ui/a03-a07-a08

Nhánh tách từ `main` tại `e76cce1` (phase3/A01/A02 đã merge). File này chỉ ghi tiến độ UI của ba màn; `docs/status.md` do người dùng tổng hợp.

## A03 — Hồ sơ khách hàng và người được tiêm

- Hai tab, tìm tên/CCCD/SĐT, chọn khách hàng bảo trợ, thêm người tiêm và nhãn trẻ dưới 14 tuổi chưa có CCCD. Form kiểm tra CCCD 12 số, SĐT 10–11 số, email, ngày sinh không vượt hôm nay; báo CCCD trùng trong dữ liệu mẫu. Không tìm thấy có nút tạo hồ sơ mới.
- `IProfilesService`: `GetCustomers`, `GetRecipients`, `NationalIdExists`, `SaveCustomer`, `SaveRecipient`; `SampleProfilesService` có cờ `UseSampleData`. DTO UI: `CustomerRecord`, `RecipientRecord`. TODO nối API thật và kiểm tra trùng ở server. `contracts/README.md` chỉ liệt kê GET/POST customers/recipients và PUT recipients dự kiến; chưa có contract tìm kiếm, sửa khách hàng, lỗi trùng hoặc quy tắc bảo trợ được duyệt.
- Build: `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release` — thành công, 0 warning/error. Harness WPF STA tạm ngoài repo render trực tiếp `ProfilesView` 1400×900 DIP; ảnh thường/lỗi/rỗng tại `docs/ui-review/ui-a03-a07-a08/a03-*.png`, mỗi ảnh dưới 500 KB.
- Chưa kiểm chứng: API/auth/quyền thật, lưu bền, thao tác bàn phím toàn phần, DPI khác, quy tắc bảo trợ tại backend. Lưu và kiểm tra CCCD hiện chỉ thay đổi bộ nhớ mẫu.

## Tồn đọng nền phase3 đã merge

PR #22 còn góp ý review về startup tạo thêm login, A02 compact, điều hướng nhóm Tổng quan, số liệu A01 theo ngày và badge nguồn A02. Các file này ngoài phạm vi A03/A07/A08 của nhánh này.
