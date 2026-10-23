# M1 implementation — đang kiểm chứng

Nhánh `codex/m1-implementation`, dependency #32 (đặc tả kỹ thuật `20a9a72b`, gate người dùng `053ee86`). Không M2–M4/client. Owner không đổi; cần review độc lập implementation trước merge.

## Chạy local

SQL Server thật đã dùng: LocalDB 17.0.4025.3, instance riêng `KLCN_M1_Test`. Suite tạo DB `KLCN_M1_Test_<guid>` synthetic, giữ DB để chẩn đoán; không migrate DB chung. Connection qua biến môi trường, không Git.

```powershell
$env:M1_SQL_TEST_CONNECTION='Server=(localdb)\KLCN_M1_Test;Integrated Security=true;TrustServerCertificate=true'
dotnet test backend/TiemChung.slnx --configuration Release
```

Thiếu SQL là test failure, không skip. CI khởi động SQL Server 2022 container riêng, secret ngẫu nhiên được mask mỗi run. Chưa có kết quả GitHub CI implementation tại lúc viết.

Migration bổ sung `20261023073816_M1Identity`, không sửa InitialCreate. Sau backup/preflight và review DDL, chạy qua EF CLI với connection ngoài Git; API không auto migrate:

```powershell
dotnet ef database update --project backend/src/TiemChung.Infrastructure --startup-project backend/src/TiemChung.Api
```

ConnectionStrings:TiemChung, Identity:SigningKey và Identity:MailEncryptionKey phải cấu hình bằng secret store/user-secrets; hai key base64 256 bit độc lập. Không token/password trong URL/log. Operator build/run riêng tại `backend/tools/TiemChung.IdentityOperator`; connection/secret/OS allowlist qua M1_OPERATOR_CONNECTION, M1_OPERATOR_SECRET, M1_OPERATOR_ALLOWED_IDENTITIES. Nhập mật khẩu/secret ẩn, không argv. Recovery manifest sau migration trong maintenance theo contract. API publish không tham chiếu operator/test assembly.

## Bằng chứng hiện có và giới hạn

Suite hiện tại 19 test đạt trên working tree ngày 23/10/2026; gồm unit và SQL/HTTP. Recovery có upgrade Admin cũ, stale-version, audit-trigger failure rollback, hai operator, retry cùng manifest và operation mới sau khóa lại. Refresh hai connection một 200/một 409, replay và hai thứ tự rotation/logout. HTTP có sáu title, Admin XemHoSo, PUT bất biến, account locked, scope A/B và fixture Production/marker negative. Không dùng số test này để tuyên bố toàn checklist đạt.

Cập nhật working tree: suite 21/21 đạt trên LocalDB; đã thêm audit-trigger rollback POST Profiles/refresh (không hồ sơ dở dang, không mất nhánh token active), bootstrap/self-change/Director negative và mail sink retry/resend/used token. CI `670b599`: backend SQL/web/Windows success tại lần kiểm, mobile còn chạy; các test mới chưa thuộc SHA đó.

Cập nhật tiếp: Release suite 24/24 đạt trên LocalDB, gồm thu hồi/ghi hai thứ tự, confirm hai connection, manifest sai DB/mốc/target, backup/restore định danh DB/ledger và validation HTTP cơ bản. API Release publish không có Operator/Tests assembly. Test backup tạo bản sao synthetic, giữ file/DB để chẩn đoán; không restore đè DB gốc.

Đã thêm operator authentication unit negative (allowlist/secret yếu/sai/không lộ secret), audit writeDenied, mock giao thức coordinator (không phải browser E2E), SQL register race chuẩn hóa email, customer email 254 và account/duty constraints. Các ca này chạy local đạt; tổng suite cần ghi lại theo HEAD cuối.

Release suite hiện tại 32/32 đạt: đã thêm preflight duplicate legacy dừng trước DDL/giữ rows, HTTP SQL unavailable 503, stale SecurityVersion/session missing/refresh expired và verification expired. Bằng chứng các ca ở test source tương ứng; không suy rộng sang các tổ hợp chưa chạy.

Còn kiểm chứng trước ready: đối chiếu checklist cuối và CI tại HEAD cuối. Client/browser E2E thuộc M2, mock không được gọi là client đã triển khai. Những hàng checklist gộp nhiều tổ hợp vẫn chỉ được xác nhận theo test thực có; không đánh dấu blanket PASS.

## Khoảng trống contract cần quyết định/review

## Đối chiếu bằng chứng theo nhóm checklist

| Nhóm | Bằng chứng đã chạy | Giới hạn |
|---|---|---|
| SQL upgrade/recovery | IdentityRecoverySqlTests, IdentityPreflightSqlTests, IdentityBackupSqlTests | Legacy reset ngoài target recovery còn thiếu contract |
| Constraint/concurrency | IdentitySqlTests: register/CCCD race, rowversion stale update, FK Restrict, duty/type, email 254 | Không suy ra mọi dữ liệu legacy đều ánh xạ được |
| Phiên/revoke/audit | IdentitySqlTests, IdentityWriteRevokeSqlTests, audit-trigger HTTP test | Revoke-write test dùng final auth/transaction path, không browser E2E |
| Identity/mail | IdentityMailSqlTests: rollback outbox, resend/used/expired/confirm race; IdentityTokenTests | Mail chỉ synthetic sink, không gửi người thật |
| Profiles/transport | IdentityHttpSqlTests: sáu title, scope A/B, create/PUT, CSRF/mixed transport, dependency 503 | Một số biên length/ngày sinh theo giờ cần kiểm thêm |
| Operator/publish | IdentityAdministrationSqlTests, OperatorAuthenticationTests; API Release publish đã kiểm | Không thay review vận hành; reset legacy còn chặn |
| Coordinator | IdentityCoordinatorContractTests | Chỉ mock contract, không client M2 hoặc browser E2E |

Các kết quả trên không thay kết quả CI HEAD cuối hoặc review độc lập. Chưa đánh dấu toàn hàng checklist PASS khi một hàng có tổ hợp chưa chạy.

### Quyết định đang chờ

Schema M1 yêu cầu cấp lại mật khẩu hash legacy không xác định; provision bị unique employee/account, recovery chỉ dành Admin target một lần. Người dùng đã chấp thuận phạm vi `reset-employee-password` ngày 23/10/2026; phần contract bổ sung mới còn chờ review độc lập, chưa viết lệnh hoặc sửa schema. Xem [contract](../contracts/m1-identity-profiles.md#bổ-sung-reset-employee-password--phạm-vi-đã-duyệt-contract-chờ-review) và [checklist](m1-verification-checklist.md#bổ-sung-reset-nhân-viên--chờ-review-độc-lập-các-ca-chưa-chạy). Đây vẫn là gate trước triển khai reset/ready; kết quả suite/CI cũ không bao phủ phần mới, phải kiểm HEAD code mới sau approval. Các dòng “chưa có contract” phía trên phản ánh khoảng trống implementation trước bổ sung, không phải phủ nhận scope approval mới.
