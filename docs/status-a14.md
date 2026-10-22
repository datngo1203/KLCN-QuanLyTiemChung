# A14 Đổi mật khẩu — 2026-10-22

## Đối chiếu Screen Spec

- Có ba ô mật khẩu dạng che ký tự và nút Hủy bỏ; nút đổi mật khẩu chỉ bật khi đủ ba ô, mật khẩu mới trùng xác nhận, khác mật khẩu hiện tại và dài tối thiểu 6 ký tự.
- Lỗi mật khẩu hiện tại không chính xác được hiển thị từ `SampleChangePasswordService`; trường hợp mẫu dùng giá trị hiện tại `sai` để trình diễn lỗi.
- `IChangePasswordService` chỉ phục vụ UI mẫu. Không lưu mật khẩu; phản hồi thành công ghi rõ backend chưa có xác thực/đổi mật khẩu thật. Backend phải xác minh và hash mật khẩu ở phía server.
- A14 dùng route/menu phase3 đã có; không đổi điều hướng hoặc startup.

## Kiểm chứng

- Build: `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release` — thành công, 0 warning, 0 error.
- Ảnh dưới 500 KB tại `docs/ui-review/ui-login-a06-a09-a11-a12-a14/`: [bình thường](ui-review/ui-login-a06-a09-a11-a12-a14/a14-normal.png), [mật khẩu hiện tại sai](ui-review/ui-login-a06-a09-a11-a12-a14/a14-error.png), [rỗng](ui-review/ui-login-a06-a09-a11-a12-a14/a14-empty.png). Đã xem ảnh lỗi sau khi nạp Theme.
- Chưa kiểm chứng xác thực/đổi mật khẩu qua API, lưu phiên thật hoặc DPI khác.
