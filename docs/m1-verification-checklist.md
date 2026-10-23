# Checklist M1 — kế hoạch kiểm chứng, chưa chạy SQL/auth

[Spec](m1-identity-review.md), [contract](../contracts/m1-identity-profiles.md), [schema](../database/m1-identity-schema-review.md). Các ô dưới đây là tiêu chí nghiệm thu sau triển khai, không phải kết quả PASS. PR hiện tại chỉ kiểm tài liệu; CI xanh không thay review độc lập hoặc chứng minh các test tương lai đã chạy.

## Gate review trước code

- [ ] Bảo/Quý không phải tác giả review độc lập Identity + schema, ghi SHA/approval; Bảo xác nhận Profiles nghiệp vụ, Đạt phụ trách SQL/quyền. Không tự ký thay reviewer.
- [ ] Chấp thuận 10 quyết định kỹ thuật trong spec, đặc biệt SQL account độc lập, nghĩa PUT từ chối, phạm vi nhân viên toàn trung tâm hiện tại và refresh 409/5 giây.
- [ ] Review diff so develop chỉ tài liệu, không migration/DTO/API/client/ảnh gốc; không mang #29/#30/#31 hoặc 38c4f84/fa027ee.
- [ ] Sau approval mới chia PR implementation SQL → Identity → Profiles; mỗi contract đổi phải review lại. Không tự merge PR đặc tả, không Goal code trước gate.

## SQL Server thật, chạy tự động trong suite riêng

Yêu cầu SQL Server container/instance riêng, DB tên duy nhất mỗi run với dữ liệu giả; credentials qua secret, không production. Runner CI phải start SQL, wait readiness rồi migrate/test; skip hoặc thiếu SQL là NOT VERIFIED, không PASS. Unit tests/EF InMemory/GenerateCreateScript không thay suite này. Báo SQL version, migration ID, SHA, lệnh và log đã loại bí mật.

- [ ] Fresh migration và upgrade từ InitialCreate; so dữ liệu/constraint, backup/restore bản sao. Preflight lỗi dừng trước sửa, không tự merge CCCD/account.
- [ ] Customer account không CustomerId/MaNhanVien/CCCD; Employee bắt buộc FK thật và unique; loại account/duty sai bị chặn; customer email 254 không bị cắt vào username 100.
- [ ] Email/username normalization và unique race register; CCCD 0 đầu/12 ASCII/duplicate race; không unique recipient CCCD ngoài quy tắc đã duyệt.
- [ ] Refresh hai connection đồng thời: một 200, một 409, một active token; dùng lại sau 5 giây revoke; rollback không mất cả nhánh active; login session khác không bị ảnh hưởng.
- [ ] Rotation/logout, revoke/Profiles write hai thứ tự commit; request bắt đầu sau revoke commit luôn bị chặn dù JWT còn hạn; quyền SQL không đọc được trả 503.
- [ ] Audit/outbox failure rollback đúng; mail retry không nhân tài khoản; payload mã hóa và xóa sau gửi/hết hạn; FK Restrict, rowversion và query scope đúng.

## Identity/nhân viên

- [ ] Register/resend mới/cũ phản hồi chung; không thay password pending qua register lặp, không tạo Customer/link/NhanVien; mail sink nhận một token hợp lệ.
- [ ] Verify token đúng/sai/hết hạn/used/resend cũ, cạnh tranh confirm; chỉ đổi email state, không nhận quyền theo email/CCCD.
- [ ] Login customer pending/verified/unlinked; Employee đúng/sai/inactive, username spoof/role/employeeId payload bị reject; chức danh/quyền chỉ từ server.
- [ ] TTL/access ký sai/session missing/lock/securityVersion, logout access đã hết hạn, refresh revoked/expired, rate limit và Retry-After; không token/password trong log.
- [ ] Web cookie flags, CSRF sai/thiếu/Origin lạ, cookie + body token, native transport; auth/me không lộ hash/PII và không cache.
- [ ] Client coordinator mock nhiều request/tab: single-flight, 409 không loop, mất response thắng phải login lại; không auto retry POST Profiles khi timeout.
- [ ] Bootstrap không account mới được chạy; tài khoản cũ chặn; actorId giả không được tin; Director/self-change/missing QuanLyTaiKhoan bị chặn; cấp/khóa/thay quyền đều audit/revoke.

## Năm route Profiles và phạm vi

- [ ] Anonymous 401; customer chưa verify bị chặn; verified unlinked GET items rỗng, POST/PUT 403. Không map endpoint claim/link.
- [ ] Fixture A/B chính chủ: list trước search/page không lộ B; recipient không tự xuất hiện vì cùng CustomerId; ID ngoài phạm vi không tiết lộ tồn tại.
- [ ] LeTan đọc/tạo; Admin + XemHoSo chỉ đọc; Admin thiếu quyền và các title khác không truy cập list. Không tự cho ThuNgan/BacSi/DieuDuong quyền rộng.
- [ ] PUT đủ quyền/in-scope vẫn 409 identityApprovalRequired, không đổi tên/CCCD/ngày sinh/giới tính/quan hệ; không có PUT customers. Thiếu quyền 403, ngoài scope 404.
- [ ] Required/null/whitespace/unknown fields/length/page/search; ngày sinh thiếu/min/future và biên nửa đêm Saigon; gender allowlist; email/SĐT; SQL race map 409 đúng loại.
- [ ] Create không tự verified/link, restart API/SQL dữ liệu còn; lỗi SQL/mạng không sample fallback; Location chỉ resource ID, không đòi route GET-id chưa có.
- [ ] Tạo recipient retry timeout không có bảo đảm exactly-once: client không tự retry; ghi giới hạn nghiệm thu, không báo đã giải quyết idempotency.
- [ ] Fixture runner từ chối Production và DB không marker; test chỉ chứa người giả; production publish không có fixture runner/claim bypass.

## Mốc tiếp theo giữ trong P0–P7

M2: nháp tự khai/sửa/gửi/rút và ba client (Windows Đạt, Profiles Bảo review, Mobile Quý). M3: xác minh/đại diện nhiều người, quyền cố định theo hồ sơ/đăng ký/lịch/lịch sử/thanh toán/đề nghị sửa, thu hồi. M4: sửa định danh/đổi email/E2E. Không xây permission builder; điều kiện bằng chứng/chính sách vận hành thật chưa đủ thì server đóng chức năng, vẫn test end-to-end bằng fixture trong môi trường riêng.
