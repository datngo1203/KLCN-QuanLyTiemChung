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
# S1: bổ sung một lần nếu chưa có (giữ nguyên key đã có).
# Hai key là Base64 của 32 byte ngẫu nhiên độc lập; thư mục mail phải ở ngoài Git.
$mailDirectory = Join-Path $env:USERPROFILE 'Documents/Codex/KLCN090-Coordination/S1/mail'
New-Item -ItemType Directory -Force $mailDirectory | Out-Null
# Điền 3 biến trong infra/.env, không in key/credential ra log:
# IDENTITY_SIGNING_KEY=<Base64 key 32 bytes>
# IDENTITY_MAIL_ENCRYPTION_KEY=<Base64 key 32 bytes>
# IDENTITY_MAIL_DIRECTORY=C:/Users/<user>/Documents/Codex/KLCN090-Coordination/S1/mail
# Sinh từng key bằng [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32)).
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

Mở origin `http://localhost:5173` do CORS backend cho phép origin này; địa chỉ API/SQL vẫn dùng `127.0.0.1`. Nút Đăng nhập mở đăng nhập/đăng ký tài khoản khách hàng qua API thật. Token chỉ giữ trong bộ nhớ, tải lại trang cần đăng nhập lại. Biến VITE_* chỉ chứa giá trị công khai. Các form nghiệp vụ ngoài S1 còn dùng state mẫu.

## Windows

```powershell
$env:TIEMCHUNG_API_BASE_URL='http://127.0.0.1:5080'
dotnet run --project apps/windows/TiemChung.Desktop.csproj
```

Login nhân viên, Đổi mật khẩu và Đăng xuất gọi API thật; access/refresh token chỉ giữ trong bộ nhớ. Đổi mật khẩu thành công thu hồi mọi phiên, mở lại Login. Nếu logout lỗi mạng, thử lại để API thu hồi phiên. Các màn nghiệp vụ mẫu không chứng minh đã lưu SQL. Xem [contracts](../contracts/README.md).

## S1 — Admin đầu tiên và đăng ký khách hàng demo

Sau migration, chạy seed Development dưới đây. Lấy credential synthetic từ sổ local ngoài Git `C:/Users/DAT/Documents/Codex/KLCN090-Coordination/TEST-ACCOUNTS.local.md`, hoặc chọn và ghi sổ trước khi seed. Không đưa username/password vào repo, PR hay log công khai. Không cần CLI/ACL M1; không dùng bộ m1br*.

```powershell
# Đặt DevelopmentSeed__Username và DevelopmentSeed__Password trong môi trường shell
# bằng credential synthetic local (password 12–128 ký tự; username 3–100, không @).
docker compose --env-file infra/.env -p klcn-s0 -f infra/compose.yaml run --rm -e DevelopmentSeed__Username -e DevelopmentSeed__Password api --seed-admin
Remove-Item Env:DevelopmentSeed__Username,Env:DevelopmentSeed__Password -ErrorAction SilentlyContinue
```

Seed chỉ chạy ở `Development`, tạo nhân viên Admin/Technical, hash bằng backend, cấp 12 quyền đã có và audit trong cùng transaction. Nếu DB đã có Admin thì không đổi account, hash hoặc quyền. Không reset dữ liệu hay chạy recovery. Có thể chạy trực tiếp API với `--seed-admin` khi đã đặt connection string local và môi trường Development.

Web: Đăng nhập → Tạo tài khoản khách hàng → dùng email synthetic `@example.invalid` → nhập mã xác minh từ file JSON mới trong thư mục `IDENTITY_MAIL_DIRECTORY` ngoài Git → Đăng nhập → Đăng xuất. File chứa `email` và `token`, mã hết hạn sau 30 phút; gửi lại mã sẽ vô hiệu mã cũ. Đây là mail pickup Development, không gửi mail thật, không tự bỏ xác minh; Production/Test không mở đường pickup Development. Trên DB Compose, bật `Identity:MailMode=Synthetic`, key mã hóa và thư mục tuyệt đối qua Compose như trên. Giữ key để outbox chưa gửi vẫn giải mã được qua lần khởi động sau. File mail là dữ liệu test local, không đưa vào Git/log.

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

## S2 — Seed danh mục vào SQL thật

Sau migration, dùng image API của nhánh S2:

```powershell
docker compose --env-file infra/.env -p klcn-s0 -f infra/compose.yaml build api
docker compose --env-file infra/.env -p klcn-s0 -f infra/compose.yaml run --rm api --seed-catalog
docker compose --env-file infra/.env -p klcn-s0 -f infra/compose.yaml up -d api
```

Hoặc đặt `ASPNETCORE_ENVIRONMENT=Development`, `Catalog__UseDemoData=false`, `ConnectionStrings__TiemChung` trỏ SQL Compose `tcp:127.0.0.1,14333`, rồi `dotnet run --project backend/src/TiemChung.Api --no-launch-profile -- --seed-catalog`.
Seed thêm 2 loại tiêm, 2 loại vắc xin, 2 vắc xin và 1 gói chứa hai vắc xin, tên có `[DEMO]`, giá giả VND; chạy lại không nhân bản hoặc ghi đè dữ liệu. Chỉ Development; không xóa/reset DB. Để Web/Windows đọc SQL thật, giữ `Catalog__UseDemoData=false` (mặc định Development và Compose). Chế độ `source=demo` cũ chỉ để smoke không SQL.

Windows → Danh mục: chọn một trong bốn tab, Tìm kiếm, chọn dòng để sửa/xóa hoặc Mới → nhập → Lưu. Gói chọn các vắc xin và số lượng; API kiểm quyền chuyên môn + giá. Web → Danh mục: giới thiệu vắc xin/gói, tìm tên, lọc loại tiêm/loại vắc xin và khoảng giá; lỗi kết nối hiển thị trên trang.
