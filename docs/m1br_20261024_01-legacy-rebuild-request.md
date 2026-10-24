# Xin phép tái tạo riêng fixture legacy generation 2

## Lý do và giới hạn

Người dùng không nhớ mật khẩu thực tế nhập khi CLI Recover thành công. Chẩn đoán terminal nhận đúng chuỗi đề xuất (23 ký tự, 0 control), nhưng PasswordHasher backend xác nhận không khớp hash; actor đủ quyền/Active/Technical, reset bị từ chối trước target input. Không có bằng chứng lỗi phân quyền hoặc paste transport hiện tại; không đổi code bảo mật, hash hoặc quyền để vượt từ chối. Ledger recovery cũ đã dùng, không xóa/mở lại ledger hoặc quota.

Quyền provisioning cũ yêu cầu dừng khi tên đã tồn tại và chỉ cleanup sau giữ evidence. Vì vậy **DROP/tạo lại fixture đang có cần người dùng chấp thuận riêng**. Tài liệu/script này mới chuẩn bị; chưa backup, DROP, CREATE, RESTORE hoặc đổi manifest local.

## Chính xác phạm vi xin phép

- Instance duy nhất `.\SQLEXPRESS`; thay **chỉ** DB synthetic `KLCN_M1_Test_Legacy_m1br_20261024_01` đang được run tạo. DB Boundary và tài khoản bootstrap giữ nguyên.
- Giữ nguyên hai SQL login/password/DPAPI secret; tạo lại đúng mapped users/m1_app/m1_operator/grants/guards/FixtureOnly trong DB legacy mới. Không thêm login/quyền server/DDL cho runtime hoặc đổi instance/service/sa/TCP/firewall/OS/ACL/trust.
- Backup SQL `COPY_ONLY,CHECKSUM,NOINIT`, file mới chưa tồn tại, tại thư mục backup SQL có sẵn: `C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\KLCN_M1_Test_Legacy_m1br_20261024_01_before_g2.bak`. Không tạo thư mục/đổi ACL/cấu hình backup. Nếu không ghi được thì dừng trước DROP. Backup chứa dữ liệu synthetic/hash/session, **không Git hoặc log công khai**; giữ nguyên để rollback, chưa xin quyền xóa backup này.
- Sau `RESTORE VERIFYONLY ... WITH CHECKSUM` đạt, ghi record trước write và kiểm lại không phiên khác; DROP/CREATE đúng DB legacy, không SINGLE_USER/ROLLBACK IMMEDIATE/kill. Migrate InitialCreate → seed 3 nhân viên/2 legacy accounts giả với hash không xác thực → M1 → apply boundary DDL đã duyệt, users/grants/guards/marker. Không gán trước mật khẩu Admin đã biết bằng SQL.
- DB mới có RecoveryDatabaseId mới và ledger trống tự sinh từ migration, marker TestGeneration=2; đây là **fixture khác**, không phải reset quota DB cũ. Giữ snapshot/audit/ledger/CLI exit của generation1; không trình bày lần recovery mới là retry idempotent của DB cũ.
- Archive manifest local `recovery.json` sang tên unique cùng thư mục ngoài Git, không overwrite; launcher sẽ tạo manifest mới gắn DB identity/version mới khi Recover. Không reset OperationId/validity hoặc biến manifest thành quyền vận hành.

## Script / lệnh cụ thể

`scripts/m1br-rebuild-legacy.ps1` mặc định `Check`, chỉ đọc. Apply/Rollback chỉ chạy sau khi được phép phạm vi trên, bằng Windows deployment credential quản trị hiện có; không cần elevated OS token cho SQL DDL/backup nếu SQL quyền hiện có đáp ứng. SQL runtime credential không được dùng provision.

```powershell
# Đã chạy: chỉ đọc, không thay fixture
.\scripts\m1br-rebuild-legacy.ps1 -Action Check

# CHƯA CHẠY — chỉ sau người dùng chấp thuận thay fixture này
.\scripts\m1br-rebuild-legacy.ps1 -Action Apply
```

Preflight so DB ID/create_date/owner/run marker với inventory g1; 2 DB run duy nhất/0 phiên khác; sysadmin deployment/Mixed Mode đúng instance; backup path từ server property, chưa có file. Stop nếu trạng thái khác, không tự xử lý đối tượng ngoài run. Record execution ở `docs/m1br_20261024_01-legacy-rebuild.json`; inventory g1 giữ riêng tại [SQL evidence g1](m1br_20261024_01-sql-evidence.g1.json).

Apply thành công rồi kiểm SQL grants/actual login và refresh inventory generation2 trước acceptance. Chỉ sau đó hướng dẫn người dùng **một lệnh Recover**, nhập mật khẩu test gõ tay và kiểm ngay hash/actor từ sổ local trước Reset. Chưa gọi recovery/reset/token/login/CLI concurrency/rollback PASS. Các tests 21/21 cũ vẫn chỉ là evidence của HEAD đã chạy/generation1.

## Rollback đã chuẩn bị

Khi Apply đã hoàn tất nhưng muốn trả fixture cũ, script xác minh replacement RecoveryDatabaseId/DB ID/create_date/owner và 0 phiên khác, lưu snapshot hiện tại trước restore:

```powershell
# CHƯA CHẠY — chỉ khi rollback nằm trong chấp thuận và fresh checks đạt
.\scripts\m1br-rebuild-legacy.ps1 -Action Rollback
```

Restore đúng backup verified về đúng DB legacy (`RESTORE DATABASE ... WITH REPLACE,CHECKSUM`), không forced disconnect. Xác minh RecoveryDatabaseId trở về bản cũ; ledger consumed/hash cũ được giữ, mật khẩu cũ vẫn chưa biết. Không tuyên bố rollback làm actor dùng được.

Nếu Apply lỗi giữa DROP/migration trước record `Applied=true`, dừng, giữ record/backup và trạng thái thực; script Rollback tự từ chối trạng thái partial chưa xác minh. Lệnh khôi phục cụ thể sau khi quản trị kiểm đúng target/owner/no sessions và file backup verified: `RESTORE DATABASE [KLCN_M1_Test_Legacy_m1br_20261024_01] FROM DISK=@path WITH REPLACE,CHECKSUM` với parameter chỉ trỏ backup cố định trên. Không tự chạy restore trước khi đối chiếu trạng thái partial; không mở quyền/đổi config để che lỗi. Backup chưa được restore thử; VERIFYONLY không thay bằng chứng restore thực.

## Kiểm đã chạy ở lượt chuẩn bị

- PowerShell parser đạt; `-Action Check` kết nối SQL thực read-only đạt: 2 DB run/0 phiên khác, đúng DB identity/owner/marker và backup filename chưa tồn tại. Đã sửa đọc xp_fileexist kiểu byte bằng Convert.ToInt32 trong checker.
- Chỉ prepare script/check/docs; **chưa Apply/Rollback/backup/migration hoặc suite SQL/auth mới**. Không nâng gate hay gọi PASS phần chưa chạy. Không API/schema/contract/M2 thay đổi. AGENTS/status từ chat khác giữ riêng.
