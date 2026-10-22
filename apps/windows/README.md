# Windows — Đạt phụ trách nền giao diện

C# WPF .NET 10 + WPF UI 4.3.0. A01/A02 và các màn mới dùng ViewModels/Services riêng; Login hiện chạy qua `IAuthService` với `SampleAuthService` cho giao diện mẫu. Backend hiện chưa có xác thực, không có tài khoản/mật khẩu đăng nhập cố định trong UI. `TiemChungApiClient` và nút Kiểm tra API ở sidebar chỉ gọi các endpoint backend hiện có.

```powershell
dotnet run --project TiemChung.Desktop.csproj
dotnet build TiemChung.Desktop.csproj
```

Bảo nối hồ sơ/đăng ký/thanh toán/ưu đãi; Quý nối lịch/theo dõi; Đạt nối khám/kho/danh mục/tài khoản/báo cáo. Tách feature thành file riêng khi cùng sửa MainWindow.xaml.cs để tránh xung đột. Không thêm SqlConnection vào WPF. Bỏ login demo khi backend Identity hoàn tất.
