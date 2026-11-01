# VAC — Hệ thống quản lý tiêm chủng

Dự án gồm Website dành cho khách hàng và ứng dụng Windows dành cho nhân viên. Hai ứng dụng dùng chung ASP.NET Core API; API xử lý nghiệp vụ và lưu dữ liệu trong SQL Server.

Bản hiện tại đã kết nối đăng nhập, danh mục vắc xin/gói tiêm và hồ sơ trên Web, Windows. Đăng ký dịch vụ, thanh toán, lịch tiêm và báo cáo chưa hoàn thiện; các màn mẫu chưa thể dùng để kiểm chứng nghiệp vụ thật.

S5.1: Windows **Lịch hẹn & lịch sử** đã tạo/xem buổi hẹn qua API/SQL từ dòng dịch vụ lẻ hoặc gói. Tài khoản cần Lễ tân Active có quyền `QuanLyLichHen`; các lệnh seed hiện tại không tự cấp quyền này. S5.2: Website **Lịch hẹn** cho khách hàng đăng nhập xem danh sách/chi tiết thuộc hai liên kết hồ sơ Active, tải lại/phân trang qua API thật; giờ hiển thị theo thiết bị. Tải lại trang cần đăng nhập lại. Chưa có đổi/hủy lịch hoặc nhắc lịch thật. Lịch hẹn không phải kết luận đủ điều kiện tiêm.

Phạm vi dự kiến theo [SCOPE.md](SCOPE.md): thanh toán toàn bộ một lần có nhãn mô phỏng, ưu đãi giảm tiền VND không cộng dồn, ghi tiêm có kết luận/chỉ định và kiểm lô/hạn dùng/tồn kho/quyền tại API. S5.4 có lệnh seed lô/tồn synthetic lưu SQL riêng cho Development/Test (hướng dẫn bên dưới); S5.5 ghi nhận mũi/trừ tồn qua API thật tại A06; không làm nhập kho/khám chi tiết hoặc đặt cọc/trả góp. R00 chỉ đồng bộ tài liệu. Web đổi mật khẩu tại **Tài khoản → Đổi mật khẩu**; thành công thu hồi mọi phiên và yêu cầu đăng nhập lại.

S5.5: Windows **Chuyên môn → Ghi nhận mũi tiêm (A06)** chỉ dành cho `DieuDuong` Active. Chọn chỉ định bác sĩ từ lịch/đăng ký thật → đối chiếu người tiêm/vắc xin → chọn lô SQL → nhập vị trí thực tế → xác nhận đã thực hiện. API kiểm lại chỉ định, nguồn, thanh toán G3 toàn bộ và lô/hạn dùng theo ngày cơ sở Việt Nam UTC+7; tổng 0 chưa được phép ghi tiêm. Một transaction ghi mũi, trừ 1 tồn, ghi biến động xuất và audit. Kết quả chỉ đọc, có thể mở lại bằng mã chỉ định `CD-...`. Mất phản hồi phải **GET đối chiếu** trước khi thử lại đúng yêu cầu cũ; đã nhận 201 chỉ GET, không POST lại. Khu vực ghi mũi không tự tạo theo dõi hoặc quyết định cho về. Contract và migration bảo toàn legacy ở [backend/contracts/README.md](backend/contracts/README.md#s55--điều-dưỡng-ghi-nhận-một-mũi-đã-thực-hiện-windows-a06); không tự migrate/seed DB demo hiện có khi kiểm thử.


S5.6: Windows **Chuyên môn → Theo dõi sau tiêm** (cạnh Ghi nhận mũi tiêm) dành cho `DieuDuong` Active. Chọn mũi SQL chưa có theo dõi, đối chiếu người/vắc xin/lô/thời điểm, chủ động nhập tình trạng (100 ký tự), mức độ (100) và xử trí thực tế (500), xem lại rồi xác nhận lưu. Không có nội dung mặc định. API chỉ ghi theo dõi và audit cùng transaction; không đổi mũi/tồn/thanh toán, không kiểm lại thanh toán hoặc đòi lô còn tồn/còn hạn. Mỗi mũi tối đa một bản ghi, sau lưu chỉ đọc; mở lại bằng mã `MT-...`. Mất phản hồi GET đối chiếu trước retry, nhận 201 chỉ GET. Đây không phải danh sách người còn ở cơ sở, quyết định an toàn/cho về, nhật ký sinh hiệu hoặc timer 30 phút. Chi tiết contract ở [backend/contracts/README.md](backend/contracts/README.md#s56--điều-dưỡng-ghi-kết-quả-theo-dõi-sau-tiêm-windows-a06).

S5.7A: Website **Lịch sử tiêm** (cạnh Lịch hẹn) cho Customer Active đã xác minh email đọc các mũi `DaTiem` có nguồn S5 và đủ hai liên kết hồ sơ Active. Danh sách tổng hợp nhiều người được tiêm, phân trang 25 mũi; chọn mũi để xem người/vắc xin/thời gian địa phương/mã lịch/số lô/vị trí. Tải lại đọc SQL, logout/đổi phiên xóa dữ liệu trong bộ nhớ; tải lại cả trang cần đăng nhập lại. Mũi vẫn đọc được khi lô đã hết tồn/hết hạn hoặc thanh toán thay đổi, không cần bản theo dõi. Không hiển thị tiền sử/khám/nội dung theo dõi, không suy phác đồ hoặc chứng nhận hoàn thành. API chỉ đọc, không migration; contract ở [S5.7A](backend/contracts/README.md#s57a--customer-đọc-lịch-sử-tiêm-trên-website). Nhắc lịch thuộc S5.7B, S5 chưa hoàn tất.

## 1. Cần cài đặt

S5.7B: Windows **A09 Lịch hẹn & lịch sử → Nhắc lịch mô phỏng** dành cho Lễ tân Active có quyền `QuanLyLichHen`. Chọn lịch → Xem nhắc lịch → xem preview → xác nhận ghi log. API kiểm nguồn/lịch tương lai/chưa có mũi thuộc buổi hẹn, tạo snapshot giờ Việt Nam UTC+07:00 và audit cùng transaction. Mỗi lịch tối đa một log; đây là thao tác nội bộ, **không gửi email/SMS và không chứng minh khách hàng nhận thông báo**. Sau lưu chỉ đọc nội dung/người ghi/thời điểm; mất response phải GET đối chiếu trước retry thủ công, nhận201 lỗi body chỉ GET. Migration mới giữ legacy, có kiểm xung đột và Down guard; kiểm thử chỉ trên DB disposable. Không scheduler, sửa/hủy lịch hoặc suy phác đồ. Contract ở [S5.7B](backend/contracts/README.md#s57b--log-nhắc-lịch-mô-phỏng-windows-a09); gói đang bàn giao, chưa tự đánh dấu toàn bộ S5 hoàn tất.

Chạy theo hướng dẫn trên máy Windows, có kết nối Internet để tải thư viện lần đầu:

- .NET SDK 10.
- Node.js 24 LTS, kèm npm.
- Docker Desktop, bật chế độ Linux containers và mở Docker trước khi chạy.

Giải nén dự án vào một thư mục riêng. Mở PowerShell tại thư mục chứa file hướng dẫn này. Các lệnh bên dưới đều chạy từ đây.

### Chạy một chạm trên Windows

Sau khi thiết lập lần đầu ở mục 2 để có `infra/.env` và các image Compose (chọn đúng project bên dưới), mở thư mục [`scripts/devtools/`](scripts/devtools/) và nhấp đúp:

- **VAC-Start.cmd**: kiểm tra/mở Docker Desktop, khởi động SQL/API bằng image có sẵn, chờ API, mở Web tại port 5173 và Windows WPF. Start lặp lại dùng lại tiến trình do launcher quản lý; không tự seed hoặc chạy full test.
- **VAC-Check.cmd**: chạy smoke **chỉ đọc** (trạng thái SQL/API/Web/WPF và GET danh mục SQL), rồi hiện menu **F** full test, **A** mở sổ tài khoản, **R** rebuild API, **S** status. Chỉ rebuild khi backend/Dockerfile đổi hoặc chưa có image; Stop trước khi rebuild. Nếu cần xóa container API/migrate đã dừng, launcher liệt kê ID/phạm vi quản lý và yêu cầu nhập **REBUILD** để xác nhận; không xóa container SQL hoặc volume.
- **VAC-Stop.cmd**: chỉ dừng tiến trình/container launcher đã quản lý, dùng Compose `stop` để giữ database/volume; giữ nguyên thành phần chạy từ trước. Nếu port bị chương trình khác chiếm, launcher báo lỗi và giữ nguyên chương trình đó.

Tạo shortcut Desktop: chuột phải từng `.cmd` → **Show more options → Send to → Desktop (create shortcut)**; giữ file gốc trong repository. Launcher tự tìm thư mục gốc, không cần sửa đường dẫn máy.

**Chọn đúng Compose project:** launcher mặc định **`klcn-s0`**, là demo hiện đang dùng. Các lệnh thủ công bên dưới dùng **`vac-demo`**, một môi trường khác với volume riêng (`klcn-s0_sqlserver-data` khác `vac-demo_sqlserver-data`); đổi tên project không chuyển dữ liệu và có thể xung đột các port chung. Khi thao tác demo hiện tại, thay mọi `-p vac-demo` bên dưới bằng `-p klcn-s0`. Chỉ đặt biến môi trường `VAC_COMPOSE_PROJECT` (hoặc tham số `-ComposeProject` của `VAC.ps1`) khi chủ động dùng project khác; dùng cùng giá trị cho Start/Check/Stop. Không tạo lại `.env` hoặc seed lại để xử lý nhầm project.

Menu **A** yêu cầu biến môi trường `VAC_TEST_ACCOUNTS_FILE` trỏ tới file sổ tài khoản local tuyệt đối **ngoài repository** (hoặc `-AccountsFile` khi gọi `VAC.ps1`). Chưa cấu hình/file không tồn tại sẽ báo rõ; launcher chỉ mở file, không tạo sổ, sao chép hay in mật khẩu. Không đưa sổ hoặc cấu hình chứa credential vào Git.

Smoke không thay kiểm thử nghiệp vụ. **Full test** gọi `scripts/verify.ps1`, cần `M1_SQL_TEST_CONNECTION` của SQL development/test có quyền tạo DB test riêng; thiếu biến sẽ báo **CHƯA CHẠY**, không tự tạo SQL login hay đổi quyền/mật khẩu. Kiểm tra riêng launcher, không cần Docker/SQL chạy: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/devtools/VAC.SelfTest.ps1`.

## 2. Khởi động SQL Server và API

### Thiết lập lần đầu

Đoạn lệnh sau tạo cấu hình riêng cho máy người nhận. Chỉ chạy một lần; giữ nguyên file `infra/.env` ở những lần sử dụng tiếp theo.

```powershell
if (Test-Path infra/.env) { throw 'Da co infra/.env; bo qua buoc tao cau hinh.' }
function New-VacKey {
    $bytes = New-Object byte[] 32
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes); [Convert]::ToBase64String($bytes) }
    finally { $rng.Dispose() }
}
$vacMailDir = Join-Path $env:LOCALAPPDATA 'VAC-Demo/Mail'
New-Item -ItemType Directory -Force $vacMailDir | Out-Null
@(
    "MSSQL_SA_PASSWORD=VacSql!$([Guid]::NewGuid().ToString('N'))A1"
    "IDENTITY_SIGNING_KEY=$(New-VacKey)"
    "IDENTITY_MAIL_ENCRYPTION_KEY=$(New-VacKey)"
    "IDENTITY_MAIL_DIRECTORY=$($vacMailDir.Replace('\','/'))"
) | Set-Content -Encoding ascii infra/.env
```

Khởi động từ mã nguồn:

```powershell
docker compose --env-file infra/.env -p vac-demo -f infra/compose.yaml up -d --build
Invoke-RestMethod http://127.0.0.1:5080/health
```

Lần đầu Docker cần tải các image nên có thể mất vài phút. Khi sẵn sàng, lệnh kiểm tra trả `status: ok`. SQL Server chạy tại `127.0.0.1,14333`; API chạy tại `http://127.0.0.1:5080`. Cấu trúc bảng được tạo tự động bằng migration.

### Tạo dữ liệu và tài khoản demo

Tạo danh mục vắc xin/gói tiêm mẫu:

```powershell
docker compose --env-file infra/.env -p vac-demo -f infra/compose.yaml run --rm api --seed-catalog
```

S5.4 — tạo nguồn lô/tồn synthetic để chuẩn bị kiểm thử ghi tiêm (chỉ chạy trên DB Development/Test chủ động chọn, không chạy trên Production):

```powershell
docker compose --env-file infra/.env -p vac-demo -f infra/compose.yaml run --rm api --seed-inventory
```

Lệnh yêu cầu môi trường **Development**, migration đã áp dụng và đúng một vắc xin SQL tên `[DEMO] Vắc xin A`, `[DEMO] Vắc xin B` từ catalog synthetic. Thiếu/trùng nguồn hoặc mã lô gắn sai vắc xin thì báo lỗi và rollback; không tự gọi `--seed-catalog`, migrate hoặc tạo vắc xin thay thế. Chạy từng lệnh riêng, không ghép `--seed-inventory` với lệnh seed/migrate khác. API khởi động bình thường không tự seed. Nếu image API có sẵn từ trước S5.4, cần rebuild image từ source mới trước khi chạy lệnh.

| Mã lô synthetic | Vắc xin | Ngày sản xuất / hạn dùng khi tạo lần đầu | Ban đầu / tồn | Trạng thái |
|---|---|---|---|---|
| `[DEMO] S5-VALID-A` | `[DEMO] Vắc xin A` | Ngày UTC chạy lệnh − 6 tháng / + 1 năm | 100 / 100 | Active |
| `[DEMO] S5-EXPIRED-A` | `[DEMO] Vắc xin A` | Ngày UTC chạy lệnh − 2 năm / − 1 ngày | 20 / 20 | Active |
| `[DEMO] S5-EMPTY-B` | `[DEMO] Vắc xin B` | Ngày UTC chạy lệnh − 6 tháng / + 1 năm | 10 / 0 | Active |

Mã lô cố định, không lấy từ lô thực tế. Transaction và khóa SQL `sp_getapplock` tuần tự hóa các lần seed đồng thời. Chỉ thêm mã thiếu; mã có sẵn phải đúng chính xác mã và `MaVacXin` nguồn. Chạy lại giữ nguyên mọi trường/RowVersion của lô đã có, kể cả tồn đã giảm, số lượng ban đầu, ngày sản xuất/hạn dùng và trạng thái đã đổi; không sửa lô khác. Vì ngày không được gia hạn khi chạy lại, lô hợp lệ ban đầu có thể hết hạn về sau. Lệnh không tạo biến động kho, lịch hẹn, phiếu khám/chỉ định, thanh toán hay mũi tiêm; lệnh seed không cung cấp API CRUD lô/UI kho hoặc thực hiện trừ tồn; ghi mũi S5.5 dùng API riêng.

Với SQL test disposable đã được phép và biến `M1_SQL_TEST_CONNECTION` ngoài Git, chạy bộ kiểm thử S5.4 (fixture tự tạo DB GUID mới, migrate và thu hồi đúng DB thuộc sở hữu fixture):

```powershell
dotnet test backend/tests/TiemChung.Tests/TiemChung.Tests.csproj --configuration Release --filter FullyQualifiedName~DevelopmentInventorySeedSqlTests
```

Tạo Admin để đăng nhập ứng dụng Windows; tự chọn mật khẩu có ít nhất 12 ký tự và ghi nhớ mật khẩu vừa nhập:

```powershell
$env:DevelopmentSeed__Username = 'demo.admin'
$env:DevelopmentSeed__Password = Read-Host 'Nhap mat khau Admin demo (it nhat 12 ky tu)'
docker compose --env-file infra/.env -p vac-demo -f infra/compose.yaml run --rm -e DevelopmentSeed__Username -e DevelopmentSeed__Password api --seed-admin
Remove-Item Env:DevelopmentSeed__Username, Env:DevelopmentSeed__Password
```

Các lệnh seed không ghi đè tài khoản Admin hoặc nhân bản danh mục đã có. Dữ liệu có nhãn `[DEMO]` và giá giả, chỉ dùng để xem thử.

Tạo Lễ tân Development để ghi hồ sơ qua API thật (không cấp quyền này cho Admin):

```powershell
$env:DevelopmentSeed__Username = 'demo.reception'
$env:DevelopmentSeed__Password = Read-Host 'Nhap mat khau Le tan demo (12-128 ky tu)'
docker compose --env-file infra/.env -p vac-demo -f infra/compose.yaml run --rm -e DevelopmentSeed__Username -e DevelopmentSeed__Password api --seed-reception
Remove-Item Env:DevelopmentSeed__Username, Env:DevelopmentSeed__Password
```

Lệnh chỉ chạy ở Development, tạo nhân viên `[DEMO]` chức danh `LeTan` và tài khoản Active, hash tại backend, không thêm quyền mở rộng. Chạy lại giữ nguyên mật khẩu, trạng thái và dữ liệu hiện có; username đã thuộc chức danh khác bị từ chối. Đây không phải chức năng quản lý nhân viên. Credential test được ghi trong sổ local theo AGENTS, ngoài Git.

## 3. Mở các giao diện

Giữ Docker đang chạy. Mỗi giao diện mở trong một cửa sổ PowerShell riêng.

### Website

```powershell
npm --prefix apps/web ci
npm --prefix apps/web run dev -- --host 127.0.0.1 --port 5173 --strictPort
```

Mở `http://localhost:5173`. Trang chủ và Danh mục có thể xem ngay. Để thử hồ sơ, chọn **Đăng nhập → Tạo tài khoản khách hàng**, dùng email dạng `ten@example.invalid` và mật khẩu 12–128 ký tự. Mở file JSON mới nhất trong `%LOCALAPPDATA%\VAC-Demo\Mail`, lấy trường `token` nhập vào bước xác minh email, rồi đăng nhập. Thư xác minh được lưu local, không gửi email ra ngoài.

### Ứng dụng Windows

```powershell
$env:TIEMCHUNG_API_BASE_URL = 'http://127.0.0.1:5080'
dotnet run --project apps/windows/TiemChung.Desktop.csproj
```

Đăng nhập bằng `demo.admin` và mật khẩu đã chọn ở bước tạo Admin. Có thể mở Danh mục và xem hồ sơ; thêm/sửa/xóa hồ sơ bằng tài khoản nhân viên yêu cầu chức danh Lễ tân (`LeTan`) tại API. Lệnh `--seed-admin` không tạo tài khoản Lễ tân; không nới quyền Admin để thay thế. Tài khoản nhân viên dùng cho Windows; tài khoản khách hàng dùng cho Website và chỉ thao tác hồ sơ thuộc phạm vi liên kết của mình.

## 4. Cấu trúc dự án

| Thư mục / file | Vai trò |
|---|---|
| `apps/web/` | Giao diện Website React; gọi API để lấy và cập nhật dữ liệu. |
| `apps/windows/` | Ứng dụng nhân viên C# WPF; màn hình trong `ViewsV2`, xử lý giao diện trong `ViewModels`, gọi API trong `Services`. |
| `backend/src/TiemChung.Api/` | Nhận yêu cầu từ các giao diện, kiểm tra tài khoản/quyền và xử lý nghiệp vụ. |
| `backend/src/TiemChung.Contracts/` | Cấu trúc dữ liệu trao đổi với API. |
| `backend/src/TiemChung.Domain/` | Các đối tượng dữ liệu và quy tắc nghiệp vụ. |
| `backend/src/TiemChung.Infrastructure/` | Kết nối SQL Server và các migration tạo/cập nhật bảng. |
| `backend/contracts/` | Mô tả API và request mẫu HTTP/Postman. |
| `backend/database/` | Script SQL bổ trợ; schema chính nằm trong EF migrations. |
| `backend/tests/`, `backend/tools/` | Kiểm thử và công cụ Identity hiện có. |
| `backend/Dockerfile`, `infra/compose.yaml` | Đóng gói API và khởi động API cùng SQL Server. |
| `scripts/` | Cài thư viện và kiểm tra dự án. |
| `.github/` | CI và cấu hình cộng tác GitHub. |
| `AGENTS.md`, `SCOPE.md` | Quy tắc phát triển và phạm vi nghiệp vụ. |
| `global.json`, `Directory.Build.props` | Phiên bản .NET và cấu hình build dùng chung. |

Luồng hoạt động: **Web / Windows → API → SQL Server**. Giao diện không kết nối SQL trực tiếp; mật khẩu đăng nhập được hash ở backend.

Thư mục chỉ giữ source, tài nguyên ứng dụng, test, cấu hình và hướng dẫn cần thiết. Thư viện/cache và kết quả build được tạo lại khi restore/build; không chép chúng cùng source. Giữ `infra/.env` riêng trên máy đang dùng để chạy lại với dữ liệu SQL hiện có. Không đưa file này vào Git.

## Kiểm tra source

Chạy từ thư mục gốc:

```powershell
./scripts/bootstrap.ps1
./scripts/verify.ps1
```

`verify.ps1` chạy test backend, lint/test/build Web và build Windows. Suite backend đầy đủ cần `M1_SQL_TEST_CONNECTION` trỏ SQL development/test có quyền tạo DB test riêng; đặt biến trong shell, không ghi credential vào source. Nếu chưa có SQL, chỉ kiểm tra các test không cần DB bằng lệnh dưới; kết quả này không thay cho kiểm thử SQL:

```powershell
dotnet test backend/TiemChung.slnx --filter 'FullyQualifiedName!~SqlTests'
```

## 5. Dừng và chạy lại

Dừng Web bằng `Ctrl+C`; đóng cửa sổ Windows. Dừng API và SQL nhưng giữ dữ liệu:

```powershell
docker compose --env-file infra/.env -p vac-demo -f infra/compose.yaml stop
```

Lần sau dùng lại lệnh `up -d` ở bước 2 và mở các giao diện; không cần tạo lại cấu hình hoặc seed. Nếu báo không kết nối API, kiểm tra Docker đang chạy và `http://127.0.0.1:5080/health`. Các cổng 5080, 5173 và 14333 cần trống trước khi khởi động.

Tài nguyên giao diện: logo VAC được cung cấp cho dự án; ảnh [tiêm chủng](https://www.pexels.com/photo/man-vaccinating-a-woman-8770702/), [nhân viên y tế](https://www.pexels.com/photo/doctor-holding-vaccine-bottle-6285354/), [chăm sóc khi tiêm](https://www.pexels.com/photo/person-getting-vaccinated-3985170/) của Gustavo Fring/Pexels; icon từ [Lucide](https://lucide.dev/).
