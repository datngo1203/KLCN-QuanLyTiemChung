# Operator positive-path — yêu cầu môi trường test có thẩm quyền

**Đính chính sau khảo sát ACL máy hiện tại:** không còn coi “VM/root ACL phải pass checker cũ” là yêu cầu môi trường. [Diff contract ACL/path](../contracts/m1-operator-acl-scope-review.md) phân biệt sibling creation và takeover, đề nghị app subtree cố định dưới Program Files và kiểm ancestor theo mutation thực; chờ review trước code. Chưa provision, không đổi root/ProgramData/ACL/OS user/SQL chung. Bảng yêu cầu bên dưới giữ truy vết thiết kế cũ, không được thi hành nguyên trạng.

Chưa chạy CLI positive. Khảo sát chỉ đọc trên máy hiện tại: token Windows không elevated, policy cố định chưa tồn tại; `C:\ProgramData` owner SYSTEM, DACL protected. Chưa kiểm/cấp quyền SQL deployment principal. Không thay ACL/SQL/policy hiện hữu, không hứa kết quả service test là CLI PASS.

## Danh sách cần quản trị xác nhận trước thao tác

Ưu tiên VM Windows test riêng/snapshot, SQL Server test riêng với TLS hợp lệ. Không dùng root ACL hoặc DB dùng chung để vượt chặn. Nếu toàn chuỗi đường dẫn không đạt boundary, dừng và chuẩn bị VM, không sửa root trên máy đang dùng chung.

| Đối tượng | Đề nghị cụ thể, chưa thực hiện |
|---|---|
| Binary | Publish operator Release tại `C:\ProgramData\KLCN090\IdentityOperator\bin`; quản trị kiểm hash theo HEAD được review và mọi dependency/runtime; caller không được sửa |
| Policy | Native default CommonApplicationData `C:\ProgramData\KLCN090\IdentityOperator\policy.json`; phải là đường cố định thực tế do API Known Folder trả, không env/argument override |
| Owner/DACL | Toàn chuỗi root → ProgramData → KLCN090 → IdentityOperator → bin/file: owner SYSTEM hoặc Administrators, DACL protected theo code, không symlink/reparse, không write/delete/change-DACL/take-owner cho principal khác. Chỉ SYSTEM/Administrators FullControl, SID vận hành Read/ReadExecute. Cần quản trị kiểm từng ACE thực và tổ chức deployment phù hợp; không tự xóa ACE root/mục dùng chung |
| Principal OS | Tài khoản test riêng do quản trị cấp, ví dụ `KLCN_M1_OperatorTest`; phải lấy SID từ token thực. Không tự tạo hoặc giả SID, không gán quyền cho DAT hiện tại |
| SQL | Integrated login cho đúng SID test, chỉ user trong hai DB synthetic; quyền DML cần thiết trên bảng M1/NhanVien/QuyenHan và quyền đọc migration metadata, applock trong DB. Không sysadmin/dbcreator/db_owner, không DDL. Principal migration/DBA riêng tạo DB/migrate và chuẩn bị legacy fixtures; app principal riêng không được ghi operator/DDL. Quản trị xác nhận effective grants và test deny trực tiếp bằng app principal |
| DB | Hai DB riêng: `KLCN_M1_Operator_Clean_<runId>` và `KLCN_M1_Operator_Legacy_<runId>`; dữ liệu tên/email `example.invalid`, không bệnh nhân thật. DBA tạo sạch và fixture theo baseline; không migrate DB chung |
| Secret/policy | ExpectedSecret base64 32 byte ngoài Git; nhập secret/password qua terminal ẩn. Policy chứa Server/Database/AllowedSids/ExpectedSecret/Manifest; admin thay đích giữa hai DB test qua policy bảo vệ, không caller path. Không dump JSON hoặc token/credential vào log/argv/transcript |

Tác động: cài binary/policy test, tạo principal test và hai DB riêng. Cần quyền quản trị OS cho provisioning/ACL, DBA cho DB/login/grants và người được phân công vận hành principal test; các quyền này chưa có. Đây không phải yêu cầu cấp quyền không giới hạn cho agent.

## Trình tự CLI và kiểm chứng sau khi quản trị cho phép

1. Ghi runId, snapshot VM/DB và manifest danh sách file/grants được tạo. Kiểm ACL/owner toàn chuỗi read-only trước chạy, hash binary tương ứng HEAD. Không ghi secret.
2. DBA migrate clean DB, seed một NhanVien Admin thật trong fixture; policy bind clean DB. Đăng nhập OS bằng principal test; chạy `dotnet C:\ProgramData\KLCN090\IdentityOperator\bin\TiemChung.IdentityOperator.dll bootstrap-admin`. Secret/employeeId/username/password nhập prompt, không argv. Assert account/hash/quyền/audit từ DB bằng kênh read-only; bootstrap lần hai bị từ chối.
3. DBA tạo legacy DB ở InitialCreate với Admin và nhân viên giả lập dùng hash legacy, migrate M1. Giữ maintenance; admin bind manifest đúng DB ID/mốc/migration/target/RowVersion mới và secret trong policy. Chạy cùng binary `recover-upgraded-admin` bằng principal OS test. Assert target/status/duty/quyền/ledger một lần, audit OS actor và mọi session/credential cũ revoked; thử lại cùng manifest không nhân ledger, manifest mới bị chặn. Không mở recovery trên DB khác.
4. Đọc target Employee ID/RowVersion từ legacy DB; chạy binary `reset-employee-password`, xác thực actor bằng credential sau recovery, nhân viên fixture nhập password mới trực tiếp. Assert đúng account/FK/hash/version tăng một lần, status/duty/quyền không đổi, audit/ledger giữ quy tắc. Tạo phiên trước reset bằng API host test trỏ đúng DB, sau reset access/refresh cũ bị chặn và login mới thành công; không lấy token bằng output log.
5. Ghi exit code, SHA, ID thao tác/audit/ledger, count/invariant và kết quả auth đã loại token/password; không ghi hash/password. CLI terminal/boundary chỉ được PASS khi chính các bước này đã chạy, không dùng Linux CI hay service tests thay.

## Khôi phục và phần còn chặn

Sau test, quản trị thu hồi đúng login/grants/account test và policy/binary mới theo manifest, xóa hai DB synthetic sau khi giữ evidence sạch hoặc phục hồi snapshot VM. Không xóa/ghi đè policy có trước, không reset recovery ledger để chạy lại; chạy mới dùng DB/runId mới. Mọi cleanup cần kiểm đúng absolute path/DB và ownership, không wildcard/recursive delete root chung.

**Còn chặn:** cần quản trị chỉ định/cấp môi trường Windows test với quyền provisioning OS/DB giới hạn và phê duyệt danh sách trên, hoặc tự provision rồi cung cấp phiên operator hợp lệ. Chưa có thẩm quyền sửa `C:\ProgramData`/ancestor ACL hay cấp SQL principal trong môi trường hiện tại. Batch fix/tests/runbook tiếp tục độc lập; M1 chưa đủ nghiệm thu khi positive CLI chưa có bằng chứng.
