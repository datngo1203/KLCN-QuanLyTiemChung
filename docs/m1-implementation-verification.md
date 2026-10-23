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

Còn kiểm chứng trước ready: operator xác thực CLI negative, validation biên đầy đủ, multi-tab coordinator mock và CI tại HEAD cuối. CI phải ghi theo SHA thực tế. Các ca trong checklist chưa có bằng chứng không được đánh dấu PASS.
