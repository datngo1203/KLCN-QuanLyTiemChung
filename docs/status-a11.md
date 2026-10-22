# A11 Báo cáo — 2026-10-22

## Đối chiếu Screen Spec

- Bộ lọc `TuNgay`/`DenNgay` giữ điều kiện `TuNgay ≤ DenNgay ≤ hôm nay`; khi sai hiển thị lỗi, ẩn kết quả cũ và khóa tạo/xuất.
- Khi đổi một trong hai ngày, snapshot hiện tại được xóa và phải tạo báo cáo lại trước khi xem/xuất; tránh dùng kết quả của khoảng ngày trước.
- Có doanh thu, tỷ lệ phản ứng, số mũi theo loại vắc xin ở bảng và biểu đồ cột, cùng bảng lô sắp hết hạn.
- Trạng thái không có dữ liệu hiện gợi ý chọn khoảng khác. Excel/PDF/CSV chỉ bật khi snapshot có dữ liệu; nút hiện chỉ phản hồi TODO, chưa tạo tệp.
- `IReportService`/`SampleReportService` và snapshot riêng cung cấp dữ liệu fixture; `UseSampleData=false` trả snapshot rỗng và nhãn Chưa có nguồn dữ liệu.
- `contracts/` chưa có API báo cáo đã triển khai; công thức doanh thu/tỷ lệ, phạm vi dữ liệu và tiêu chí “sắp hết hạn” cần được API owner xác nhận, không suy ra từ client. Số liệu hiện tại chỉ là dữ liệu mẫu.

## Kiểm chứng

- Build: `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release` — thành công, 0 warning, 0 error.
- Ảnh dưới 500 KB trong `docs/ui-review/ui-login-a06-a09-a11-a12-a14/`: [bình thường](ui-review/ui-login-a06-a09-a11-a12-a14/a11-normal.png), [khoảng ngày sai](ui-review/ui-login-a06-a09-a11-a12-a14/a11-error.png), [không có dữ liệu](ui-review/ui-login-a06-a09-a11-a12-a14/a11-empty.png). Đã xem ảnh sau khi nạp Theme.
- Chưa kiểm chứng API, số liệu thật, công thức nghiệp vụ, xuất tệp thật, quyền phiên hoặc DPI khác.
