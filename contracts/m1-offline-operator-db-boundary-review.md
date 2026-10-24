# Thay thế Windows-only operator bằng backend CLI — chờ review trước code

Base `0af5f9c`. Người dùng đã chốt hướng tài khoản/nghiệp vụ thuộc API/SQL, không cần tài khoản Windows riêng, SID/ACL cố định hoặc VM để sử dụng/nghiệm thu M1. Đây là **một gói thay contract/spec/runbook/checklist**, chưa phải approval giải pháp kỹ thuật dưới đây. Chưa code/migration/grant/deployment. Giữ nguyên batch `48171fe`, delivery/dedup, clients và lộ trình M2.

Sau review/chấp thuận, tài liệu này thay **toàn bộ** yêu cầu Windows-only, Known Folder policy, OS allowlist, expected operator secret và ancestor ACL trong các tài liệu operator trước. Không tiếp tục phát triển ProtectedOperatorPolicy/CheckChain. Không thay quyền nghiệp vụ, quota recovery hoặc mật khẩu client. CLI là công cụ backend riêng, không HTTP/public endpoint, không đưa vào API/client publish.

## Boundary thực và người giữ credential

- DBA nắm principal triển khai và cấp **DB user vận hành riêng trong đúng DB đích**; người quản trị backend được phân công giữ credential user vận hành qua secret store của môi trường, ngoài Git/argv/log. DBA không chuyển sysadmin/DB-owner credential cho CLI. App runtime dùng credential DB khác; không đưa credential operator vào API config hoặc VITE/mobile/Windows.
- CLI nhận connection/credential qua secret provider cấu hình vận hành (M1 có thể dùng environment do launcher cung cấp). Giá trị từ caller là **credential cần xác thực**, không phải policy chứng minh mình đúng. SQL Server thật phải xác thực credential và membership/quyền của connection; caller tự đặt username/password/role-name hoặc trỏ file khác không tạo được SQL login/membership trong DB được quản lý. Không có expected-secret/allowlist thứ hai cùng caller tự cấp.
- Có credential hợp lệ nhưng chỉ app principal cũng không được operator. CLI kiểm đúng DB/migration metadata và membership role `m1_operator` bằng execution context thực từ SQL (`IS_ROLEMEMBER` không truyền username caller); null/0 hoặc quyền thiếu thì fail closed trước mutation. DB identity/epoch lấy từ metadata DB, không coi server/database/manifest do caller khai là bằng chứng quyền. SQL sai credential/đích sai/mất kết nối từ chối; không fallback tài khoản khác. Tên role cố định do migration/DBA tạo, caller không được thay.
- Người có thể tạo DB giả và credential riêng chỉ thao tác được DB giả đó, không được nhận quyền trên DB trung tâm. Connection đích sản phẩm được launcher/deployment quản lý; không tuyên bố CLI tự chứng thực một SQL server do kẻ tấn công kiểm soát là DB sản phẩm. Không đưa hash/bí mật sản phẩm sang server chưa được quản trị xác nhận. TLS chứng chỉ server được kiểm; test SQL synthetic riêng có cấu hình trust test rõ, không đổi production.
- Phạm vi tin cậy: DBA/host admin và người nắm credential operator có quyền DB tương ứng là trusted vận hành. Không hứa ngăn họ dùng SQL tool riêng, sửa host/binary hoặc tác động ngoài CLI. Guard SQL dưới đây chống principal **không được cấp quyền vận hành**, không phải cơ chế chống DBA. Việc caller biết đường config hoặc tự viết secret không đủ, nhưng credential operator bị lộ là sự cố phải revoke ở SQL/secret store.

## Cưỡng chế SQL tối thiểu — không chỉ CLI check

Đề nghị bổ sung script/migration bảo vệ M1 riêng sau approval; không sửa InitialCreate. Không áp lên SQL dùng chung trong lượt này. Không dùng db_owner/db_datawriter/db_datareader làm shortcut cho app hoặc operator.

| Principal | Grant tối thiểu và cấm |
|---|---|
| Deployment/DBA | Tạo DB/migrate/schema/roles/guard/grants; tách khỏi runtime. Không nằm trong cấu hình CLI/API. Seed code quyền/metadata qua migration, không account demo |
| `m1_app` | SELECT các bảng Identity/Profiles cần auth; INSERT/UPDATE TaiKhoan cho luồng customer bị guard bên dưới giới hạn; DML session/refresh/email-verification/outbox phục vụ auth, INSERT audit; SELECT danh mục quyền/NhanVien cho quyền nhân viên. Profiles chỉ grants read/create đang dùng. Không ghi NhanVien, QuyenHan, TaiKhoan_QuyenHan, recovery metadata/ledger, AccountProfileLink. Không DELETE account/audit; không UPDATE/DELETE audit; không ALTER schema/table/trigger, CONTROL, TAKE OWNERSHIP, IMPERSONATE, ALTER ROLE hoặc GRANT WITH GRANT OPTION |
| `m1_operator` | CONNECT; SELECT TaiKhoan/NhanVien/QuyenHan/grants/sessions/credentials/recovery metadata+ledger/migration history; INSERT TaiKhoan/ledger/audit/grants; UPDATE TaiKhoan, NhanVien.TrangThai, sessions/credentials để revoke; DELETE grants để thay quyền. Không xóa account/nhân viên/audit/ledger/metadata; không sửa metadata DB/mốc, không DDL/role/impersonation/quyền server, không dữ liệu y tế/tài chính. Khả năng đọc hash account là cần cho xác thực actor backend, không in ra ngoài |

**TaiKhoan dùng chung Customer/Employee nên table grant đơn thuần không đủ.** Cần đúng một guard DML transaction trên TaiKhoan: khi execution context SQL **không phải `m1_operator`**, từ chối cả statement nếu inserted **hoặc deleted** chứa Employee, MaNhanVien non-null, duty khác None hoặc thay đổi AccountType của row sang/từ Employee. Do kiểm cả old/new, app không thể đổi Employee→Customer để sửa hash rồi đổi lại, INSERT nhân viên Admin, reset Employee, đổi status/duty/SecurityVersion nhân viên hoặc dùng MERGE/batch nhiều row lách guard. Customer vẫn theo CHECK hiện có. Không dựa APP_NAME/HOST_NAME/SESSION_CONTEXT/env/username payload; membership null coi là không có quyền. App không có ALTER table nên không disable/replace trigger. Guard phải THROW rollback cả statement/transaction; không chỉ log rồi cho ghi.

Thêm guard nhỏ trên INSERT SecurityAudit: app không được ghi action quản trị dành riêng `employeeProvisioned`, `employeePasswordReset`, `accountStatusChanged`, `employeeStatusChanged`, `permissionsChanged`, bootstrap/recovery action đúng tên implementation sau rà mapping; operator mới được ghi. Audit không update/delete với cả hai runtime roles. Không dùng audit này để hứa chống người có credential operator. Quyền metadata/ledger/grants đã được SQL object permissions chặn, không cần stored procedure đại trà hoặc trigger mọi bảng.

DBA phải kiểm effective grants của user thực, public/nested roles/column grants và không kiêm `m1_app`/`m1_operator` trên cùng identity. Table DENY không được dùng để che column GRANT còn sót. DB owners/sysadmin không phải principal dùng cho acceptance. Không coi `IS_ROLEMEMBER` một mình là kiểm đầy đủ quyền: startup CLI kiểm permission set cần cho lệnh, SQL thực thi DML vẫn là ràng buộc cuối. Thiếu grant/guard/version migration thì từ chối, không nới app principal. Không migration auto-run từ API/CLI.

## Luồng CLI và invariants giữ nguyên

- Functionality chạy trên .NET CLI Windows/Linux; không import WindowsIdentity/ACL/Known Folder để mở quyền. Secret bảo vệ theo deployment, không bắt đường Program Files. Operator launcher và CLI không được nhận secret qua argv/stdout/transcript. Actor password, password target prompt ẩn; redirected secret input bị từ chối như contract reset. Manifest có thể từ cấu hình vận hành ngoài Git, nhưng không tự tạo quyền SQL hoặc reset quota; kiểm DB ID/epoch/target/version/TTL/ledger như trước.
- Bootstrap: sau khi SQL xác thực operator role/quyền, chỉ DB đủ điều kiện không account; NhanVien Admin fixture/production thật đã có, không tạo nhân viên giả. Khóa quản trị/transaction, seed đúng quyền, hash backend/audit, lần sau từ chối.
- Recovery: operator DB principal thay OS-secret gate; vẫn maintenance, bind version sau migration, DB/mốc một lần, ledger UNIQUE/retry cùng fingerprint/rollback/concurrency nguyên trạng. Operator DB identity được ghi audit; không gán ActorAccountId giả khi chưa có actor app. Recovery không biến thành đường mở khóa thường ngày.
- Reset và quản trị thường: **hai lớp** SQL operator principal + actor account Admin active/nhân viên active/QuanLyTaiKhoan, không Director/self-reset; target/version bất biến, hash backend, tăng SecurityVersion/revoke sessions+credentials/audit cùng transaction. Có DB operator credential không thay app actor credential. Không tự đổi target status/duty/quyền, ledger hoặc tạo account trùng.
- Audit ngoài account dùng danh tính SQL được server trả (`ORIGINAL_LOGIN`, user execution context) và operation/trace ID, không chuỗi actor operator caller tự gửi. Actor account reset vẫn từ xác thực DB hash hiện hành sau recovery. Không log connection/password/token/hash. Không coi login SQL app là được chạy công cụ quản trị.

## Tác động code/schema/tests sau review

| Phần | Thay đổi dự kiến, chưa thực hiện |
|---|---|
| Operator Program/ProtectedOperatorPolicy/OperatorAuthentication | Bỏ Windows checker và expected-secret gate; thay resolver kết nối vận hành + kiểm SQL context/role/effective permissions/guard version. Không giữ dev bypass OS hoặc code chạy song song hai boundary |
| IdentityRecovery/Administration | Giữ hash/actor/transaction/lock/rowversion/revoke; nhận audit operator identity đã xác thực từ SQL thay OS string, không caller tự nhận identity |
| SQL migration/deployment script | Role/grants/2 guards hẹp nêu trên, kiểm effective grants; không chỉnh business account fields hoặc public routes. Cần review DDL trước apply, chỉ DB synthetic được cấp phép |
| API auth/Profiles | Giữ behavior; chạy regression dưới m1_app thật để chứng minh register/verify/login/refresh/logout/Profiles không dựa quyền DBA. Không mở thêm endpoint |
| Tests/runbook | Thay Windows ACL acceptance bằng SQL-principal/CLI acceptance dưới đây, giữ negative không secret-output và mail/batch tests độc lập |

## Acceptance theo boundary mới — chưa chạy

1. Fixture SQL Server riêng có hai login/user thực app/operator và migration principal; credential ngẫu nhiên/secret ngoài Git/log. Sai credential, caller tự đặt secret/role-name/path và app principal gọi CLI đều từ chối trước ghi. Connection mất/quyền thiếu/role null/migration thiếu guard bị chặn. Tests `EXECUTE AS` chỉ bổ sung, không thay CLI với connection login thật.
2. Dùng app login SQL trực tiếp thử INSERT Employee/Admin, UPDATE hash/status/duty Employee, đổi loại old/new, MERGE/multirow, thay grants/NhanVien/ledger/metadata hoặc giả audit quản trị đều bị SQL chặn và rollback. App không sửa/disable guard/role/column grants. Đối chứng customer HTTP register→worker→verify→login/refresh/logout/Profiles hoạt động với app grants tối thiểu; không mock quyền, không dùng sa/db_owner thay app.
3. CLI process thật bằng operator login: clean synthetic DB → bootstrap; legacy synthetic DB → migration → recovery → reset nhân viên legacy. Kiểm SQL account/hash/status/duty/grants/version, audit SQL identity và actor, ledger một lần, phiên/token cũ vô hiệu và HTTP login mới. Hai process cạnh tranh/rollback/retry giữ invariant, reset thiếu actor/sai quyền/Director/self/version target bị chặn. Service tests không thay process CLI.
4. CLI Windows trên máy hiện tại và CI Linux kiểm chức năng tương đương khi SQL test cấp phép; OS/user không quyết định quyền nghiệp vụ. Terminal nhập ẩn/handoff người nhận vẫn có giới hạn test cần ghi; không gọi stdin mock là handoff thật. CI cũ 44/44 chỉ là lịch sử trước sửa boundary.
5. App principal bị chặn trực tiếp ở SQL là điều kiện bắt buộc, không xóa gap CLI để báo đạt. Publish API/client không chứa operator credential/assembly. Không email thật, không VM/ACL hệ thống để ép pass.

## Runbook/quyền môi trường cần được phép riêng

DBA tạo hai DB synthetic clean/legacy, hai login/user giới hạn và principal migration riêng trong SQL test được chỉ định; không đổi quyền/role của app hoặc DB dùng chung. Nếu instance hiện tại không được phép cấp logins thì chưa thể chạy acceptance; không tự cài SQL/VM hoặc dùng sa như “operator” để báo đạt. Sau test revoke/drop đúng users/logins/DB synthetic do runId tạo, giữ evidence sạch; không reset ledger DB đang dùng để chạy lại. Secret thu hồi sau test.

Người quản trị backend nắm credential operator, API service chỉ nhận app credential; DBA giữ deployment credential, mapping DB và quyền cấp/thu hồi. Cùng người có thể đảm nhiệm ở môi trường học tập nhưng phải dùng credential/grants tách biệt, không lấy quyền DBA làm bằng chứng operator least privilege. Không cần tài khoản OS mới, SID riêng hay ACL Program Files. Secret ở đâu do môi trường quản trị quyết định; không lấy quyền đọc file caller chọn làm chứng cứ xác thực SQL.

Để triển khai tiếp: reviewer kiểm **một diff này** gồm quyền SQL/guards/tác động migration và acceptance; người dùng chấp thuận phần kỹ thuật rồi mới code phụ thuộc và xin/cấp quyền SQL test cụ thể. Hiện chưa sửa OS/SQL dùng chung, code bảo mật hoặc schema; chưa đủ nghiệm thu.

Nguồn: [Microsoft DENY object permissions](https://learn.microsoft.com/en-us/sql/t-sql/statements/deny-object-permissions-transact-sql?view=sql-server-ver17) (cảnh báo column GRANT), [SQL IS_ROLEMEMBER](https://learn.microsoft.com/en-us/sql/t-sql/functions/is-rolemember-transact-sql?view=sql-server-ver17) (context/membership/null, không thay effective permissions).
