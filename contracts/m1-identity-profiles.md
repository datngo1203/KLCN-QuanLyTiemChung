# Contract review M1 — chưa thực thi

Base `/api/v1`, JSON camelCase, UTC ISO 8601, DateOnly `YYYY-MM-DD`, ID int như hiện tại. Đây là đặc tả Markdown chờ review, không phải DTO/OpenAPI đã triển khai. [Quyết định và gate](../docs/m1-identity-review.md), [schema](../database/m1-identity-schema-review.md).

## Identity requests/responses

POST JSON chỉ nhận trường liệt kê; reject trường lạ (400) để chống gán accountType/employeeId/permissions/customerId. Không trim mật khẩu; email trim/normalize theo spec. Email tối đa 254, mật khẩu 12–128, request body tối đa 16 KiB. Nội dung lỗi không phản chiếu token/mật khẩu.

| Endpoint | Auth/input | Thành công và lỗi chính |
|---|---|---|
| POST auth/register | Public `{email,password}` | 202 `{status:"verificationPending"}` cho email mới/cũ; không tạo Customer. Email active không đổi password hoặc phát lại token đăng ký |
| POST auth/email-verifications/resend | Public `{email}` | 202 thông điệp chung; pending account được token mới, vô hiệu token cũ; không báo tồn tại |
| POST auth/email-verifications/confirm | Public `{token}` | 204; sai/hết hạn/đã dùng 400 `invalidVerificationToken`; không login tự động, không cấp liên kết |
| POST auth/login | Public `{identifier,password,clientType}`; clientType web/native | 200 token envelope; sai/khóa/nhân viên inactive 401 `invalidCredentials`; khách đúng mật khẩu nhưng chưa verify 403 `emailVerificationRequired` |
| POST auth/refresh | Refresh credential + clientType | 200 envelope xoay refresh; 401 invalid/revoked/expired, 409 `refreshInProgress` theo quy tắc dưới |
| POST auth/logout | Refresh credential; không đòi access còn hạn | 204 cả khi credential đã hết hạn/thu hồi; thu hồi session tìm được, xóa cookie; CSRF áp dụng với web |
| GET auth/me | Bearer, kiểm SQL mỗi request | 200 MeResponse; 401 hết/thu hồi phiên, 503 không đọc được trạng thái |

Nhân viên login bằng TenDangNhap được cấp, khách bằng email; TenDangNhap nhân viên không chứa @ để không nhập nhằng namespace. Không có customerId/employeeId/role trong login request. clientType chỉ chọn transport token, không quyết định quyền.

TokenEnvelope: `{accessToken, tokenType:"Bearer", expiresIn:300, sessionId, refreshToken?}`; response `Cache-Control: no-store`. Web không có refreshToken trong JSON; native nhận token để giữ trong kho bí mật. Không đưa token vào URL hoặc log. MeResponse: `{accountId,accountType:"customer"|"employee",emailVerified,profileState:"unlinked"|"linked"|"notApplicable",employee:null|{id,title,administrativeDuty},permissions:[],capabilities:{profilesRead,profilesCreate,profilesUpdate:false,selfClaim:false,representatives:false}}`. Customer permissions rỗng ở M1; phạm vi được server giải quyết, không trả toàn bộ ID vào token. Employee trả đúng một title từ NhanVien, administrativeDuty từ server. Không trả dữ liệu nhạy cảm hồ sơ trong auth/me.

### Email, rate limit và token transport

- Register/resend public trả cùng status/body cho email mới/cũ. Pending registration lặp không thay hash mật khẩu hiện có. Unique race không thành 500. Verify chỉ dùng token ngẫu nhiên; request chứa token qua POST, client xóa token khỏi URL nếu frontend nhận email link.
- Login tối đa 10 lần/15 phút theo identifier chuẩn hóa + IP; register/resend 5 lần/giờ/IP và 3 lần/giờ/email, cooldown resend 60 giây; confirm 10 lần/15 phút/IP. 429 có Retry-After, áp dụng cùng hành vi dù account tồn tại. Đây là cấu hình kỹ thuật đề nghị review, không mô phỏng chính sách khóa UI. Không khóa account vĩnh viễn do brute force.
- Web refresh cookie: `__Host-klcn-refresh`, HttpOnly, Secure, SameSite=Strict, Path=/, không Domain. Refresh/logout web body không nhận token; kiểm Origin đúng allowlist + signed double-submit CSRF token ở cookie đọc được và header `X-CSRF-Token`. CSRF token được cấp login và xoay cùng refresh, gắn session; cookie không thay bearer cho Profiles. Dev HTTPS hoặc test transport riêng, không nới production.
- Native refresh/logout body `{clientType:"native",refreshToken}`; không gửi cookie. Web body `{clientType:"web"}`. Có cả cookie và body token hoặc type không khớp thì 400. Không coi User-Agent/clientType là danh tính đáng tin.
- Email gửi qua outbox cùng transaction account/token; retry delivery không tạo tài khoản/token mới. Payload outbox có token cần mã hóa bằng khóa ngoài DB, xóa payload sau gửi/hết hạn; không log body email. Không lưu token rõ ở bảng token.

### Refresh cạnh tranh và request tiếp theo

Mỗi login tạo session riêng, mỗi session chỉ có một refresh token active. Client phải single-flight refresh (mọi request cùng chờ một promise; nhiều tab web phối hợp một leader). Không tự động refresh lại request register/logout hoặc retry ghi Profiles chưa biết kết quả.

Trong transaction, khóa account/session rồi CAS token active → consumed và tạo token kế tiếp; một request thắng. Request thua dùng cùng token trong 5 giây kể từ ConsumedAt nhận 409 + Retry-After: 1, không token và không thu hồi token thắng. Sau 5 giây, dùng lại token consumed thu hồi session và 401 `refreshReplay`. Nếu không phân biệt được cạnh tranh với replay trong 5 giây, vẫn không cấp token thứ hai; rủi ro cửa sổ này được reviewer xem xét rõ.

Web waiter chờ leader cập nhật cookie; native waiter nhận token mới từ coordinator. Chỉ retry refresh một lần sau leader thành công; không leader/token mới hoặc response thắng bị mất thì login lại. Không lưu/cache response token rõ ở server để phục hồi. Logout và refresh serialize cùng session; logout commit sau rotation phải thu hồi cả token mới. Native retry 409 không gửi mãi token cũ.

Account locked, session revoked, SecurityVersion lệch hoặc employee inactive làm access và refresh thất bại ở request tiếp theo sau commit. Mọi request đọc primary SQL, không replica trễ/cache quyền. Ghi kiểm lại quyền trong transaction như [spec](../docs/m1-identity-review.md). JWT có chữ ký/hạn hợp lệ chưa đủ cho phép truy cập.

## Cấp nhân viên và quản trị tối thiểu

Không thêm HTTP quản trị accounts/permissions ở M1. Công cụ vận hành riêng cung cấp `bootstrap-admin`, `provision-employee`, `set-account-status`, `set-extra-permissions`, `set-employee-status`. Auth actor qua credentials được kiểm tra cùng hash/account/employee/quyền hiện hành, không nhận actorId để tin ngay. Audit lưu actor đã xác thực; không mật khẩu trong argv hoặc stdout.

- Bootstrap chỉ chạy một lần khi chưa có account, với secret ngoài Git và xác nhận local operator; cần NhanVien thật đã tồn tại. Tạo Admin Technical, seed đủ 12 mã quyền đã duyệt. Bất kỳ dữ liệu account cũ nào cũng chặn bootstrap, không tự tiếp quản.
- Actor các lệnh khác cần Employee active + Admin + QuanLyTaiKhoan + không Director. Provision nhận employeeId, username và mật khẩu ban đầu nhập an toàn; server kiểm FK, chức danh hợp lệ, unique và trạng thái. Không nhận/chấp nhận title/quyền trong public register/login.
- Không tự khóa hoặc tự thay quyền; cấm gán QuanLyTaiKhoan cho Director dù command chạy riêng. Không cho đổi administrativeDuty qua public API; thay duty qua vận hành có review và audit. Các thay đổi account/quyền/status tăng SecurityVersion và thu hồi sessions trong transaction.
- Danh mục 12 quyền giữ nguyên Screen Spec, ánh xạ code ổn định sang MaQuyen int; không seed quyền y tế chỉ vì Technical có đủ mã bổ sung. Không suy quyền ghi chuyên môn từ Admin.

## Năm route Profiles — cùng URL, bảo vệ và giới hạn mới

| Route | Employee | Customer đã verify |
|---|---|---|
| GET customers?search= | LeTan: đọc; Admin + XemHoSo: chỉ đọc | Chỉ Customer có liên kết chính chủ active; không liên kết → items rỗng |
| POST customers | LeTan active: tạo hồ sơ, không cấp verified/link | 403, dùng bản tự khai M2 thay vì hồ sơ chính thức |
| GET recipients?customerId=&search= | LeTan hoặc Admin + XemHoSo | Chỉ recipient có liên kết chính chủ active trực tiếp; không suy quyền từ CustomerId |
| POST recipients | LeTan active; CustomerId phải tồn tại; tạo không xác minh | 403, không suy quyền tạo từ chính chủ Customer |
| PUT recipients/{id} | Kiểm quyền ghi LeTan + phạm vi trước xử lý body | 403 ở M1, chưa có flow duyệt sửa |

Không có role khác mở danh sách Profiles. Clinical/thu ngân cần endpoint đọc hẹp ở module tương ứng, không tận dụng list toàn hệ thống. Nhân viên có quyền đọc/ghi M1 truy cập phạm vi trung tâm hiện tại (repo chưa có mô hình cơ sở/ca phụ trách); không tự đặt lọc cơ sở giả. Không dùng phạm vi này cho triển khai nhiều cơ sở trước khi có contract riêng.

GET lọc auth scope trước search/order/Take; thêm `page` mặc định 1 và `pageSize` mặc định 25, tối đa 100, số dương; `search` trim, tối đa 150. Response giữ `{items:[CustomerDto|RecipientDto]}`, không trả tổng toàn hệ thống. customerId sai định dạng/không dương 400; ngoài scope trả items rỗng, không xác nhận hồ sơ tồn tại. DTO giữ các trường hiện tại; trang liên tiếp không cam kết snapshot khi có tạo mới.

POST giữ CreateCustomerRequest/CreateRecipientRequest hiện tại; thành công 201 + DTO và Location hiện có. Không thêm GET-by-id ở M1; Location là định danh resource, chưa hứa dereference được. Customer identityNumber required, recipient nullable; không tạo recipient tự động khi tạo customer. Không nhận verified/ownerAccountId/permissions trong payload.

PUT giữ UpdateRecipientRequest để review tương thích nhưng **không cho cập nhật thực tế ở M1**: đủ quyền + bản ghi trong phạm vi thì 409 `identityApprovalRequired`, kể cả body không đổi; body sai schema trả 400 sau khi kiểm auth. Ngoài scope/không tồn tại 404; thiếu quyền chức năng 403. Không ghi trước rồi hoàn tác; không tự mở sửa quan hệ/Gender như đường vòng sửa danh tính. M2–M4 phải review contract trước mở lại. Không bổ sung PUT customers trong M1.

### Validation thống nhất cho tạo hồ sơ chính thức

- fullName required, trim, 1–150; identityNumber đúng 12 ký tự ASCII 0–9, giữ số 0 đầu; customer required, recipient blank → null.
- phoneNumber customer required, 10–11 chữ số ASCII theo Screen Spec; email liên hệ required, cú pháp email một địa chỉ, tối đa 254. Không dùng email liên hệ đăng nhập hoặc nhận quyền. Không dùng regex hứa kiểm toàn RFC.
- dateOfBirth required, ngày hợp lệ, không DateOnly.MinValue hoặc tương lai theo Asia/Saigon; gender Nam/Nữ/Khác; relationshipToCustomer required trim 1–50, chỉ là lời khai.
- CustomerId > 0 và FK tồn tại. Không tự đưa ngưỡng tuổi/điều kiện y tế. CCCD khách unique ở SQL; recipient chưa thêm unique CCCD vì người có thể đồng thời là customer/recipient và quy tắc hợp nhất chưa được duyệt. Không đổi required của hồ sơ chính thức.
- POST không có idempotency contract ở M1. Client không tự retry ghi khi timeout, phải tra lại trước; concurrent customer trùng CCCD → 409. Không tuyên bố exactly-once cho tạo recipient; test phải ghi rõ giới hạn này.

## Problem Details

Envelope lỗi `{type,title,status,code,traceId,errors?}`; errors là map camelCase → string[]. Type dùng `about:blank`; không phụ thuộc text để điều khiển UI. 400 validation/unknownFields/invalidVerificationToken; 401 invalidCredentials/invalidSession/refreshReplay; 403 permissionDenied/emailVerificationRequired/csrfFailed; 404 resourceNotFound; 409 duplicateIdentity/identityApprovalRequired/refreshInProgress/concurrencyConflict; 429 rateLimited; 503 dependencyUnavailable. Không trả stack/SQL/token hoặc hồ sơ ngoài phạm vi; log traceId và code, không CCCD/nội dung y tế/email token. SQL constraint race map đúng 409, không bắt mọi DbUpdateException thành duplicate.
