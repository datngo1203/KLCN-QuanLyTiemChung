# A09 Lịch & nhắc lịch — 2026-10-22

## Đối chiếu Screen Spec

- Danh sách hiển thị giờ hẹn, địa điểm, trạng thái và badge đỏ “Quá hạn” cho lịch “Đã xếp lịch” đã qua thời điểm hẹn. Trạng thái “Đã đến”/“Hủy hẹn” dùng badge ngữ nghĩa riêng.
- Tạo/đổi lịch yêu cầu người được tiêm, địa điểm, `ThoiGianHen > DateTime.Now` và giờ trong 07:30–17:30; đổi lịch chỉ bật khi lịch “Đã xếp lịch” chưa diễn ra.
- Lịch sử giữ kênh SMS/Zalo/Email/Gọi điện, thời điểm và trạng thái gửi. “Đã nhắc gần đây” xuất hiện khi có lần gửi thành công trong 24 giờ. Gửi nhắc chỉ bật khi lịch còn trạng thái “Đã xếp lịch” và chưa quá hạn.
- `IAppointmentService`/`SampleAppointmentService` cung cấp danh sách, tạo/đổi lịch và ghi fixture lịch sử. Khi tắt Sample, danh sách/nguồn người tiêm rỗng; không giữ nội dung người tiêm mẫu trong form.
- `contracts/` chưa có contract đã triển khai để tạo/đổi lịch hoặc gửi nhắc. Service mẫu không gửi SMS/Zalo/Email/cuộc gọi thật; cần API owner xác định contract và kiểm tra trạng thái/quyền khi tích hợp.

## Kiểm chứng

- Build: `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release` — thành công, 0 warning, 0 error.
- Ảnh dưới 500 KB tại `docs/ui-review/ui-login-a06-a09-a11-a12-a14/`: [bình thường](ui-review/ui-login-a06-a09-a11-a12-a14/a09-normal.png), [giờ ngoài khung](ui-review/ui-login-a06-a09-a11-a12-a14/a09-error.png), [rỗng/không có nguồn](ui-review/ui-login-a06-a09-a11-a12-a14/a09-empty.png). Đã xem ảnh sau khi nạp theme.
- Chưa kiểm chứng API, thông báo thật, quyền phiên, thao tác toàn bàn phím hoặc DPI khác.
