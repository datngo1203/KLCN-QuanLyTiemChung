# Checklist M1 — kế hoạch kiểm chứng, chưa chạy SQL/auth

[Spec](m1-identity-review.md), [contract](../contracts/m1-identity-profiles.md), [schema](../database/m1-identity-schema-review.md). Các ô dưới đây là tiêu chí nghiệm thu sau triển khai, không phải kết quả PASS. PR hiện tại chỉ kiểm tài liệu; CI xanh không thay review độc lập hoặc chứng minh các test tương lai đã chạy.

## Gate review trước code

- [x] Gate riêng M1 do người dùng quyết định ngày 23/10/2026 trong chat: báo cáo kỹ thuật từ chat độc lập tại `20a9a72b42c660bb642cac213045616a111de8ee` + chấp thuận contract/nghiệp vụ của người dùng thay yêu cầu Bảo/Quý xác nhận trước code. Không phải GitHub approval hoặc ý kiến thay thành viên; owner giữ nguyên.
- [x] Người dùng chấp thuận đặc tả và 10 quyết định kỹ thuật M1 tại SHA trên; không thay nội dung kỹ thuật trong lần đổi gate hành chính. Contract/schema đổi thực chất cần review độc lập phần đổi.
- [ ] Review diff so develop chỉ tài liệu, không migration/DTO/API/client/ảnh gốc; không mang #29/#30/#31 hoặc 38c4f84/fa027ee.
- [ ] Gate đã cho phép Goal/implementation SQL → Identity → Profiles trên nhánh riêng; ghi dependency #32 nếu chưa merge. Mọi thay đổi contract/schema mới cần review phần đổi; không tự merge hoặc bỏ qua branch protection.

## SQL Server thật, chạy tự động trong suite riêng

Ánh xạ review `ff1294b9`/base `f1880843` → sửa → kiểm dự kiến, tất cả chưa chạy:

| Finding | Mục sửa | Ca kiểm bắt buộc |
|---|---|---|
| P1 | Contract recovery; schema ledger/audit | DB chỉ Admin cũ → migration thành công → maintenance đọc/bind RowVersion mới → recovery thành công; stale-version/sai DB/mốc/migration/target/secret/operator/Director/hết hạn bị từ chối không ghi dở dang. Assert SQL rollback trước commit không có ledger thành công/quyền/audit dở dang; retry sau timeout đã commit trả kết quả lưu, count ledger/audit/cấp quyền không tăng. Manifest/operationId mới bị chặn sau thành công kể cả Admin lại Locked; hai operator đồng thời chỉ một lần thành công nhờ UNIQUE DB/mốc. Bootstrap DB cũ vẫn bị chặn; không reset metadata/ledger khi đổi secret/manifest hoặc backup/restore |
| P2 | Schema invariant; contract revoke | Assert SQL count credential ConsumedAt NULL AND RevokedAt NULL = 0 cho từng session revoked sau logout/replay/khóa/đổi quyền/status/duty/recovery; rotation thành công = 1, 409 không đổi nhánh thắng; hai thứ tự logout/rotation, audit rollback và phạm vi session khác |
| CSRF nhiều tab | Contract coordinator | A rotate, B có header cũ phải đọc lại cookie trong lock; đóng leader trước/sau response, mất broadcast/response, timeout, 409, cookie thiếu, browser thiếu Web Locks kết thúc hữu hạn/login lại; không nới CSRF/replay |
| Kho bearer | Contract memory-only | Không token trong localStorage/sessionStorage/IndexedDB/log/URL; logout/reload xóa memory; refresh HttpOnly/Secure và CSRF cookie riêng; channel không lưu bền |

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

## Bổ sung reset nhân viên — chờ review độc lập, các ca chưa chạy

- [ ] Review phần contract reset riêng theo commit bổ sung; scope approval không thay approval kỹ thuật. Không code lệnh trước review, không endpoint/reset SQL thủ công/khôi phục recovery một lần.
- [ ] SQL thật: upgrade legacy → Admin recovery → actor xác thực bằng credential mới → reset Employee legacy Active → login bằng password mới. Hash legacy cũ/password cũ không còn dùng được; reset không tạo account/nhân viên trùng.
- [ ] Actor sai credential/thiếu QuanLyTaiKhoan/Locked/nhân viên inactive/Director/self-reset bị từ chối; secret recovery/token cũ/actorId giả không thay credential. Actor bị đổi password/status/quyền sau bước nhập phải bị phát hiện dưới khóa, không commit.
- [ ] Target Customer/không tồn tại/EmployeeId không khớp/version thiếu hoặc sai bị từ chối. Snapshot trước/sau: status account/nhân viên, duty/title/quyền/username/email/FK và recovery ledger không đổi. Target Locked/inactive được reset hash nhưng login vẫn bị từ chối.
- [ ] Session/access token cũ target vô hiệu request tiếp theo; mọi session target revoked có 0 credential chưa consumed/chưa revoked, kể cả token hết hạn; actor/account khác không bị revoke.
- [ ] Hai SQL connection cùng RowVersion: chỉ một reset thành công; stale version không tự reload/retry. Reset đua với refresh/logout/lock/đổi quyền/credential actor: khóa và kiểm lại không để bypass hoặc trạng thái dở dang.
- [ ] Inject failure tại update hash, revoke và audit: hash/SecurityVersion/session/credential/audit rollback nguyên vẹn; đúng một lần tăng version/audit sau thành công. Timeout commit chưa rõ không tự phát lại reset hoặc recovery.
- [ ] Password ẩn/nhập lại/chính sách/hash thư viện backend; stdin redirect bị chặn; stdout/stderr/log/exception/audit/argv/env/file không chứa password hoặc hash. Test dùng secret giả marker, không ghi password thật vào test artifacts.
- [ ] Runbook bàn giao trực tiếp/đúng người, dừng nếu chưa có kênh được chấp thuận; không gửi mail/SMS/chat, không giả force-change/password expiry. Ledger recovery và quota một lần không đổi.
- [ ] Chỉ sau review bổ sung mới chạy test reset và toàn SQL suite trên HEAD code mới; cập nhật lệnh/SQL version/kết quả/CI SHA mới, không dùng 33/33 hoặc CI SHA trước để kết luận reset PASS.

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
