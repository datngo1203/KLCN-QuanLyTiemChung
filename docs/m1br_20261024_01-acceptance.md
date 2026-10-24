# m1br_20261024_01 — acceptance một phần, chưa đóng gate CLI

## Generation 2 sau chấp thuận thay fixture

Backup SQL COPY_ONLY/CHECKSUM + VERIFYONLY thực đạt trước thay duy nhất DB legacy. [Record](m1br_20261024_01-legacy-rebuild.json), [archive manifest đúng run trước Apply](m1br_20261024_01-manifest-archives.json), [so sánh Boundary/login](m1br_20261024_01-g2-preservation.json). Backup giữ g1/ledger consumed, g2 có DB identity mới/ledger trống/Admin Locked/None; không sửa hash/quota g1 hoặc thay Boundary/login/password/instance. G2 actual-login boundary Release **21/21**. Chưa G2 Recover/Reset/actor-password match/login/token/concurrency/rollback; mật khẩu Recover chỉ dự kiến trong sổ local, kiểm khớp ngay sau terminal mới tiếp tục Reset. Không restore hoặc cleanup/mode rollback khi không cần/acceptance chưa đủ. Checkpoint g1 dưới đây là lịch sử.

## Checkpoint terminal / lỗi manifest PowerShell 5.1

**Checkpoint mới:** Recover đã exit 0 và audit/ledger ghi đúng SQL operator. Reset hai lần bị permissionDenied trước target input; mật khẩu đề xuất không khớp hash. Diagnostic paste nhận đúng 23 ký tự/0 control, người dùng không nhớ mật khẩu thực tế. Không có reset commit. [Đề nghị tái tạo riêng fixture legacy g2, chờ phép](m1br_20261024_01-legacy-rebuild-request.md); chưa thực thi. Không xóa quota/hash để vượt gate.

- Bootstrap terminal thật exit 0, binary SHA256 `54B39542B7D5C80B23ADBEB66147FA2B686E2162C5D20A436871408D8B04568F`; DB Boundary account/employee 1, Admin active/Technical, 12 quyền, backend Identity hash; audit adminBootstrapped Success với original SQL operator identity, ledger 0. Exit 1 trước đó giữ lịch sử, không suy nguyên nhân. Chưa kiểm HTTP login của tài khoản bootstrap.
- Recover hai lần exit 1 trước prompt password: Windows PowerShell 5.1 ConvertTo-Json xuất DateTimeOffset dạng `/Date(milliseconds)/`, không hợp System.Text.Json. DB legacy old-admin vẫn Locked/None/SecurityVersion1; audit recovery/ledger đều 0, không có recovery commit.
- Writer `scripts/m1br-manifest-json.ps1` chuyển DateTimeOffset sang chuỗi ISO-8601 rõ ràng trước JSON. Regression chạy **powershell.exe thực trên Windows** và deserialize thành RecoveryManifest bằng .NET, giữ đúng instant/25 phút/operation ID/target/version; không giả terminal CLI. Boundary suite sau sửa **21/21**. Không đổi API/schema/contract hay validity policy.
- File manifest local đã chuẩn hóa hai trường ngày, backup ngoài Git `.pre-iso`; giữ nguyên thời điểm gốc, OperationId, target, version và mọi trường khác. Không gia hạn/quota mới hoặc reset ledger. User chỉ cần chạy lại Recover trong thời hạn manifest còn hiệu lực; chưa chạy Reset.
- CLI evidence không chứa password tại [jsonl](m1br_20261024_01-cli-evidence.jsonl). Dữ liệu credential synthetic chỉ ở sổ local đã được người dùng cho phép, không Git/PR/log công khai. AGENTS/status có thay đổi từ chat khác được giữ riêng. Các dòng chưa bootstrap phía dưới là lịch sử trước checkpoint này.

## Môi trường và provisioning thực tế

- Người dùng chạy apply elevated thành công: [evidence trước/sau](m1br_20261024_01-auth-mode-evidence.json). Agent kiểm lại `LAPTOP-DAT\SQLEXPRESS`, IntegratedOnly=0, Windows admin còn login được, encryption TRUE, NTLM; quyền deployment tạo DB/login=1; tên hai DB/hai login chưa tồn tại trước tạo. `sa` vẫn disabled. Không cấu hình khác/TCP/firewall/ACL/trust store.
- Ngoại lệ `TrustServerCertificate=True`/`-N -C` chỉ kết nối test local/run này. Encryption đạt; **certificate validation không được xác minh**. Không đổi default production.
- Tạo đúng hai DB clean/legacy và hai SQL login trong [manifest](m1br_20261024_01-manifest.json). Clean migrate đầy đủ; legacy InitialCreate → seed Admin/LeTan có hash legacy không xác thực → migrate M1. Apply boundary DDL, 2 users, m1_app/m1_operator tách, fixtures synthetic/marker chỉ trong hai DB này. Không sao chép deployment credential vào runtime.
- Secret SQL ngẫu nhiên lưu DPAPI theo user tại LocalApplicationData ngoài Git; launcher giải mã trong process, không argv/log/terminal output. Recovery manifest cũng ngoài Git, không phải authority. Không gửi mail người thật.

## Lỗi thực đã tìm và sửa trong phạm vi thiết kế

| Lỗi thực | Sửa | Bằng chứng |
|---|---|---|
| DDL có database permission `IMPERSONATE ANY USER` không được SQL Server hỗ trợ; transaction boundary rollback | Dùng DENY ALTER ANY ROLE/USER hợp lệ, DENY IMPERSONATE trên dbo/guest; không grant impersonation; deny giữa hai runtime users trong provisioning | DDL thực apply thành công cả hai DB; effective IMPERSONATE dbo=0; login app thực EXECUTE AS dbo bị 15517, không dùng impersonation thay bằng chứng |
| HTTP register 500: EF trigger-compatible INSERT audit SELECT lại Id từ bảng bị DENY SELECT | `AuditInsert.cs`: parameterized INSERT + SCOPE_IDENTITY cùng connection/caller/transaction, không đọc audit hoặc nâng context, không thêm grants/schema; audit được detach sau acceptance để bỏ snapshot key tạm | HTTP host thực qua app login đạt; test append không có SELECT, SaveChanges lần hai không chèn lại, caller rollback, audit SELECT Id/OperatorIdentity bị chặn; collector xác nhận trace append rollback không tồn tại |

Thử nghiệm trung gian 15/16 và 19/20/34-of-42 thất bại không được dùng làm PASS. Regression phát hiện snapshot key audit sau reattach trong context lâu sống; đã sửa detach append-only và chạy lại toàn bộ. Không thay contract/nghiệp vụ/reset/ledger hoặc batch/delivery.

## Kiểm chứng đã chạy sau sửa

- `dotnet test backend/tests/TiemChung.BoundaryAcceptance/TiemChung.BoundaryAcceptance.csproj -c Release`: **20 passed, 0 failed/skipped**, bằng hai SQL login thật trên legacy DB. Bao gồm operator gate chấp nhận op/từ chối app; app DML Employee old/new/multirow/MERGE, NhanVien/grants/ledger/audit/meta/trigger denial; op không DDL/meta/audit delete; CLI process dùng app credential bị chặn trước prompt, caller tự đặt allowlist/secret không giúp vượt.
- App HTTP bằng app grants, không operator/deployment credential: register → dispatcher lifecycle → synthetic sink → verify → login → auth/me; GET customers/recipients; POST customers/recipients và PUT recipient bị 403; refresh → logout → auth/me 401. Không gọi drain helper thay lifecycle.
- `dotnet test backend/TiemChung.slnx -c Release`: **42 passed, 0 failed/skipped** chạy mới sau sửa audit trên `(localdb)\KLCN_M1_Test` synthetic riêng theo regression có sẵn. Bao gồm SQL/auth/reset/recovery rollback/concurrency, batch/dedup/lifecycle; **không thay actual CLI positive hoặc principal acceptance**. Không tạo DB regression khác trên SQLEXPRESS.
- TRX/log raw nằm `artifacts/m1br-boundary/`, `artifacts/m1br-regression/` ngoài Git. [Evidence SQL sạch](m1br_20261024_01-sql-evidence.json) đọc bằng actual login, effective perms, grants/membership, guards CALLER/enabled, DB/login creation identity, audit/outbox counts. Runtime sysadmin/db_owner/CONTROL/schema ALTER/IMPERSONATE dbo đều 0; hai role không kiêm nhau; audit SELECT=0.
- Script parser/JSON/diff được kiểm; publish operator riêng tại artifacts/m1br-operator. CI theo HEAD cuối xem Draft #33, không lấy CI lịch sử thay kết quả mới. CI không tự provision SQLEXPRESS hoặc chạy positive terminal CLI.

## Gate còn mở và bước terminal thực

`scripts/m1br-cli.ps1 -Command Bootstrap`, `Recover`, `Reset`: yêu cầu terminal thật không stdin redirect, password nhập ẩn trực tiếp vào CLI. Launcher dùng binary đã publish, ghi command/database/exit/hash/time không credential; hướng dẫn employee/account/version. Bootstrap clean DB employee 1, username test-admin; recovery old-admin dùng password mới; reset actor old-admin/password recovery, target legacy-staff, khác password actor. Khách hàng/nhân viên chỉ là synthetic.

**Chưa có bằng chứng CLI positive** bootstrap/recovery/reset, CLI retry/concurrency/rollback/actor negatives, SQL audit/ledger/token/login sau terminal commands. Service tests và app CLI rejection không thay chúng. Chưa xác nhận handoff password/old-token revocation từ actual CLI; không giả workflow đổi password. Chỉ nhập password test trong terminal, không chat/argv/Git.

Chưa cleanup hoặc rollback mode: giữ đúng hai DB/hai login để các ca CLI còn thiếu chạy được; service Running/Mixed Mode/sa disabled. `scripts/m1br-cleanup.ps1` đã chuẩn bị nhưng chưa chạy: gate acceptanceComplete, đối chiếu DB ID/create_date/owner/marker và login SID/create_date với evidence, từ chối phiên còn mở, drop đúng tên không forced disconnect/wildcard. Sau khi đủ evidence/cleanup, người dùng chạy [rollback LoginMode=1 có kiểm soát](m1br_20261024_01-auth-mode-runbook.md); agent không elevated nên chưa thực hiện. Chưa M1 nghiệm thu/production-ready hoặc merge.
