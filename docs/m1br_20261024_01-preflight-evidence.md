# Run m1br_20261024_01 — Windows-only authentication blocks provisioning

**Đã vượt blocker lịch sử:** người dùng apply Mixed Mode, preflight encrypted local exception đạt; đã provision đúng run và chạy boundary login thật. [Evidence acceptance mới và gate còn mở](m1br_20261024_01-acceptance.md). Lịch sử dưới đây giữ truy vết.

## Quyền mới và bước elevated còn chặn

Người dùng đã cho phép đổi đúng LoginMode/restart/rollback có kiểm soát; agent đọc lại đúng instance, 0 DB/0 phiên khác, sa disabled, Running nhưng token không elevated. Chưa đổi cấu hình. Xem [baseline, script Administrator apply/rollback và xử lý lỗi](m1br_20261024_01-auth-mode-runbook.md). Các giới hạn chưa cho đổi mode dưới đây là lịch sử trước quyền mới. Provisioning vẫn chờ apply và preflight đạt; không đối tượng mới hoặc acceptance PASS.

## Kết quả mới: ngoại lệ TLS test được người dùng cho phép

- Chỉ run `m1br_20261024_01` tới `.\SQLEXPRESS` trên máy này: người dùng cho phép `sqlcmd -N -C` (hoặc `Encrypt=True;TrustServerCertificate=True`). Encryption vẫn bắt buộc; **không xác minh chứng chỉ đạt**, không đổi trust store/chứng chỉ/instance hoặc mặc định production. Script preflight mặc định vẫn `-N` không `-C`.
- Chạy read-only `sqlcmd -S '.\SQLEXPRESS' -d master -E -N -C -l 5 -b -i scripts/m1br-preflight.sql`: `IntegratedOnly=1`, deployment sysadmin=1, encryption `TRUE`, auth scheme `NTLM`, hai counts DB/login trùng tên đều 0. Exit 1, SQL error 51030: SQL authentication unavailable. Dừng trước provisioning; không đổi Mixed Mode hoặc restart.
- Sửa truy vấn server permissions trong preflight từ class `'SERVER'` sang `NULL` theo chữ ký `HAS_PERMS_BY_NAME` cho server-level securable; truy vấn cũ trả NULL. Chạy lại SQL read-only sau sửa: `ALTER ANY LOGIN=1`, `CREATE ANY DATABASE=1`; auth mode vẫn chặn. Không thay quyền SQL thực.
- Không tạo DB/login/user/grant/fixture; chưa BoundaryAcceptance/CLI. Service xác nhận Running, không stop vì chưa xác minh tác động công việc khác. Cần instance test hiện hữu có SQL authentication được phép để chạy acceptance bằng app/operator SQL login thật; không dùng deployment/sysadmin thay. Hiện không có quyền bật Mixed Mode/restart hay chuyển instance nên dừng đúng bước.
- Manifest phân biệt encryption đã kiểm với certificate validation chưa đạt. Lượt này chỉ SQL preflight read-only/JSON/diff; không SQL/auth Release hoặc CLI PASS mới.

Ngày 24/10/2026 người dùng cho phép start service và provisioning **có điều kiện preflight đạt**, chỉ hai DB/hai login đã liệt kê. Không phải quyền đổi instance config/Mixed Mode/ACL/OS user hoặc bỏ TLS. Thiết kế M1 không thay.

## Kết quả mới sau khi người dùng start service

- Người dùng báo script start trả Running; agent xác nhận `MSSQL$SQLEXPRESS: Running`. HEAD trước lượt `58a3ee1e413832d36e83248fc90edde70a32ab5d`, working tree sạch.
- Chạy đúng `scripts/m1br-preflight.ps1`: SQLCMD ODBC Driver 17, `.\SQLEXPRESS`, Integrated Authentication `-E`, TLS `-N`, không `-C`. Kết nối thất bại: `SSL Provider: The certificate chain was issued by an authority that is not trusted.` Sau đó `Client unable to establish connection`; script dừng, không retry nới TLS.
- Chưa thiết lập session SQL: auth mode, effective deployment permissions, TLS hợp lệ và collision của hai DB/hai login đều **chưa xác minh**. Không suy từ lỗi chứng chỉ ra auth mode hoặc quyền SQL.
- Không tạo DB/login/user/role/grant/guard/fixture; manifest created vẫn rỗng. Không chạy BoundaryAcceptance hoặc CLI bootstrap/recovery/reset. Không chạy lại SQL/auth suite cho thay đổi tài liệu này; kết quả cũ không thay acceptance.
- Service giữ Running. Chưa xác minh ảnh hưởng tới kết nối/công việc khác nên không tự stop. Không đổi instance config, chứng chỉ/trust store, ACL hoặc quyền SQL.
- Điều kiện còn thiếu: kết nối TLS có chuỗi chứng chỉ được máy xác thực tin cậy tới đúng instance. Cần quản trị cung cấp phương án/endpoint TLS hợp lệ trong môi trường được phép; hiện chưa có quyền thay trust/certificate/configuration. Không dùng `TrustServerCertificate` hoặc tắt encryption để vượt preflight. Sau khi điều kiện này được giải quyết có thẩm quyền, chạy lại toàn bộ preflight trước mọi provisioning.

## Lịch sử trước khi người dùng start service

- HEAD code trước lượt: `3c1927a`, working tree sạch. Windows process `LAPTOP-DAT\DAT`, token không elevated.
- `Get-Service 'MSSQL$SQLEXPRESS'`: Stopped trước/sau. `Start-Service -ErrorAction Stop` bị từ chối: `Cannot open 'MSSQL$SQLEXPRESS' service on computer '.'`. Không tự nâng quyền hoặc đổi service permissions.
- sqlcmd hiện hữu tại SQL Server Client SDK ODBC 170. Chưa kết nối `.\SQLEXPRESS`; chưa xác nhận auth mode/TLS/grants/tên DB/login. Không dùng LocalDB/DBA login thay acceptance app/operator.
- [Manifest đối tượng run](m1br_20261024_01-manifest.json): mọi danh sách created rỗng. Không có cleanup DB/login; service vẫn stopped đúng trạng thái ban đầu, không tự stop session/công việc khác.

## Bước start đã được người dùng thực hiện

Mở **PowerShell bằng Run as administrator**, dùng đúng checkout này:

```powershell
& 'D:\Projects\KLCN-QuanLyTiemChung\scripts\m1br-start-sqlexpress.ps1'
```

Script chỉ start service hiện có nếu đang Stopped, chờ Running và in trạng thái. Không cài/đổi startup/restart/Mixed Mode/ACL/SQL/grants. Tác động là instance bắt đầu chạy; chưa tạo DB/login. Không tự stop sau đó nếu chưa kiểm kết nối/công việc khác. Nếu script fail, giữ lỗi gốc và dừng, không thay service ACL.

Sau khi service Running, có thể chạy preflight read-only bằng Windows credential quản trị hiện có (không credential mới):

```powershell
& 'D:\Projects\KLCN-QuanLyTiemChung\scripts\m1br-preflight.ps1'
```

`-E -N` kiểm SQL auth mode, TLS encrypted/validated, quyền deployment và tên run chưa tồn tại. Không `-C`, không password argv/log. Nếu cert không hợp lệ/auth mode chỉ Windows/quyền thiếu/tên đã có thì dừng đúng bước, không đổi config/restart/trust fallback. Preflight script không provision dù thành công; agent sẽ đối chiếu kết quả và tiếp tục đúng run đã cho phép.

Đây là bước môi trường còn chặn, không xin duyệt lại thiết kế hoặc nới gate. Các ca BoundaryAcceptance/CLI/bootstrap/recovery/reset/grants chưa chạy; tests cũ không thay evidence. Provision/apply/assert/cleanup sau preflight vẫn theo [phạm vi đã duyệt](m1-sql-boundary-provision-request.md).
