# Cài đặt và chạy

Phạm vi hiện hành: [SCOPE.md](SCOPE.md). Cách chạy chính là Compose với SQL Server thật; chế độ danh mục demo được ghi riêng bên dưới.

## Công cụ và dependencies

- Git, Node.js 24 LTS, .NET 10 SDK; Windows để chạy WPF.
- Docker Desktop chạy Linux containers cho SQL Server 2022 Developer và API.
- Flutter/Android SDK chỉ cần khi chủ động làm Mobile sau S8.

```powershell
./scripts/bootstrap.ps1
# Chỉ khi cần Mobile: ./scripts/bootstrap.ps1 -IncludeMobile
```

Bootstrap restore .NET tools, backend, Windows và Web. Không commit node_modules/bin/obj hoặc cấu hình bí mật.

## Compose — SQL/API thật (S0)

```powershell
# Chỉ tạo file nếu chưa có; không ghi đè password của volume đang dùng.
if (-not (Test-Path infra/.env)) { Copy-Item infra/.env.example infra/.env }
# Điền MSSQL_SA_PASSWORD synthetic đủ mạnh trong infra/.env local.
docker compose --env-file infra/.env -p klcn-s0 -f infra/compose.yaml up -d --build
Invoke-RestMethod http://127.0.0.1:5080/api/v1/catalog/vaccines
```

Compose chờ SQL tại `127.0.0.1:14333` healthy, chạy service `migrate`, rồi mở API tại `http://127.0.0.1:5080` với `Catalog:UseDemoData=false`. Catalog phải có `source=database`; DB mới có thể rỗng, không tự seed tài khoản/nghiệp vụ. Health `/health` chỉ kiểm tiến trình; `/openapi/v1.json` có trong Development.

Giữ cùng project `klcn-s0` và password để dùng lại volume. Chạy lại migration không xóa dữ liệu:

```powershell
docker compose --env-file infra/.env -p klcn-s0 -f infra/compose.yaml run --rm migrate
# Dừng và giữ DB; không thêm -v.
docker compose --env-file infra/.env -p klcn-s0 -f infra/compose.yaml down
```

Chạy suite backend với `M1_SQL_TEST_CONNECTION` lấy từ credential local, server `tcp:127.0.0.1,14333` để dùng TCP IPv4 tường minh. Suite tạo DB test `KLCN_M1_Test_<GUID>` riêng; không đưa connection string có password vào log/Git.

```powershell
# Đặt M1_SQL_TEST_CONNECTION trong phiên shell bằng credential local trước lệnh này.
dotnet test backend/TiemChung.slnx --configuration Release
```

## Web

```powershell
$env:VITE_API_BASE_URL='http://127.0.0.1:5080'
npm --prefix apps/web run dev
```

Mở origin `http://localhost:5173` do CORS backend hiện cho phép origin này; địa chỉ API/SQL vẫn dùng `127.0.0.1`. Đây là ngoại lệ origin trình duyệt, không đổi CORS trong lượt dọn dẹp. Nút Kiểm tra API gọi backend; form còn dùng state cục bộ, chưa login backend thật. Biến VITE_* chỉ chứa giá trị công khai.

## Windows

```powershell
$env:TIEMCHUNG_API_BASE_URL='http://127.0.0.1:5080'
dotnet run --project apps/windows/TiemChung.Desktop.csproj
```

Login hiện dùng SampleAuthService, chưa xác thực backend. Nút Kiểm tra API chỉ kiểm kết nối; không chứng minh client đã lưu dữ liệu SQL. Backend đã có một số endpoint Identity/Profiles, xem [contracts](../contracts/README.md).

## Chế độ danh mục demo — tách khỏi S0

```powershell
# Không chạy đồng thời với Compose API trên cùng cổng 5080.
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:Catalog__UseDemoData='true'
dotnet run --project backend/src/TiemChung.Api --no-launch-profile --urls http://127.0.0.1:5080
```

Chế độ này chỉ cho danh mục mẫu `source=demo` và health chạy không cần DB; các route Identity/Profiles vẫn cần SQL/cấu hình tương ứng. Không dùng làm bằng chứng S0. API chặn demo ngoài Development. Kết thúc phiên demo cần bỏ override:

```powershell
Remove-Item Env:Catalog__UseDemoData -ErrorAction SilentlyContinue
```

## Mobile — đóng băng

Chỉ khi làm tiếp sau S8: restore với `-IncludeMobile`, dùng Android emulator với `--dart-define=API_BASE_URL=http://10.0.2.2:5080`. Emulator dùng 10.0.2.2 để tới host; iOS build cần macOS/Xcode. Không cần Flutter để chạy Web/Windows/S0.

Nếu cổng đang dùng, chỉ dừng tiến trình mình đã mở hoặc đổi cổng và base URL/CORS tương ứng; không dừng tiến trình không rõ chủ. Xem [database/README.md](../database/README.md) khi cần migration thủ công; không reset/xóa DB để chạy demo.
