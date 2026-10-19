# Windows — Đạt phụ trách nền giao diện

C# WPF .NET 10 + WPF UI 4.3.0. Kế thừa UI A01–A14 và login mô phỏng admin/123. Services/TiemChungApiClient.cs và nút Kiểm tra API ở sidebar gọi backend thật.

```powershell
dotnet run --project TiemChung.Desktop.csproj
dotnet build TiemChung.Desktop.csproj
```

Bảo nối hồ sơ/đăng ký/thanh toán/ưu đãi; Quý nối lịch/theo dõi; Đạt nối khám/kho/danh mục/tài khoản/báo cáo. Tách feature thành file riêng khi cùng sửa MainWindow.xaml.cs để tránh xung đột. Không thêm SqlConnection vào WPF. Bỏ login demo khi backend Identity hoàn tất.
