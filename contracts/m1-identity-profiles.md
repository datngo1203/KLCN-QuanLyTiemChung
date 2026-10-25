# Contract review M1 — chưa thực thi

Review implementation tại `9fa35ab`: [bổ sung hẹp hai P1 operator/delivery](m1-p1-operator-delivery-review.md) đang chờ review trước code phụ thuộc. Các mô tả nguồn secret/config và delivery cũ dưới đây không chứng minh boundary/lifecycle đã an toàn; chưa được coi hai findings đã sửa.

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

Web access token chỉ ở memory, không localStorage/sessionStorage/IndexedDB. Refresh giữ cookie HttpOnly/Secure; CSRF cookie riêng theo transport trên. Coordinator dùng Web Locks API độc quyền `klcn-auth` chung login/refresh/logout toàn origin; browser thiếu Web Locks phải yêu cầu login, không fallback lease storage. Trong lock, đọc lại CSRF cookie ngay trước fetch, không giữ header cũ; browser tự gửi refresh cookie hiện hành. Leader giữ lock đến response hoàn tất, chỉ phát completion/access token qua BroadcastChannel sau browser áp dụng Set-Cookie; waiter dùng access token này trong memory, không refresh lại. Nếu bỏ lỡ completion, waiter lấy lock và đọc lại cookie/CSRF để refresh tối đa một lần. Tab đóng tự giải phóng lock; leader lỗi/mất response, timeout tổng 15 giây hoặc 409 không có completion thì xóa memory và yêu cầu login lại, không retry khi kết quả cũ chưa biết. Logout broadcast xóa memory. Thiếu một cookie hoặc csrfFailed thì login lại; không nới CSRF/Origin/replay. Channel không lưu bền token.

Mọi đường revoke session/family phải revoke credential chưa consumed trong cùng transaction theo invariant schema; mọi sử dụng vẫn kiểm account/session/SecurityVersion.

Mỗi login tạo session riêng, mỗi session chỉ có một refresh token active. Client phải single-flight refresh (mọi request cùng chờ một promise; nhiều tab web phối hợp một leader). Không tự động refresh lại request register/logout hoặc retry ghi Profiles chưa biết kết quả.

Trong transaction, khóa account/session rồi CAS token active → consumed và tạo token kế tiếp; một request thắng. Request thua dùng cùng token trong 5 giây kể từ ConsumedAt nhận 409 + Retry-After: 1, không token và không thu hồi token thắng. Sau 5 giây, dùng lại token consumed thu hồi session và 401 `refreshReplay`. Nếu không phân biệt được cạnh tranh với replay trong 5 giây, vẫn không cấp token thứ hai; rủi ro cửa sổ này được reviewer xem xét rõ.

Web waiter chờ leader cập nhật cookie; native waiter nhận token mới từ coordinator. Chỉ retry refresh một lần sau leader thành công; không leader/token mới hoặc response thắng bị mất thì login lại. Không lưu/cache response token rõ ở server để phục hồi. Logout và refresh serialize cùng session; logout commit sau rotation phải thu hồi cả token mới. Native retry 409 không gửi mãi token cũ.

Account locked, session revoked, SecurityVersion lệch hoặc employee inactive làm access và refresh thất bại ở request tiếp theo sau commit. Mọi request đọc primary SQL, không replica trễ/cache quyền. Ghi kiểm lại quyền trong transaction như [spec](../docs/m1-identity-review.md). JWT có chữ ký/hạn hợp lệ chưa đủ cho phép truy cập.

## Cấp nhân viên và quản trị tối thiểu

### Recovery DB nâng cấp một lần

`recover-upgraded-admin` là công cụ offline riêng, không HTTP/startup seeder và không nằm trong API publish. Chỉ chạy maintenance sau migration M1 khi không có Employee active + Admin + QuanLyTaiKhoan + không Director. Operator xác thực bằng secret 256 bit ngoài DB qua secret store và OS identity allowlist, không cần actor account active. Không secret trong argv/log. Bootstrap vẫn chỉ DB không account; recovery chỉ DB có account cũ; quản trị tiếp theo vẫn cần actor account như dưới.

Trình tự bắt buộc: migration M1 thành công → vẫn maintenance, không nhận ghi đồng thời → operator đã xác thực ngoài DB đọc lại đúng DB/migration/target và RowVersion thực tế → lập/bind manifest trong secret store → thực thi recovery. Manifest gồm operationId duy nhất, RecoveryDatabaseId bền vững của DB, mốc nâng cấp M1, migration ID, target accountId/employeeId/expected RowVersion vừa đọc, xác nhận người thật được phân công Technical không Director, hạn tối đa 30 phút. Phê duyệt target trước migration nếu có chỉ xác nhận target/duty, không chứa version dự đoán; phải bind version sau migration. Không nhận tham số tùy ý chọn target/quyền. Kiểm target Employee cũ, nhân viên active/title Admin; sai DB/mốc/migration/target/version/hạn thì dừng trước ghi. Không suy duty từ tên/BoPhan.

Transaction Serializable lấy application lock độc quyền chung với bootstrap/provision/status/permission/duty commands. Trước ghi, kiểm lại DB/mốc/migration/target và expected RowVersion dưới khóa target, điều kiện không có admin hiệu lực và ledger. Ledger duy nhất theo (RecoveryDatabaseId, M1UpgradeEpoch): operationId chỉ dùng nhận diện retry, không cấp thêm lượt. Chỉ target được đổi Technical + Active, cấp đúng 12 mã đã duyệt, đặt lại password nhập an toàn nếu hash cũ không xác định, tăng SecurityVersion, revoke toàn session/credential. Ghi ledger operationId/fingerprint manifest/kết quả và audit operator OS identity đã xác thực (ActorAccountId null, SubjectAccountId target) cùng transaction. Audit lỗi hoặc stale-version rollback toàn bộ, không có dấu thành công. Hai operator chỉ một commit. Retry được xác thực đúng cùng operationId/fingerprint/DB/mốc trả kết quả đã ghi trước khi xét version/trạng thái đã đổi, không ghi audit/cấp quyền lại; nếu chưa commit thì kiểm lại đầy đủ để thử trong hạn. Manifest hoặc operationId mới sau thành công bị chặn theo DB/mốc, kể cả Admin lại bị khóa. Sau commit hủy secret/manifest ngoài DB chỉ là cleanup; không reset ledger khi đổi secret/manifest. Tài khoản bị khóa về sau nằm ngoài recovery nâng cấp một lần này, không tự mở đường recovery mới. Không target Admin cũ hợp lệ thì dừng xử lý dữ liệu qua quy trình nâng cấp được review, không tự tạo nhân viên/account thay thế. Đây là ngoại lệ recovery có target cố định, không cho self-change trong quản trị thường ngày.

Không thêm HTTP quản trị accounts/permissions ở M1. Công cụ vận hành riêng cung cấp `bootstrap-admin`, `provision-employee`, `set-account-status`, `set-extra-permissions`, `set-employee-status`; bổ sung đề nghị review `reset-employee-password` theo mục dưới, chưa được phép viết lệnh trước review. Auth actor qua credentials được kiểm tra cùng hash/account/employee/quyền hiện hành, không nhận actorId để tin ngay. Audit lưu actor đã xác thực; không mật khẩu trong argv hoặc stdout.

- Bootstrap chỉ chạy một lần khi chưa có account, với secret ngoài Git và xác nhận local operator; cần NhanVien thật đã tồn tại. Tạo Admin Technical, seed đủ 12 mã quyền đã duyệt. Bất kỳ dữ liệu account cũ nào cũng chặn bootstrap, không tự tiếp quản.
- Actor các lệnh khác cần Employee active + Admin + QuanLyTaiKhoan + không Director. Provision nhận employeeId, username và mật khẩu ban đầu nhập an toàn; server kiểm FK, chức danh hợp lệ, unique và trạng thái. Không nhận/chấp nhận title/quyền trong public register/login.
- Không tự khóa hoặc tự thay quyền; cấm gán QuanLyTaiKhoan cho Director dù command chạy riêng. Không cho đổi administrativeDuty qua public API; thay duty qua vận hành có review và audit. Các thay đổi account/quyền/status tăng SecurityVersion và thu hồi sessions trong transaction.
- Danh mục 12 quyền giữ nguyên Screen Spec, ánh xạ code ổn định sang MaQuyen int; không seed quyền y tế chỉ vì Technical có đủ mã bổ sung. Không suy quyền ghi chuyên môn từ Admin.

### Bổ sung reset-employee-password — phạm vi đã duyệt, contract chờ review

Ngày 23/10/2026 người dùng chấp thuận phạm vi reset account nhân viên legacy đã tồn tại; đây không phải approval contract mới. Chỉ lệnh offline operator hiện có, không HTTP/public endpoint, không SQL thủ công, không bootstrap lại, không mở lại recovery DB nâng cấp một lần. Không tạo account/nhân viên hoặc cập nhật ledger recovery. Không sửa schema để làm reset; dùng hash/version/session/credential/audit hiện có.

**Input không bí mật:** `targetAccountId` int, `targetEmployeeId` int và `expectedTargetRowVersion` base64 bắt buộc, đọc từ đúng DB/account hiện hành qua công cụ vận hành read-only. Target chọn bằng cặp ID bất biến, không bằng tên/username/email hoặc kết quả tìm kiếm có thể thay đổi; kiểm liên kết EmployeeId dưới khóa trước ghi. Người vận hành xác nhận cặp ID/username hiển thị trước khi nhập password. Không cho client gửi status/duty/quyền hoặc actorId để tự nhận actor. Sai/malformed version/ID thì từ chối trước ghi; không tự tải version mới để ép ghi đè.

**Xác thực actor:** operator nhập identifier và mật khẩu hiện hành ở prompt ẩn, đối chiếu PasswordHasher backend với hash trong DB. Sau Admin recovery phải dùng credential mới được recovery đặt/cấp và hash hiện hành; secret/manifest/OS identity dùng cho recovery hoặc access token cũ không thay được credential actor thường ngày. Actor là account Employee Active, nhân viên Active, title Admin, có QuanLyTaiKhoan và duty không Director theo DB; targetAccountId khác actor. Ghi nhận actorId/RowVersion/SecurityVersion khi xác thực; dưới khóa transaction kiểm lại trạng thái, quyền, hash/version actor không đổi. Thay credential/khóa/thu hồi quyền actor trong lúc chuẩn bị thì từ chối, yêu cầu xác thực lại; không tin snapshot trước giao dịch. Account hash legacy không xác định không được dùng làm actor, cần Admin đã recovery hợp lệ.

**Password target:** nhập hai lần bằng prompt tương tác không echo; từ chối stdin redirect/không có terminal nhập ẩn. Không nhận password qua argv, environment, file, clipboard tự động, log/stdout/stderr/exception/audit hoặc transcript; không in hash. Kiểm chính sách 12–128 hiện có, không trim; hash bằng PasswordHasher backend. Không dùng password actor làm password target. Hash tính trước transaction, sau đó kiểm lại version dưới khóa; dữ liệu password chỉ giữ trong bộ nhớ thời gian cần thiết, giải phóng/xóa buffer khi khả thi, không hứa xóa được mọi bản sao managed string.

**Transaction:** dùng Serializable/application lock quản trị hiện có và thứ tự khóa nhất quán. Kiểm actor như trên; kiểm target tồn tại, AccountType Employee, FK đúng targetEmployeeId và RowVersion bằng expected. Customer/nonexistent/mismatched employee bị từ chối; không provision account trùng. Reset được hash cho Employee Locked hoặc nhân viên inactive nhưng không cho họ login cho đến khi trạng thái được thay bởi quy trình riêng đã được phép. Không thay TenDangNhap, TrangThai account/nhân viên, title, duty, quyền, email, FK hoặc ledger recovery. Cập nhật chỉ MatKhauHash và tăng SecurityVersion đúng một lần; thu hồi toàn bộ sessions target và mọi refresh credential chưa consumed/chưa revoked của các session đó, kể cả credential hết hạn. Audit `employeePasswordReset` có actor đã xác thực, targetAccountId, traceId và tên trường thay đổi, không mật khẩu/hash/nội dung bàn giao. Tất cả cùng một transaction; version mismatch/audit/revoke/SQL failure rollback toàn bộ. Không revoke session actor hoặc nhân viên khác.

**Kết quả/lỗi:** thành công chỉ sau commit, output targetAccountId/targetEmployeeId và RowVersion mới, không credential. Actor sai credential/locked/thiếu quyền/Director/self-reset → từ chối; target sai loại/không tồn tại/ID không khớp → từ chối; stale target hoặc actor version → conflict. Hai reset cùng expectedTargetRowVersion chỉ một commit; lệnh thua không ghi password/version/audit thành công. Timeout chưa rõ commit không tự retry: đọc trạng thái/version qua operator và xác minh vận hành, không suy thành công từ password nhập lại, không dùng recovery để thử lại. Đây không phải lệnh idempotent tự phát lại password.

**Bàn giao credential:** đúng nhân viên nhận trực tiếp tại phiên hỗ trợ đã đối chiếu danh tính theo quy trình nội bộ được người quản trị chấp nhận; người nhận tự nhập mật khẩu mới trên terminal ẩn là mặc định. Nếu không tổ chức được phiên trực tiếp, dừng reset cho đến khi có kênh bàn giao bảo mật được phê duyệt; không tự email/SMS/chat hoặc ghi password ra file. Audit chỉ xác nhận sự kiện reset, không chứa credential hoặc bằng chứng danh tính nhạy cảm. M1 chưa có flow đổi mật khẩu nhân viên/self-service/force-change: không hứa mật khẩu “tạm” sẽ bị buộc đổi hoặc hết hạn. Nhân viên Active đăng nhập bằng credential vừa đặt; account Locked/inactive vẫn bị chặn. Không dùng reset thay quy trình mở khóa/cấp quyền.

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
