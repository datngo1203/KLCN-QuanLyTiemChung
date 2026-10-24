# m1br_20261024_01 — đổi LoginMode có kiểm soát

Người dùng cho phép chỉ `.\SQLEXPRESS`: LoginMode 1 → 2 và restart `MSSQL$SQLEXPRESS`, sau test/cleanup khôi phục 1 và restart nếu không có công việc khác. Không bật/reset sa, TCP/firewall/remote access, trust store, service permissions/ACL hoặc config khác. Không đổi thiết kế Identity.

## Baseline chỉ đọc trước apply

HEAD trước lượt `ec7afa9c398896351bd176ed2582c27cc1c4ed22`, tree sạch. Agent xác nhận `LAPTOP-DAT\SQLEXPRESS`, instance `SQLEXPRESS`, IntegratedOnly=1, **0 DB người dùng / 0 phiên người dùng khác**, `sa` disabled=1, service Running. Windows admin kết nối master bằng `-E -N -C`, encryption TRUE/auth NTLM. Ngoại lệ certificate chỉ cho local test này, không chứng minh certificate validation đạt. Process agent không elevated; **chưa đổi mode/restart**.

## Apply — người dùng chạy PowerShell Administrator

```powershell
Set-Location 'D:\Projects\KLCN-QuanLyTiemChung'
& '.\scripts\m1br-auth-mode.ps1' -Action Apply
```

Script đọc mapping SQLEXPRESS tại registry 64-bit, chỉ chấp nhận instance ID `MSSQL<version>.SQLEXPRESS`; kiểm lại SQL server identity đúng máy hiện tại, mode, existing admin, encryption, sa disabled, 0 DB/phiên khác và chưa có hai login run. Kiểm không có dependent service đang chạy. Nếu có DB/phiên khác thì dừng trước write/restart để hỏi mục đích; không kill phiên. Người vận hành giữ cửa sổ maintenance không có workload mới trong khoảng kiểm tra/restart.

Baseline registry LoginMode/service/SQL/sa được lưu **trước write** tại `docs/m1br_20261024_01-auth-mode-evidence.json` (không secret). Nếu file đã tồn tại thì không overwrite/reapply. Chỉ Set-ItemProperty LoginMode=2 trên mapping đã xác minh và Restart-Service đúng tên không Force. Sau restart kiểm Windows admin login, encryption, IntegratedOnly=0 và sa không đổi. Sau thành công agent chạy lại preflight read-only `-E -N -C` và collision checks trước provisioning hai DB/hai login đã được phép; script này không provision.

## Rollback sau cleanup / khi apply postcheck lỗi

Sau đóng toàn bộ kết nối run và cleanup đúng hai DB/hai login có manifest, kiểm không còn DB/phiên khác rồi chạy Administrator:

```powershell
& 'D:\Projects\KLCN-QuanLyTiemChung\scripts\m1br-auth-mode.ps1' -Action Rollback
```

Script yêu cầu baseline đúng run/path/mode gốc 1; 0 DB người dùng, 0 phiên khác, không còn hai login run, sa giữ nguyên. Cho phép registry/runtime lệch 1/2 sau apply lỗi để khôi phục đúng LoginMode=1, restart và kiểm IntegratedOnly=1/Windows admin/encryption/sa. Không drop đối tượng hoặc ngắt phiên để ép rollback pass. Nếu có DB/phiên chưa rõ thì dừng hỏi trước.

Nếu restart/kết nối lỗi: dừng, giữ evidence `LastFailure`, không thử account/grants/config khác. Nếu service Stopped sau lỗi, quản trị kiểm trạng thái rồi chỉ start lại service hiện có:

```powershell
Get-Service -Name 'MSSQL$SQLEXPRESS'
Start-Service -Name 'MSSQL$SQLEXPRESS' -ErrorAction Stop
```

Chỉ chạy Start-Service khi xác nhận Stopped và không có thay đổi/công việc quản trị khác; không restart mù. Khi Windows admin kết nối lại được, chạy Rollback có các checks trên. Nếu vẫn không kết nối/start được, báo quản trị với baseline và lỗi; không sửa registry/ACL/quyền khác hoặc tuyên bố khôi phục. Trạng thái service ban đầu của toàn run là Stopped, nhưng trước đổi mode đang Running; việc stop cuối run chỉ sau xác minh không ảnh hưởng công việc khác theo quyền đã cấp.

## Kiểm chứng thực tế lượt chuẩn bị

Chỉ baseline SQL chỉ đọc và kiểm cú pháp script/diff/JSON; apply/restart/rollback chưa chạy do thiếu elevated token. Không gọi build/service tests là CLI hoặc least-privilege acceptance. Chưa tạo DB/login; SQL/auth Release/BoundaryAcceptance/CLI chưa chạy mới ở lượt này.
