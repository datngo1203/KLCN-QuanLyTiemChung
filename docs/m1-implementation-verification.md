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

Còn kiểm chứng trước ready: audit rollback Profiles/refresh, thu hồi cạnh tranh ghi hai thứ tự, bootstrap/operator negative cases, mail sink retry/cleanup, verification race/resend, validation biên đầy đủ, backup/restore, multi-tab coordinator mock và production publish. CI phải ghi theo SHA thực tế. Các ca trong checklist chưa có bằng chứng không được đánh dấu PASS.
