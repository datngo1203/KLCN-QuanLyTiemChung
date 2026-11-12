# Người 3 — Backend, CSDL và kiểm thử

> Bản đọc trực tiếp trên GitHub, chuyển từ tài liệu Word ôn tập ngày 12/11/2026. Đây là tài liệu học; khi có xung đột, đối chiếu SCOPE.md và implementation hiện hành.

Người 3 – API, SQL, thiết kế và test

KLCN090  |  TÀI LIỆU ÔN TẬP BẢO VỆ  |  13–16/11/2026

**BỘ TÀI LIỆU 03**
NGƯỜI 3 · BACKEND & THIẾT KẾ
Học từ số 0 • Nói slide 13–17 • 16 câu phản biện có đáp án

- Đối tượng
- Thành viên phụ trách kiến trúc, ASP.NET Core API, SQL Server, UML và kiểm thử

- Lịch học
- 13–16 tháng 11 năm 2026

- Buổi bảo vệ
- 20 phút trình bày + 20 phút hỏi đáp

Nguồn:  SCOPE main, backend/AGENTS.md, backend source, đề cương và báo cáo HUIT.
Lưu ý:  Đây là tài liệu ôn tập dựa trên bằng chứng của repo và đề cương; không thay thế việc kiểm tra demo thực tế trước ngày bảo vệ.

A. Mục tiêu học
Giải thích hệ thống có 2 client, 1 API, 1 cơ sở dữ liệu; hiểu “modular monolith” ở mức cử nhân.
Biết đi theo một request: Web/WPF → endpoint → xử lý kiểm quyền/validation → SQL/EF Core → response.
Giải thích PK/FK, quan hệ 1–n, transaction, authorization và concurrency bằng ví dụ dự án.
Phân biệt Class Diagram mức phân tích, mức thiết kế, lược đồ dữ liệu và Sequence Diagram; không đánh tráo chúng.
Nói chính xác bằng chứng test/CI so với runtime demo; nêu giới hạn S8/PAY-R1.
Trình bày slide 13–17 trong khoảng 6 phút và hỗ trợ giải đáp kỹ thuật cả nhóm.
B. Các khái niệm kỹ thuật không thể bỏ qua
- Từ
- Giải thích cho người mất gốc
- Liên hệ

- ASP.NET Core
- Framework .NET để xây API trên server.
- backend/src/TiemChung.Api/

- C#
- Ngôn ngữ dùng viết nhiều phần backend và Windows.
- Các tệp .cs.

- HTTP request/response
- Client gửi yêu cầu; server trả trạng thái và dữ liệu.
- Web/WPF gọi API.

- Endpoint
- Đường vào API cho một hành động/nhóm dữ liệu.
- RegistrationEndpoints.cs, AppointmentEndpoints.cs...

- EF Core / DbContext
- Công cụ ánh xạ entity với SQL; DbContext quản lý tập dữ liệu và thao tác.
- TiemChungDbContext.cs.

- Migration
- Thay đổi cấu trúc DB theo phiên bản có kiểm soát.
- Infrastructure/Migrations/

- PK/FK
- Khóa chính định danh; khóa ngoại liên kết quan hệ.
- Khách hàng ↔ đăng ký ↔ lịch ↔ chỉ định ↔ mũi.

- Transaction
- Các bước ghi quan trọng phải cùng thành công hoặc rollback.
- Ghi mũi, trừ tồn, biến động, audit.

- Authorization
- Kiểm quyền truy cập thao tác.
- API kiểm actor/capability và ownership.

- Idempotency / reconcile
- Chống lặp tác vụ; đối chiếu trạng thái nếu kết quả chưa rõ.
- Ghi mũi/nhắc lịch/đổi lịch.

- CI
- Bộ kiểm tra tự động khi làm việc với repo.
- Không thay nghiệm thu runtime.

- Modular monolith
- Một ứng dụng server có các phần chức năng tách ranh giới nội bộ.
- Dự án không cần microservices.

C. Kiến trúc cụ thể
- Một yêu cầu đăng ký từ Website
(1) UI React thu dữ liệu và gọi endpoint; (2) ASP.NET Core xác định tài khoản và quyền thao tác, validate dữ liệu/nghiệp vụ; (3) backend thao tác các entity liên quan qua EF Core/SQL Server theo transaction nếu cần; (4) API trả HTTP status/dữ liệu; (5) React cập nhật giao diện. Với WPF cũng tương tự; sự khác nhau chính là actor và UX.

Backend là “source of truth” cho quyền, tiền, tồn kho, clinical validation và mutation. Một thành phần UI hiển thị nút không có nghĩa nút đó đủ quyền/đủ dữ liệu để ghi vào DB.
D. CSDL: học đủ để giải thích sơ đồ lớp và quan hệ
Học theo chuỗi domain, không học từng bảng theo thứ tự chữ cái. Các tên như KhachHang, NguoiDuocTiem, DangKyDichVu, LichHenTiem, PhieuKhamSangLoc, ChiDinhTiem, MuiTiem, TheoDoiSauTiem, VacXin, LoVacXin, ThanhToan là các khái niệm được ghi nhận trong checkpoint class diagram thesis. Phải đối chiếu từng quan hệ cụ thể trong TiemChungDbContext và model đang dùng nếu đem lên slide.
- Cặp dữ liệu
- Mối quan hệ diễn giải mức học tập

- Khách hàng – Người được tiêm
- Một khách hàng có thể gắn nhiều hồ sơ người được tiêm; dùng quan hệ phù hợp thay vì lặp tên trong từng nghiệp vụ.

- Đăng ký – Lịch hẹn
- Một đăng ký có thể liên quan các lịch; FK có thể không bắt buộc theo mô hình hiện hành, cần phân biệt với “đã có lịch xác nhận”.

- Lịch – Phiếu khám
- Lịch là nguồn liên hệ; phiếu khám chứa thông tin chuyên môn được ghi nhận.

- Phiếu khám – Chỉ định
- Quyết định chuyên môn được gắn với bằng chứng khám.

- Chỉ định – Mũi tiêm
- Không thể đồng nhất chỉ định dự kiến với sự kiện đã tiêm thực tế.

- Mũi – Lô – Tồn
- Sự kiện tiêm phải liên kết lô dùng thực, ảnh hưởng số tồn và biến động.

- Mũi – Theo dõi sau tiêm
- Thông tin sau tiêm được lưu riêng; không tự sinh.

- Đăng ký – Thanh toán
- Trạng thái tiền liên hệ nguồn đăng ký, không có nghĩa tất cả payment provider đã được tích hợp.

Các multiplicity cụ thể và optional FK phải lấy đúng từ sơ đồ đã khóa trong Word và EF Core mapping; không tự phát biểu “1–1” chỉ vì nhìn tên bảng. Sơ đồ class analysis và class design không giống ERD vật lý.
E. Bảo mật, tính nhất quán và lỗi
1) Auth và authorization
Authentication hỏi “bạn là ai?”; Authorization hỏi “bạn có được thực hiện thao tác này không?”. Backend kiểm cả customer ownership và employee role/capability. Admin không mặc định vượt mọi gate chuyên môn, thu ngân, kho hoặc lễ tân nếu contract không cho phép.
2) Transaction và concurrency
Ví dụ ghi mũi: nếu ghi mũi thành công nhưng không trừ tồn, hệ thống sai. Vì vậy nhiều thay đổi quan trọng được xử lý trong transaction; concurrency tránh hai request cùng dùng số tồn cuối. Đừng nói “có transaction là không bao giờ lỗi”; cần mô tả cách hệ thống xử lý xung đột/rollback theo code và test.
3) Unknown outcome
Nếu API đã ghi thành công nhưng mạng ngắt trước khi client nhận response, UI không biết chắc kết quả. Hướng xử lý theo repo là GET/reconcile trạng thái trước khi retry; không lặp POST tùy tiện khiến ghi trùng.
4) Đặc thù y tế
Hệ thống không tự đưa ra eligibility/chỉ định; cần dữ liệu clinical được người có chuyên môn ghi nhận. API phải fail-closed nếu thiếu bằng chứng cần thiết hoặc không thỏa điều kiện kho/tiền/role. Trình bày đây là ràng buộc của phần mềm, không phải một quy định y khoa tự suy.
F. Học đúng file ở repo – thứ tự đọc ngắn nhất
- Bước
- File
- Mục đích

- 1
- backend/AGENTS.md
- Các luật auth, SQL, migration, concurrency, medical source-of-truth.

- 2
- backend/src/TiemChung.Api/Program.cs
- Nhìn API được cấu hình và ghép nhóm endpoint như thế nào; không cần thuộc toàn bộ.

- 3
- backend/src/TiemChung.Api/RegistrationEndpoints.cs
- Chọn một request làm ví dụ client → API.

- 4
- backend/src/TiemChung.Api/VaccinationEndpoints.cs
- Chọn mutation quan trọng để tìm các gate.

- 5
- backend/src/TiemChung.Infrastructure/Data/TiemChungDbContext.cs
- Tìm entity, key, FK, relation và constraint liên quan.

- 6
- backend/src/TiemChung.Domain/Entities/
- Nhìn lớp dữ liệu nghiệp vụ (không đọc tất cả).

- 7
- backend/tests/TiemChung.Tests/
- Xem test tương ứng với endpoint đang trình bày.

- 8
- backend/contracts/README.md
- Chỉ đọc section contract đúng chức năng đã chọn, không đọc toàn file.

G. Hai bài luyện truy vết code (không yêu cầu sửa code)
Bài 1 – Đọc đường đi: mở apps/web/src/Registrations.jsx, tìm lời gọi API; chuyển đến RegistrationEndpoints.cs; ghi bằng lời dữ liệu vào và ra. Nếu không hiểu code, đánh dấu chỗ chưa rõ và hỏi lại, không suy đoán.
Bài 2 – Ghi mũi: từ VaccinationView.xaml → VaccinationViewModel.cs → VaccinationEndpoints.cs → DbContext. Tìm tối thiểu các từ khóa hoặc khối code liên quan đến quyền, chỉ định, tồn kho, transaction. Viết một sơ đồ chuỗi 4–6 mũi tên.
Acceptance tối thiểu: không cần nhớ tên method, chỉ cần nói được thành phần nào gọi thành phần nào, ai kiểm quyền, ai ghi SQL, và điều gì xảy ra khi điều kiện thất bại.
H. Học UML cho phần báo cáo
- Sơ đồ
- Ý nghĩa
- Lỗi thường gặp

- Use Case
- Ai sử dụng chức năng nào trong ranh giới phần mềm.
- Nhầm thành sơ đồ trình tự các bước.

- Activity
- Các bước xử lý và các nhánh quyết định.
- Bỏ mất nhánh lỗi/điều kiện.

- Class phân tích
- Các lớp/khái niệm domain và quan hệ ở mức phân tích.
- Biến mọi lớp thành bảng vật lý.

- Class thiết kế
- Tinh chế thuộc tính, kiểu dữ liệu, quan hệ theo thiết kế đã chốt.
- Tự invent method/aggregation/kiểu visibility.

- CSDL/quan hệ
- Các bảng, khóa và ràng buộc lưu trữ.
- Dùng multiplicity không đúng FK.

- Sequence
- Tương tác theo thời gian giữa actor/UI/API/database.
- Không khớp luồng thực tế hoặc contract.

Báo cáo thesis hiện đã chốt mô hình tổng quan 14 System UC. 14 sequence diagram có trong checkpoint Word gần nhất của nhánh thesis, nhưng cần mở đúng file Word final để khẳng định vị trí/độ rõ mỗi hình. Tránh dùng hình 18/21 UC trước đó.
I. Phân biệt test – CI – runtime – nghiệm thu
- Khái niệm
- Bằng chứng chứng minh
- Không chứng minh

- Unit test
- Hàm/lớp chạy đúng các case đã viết.
- UI WPF/Web nhìn và thao tác tốt trên máy.

- Integration SQL/API
- Endpoint, ràng buộc DB, trạng thái sau mutation.
- Cả ứng dụng full-shell chạy ổn trong mọi ngữ cảnh.

- CI xanh
- Các job cấu hình trên SHA được chạy và PASS.
- Demo ngày bảo vệ đang chạy được.

- Browser smoke
- Web tải và thao tác trên viewport theo test.
- WPF native đã PASS.

- WPF native smoke
- UI Windows chạy trên full shell và thao tác cần thiết.
- Mọi tình huống đồng thời đã được mô phỏng thực tế.

- S8 e2e
- Liên thông end-to-end cuối của hệ thống.
- SCOPE vẫn ghi NOT STARTED trong trạng thái tham khảo; không được nhận là DONE.

J. Lời thuyết trình mẫu cho slide 13–17
Slide 13 · Kiến trúc hệ thống (55–65 giây)
Lời nói gợi ý: Kiến trúc gồm hai ứng dụng giao diện: React Website cho khách hàng và Windows WPF cho nhân viên. Cả hai gọi chung một ASP.NET Core API, rồi API làm việc với SQL Server. Cách tổ chức này giúp dữ liệu nghiệp vụ có một nguồn quyết định chung; quyền, tiền, tồn kho, tính hợp lệ của thông tin tiêm được kiểm tra ở API, thay vì mỗi giao diện tự đưa ra quy tắc riêng.
Slide 14 · API và quyền (65–75 giây)
Lời nói gợi ý: Backend được phát triển bằng ASP.NET Core, tổ chức các endpoint theo nghiệp vụ như hồ sơ, đăng ký, lịch hẹn, khám sàng lọc, ghi mũi và kho. Khi có yêu cầu, API kiểm tra định danh, quyền và dữ liệu đầu vào trước khi thao tác SQL. Ví dụ, điều dưỡng không thể ghi mũi chỉ nhờ nhìn thấy một nút trên giao diện: server còn phải kiểm tra chỉ định, nguồn, thanh toán, lô và tồn kho. Đây là lớp kiểm soát nghiệp vụ quan trọng của hệ thống.
Slide 15 · Thiết kế dữ liệu (75–90 giây)
Lời nói gợi ý: Cơ sở dữ liệu được thiết kế quanh các đối tượng như khách hàng, người được tiêm, đăng ký dịch vụ, lịch hẹn, phiếu khám, chỉ định, mũi tiêm, theo dõi sau tiêm, thanh toán và lô vắc xin. Các khóa và mối quan hệ giúp truy vết một hoạt động tiêm từ nguồn đăng ký đến sự kiện thực tế. Với những thao tác quan trọng như ghi mũi và trừ tồn, backend phải giữ nhất quán bằng transaction và kiểm soát tranh chấp. Sơ đồ trên slide là bản tóm tắt; chi tiết nằm trong báo cáo và mã nguồn hiện hành.
Slide 16 · Kiểm thử (65–75 giây)
Lời nói gợi ý: Nhóm sử dụng kiểm thử theo nhiều mức: kiểm thử xử lý nghiệp vụ ở backend, kiểm thử liên quan SQL/API, kiểm thử trên Web và Windows, và CI theo commit. Với phần giao diện, build hay CI xanh không tự chứng minh runtime đạt; cần có browser hoặc native smoke. Trong bài bảo vệ, nhóm chỉ đưa ra các số liệu kiểm thử và bằng chứng đã có ở đúng phiên bản code, đồng thời không coi một môi trường demo chưa kiểm tra là PASS.
Slide 17 · Kết quả và giới hạn (65–75 giây)
Lời nói gợi ý: Các module quản lý cốt lõi từ nền dữ liệu, tài khoản, danh mục, hồ sơ, đăng ký/thanh toán lõi, lịch hẹn, khám/tiêm, ưu đãi và báo cáo đã được tích hợp theo trạng thái main. Tuy nhiên, tích hợp cuối S8 chưa được xác nhận hoàn thành trong SCOPE; thanh toán với nhà cung cấp thật và một số mở rộng kho/thông báo chưa nghiệm thu trên main. Đây là giới hạn được nhóm công khai thay vì trình bày vượt quá kết quả thực tế. Hướng phát triển là hoàn thiện các bước kiểm thử và tích hợp còn lại.
K. 16 câu phản biện – kèm đáp án mẫu
## 1. API là gì?
Gợi ý trả lời: Là giao diện giao tiếp giữa client và server; dự án dùng ASP.NET Core API để tiếp nhận request, kiểm quyền, xử lý nghiệp vụ và truy cập SQL.
## 2. Vì sao không để WPF đọc SQL thẳng?
Gợi ý trả lời: Để tránh hai client có logic khác nhau và bảo vệ nguồn dữ liệu/validation. API làm nơi quyết định thống nhất.
## 3. ASP.NET Core với React khác nhau ở đâu?
Gợi ý trả lời: React tạo UI phía Web; ASP.NET Core là backend xử lý request/logic/dữ liệu trên server.
## 4. Modular monolith là gì?
Gợi ý trả lời: Một backend ứng dụng được chia các phần chức năng trong cùng tiến trình/triển khai; không tách thành hệ microservices.
## 5. DbContext dùng để làm gì?
Gợi ý trả lời: Giúp EF Core ánh xạ/điều phối entity và thao tác trên SQL Server theo mô hình hiện hành.
## 6. Khóa chính và khóa ngoại là gì?
Gợi ý trả lời: PK định danh duy nhất bản ghi; FK nối bản ghi với đối tượng nguồn ở bảng khác.
## 7. Vì sao không gộp tất cả dữ liệu vào 1 bảng?
Gợi ý trả lời: Dữ liệu có nhiều thực thể, thuộc tính và quan hệ khác nhau; tách theo mô hình để hạn chế lặp/khó đảm bảo toàn vẹn.
## 8. Transaction là gì?
Gợi ý trả lời: Nhóm thao tác dữ liệu được xử lý nguyên tử theo contract; ví dụ ghi mũi/trừ tồn/biến động/audit cùng thành công hoặc rollback.
## 9. Hai request đồng thời cùng dùng lô còn một liều thì sao?
Gợi ý trả lời: Backend phải kiểm soát race/concurrency và từ chối một trường hợp không hợp lệ, không cho trừ âm hoặc ghi mũi trùng.
## 10. Authentication và authorization khác gì?
Gợi ý trả lời: Authentication xác minh người dùng; authorization xác định hành động/tài nguyên họ được phép thao tác.
## 11. Tại sao Admin không làm mọi nghiệp vụ?
Gợi ý trả lời: Quyền chuyên môn theo role/capability được API kiểm soát; Admin không tự bypass các gate clinical/cashier/kho khi contract không cấp.
## 12. Nếu mất mạng sau khi ghi POST thì sao?
Gợi ý trả lời: Đối chiếu state hiện hành bằng GET trước khi retry, vì server có thể đã commit còn client mất response.
## 13. Class Diagram phân tích khác mức thiết kế?
Gợi ý trả lời: Phân tích biểu diễn khái niệm domain; thiết kế tinh chế kiểu dữ liệu, thuộc tính, quan hệ theo hướng triển khai. Nó khác sơ đồ CSDL vật lý.
## 14. Vì sao CI xanh không có nghĩa demo chạy được?
Gợi ý trả lời: CI chỉ xác nhận job được cấu hình trên đúng commit; WPF full-shell, UI browser và dữ liệu demo còn cần runtime smoke.
## 15. Hệ thống đã thanh toán payOS production chưa?
Gợi ý trả lời: Không tuyên bố vậy: PAY-R1 vẫn parked trên Draft PR và chưa real-provider acceptance trong trạng thái kiểm tra.
## 16. Nếu hội đồng hỏi chức năng chưa làm thì sao?
Gợi ý trả lời: Nêu rõ DONE/ACTIVE/NEXT/PARKED theo SCOPE, giải thích phạm vi đồ án, không bịa tính năng hoàn thành.
L. Bài tập bắt buộc trước tối 15/11
Vẽ sơ đồ 4 thành phần và giải thích trong 45 giây.
Tìm RegistrationEndpoints.cs và nói được request vào → kiểm tra → thao tác DB → response.
Tìm VaccinationEndpoints.cs và liệt kê bằng lời các gate chính, không cần thuộc phương thức.
Mở DbContext và tìm hai quan hệ domain, đọc đúng optional/required.
Phân biệt 14 System UC với số lượng màn hình/endpoint.
Ghi ba bằng chứng test cụ thể từ PR/CI đúng SHA nếu muốn đưa số liệu trên slide.
Nói 5 slide đúng 6 phút, hỗ trợ Q&A cho cả nhóm.
Nguồn học & điểm truy cập GitHub
- Nguồn
- Đọc để làm gì

- SCOPE.md trên main
- Trạng thái và phạm vi đã chốt. Ưu tiên hơn README khi thông tin tiến độ mâu thuẫn.

- AGENTS.md trên main
- Kiến trúc, nguyên tắc dữ liệu, clinical, security và cách đọc repo.

- Issue #182 / nhánh docs/thesis-r1-current-state
- Source of truth của báo cáo, 14 System UC và sơ đồ học thuật.

- backend/AGENTS.md; backend/src/TiemChung.Api/
- Quyền và các endpoint xử lý nghiệp vụ.

- backend/src/TiemChung.Infrastructure/Data/TiemChungDbContext.cs
- Quan hệ và EF Core mapping.

- backend/tests/TiemChung.Tests/
- Ví dụ kiểm thử thực tế.

- HUIT Chương 6, 7 do nhóm cung cấp
- Chuẩn thuật ngữ class diagram và thiết kế lớp.

Đường dẫn repo: https://github.com/datngo1203/KLCN-QuanLyTiemChung
Nhánh main lúc đối chiếu: 92d26778777909daeea200089f9f4461be25cc84. Nhánh thesis lúc đối chiếu: 23da2c015eecb1848486097cd2d483b67628db0c. Ngày đối chiếu: 12/11/2026.
Đề cương CNTT-KLCN090 và mẫu báo cáo HUIT do nhóm cung cấp: dùng để đối chiếu mục tiêu, yêu cầu và thang điểm học thuật.
