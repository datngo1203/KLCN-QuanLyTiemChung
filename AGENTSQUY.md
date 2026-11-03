**Phân công 02/11/2026:** Quý sở hữu toàn bộ S7 (thống kê/báo cáo). Đọc README.md, AGENTS.md, SCOPE.md và backend/contracts/README.md trên nhánh main. Không dùng develop hay tài liệu status/Issue cũ để tự mở rộng phạm vi.

## Ranh giới độc lập

Quý phát triển trọn lát dọc **SQL truy vấn chỉ đọc → ASP.NET Core API → Windows A11 → test**. Có thể bắt đầu ngay, không đợi Đạt nghiệm thu S5 hoặc Bảo triển khai S6. Không sửa Registration/Payment/Vaccination/Clinical, S5/S6, Identity, migrations hoặc database schema.

UI Windows hiện có ở apps/windows/ViewModels/ReportViewModel.cs, apps/windows/Services/IReportService.cs, apps/windows/ViewsV2/ReportView.xaml và SampleReportService.cs, hiện chỉ là Sample. Thay bằng API/SQL thật, không coi dữ liệu Sample hay CI là nghiệm thu. Tỷ lệ phản ứng, lô sắp hạn, xuất Excel/PDF/CSV trong màn mẫu không thuộc ba báo cáo S7.

## Ba lát kết quả

- **S7.1: Doanh thu theo tháng** từ số tiền trong giao dịch ThanhToan mô phỏng G3 đã ghi thành công; nhóm theo thời gian giao dịch tại cơ sở Việt Nam UTC+7; không nhân tiền khi join với nhiều chi tiết đăng ký. Gắn nhãn doanh thu mô phỏng, không nói là tiền ngân hàng.

- **S7.2: Số mũi tiêm theo vắc xin**: đọc MuiTiem đã ghi thành công và danh mục vắc xin; không đếm đăng ký/lịch/chỉ định là đã tiêm; lọc thời gian theo thời điểm thực hiện.

- **S7.3: Số đăng ký theo trạng thái**: từ DangKyDichVu và trạng thái thực tế có trong DB, nhóm/lọc thời gian đăng ký, không tạo trạng thái mới cho biểu đồ.

Không chờ S6: doanh thu là SUM giao dịch đã lưu; khi có ưu đãi, số tiền đã ghi sau giảm sẽ là nguồn của report, không tính lại từ catalog/ưu đãi. Không tạo migration, bảng tổng hợp, scheduler hoặc export theo scope hiện tại.

## Ranh giới file và kiểm thử

- [ ] Mỗi lát có API, permission kiểm backend, DTO hợp lý, query hiệu quả trên SQL thật, test normal/empty/401/403/giá VND lớn/biên tháng UTC+7.

- [ ] Windows A11 gọi Identity/API thật, thể hiện loading/empty/error, giữ focus và filter ổn định, không dùng Sample để demo kết quả.

- [ ] Kiểm số liệu bằng SQL fixture có đăng ký nhiều dòng + 1 giao dịch, nhiều mũi/vắc xin, chênh lệch UTC gần đầu/cuối tháng; không double-count.

- [ ] Chỉ sửa Report files và endpoint/contracts mới; shared Program.cs, ShellViewModel.cs, NavigationRegistry.cs, backend/contracts/README.md chỉnh tối thiểu, để Đạt review; không sửa S5/S6.

- [ ] Một lát = một branch từ main sạch + PR + test + review. Không tự merge, không cập nhật tài liệu tiến độ riêng.

## Prompt Codex bàn giao — chạy đầu tiên trong clone riêng

~~~text

Bạn là Codex phụ trách S7 thống kê/báo cáo (Quý) của KLCN090. Trước tiên đọc README.md, AGENTS.md, SCOPE.md, backend/contracts/README.md và GitHub Issue #12 trên main. Kiểm tra git branch/status, bảo toàn dữ liệu local. Khảo sát ReportViewModel, ReportView, IReportService, SampleReportService, Shell/Navigation, DB Entity và API hiện có của ThanhToan, MuiTiem, DangKyDichVu. Xác nhận role/permission báo cáo hiện hữu thay vì tự đặt mã quyền.

Chỉ lập kế hoạch S7.1, S7.2, S7.3 theo SCOPE: doanh thu mô phỏng từ giao dịch đã ghi (không SUM trên join nhiều dòng), số mũi đã thực hiện, đăng ký theo trạng thái có thật; filter thời gian UTC+7, SQL read-only, không đợi S6. Đề xuất API contract, DTO, file riêng/chung, test SQL/API/WPF và thứ tự PR nhỏ. Không sửa S5/S6/Identity, không thêm migration/schema/export, không commit/push/migrate/reset/seed DB trong lượt khảo sát. Báo kế hoạch và dừng; chỉ triển khai S7.1 khi được giao.

~~~