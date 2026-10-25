# Windows UI A03/A07/A08 — ui/a03-a07-a08

Nhánh tách từ `main` tại `e76cce1` (phase3/A01/A02 đã merge). File này chỉ ghi tiến độ UI của ba màn; `docs/status.md` do người dùng tổng hợp.

## A03 — Hồ sơ khách hàng và người được tiêm

- Hai tab, tìm tên/CCCD/SĐT, chọn khách hàng bảo trợ, thêm người tiêm và nhãn trẻ dưới 14 tuổi chưa có CCCD. Form kiểm tra CCCD 12 số, SĐT 10–11 số, email, ngày sinh không vượt hôm nay; báo CCCD trùng trong dữ liệu mẫu. Không tìm thấy có nút tạo hồ sơ mới.
- `IProfilesService`: `GetCustomers`, `GetRecipients`, `NationalIdExists`, `SaveCustomer`, `SaveRecipient`; `SampleProfilesService` có cờ `UseSampleData`. DTO UI: `CustomerRecord`, `RecipientRecord`. TODO nối API thật và kiểm tra trùng ở server. `contracts/README.md` chỉ liệt kê GET/POST customers/recipients và PUT recipients dự kiến; chưa có contract tìm kiếm, sửa khách hàng, lỗi trùng hoặc quy tắc bảo trợ được duyệt.
- Build: `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release` — thành công, 0 warning/error. Harness WPF STA tạm ngoài repo render trực tiếp `ProfilesView` 1400×900 DIP; ảnh thường/lỗi/rỗng và tab người được tiêm tại `docs/ui-review/ui-a03-a07-a08/a03-*.png`, mỗi ảnh dưới 500 KB.
- Chưa kiểm chứng: API/auth/quyền thật, lưu bền, thao tác bàn phím toàn phần, DPI khác, quy tắc bảo trợ tại backend. Lưu và kiểm tra CCCD hiện chỉ thay đổi bộ nhớ mẫu.

## A07 — Đăng ký dịch vụ

- Chọn khách hàng/người tiêm cùng hồ sơ, hình thức, ngày giờ dự kiến; thêm/xóa dòng vắc xin lẻ và gói, số lượng dương, giá không âm, chốt giá mẫu theo dòng. Tổng tạm tính cộng các dòng. Chặn tạo khi không có dịch vụ hoặc thời điểm trong quá khứ. Vắc xin tồn 0 có gợi ý và lệnh `ThemVaoHangDoiCommand`; danh sách đăng ký có mã và badge trạng thái.
- `IRegistrationService`: `GetCustomers`, `GetRecipients`, `GetCatalog`, `GetRegistrations`, `SaveRegistration`, `AddToWaitlist`; `SampleRegistrationService` có cờ `UseSampleData`. DTO UI: `RegistrationCatalogItem`, `RegistrationLine`, `RegistrationRecord`. `contracts/README.md` chỉ có GET/POST registrations và POST items dự kiến; thiếu contract danh mục kèm tồn theo tác vụ, hàng đợi, snapshot giá, khuyến mãi và trạng thái. Không tự đặt route hoặc tính mức giảm khuyến mãi; lựa chọn mẫu được ghi nhận nhưng tổng chưa trừ ưu đãi.
- Build Release 0 warning/error. Harness WPF STA tạm ngoài repo render `RegistrationView` 1400×900 DIP; ảnh thường/lỗi/rỗng và vắc xin tạm hết tại `docs/ui-review/ui-a03-a07-a08/a07-*.png`, mỗi ảnh dưới 500 KB. Ảnh lỗi dùng ngày dự kiến trong quá khứ và danh sách dịch vụ rỗng.
- Chưa kiểm chứng API/quyền/tồn thật, lưu bền hoặc công thức ưu đãi; các lệnh chỉ thay đổi dữ liệu mẫu trong bộ nhớ. Phải kiểm tra lại tồn và giá tại API khi tích hợp.

## A08 — Thu ngân và thanh toán

- Tìm phiếu, xem tổng phải thu/đã thu/còn lại và lịch sử nhiều giao dịch. Thu theo số tiền, phương thức, loại đặt cọc/trả góp/toàn bộ và trạng thái. Kiểm tra số tiền > 0, không vượt nợ ngay dưới ô; chỉ giao dịch thành công giảm nợ. Phiếu trả đủ có badge `Đã hoàn tất` và khóa form. `InBienLaiCommand` chỉ bật khi chọn giao dịch thành công; hiện phản hồi biên lai mẫu, chưa in/xuất hóa đơn thật.
- `IPaymentService`: `GetInvoices`, `AddPayment`; `SamplePaymentService` có cờ `UseSampleData`. DTO UI: `PaymentInvoice`, `PaymentRecord`. `contracts/README.md` nêu POST registrations/{id}/payments và GET payments dự kiến; thiếu contract tra phiếu theo phạm vi, lịch sử, trạng thái, idempotency và in biên lai. Chưa tự đặt route. Việc giữ lô sau đặt cọc vẫn cần backend và chính sách được duyệt.
- Build Release 0 warning/error. Harness WPF STA ngoài repo render `PaymentView` 1400×900 DIP; ảnh thường/lỗi/rỗng và đã trả đủ tại `docs/ui-review/ui-a03-a07-a08/a08-*.png`, mỗi ảnh dưới 500 KB. Ảnh lỗi nhập 450.000đ khi còn 400.000đ; ảnh đã trả đủ xác nhận form khóa.
- Chưa kiểm chứng API/auth/quyền, thanh toán thật, in hóa đơn thật, transaction/idempotency hoặc đồng bộ công nợ với A07. Service mẫu giữ dữ liệu trong bộ nhớ riêng của màn.

## Kiểm tra biên ViewModel

Harness tạm ngoài repo chạy 12 kiểm tra PASS: A03 CCCD trùng/hợp lệ, CCCD người tiêm bỏ trống và tag trẻ phụ thuộc, ngày sinh tương lai; A07 không có dịch vụ, hết hàng/hàng đợi, kết hợp lẻ+gói và tổng snapshot, ngày quá khứ; A08 giao dịch thất bại không trừ nợ, thu vượt nợ, in biên lai theo trạng thái và khóa form khi trả đủ. Harness này không thay thế kiểm tra API hoặc thao tác end-to-end trên máy người dùng.

## Tồn đọng nền phase3 đã merge

PR #22 còn góp ý review về startup tạo thêm login, A02 compact, điều hướng nhóm Tổng quan, số liệu A01 theo ngày và badge nguồn A02. Các file này ngoài phạm vi A03/A07/A08 của nhánh này.
