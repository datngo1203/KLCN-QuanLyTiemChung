# Hai P1 M1 — implementation và giới hạn kiểm chứng

Người dùng chấp thuận contract operator/delivery tại `8a6a0e4`, gồm giới hạn dedup cùng sink instance/lifetime. Không phải approval GitHub/thành viên. Không schema mới, M2–M4, provider email thật hoặc thay OS/SQL grants chung.

## Finding → sửa → bằng chứng

| Finding | Code | Test thực tế |
|---|---|---|
| Operator tin env caller | ProtectedOperatorPolicy.cs; operator Program.cs lấy Known Folder Windows bằng native API, kiểm SID token/owner/DACL/reparse toàn chuỗi policy và binary, Integrated SQL; manifest từ policy | OperatorPromptTests: caller đặt env allowlist/secret/connection/path/ProgramData không vượt gate, không echo secret; ProtectedPolicyTests trên Windows dùng file thật với owner/DACL không phù hợp và path missing bị chặn. Linux chỉ kiểm refusal. Không mô phỏng policy để gọi đó là ACL thật |
| Outbox thiếu lifecycle | IdentityMailDispatcher.cs, Program.cs DI/BackgroundService, IdentityEndpoints.cs gate trước ghi, IdentityMailSink.cs SQL application lock/transaction | IdentityHttpSqlTests và MailLifecycleSqlTests khởi động host thật: register → worker → sink → confirm → login, không gọi drain thay lifecycle. Sink fail lần đầu, worker retry, shutdown readiness false; production synthetic flag vẫn 503 không account/outbox; thiếu marker/mode cũng 503 |
| Race/dedup | deliveryId là outbox.Id, SyntheticVerificationSink dedup nguyên tử theo instance | SQL trigger rollback sau accepted: cùng instance count=1; sink A/B độc lập tổng=2 cùng ID, SentAt null/payload còn trước retry và SentAt set/payload xóa sau commit; confirm một lần/account không tăng. Hai connection drain cạnh tranh shared sink count=1. Test drain trực tiếp ở ca transaction chỉ kiểm rollback/race, tách test lifecycle host |

Chạy local: `M1_SQL_TEST_CONNECTION` trỏ `(localdb)\KLCN_M1_Test`, SQL Server LocalDB riêng, DB synthetic tên riêng mỗi test. `dotnet test backend/TiemChung.slnx -c Release --no-restore`: **41 passed, 0 failed, 0 skipped**, build không warning/error. Test không gửi mail người thật. SHA code/HEAD và kết quả CI cuối ghi trong PR #33, không lấy 35/35/CI 9fa35ab thay thế.

## Giới hạn chưa kiểm và điều kiện môi trường

- Máy hiện tại không có policy/binary triển khai tại đường quản trị cố định với owner SYSTEM/Administrators và protected DACL toàn chuỗi. Không sửa ACL/grants chung để chạy positive. **Chưa kiểm đường CLI bootstrap/recovery/reset hợp lệ qua boundary mới**, terminal nhập ẩn, policy tamper trong đường trusted, cấu hình malformed bên trong boundary trusted hoặc SQL principal least-privilege thật. Tests service SQL recovery/reset cũ vẫn chạy, không thay bằng chứng CLI mới. Đường caller-owned/missing và env spoof là negative thực, không chứng minh positive deployment.
- Code fail-closed nghiêm ngặt: nếu ancestor Windows có owner/ACE/protection không đạt thì từ chối. Không tự sửa ACL hay nới policy vì tiện chạy. Cần quản trị môi trường triển khai xác nhận toàn chuỗi trước vận hành; chưa production-ready.
- SQL race/rollback đã kiểm, không hứa exactly-once xuyên sink/host độc lập hay restart. Retry không đánh dấu gửi khi sink fail; payload hết hạn xóa. Shutdown đã kiểm host dừng worker, chưa diễn tập kill-process mọi thời điểm commit.
- Synthetic sink chỉ bộ nhớ nội bộ DI, không endpoint lấy token, log hay provider mạng. Restart mất collection nhận; retry message chưa SentAt có thể chuyển lại. Readiness chỉ mở register/resend cho Development/Test + Synthetic + key hợp lệ + DB fixture marker. Production M1 luôn đóng intake 503, verify/login không bị đóng chỉ vì mail.

## Runbook thay thế cấu hình operator cũ

1. Admin triển khai binary không writable bởi caller, policy tại native default CommonApplicationData/KLCN090/IdentityOperator/policy.json; không dùng env ProgramData hoặc path argument. Không checkout/build từ thư mục user rồi coi đó là binary trusted.
2. Policy JSON fields: AllowedSids (SID thật), Server, Database, ExpectedSecret (base64 32 byte), Manifest (RecoveryManifest đã bind hoặc null nếu không recovery). Cấu hình qua công cụ quản trị ngoài Git; không stdout/transcript, không ví dụ secret thật. DACL protected; owner SYSTEM/Administrators; mọi ancestor và file không được principal khác ghi/delete/change permissions/owner; symlink/reparse/ACE không hiểu bị chặn. Không nhận policy override trong binary.
3. Dùng principal OS vận hành riêng với SQL Integrated Security trong đúng DB, TLS chứng chỉ hợp lệ; không TrustServerCertificate, không sysadmin/dbcreator. Migration dùng principal riêng. App SQL runtime không được cấp quyền operator/DDL. Không tự sửa quyền chung bằng tool của agent.
4. Bootstrap/recovery nhập secret ẩn so với policy bảo vệ, recovery dùng manifest từ policy; các lệnh thường vẫn xác thực actor DB. Không tạo lượt recovery mới. Các bước ID/RowVersion/handoff reset giữ theo runbook reset.
5. Boundary không chống OS/DB administrator, người chiếm principal vận hành hoặc sửa binary/policy. SQL permissions phải bảo vệ cả đường truy cập trực tiếp; CLI không thể bảo vệ dữ liệu trước chủ thể đã có quyền SQL tương ứng.

CI Linux không kiểm positive Windows ACL. Cần reviewer kiểm lại implementation và yêu cầu triển khai trên môi trường OS/SQL được quản trị trước nghiệm thu; chưa approve/merge.
