# SQL boundary implementation — chưa đủ acceptance provisioning

Thiết kế `ad6a7f7` được người dùng chấp thuận cùng review độc lập. Không phải approval GitHub/thành viên. Windows checker/expected-secret gate và tests riêng của cơ chế đó đã bỏ; không có hai bypass song song. M1/batch/delivery/actor/reset/ledger giữ phạm vi cũ.

| Thiết kế | Implementation | Kiểm chứng hiện tại |
|---|---|---|
| SQL principal xác thực, không caller allowlist | SqlOperatorBoundary + operator Program: connection là credential SQL; server context/role/quyền/guard enabled+CALLER/marker/dictionary được kiểm, dbo/sysadmin/control/DDL bị từ chối; không Windows import/secret đối chiếu | Build; negative CLI connection lỗi được sanitize. Chưa thử operator/app login thật |
| Cưỡng chế app không sửa Employee/audit quản trị | `database/m1-sql-boundary-up.sql`: 2 triggers CALLER, old+new/multirow; action mapping adminBootstrapped/adminRecovered/employeeProvisioned/employeePasswordReset/accountStatusChanged/employeeStatusChanged/permissionsChanged; object/column grants và deny DDL/meta | DDL đã viết/rà cùng câu SQL code, **chưa apply hoặc xác nhận syntax/runtime bằng DB được provision**; không tự cấp roles/grants |
| EF tương thích DML triggers | TaiKhoan/SecurityAudit `UseSqlOutputClause(false)` để không OUTPUT trực tiếp vào table có trigger | Existing suite SQL trên DB synthetic schema cũ; chưa chứng minh grants mới |
| Actor app/reset/recovery bất biến | Administration/Recovery giữ transaction/lock/rowversion/hash/revoke, audit OperatorIdentity đọc ORIGINAL_LOGIN/USER_NAME từ SQL thay chuỗi caller | Service regression SQL đạt; không thay positive CLI hoặc trust boundary tests |
| Acceptance thực dưới grants | `TiemChung.BoundaryAcceptance` tách khỏi suite cũ: app HTTP, DML negative/multirow/MERGE, role/context, actual CLI app rejection | Build 0 warning/error; **chưa chạy** vì chưa được provision. Missing env fail, không skip. Positive CLI terminal/cạnh tranh/rollback có runbook, chưa được kiểm/không coi suite này đã hoàn tất toàn acceptance |

Suite `backend/TiemChung.slnx` Release trên LocalDB riêng hiện **42 passed/0 failed/0 skipped**; giảm từ 44 vì bỏ 2 tests Windows/expected-secret đã hết thiết kế. Suite cũ có service SQL/bootstrap/recovery/reset, mail/batch regression và HTTP, không apply script boundary, không tạo login/grants mới, không chứng minh least privilege. CI suite cũ dù xanh cũng không đóng acceptance mới. BoundaryAcceptance build riêng; chưa thêm vào default suite để tránh giả cần DB-owner/membership hoặc tự provision chưa được phép.

[Instance/DB/login/roles/grants/lệnh/apply/assert/cleanup cụ thể cần duyệt](m1-sql-boundary-provision-request.md). Khảo sát chỉ đọc service/LocalDB; chưa start SQLEXPRESS, chưa tạo login/roles/grants, chưa SQL dùng chung/OS/ACL/VM. CLI positive/grants và DDL runtime còn là gate môi trường. Không hạ gate/không production-ready.

Runtime secrets: M1_OPERATOR_CONNECTION là credential SQL launcher quản lý, không authority tự khai; M1_RECOVERY_MANIFEST là dữ liệu request bị kiểm SQL metadata/ledger, không recovery permission. Không password ở argv/log; CLI lỗi generic không in provider exceptions. API/client publish không nhận env operator hoặc operator assembly; chỉ operator launcher nắm credential vận hành. SQL operator credential owner/DBA/host admin nằm trong trust vận hành, không tuyên bố chống họ kiểm soát DB/host.
