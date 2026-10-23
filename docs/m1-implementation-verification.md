# M1 implementation — đang kiểm chứng

Sau review hai P1 tại 9fa35ab và chấp thuận contract 8a6a0e4: xem [kiểm chứng implementation mới và giới hạn ACL](m1-p1-implementation-verification.md). Operator env cũ và ghi nhận 35/35 bên dưới là lịch sử, không thay bằng chứng sửa P1.

Nhánh `codex/m1-implementation`, dependency #32 (đặc tả kỹ thuật `20a9a72b`, gate người dùng `053ee86`). Không M2–M4/client. Owner không đổi; cần review độc lập implementation trước merge.

## Chạy local

SQL Server thật đã dùng: LocalDB 17.0.4025.3, instance riêng `KLCN_M1_Test`. Suite tạo DB `KLCN_M1_Test_<guid>` synthetic, giữ DB để chẩn đoán; không migrate DB chung. Connection qua biến môi trường, không Git.

```powershell
$env:M1_SQL_TEST_CONNECTION='Server=(localdb)\KLCN_M1_Test;Integrated Security=true;TrustServerCertificate=true'
dotnet test backend/TiemChung.slnx --configuration Release
```

Thiếu SQL là test failure, không skip. CI khởi động SQL Server 2022 container riêng, secret ngẫu nhiên được mask mỗi run. CI code `4c9af44` đã success; HEAD cuối sau test reset mở rộng phải kiểm riêng, không lấy CI trước thay thế.

Migration bổ sung `20261023073816_M1Identity`, không sửa InitialCreate. Sau backup/preflight và review DDL, chạy qua EF CLI với connection ngoài Git; API không auto migrate:

```powershell
dotnet ef database update --project backend/src/TiemChung.Infrastructure --startup-project backend/src/TiemChung.Api
```

ConnectionStrings:TiemChung, Identity:SigningKey và Identity:MailEncryptionKey phải cấu hình bằng secret store/user-secrets; hai key base64 256 bit độc lập. Không token/password trong URL/log. Operator build/run riêng tại `backend/tools/TiemChung.IdentityOperator`; connection/secret/OS allowlist qua M1_OPERATOR_CONNECTION, M1_OPERATOR_SECRET, M1_OPERATOR_ALLOWED_IDENTITIES. Nhập mật khẩu/secret ẩn, không argv. Recovery manifest sau migration trong maintenance theo contract. API publish không tham chiếu operator/test assembly.

## Bằng chứng hiện có và giới hạn

Code/test SHA `e477bac1b2d55d6ad4d5afa7ab3abd49968b5784`: Release suite chạy lại 35 passed, 0 failed, 0 skipped trên SQL Server LocalDB 17.0.4025.3. Kết quả gồm code reset mới, không kế thừa kết quả 33/33 cũ. API Release publish tại SHA này không có assembly Operator/Tests. Diff/link đã kiểm; CI HEAD cuối được ghi trong PR/bàn giao sau khi hoàn tất, không suy từ local tests.

## Đối chiếu bằng chứng theo nhóm checklist

| Nhóm | Bằng chứng đã chạy | Giới hạn |
|---|---|---|
| SQL upgrade/recovery | IdentityRecoverySqlTests, IdentityPreflightSqlTests, IdentityBackupSqlTests | Backup/restore DB synthetic riêng; không diễn tập DB production |
| Constraint/concurrency | IdentitySqlTests: register/CCCD race, rowversion stale update, FK Restrict, duty/type, email 254 | Không suy ra mọi dữ liệu legacy đều ánh xạ được |
| Phiên/revoke/audit | IdentitySqlTests, IdentityWriteRevokeSqlTests, audit-trigger HTTP test | Revoke-write test dùng final auth/transaction path, không browser E2E |
| Identity/mail | IdentityMailSqlTests: rollback outbox, resend/used/expired/confirm race; IdentityTokenTests | Mail chỉ synthetic sink, không gửi người thật |
| Profiles/transport | IdentityHttpSqlTests: sáu title, scope A/B, create/PUT, CSRF/mixed transport, dependency 503, length/page/search; ProfileDateTests midnight Saigon | Không chứng minh client/browser E2E |
| Operator/reset/publish | IdentityAdministrationSqlTests, EmployeeResetSqlTests, OperatorAuthenticationTests, OperatorPromptTests; API Release publish đã kiểm | Child process kiểm stdin redirect; chưa kiểm người nhận thật hoặc terminal E2E |
| Coordinator | IdentityCoordinatorContractTests | Chỉ mock contract, không client M2 hoặc browser E2E |

Các kết quả trên không thay kết quả CI HEAD cuối hoặc review độc lập. Chưa đánh dấu toàn hàng checklist PASS khi một hàng có tổ hợp chưa chạy.

### Reset đã chấp thuận và triển khai

Ngày 23/10/2026 người dùng chấp thuận contract reset `f30641b` cùng báo cáo review độc lập qua ủy quyền nguyên văn từ chat điều phối; đây không phải approval GitHub/thành viên. Lệnh offline đã triển khai, không schema mới hoặc endpoint. [Runbook reset](m1-reset-runbook.md). SQL tests có legacy credential sau Admin recovery, hai reset cùng version, stale actor/target, sai ID/self/credential, password mới login, Locked vẫn chặn và ledger giữ nguyên.

Reset mở rộng: Release suite 35/35 chạy lại trên SQL LocalDB sau bổ sung EmployeeResetSqlTests và OperatorPromptTests. Assert trực tiếp Reset cho actor Locked/Director/thiếu quyền/nhân viên inactive, Customer target; reset giữ target inactive, status/duty/title/FK/quyền/ledger; access/refresh active trước reset bị chặn sau commit, actor session không bị revoke. Fault injection tại update hash, revoke credential và audit rollback hash/version/session/credential nguyên vẹn. Child process stdin redirect bị từ chối/không echo synthetic secret marker; không giả terminal E2E. Các ca nằm trong test method nhiều assert, không suy theo số lượng test. CI phải xác nhận HEAD cuối; bản handoff là operator offline, không self-service M2.

Lịch sử: thiếu contract reset từng chặn implementation; bổ sung `f30641b` đã được review và người dùng chấp thuận trước code `4c9af44`. Không còn gate chờ contract reset; review implementation/CI vẫn độc lập. Các ghi nhận pending trong status cũ là lịch sử, không phải trạng thái hiện hành.
