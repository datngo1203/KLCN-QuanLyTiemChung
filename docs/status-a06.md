# A06 Tiêm & theo dõi — 2026-10-22

## Đối chiếu Screen Spec

- Hàng chờ chỉ nhận lượt có chỉ định, kết luận đủ điều kiện và đã thanh toán. Xác nhận tiêm bị khóa đến khi chọn lượt, chọn lô, nhập vị trí; danh sách lô lọc đúng vắc xin, hạn dùng từ hôm nay trở đi và tồn lớn hơn 0.
- Xác nhận chuyển sang theo dõi bằng đồng hồ 30 phút. Form gồm tình trạng, mức độ và hướng xử trí; mức Nặng/Cấp cứu hiện `CE.Alert.Safety` và bắt buộc đánh dấu đã báo bác sĩ trực.
- Hoàn tất theo dõi cần đủ ba trường. Nếu chưa hết 30 phút, lệnh hiển thị cảnh báo và yêu cầu xác nhận cho về sớm. Đây là xác nhận giao diện, chưa gọi quy trình xử trí hoặc ghi dữ liệu y tế.
- `IVaccinationService`/`SampleVaccinationService` cung cấp queue, lô hợp lệ, ghi nhận tiêm và ghi nhận theo dõi. Cờ `UseSampleData` tắt trả trạng thái không có nguồn; dữ liệu là fixture, không chứa hồ sơ thật.
- Đối chiếu `contracts/`: chưa có contract đã triển khai cho queue tiêm, xác nhận mũi, trừ tồn, hoặc theo dõi sau tiêm. DTO/endpoint cần được API owner xác định; API phải kiểm tra lại quyền, kết luận, thanh toán, hạn/tồn và loại vắc xin khi ghi.

## Kiểm chứng

- Build: `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release` — thành công, 0 warning, 0 error.
- Ảnh WPF ở `docs/ui-review/ui-login-a06-a09-a11-a12-a14/`, tất cả dưới 500 KB: [bình thường](ui-review/ui-login-a06-a09-a11-a12-a14/a06-normal.png), [cảnh báo cho về sớm](ui-review/ui-login-a06-a09-a11-a12-a14/a06-error.png), [nguồn dữ liệu rỗng](ui-review/ui-login-a06-a09-a11-a12-a14/a06-empty.png), [phản ứng cấp cứu](ui-review/ui-login-a06-a09-a11-a12-a14/a06-safety.png).
- Chụp `RenderTargetBitmap` từ View với Theme hiện hành và `SampleVaccinationService`; đã xem các ảnh. Chưa kiểm chứng API, quyền phiên thật, dữ liệu kho/tiêm thật, xử trí y tế, thao tác toàn bàn phím hoặc DPI khác.
