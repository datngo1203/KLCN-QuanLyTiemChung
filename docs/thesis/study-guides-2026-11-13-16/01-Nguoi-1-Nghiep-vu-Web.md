# Người 1 — Nghiệp vụ và Website

> Bản đọc trực tiếp trên GitHub, chuyển từ tài liệu Word ôn tập ngày 12/11/2026. Đây là tài liệu học; khi có xung đột, đối chiếu SCOPE.md và implementation hiện hành.

Người 1 – Nghiệp vụ và Web

KLCN090  |  TÀI LIỆU ÔN TẬP BẢO VỆ  |  13–16/11/2026

**BỘ TÀI LIỆU 01**
NGƯỜI 1 · NGHIỆP VỤ & WEBSITE
Học từ số 0 • Nói slide 1–6 • 12 câu phản biện có đáp án

- Đối tượng
- Thành viên phụ trách tổng quan, phân tích yêu cầu và Website khách hàng

- Lịch học
- 13–16 tháng 11 năm 2026

- Buổi bảo vệ
- 20 phút trình bày + 20 phút hỏi đáp

Nguồn:  SCOPE main, AGENTS.md, apps/web/AGENTS.md, mô hình UC thesis và đề cương HUIT.
Lưu ý:  Đây là tài liệu ôn tập dựa trên bằng chứng của repo và đề cương; không thay thế việc kiểm tra demo thực tế trước ngày bảo vệ.

A. Mục tiêu học và thuyết trình
Trong 1 phút nói rõ đề tài giải quyết vấn đề gì, mục tiêu là gì và khách hàng dùng Website ra sao.
Nhớ 7 quy trình nghiệp vụ và phân biệt chúng với 14 Use Case hệ thống của báo cáo.
Biết giải thích actor, ranh giới hệ thống, luồng Web → API → SQL mà không cần học React chuyên sâu.
Biết mô tả tra cứu, hồ sơ, đăng ký dịch vụ, lịch hẹn và lịch sử; tránh nhầm ngày giờ mong muốn với lịch xác nhận.
Nói trôi chảy slide 1–6 khoảng 5 phút.
B. Kiến thức nền bắt đầu từ zero
- Khái niệm
- Hiểu thế này
- Liên hệ dự án

- Bài toán / mục tiêu
- Bài toán là điều bất tiện cần giải quyết; mục tiêu là kết quả phần mềm phải hỗ trợ.
- Giảm phân tán thông tin của đăng ký, lịch hẹn, hồ sơ và quy trình dịch vụ.

- Yêu cầu chức năng
- Chức năng phần mềm phải làm được.
- Tra cứu, đăng ký, quản lý hồ sơ...

- Yêu cầu phi chức năng
- Đặc tính như dễ dùng, an toàn, khả năng tương thích.
- Web responsive, kiểm quyền server, trạng thái lỗi rõ ràng.

- Actor
- Vai trò tương tác với hệ thống hoặc tham gia nghiệp vụ.
- Khách hàng, lễ tân, bác sĩ...

- Business Use Case
- Quy trình nghiệp vụ, có thể gồm cả bước thủ công.
- Quy trình đăng ký dịch vụ tiêm chủng.

- System Use Case
- Hành vi của phần mềm mà actor gọi.
- Tra cứu dịch vụ, đăng ký dịch vụ.

- React
- Thư viện UI JavaScript để xây Web.
- Nội dung Website nằm dưới apps/web/src/.

- Customer ownership
- Khách chỉ đọc/sửa dữ liệu mà quyền/hồ sơ cho phép.
- API kiểm quyền/hồ sơ; không chỉ ẩn nút trên Web.

C. Nghiệp vụ phải nắm sâu
1) Bài toán và phạm vi
Theo đề cương HUIT, đề tài gồm Website và phần mềm Windows, có API và cơ sở dữ liệu, thực hiện khảo sát – phân tích – thiết kế – cài đặt – kiểm thử. Khi nói mục tiêu, dùng “hỗ trợ quản lý tại trung tâm” thay vì hứa “tự động hóa toàn bộ y tế”.
2) Khách hàng, người được tiêm và hồ sơ
Khách hàng là người sử dụng dịch vụ/giữ tài khoản; “người được tiêm” là hồ sơ đối tượng nhận dịch vụ. Hai khái niệm không nhất thiết trùng nhau. Website cần bảo vệ quan hệ sở hữu/được phép truy cập; API quyết định điều đó, không phải một nút bấm ẩn/hiện.
3) Tra cứu và đăng ký dịch vụ
Trên Web, khách hàng xem danh mục vắc xin/dịch vụ, tìm kiếm và lựa chọn theo dữ liệu Catalog hiện hành. Việc đăng ký là ghi nhận yêu cầu thực qua API/SQL. Không suy rằng mọi mục Catalog đều sẵn sàng cho mọi đối tượng hoặc tự động đủ điều kiện tiêm.
4) Ngày giờ mong muốn và lịch hẹn xác nhận
Theo RF-05F.1, Website có thể lưu ngày giờ mong muốn; nhân viên dùng preference để gợi ý. LichHenTiem là thực thể được xác nhận/quản lý riêng. Một yêu cầu đăng ký không tự động là một lịch tiêm đã chốt.
5) Website và trải nghiệm người dùng
Web dành cho khách hàng nên ưu tiên tra cứu, thao tác rõ ràng, hiển thị loading/empty/error, thông báo có ý nghĩa và responsive. Hiển thị giá theo snapshot/provenance; tuyệt đối không gắn mọi thông tin y khoa hiển thị thành chỉ định cho một cá nhân.
D. Luồng minh họa để giải thích trên slide
- Luồng tra cứu → đăng ký (mức hiểu tổng quan)
## 1. Khách hàng xem danh mục trên React Web. 2. Web gửi yêu cầu tới ASP.NET Core API. 3. API đọc dữ liệu Catalog ở SQL Server rồi trả kết quả. 4. Khách gửi yêu cầu đăng ký dịch vụ theo UI thực tế. 5. API kiểm quyền, thông tin yêu cầu và lưu nguồn đăng ký. 6. Nhân viên xử lý các bước tiếp theo theo nghiệp vụ/quyền; ngày mong muốn không mặc định là lịch hẹn đã chốt.

Phân biệt: “đã gửi đăng ký” ≠ “đã có lịch được xác nhận” ≠ “đã được bác sĩ chỉ định” ≠ “đã tiêm”. Đây là chuỗi trạng thái cần được trình bày riêng.
E. Học trong GitHub: chỉ đọc đúng file
- Vùng/file
- Việc cần quan sát

- apps/web/AGENTS.md
- Web là customer client gọi API thật; yêu cầu UX/loading/empty/error; quyền tại backend.

- apps/web/src/Registrations.jsx
- Tìm form, dữ liệu đăng ký, nơi gửi request và trạng thái kết quả; không cần thuộc từng dòng.

- apps/web/src/Schedule.jsx
- Nhìn cách lịch hẹn được hiển thị và phân biệt với đăng ký.

- apps/web/src/VaccinationHistory.jsx
- Quan sát thông tin lịch sử đã tiêm hiển thị cho khách hàng.

- apps/web/src/Payments.jsx
- Xem màn thanh toán hiện có; không gộp với tính năng payOS parked.

- apps/web/src/api/
- Xem cách React tổ chức client gọi endpoint, chọn đúng module khi cần.

- backend/src/TiemChung.Api/RegistrationEndpoints.cs
- Nhìn API tiếp nhận đăng ký; nếu thiếu nền, chỉ xem tên route và điều kiện.

- backend/src/TiemChung.Api/AppointmentEndpoints.cs
- Nhìn nơi API sở hữu logic lịch hẹn; không đồng nhất với preference.

F. Lời thuyết trình mẫu cho slide 1–6
Lời nói dưới đây là dàn ý diễn đạt tự nhiên. Cần thay hình, số liệu và minh chứng bằng artifact Word/runtime chính thức; luyện tốc độ 110–135 từ/phút, không đọc y nguyên như văn bản.
Slide 1 · Giới thiệu (20–30 giây)
Lời nói gợi ý: Kính thưa thầy cô, nhóm em trình bày khóa luận xây dựng hệ thống quản lý tiêm chủng tại trung tâm tiêm chủng. Đề tài hướng tới việc tổ chức dữ liệu và hỗ trợ các nghiệp vụ chính bằng một Website cho khách hàng, một phần mềm Windows cho nhân viên và hệ thống xử lý dùng chung.
Slide 2 · Lý do chọn đề tài (45–55 giây)
Lời nói gợi ý: Quá trình cung cấp dịch vụ tiêm chủng liên quan đến nhiều loại thông tin: hồ sơ khách hàng, người được tiêm, loại vắc xin, đăng ký, lịch hẹn, khám sàng lọc, thanh toán và theo dõi sau tiêm. Khi những hoạt động này được quản lý rời rạc, việc tra cứu và liên kết thông tin khó khăn. Nhóm lựa chọn bài toán để thiết kế một giải pháp hỗ trợ quản lý dữ liệu và các thao tác nghiệp vụ có liên quan, không thay thế việc quyết định chuyên môn y khoa.
Slide 3 · Mục tiêu và phạm vi (55–65 giây)
Lời nói gợi ý: Mục tiêu gồm khảo sát hiện trạng, phân tích yêu cầu, mô hình hóa nghiệp vụ, thiết kế cơ sở dữ liệu và giao diện, xây dựng API, triển khai Website cùng ứng dụng Windows và kiểm thử. Phạm vi khóa luận bám đề cương và trạng thái triển khai được chốt trên GitHub. Những nội dung như tích hợp provider thanh toán thực tế hoặc các hệ thống quản trị kho mở rộng cần được phân biệt với chức năng cốt lõi đã hoàn thành.
Slide 4 · Tác nhân và người dùng (35–45 giây)
Lời nói gợi ý: Về nghiệp vụ, các vai trò chính có khách hàng, lễ tân, bác sĩ, điều dưỡng, thu ngân và nhân viên kho. Mỗi vai trò tham gia các công việc khác nhau. Trong mô hình Use Case hệ thống, nhóm còn thể hiện quản trị viên và dịch vụ thư điện tử, vì đây là góc nhìn tương tác với phần mềm. Chính sự phân vai này là cơ sở thiết kế các chức năng và kiểm tra quyền.
Slide 5 · Bảy quy trình (65–75 giây)
Lời nói gợi ý: Nhóm xác định bảy quy trình cốt lõi: nhập vắc xin, quản lý hồ sơ, đăng ký dịch vụ, khám sàng lọc và chỉ định, thanh toán, ghi nhận tiêm và theo dõi sau tiêm, quản lý lịch và nhắc lịch. Chúng tạo thành chuỗi dữ liệu liên quan, nhưng không phải mọi bước đều diễn ra theo một thứ tự cứng. Trong hệ thống, từng nghiệp vụ được thể hiện qua các chức năng và ràng buộc dữ liệu tương ứng.
Slide 6 · Website khách hàng (65–75 giây)
Lời nói gợi ý: Website React giúp khách hàng xem thông tin vắc xin, tra cứu danh mục và đăng ký dịch vụ. Khi người dùng gửi yêu cầu, Web gọi ASP.NET Core API; API kiểm tra điều kiện và làm việc với SQL Server. Khách còn có các chức năng theo dõi phù hợp tài khoản và hồ sơ. Cần phân biệt thời gian mong muốn khi đăng ký với lịch hẹn đã được trung tâm xác nhận; Website không tự quyết định người dùng có đủ điều kiện tiêm hay không.
G. Bài tập thực hành ngày 13–14/11
Viết lại 7 quy trình nghiệp vụ từ trí nhớ; mô tả mỗi quy trình bằng đúng 1 câu.
Tự vẽ sơ đồ 4 ô: React Web và WPF → API → SQL Server; nói rõ Web không đọc SQL trực tiếp.
Mở apps/web/src/Registrations.jsx, chỉ ra bằng lời người dùng nhập gì, bấm gì, phần nào hiển thị kết quả; nếu máy chưa chạy, ghi CHƯA KIỂM RUNTIME.
Mở các trang Web thực tế để tìm 3 ảnh minh họa cho slide 6, che dữ liệu nhạy cảm.
Tập nói 6 slide trong 5 phút; ghi âm; người 2/3 hỏi chéo 5 câu bất kỳ.
H. 12 câu phản biện – kèm đáp án mẫu
## 1. Tại sao chọn đề tài quản lý tiêm chủng?
Gợi ý trả lời: Vì bài toán có nhiều dữ liệu liên quan xuyên suốt quá trình cung cấp dịch vụ. Hệ thống hỗ trợ tập trung, tra cứu và theo dõi, đúng phạm vi đề cương.
## 2. Phân biệt khách hàng và người được tiêm?
Gợi ý trả lời: Khách hàng là chủ thể đăng ký/quản lý hồ sơ; người được tiêm là đối tượng nhận dịch vụ. Một khách có thể quản lý hồ sơ người được tiêm theo quan hệ được hệ thống cho phép.
## 3. Yêu cầu chức năng là gì?
Gợi ý trả lời: Hành vi phần mềm phải đáp ứng, ví dụ tra cứu danh mục, đăng ký dịch vụ hoặc xem lịch.
## 4. Yêu cầu phi chức năng là gì?
Gợi ý trả lời: Đặc tính vận hành như dễ sử dụng, an toàn thông tin, khả năng thích ứng giao diện; không phải một nghiệp vụ riêng.
## 5. Business Use Case và System Use Case có giống nhau?
Gợi ý trả lời: Không. Cái thứ nhất mô tả hoạt động tổ chức; cái thứ hai mô tả tương tác với phần mềm theo ranh giới hệ thống.
## 6. Vì sao 7 quy trình nhưng 14 System UC?
Gợi ý trả lời: Hai cấp độ phân tích khác nhau. Quy trình là nhóm hoạt động nghiệp vụ; nhiều chức năng phần mềm có thể hỗ trợ cùng một quy trình và ngược lại.
## 7. Vì sao Web dùng React?
Gợi ý trả lời: React hỗ trợ xây UI theo thành phần và quản lý tương tác; trong dự án React là client khách hàng. Không phải vì React tự giải quyết bảo mật hoặc SQL.
## 8. Khách có được truy cập hồ sơ của người khác?
Gợi ý trả lời: Không được mặc định. API kiểm tra quan hệ sở hữu/hồ sơ được phép; UI không phải ranh giới bảo mật.
## 9. Khách chọn ngày giờ có nghĩa là có lịch hẹn?
Gợi ý trả lời: Không. Theo RF-05F.1, đó là preference/yêu cầu; LichHenTiem xác nhận được quản lý độc lập.
## 10. Catalog có quyết định vắc xin phù hợp cho từng người?
Gợi ý trả lời: Không. Thông tin Catalog/phác đồ tham khảo không thay thế sàng lọc và chỉ định cá nhân.
## 11. Đăng ký dịch vụ có đồng nghĩa đã thanh toán?
Gợi ý trả lời: Không; đó là các thông tin/trạng thái nghiệp vụ riêng có liên kết. Phải mô tả theo contract thực tế.
## 12. Nếu trang Web bị lỗi API thì sao?
Gợi ý trả lời: UI cần báo lỗi rõ ràng và có trạng thái tải lại theo thiết kế. Không được coi gửi request thất bại/không rõ kết quả là giao dịch thành công.
I. Phiếu tự đánh giá trước khi ngủ ngày 15/11
□ Tôi nói được 7 quy trình và 6 actor mà không nhìn tài liệu.
□ Tôi phân biệt được 7 quy trình với 14 UC.
□ Tôi nói đúng React → API → SQL, không cho Web viết DB.
□ Tôi phân biệt được đăng ký, preference và lịch xác nhận.
□ Tôi tìm được ít nhất 2 file React liên quan và giải thích được tác dụng.
□ Tôi tự nói 6 slide trong ≤5:15, không đọc nguyên văn.
□ Tôi trả lời đúng ít nhất 9/12 câu phản biện.
Nguồn học & điểm truy cập GitHub
- Nguồn
- Đọc để làm gì

- SCOPE.md trên main
- Trạng thái và phạm vi đã chốt. Ưu tiên hơn README khi thông tin tiến độ mâu thuẫn.

- AGENTS.md trên main
- Kiến trúc, nguyên tắc dữ liệu, clinical, security và cách đọc repo.

- Issue #182 / nhánh docs/thesis-r1-current-state
- Source of truth của báo cáo, 14 System UC và sơ đồ học thuật.

- apps/web/AGENTS.md; apps/web/src/Registrations.jsx
- Rule Web và một luồng khách đăng ký.

- apps/web/src/Schedule.jsx; VaccinationHistory.jsx
- Luồng xem lịch và lịch sử khách hàng.

- HUIT Chương 4 + 5 do nhóm cung cấp
- Phân biệt Business/System Use Case và actor.

Đường dẫn repo: https://github.com/datngo1203/KLCN-QuanLyTiemChung
Nhánh main lúc đối chiếu: 92d26778777909daeea200089f9f4461be25cc84. Nhánh thesis lúc đối chiếu: 23da2c015eecb1848486097cd2d483b67628db0c. Ngày đối chiếu: 12/11/2026.
Đề cương CNTT-KLCN090 và mẫu báo cáo HUIT do nhóm cung cấp: dùng để đối chiếu mục tiêu, yêu cầu và thang điểm học thuật.
