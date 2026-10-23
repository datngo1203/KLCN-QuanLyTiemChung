# M1 — Identity tối thiểu và bảo vệ Profiles

Trạng thái: **đặc tả M1 được người dùng chấp thuận để triển khai, chưa triển khai**. Ngày 23/10/2026, người dùng thay gate riêng M1: báo cáo review kỹ thuật từ chat độc lập tại `20a9a72b42c660bb642cac213045616a111de8ee` cùng chấp thuận contract/nghiệp vụ của người dùng thay yêu cầu Bảo/Quý xác nhận trước code. Không phải approval của Bảo/Quý hoặc GitHub approval. Base `develop` tại `f188084389a0033b856594632ae2c6495b29e248`. PR này chỉ tài liệu; thay gate hành chính không đổi nội dung kỹ thuật đã review.

## Phạm vi và nguồn quyết định

Căn cứ: [bối cảnh](project-context.md), [kiến trúc](architecture.md), [phân công](team.md), [quyền đã duyệt](desktop-screen-specs.md), quyết định người dùng trong chat Profiles + Identity. Web/Mobile là khách hàng; Windows là nhân viên. Quyền sở hữu module không cấp quyền nghiệp vụ cho client.

- M1: SQL → Identity → bảo vệ năm route Profiles; tài khoản khách độc lập, email verification/login/logout/refresh, cấp nhân viên có kiểm soát, auth/me, quyền/phạm vi và audit tối thiểu.
- M2: bản tự khai và nối client; M3: xác minh/liên kết/nhiều đại diện/thu hồi; M4: yêu cầu sửa định danh/đổi email và E2E. Chưa triển khai M2–M4, không loại khỏi lộ trình P0–P7.
- #29 UI Windows, #30 housekeeping và #31 Mobile chưa được giả định đã tích hợp. Không lấy nội dung từ các nhánh này; #21/#27 chỉ truy vết. Không kéo `38c4f84`/`fa027ee`.
- Code base đã map năm route Profiles và gọi SQL nhưng chưa auth/scope; tài liệu cũ ghi “dự kiến” không phải bằng chứng chưa có code. Health/catalog demo không chứng minh SQL/auth.

## Gói review và owner

| Thành phần | Đầu mối | Review độc lập cần có |
|---|---|---|
| Identity, quyền, SQL, điều phối | Đạt | Gate trước code M1 theo quyết định người dùng ở đầu tài liệu; thay đổi contract/schema mới cần review độc lập |
| Profiles API/nghiệp vụ | Bảo | Người dùng đã chấp thuận contract/nghiệp vụ M1; thay đổi mới giữ review chéo Đạt/Quý theo module |
| Windows A03/Login/A12/A14 và điểm nối (M2) | Đạt | Bảo review A03/API; Quý review phần liên quan |
| Web (M2) / Mobile (M2) | Bảo / Quý | Thành viên khác review contract/luồng |

Agent được phép làm các module không đổi owner. Không coi CI, tự rà hoặc nhận xét của chính tác giả là review độc lập. Không tự cấp quyền GitHub hoặc gán người không đủ quyền.

Đọc theo thứ tự: [contract](../contracts/m1-identity-profiles.md) → [schema đề xuất](../database/m1-identity-schema-review.md) → [checklist SQL/auth](m1-verification-checklist.md). Gate trước code M1 đã đáp ứng bằng nguồn quyết định ở đầu tài liệu, bao phủ đặc tả tại SHA đã review; được tiếp tục migration/API/test trên nhánh riêng phụ thuộc #32 mà không merge PR này. Contract/schema thay đổi thực chất cần review độc lập phần đổi trước thực hiện phần bị ảnh hưởng. Giữ owner/review chéo và branch protection; không tự gán GitHub approval. Gate hành chính này không yêu cầu review lại nội dung kỹ thuật không đổi.

## Quyết định kỹ thuật đề nghị chấp thuận

1. Mở rộng TaiKhoan hiện có thành chủ thể độc lập; phân loại Customer/Employee, khách không có FK bắt buộc đến hồ sơ. Không thay required của KhachHang/NguoiDuocTiem, không nhân viên/CCCD giả.
2. Email đăng nhập chuẩn hóa trim + invariant uppercase, unique; email gốc giữ để hiển thị. Không tự bỏ dấu chấm/đuôi + của email. Email liên hệ không dùng để nhận quyền/khôi phục.
3. PasswordHasher của ASP.NET Core; mật khẩu 12–128 ký tự, không trim/ghi log; đây là mặc định kỹ thuật cần review, không kế thừa 6 ký tự của UI mẫu. Cho phép passphrase/Unicode. Token ngẫu nhiên 256 bit; SQL chỉ lưu SHA-256 token, không lưu token rõ.
4. Access token ký bởi server, hạn 5 phút; refresh hạn tuyệt đối 7 ngày/session, không kéo dài theo rotation. Email token 30 phút, một lần. Dùng UTC cho thời điểm, Asia/Saigon cho validation ngày sinh. Secret ký/mail ở secret store, không Git.
5. Mọi request bảo vệ kiểm SQL account/session/employee còn hoạt động, email verified nếu khách, và quyền hiện hành; không dùng quyền/JWT cache để cho phép request. SQL lỗi thì fail closed 503. Token chỉ định danh sub/sid, không tin chức danh hoặc employeeId do client gửi.
6. Refresh một nhánh tuần tự/session; CAS trong transaction, không grace window trả lại token đã xoay. Cạnh tranh hợp lệ trả 409 không thu hồi phiên ngay; token đã dùng lại ngoài cửa sổ cạnh tranh 5 giây thu hồi toàn session. Chi tiết bắt buộc trong contract; không retry vô hạn.
7. Hồ sơ chính thức ở M1 chỉ được tạo bởi LeTan có trạng thái nhân viên hợp lệ. Không có bằng chứng xác minh mới thì không đánh dấu verified/không tự cấp liên kết chính chủ. M1 không có flow duyệt sửa định danh nên PUT thay đổi bất kỳ trường định danh/quan hệ đều bị từ chối.
8. Không thêm permission builder hoặc mã quyền mới ngoài M1. Nhận diện nhiệm vụ Director bằng thuộc tính server riêng, không tên/BoPhan. Cấm QuanLyTaiKhoan với Director ở bootstrap, lệnh vận hành, quản trị và kiểm tra request.
9. Cấp nhân viên/khóa tài khoản/thay quyền qua công cụ vận hành có xác thực actor server-side và transaction/audit; chưa mở API CRUD nhân viên/quyền trong M1. Không tạm dùng script SQL tùy ý làm quy trình cấp tài khoản. Công cụ chạy riêng không được gọi từ API public.
10. Chỉ fixture test/dev riêng tạo liên kết chính chủ; không endpoint claim/accept/link, không bật bằng một flag trên DB production. M1 không có đường cấp quyền đại diện thực tế.

## Khóa/thu hồi và ranh giới request

Bổ sung phạm vi ngày 23/10/2026: người dùng cho phép `reset-employee-password` offline để xử lý hash legacy ngoài target Admin recovery. [Contract bổ sung](../contracts/m1-identity-profiles.md#bổ-sung-reset-employee-password--phạm-vi-đã-duyệt-contract-chờ-review) còn chờ review độc lập; chưa viết lệnh, không coi approval M1 trước đây bao phủ phần mới. Actor dùng credential DB mới sau recovery; target chọn bằng ID bất biến và RowVersion. Chỉ hash/SecurityVersion/session/credential/audit thay đổi nguyên tử; giữ status/duty/quyền và ledger recovery. Không endpoint, schema mới, SQL thủ công hoặc mở rộng M2–M4. Bàn giao trực tiếp cho đúng nhân viên, không giả flow đổi mật khẩu chưa triển khai. Sau review được chấp thuận mới tiếp tục Goal M1, viết lệnh và chạy tests/SQL suite/CI tại HEAD code mới; không gọi Goal complete tại mốc tài liệu này.

Account có SecurityVersion; session lưu phiên bản lúc cấp. Khóa account, đổi trạng thái nhân viên hoặc thay quyền được commit cùng audit; thao tác tăng SecurityVersion và thu hồi refresh sessions. Request tiếp theo (bắt đầu sau commit) đọc SQL và bị 401 nếu phiên đã thu hồi. Sau login lại, quyền mới có hiệu lực. Không đợi JWT hết hạn. auth/me cũng chịu quy tắc này.

Request đang chạy trước thu hồi không bị hứa hẹn hủy ngược. Mọi ghi Profiles phải kiểm lại account/session/quyền trong transaction trước commit; cùng khóa account theo thứ tự account → session → hồ sơ → audit để serialize với lệnh thu hồi. Nếu quyền bị thu hồi trước bước kiểm cuối thì rollback. Test hai thứ tự commit. Không trả thành công ghi nếu audit cùng transaction thất bại.

## Vòng đời và nhóm trường (lộ trình, không tạo workflow M2–M4 trong M1)

Tài khoản PendingEmail → ActiveUnlinked sau verify → ActiveLinked khi có liên kết hợp lệ; Locked độc lập với trạng thái liên kết. ActiveUnlinked là trạng thái suy ra, không bắt tạo Customer. Xác minh email không tạo quyền với hồ sơ có email/CCCD trùng.

| Đối tượng/actor | Định danh: tên/CCCD/ngày sinh/giới tính | Liên hệ | Liên kết/quyền |
|---|---|---|---|
| Chủ nháp tự khai (M2) | Sửa dữ liệu mình khai | Sửa | Không tự cấp |
| Đã gửi xác minh (M3) | Khóa snapshot; rút trước xử lý để sửa/gửi lại | Như định danh | Xem trạng thái |
| Chính chủ hồ sơ chính thức | Yêu cầu duyệt M4, chưa đổi dữ liệu hiện hành | Sửa khi có contract tương ứng | Không tự đổi |
| Đại diện (M3) | Đề nghị sửa chỉ khi được cấp | Theo quyền cấp rõ | Không tự thêm/chuyển quyền |
| Nhân viên | Theo ma trận chức danh; M1 không duyệt sửa định danh | Theo quyền nghiệp vụ | Quyền xác minh riêng M3 |

Sau xác minh, khóa snapshot tự khai và dùng liên kết chính thức. Yêu cầu sửa lưu giá trị đề nghị riêng, duyệt có kiểm phiên bản; từ chối/rút giữ nguyên dữ liệu. Không biến hồ sơ đã liên kết thành người khác bằng đổi tên/CCCD/ngày sinh. Không dùng quan hệ “Con” hoặc CustomerId để suy quyền.

## Giao hàng và điều kiện vận hành

Recovery sửa tiếp theo review `427f52a9`/base `f1880843`: migration xong vẫn maintenance → operator ngoài DB đọc/bind RowVersion mới → transaction kiểm lại trước ghi. Ledger unique theo định danh DB bền vững và mốc M1; operationId chỉ chống retry. Manifest mới không cấp lượt mới, kể cả admin lại bị khóa; không mở cơ chế recovery khác trong M1. Ba mục revoke credential/CSRF/bearer memory-only giữ nguyên.

Sửa review trên `ff1294b9`/base `f1880843`: recovery offline DB nâng cấp theo contract với manifest ngoài DB/ledger/audit; revoke session đồng thời revoke credential chưa consumed; Web Locks và đọc lại CSRF cookie trong lock; bearer web chỉ memory. Bootstrap DB rỗng, recovery DB nâng cấp và quản trị thường ngày theo ba điều kiện riêng trong contract. Bổ sung này cần review lại, chưa phải implementation hoặc approval độc lập.

P0–P7 giữ nguyên lộ trình. Mỗi lát chỉ làm đặc tả → migration/API → test tương ứng sau approval, không migrate trước toàn bộ đại diện/audit mở rộng. M1 SQL/Identity/Profiles phải qua checklist SQL thật trước nối client M2. Không gọi M1 là đã hoàn thành khi mới có PR đặc tả.

Production chưa cho sử dụng dữ liệu thật trước nghiệm thu. Endpoint M2–M4 chưa được map; feature capability trả false. Khi triển khai các mốc này, server vẫn gate theo môi trường/quyết định vận hành, UI chỉ phản ánh capability; không có bypass production. Mail M1 chỉ gửi hộp thư sink kiểm thử cho đến khi dịch vụ gửi thật được cấu hình và kiểm chứng. DB test/dev và bí mật tách biệt; fixture runner phải từ chối non-Development/non-Test và DB không có marker fixture, không nhận connection production.

Các lựa chọn kỹ thuật 1–10 của M1 tại SHA đã review được người dùng chấp thuận theo gate riêng; không suy ra nghiệp vụ M2–M4 được duyệt. Tiêu chuẩn bằng chứng chính chủ/đại diện, người được phân công xác minh, đồng ý/thời hạn quyền, sửa định danh/tranh chấp và chính sách thanh toán/nhắc/đổi hủy chờ quyết định ở M3/M4. Ghi chú nhân viên không thay thế bằng chứng.
