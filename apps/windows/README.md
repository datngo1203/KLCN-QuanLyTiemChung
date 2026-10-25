# Windows — Đạt phụ trách nền giao diện

C# WPF .NET 10 + WPF UI 4.3.0. Login và Đổi mật khẩu dùng `IdentityApiService` gọi Identity API thật; Đăng xuất thu hồi phiên tại API. Token chỉ giữ trong bộ nhớ. Cấu hình `TIEMCHUNG_API_BASE_URL` và seed Admin Development theo [setup](../../docs/setup.md); không có credential cố định trong UI. Các màn nghiệp vụ mẫu vẫn giữ nhãn mẫu.

```powershell
dotnet run --project TiemChung.Desktop.csproj
dotnet build TiemChung.Desktop.csproj
```

Bảo nối hồ sơ/đăng ký/thanh toán/ưu đãi; Quý nối lịch/theo dõi; Đạt nối danh mục/tài khoản/báo cáo theo SCOPE. Tách feature thành file riêng khi cùng sửa MainWindow.xaml.cs để tránh xung đột. Không thêm SqlConnection vào WPF.
