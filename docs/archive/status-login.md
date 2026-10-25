# Login WPF — 2026-10-22

## Đối chiếu Screen Spec

- Cửa sổ `LoginWindow` riêng; `App.xaml.cs` hiển thị trước `ViewsV2.MainWindow`. Sau đăng nhập thành công, điều hướng phase3 tiếp tục vào A01; đăng xuất từ shell mở lại cửa sổ Login.
- Có `TenDangNhap`, `MatKhau`, chỉ báo đang xử lý, `LoginCommand` và `ThoatCommand`. Tên đăng nhập tối thiểu 3 ký tự, không có khoảng trắng đầu/cuối; mật khẩu bắt buộc. Khi xử lý, hai ô và nút đăng nhập bị khóa.
- `IAuthService` và `SampleAuthService` tách khỏi ViewModel; không còn thông tin đăng nhập cố định trong code-behind. Service mẫu mô phỏng sai thông tin, tài khoản bị khóa, lỗi mạng và thành công; dữ liệu này không xác thực danh tính thật.
- README gốc và Windows ghi rõ backend chưa có xác thực. `SampleAuthService` có TODO thay bằng dịch vụ Identity API; không hash mật khẩu tại client.

## Kiểm chứng

- Build: `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release` — thành công, 0 warning, 0 error.
- Ảnh WPF `RenderTargetBitmap` từ LoginWindow với resource Theme hiện tại; các ảnh dưới 500 KB: [rỗng](../ui-review/ui-login-a06-a09-a11-a12-a14/login-empty.png), [bình thường](../ui-review/ui-login-a06-a09-a11-a12-a14/login-normal.png), [sai thông tin](../ui-review/ui-login-a06-a09-a11-a12-a14/login-error.png), [tài khoản bị khóa](../ui-review/ui-login-a06-a09-a11-a12-a14/login-locked.png), [lỗi mạng](../ui-review/ui-login-a06-a09-a11-a12-a14/login-network-error.png).
- Các chế độ lỗi là fixture nội bộ của `SampleAuthService`, không gọi backend. Chưa kiểm chứng xác thực/API thật, DPI hoặc đăng nhập end-to-end với dữ liệu thật.
