# API dùng chung · v1

## AUTHZ-R1 — quyền nhân viên theo chức danh (#142)

Nguồn runtime duy nhất: `EmployeeAuthorization` trong Infrastructure. Employee Active có đúng một trong sáu chức danh; grant legacy được giữ trong SQL nhưng không thêm/bớt quyền runtime. `AdministrativeDuty=Director` tiếp tục không được quản trị tài khoản. Customer và kiểm tra ownership hiện hành không đổi.

| Chức danh | Màn Windows | Quyền nghiệp vụ |
|---|---|---|
| LeTan | A01/A03/A07/A09/A14 | Hồ sơ đọc/ghi, đăng ký, lịch hẹn/nhắc lịch |
| BacSi | A01/A02/A14 | Đọc hồ sơ, kết luận/chỉ định |
| DieuDuong | A01/A06/A06F/A14 | Đọc hồ sơ, ghi tiêm/theo dõi |
| ThuNgan | A01/A08/A14 | Tra cứu đăng ký phục vụ thanh toán, ghi/xác nhận thanh toán |
| ThuKho | A01/A05/A14 | Đọc kho, nhập một lô theo contract hiện hành |
| Admin | A01/A04/A10/A11/A12/A14 | Danh mục, ưu đãi, báo cáo, chức danh tài khoản |

`GET /api/v1/auth/me` giữ `permissions` và các capability Customer cũ; Employee permissions được derive từ matrix, capabilities bổ sung boolean `overview`, `changePassword`, `registrations`, `screening`, `vaccinations`, `aftercare`, `paymentsRead`, `paymentsWrite`, `inventoryRead`, `inventoryReceive`, `appointments`, `catalog`, `promotions`, `reports`, `accounts`. Windows dùng các boolean từ server cho menu/search/direct navigation; thiếu capability thì từ chối. Thu ngân/thủ kho/Admin đọc dữ liệu cần thiết qua API riêng của module, không được đọc danh sách hồ sơ chung. Admin không bypass actor lâm sàng/thu ngân/thủ kho/lễ tân. Các điều kiện tiền, tồn, chỉ định và transaction nghiệp vụ giữ nguyên.

A12 bỏ các thao tác Sample và checkbox quyền rời rạc, chỉ đổi một chức danh cho tài khoản nhân viên có sẵn. Provision/reset/khóa tài khoản tiếp tục qua công cụ operator hiện có, không bổ sung HTTP cho các thao tác này.

- `GET /api/v1/employee-access`: Admin có capability accounts; no-store; trả `{items:[{accountId,employeeId,fullName,username,title,employeeStatus,accountStatus,version}],roles:[{title,label,access}]}`. `version` là RowVersion base64, `label`/`access` dùng tiếng Việt cho UI.
- `PUT /api/v1/employee-access/{accountId}/role`: `{title,expectedTitle,version}`; title/expectedTitle phải thuộc sáu chức danh, version 8 byte. Không tự đổi chức danh; không nhận Customer. Serializable + khóa administration/account; kiểm expectedTitle/RowVersion, đổi ChucDanh + tăng SecurityVersion + thu hồi sessions/refresh credentials + audit cùng transaction; 204. Không đổi hash mật khẩu hoặc grant legacy.
- Lỗi: 400 validation, 401 invalidSession, 403 permissionDenied, 404 resourceNotFound, 409 concurrencyConflict, 503 dependencyUnavailable. Client tải lại sau lỗi không xác định/conflict, bỏ response trễ khi đổi phiên.
- Không migration. Automated WPF render/navigation cho sáu role; manual full-shell deferred to S8 theo giao việc #142.


## RF-01 — Khách hàng quên mật khẩu (Issue #77)

- POST `/api/v1/auth/password-resets/request`, đúng JSON `{ "email": "customer@example.invalid" }`: `202 { "status": "passwordResetPending" }` cho email Customer Active, không tồn tại, Employee hoặc tài khoản không Active. Không tạo account hoặc tiết lộ tài khoản có tồn tại. Username không phải email trả `400 validation`.
- POST `/api/v1/auth/password-resets/confirm`, đúng JSON `{ "token": "…", "newPassword": "…" }`: mật khẩu 12–128 ký tự; `204` khi thành công. Token sai/hết hạn/đã dùng/bị thay thế hoặc tài khoản không còn Customer Active đều trả `400 invalidPasswordResetToken`. Sai mật khẩu mới trả `400 validation`. Trường lạ bị từ chối.
- Hai endpoint public, `Cache-Control: no-store`; dùng rate-limit hiện có: request dùng mail IP/email (5/giờ/IP, 3/giờ/email, dùng chung ngân sách register/resend), confirm dùng 10/15 phút/IP chung xác minh email; quá giới hạn `429 rateLimited`. Request giữ mail readiness Development/Synthetic hiện hành: chỉ `@example.invalid`, không gửi thư thật; chưa sẵn sàng `503 dependencyUnavailable` áp dụng chung cho mọi email.
- `CustomerPasswordReset`: token random 256 bit, SQL chỉ lưu SHA-256, TTL cố định 30 phút, một token chưa dùng/chưa vô hiệu cho mỗi account. Request tiếp theo vô hiệu token trước; không ảnh hưởng token xác minh email. Cùng khóa account hiện có để tuần tự hóa request/confirm, không cho hai confirm cùng thành công.
- Confirm transaction Serializable: đánh dấu token dùng → hash mật khẩu bằng backend → tăng SecurityVersion/thu hồi tất cả session và refresh credential → audit `passwordResetConfirmed` → commit. Request + outbox mã hóa + audit `passwordResetRequested` cũng một transaction. Lỗi DB/audit rollback toàn bộ, không log token/mật khẩu/email.
- Migration `20261103115754_RF01CustomerPasswordReset`: thêm bảng reset, FK/index/expiry constraint; bổ sung `IdentityMailOutbox.PasswordResetId`, cho `VerificationId` nullable và constraint đúng một loại thư. Thư xác minh cũ được giữ nguyên; outbox reset tái dùng mã hóa AES-GCM, retry/TTL và pickup JSON `{email,token}` hiện có. Down từ chối khi đã có lịch sử reset, không xóa để downgrade.
- Web: Đăng nhập → Quên mật khẩu → yêu cầu email → nhập token + mật khẩu mới + xác nhận → quay lại đăng nhập. Đăng ký thêm xác nhận mật khẩu chỉ ở client, body register vẫn `{email,password}`. Hiện/ẩn không mất giá trị; lỗi giữ bản nháp; các token/session chỉ ở bộ nhớ. Windows không thêm reset tự phục vụ; login/refresh/logout/change-password hiện hành giữ nguyên.

## RF-05E — Email nhắc lịch (Windows A09, #166)

- Lễ tân Active / `GuiNhacLich` theo quyền backend hiện hành; kiểm lại account/session/role mỗi thao tác. Các route cũ S5.7B bên dưới giữ nguyên để đọc/ghi log legacy; `LogMoPhong` không chứng minh email đã gửi.
- GET `/api/v1/reminders/email-queue?search=&page=1&pageSize=25`: `{items: AppointmentDto[]}`, nguồn Clinical hợp lệ, lịch tương lai `DaXepLich`, chưa ghi mũi. Giữ cả lịch đã enqueue để mở lại/đối chiếu/retry; không loại lịch vì log cũ. Search tên hoặc LH-code, paging/validation như queue RF-05D. GET queue/detail không ghi dữ liệu.
- GET `/api/v1/appointments/{id}/email-reminder`: `{appointment, preview, canRecord, reminder, maskedRecipient, subject, unavailableReason}`. Preview subject/body do server tạo; chỉ hiện khi nguồn đủ điều kiện và có đúng một liên kết Customer cùng một liên kết Recipient Active thuộc cùng Customer account Active, email đăng nhập hợp lệ/đã xác minh. Thiếu/trùng/khác owner → `reminderRecipientUnavailable`, không preview/enqueue/provider. Không fallback email đăng ký/nhân viên.
- POST cùng route với `{}`: transaction Serializable, khóa actor/session → lịch, kiểm nguồn và recipient → lưu immutable snapshot `ReminderEmail` + audit `reminders.enqueued` → commit. Trả **201 là enqueue**, chưa phải đã gửi. Client không gửi recipient/subject/body; unknown field bị 400. Cùng appointment + registration + recipient + dòng dịch vụ + thời gian UTC trả cùng delivery id khi replay/race/mất response; unique index bảo vệ DB. Đổi giờ/source có thể tạo snapshot mới trong lát tương lai, không unique trọn đời lịch.
- `reminder`: `{id, appointmentId, channel:"Email", status:"ChoGui"|"DaGui"|"GuiLoi", content, recordedAt, recordedBy, maskedRecipient, subject, attempts, deliveredAt, nextAttemptAt, lastErrorCode, canRetry}`. Không trả email đầy đủ hay raw provider exception. Snapshot lưu người/dịch vụ/địa điểm/giờ/recipient/subject/body/người tạo; dispatcher không tái tạo body từ master.
- Dispatcher chạy Development khi `ReminderMail:Mode=Synthetic|Smtp`, chỉ xử lý yêu cầu đã enqueue, không scheduler nhắc lịch. Khóa SQL session tuần tự hóa workers; claim bền vững trong transaction ngắn → SMTP **ngoài transaction** → status + audit `reminders.delivered`/`reminders.failed` trong transaction ngắn. Recheck nguồn và ownership/email trước gửi. `DaGui` là SMTP/provider đã chấp nhận, không phải bằng chứng khách đã đọc hoặc thư vào inbox.
- Lỗi an toàn: `providerUnavailable` (trước gửi), `providerRejected` (SMTP từ chối rõ), `deliveryUncertain` (không biết đã được chấp nhận), `reminderRecipientUnavailable`, `reminderSourceConflict`. Chỉ hai lỗi provider xác định được phép retry sau tối thiểu 1 phút; không auto retry. POST `.../email-reminder/retry` với `{expectedAttempts:n}` kiểm nguồn/recipient hiện hành còn khớp snapshot, trạng thái/thời điểm/attempts rồi chuyển **cùng row** về `ChoGui` + audit `reminders.retried`. Double/concurrent/stale retry bị 409 `reminderRetryUnavailable`; GET trước khi thử lại nếu mất response. Không retry email đã gửi hoặc kết quả chưa rõ.
- SMTP không có giao dịch phân tán/exactly-once với SQL. Claim bị gián đoạn hoặc SQL/audit lỗi sau provider acceptance chuyển `GuiLoi/deliveryUncertain` ở lần drain sau, **không gửi lại**; phải đối chiếu thủ công. Không tự lease-expire rồi gửi lại vì có thể gửi trùng.
- Migration `RF05EReminderEmail` thêm bảng/index/FK/check riêng, không sửa `NhacLich`/log cũ; Down chỉ cho phép khi bảng email rỗng. Chỉ kiểm migrate trên DB disposable, không tự migrate DB demo.
- Windows giữ queue/detail hiện có, email masked, `Chưa gửi / Đang gửi / Đã gửi / Gửi lỗi`; xác nhận gửi, refresh/đối chiếu, retry theo `canRetry`; không hiển thị enum/debug/provider raw. Phiên đổi/thu hồi xóa dữ liệu và bỏ response trễ.
- Không SMS, OTP, scheduler, sửa/hủy lịch hay thay đổi clinical/payment. Cấu hình local SMTP và giới hạn smoke trong README gốc.

## S5.7B — Log nhắc lịch mô phỏng (Windows A09)

Chỉ Employee/Account/NhanVien Active, LeTan + QuanLyLichHen; dùng Identity hiện hành, không thêm quyền. Không gửi email/SMS, không scheduler. Một lịch tối đa một log LogMoPhong; các kênh legacy giữ nguyên.

- RF-05D GET `/api/v1/reminders/queue?search=&page=1&pageSize=25`: `{items: AppointmentDto[]}`, Lễ tân/quyền nhắc lịch như detail. Chỉ nguồn Clinical hợp lệ, DaXepLich ở tương lai, chưa có mũi DaTiem và chưa có LogMoPhong (Email legacy không loại lịch). Search tên người được tiêm hoặc mã LH-id, trim/tối đa 100 ký tự; page ≥1, pageSize 1–100, chặn overflow. Sắp thời gian hẹn rồi id tăng dần. GET không ghi; chọn row phải đọc preview hiện tại, POST kiểm lại nguồn trong transaction. Không schema/migration mới.
- GET `/api/v1/appointments/{id}/reminders`: `{ appointment: AppointmentDto, preview: string|null, canRecord: boolean, reminder: ReminderDto|null }`. Preview từ SQL khi lịch đủ điều kiện; đã ghi thì trả snapshot cũ, không tạo lại nội dung. GET không ghi dữ liệu.
- POST cùng route, body đúng `{}` (từ chối trường lạ), 201 + Location GET tương ứng, trả ReminderDto `{id,appointmentId,channel,status,content,recordedAt,recordedBy:{id,fullName}|null}`. Server đặt LogMoPhong/DaGhiLogMoPhong, thời điểm UTC và người ghi từ phiên; không nhận nội dung/actor/time từ client.
- Nguồn S5: đăng ký DaDangKy, lịch DaXepLich ở tương lai, dòng lẻ/gói đúng đăng ký, Recipient thuộc Customer. Đã có MuiTiem DaTiem qua chỉ định/phiếu thuộc buổi hẹn thì không ghi lời nhắc tham dự. Nội dung chứa mã lịch/người/dịch vụ/giờ UTC+07:00/địa điểm và nhãn chưa gửi; tối đa1000 ký tự, quá dài trả400 validation, không cắt.
- Transaction Serializable và khóa Identity → lịch UPDLOCK/HOLDLOCK; kiểm nguồn/chưa có log → insert NhacLich + audit reminders.simulated/Reminder/ID/actor/session → commit. Unique filtered index chặn trùng; lỗi insert/audit rollback. Không sửa lịch/đăng ký/tiền/mũi/tồn/theo dõi.
- No-store và Problem Details:400 validation,401 invalidSession,403 permissionDenied,404 resourceNotFound,409 reminderSourceConflict/reminderAlreadyRecorded,503 dependencyUnavailable. Không trả nội dung hồ sơ/SQL trong lỗi.
- Migration S5ReminderSimulation thêm MaNhanVien nullable FK Restrict, NoiDungMoPhong nullable nvarchar(1000), unique MaLichHen filter KenhGui=LogMoPhong; Up dừng nếu xung đột, Down dừng khi còn snapshot/link mới. Không backfill hoặc xóa legacy; chỉ migrate DB disposable trong kiểm thử.
- Windows xem preview → xác nhận → ghi/đọc lại snapshot. Mất response/503 giữ pending, GET trước retry thủ công cùng lịch;201 lỗi body chỉ GET. Đổi phiên xóa dữ liệu/pending và bỏ response trễ. Log chỉ chứng minh thao tác nội bộ, không chứng minh khách nhận thông báo.

Base URL phát triển: `http://localhost:5080`. JSON camelCase, tiền decimal đơn vị VND, ngày `YYYY-MM-DD`, thời điểm ISO 8601 UTC. ID số nguyên; lỗi dùng Problem Details. OpenAPI thực thi: `GET /openapi/v1.json` ở Development.

## Đã có

Đối chiếu `Program.cs`, `IdentityEndpoints.cs`, `ProfileEndpoints.cs`; OpenAPI ở `/openapi/v1.json` trong Development. S1 nối Web/Windows vào Identity. Các màn nghiệp vụ mẫu chưa đồng nghĩa đã tích hợp SQL.

| Method/path | Nội dung | Auth |
|---|---|---|
| GET `/health` | Liveness tiến trình; không xác nhận SQL sẵn sàng | Không |
| GET `/api/v1/catalog/vaccines?search=` | `{ source: "database", items: [{ id, name, manufacturer, origin, price, technologies }] }` | Không; danh mục công khai |
| GET `/api/v1/modules` | Module/người phụ trách/trạng thái kế hoạch cũ | Không |
| GET `/openapi/v1.json` | Mô tả endpoint | Không; chỉ Development |
| POST `/api/v1/auth/register` | Đăng ký email/password, trả 202 verificationPending khi mail sink sẵn sàng | Không; rate limit và mail readiness |
| POST `/api/v1/auth/email-verifications/resend` | Gửi lại xác minh | Không; rate limit và mail readiness |
| POST `/api/v1/auth/email-verifications/confirm` | Xác nhận token email | Không; rate limit |
| POST `/api/v1/auth/login` | identifier/password/clientType; cấp access/refresh | Không; kiểm credential, trạng thái và rate limit |
| POST `/api/v1/auth/refresh` | Đổi token phiên | Refresh token; Web thêm cookie, Origin và CSRF |
| POST `/api/v1/auth/logout` | Thu hồi phiên | Refresh token; Web thêm cookie, Origin và CSRF |
| POST `/api/v1/auth/change-password` | `{ currentPassword, newPassword }`; mật khẩu mới 12–128 ký tự, khác hiện tại; trả 204, thu hồi mọi phiên, đăng nhập lại | Bearer; kiểm mật khẩu hiện tại, transaction/hash/audit; sai mật khẩu 401 |
| GET `/api/v1/auth/me` | Tài khoản, phạm vi và capabilities hiện hành | Bearer access token |
| GET `/api/v1/customers?search=&page=&pageSize=` | Danh sách khách hàng, có phân trang | Bearer; nhân viên Active hoặc khách hàng theo liên kết hồ sơ Active |
| PUT `/api/v1/customers/{id}` | Cập nhật khách hàng | Bearer; LeTan hoặc chủ hồ sơ Active |
| DELETE `/api/v1/customers/{id}` | Xóa nếu không bị tham chiếu | Bearer; LeTan hoặc chủ hồ sơ Active |
| DELETE `/api/v1/recipients/{id}` | Xóa nếu không bị tham chiếu | Bearer; LeTan hoặc chủ hồ sơ Active |
| POST `/api/v1/customers` | Tạo khách hàng | Bearer; LeTan hoặc khách hàng tạo hồ sơ của chính mình |
| GET `/api/v1/recipients?customerId=&search=&page=&pageSize=` | Danh sách người được tiêm, có phân trang | Bearer; nhân viên Active hoặc khách hàng theo liên kết hồ sơ Active |
| POST `/api/v1/recipients` | Tạo người được tiêm gắn khách hàng | Bearer; LeTan hoặc khách hàng tạo hồ sơ của chính mình |
| PUT `/api/v1/recipients/{id}` | Cập nhật thông tin người được tiêm | Bearer; LeTan hoặc khách hàng có liên kết hồ sơ Active |
| POST `/api/v1/appointments` | Tạo buổi hẹn từ dòng dịch vụ đăng ký; 201 + Location | Bearer; LeTan Active + QuanLyLichHen |
| GET `/api/v1/appointments` | Danh sách lịch SQL đã liên kết nguồn; phân trang/bộ lọc theo S5.1 | Bearer; cùng quyền tạo lịch |
| GET `/api/v1/appointments/sources`, `/api/v1/appointments/sources/{registrationId}` | Đăng ký nguồn, ngày mong muốn và điều kiện xếp lịch cho màn Lịch hẹn & lịch sử | Bearer; Lễ tân Active + `QuanLyLichHen` |
| GET `/api/v1/appointments/{id}` | Chi tiết buổi hẹn, thời gian UTC | Bearer; cùng quyền tạo lịch |
| POST `/api/v1/registrations` | Tạo đầu đăng ký và toàn bộ chi tiết lẻ/gói trong một transaction; 201 + Location | Bearer; Customer có cả hai liên kết Active hoặc nhân viên Active LeTan |
| GET `/api/v1/registrations/settings` | Đọc phí khám/tư vấn để xem lại trước khi đăng ký | Bearer; cùng quyền G2 |
| GET `/api/v1/registrations?customerId=&recipientId=&page=&pageSize=` | Danh sách đăng ký SQL trong phạm vi hiện hành | Bearer; cùng quyền G2 |
| GET `/api/v1/registrations/{id}` | Chi tiết đăng ký, đơn giá đã chốt và thành phần gói | Bearer; cùng quyền G2 |
| GET `/api/v1/payment-registrations?search=&customerId=&recipientId=&page=&pageSize=` | Tra cứu đăng ký SQL và tiền phải thu | Bearer; ThuNgan Active + XemThanhToan |
| GET `/api/v1/registrations/{id}/payment` | Dịch vụ, giá snapshot và kết quả thanh toán mô phỏng | Bearer; ThuNgan + XemThanhToan hoặc Customer đủ ownership G2 |
| POST `/api/v1/registrations/{id}/payment` | JSON `{}`; ghi nhận toàn bộ một lần, 201 + Location | Bearer; ThuNgan Active + XemThanhToan + GhiThanhToan |

S1 dùng nguyên mô hình chức danh/quyền backend, không làm thêm UI quản trị quyền. Web S1 và Windows dùng `clientType=native` để nhận access/refresh trong response và chỉ giữ trong bộ nhớ; fetch Web dùng `credentials=omit`, không lưu browser storage/cookie. Luồng `clientType=web` cookie/Origin/CSRF hiện có được giữ nguyên. Không phục hồi phiên sau tải lại/đóng ứng dụng; logout gửi refresh token để thu hồi tại API. Chưa có HTTP quản lý tài khoản. Profiles S3 đã nối Web/Windows theo mục S3 bên dưới.

G1 sửa giá trị capabilities của `/auth/me` cho khớp API Profiles S3, không thêm trường/endpoint: `profilesRead=true` cho khách hàng đã xác minh và nhân viên Active; `profilesCreate=true` cho khách hàng (tạo hồ sơ bản thân/người được tiêm theo điều kiện S3) hoặc nhân viên `LeTan`; `profilesUpdate=true` cho khách hàng có liên kết Active hoặc nhân viên `LeTan`. Đây là khả năng theo tài khoản, API vẫn kiểm từng hồ sơ/liên kết khi ghi/sửa/xóa; Admin và các chức danh khác không được ghi hồ sơ. `selfClaim`/`representatives` vẫn false. Windows dùng capabilities cho ghi hồ sơ và hai mã quyền danh mục hiện có cho các nút ghi; mỗi lần đổi người dùng tạo shell và phiên bộ nhớ mới.

Đăng ký demo chỉ nhận địa chỉ `@example.invalid`, vẫn cần xác minh email. Cấu hình Development mail pickup ngoài Git theo `README.md` ở gốc dự án; token không được trả qua HTTP hoặc ghi log. Chưa gửi email thật.

Admin đầu tiên: lệnh `--seed-admin` chỉ ở Development, nhận `DevelopmentSeed:Username`/`DevelopmentSeed:Password` từ cấu hình local hoặc biến môi trường; không có credential cố định trong repo. Lệnh tạo Admin/Technical và quyền backend trong transaction, không đổi account/hash/quyền nếu đã có Admin; Production bị từ chối. Cách chạy Compose và sổ credential ngoài Git xem [README](../../README.md#tạo-dữ-liệu-và-tài-khoản-demo). Đây là lệnh khởi tạo local, không phải endpoint HTTP.

G1 thêm `--seed-reception` chỉ ở Development, cùng cấu hình local `DevelopmentSeed:Username`/`Password`: tạo nhân viên `[DEMO]` `LeTan`/Active và tài khoản Employee/None/Active, không có quyền mở rộng, hash/transaction/audit tại backend. Lặp lại theo username chuẩn hóa không đổi hash/trạng thái/dữ liệu; username thuộc chức danh khác bị từ chối. Production/Test không chạy lệnh này; test SQL có fixture riêng. Không đổi schema/migration hoặc quyền endpoint. Lệnh `provision-employee` trong IdentityOperator vẫn yêu cầu bản ghi nhân viên có sẵn và kiểm tra operator/Admin; không thêm HTTP quản trị.

## Tổng quan module và phần còn lại

Các mục đã triển khai được mô tả chi tiết ở phần bên dưới. Các mục chưa thực hiện phải đối chiếu SCOPE.md; bảng này không cấp phép tự mở rộng nghiệp vụ.

| Module | Endpoint dự kiến | Người làm |
|---|---|---|
| Identity | Quản lý accounts/permissions (hoãn ngoài S1) | Đạt |
| Promotions | GET/POST promotions; giảm tiền VND không âm trong thời gian hiệu lực, một ưu đãi mỗi đăng ký, không cộng dồn; chốt khoản giảm lúc đăng ký, không làm tổng âm | Bảo |
| Appointments/Reminders | S5.1/S5.2 lịch hẹn, S5.7A lịch sử Customer, S5.7B log nhắc lịch mô phỏng đã có; không bổ sung đổi/hủy lịch hoặc kênh gửi thật | Đạt |
| Aftercare | GET/POST vaccinations/{id}/follow-ups đã có trong S5.6 bên dưới | Đạt |
| Clinical — ghi tiêm | POST vaccinations; kết luận/chỉ định S5.3, ghi tiêm S5.5; không khám chi tiết | Đạt |
| Lô/tồn tối thiểu | Đọc lô phù hợp, trừ tồn và ghi biến động cùng thao tác ghi tiêm; nguồn seed Development/Test dự kiến trong G5, không mở module nhập kho | Đạt |
| Reports | GET reports; doanh thu theo tháng, số mũi theo vắc xin, đăng ký theo trạng thái; API chỉ đọc SQL và Windows, hoãn xuất file | Quý |

Endpoint nghiệp vụ chỉ được mở sau khi có xác thực và kiểm tra quyền tại API. Khách hàng chỉ đọc hồ sơ mình/người được tiêm được ủy quyền; vai trò nhân viên tách theo chức danh. Không lấy quyền từ trường do client gửi lên.

Tên endpoint dự kiến có thể đổi khi review. Sửa DTO, lỗi, nullable hoặc ID phải cập nhật cả hai client Web/Windows; ghi thay đổi vào PR và nhờ một người khác review. Thanh toán và ghi tiêm/trừ tồn cần transaction, chống ghi trùng và kiểm thử cạnh tranh/rollback tại nghiệp vụ; hoãn cơ chế idempotency tổng quát. Không tự kết nối ngân hàng, SMS hoặc gửi email thật.

G5 phải kiểm chứng đúng lịch/người/vắc xin, kết luận/chỉ định hợp lệ cho buổi tiêm, nhân viên Active đúng quyền/chức danh, lô phù hợp được phép sử dụng/còn hạn/tồn dương và điều kiện thanh toán. Ghi mũi, trừ tồn, biến động và audit cùng transaction; thiếu bằng chứng phải từ chối, không fallback Sample hoặc tự tạo chỉ định/lô. Seed lô/tồn tương lai chỉ ghi SQL synthetic `[DEMO]`, có số lô/vắc xin/ngày sản xuất/hạn dùng/số lượng ban đầu và tồn/trạng thái; chạy lại không nhân bản, bù tồn đã dùng, đổi hạn dùng hoặc ghi đè. Fixture hợp lệ/hết hạn/hết tồn nằm trong DB test theo [SCOPE](../../SCOPE.md#an-toàn-tối-thiểu-của-s5--g5--chưa-triển-khai).

G1/G2 có thể phát triển song song sau khi contract, quyền, actor và schema liên quan thống nhất và được review chéo; Đạt điều phối migration, không sửa đồng thời. R00 chỉ sửa mô tả phạm vi dự kiến; không đổi contract thực thi, chốt DTO mới hoặc triển khai endpoint/seed. Mỗi gói được giao riêng, xong thì dừng bàn giao.

## S2 — Catalog

RF-02B1.5 (#88): Catalog runtime chỉ đọc/ghi SQL, không có fallback in-memory. Phòng bệnh là taxonomy canonical; Công nghệ vắc xin là taxonomy kỹ thuật tham khảo N:N mới. Retire hai taxonomy cũ theo quyết định đã khóa; không thay ID số mà Registration/Inventory/Appointment/Clinical/Vaccination/Reports đang dùng.

| Route dưới `/api/v1/catalog` | Thao tác |
|---|---|
| `/diseases`, `/vaccine-technologies` | GET danh sách public; POST tạo; PUT/DELETE `/{id}` |
| `/vaccines`, `/packages` | GET danh sách public; POST tạo; PUT/DELETE `/{id}` |
| `/vaccines/{id}`, `/packages/{id}`, `/vaccine-technologies/{id}` | GET chi tiết public với đầy đủ graph; không tồn tại trả 404 `resourceNotFound` |
| `/suggestions` | GET public `{ manufacturers: string[], origins: string[], administrationRoutes: string[] }`; distinct giá trị SQL đã trim, đường dùng từ canonical backend constants; không thêm bảng hãng/quốc gia |

Danh sách trả `{ source: "database", items: [...] }`, không phân trang trong danh mục nhỏ của khóa luận. POST trả 201 và Location, PUT 200, DELETE 204. Detail trả trực tiếp DTO.

**Quyền/lỗi/transaction:** write giữ nhân viên Active có `QuanLyDanhMucChuyenMon`; vaccine/package cần thêm `QuanLyDanhMucGia`. Không lấy quyền từ client. Thiếu phiên 401, thiếu quyền 403, đầu vào sai 400 `validation`, không tồn tại 404 `resourceNotFound`, trùng code/tên lookup 409 `catalogDuplicate`, xóa bản ghi được tham chiếu 409 `catalogReferenced`, SQL/audit không ghi được 503 `dependencyUnavailable`. Vaccine + disease/technology links + regimen/doses + audit cùng transaction; một child hoặc audit lỗi rollback toàn bộ. Package + items + audit cũng atomic. Xóa vaccine chỉ xóa các child Catalog sở hữu (bệnh/công nghệ liên kết/phác đồ), không xóa lô/chỉ định/mũi/đăng ký; FK từ module khác chặn và rollback sạch. Xóa disease/technology đang được vaccine tham chiếu bị chặn. Mỗi ghi thành công có audit actor/thời điểm/resource.

**Code và trạng thái:** cả 4 danh mục có `code` required khi POST, max 50, ASCII uppercase/số/dấu `-`, regex `^[A-Z0-9]+(?:-[A-Z0-9]+)*$` (không nhận cả newline cuối), prefix `BENH-`, `CNVX-`, `VX-`, `GOI-` tương ứng. Unique case-insensitive tại SQL. Code immutable: PUT được bỏ code hoặc gửi đúng code hiện có; đổi tên không regenerate. Các text khác trim ở backend. `status` nhận `Active|Inactive`; POST mặc định Active nếu bỏ trường, PUT bỏ trường thì giữ trạng thái. Inactive có nghĩa không đang sử dụng, không phải trạng thái draft/nhập thiếu. Lookup mới cũng có status; các lookup legacy backfill Active; status vaccine/gói đã có được bảo toàn.

**Lookup/bệnh:** DTO `{ id, code, name, description, status }`; POST/PUT bỏ id. Name required, max 120 cho công nghệ, max 150 cho bệnh, unique case-insensitive; description optional max 500. Quan hệ vaccine–bệnh chỉ dùng `VacXin_Benh`, không có text PhongBenh trùng lặp. Public list nhận `search` (code/name, max 150) và `status`.

**Công nghệ vắc xin:** `/vaccine-technologies` CRUD/search/status; detail GET `/{id}`. DTO/request gồm `name`, `description` (500), `code` CNVX- (immutable), `status`, `technicalCharacteristics` (2000), `professionalNotes` (2000, tham khảo), `sourceName` (250), `sourceUrl` (1000, absolute http/https, không credentials), `informationUpdatedAt` (nullable ISO DateTimeOffset). Response thêm `id`. Name required max 120 unique CI; metadata nullable; PUT thay thế metadata (null/omitted xóa optional metadata). Không dùng technology tạo clinical rule/cảnh báo/lịch/chỉ định. Delete đang được vaccine tham chiếu trả 409, không cascade links.

**Vaccine response:** `id, name, manufacturer, origin, price, code, status, technologies, diseases, description, targetAudience, administrationRoute, administrationRouteNote, dosage, volume, sourceName, sourceUrl, informationUpdatedAt, regimens`. Mỗi nested technology/disease là `{ id, code, name }`. `PhanUngSauTiem` giữ trong SQL, không public/edit qua RF-02.

POST/PUT vaccine nhận các field scalar trên (bỏ id/nested lookup); dùng `diseaseIds: int[]` thay `diseases`, `technologyIds: int[]?` thay `technologies`. Quy tắc:

- Name/manufacturer required max 150, origin required max 100. Không nhận VaccineTypeId/InjectionTypeId. `technologyIds` không bắt buộc: create null/omitted/[] hợp lệ; PUT null/omitted giữ links, [] chủ động xóa hết. Nếu gửi phải positive, distinct, tồn tại; chỉ add Active, existing Inactive được giữ hoặc bỏ rõ ràng, không tự lọc/xóa ngầm. Không suy công nghệ theo tên vaccine.
- Bản ghi mới phải có targetAudience (max 1000), administrationRoute và ít nhất một disease có thật. Active mới phải có ít nhất một disease Active. DiseaseIds không trùng, không nhận mảng rỗng khi gửi thay thế. PUT cũ không gửi diseaseIds giữ liên kết; không bắt migration bịa bệnh cho legacy. Kích hoạt lại vaccine phải có bệnh Active, đối tượng và đường dùng hợp lệ.
- `description` optional max 2000; targetAudience là mô tả tham khảo, không là eligibility. Đường dùng canonical: `Intramuscular`, `Subcutaneous`, `Intradermal`, `Oral`, `Nasal`, `Other`. Chỉ Other nhận/bắt buộc `administrationRouteNote` (max 500); route chuẩn không nhận note tùy ý.
- `dosage` tái sử dụng `LieuLuong`, max 200, không parse thành clinical logic. `volume` tái sử dụng `TheTich`, decimal không âm, tối đa 99.999.999,99 và tối đa 2 số lẻ. Nghĩa là **thể tích (mL)**; không suy thành thể tích mỗi liều. Legacy 0/empty được giữ, không bịa giá trị. Bỏ volume/dosage khi PUT giữ dữ liệu cũ.
- `price` required, số nguyên VND không âm, tối đa 9.999.999.999.999.999.
- `sourceName` optional max 250, `sourceUrl` optional max 1000, URL tuyệt đối HTTP(S) có host và không chứa user/password; `informationUpdatedAt` nullable ISO 8601 DateTimeOffset. Nguồn chỉ là tham khảo, không chứng minh đã duyệt y khoa. Không đổi legacy Active thành Inactive vì thiếu nguồn.
- PUT bỏ/null các field mới giữ giá trị cũ; chuỗi rỗng xóa nội dung optional. Không nhận chuỗi rỗng cho targetAudience khi được gửi. `regimens: null`/bỏ trường giữ graph; `regimens: []` xóa các phác đồ Catalog. Không sửa legacy PhanUngSauTiem.

**Phác đồ tham khảo:** request regimen `{ name, targetAudience, summary, sourceName?, sourceUrl?, doses }`; name required max 150, targetAudience required max 1000, summary optional max 1500, nguồn cùng giới hạn vaccine. Response thêm `id` và `doses[].id`. Mỗi dose `{ doseOrder, timingLabel, minimumIntervalDays?, recommendedIntervalDays?, note? }`: order 1–20 unique trong regimen, timingLabel required max 250, interval nguyên không âm, recommended >= minimum khi có cả hai, note max 500. Regimen có 1–20 dose; một vaccine có 0..N regimen. PUT gửi regimens thay toàn bộ child graph và ID child có thể đổi; không có FK từ module nghiệp vụ sang các child tham khảo này. Số mũi tính từ số dose rows, không lưu TotalDose. Không tự chọn theo DOB, tính ngày, tạo lịch/chỉ định/mũi hoặc thay safety gate S5.

**Gói response:** giữ `{ id, name, price, items: [{ vaccineId, quantity, vaccineName }] }`; thêm `code, description, targetAudience, status, totalDoses, diseases` và `items[].vaccineCode`. totalDoses = SUM(quantity); diseases = union distinct bệnh của components, không lưu cột/text dư thừa. POST/PUT nhận name/price/items + code/status/description/targetAudience; items chỉ cần vaccineId/quantity. Name max 150, description max 2000, targetAudience max 1000 và bắt buộc cho gói mới; không nhận chuỗi rỗng khi gửi targetAudience. 1–100 component khác nhau, quantity nguyên 1–100; ID phải tồn tại. Resulting status Active (create/update/activation) chỉ nhận vaccine Active; Inactive được giữ component Inactive. Chuyển vaccine sang Inactive khi còn trong gói Active trả 409 `catalogReferenced`; nhân viên phải chủ động ngừng sử dụng gói trước, không tự đổi trạng thái gói/vaccine. Ba thao tác PUT vaccine, POST/PUT package dùng khóa transaction `KLCN.Catalog.PackageStatus` trước khi đọc hai phía để giữ invariant khi hai nhân viên ghi đồng thời, vẫn giữ khóa hàng gói của Registration. Giá theo giới hạn vaccine. PUT thay items trong transaction khi tập ID/quantity đổi. Nếu đã có `ChiTietGoiVacXin` tham chiếu, thay thành phần trả 409 `catalogReferenced`; cùng thành phần vẫn cho đổi giá/tên/metadata. Catalog PUT và Registration khóa cùng hàng gói trước kiểm tra/cập nhật tới commit, giữ behavior snapshot/catalogChanged.

**Search/filter:** vaccines và packages nhận `search`, `diseaseId`, `technologyId`, `minPrice`, `maxPrice`, `status`. ID filter >0; min/max là integer VND hợp lệ, min <= max. Không gửi status thì trả cả hai trạng thái; không tự đổi Registration/Inventory/Clinical để ẩn Inactive. Vaccine search code/name/manufacturer/disease name. Package search code/name/component code/name/disease name; bệnh và công nghệ filter phải khớp cùng một component. Query dùng EXISTS nên không nhân bản kết quả khi nhiều bệnh/component cùng match. Website mặc định Active để RF-02C.

**Migration/seed:** `20261103183142_RF02ACatalogSchemaContract` thêm 5 Code unique case-insensitive; status cho 3 lookup; metadata vaccine/gói; nới LieuLuong 100→200; bảng `VaccineRegimen`, `VaccineRegimenDose` và unique (RegimenId,DoseOrder)/check interval. Giữ PK/FK và toàn bộ VacXin_Benh, LieuLuong/TheTich/PhanUngSauTiem cũ. Backfill từ tên: uppercase, transliterate tiếng Việt sang ASCII, punctuation thành một `-`, bỏ combining marks, cắt tối đa 50; xử lý theo ID tăng dần. Chỉ collision mới thêm `-<technicalId>` (thêm counter nếu chính candidate đó cũng trùng). Không dùng ID làm toàn bộ code. Nếu tên lookup legacy trùng case-insensitive hoặc tên không thể tạo slug ASCII có nghĩa, migration THROW rõ và rollback, yêu cầu owner xử lý trước khi áp dụng; không tự đổi tên/xóa row. Không tạo bệnh/đối tượng/nguồn/phác đồ cho legacy.

`--seed-catalog` vẫn Development-only, idempotent, SQL thật: hai vaccine `[DEMO]`, hai bệnh giả, vaccine A liên kết hai bệnh, mỗi vaccine có regimen hai bước synthetic, một gói A+B có components; code semantic và SourceName `Dữ liệu mô phỏng`. Không claim dữ liệu nội bộ VNVC; seed chạy lại không ghi đè giá/nội dung/status hiện hữu. Nếu cần tạo gói DEMO mới mà vaccine DEMO hiện có không Active, seed từ chối/rollback thay vì tạo gói Active không hợp lệ hoặc tái kích hoạt vaccine. RF-02A không migrate/reset/reseed database demo. Migration bổ sung `20261103192651_RF02AActivePackagePreflight` chỉ preflight: gói Active có vaccine không Active sẽ THROW 51000 với ID gói/vaccine, không ghi migration thành công hoặc đổi trạng thái/dữ liệu. Owner xử lý rõ ràng rồi chạy lại; không sửa migration cũ, không thêm bảng/cột/index/FK ở migration preflight.

**Windows compatibility được người dùng bổ sung cho RF-02A:** giữ màn/layout CE.*; thêm Code auto-suggest khi tạo, chỉ đọc khi sửa; checkbox bệnh Active, target audience, ComboBox đường dùng từ suggestions, note khi Other; gói thêm Code/target. Giữ status hiện hữu khi PUT, không chuyển Inactive để né dữ liệu thiếu. Với edit legacy, target gốc null còn để trống và disease links gốc rỗng vẫn chưa chọn sẽ gửi null để preserve; route gốc null giữ null. Nếu metadata gốc đã có, thao tác xóa gửi rỗng (kể cả route xóa selection → chuỗi rỗng) để backend chặn; create vẫn bắt buộc metadata theo contract. Không biến field legacy thiếu thành explicit empty. Không thêm disease CRUD UI/searchable chips/autocomplete/regimen editor/detail nâng cao; các phần đó vẫn RF-02B. Contract/schema này cần coordinator/owner liên quan review đúng phạm vi trước merge; RF-02A chỉ mở PR, không tự merge.

**RF-02B1.5 retirement/migration:** migration `RF02B15CatalogSemantics` tạo `CongNgheVacXin` và `VacXin_CongNghe(MaVacXin,MaCongNghe)` composite PK/FK Restrict, unique CI Code/Name và status check. Drop FK/index/assignment `VacXin.MaLoaiVacXin`, `VacXin.MaLoaiTiemChung` và hai bảng legacy; không convert dữ liệu cũ. Preflight từ chối FK taxonomy ngoài hai FK đã audit trước khi drop. Không xóa/re-key vaccine hoặc core transaction rows. Down từ chối vì không thể tái dựng assignment đã bỏ mà không suy diễn; cần backup trước khi áp dụng, restore backup khi cần rollback. Migration/seed chỉ chạy trên SQL disposable khi kiểm; không apply demo. Seed mới chỉ technology synthetic `[DEMO]`; không gán công nghệ cho vaccine thật, không đổi links vaccine đã có khi seed lặp.

**RF-02B1.5 clients:** Windows giữ shell B1 với 4 resource **Phòng bệnh | Công nghệ vắc xin | Vắc xin | Gói vắc xin**; technology CRUD đủ metadata/nguồn/ngày, vaccine checkbox N technology không bắt buộc, Inactive hiện hữu hiển thị/giữ, Inactive chưa liên kết không cho chọn mới. Web bỏ injection filter, type filter thay bằng Công nghệ vắc xin/technologyId. Routes `/injection-types`, `/vaccine-types` không còn đăng ký (404); old vaccine fields/read objects/filter parameters rời active contract, không alias sang technology. B2/B3/C chưa triển khai trong lát này.

## S3 — Profiles

Tái sử dụng Profiles M1 và AccountProfileLink. Nhân viên Active đọc được tất cả hồ sơ; ghi nhân viên vẫn yêu cầu chức danh LeTan. Khách hàng chỉ đọc/sửa/xóa hồ sơ có liên kết Active của chính tài khoản.

- POST `/customers`: `{ fullName, identityNumber?, phoneNumber, email, salutation, dateOfBirth }`. Khách hàng chưa có hồ sơ được tạo một hồ sơ và liên kết Active trong cùng transaction. Đã có trả 409 `profileAlreadyExists`. PUT `/customers/{id}` cùng body, trả CustomerDto. Danh xưng bắt buộc thuộc Ông/Bà/Anh/Chị/Khác. Ngày sinh bắt buộc theo định dạng YYYY-MM-DD và không sau hôm nay tại Việt Nam (UTC+7). CustomerDto trả null cho trường chưa cập nhật của hồ sơ cũ. CCCD nullable/rỗng được lưu NULL; nếu có phải đúng 12 chữ số ASCII và duy nhất giữa khách hàng. Tên bắt buộc tối đa 150, điện thoại 10–11 chữ số, email hợp lệ tối đa 254.
- POST `/recipients`: `{ customerId, fullName, identityNumber?, dateOfBirth, gender, relationshipToCustomer }`. Khách hàng phải sở hữu customerId; API tạo liên kết Active cho người được tiêm mới. PUT `/recipients/{id}` cùng body bỏ customerId, không chuyển chủ hồ sơ. Ngày sinh YYYY-MM-DD không sau hôm nay tại Việt Nam; gender Nam/Nữ/Khác. Quan hệ của người được tiêm đối với Customer thuộc Bản thân/Con/Anh/Chị/Em/Cha/Mẹ/Vợ/Chồng/Ông/Bà/Cùng hộ khẩu. Khi Customer có ngày sinh, ngày sinh người được tiêm phải bằng (Bản thân), sau (Con), trước (Cha/Mẹ/Ông/Bà), trước hoặc bằng (Anh/Chị), hoặc sau hoặc bằng (Em). Vợ/Chồng/Cùng hộ khẩu không ràng buộc tương quan ngày sinh.
- POST `/customers/claim`: `{ fullName, phoneNumber, dateOfBirth }`. Chỉ tài khoản Customer đã xác minh email và chưa có liên kết Customer Active. Email đã xác minh phải khớp email hồ sơ. Thành công trả 200 và CustomerDto vừa liên kết.
- AUTH-R2 (#145): POST `/recipients/find` không yêu cầu đăng nhập, nhận cùng hai payload của `/recipients/claim` bên dưới. Chỉ trả `200 { "matched": true|false }`, `Cache-Control: no-store`; không trả ID/PII/lịch sử, không tạo session/token/link/audit claim. `true` chỉ nghĩa là khớp duy nhất, không cho biết hồ sơ còn khả dụng hay đã liên kết; không khớp/nhiều kết quả cùng trả `false`. Trường không hợp lệ/trộn mode trả 400 generic, không trả chi tiết trường khớp. Dùng chung ngân sách 30 lần/IP/15 phút với claim (kể cả payload sai), 429 + Retry-After khi vượt ngưỡng. Đây là giới hạn trong tiến trình API hiện có, không phải cơ chế chống dò phân tán. Web chỉ giữ draft trong bộ nhớ lần mở trang; sau đăng nhập/xác minh email phải xác nhận claim bằng payload gốc, server xác minh lại dữ liệu/ownership trong transaction hiện có. Tải lại trang/mở email ở trang khác cần tìm lại; không lưu draft/token vào browser storage.
- POST /recipients/claim (RF-05A0.2A, #120): Customer đăng nhập chọn đúng một trong hai payload: {"mode":"profileCode","profileCode":"VAC-ABCDEF123456","phoneNumber":"0901234567"} hoặc {"mode":"personalInfo","phoneNumber":"0901234567","fullName":"Nguyễn Văn A","dateOfBirth":"2000-01-01"}. Thiếu trường hoặc trộn hai mode trả 400.
- Khi chưa liên kết Customer, nếu chỉ khớp một Recipient và cả hai hồ sơ đều khả dụng, API liên kết Customer và Recipient cùng transaction với audit. Nếu đã liên kết Customer, chỉ xét Recipient thuộc Customer đó. Thử lại với Recipient đã liên kết cùng tài khoản trả thành công. Không khớp, nhiều kết quả hoặc liên kết xung đột đều trả 409 profileClaimUnavailable.
- Thành công trả 200 với customer (CustomerDto) và recipient (RecipientDto). RecipientDto thêm profileCode do hệ thống sinh, không nhận trong create/update. Mã VAC- và 12 ký tự hex viết hoa là mã công khai của người được tiêm, không dùng ID nội bộ.
- Claim so khớp chính xác sau trim, không phân biệt hoa thường cho tên/email. Không khớp, trùng nhiều hồ sơ hoặc hồ sơ đã thuộc tài khoản khác đều trả 409 `profileClaimUnavailable`. Hai endpoint dùng chung giới hạn trong tiến trình API (6 lần/tài khoản và 30 lần/IP trong 15 phút), trả 429 khi vượt ngưỡng. Audit ghi người thực hiện, phiên, loại hồ sơ và kết quả, không ghi dữ liệu tra cứu. `/auth/me.capabilities.selfClaim` cho biết tài khoản hiện có thể liên kết Customer.
- DELETE `/customers/{id}` và `/recipients/{id}`: 204; xóa liên kết cùng transaction. FK nghiệp vụ đang tham chiếu chặn xóa với 409 `profileReferenced`, rollback giữ hồ sơ/liên kết. Không xóa dây chuyền hồ sơ bé hay lịch sử tiêm.
- Danh sách giữ phân trang page/pageSize (tối đa 100); chỉ liên kết người được tiêm trực tiếp cho phép khách hàng xem bé, không tự nhận các hồ sơ bé đã có từ liên kết khách hàng.
- Ghi kiểm phiên/quyền/phạm vi ở API trong transaction, lưu audit người thực hiện/thời điểm. Thiếu phiên 401, trái phạm vi/quyền 403, dữ liệu sai 400 `validation`, thiếu hồ sơ 404, CCCD khách hàng trùng 409 `duplicateIdentity`.

Web: Tài khoản → Hồ sơ của tôi, tạo/sửa hồ sơ và người được tiêm. Windows: Hồ sơ → tải/tìm, tạo/sửa/xóa khách hàng và bé qua API thật. Nullable CustomerDto.IdentityNumber là thay đổi contract cần review chéo trước merge.

## RF-04A — Thông tin liên hệ, ngày mong muốn và phí khám/tư vấn

Contract dưới đây bổ sung G2/G3 và áp dụng cho Web và Windows.

- `GET /api/v1/registrations/settings` yêu cầu cùng phiên/quyền đọc đăng ký, trả `{ consultationFee }` cho bước xem lại. API đọc `Registration:ConsultationFeeVnd` lúc khởi động (biến môi trường `Registration__ConsultationFeeVnd`). Thiếu key dùng 0. Chỉ nhận chuỗi chữ số biểu diễn VND nguyên trong giới hạn 0–9 999 999 999 999 999. Cấu hình sai trả 503 `dependencyUnavailable` ở GET settings và POST đăng ký.
- POST bắt buộc `preferredDate` dạng `yyyy-MM-dd`, từ hôm nay tại UTC+07:00. Lưu 00:00:00 của ngày đã chọn vào `ThoiGianDuKien` và đọc phần ngày trực tiếp. Đây là ngày mong muốn, luồng này chưa tạo lịch.
- POST bắt buộc `contact: { fullName, phoneNumber, email, provinceCity, wardCommune, addressLine }`. Tên/tỉnh hoặc thành phố/phường hoặc xã/địa chỉ được trim, độ dài lần lượt 1–150/1–100/1–100/1–250. Điện thoại 10–11 chữ số ASCII, email hợp lệ tối đa 254 ký tự. Contact được lưu riêng trên đăng ký; Customer master giữ dữ liệu hồ sơ.
- API chốt phí cấu hình tại POST; request chỉ gửi hồ sơ, contact, ngày và items. `subtotal` vẫn là tổng dòng dịch vụ. API giới hạn `totalBeforeDiscount = subtotal + consultationFee` theo giới hạn VND. Detail trả `preferredDate`, `contact`, `consultationFee` và `totalBeforeDiscount`; list trả các trường mới trừ contact. Trường lịch sử chưa có dữ liệu trả null và tổng coi phí null bằng 0.
- Payment list/detail thêm `consultationFee`. `subtotal` vẫn là tiền dịch vụ và `totalDue` gồm phí đã chốt. Cashier chỉ ghi đúng toàn bộ `totalDue`. S5 dùng cùng số tiền để kiểm đủ thanh toán. Đăng ký có mã ưu đãi tiếp tục bị từ chối theo quy tắc G3 hiện hành.

## G2B — Registration

Một đăng ký gồm một khách hàng, một người được tiêm và 1–100 dòng vắc xin lẻ/gói. Không mở sửa/xóa/hủy/chuyển trạng thái hoặc `POST registrations/{id}/items`. Không có giỏ hàng SQL, giữ tồn, hàng đợi, lịch, thanh toán, khám, ghi tiêm hoặc ưu đãi trong G2. Thành công chỉ có nghĩa đã lưu đăng ký, không có nghĩa đủ điều kiện tiêm.

### Tạo đăng ký

`POST /api/v1/registrations` nhận:

```json
{
  "customerId": 12,
  "recipientId": 34,
  "preferredDate": "2026-11-05",
  "contact": {
    "fullName": "Liên hệ thử",
    "phoneNumber": "09123456789",
    "email": "contact@example.invalid",
    "provinceCity": "Tỉnh thử",
    "wardCommune": "Phường thử",
    "addressLine": "Địa chỉ thử"
  },
  "items": [
    { "kind": "vaccine", "serviceId": 5, "quantity": 2, "expectedUnitPrice": 150000 },
    { "kind": "package", "serviceId": 3, "quantity": 1, "expectedUnitPrice": 350000,
      "expectedComponents": [ { "vaccineId": 5, "quantity": 1 }, { "vaccineId": 6, "quantity": 1 } ] }
  ]
}
```

- ID nguyên dương; `kind` chỉ `vaccine` hoặc `package`; số lượng nguyên 1–100. Không trùng `(kind, serviceId)` trong request. Không tự gộp/xóa dịch vụ chỉ vì một vắc xin cũng nằm trong gói.
- `expectedUnitPrice` bắt buộc kể cả giá 0, số nguyên VND từ 0 đến 9.999.999.999.999.999. Tổng tiền cũng không vượt giới hạn này. Các giới hạn số dòng/số lượng là giới hạn kỹ thuật của request, không phải phác đồ hoặc điều kiện y khoa.
- Dòng gói bắt buộc `expectedComponents`: 1–100 vaccine ID khác nhau, quantity 1–100; so sánh tập ID/số lượng không phụ thuộc thứ tự. Dòng lẻ bỏ trường này hoặc gửi null. Thuộc tính JSON không được định nghĩa bị từ chối ở cả request, dòng và thành phần.
- Các trường expected chỉ kiểm nội dung người dùng đã xem. API đọc giá/trạng thái/thành phần SQL trong transaction; giá gói lấy `GiaGoi`, không cộng giá thành phần. Giá/thành phần khác expected trả 409 `catalogChanged`; không tạo đăng ký. Client tải lại danh mục, giữ bản nháp và cho người dùng xem/xác nhận lại; không tự retry POST khi timeout.
- Dịch vụ và vắc xin thành phần phải tồn tại, có trạng thái `Active`; gói không rỗng, số lượng thành phần hợp lệ. Không kiểm tồn/tuổi/phác đồ/chỉ định trong G2. Catalog runtime chỉ có nguồn SQL; POST đăng ký luôn đối chiếu SQL, không fallback.
- Server tự gán `id`, `createdAt` UTC, `status="DaDangKy"`, `employeeId` từ phiên nhân viên hoặc null với Customer. `HinhThucTiem` và `MaKhuyenMai` để NULL. `ThoiGianDuKien` lưu ngày mong muốn theo RF-04A. Không nhận nhân viên, quyền, trạng thái, tổng tiền, thời điểm, hồ sơ mới hoặc ưu đãi từ request.
- Đầu đăng ký, hai loại chi tiết có `GiaTaiThoiDiemDangKy` và `SecurityAudit` (`registrations.created`, ActorAccountId, SessionId, OccurredAt, ResourceType/ResourceId) commit cùng nhau. Lỗi xác thực/validation/danh mục/ghi/audit rollback toàn bộ. Dùng transaction Serializable hiện có; gói được khóa theo thứ tự ID và Catalog PUT dùng cùng khóa gói, giá vắc xin được giữ ổn định tới commit. Chưa có idempotency tổng quát.

201 trả `Location: /api/v1/registrations/{id}` và `RegistrationDto`:

```text
{ id, createdAt, status,
  customer: { id, fullName }, recipient: { id, fullName }, employeeId: int|null,
  items: [{ id, kind, serviceId, name, quantity, unitPrice, lineTotal,
            components: null | [{ vaccineId, quantity, vaccineName }] }],
  preferredDate, contact, subtotal, consultationFee, totalBeforeDiscount, currency: "VND" }
```

`id` của dòng là khóa chi tiết tương ứng; cần đi cùng `kind` vì hai bảng có thể trùng ID số. Các dòng trả lẻ trước, gói sau, mỗi loại theo ID tăng dần. `unitPrice` đọc giá đã lưu; `lineTotal=quantity*unitPrice`; `subtotal` cộng các dòng, không lấy giá Catalog mới. `components.quantity` là số lượng trong **một** gói. Tên khách hàng/người tiêm/dịch vụ vẫn là tên hiện hành, không có snapshot tên. Thành phần gói được bảo toàn bằng chặn sửa gói đã tham chiếu, không thêm bảng snapshot.

### Đọc và phân quyền

- Customer: tài khoản Active, email đã xác minh, phiên hợp lệ; cần liên kết `AccountProfileLink` Active trực tiếp với **cả** Customer và Recipient, và `NguoiDuocTiem.MaKhachHang` đúng khách hàng đăng ký. Không tự nhận hồ sơ bằng CCCD/email, không suy quyền từ liên kết Customer sang mọi Recipient.
- Nhân viên Active chức danh `LeTan` được tạo/xem mọi đăng ký phục vụ tiếp nhận; các chức danh khác, kể cả Admin, bị 403. Không thêm mã quyền/capabilities Identity. Client dùng `accountType`/`employee.title` hiện có để thể hiện UI; API vẫn kiểm hiện hành.
- Quyền xem theo liên kết hiện tại, không theo tài khoản từng tạo: khách hàng đủ liên kết xem được đăng ký do Lễ tân tạo; mất một liên kết thì đăng ký biến mất khỏi danh sách, detail bị từ chối.
- GET danh sách trả `{ items: [{ id, createdAt, status, customer, recipient, employeeId, preferredDate, subtotal, consultationFee, totalBeforeDiscount, currency }] }`. Bộ lọc customerId/recipientId chỉ thu hẹp phạm vi. Mặc định page=1/pageSize=25, pageSize tối đa 100, thứ tự ID giảm dần; trang rỗng trả `items: []`. Không trả count tổng hoặc chi tiết dịch vụ trong danh sách.
- GET detail trả cùng `RegistrationDto` như POST. Customer gặp ID không tồn tại hoặc ngoài phạm vi đều nhận 403 `permissionDenied` để không phân biệt sự tồn tại. Lễ tân gặp ID không tồn tại nhận 404. ID/query không hợp lệ trả 400. Response Registration đặt `Cache-Control: no-store`.

Lỗi dùng `{ type, title, status, code, traceId, errors? }`: 400 `validation` (kể cả JSON sai/trường lạ), 401 `invalidSession`, 403 `permissionDenied`/`emailVerificationRequired`, 404 `resourceNotFound` (hồ sơ/dịch vụ thiếu trong phạm vi được phép), 409 `catalogChanged`; lỗi phụ thuộc SQL/ghi dữ liệu trả 503 `dependencyUnavailable` không chứa SQL/PII/credential. Không tạo audit thành công ngoài transaction đã rollback.

### Schema và ranh giới tích hợp

Migration mới `20261028172727_G2RegistrationNullableFields` chỉ cho phép NULL ở ba cột `DangKyDichVu.MaNhanVien`, `HinhThucTiem`, `ThoiGianDuKien`, giữ FK và dữ liệu cũ; không thêm bảng/cột khác. Nâng cấp qua EF migrations trên DB test riêng; không tự áp dụng database demo. Down từ chối nếu còn bản ghi có NULL ở ba cột, không điền nhân viên/thời gian/chuỗi giả để rollback.

G3 dùng giá chi tiết đã chốt cho thanh toán một lần có nhãn mô phỏng; tổng 0 được phép ở G2 nhưng không tạo thanh toán 0. G4 dùng ID đăng ký/chi tiết và thời gian nhân viên nhập; chưa sửa `LichHenTiem` trong G2. G5 tự kiểm chỉ định/thanh toán/lô/tồn/quyền khi ghi tiêm. G6 sẽ thêm chọn ưu đãi và snapshot giảm VND lúc tạo; không hồi tố giá/ưu đãi cho đăng ký G2 cũ. G2B chưa thay UI hoặc đánh dấu S4 hoàn tất; contract/schema vẫn cần review chéo trước merge.

## G3B / RF-04B1 — Thanh toán toàn bộ một lần (mô phỏng)

Chỉ ghi nhận trên SQL để demo khóa luận, không xử lý hoặc xác nhận tiền thật. Không có đặt cọc, trả góp, thu một phần, hoàn tiền, cổng ngân hàng, hóa đơn thật, SMS hoặc hàng đợi. Windows Thu ngân ghi thanh toán; Website khách hàng đọc kết quả. Không có alias `/payments` hoặc đường dẫn `registrations/{id}/payments`.

### Quyền và ownership

- Mọi endpoint kiểm phiên/tài khoản/nhân viên hiện hành bằng Identity. Nhân viên phải Active. GET danh sách chỉ cho `ThuNgan` có `XemThanhToan`; GET chi tiết cũng cho Customer có email xác minh và đủ cả hai liên kết `AccountProfileLink` Active, đúng cặp Customer/Recipient theo G2.
- POST chỉ cho `ThuNgan` có đồng thời `XemThanhToan` và `GhiThanhToan`. Thu ngân chỉ có quyền xem vẫn tra cứu và đọc chi tiết, `canRecordPayment=false`. Admin, LeTan và chức danh khác bị 403 kể cả có các mã quyền này. Không thay seed/quyền/capabilities Identity hoặc quyền endpoint đăng ký G2.
- Customer không được gọi danh sách Thu ngân hoặc POST. GET chi tiết ngoài ownership và ID không tồn tại đều trả 403 `permissionDenied`; Thu ngân đủ quyền gặp ID không tồn tại nhận 404 `resourceNotFound`. Thu hồi một trong hai liên kết là mất quyền xem, không dựa vào người đã tạo đăng ký hay CCCD/email.
- Tất cả response tài chính, kể cả lỗi binding/validation, có `Cache-Control: no-store`.

### Tra cứu và DTO

`GET /api/v1/payment-registrations?search=&customerId=&recipientId=&page=&pageSize=`: search sau trim tối đa 100 ký tự, tìm mã đăng ký số hoặc `DK-0001`, tên Customer/Recipient hiện hành; bộ lọc customerId/recipientId chỉ thu hẹp kết quả. ID bộ lọc phải dương; page mặc định 1, pageSize mặc định 25, tối đa 100; chặn skip vượt int. Thứ tự ID giảm dần, trang rỗng trả `items: []`; không có tổng count.

```text
PaymentRegistrationListResponse = { items: PaymentRegistrationSummaryDto[] }
PaymentRegistrationSummaryDto = {
  id, createdAt, customer: { id, fullName }, recipient: { id, fullName },
  subtotal, consultationFee, totalDue, paidAmount, remainingAmount, paymentId: int|null,
  currency: "VND", paymentState, expectedChannel
}
```

`GET /api/v1/registrations/{id}/payment` trả 200 `RegistrationPaymentDto`; đăng ký chưa có giao dịch vẫn trả 200 với `payment:null`:

```text
RegistrationPaymentDto = {
  registrationId, registrationStatus, customer, recipient,
  items: [{ id, kind, serviceId, name, quantity, unitPrice, lineTotal, components }],
  subtotal, consultationFee, totalDue, paidAmount, remainingAmount, currency: "VND",
  requiresPayment, canRecordPayment, canConfirmPendingPayment, isSimulation: true, message,
  paymentState, expectedChannel, registrationContact: null | RegistrationContactDto,
  payment: null | PaymentDto
}
PaymentDto = {
  id, registrationId, amount, employeeId: int|null, occurredAt,
  type: "ToanBo", method, status, isSimulation: true,
  transactionCode, payer, provider, holderName, maskedIdentifier, reference, cashTendered, changeAmount
}
```

`customer`/`recipient` và `items` dùng DTO G2, cùng quy tắc ID dòng phải đi với kind, giá snapshot và thành phần gói. Tên là tên hiện hành, không tạo snapshot tên mới. `createdAt`/`occurredAt` là ISO UTC. Tiền là JSON decimal VND nguyên, từ 0 đến `9999999999999999` như G2; client Web phải giữ chính xác các trường tiền lớn hơn Number.MAX_SAFE_INTEGER.

Server tính `subtotal = Σ(SoLuong*GiaTaiThoiDiemDangKy) + Σ(SoLuongGoi*GiaTaiThoiDiemDangKy)` từ giá snapshot. `totalDue=subtotal+(consultationFee ?? 0)`. Chỉ `ThanhCong` hợp lệ mới được tính vào `paidAmount`. `ChoXacNhan` có `paidAmount=0` và `remainingAmount=totalDue`. `ChuaThanhToan` biểu thị đăng ký chưa có giao dịch. `requiresPayment` cho biết còn phải thu. `canRecordPayment` yêu cầu quyền ghi, tổng dương và vị trí giao dịch còn trống.

`expectedChannel` nhận `TaiTrungTam`, `TrucTuyen` hoặc null trong DTO đăng ký và DTO thanh toán. Đây là kênh dự kiến. B1 chỉ đọc.

Tổng 0: subtotal/totalDue/paidAmount/remainingAmount đều 0, payment=null, requiresPayment=false, canRecordPayment=false, message="Không phát sinh khoản thu."; không gọi đây là đã thanh toán. Nhãn mô phỏng có ở isSimulation, giao dịch và message. Không có trạng thái giao dịch Đặt cọc/Trả góp/Đang xử lý/Thất bại.

### Ghi nhận, cạnh tranh và lỗi

`POST /api/v1/registrations/{id}/payment` nhận `{ payer, method, cashTendered?, card?, transfer? }` với đúng dữ liệu của phương thức được chọn. Server gán số tiền, nhân viên, mã giao dịch, thời điểm, trạng thái và loại `ToanBo`. JSON có trường lạ bị từ chối. Thành công trả 201, Location `/api/v1/registrations/{id}/payment` và `RegistrationPaymentDto`. ID phải là số nguyên dương.

- `payer` gồm `fullName` (1–150), `phoneNumber` (10–11 chữ số ASCII) và các trường tùy chọn `email` (địa chỉ hợp lệ, tối đa 254), `provinceCity` (100), `wardCommune` (100), `addressLine` (250). A08 điền từ snapshot liên hệ của đăng ký và lưu snapshot riêng của giao dịch.
- `TienMat` yêu cầu `cashTendered` nguyên VND trong đoạn từ tổng phải thu đến MaxMoney. `amount=totalDue`, `changeAmount=cashTendered-totalDue`.
- `TheTaiQuay` yêu cầu `card: { cardBrand, cardholderName, cardNumber, expiryMonth, expiryYear, authorizationReference }`. Loại thẻ VISA, MASTER, JCB, ATM hoặc Khác. Số thẻ mô phỏng có 12–19 chữ số ASCII sau khi bỏ dấu cách, qua Luhn. Tháng/năm hết hạn tính theo Việt Nam (UTC+7). Mã xác thực 1–100 ký tự. Chỉ lưu loại thẻ, tên chủ thẻ, dạng `****` và bốn số cuối, mã xác thực.
- `ChuyenKhoanQR` yêu cầu `transfer: { sourceBankOrWallet, accountHolderName, sourceAccountOrWalletId, transferReference }`. Nguồn và mã tham chiếu dài 1–100, tên chủ dài 1–150. Định danh nguồn dài 1–100, dùng chữ cái/chữ số ASCII và ký tự `+-_.@`. Chỉ lưu nguồn, tên chủ, định danh đã che và mã tham chiếu.

`POST /api/v1/registrations/{id}/payment/confirm` dùng cùng quyền Thu ngân và khóa đăng ký. Nhận giao dịch `ChuyenKhoanOnline` hợp lệ đang `ChoXacNhan` với `employeeId=null`. Cập nhật chính bản ghi sang `ThanhCong`, gán nhân viên xác nhận và thời điểm UTC, ghi audit `payments.confirmed` cùng transaction. Trả 200 với chi tiết, 409 `paymentNotPending` khi thiếu giao dịch chờ hoặc đã được xác nhận. `canConfirmPendingPayment` chỉ true cho Thu ngân đủ quyền với giao dịch chờ hợp lệ.

Transaction Serializable dùng lại `IdentityTransactions`: kiểm/khóa account → session, quyền/nhân viên hiện hành → `DangKyDichVu WITH (UPDLOCK,HOLDLOCK)` → kiểm đúng cặp hồ sơ, trạng thái `DaDangKy`, chưa có ưu đãi chưa snapshot, 1–100 dòng, số lượng 1–100 và giá VND hợp lệ → kiểm giao dịch cũ → ghi ThanhToan → ghi SecurityAudit → commit. Không cập nhật trạng thái đăng ký hoặc tạo lịch/chỉ định/mũi tiêm. SecurityAudit có action `payments.recorded`, outcome `Success`, ResourceType `Payment`, ResourceId là payment ID, ActorAccountId/SessionId từ phiên. Lỗi thanh toán hoặc audit trước commit rollback cả hai.

Legacy `MoPhong` được đọc với nhân viên hợp lệ, trạng thái `ThanhCong` và số tiền khớp tổng snapshot. Giao dịch B1 cần mã, payer và metadata hợp lệ theo phương thức. Các tổ hợp sai, tổng vượt giới hạn/không nguyên, đăng ký thiếu dòng/sai cặp/trạng thái hoặc ưu đãi chưa snapshot trả 409 `paymentDataConflict`. Danh sách có dòng mâu thuẫn trong trang cũng trả 409.

Đã có giao dịch hợp lệ trả 409 `paymentAlreadyRecorded`; tổng 0 trả 409 `paymentNotRequired`. Hai Thu ngân cùng ghi được tuần tự hóa theo khóa đăng ký; unique `IX_ThanhToan_MaDangKy` bảo vệ cuối cùng. Chỉ SqlException 2601/2627 xác định đúng index này được ánh xạ lỗi trùng; lỗi DbUpdateException khác trả 503 `dependencyUnavailable`.

Lỗi có `{ type, title, status, code, traceId, errors? }`: 400 `validation` cho query/ID/body sai; 401 `invalidSession`; 403 `permissionDenied`/`emailVerificationRequired`; 404 `resourceNotFound` theo quyền trên; 409 các mã tiền vừa nêu; 503 `dependencyUnavailable` cho phụ thuộc SQL/ghi/audit. Không trả SQL, credential/token hoặc thông tin hồ sơ qua lỗi.

A08 đọc biên nhận bằng GET sau khi nhận 201 (thu tại quầy), 200 (xác nhận) hoặc gặp 409 nghiệp vụ. Nếu mất phản hồi, timeout hoặc lỗi 5xx, A08 khóa gửi lại. Nếu GET chưa thấy kết quả thành công, người dùng phải xác nhận đã đối chiếu trước khi mở lại thao tác.

### Migration và ranh giới

`20261029000847_G3SinglePayment` chỉ đổi index `ThanhToan.MaDangKy` thành unique; giữ FK, CHECK SoTien>0 và toàn bộ dữ liệu. Preflight trước DDL từ chối nhiều thanh toán cùng đăng ký hoặc thanh toán cũ sai contract/số tiền/đăng ký snapshot. Không tự xóa/gộp/đổi trạng thái/backfill; lỗi yêu cầu review dữ liệu trước triển khai. Down chỉ trả index thường. Kiểm Up/Down/Up và lặp migration trên DB test riêng; không tự áp DB demo.

`20261104112618_RF04B1CashierPayment` giữ khóa duy nhất của đăng ký, cho phép nhân viên null đối với giao dịch chờ, thêm metadata và kênh dự kiến nullable. Mã giao dịch mới có unique index lọc khác null. Down từ chối khi dữ liệu nhân viên null, kênh dự kiến hoặc lịch sử B1 sẽ bị mất. B1 chỉ xác nhận giao dịch chờ đã có trong SQL. API tạo giao dịch trực tuyến thuộc B2.

G5 với tổng dương phải kiểm giao dịch G3 hợp lệ, toàn bộ số tiền và đúng đăng ký; `DaDangKy` hay remainingAmount=0 không đủ làm bằng chứng. Điều kiện tiền khi tổng 0 chờ quyết định ở G5, không tự coi đủ điều kiện tiêm. G6 sẽ snapshot khoản giảm lúc đăng ký; G3 không đọc MucUuDai hiện hành, áp ưu đãi hoặc hồi tố giá/khoản giảm.


## RF-04B2 — Checkout Web

Ba route sau yêu cầu Customer Active, phiên hợp lệ và email đã xác minh. Route theo đăng ký kiểm cả hai liên kết hồ sơ Active như GET Payment. Đăng ký ngoài phạm vi hoặc không tồn tại đều trả 403.

| Route | Request | Kết quả |
|---|---|---|
| `GET /api/v1/payment/settings` | Không có body | 200 `{ receiverBankName, receiverAccountName, receiverAccountNumber }` với tài khoản thử nghiệm VAC. Chỉ đọc. |
| `POST /api/v1/registrations/{id}/payment/online` | `{ payer, method, termsAccepted, internationalCard?, domestic?, transfer? }` | 201, Location trỏ tới GET Payment. Tổng VND, loại `ToanBo`, trạng thái, mã giao dịch và thời điểm do server quyết định. |
| `PUT /api/v1/registrations/{id}/payment/channel` | `{ channel: "TaiTrungTam" }` | 200 chi tiết. Nếu chưa có thanh toán, lưu kênh dự kiến, ghi audit khi kênh thay đổi và giữ trạng thái chưa thanh toán. |

- `payer` dùng tên, điện thoại, tỉnh/thành, phường/xã, địa chỉ bắt buộc và email tùy chọn. Web điền từ `registrationContact`. Chỉnh sửa chỉ lưu vào snapshot giao dịch. `termsAccepted` phải là `true`, server lưu thời điểm đồng ý UTC.
- `TheQuocTe` nhận `internationalCard: { cardBrand, cardholderName, cardNumber, expiryMonth, expiryYear, cvv }`. Kiểm prefix và độ dài VISA/MASTER/JCB, Luhn, tháng hết hạn theo UTC+7 và CVV 3 chữ số ASCII. Lưu metadata đã che và trạng thái `ThanhCong`, nhân viên null.
- `TheNoiDia` nhận `domestic: { bankName, accountHolderName, cardOrAccountNumber, otp }`. Ngân hàng 1–100 ký tự, chủ tài khoản 1–150, định danh 8–20 chữ số ASCII, OTP thử nghiệm 6 chữ số ASCII. Lưu metadata đã che và trạng thái `ThanhCong`, nhân viên null.
- `ChuyenKhoanOnline` nhận `transfer` cùng cấu trúc chuyển khoản tại quầy. Mở hướng dẫn chỉ GET settings và tạo QR tại trình duyệt. Khi khách xác nhận đã chuyển, lưu metadata và mã tham chiếu, trạng thái `ChoXacNhan`, nhân viên null. A08 dùng route confirm B1 để xác nhận trên cùng ID và mã giao dịch.
- Mỗi request online có đúng một payload tương ứng phương thức. Trường ngoài contract bị từ chối. Số thẻ, CVV, OTP và định danh nguồn đầy đủ chỉ dùng trong request; biên nhận chỉ có metadata.
- Dùng chung khóa đăng ký, phép tính tổng, unique index và audit B1. Online đặt `TrucTuyen` cùng transaction với thanh toán. S5 chỉ chấp nhận thanh toán hợp lệ đã thành công và S7 chỉ tính doanh thu từ trạng thái `ThanhCong`.
- Web đối chiếu bằng GET sau 201 hoặc 409 trùng/không cần thu. 409 dữ liệu mâu thuẫn khóa gửi và hướng dẫn liên hệ quầy. Nếu mất kết nối, timeout hoặc lỗi 5xx, Web khóa gửi lại và GET đối chiếu. Nếu GET chưa thấy giao dịch, khách phải xác nhận đã kiểm tra để thử lại. Phiên thay đổi sẽ bỏ kết quả cũ.

Migration `20261104134233_RF04B2WebCheckout` thay một CHECK tổ hợp phương thức, nhân viên, trạng thái và thời điểm đồng ý. Các thanh toán tại quầy và legacy theo B1 vẫn đọc được. Bản ghi online thử nghiệm từ fixture cũ thiếu thời điểm đồng ý sẽ bị SQL từ chối khi nâng cấp. Down dừng với lỗi 51006 nếu đã có lịch sử thanh toán online.

## S5.1 — Lịch hẹn từ dịch vụ đã chốt (Windows)

Tạo và xem buổi hẹn từ đăng ký đủ điều kiện xếp lịch. Điều kiện ghi tiêm được kiểm ở luồng chuyên môn. S5.2 mở Customer GET theo ownership bên dưới.

- POST yêu cầu phiên/tài khoản/nhân viên Active, chức danh `LeTan` và mã quyền hiện hữu `QuanLyLichHen`. GET cho Lễ tân cùng quyền hoặc Customer theo S5.2. Không cấp quyền qua seed. Admin và chức danh khác bị 403 dù có mã quyền. Customer không được POST/PUT/DELETE lịch. Chế độ xem chi tiết trên WPF là read-only, tạo là tác vụ riêng.
- `POST /api/v1/appointments`: `{ registrationId, itemKind: "vaccine"|"package", registrationItemId, scheduledAt }`. ID nguyên dương, dùng **ID dòng chi tiết** cho registrationItemId. Từ chối trường JSON lạ, kể cả `location`. API lấy người tiêm từ đăng ký và kiểm cặp hồ sơ, trạng thái `DaDangKy` và dòng dịch vụ.
- `scheduledAt` bắt buộc ISO `YYYY-MM-DDTHH:mm:ss[.fffffff]Z` hoặc có offset `±HH:mm`. API kiểm thời điểm tương lai và chuẩn hóa UTC tới 7 chữ số thập phân. Server gán `DaXepLich` và lưu `DiemTiem` từ `Appointments:FacilityName` và `Appointments:FacilityAddress` dưới dạng `tên · địa chỉ` (tối đa 200 ký tự). Lịch đã lưu giữ địa điểm cũ.
- Sau khi khóa đăng ký theo cùng thứ tự với thao tác thanh toán, POST đọc projection RF‑04 trong transaction hiện tại. Dữ liệu bất nhất trả 409 `paymentDataConflict`. `ChoXacNhan` trả 409 `appointmentPaymentRequired`. Các trường hợp đủ điều kiện gồm tổng bằng 0, `ThanhCong` hoặc kênh `TaiTrungTam`. Các trường hợp còn lại trả 409 `appointmentPaymentRequired`.
- `GET /api/v1/appointments/sources?page=&pageSize=` trả `{ items }` gồm các đăng ký `DaDangKy`, theo ID giảm dần. Mặc định trang 1 và 25 mục, tối đa 100 mục/trang. Mỗi mục gồm `registrationId, status, customer, recipient, preferredDate, paymentState, expectedChannel, canSchedule, readinessCode, facility`. Readiness dùng chung quy tắc với POST, với các mã `zero`, `paid`, `center`, `pending`, `unpaid`. Trang có dữ liệu thanh toán bất nhất trả 409 `paymentDataConflict`.
- `GET /api/v1/appointments/sources/{registrationId}` trả `{ source, items }`. `source` cùng cấu trúc với mục trong danh sách, `items` gồm `id, kind, serviceId, name, quantity`. Cả hai GET nguồn chỉ cho Lễ tân Active có `QuanLyLichHen`. `preferredDate` là ngày khách mong muốn, có thể rỗng với đăng ký cũ. Lễ tân có thể chọn thời điểm tương lai khác.
- 201 + Location `/api/v1/appointments/{id}`, trả `AppointmentDto = { id, registrationId, customer: { id, fullName }, recipient: { id, fullName }, itemKind, registrationItemId, serviceId, serviceName, scheduledAt, location, status }`. GET trả cùng DTO; thời gian UTC `+00:00`, tên hiện hành. Không trả giá/thông tin khám.
- `GET /api/v1/appointments?registrationId=&recipientId=&page=&pageSize=` trả `{ items: AppointmentDto[] }`, ID giảm dần, mặc định 1/25, tối đa 100; query chỉ thu hẹp kết quả, không có tổng count. Đối chiếu POST dùng thêm `itemKind` + `registrationItemId` (đi cùng nhau) và `scheduledAt` bằng thời điểm UTC chính xác. Bộ lọc thời gian cho phép lịch quá khứ để đối chiếu.
- `GET /api/v1/appointments/{id}`: 200; Lễ tân đủ quyền gặp ID không tồn tại nhận 404 `resourceNotFound`. Customer gặp ID không tồn tại hoặc ngoài phạm vi đều nhận 403 `permissionDenied`. Lịch legacy thiếu nguồn giữ nguyên trong SQL, không xuất hiện trong API S5.1 và không được tự đoán nguồn.
- Ghi lịch và audit `appointments.created` / ResourceType `Appointment`, ResourceId ID lịch, actor/session/thời điểm thật cùng transaction Serializable. Khóa đăng ký tuần tự hóa yêu cầu; hai unique filtered indexes chặn cùng dòng tại cùng thời điểm UTC, cho phép đặt thời điểm khác. Không tạo payment/mũi/nhắc lịch hoặc đổi trạng thái đăng ký.
- Problem Details có code/traceId. Các mã gồm 400 `validation`, 401 `invalidSession`, 403 `permissionDenied`, 404 `resourceNotFound`, 409 `appointmentAlreadyExists`, `appointmentSourceConflict`, `appointmentPaymentRequired` hoặc `paymentDataConflict`, và 503 `dependencyUnavailable`. Mọi response, kể cả lỗi binding, có `Cache-Control: no-store` và không lộ SQL/PII.
- POST không retry tự động. Khi mất response/503 hoặc đọc 201 lỗi, giữ nguyên request và khóa tạo mới; GET với đúng nguồn/thời điểm trước khi thử lại. Nếu GET chưa thấy thì retry thủ công **cùng request**; unique DB vẫn bảo vệ kết quả POST cũ đến muộn. Đã nhận 201 không POST lại, chỉ tải lại bằng GET.
- Migration `S5AppointmentSource` thêm ba cột nullable `MaDangKy`, `MaChiTietVacXin`, `MaChiTietGoi`; CHECK cho phép toàn NULL (legacy) hoặc đúng một dòng chi tiết; FK ghép bảo đảm cùng đăng ký và người tiêm. Không backfill/xóa dữ liệu. Down từ chối khi còn lịch đã liên kết. Chỉ áp dụng trên DB test riêng trong nghiệm thu; DB demo cần giao triển khai riêng.

### S5.2 — Website khách hàng chỉ đọc lịch hẹn

- Identity kiểm tài khoản Customer Active, email đã xác minh, phiên chưa hết hạn/thu hồi và SecurityVersion hiện hành. Ownership dùng lại `RegistrationEndpoints.Scope`: AccountProfileLink Active trực tiếp tới cả Customer và Recipient của đăng ký nguồn, Recipient thuộc đúng Customer; người tiêm trên lịch phải trùng đăng ký. Không dựa vào tài khoản từng tạo đăng ký hoặc CCCD.
- SQL lọc phạm vi trước phân trang và trước projection DTO; lịch thiếu MaDangKy không xuất hiện. Mọi bộ lọc hiện có chỉ thu hẹp kết quả; accountId/ownerId từ client không cấp quyền. Danh sách không có lịch trả `{ items: [] }`. Giữ `Cache-Control: no-store` và Problem Details, không đổi DTO hoặc schema.
- Website dùng GET danh sách/chi tiết qua Identity client, token chỉ trong bộ nhớ, credentials omit/cache no-store. Danh sách → chi tiết, phân trang/tải lại, giờ địa phương từ UTC API; không có tra cứu bằng CCCD, lịch sử tiêm giả hoặc thao tác ghi lịch. Đổi phiên xóa dữ liệu cũ và loại bỏ phản hồi trễ; tải lại trang cần đăng nhập lại.

## S5.3 — Kết luận và chỉ định theo lịch hẹn (Windows A02)

Chỉ tài khoản Employee có phiên hợp lệ, account/nhân viên Active và chức danh BacSi được GET/POST. Identity kiểm phiên/security version hiện hành; không thêm mã quyền. Customer, Admin và chức danh khác bị từ chối. Không phải hàng đợi đã đến khám, không xác nhận đã tiêm.

- `GET /api/v1/clinical/appointments?search=&page=&pageSize=`: `{ items: AppointmentDto[] }` như S5.1, nguồn đăng ký DaDangKy đúng khách hàng/người tiêm/dòng lẻ hoặc gói có thành phần. Search tối đa 100 ký tự sau trim, tìm tên người tiêm hoặc mã lịch số/LH-0001. Page mặc định 1/25, pageSize tối đa 100, ID giảm dần. Chỉ thu hẹp SQL trước phân trang, không hiển thị legacy thiếu nguồn.
- `GET /api/v1/clinical/appointments/{id}`: `{ appointment: AppointmentDto, allowedVaccines: [{ id, name }] }`. Lẻ chỉ đúng vaccine của dòng; gói chỉ thành phần GoiVacXin_VacXin của gói trên dòng đăng ký. Không tự chọn thành phần hoặc suy số mũi.
- `POST /api/v1/clinical/appointments/{id}/assessment`: `{ conclusion, reason: string|null, vaccineIds: int[] }`. Conclusion đúng một trong `Đủ điều kiện`, `Tạm hoãn`, `Chống chỉ định`, do bác sĩ chọn. Đủ điều kiện yêu cầu 1–100 vaccine ID nguyên dương, không trùng, thuộc allowedVaccines; hai kết luận còn lại yêu cầu lý do 1–500 ký tự sau trim và vaccineIds rỗng. Reason nếu cung cấp tối đa 500. Từ chối trường lạ; không nhận actor, người tiêm, thời điểm hoặc dữ liệu sinh tồn từ client.
- Thành công 201 + Location tới `/api/v1/clinical/appointments/{id}/assessment`, trả ClinicalAssessmentDto `{ id, appointmentId, registrationId, recipientId, doctor: {id, fullName}, recordedAt, conclusion, reason, vitalSigns, medicalHistory, indications: [{ id, vaccineId, vaccineName, result, recordedAt }] }`. Server đặt bác sĩ/thời điểm UTC, kết quả chỉ định được chọn là `DuocTiem`. Hai kết luận khác không tạo dòng chỉ định. Sinh tồn/tiền sử không thu thập ở S5.3 nên NULL, không suy là bình thường.
- `GET /api/v1/clinical/appointments/{id}/assessment`: `{ assessment: ClinicalAssessmentDto|null }`; null nghĩa lịch hợp lệ chưa có kết luận. Không mở đọc phiếu legacy không gắn lịch hoặc phiếu của Customer. Không sửa/xóa kết luận.
- Ghi trong transaction Serializable: khóa account/session qua Identity, khóa lịch rồi kiểm nguồn hiện hành, chặn phiếu đã có, ghi phiếu + chỉ định + audit `clinical.assessed` (ResourceType `ClinicalAssessment`, ID phiếu) cùng commit. Unique MaLichHen và unique (MaPhieuKham,MaVacXin) bảo vệ ghi trùng. Không đổi lịch/đăng ký/thanh toán, không ghi MuiTiem/LoVacXin/BienDongKho.
- Problem Details/no-store: 400 validation, 401 invalidSession, 403 permissionDenied, 404 resourceNotFound cho lịch thiếu/legacy/nguồn không hợp lệ, 409 assessmentAlreadyRecorded cho phiếu đã tồn tại, 503 dependencyUnavailable. Nội dung kết luận/lý do không đưa vào lỗi/log. POST không retry tự động. Mất phản hồi/503 phải giữ đúng request, khóa gửi mới và GET đối chiếu; nếu GET đã có thì chỉ đọc, nếu chưa có mới cho retry thủ công cùng request. Đã nhận 201 thì chỉ GET, không POST lại.
- Migration mới S5ClinicalAssessment: MaLichHen/ThoiDiemKham nullable để giữ nguyên legacy, NULL sinh tồn/tiền sử, unique indexes và SQL composite FK (MaLichHen,MaDangKy,MaNguoiDuocTiem) tới lịch. FK SQL viết tường minh để giữ MaDangKy của lịch legacy nullable. Không backfill. Nếu chỉ định legacy trùng (phiếu,vaccine), Up từ chối để review dữ liệu, không xóa/gộp. Down từ chối nếu còn kết luận mới hoặc NULL sinh tồn/tiền sử; không điền giả. Chỉ migrate DB test mới trong gói này.

## S5.5 — Điều dưỡng ghi nhận một mũi đã thực hiện (Windows A06)

Chỉ Employee có phiên Identity hiện hành, account/nhân viên Active và đúng chức danh `DieuDuong` được GET/POST. Danh sách 12 quyền hiện hữu không có quyền ghi tiêm riêng; không dùng mã quyền không liên quan, không thêm/gán quyền mới. Customer và mọi chức danh khác (kể cả Admin/BacSi) bị từ chối. Đây là ghi nhận đã thực hiện, không cấp phép tiêm trên lâm sàng; bác sĩ chịu trách nhiệm kết luận/chỉ định S5.3.

- `GET /api/v1/vaccinations/eligible?search=&page=&pageSize=` trả `{ items: VaccinationIndicationDto[] }`: chỉ định `DuocTiem`, phiếu `Đủ điều kiện`, đúng lịch/nguồn lẻ hoặc thành phần gói và chưa có mũi. Search sau trim tối đa 100 ký tự, tìm tên người tiêm hoặc mã lịch số/LH-0001; page mặc định 1/25, pageSize tối đa 100, ID chỉ định giảm dần. Đây là danh sách để tiếp tục đối chiếu; không khẳng định đã đủ tiền hoặc còn lô. Không trả lý do khám/tiền sử/sinh tồn.
- `VaccinationIndicationDto = { id, appointmentId, registrationId, customer:{id,fullName}, recipient:{id,fullName}, vaccine:{id,name}, scheduledAt, location, doctor:{id,fullName}, assessedAt }`. Giờ API là UTC; không có số thứ tự mũi suy đoán.
- `GET /api/v1/vaccinations/indications/{id}` trả `{ indication: VaccinationIndicationDto, facilityDate, lots:[{number,manufacturedOn,expiresOn,stock}] }`. Kiểm nguồn/chỉ định/thanh toán hiện hành, chỉ trả lô cùng vắc xin, Active, ngày sản xuất không ở tương lai/không sau hạn dùng, hạn dùng >= facilityDate, ban đầu dương, 0 < tồn <= ban đầu. Không tự chọn FEFO. Không có lô trả mảng rỗng. Ngày cơ sở do server tính cố định UTC+07:00 (Việt Nam); hạn dùng đúng ngày cơ sở vẫn hợp lệ. UI không tự xác nhận điều kiện thay API.
- `POST /api/v1/vaccinations`: đúng ba trường `{ indicationId, lotNumber, injectionSite }`, ID nguyên dương, mã lô trim 1–80 ký tự, vị trí trim 1–100. Từ chối trường JSON lạ, không nhận vaccine/recipient/appointment/registration/doctor/employee/time/status hoặc cờ đủ điều kiện/thanh toán/tồn. Nhân viên, thời điểm UTC và trạng thái `DaTiem` do server xác định.
- 201 + Location `/api/v1/vaccinations/indications/{id}/result`, trả `VaccinationDto = { id, indicationId, appointmentId, registrationId, recipient:{id,fullName}, vaccine:{id,name}, lotNumber, injectionSite, nurse:{id,fullName}, administeredAt, status }`. `GET` Location trả `{ vaccination: VaccinationDto|null }`; null là chưa ghi, không tự retry POST. Kết quả đã ghi vẫn đọc được khi lô hết hạn/hết tồn hoặc điều kiện ghi hiện hành đổi; không mở lịch sử Customer.
- Transaction Serializable: khóa Identity account → session → chỉ định → đăng ký → lịch → lô (phiếu/nguồn đọc trong cùng transaction Serializable) theo thứ tự cố định. Đối chiếu đúng khách hàng/người tiêm/đăng ký DaDangKy/dòng lẻ hoặc thành phần gói, phiếu thuộc lịch và cùng nguồn, kết luận Đủ điều kiện/chỉ định DuocTiem. Không chấp nhận legacy thiếu nguồn hoặc tạo bằng chứng thay thế.
- Dùng lại phép đọc/kiểm tiền G3: giá và số lượng snapshot từ toàn bộ chi tiết đăng ký, tổng dương trong giới hạn VND, không MaKhuyenMai chưa snapshot; đúng một ThanhToan cùng đăng ký, ToanBo/MoPhong/ThanhCong, số tiền đúng tổng. Thiếu thanh toán bị từ chối; tổng 0 fail closed `vaccinationZeroTotalNotSupported`, không tạo ThanhToan 0 hoặc miễn phí giả.
- Một lần ghi thành công trừ đúng 1 tồn bằng UPDATE có điều kiện, insert MuiTiem có MaChiDinh, insert BienDongKho `XuatDoTiem`/số lượng 1 có MaMuiTiem/lô/điều dưỡng/thời điểm khớp mũi và audit `vaccinations.recorded` / ResourceType `Vaccination`, ResourceId ID mũi, actor/session; commit cùng nhau. Bất kỳ lỗi nào rollback cả bốn. Unique MaChiDinh và MaMuiTiem ngăn ghi trùng; tranh liều cuối chỉ một thành công.
- Problem Details/no-store cho mọi response kể cả lỗi binding: 400 `validation`, 401 `invalidSession`, 403 `permissionDenied`, 404 `resourceNotFound`, 409 `vaccinationSourceConflict`, `vaccinationNotIndicated`, `vaccinationAlreadyRecorded`, `vaccinationPaymentRequired`, `vaccinationZeroTotalNotSupported`, `paymentDataConflict`, `vaccinationLotUnavailable`; SQL/timeout/ghi/audit lỗi trả 503 `dependencyUnavailable`, không trả SQL/credential/nội dung khám.
- POST không retry tự động. Mất response/503 hoặc 201 không đọc được: giữ nguyên request, khóa thao tác mới, GET theo indicationId trước retry. GET có kết quả thì chỉ đọc; GET chưa thấy mới cho retry thủ công cùng request (unique DB bảo vệ POST cũ đến muộn). Đã nhận 201 chỉ GET, không POST lại. Đổi phiên xóa dữ liệu/draft/pending và bỏ response cũ.
- Migration mới `S5VaccinationRecord`: MuiTiem.MaChiDinh nullable/FK/unique filtered; BienDongKho.MaMuiTiem nullable/unique filtered và FK ghép (MaMuiTiem,SoLo,MaNhanVien) tới mũi. CHECK chỉ biến động gắn mũi phải XuatDoTiem/số lượng 1; legacy NULL giữ nguyên. Không backfill. Down từ chối khi còn liên kết mới, không bỏ dấu vết. Chỉ migrate DB test disposable trong gói này.

Khu vực Ghi nhận mũi tiêm A06 chỉ ghi/xem lại mũi, chọn lô từ SQL thật, xác nhận đã thực hiện trước lưu; không tự chạy timer, tạo TheoDoiSauTiem hoặc kết luận cho về. Theo dõi do Điều dưỡng ghi riêng ở S5.6 bên dưới. Nhắc lịch, sửa/xóa mũi, kho/nhập hàng/FEFO và Customer xem lịch sử thuộc gói khác.

## S5.6 — Điều dưỡng ghi kết quả theo dõi sau tiêm (Windows A06)

Chỉ phiên Identity hiện hành của Employee, account/nhân viên Active, đúng chức danh `DieuDuong`; không thêm mã quyền trong mô hình 12 quyền hiện hữu. Customer/Admin/chức danh khác không được GET/POST. Bản ghi là nội dung thực tế nhân viên nhập, không phải xác nhận an toàn/cho về hoặc theo dõi sinh hiệu liên tục.

- `GET /api/v1/vaccinations/follow-up-pending?search=&page=&pageSize=` trả `{ items: VaccinationDto[] }` (DTO mũi S5.5). Chỉ mũi DaTiem có MaChiDinh và nguồn S5.5 hợp lệ, đúng người/vắc xin/lô, chưa có TheoDoiSauTiem. Search trim tối đa 100, tìm tên người tiêm hoặc mã mũi số/MT-0001; page mặc định 1/25, pageSize tối đa 100; ID mũi giảm dần, lọc/phân trang tại SQL. Không phải danh sách người hiện ở cơ sở; chưa có check-in/out.
- `GET /api/v1/vaccinations/{id}/follow-ups` trả `{ vaccination: VaccinationDto, followUp: FollowUpDto|null }` cho mũi hợp lệ. `FollowUpDto = { id, vaccinationId, condition, severity, treatment, recordedBy:{id,fullName}|null, recordedAt }`; recordedBy null chỉ dữ liệu legacy chưa có liên kết người ghi. Không kiểm lại thanh toán/hạn dùng/tồn lô cho theo dõi; đọc/ghi được khi lô đã hết hạn/hết tồn. Không trả tiền sử/lý do khám.
- RF-05C: GET detail bổ sung `observation`, GET pending giữ nguyên `items` và bổ sung `observations` (một phần tử cho mỗi mũi, ghép bằng `vaccinationId`). Observation = `{ vaccinationId, observationUntil, canComplete, blockReason }`; `observationUntil = administeredAt + 30 phút`, UTC, derive server-side, không persist countdown. Trước mốc: `canComplete=false`, `blockReason=observationPeriodNotCompleted`; tại/sau mốc: true/null. Thời điểm nguồn ở tương lai/không hợp lệ: false/`followUpSourceConflict`. Đây chỉ là điều kiện hoàn tất ghi nhận, không xác nhận an toàn/cho về.
- `POST /api/v1/vaccinations/{id}/follow-ups`: đúng ba trường `{ condition, severity, treatment }`, bắt buộc không rỗng sau trim, tối đa 100/100/500 ký tự. Từ chối JSON lạ; không nhận người tiêm/vắc xin/nhân viên/thời điểm/cờ an toàn hoặc cho về. 201 + Location trỏ GET cùng route, trả FollowUpDto. Nhân viên từ phiên, recordedAt server UTC tại/sau mốc 30 phút tính từ lúc tiêm; không có giá trị nội dung mặc định.
- Transaction Serializable: khóa Identity account → session → MuiTiem (UPDLOCK,HOLDLOCK), kiểm nguồn/mũi/chưa có theo dõi, kiểm mốc 30 phút bằng giờ server sau khi khóa mũi (trước mốc trả 409 `observationPeriodNotCompleted`, không row/audit), insert TheoDoiSauTiem + audit `aftercare.recorded` (ResourceType `FollowUp`, ResourceId ID bản ghi, actor/session), cùng commit. Unique MaMuiTiem chặn hai request cùng mũi; lỗi ghi hoặc audit rollback cả hai. Không sửa mũi/lô/tồn/biến động/đăng ký/thanh toán/phiếu/chỉ định, không tạo timer/tác vụ tự động.
- Mọi response no-store và Problem Details: 400 validation, 401 invalidSession, 403 permissionDenied, 404 resourceNotFound (mũi thiếu), 409 followUpSourceConflict (legacy/trạng thái/nguồn sai/thời điểm tiêm ở tương lai), followUpAlreadyRecorded, 503 dependencyUnavailable. Không đưa nội dung theo dõi, SQL hoặc credential vào lỗi/log.
- Không retry POST tự động. Mất phản hồi/503 giữ nguyên request và GET đối chiếu trước retry; GET có bản ghi thì chỉ đọc. GET chưa có mới retry thủ công cùng request. Nhận 201 mà không đọc được body chỉ GET, không POST lại. Đổi phiên xóa dữ liệu/bản nháp/pending và bỏ response cũ.
- Migration mới S5PostVaccinationFollowUp chỉ bổ sung TheoDoiSauTiem.MaNhanVien nullable/FK Restrict; giữ unique MaMuiTiem và dữ liệu legacy, không backfill. Bản ghi API mới bắt buộc actor; Down từ chối nếu còn MaNhanVien không null. Chỉ áp trên DB test disposable trong gói này.

Windows thêm khu vực Theo dõi sau tiêm cạnh Ghi nhận mũi tiêm, chọn mũi → đối chiếu → nhập đủ ba nội dung → xem/xác nhận → lưu → đọc lại bằng MT-id. Không sửa/xóa, tự phân loại phản ứng, chỉ định thuốc, đổi trạng thái mũi hoặc quyết định cho về. Nhắc lịch/Customer xem lịch sử/báo cáo thuộc gói khác.

## X2.1 — Nhập một lô mới (Windows A05)

Nguồn: đề cương `CNTT_KLCN090_Tuan11.docx`, mục 1.3.1 và Use Case quy trình (1): đối chiếu vắc xin/lô/NSX/HSD/số lượng với chứng từ, kiểm tình trạng và bảo quản, ghi vị trí/người/thời điểm tiếp nhận. **CẦN BỔ SUNG** biểu mẫu và tiêu chuẩn nội bộ VNVC; form này là mô hình khóa luận, không khẳng định quy trình/biểu mẫu chính thức. Không quản lý danh mục nhà cung cấp hoặc nhiều dòng/phiếu, không nhập bổ sung lô cũ.

- Quyền: Thủ kho Active với **cả XemKho và NhapKho**, account/session hiện hành. Migration chỉ thêm mã `NhapKho`, không cấp grant; dùng `set-extra-permissions` của IdentityOperator hiện có để quản trị cấp rõ ràng (giữ các quyền cũ cần thiết). Không thêm NhapKho vào quyền Admin/recovery mặc định. Không tự cấp quyền SQL/OS hoặc tài khoản demo.
- `GET /api/v1/inventory/receiving-vaccines?search=&page=`: vắc xin Active, tìm tối đa 100 ký tự, page dương mặc định 1, 25 dòng/trang + hasMore, thứ tự tên/ID. `{items:[{id,name}],hasMore}`. Không dùng danh mục Sample.
- `POST /api/v1/inventory/receipts`: `{lotNumber,vaccineId,manufacturedOn,expiresOn,quantity,supplierName,documentNumber,storageLocation,inspectionConfirmed}`. Chuỗi trim: số lô 80, nhà cung cấp 200, chứng từ 100, vị trí 200 ký tự, bắt buộc. Ngày ISO: NSX không mặc định/không tương lai và NSX ≤ HSD; HSD ≥ ngày cơ sở UTC+7. Quantity nguyên 1..Int32.MaxValue; vaccine Active. InspectionConfirmed phải true, chỉ do nhân viên xác nhận đã đối chiếu chứng từ/hàng thực tế, tình trạng và điều kiện bảo quản; phần mềm không tự chứng nhận cold chain. Không có ngày nhập/người nhập/trạng thái do client chọn.
- Serializable transaction khóa nguồn vaccine và khóa UPDLOCK/HOLDLOCK số lô toàn hệ thống: tạo LoVacXin Active (ban đầu=tồn=quantity), một BienDongKho NhapKho (MaMuiTiem=NULL, người từ phiên, thời điểm UTC server), InventoryReceipt snapshot nhà cung cấp/chứng từ/vị trí gắn lô và biến động, audit `inventory.received` cùng transaction. PK số lô hiện hữu chống nhập trùng theo collation SQL, không ghi đè/bù tồn. Migration chỉ tạo bảng mới với FK Restrict; không gán chứng từ cho lô legacy/seed; Down từ chối khi đã có receipt.
- 201 trả `{received:<request đã trim>,vaccineName,movementId,employeeId,employeeName,recordedAt}` và Location GET URL-encoded. `GET /api/v1/inventory/receipts?lotNumber=` đọc receipt chính xác, 404 nếu chưa có (kể cả lô legacy); không sửa snapshot dù tồn đã xuất. GET X1 đọc lô/biến động mới sau commit như trước. No-store, 400 validation/inventoryVaccineUnavailable, 401 invalidSession, 403 permissionDenied, 409 inventoryLotExists, 503 dependencyUnavailable. Không trả nội dung exception SQL.
- Windows xem lại/xác nhận trước POST, không tự refresh/replay POST. Timeout/network/409 hoặc 201 body lỗi khóa draft; chỉ GET đối chiếu theo số lô. GET khớp snapshot yêu cầu mới xác nhận đã ghi; khác snapshot báo xung đột, không nhận thành công của yêu cầu này. GET 404 cho retry thủ công **đúng payload cũ** nếu chưa từng nhận 201; API vẫn chống tranh nhập. Đã nhận 201 chỉ GET, không POST lại. Đổi phiên/401/403 xóa draft/kết quả, bỏ phản hồi trễ. Thành công đọc lại GET và X1; tạo tiếp phải reset lựa chọn/draft/xác nhận. Không sửa S5/S6/S7, không X2.2.

## X2.2.1 — Tra cứu lịch sử tiếp nhận (Windows A05)

Bổ sung **chỉ đọc** cho X2.1; không phải quản lý nhà cung cấp đầy đủ. Thủ kho Active có cả `XemKho` và `NhapKho` như GET receipt hiện hành. `GET /api/v1/inventory/receipts/history?search=&page=` trả `{items,hasMore}`, 25 dòng/trang; tìm theo **số lô, tên vắc xin, nhà cung cấp, số chứng từ**, search trim tối đa 100 ký tự; page dương mặc định 1, từ chối offset vượt Int32. Sắp `BienDongKho.ThoiDiem` và `MaBienDong` giảm dần, có thứ tự ổn định; `recordedAt` UTC, client đổi múi giờ khi hiển thị. Bao gồm chỉ các lô đã ghi qua X2.1 (`InventoryReceipt`), **không** giả định nguồn/phiếu của lô legacy hoặc seed. `quantity` là số lượng **thực nhập ban đầu từ biến động NhapKho**, không lấy tồn thay đổi theo ghi tiêm. 401/403/400, no-store và SQL parameterized query. Chọn một dòng → GET receipt đúng số lô qua endpoint X2.1; không có POST/PUT/DELETE, không migration. Windows A05 hiển thị loading/rỗng/lỗi/đổi phiên, phân trang, chi tiết đọc lại SQL. Chưa có nhập bổ sung lô cũ, báo cáo phiếu nhiều dòng hoặc xác minh cold-chain nội bộ VNVC.

Query chỉ nhận liên kết receipt → biến động cùng số lô, loại `NhapKho`, không gắn mũi, số lượng dương khớp `LoVacXin.SoLuongBanDau`; bỏ nguồn bất nhất thay vì gán số lượng/chứng từ của lô khác. GET lỗi SQL trả 503 `dependencyUnavailable`, không lộ exception. Snapshot nhà cung cấp/chứng từ/vị trí lấy từ InventoryReceipt; tên vắc xin/nhân viên là tên liên kết hiện hành (X2.1 chưa lưu snapshot tên). Danh sách/chi tiết phải đối chiếu cùng số lô, mã biến động, số lượng, nguồn và người/thời điểm; nếu đã thay đổi thì báo lỗi, tải lại danh sách. Đổi bộ lọc phải Tìm/Tải lại về trang 1 trước khi phân trang; mở lại cửa sổ luôn GET kiểm quyền/dữ liệu mới. Chi tiết có vùng cuộn riêng và hỗ trợ bàn phím, không có lệnh ghi.

## X1 — Kho vắc xin chỉ đọc (Windows A05)

Chỉ nhân viên `ThuKho` Active có grant `XemKho`, account/session Active theo IdentityAuthorization hiện hành. Admin, các chức danh khác và Customer không được đọc, kể cả khi có grant XemKho. Không tự cấp grant hoặc thêm quyền mới. Contract mới cần review chéo trước merge.

- `GET /api/v1/inventory/lots?search=&page=&pageSize=`: tìm số lô/tên vắc xin, trim search tối đa 100 ký tự; page mặc định 1, pageSize mặc định 25/tối đa 100, từ chối page không dương/offset vượt Int32. Sắp hạn dùng rồi số lô tăng dần; bao gồm lô hết hạn/hết tồn/không Active và vắc xin không Active để tra cứu kho. `{items:[{lotNumber,vaccineId,vaccineName,manufacturedOn,expiresOn,initialQuantity,stock,status}],hasMore,facilityDate}`; ngày ISO, facilityDate theo UTC+7. Không suy lô được phép tiêm từ trạng thái hiển thị.
- `GET /api/v1/inventory/movements?lotNumber=...&page=&pageSize=`: số lô bắt buộc, truyền query URL-encoded để bảo toàn ký tự đặc biệt; phân trang như trên. Lô không tồn tại trả 404, lô không có biến động trả danh sách rỗng. Sắp thời điểm rồi mã biến động giảm dần. `{items:[{id,lotNumber,type,quantity,recordedAt,employeeId,employeeName}],hasMore}`; giữ nguyên loại/số lượng SQL, recordedAt ISO UTC, Windows hiển thị giờ thiết bị. Không trả hồ sơ người tiêm hoặc mã mũi. Seed lô không tạo biến động nên không tự dựng dòng nhập kho cho seed.
- GET chỉ đọc `LoVacXin`/`BienDongKho` và liên kết tên; không ghi/audit/seed, không migration. Không có POST/PUT/DELETE; A05 bỏ nút nhập/điều chỉnh Sample. Tồn hiển thị tại lúc tải; dùng Tải lại để đọc lại SQL. S5 vẫn tự kiểm lô và điều kiện ghi tiêm bằng endpoint hiện hữu.
- Mọi phản hồi `Cache-Control: no-store`; 400 `validation`, 401 `invalidSession`, 403 `permissionDenied`, 404 `resourceNotFound`, 503 `dependencyUnavailable`. Windows không fallback Sample; loading/rỗng/lỗi tách biệt, thử lại GET sau lỗi mạng; 401/403 hoặc đổi phiên xóa dữ liệu/lựa chọn, bỏ phản hồi trễ và yêu cầu đăng nhập/mở lại. Không tạo chức năng ghi kho X2.

## S5.7A — Customer đọc lịch sử tiêm trên Website

`GET /api/v1/vaccinations/history?recipientId=&page=&pageSize=`: chỉ Customer với Identity session/account Active và email đã xác minh. Mọi chức danh nhân viên kể cả Admin bị từ chối. Dùng route tổng hợp thay cho dự kiến `recipients/{id}/history` để đọc nhiều người được tiêm trong một lần gọi; recipientId tùy chọn chỉ thu hẹp phạm vi.

- Ownership dùng nguyên `RegistrationEndpoints.Scope` trong IQueryable SQL: cả link Customer và Recipient Active thuộc account từ Bearer, Recipient thực sự thuộc Customer của đăng ký. Không nhận accountId/ownerId/CCCD để xác lập quyền. Bộ lọc ngoài quyền và không có dữ liệu cùng trả `{ items: [] }`.
- Chỉ MuiTiem `DaTiem`, có MaChiDinh → phiếu có ThoiDiemKham/lịch nguồn → đăng ký cùng người tiêm; đối chiếu dòng lẻ hoặc thành phần gói với vaccine chỉ định và vaccine của lô. Legacy thiếu nguồn và chỉ định chưa ghi mũi không xuất hiện. Mỗi mũi một dòng, không suy phác đồ/thứ tự/liều tiếp theo. Không kiểm lại kết luận, trạng thái đăng ký, thanh toán, hạn dùng/tồn/trạng thái lô hiện tại; không cần TheoDoiSauTiem để sự kiện đã ghi tiếp tục đọc được.
- page mặc định 1, pageSize 25, tối đa 100; ID/query nguyên dương, từ chối offset vượt Int32. SQL lọc trước phân trang và projection, sắp ThoiDiemTiem giảm dần rồi MaMuiTiem giảm dần.
- `{ items: [{ id, appointmentId, registrationId, recipient:{id,fullName}, vaccine:{id,name}, lotNumber, injectionSite, administeredAt, status }] }`; administeredAt ISO UTC, status `DaTiem`. Không trả bác sĩ/điều dưỡng, khám/tiền sử, theo dõi, liên hệ, tiền, audit hoặc Identity. Danh sách chứa đủ chi tiết cho Web, không thêm endpoint chi tiết.
- GET chỉ đọc, không audit/ghi dữ liệu. Mọi response `Cache-Control: no-store`; Problem Details 400 `validation`, 401 `invalidSession`, 403 `permissionDenied` (gồm email chưa xác minh), 503 `dependencyUnavailable`. Dùng IdentityAuthorization hiện hành, không thêm quyền/schema/migration.
- Web chỉ giữ dữ liệu/token/lựa chọn trong bộ nhớ, credentials omit/cache no-store, dùng refresh/sessionVersion hiện hữu. Đổi phiên/sessionBusy tháo workspace và bỏ response trễ; 401/403/refresh thất bại xóa kết quả. Tải lại trang cần đăng nhập lại. Thời gian hiển thị theo thiết bị, không sửa timestamp API. Đây là lịch sử mũi đã ghi trong VAC; không xác nhận hoàn thành theo dõi/phác đồ hoặc cấp giấy chứng nhận. Nhắc lịch thuộc S5.7B.

## RF-05A1 — Hành trình tiêm chủng của khách hàng

- GET `/api/v1/vaccination-journeys?page=&pageSize=` trả `{ items }`, sắp xếp ngày đăng ký rồi mã đăng ký giảm dần. Mặc định trang 1, 25 mục, tối đa 100 mục/trang.
- GET `/api/v1/vaccination-journeys/{registrationId}` trả một mục cùng cấu trúc. Cả hai yêu cầu Bearer Customer và hai liên kết hồ sơ Active theo phạm vi Registration. Chi tiết ngoài phạm vi hoặc không tồn tại trả 403. Response đặt `Cache-Control: no-store`.
- Mỗi mục có `registrationId, createdAt, preferredDate, payment, appointments, nextAppointment, shots, shotCount`. `payment` dùng `RegistrationPaymentDto` hiện hành gồm contact, hồ sơ, dòng dịch vụ, tiền VND, kênh dự kiến và giao dịch. Lỗi bất nhất tài chính trả 409 cho response. Số tiền dùng cùng quy ước VND chính xác như Payment.
- Bỏ lịch sai người hoặc sai dòng nguồn trong cùng đăng ký. `nextAppointment` là lịch `DaXepLich` tương lai gần nhất, thời gian bằng nhau phân định theo ID. `shots` chỉ gồm mũi thuộc chuỗi đã xác thực của S5.7A, mới nhất trước, phân định theo ID. Lịch/mũi sai nguồn không đưa vào projection.
- Chỉ đọc SQL. Trạng thái trình bày được suy ra từ facts, không có Journey state lưu trong SQL.

## RF-05B — Số mũi đăng ký và thời điểm khám/ghi tiêm

- Entitlement theo **registration + vaccine**: cộng quantity dòng lẻ và `SoLuongGoi × SoLuong` thành phần gói. Thành phần gói đã được contract Catalog khóa khi có đăng ký tham chiếu; không thêm snapshot/schema/migration và không đọc catalog regimen. Các dòng cùng vắc xin trong một đăng ký dùng chung quota. Không giữ chỗ bằng chỉ định chưa thực hiện.
- Đếm chỉ `MuiTiem.DaTiem` qua chỉ định/phiếu cùng đăng ký và đúng người được tiêm. Không trừ quota khi rollback. Clinical POST và Vaccination POST dùng khóa hàng đăng ký `UPDLOCK,HOLDLOCK` đến commit; ghi tiêm kiểm lại quota trước trừ tồn, cùng transaction mũi/biến động/audit. Chỉ định có thể cũ, không bảo đảm còn quota lúc ghi tiêm.
- POST clinical (mọi kết luận/chỉ định) và ghi tiêm trả 409 `appointmentNotStarted` nếu `ScheduledAt > UtcNow`; lịch lưu đủ giờ UTC, so sánh instant, không chỉ so ngày hoặc dùng giờ máy client. Trả 409 `doseEntitlementExhausted` khi đã đủ số mũi, `doseEntitlementUnavailable` khi không xác định được quota. Quantity/component ngoài contract 1–100 hoặc gói thiếu component không được suy đoán; thiếu liên kết nguồn vẫn theo lỗi 404/409 hiện hành. Lỗi bất nhất tiền legacy vẫn fail closed tại Payment.
- Clinical detail thêm `blockReason: null|string`; mỗi `allowedVaccines` thêm `canIndicate: boolean`, `remainingDoses: int|null`. Vaccination detail thêm `blockReason`, trả `lots: []` khi bị chặn do giờ/quota. GET vẫn đọc được thông tin để Windows giải thích lý do; API POST luôn kiểm lại. Mũi/kết luận đã lưu vẫn đọc qua endpoint result/assessment hiện hữu.
- Journey thêm `progress: [{vaccineId,vaccineName,registeredDoses:int|null,administeredDoses,remainingDoses:int|null,nextAppointment:AppointmentDto|null}]`. Remaining = max(registered − administered, 0); null biểu thị không đủ căn cứ. Next chỉ lấy lịch `DaXepLich` tương lai thuộc dòng dịch vụ chứa vắc xin, gần nhất theo giờ/ID, và còn quota xác định. Không suy ngày hoặc mũi nhắc từ phác đồ; chưa có lịch xác nhận thì trả null. Web hiển thị trạng thái trung tính với legacy/rỗng.

## S6 — Ưu đãi giảm tiền cố định

- `GET /api/v1/promotions/valid` công khai, trả ưu đãi đã bật và đang trong khoảng hiệu lực UTC (gồm hai đầu mốc). Windows A10 dùng `GET/POST /api/v1/promotions`, `PUT/DELETE /api/v1/promotions/{id}` với phiên nhân viên có quyền `QuanLyKhuyenMai`. POST/PUT nhận `name`, `description`, `startsAt`, `endsAt`, `discountAmount` (VND nguyên, 0–9.999.999.999.999.999) và `isEnabled`. DELETE trả 409 `promotionReferenced` khi đã có đăng ký tham chiếu.
- POST đăng ký nhận thêm `promotionId` nullable. Server kiểm trạng thái/thời gian trong cùng transaction với đăng ký; nếu ưu đãi không còn dùng được, trả 409 `promotionUnavailable`. Tên và khoản giảm được chốt khi tạo. Khoản giảm tối đa bằng tiền dịch vụ, phí khám/tư vấn giữ nguyên.
- Danh sách/chi tiết đăng ký trả `promotionName`, `discountAmount`, `totalDue`. Payment và Journey dùng số tiền từ snapshot, với `totalDue = subtotal + consultationFee - discountAmount`. Các điều kiện thanh toán pending, xác nhận thu và ghi mũi tiếp tục dùng cùng payment summary. Các đăng ký cũ chưa có snapshot và có tham chiếu ưu đãi tiếp tục bị chặn ở cổng kiểm tiền.

S6 migration kiểm trước khi thay schema. Nếu đăng ký cũ đã có `MaKhuyenMai`, migration dừng để đối chiếu lịch sử rõ ràng, không suy snapshot từ danh mục ưu đãi hiện tại. Mức ưu đãi cũ có phần lẻ VND cũng bị từ chối trước khi đổi precision.
