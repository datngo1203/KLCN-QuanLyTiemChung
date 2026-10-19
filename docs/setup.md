# Cài đặt và chạy

## Công cụ

- Git; Node.js 24 LTS; .NET 10 SDK (đã dùng 10.0.401 để kiểm tra).
- Windows để chạy WPF. VS Code cho web/API/mobile; Visual Studio có workload .NET desktop nếu dùng designer WPF.
- Flutter 3.47.7 stable + Dart đi kèm; Android Studio/Android SDK và emulator cho Android. iOS build cần macOS + Xcode.
- SQL Server 2022 Developer/Express hoặc Docker khi bắt đầu làm dữ liệu thật. Chạy demo API không cần database.

## Khôi phục dependencies

```powershell
dotnet tool restore
dotnet restore backend/TiemChung.slnx
dotnet restore apps/windows/TiemChung.Desktop.csproj
npm --prefix apps/web ci
cd apps/mobile
flutter pub get
flutter doctor
```

`scripts/bootstrap.ps1` làm các bước restore; báo rõ khi chưa có Flutter. Không commit node_modules/bin/obj/.dart_tool/build hoặc cấu hình bí mật.

## API

```powershell
dotnet run --project backend/src/TiemChung.Api
```

`http://localhost:5080/health`, `/api/v1/catalog/vaccines`, `/openapi/v1.json`. Development dùng danh mục hư cấu và không ghi database. Nếu chạy trong Docker hoặc bỏ launch profile, cần đặt `ASPNETCORE_ENVIRONMENT=Development` rõ ràng để dùng demo. API chặn demo ở Production.

## Web

```powershell
Copy-Item apps/web/.env.example apps/web/.env.local
npm --prefix apps/web run dev
```

URL mặc định 5173, `.env.local` có `VITE_API_BASE_URL`. Chỉ đưa URL công khai vào VITE_*; mọi biến Vite đều có thể xuất hiện trong bundle trình duyệt. Nút Kiểm tra API gọi backend; các form còn cục bộ.

## Windows

```powershell
$env:TIEMCHUNG_API_BASE_URL='http://localhost:5080'
dotnet run --project apps/windows/TiemChung.Desktop.csproj
```

UI đang dùng login mô phỏng admin/123. Nút Kiểm tra API thử kết nối thật; không phát token hoặc cho phép ghi SQL. Mỗi module sẽ thay mock bằng API sau khi có kiểm tra quyền.

## Mobile

```powershell
cd apps/mobile
flutter devices
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080
```

Android emulator dùng 10.0.2.2 để tới máy Windows host. Debug manifest cho HTTP chỉ ở debug. Điện thoại thật: cùng mạng, chạy API với `--urls http://0.0.0.0:5080`, đặt API_BASE_URL thành IP LAN của máy và mở đúng cổng firewall. Không commit IP/mật khẩu riêng. iOS simulator dùng `http://localhost:5080`; dùng HTTPS cho bản phân phối. Chưa có cấu hình ký phát hành, push notification hoặc thanh toán thật.

## SQL Server

Xem database/README.md cho migration. Nếu dùng Docker:

```powershell
Copy-Item infra/.env.example infra/.env
# Điền MSSQL_SA_PASSWORD đủ mạnh trong file local này
docker compose --env-file infra/.env -f infra/compose.yaml up -d --build
```

Compose khởi động SQL tại localhost:14333 và API demo tại localhost:5080. Không tự chạy migration hoặc seed tài khoản. Chuyển API sang DB bằng ConnectionStrings:TiemChung và Catalog:UseDemoData=false sau khi apply migration. `.env` được ignore.

Nếu cổng 5080/5173 đang dùng: dừng tiến trình mình đã mở hoặc đổi cổng và cập nhật base URL/CORS; không dừng tiến trình không rõ chủ. Không cần triển khai cloud để bắt đầu code.
