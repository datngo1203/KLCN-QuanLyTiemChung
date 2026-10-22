# A12 Tài khoản & phân quyền — 2026-10-22

## Đối chiếu Screen Spec

- Có ba tab Nhân viên/Tài khoản/Quyền bổ sung; nhân viên có đúng một chức danh dropdown. Form nhân viên kiểm CCCD 12 chữ số và tên đăng nhập tối thiểu 4 ký tự, không dấu/không khoảng trắng.
- Tài khoản có khóa/mở khóa, đặt lại mật khẩu và badge Đã khóa; tài khoản hiện tại không thể tự khóa, tự đặt lại mật khẩu hoặc tự gán quyền bổ sung.
- Danh sách mã quyền bổ sung lấy từ Screen Spec. Nhân viên, tài khoản và quyền hiện là fixture; các thao tác chỉ mô phỏng và chưa gọi dịch vụ Identity/API.
- `IAccountService`/`SampleAccountService` cô lập dữ liệu mẫu. Khi tắt mẫu, danh sách rỗng; không lưu mật khẩu.

## Kiểm chứng

- Build: `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release` — thành công, 0 warning, 0 error.
- Ảnh các trạng thái bình thường, lỗi CCCD/tên đăng nhập, quyền bổ sung và rỗng: ảnh lưu cục bộ (`docs/ui-review/`); đã xem ảnh bình thường sau khi chỉnh độ rộng cột.
- Chưa kiểm chứng backend, quyền phiên thực, lưu trữ/đồng bộ nhân sự hoặc DPI khác.
