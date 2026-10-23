# Schema M1 đề nghị review — không phải migration

[Phạm vi/owner/gate](../docs/m1-identity-review.md), [contract](../contracts/m1-identity-profiles.md), [kiểm thử](../docs/m1-verification-checklist.md). Giữ migration InitialCreate hiện tại; chỉ sinh migration bổ sung sau review độc lập được chấp thuận. Không sửa initial-schema.sql hoặc ảnh UML trong PR đặc tả.

## Thay đổi tối thiểu theo lát đã duyệt

| Bảng | Cột/ràng buộc đề xuất |
|---|---|
| TaiKhoan hiện có | Giữ MaTaiKhoan int PK, TenDangNhap nvarchar(100), MatKhauHash nvarchar(255), TrangThai. MaNhanVien int nullable FK Restrict; filtered unique khi non-null. Thêm AccountType varchar(16) Customer/Employee, LoginEmail nvarchar(254) nullable, NormalizedEmail nvarchar(254) nullable filtered unique, NormalizedUsername nvarchar(100) nullable filtered unique, EmailVerifiedAt datetimeoffset nullable, SecurityVersion bigint default 1, AdministrativeDuty varchar(16) None/Technical/Director, CreatedAt datetimeoffset, RowVersion rowversion |
| IdentitySession | Id uniqueidentifier PK, AccountId int FK Restrict, SecurityVersion bigint, ClientType varchar(8) web/native, CreatedAt/ExpiresAt datetimeoffset, RevokedAt nullable, RevocationReason varchar(40) nullable, CsrfSecretHash binary(32) nullable, RowVersion. Index AccountId,ExpiresAt |
| RefreshCredential | Id uniqueidentifier PK, SessionId FK Restrict, TokenHash binary(32) unique, CreatedAt/ExpiresAt, ConsumedAt nullable, ReplacedById nullable self FK Restrict, RevokedAt nullable, RowVersion. Filtered unique SessionId khi ConsumedAt IS NULL AND RevokedAt IS NULL (một active token); index SessionId |
| EmailVerification | Id uniqueidentifier PK, AccountId FK Restrict, TokenHash binary(32) unique, CreatedAt/ExpiresAt, UsedAt nullable, InvalidatedAt nullable; filtered unique AccountId khi UsedAt/InvalidatedAt đều null. Resend invalidate token cũ trong transaction |
| IdentityMailOutbox | Id uniqueidentifier PK, VerificationId FK Restrict, EncryptedPayload varbinary(max) nullable, CreatedAt/ExpiresAt, SentAt nullable, Attempts int, NextAttemptAt nullable, LastErrorCode varchar(40) nullable. Không log/lưu lỗi provider nguyên văn; xóa payload sau gửi hoặc hết hạn |
| AccountProfileLink | Id int PK, AccountId FK TaiKhoan, CustomerId int nullable FK KhachHang, RecipientId int nullable FK NguoiDuocTiem, State varchar(16) Active/Revoked, CreatedAt/RevokedAt, RowVersion. CHECK đúng một target; filtered unique CustomerId/RecipientId khi Active. Chỉ Customer account; mỗi account tối đa một liên kết active mỗi loại target qua filtered unique AccountId tương ứng |
| SecurityAudit | Id bigint PK, OccurredAt datetimeoffset, ActorAccountId nullable FK Restrict, SubjectAccountId nullable FK Restrict, SessionId nullable, Action varchar(64), Outcome varchar(16), ResourceType varchar(32) nullable, ResourceId nvarchar(64) nullable, TraceId varchar(64), ChangedFields nvarchar(512) nullable. Không password/token/CCCD/email/nội dung hồ sơ hoặc before/after đầy đủ |
| KhachHang | Giữ nguyên required và kiểu dữ liệu; thêm unique CCCD sau preflight dữ liệu trùng. Không tự merge |
| QuyenHan | Thêm Code varchar(64) stable unique (backfill 12 mã theo Screen Spec qua review); MaQuyen int giữ nguyên. Không suy code theo label đã thay đổi |

Thiết kế AccountProfileLink ở M1 chỉ phục vụ scope và fixture. Không coi KhachHang.MaKhachHang hoặc NguoiDuocTiem.MaKhachHang là quan hệ chính chủ. Hai link customer/recipient cùng tài khoản chỉ được fixture đã xác nhận cùng người; không cho cấp target tùy ý. Server kiểm account type/quan hệ chéo; CHECK SQL không tự chứng minh danh tính. Không có endpoint tạo link. M3 cần review lại mô hình bằng chứng/đại diện trước ghi dữ liệu thật.

### CHECK và index bắt buộc

- Customer: MaNhanVien IS NULL, LoginEmail/NormalizedEmail NOT NULL, AdministrativeDuty=None. Employee: MaNhanVien NOT NULL, NormalizedUsername NOT NULL; LoginEmail tùy chọn, không dùng email nhân viên để tự nhận tài khoản. Customer TenDangNhap là mã nội bộ server sinh (không email 254 bị cắt vào cột 100), không phải nhân viên giả; NormalizedUsername null cho customer. Customer login chỉ qua email.
- AdministrativeDuty khác None chỉ hợp lệ với Employee. Technical/Director phải có NhanVien.ChucDanh Admin, kiểm ở domain/transaction vì CHECK không join bảng. Một employee đúng một title ở NhanVien; không thêm nhiều title để lách quyền.
- Chặn Director có QuanLyTaiKhoan tại service vận hành và đánh giá quyền mỗi request kể cả DB cũ đã chứa gán sai; preflight phát hiện và yêu cầu xử lý, không che bằng UI. Giữ TaiKhoan_QuyenHan PK ghép/FK Restrict.
- ExpiresAt > CreatedAt; SecurityVersion > 0; client/account/state/duty allowlist; token hash unique. Mọi FK Restrict, không cascade xóa hồ sơ/audit. Username nhân viên chuẩn hóa trim + invariant uppercase, unique; ký tự ASCII chữ/số/._-, 4–100, không @.
- Rotation phải consume token cũ trước insert token mới trong cùng transaction để thỏa unique active. Link scope query có index AccountId,State và target. Không dùng SQL view công khai hoặc connection client trực tiếp.

## Nâng cấp và rollback cần kiểm chứng sau approval

1. Preflight read-only: username trùng sau normalize, CCCD khách trùng, email xung đột, FK/status/title không hợp lệ, hash không rõ thuật toán, quyền có label/code không ánh xạ. Báo mã bản ghi qua kênh vận hành hạn chế, không dump PII vào CI/Git. Dừng nếu không ánh xạ được; người quản trị xử lý, không tự xóa/hợp nhất.
2. Migrate TaiKhoan cũ thành Employee vì đang có FK MaNhanVien; backfill username chuẩn hóa. Duty cũ None, không tự phong Technical/Director theo tên hoặc BoPhan. Tài khoản Admin cũ chưa xác định duty phải chuyển Locked, thu hồi sessions cho đến vận hành phân loại rõ và mở lại có audit. Tăng SecurityVersion khi phân loại. Migration không tự cấp QuanLyTaiKhoan cho bất kỳ account cũ nào.
3. Không tự đánh dấu EmailVerifiedAt cho khách (chưa có khách account cũ), không tạo liên kết từ CCCD/email giống nhau. Hash cũ không xác định phải buộc cấp lại qua vận hành kiểm soát trước login; không nhận mật khẩu UI admin/123.
4. Áp schema/index mới chỉ sau dữ liệu qua preflight; backup DB và thử nâng cấp bản sao trước. Không auto-run destructive migration khi API startup. DDL script review ở PR implementation riêng.
5. Rollback ứng dụng không được chạy API cũ thiếu auth vào DB có dữ liệu mới. Maintenance/backup-restore thử trên DB riêng; down migration không được xóa account/audit/link khi đã có dữ liệu. Dừng và phục hồi backup theo kế hoạch nếu rollback schema gây mất dữ liệu.

Không tạo bảng bản tự khai, đại diện, bằng chứng xác minh, yêu cầu sửa hoặc đổi email ở M1. Audit hiện tại chỉ sự kiện bảo mật/Profiles tối thiểu; các bảng mở rộng phải theo contract M2–M4 đã được review, không migrate trước.

## Transaction và audit

Register account + verification + outbox + audit cùng transaction. Confirm token CAS UsedAt + EmailVerifiedAt + audit, không link/profile. Provision/status/permission change + SecurityVersion + session revoke + audit cùng transaction. POST Profiles + audit cùng transaction; kiểm quyền cuối trong transaction. Refresh rotation/competing CAS + audit theo session lock; replay revoke cùng transaction.

Audit sự kiện: account.registered/emailConfirmed/loginSucceeded/loginFailed/logout/sessionRevoked/refreshRotated/refreshReplay/employeeProvisioned/accountStatusChanged/permissionsChanged/employeeStatusChanged; profiles.created và profiles.writeDenied (chỉ resource ID nếu actor được biết). Public failure không gán actor chỉ từ identifier chưa xác thực. Audit lỗi public ghi ở kênh security hạn chế với traceId, không lưu raw identifiers. Login/cấp token thành công phải có audit bền; failure audit dependency lỗi vẫn từ chối bảo vệ, không mở quyền để tránh lỗi.

Fixture: runner/test assembly riêng, Development/Test + DB marker FixtureOnly; chỉ DB synthetic, không startup seeder hoặc endpoint public, không binary fixture trong production publish. Production config không có switch bypass để tạo link. Có negative test cấu hình Production/DB không marker phải từ chối trước ghi. Khi chưa có tiêu chuẩn xác minh, sản phẩm M1 chỉ vận hành thử dữ liệu giả.
