# UI Windows phase 3 — 22/10/2026

## Phạm vi và cách mở rộng

- Shell có registry 13 màn A01–A12/A14 trong `apps/windows/ViewsV2/NavigationRegistry.cs`; A01 là nhóm một mục nên không hiện cấp 2. A09 đặt trong Quản trị theo nhóm người dùng gợi ý, A10 đặt trong Danh mục & kho để giữ đủ 13 màn; các trang chưa làm hiển thị tên màn.
- Để đăng ký trang tiếp theo: thêm `NavigationItem` vào `Groups` nếu là màn mới, rồi thay nhánh tương ứng của `CreatePage` bằng View và DataContext của ViewModel mới. Không cần sửa `ViewsV2/MainWindow.xaml` hoặc code-behind. A13 thông tin phiên ở menu tài khoản, ngoài 13 màn của đợt này.
- Quyền menu và `CanXxx` mới là điểm nối mặc định true. Chưa có dữ liệu quyền phiên, chưa lọc menu theo ma trận; API phải kiểm tra quyền thật khi triển khai.

## A02 — Khám sàng lọc

- Đối chiếu Screen Spec mục 3: có hàng đợi chọn/badge/trạng thái rỗng, thông tin người tiêm và tuổi cạnh ngày sinh, bốn chỉ số sinh tồn với lỗi cạnh ô, dị ứng/bệnh nền, kết luận ba lựa chọn, lý do và chỉ định theo kết luận, ghi chú, thanh Lưu/Hủy cố định. Mạch 40–200; nhiệt độ 35–42; tâm thu > tâm trương. Nhiệt độ ≥38 chỉ gợi ý Tạm hoãn. Khi kết luận khác Đủ điều kiện, danh sách chỉ định bị xóa và khóa; lý do bắt buộc. Dị ứng nguy hiểm do người dùng tự đánh dấu, cảnh báo Safety và xác nhận hai bước trước khi nút Lưu có thể bật.
- `IScreeningService.GetQueue`, `GetVaccineCatalog`, cờ `UseSampleData`; `SampleScreeningService` cấp dữ liệu placeholder. Khi tắt Sample mà API chưa có, hàng đợi và danh mục rỗng, nhãn nguồn là Chưa có nguồn dữ liệu. DTO cần từ API: lượt khám gắn đúng đăng ký/người tiêm/khách hàng, dữ liệu sinh tồn/tiền sử, danh mục vắc xin và khả dụng, yêu cầu ghi phiếu khám kèm chỉ định nguyên tử. `contracts/` chưa có hợp đồng đã triển khai cho các thao tác này; không tự đặt route.
- Chưa kiểm chứng: nghiệp vụ lưu, xác thực bác sĩ/quyền, dữ liệu thật, phát hiện dị ứng nguy hiểm tự động, thao tác bàn phím toàn phần và DPI khác. Nút Lưu chỉ phản hồi bản nháp UI, không ghi API.

## A01 — Tổng quan

- Đối chiếu Screen Spec mục 2: bộ lọc Từ ngày/Đến ngày có lỗi khoảng ngày, trạng thái rỗng, hai tab cùng hệ style, thẻ luồng hover/mũi tên và `ChuyenHuongNhanhCommand`, bảng trạng thái badge/nút Mở, tóm tắt 2×2 và thẻ Kết nối API. `TiemChungApiClient.IsHealthyAsync` gọi `/health` thật; kết quả này chỉ xác nhận endpoint health.
- `IDashboardService.GetWorkItems`, `GetStages`, `GetMetrics`, cờ `UseSampleData`; `SampleDashboardService` cấp dữ liệu placeholder. Khi tắt Sample mà API chưa có, các tập dữ liệu rỗng, nhãn nguồn là Chưa có nguồn dữ liệu. DTO cần từ API: các số tổng hợp theo khoảng ngày, bốn nhóm xử lý, danh sách lịch hẹn/công việc và trạng thái; `contracts/` hiện chưa có hợp đồng dashboard tổng hợp. Không suy ra công thức tiền hoặc số lượt từ client.
- Chưa kiểm chứng: chỉ số/số tiền thực, tổng hợp API, phân quyền thẻ chuyển hướng theo màn đích, trạng thái tải dữ liệu thật, thao tác bàn phím toàn phần và DPI khác.

## Kiểm chứng và ảnh

- Lệnh: `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release` sau rebase: 0 warning, 0 error. Build `--no-restore` đầu tiên thiếu `project.assets.json` do worktree mới; chạy restore qua build thường đã giải quyết.
- Harness WPF STA tạm trong `.capture/` (không đưa vào commit) khởi tạo App/theme và shell tại 1440×900 DIP, render `RenderTargetBitmap` trạng thái thường/lỗi/rỗng cho A01 và thường/lỗi/Tạm hoãn/rỗng cho A02. Bảy ảnh PNG tại `docs/ui-review/ui-phase3/`, mỗi ảnh dưới 500 KB. Đã xem ảnh render; đây là fixture Sample, chưa phải thao tác end-to-end hay kiểm chứng backend.
- Các trang A03–A12/A14 là placeholder. Login kế thừa UI mô phỏng. Chưa kiểm tra CI của PR trước khi mở.
