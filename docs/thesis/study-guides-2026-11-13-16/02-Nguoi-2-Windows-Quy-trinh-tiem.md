# Người 2 — Windows và quy trình tiêm

> Bản đọc trực tiếp trên GitHub, chuyển từ tài liệu Word ôn tập ngày 12/11/2026. Đây là tài liệu học; khi có xung đột, đối chiếu SCOPE.md và implementation hiện hành.

Người 2 – Windows và quy trình tiêm

KLCN090  |  TÀI LIỆU ÔN TẬP BẢO VỆ  |  13–16/11/2026

**BỘ TÀI LIỆU 02**
NGƯỜI 2 · WINDOWS & QUY TRÌNH TIÊM
Học từ số 0 • Nói slide 7–12 • 14 câu phản biện có đáp án

- Đối tượng
- Thành viên phụ trách WPF, lịch hẹn, sàng lọc, chỉ định, ghi tiêm, theo dõi và demo

- Lịch học
- 13–16 tháng 11 năm 2026

- Buổi bảo vệ
- 20 phút trình bày + 20 phút hỏi đáp

Nguồn:  SCOPE main, apps/windows/AGENTS.md, README và các endpoint/ViewsV2 liên quan.
Lưu ý:  Đây là tài liệu ôn tập dựa trên bằng chứng của repo và đề cương; không thay thế việc kiểm tra demo thực tế trước ngày bảo vệ.

A. Mục tiêu học
Nói được WPF là gì, nhân viên nào dùng từng chức năng và vì sao cần phân quyền.
Giải thích được chuỗi: hồ sơ/đăng ký → lịch hẹn → khám sàng lọc/chỉ định → điều kiện thanh toán/tồn → ghi mũi → theo dõi sau tiêm.
Biết rằng chuyên môn được bác sĩ quyết định, hệ thống không tự cấp chỉ định hoặc tự phán đoán đủ điều kiện.
Biết phân biệt thao tác ghi mũi với theo dõi sau tiêm và với kho.
Chuẩn bị kịch bản demo WPF chỉ với dữ liệu thật/đã có và nói slide 7–12 khoảng 6 phút.
B. 8 khái niệm phải hiểu trước khi mở Windows
- Khái niệm
- Giải thích

- WPF
- Windows Presentation Foundation: công nghệ xây giao diện ứng dụng Windows của Microsoft; trong repo giao diện WPF dùng XAML.

- XAML
- Tệp mô tả bố cục/điều khiển của giao diện; code-behind hoặc ViewModel hỗ trợ hành vi.

- View/ViewModel
- View là giao diện; ViewModel giữ trạng thái, dữ liệu và thao tác UI theo kiến trúc hiện hành.

- Lễ tân
- Tiếp nhận và làm việc với hồ sơ, đăng ký, lịch hẹn theo quyền.

- Bác sĩ
- Chịu trách nhiệm nghiệp vụ khám sàng lọc/chỉ định; phần mềm không tự thay thế.

- Điều dưỡng
- Ghi nhận mũi tiêm đã thực hiện và theo dõi sau tiêm theo role.

- Lô/tồn kho
- Dữ liệu về lô vắc xin, hạn, trạng thái và số lượng có thể sử dụng theo logic API.

- Audit
- Dấu vết nghiệp vụ/đối tượng/thời điểm được ghi để đối chiếu các thay đổi quan trọng.

C. Quy trình end-to-end phải kể được bằng lời
- Chuỗi hiểu nhanh – không phải thứ tự cứng cho mọi ca
Hồ sơ + đăng ký dịch vụ tạo nguồn dữ liệu → lịch hẹn được quản lý → bác sĩ ghi khám sàng lọc/chỉ định khi đủ căn cứ → điều dưỡng chọn chỉ định và lô phù hợp, API kiểm quyền/nguồn/clinical/tiền/tồn/hạn → ghi nhận mũi tiêm thực tế → theo dõi sau tiêm được ghi nhận riêng. Nhắc lịch và kho liên quan theo mốc/điều kiện của từng nghiệp vụ.

1) Lịch hẹn
Lễ tân có quyền theo dõi/tạo/quản lý lịch trên Windows theo contract. Lịch phải liên kết nguồn đăng ký/hồ sơ phù hợp; thời gian khách mong muốn là dữ liệu gợi ý, không tự xác nhận. Tính năng đổi/hủy lịch và email liên quan đã có lát nâng cấp, nhưng chỉ mô tả trạng thái nào được xác minh runtime.
2) Khám sàng lọc và chỉ định
Bác sĩ thực hiện tác nghiệp chuyên môn và ghi nhận kết luận/chỉ định. Hệ thống lưu dữ liệu và kiểm bằng chứng liên quan; không sinh kết luận đủ điều kiện tự động. Nếu thiếu nguồn/chỉ định hợp lệ, không được tạo “dữ liệu giả” để đi tiếp demo.
3) Thanh toán và điều kiện ghi mũi
S4 có thanh toán lõi qua API/SQL với nhãn mô phỏng theo phạm vi đã nghiệm thu. Trạng thái thanh toán liên quan là một trong các điều kiện ghi mũi. PAY-R1 payOS provider thực tế chưa phải capability nghiệm thu trên main theo SCOPE snapshot; không quảng bá nó như tính năng hoàn chỉnh.
4) Ghi nhận mũi tiêm
Theo README mục S5.5: chỉ Điều dưỡng Active mới thực hiện ở A06; chọn chỉ định có nguồn từ bác sĩ, đối chiếu người tiêm/vắc xin, chọn lô SQL, nhập vị trí thực tế, xác nhận mũi đã thực hiện. API kiểm lại chỉ định, nguồn, tiền, lô/hạn/tồn/quyền. Ghi mũi + trừ tồn + biến động + audit dùng transaction nhất quán và chống trùng/tranh chấp.
5) Theo dõi sau tiêm
Theo README S5.6: điều dưỡng chọn một mũi đã tiêm chưa có theo dõi, nhập tình trạng/mức độ/xử trí thực tế và lưu. Đây là bản ghi khác ghi mũi; mỗi mũi tối đa một bản ghi trong phạm vi đã chốt. Không mô tả nó như hệ thống tự xác định an toàn/cho về hoặc đồng hồ theo dõi lâm sàng.
6) Nhắc lịch
SCOPE main ghi RF-05E email thật trong Development và RF-05G tự động nhắc lịch 48/24 giờ trước, liên hệ sau 3/7 ngày theo lịch hiện hành; có revalidation và lịch sử. Điều đó không đồng nghĩa đã có nhà cung cấp SMS thật. #173 NOTIFY-R1 vẫn parked.
7) Kho
X1 đọc lô/tồn; X2.1 tiếp nhận một lô mới; X2.2.1 xem lịch sử nhập đã hoàn thành. Không nói có đầy đủ ERP nhà cung cấp/phiếu nhập nhiều dòng; X2.2.2 nâng cao parked. Không cần nhân viên kho xuất phát cho từng ca nếu vắc xin đã ở khu vực tiêm theo trường hợp thực tế.
D. Hiểu đúng dữ liệu và trạng thái
- Dữ liệu
- Sự thật cần nhớ

- Đăng ký dịch vụ
- Nguồn khách chọn dịch vụ; không mặc định đã có lịch hoặc đã thanh toán.

- Lịch hẹn
- Ngày giờ và trạng thái được trung tâm quản lý/xác nhận riêng.

- Phiếu khám / chỉ định
- Bằng chứng của hành vi chuyên môn; không do AI hay phần mềm tự quyết.

- Mũi tiêm
- Bản ghi sự kiện đã thực hiện, khác dự kiến lịch hẹn.

- Theo dõi sau tiêm
- Bản ghi riêng gắn với mũi đã tiêm, không tự sinh nếu chưa được nhập.

- Lô/tồn
- Nguồn vật tư có điều kiện trạng thái, hạn và số lượng; API cần kiểm lại khi ghi.

- Nhắc lịch/email
- Sự kiện gửi/log có lịch sử; khi đổi/hủy không được sửa lịch sử gửi cũ.

E. Học trong GitHub: xem giao diện rồi mới xem API
- File/vùng
- Cần tìm ra

- apps/windows/AGENTS.md
- WPF gọi API thật; phải kiểm UI full-shell, quyền và state.

- apps/windows/ViewsV2/AppointmentView.xaml
- Màn lịch hẹn: dữ liệu, nút và trạng thái.

- apps/windows/ViewsV2/ScreeningView.xaml.cs
- Màn khám sàng lọc: người thực hiện, dữ liệu hiển thị.

- apps/windows/ViewsV2/VaccinationView.xaml
- Màn ghi mũi: chọn chỉ định, lô, xác nhận.

- apps/windows/ViewsV2/AftercareView.xaml
- Màn theo dõi sau tiêm: dữ liệu đầu vào và kết quả.

- apps/windows/ViewsV2/InventoryView.xaml.cs
- Màn kho: xem lô/tồn và tính năng tiếp nhận đã chốt.

- backend/src/TiemChung.Api/ClinicalEndpoints.cs
- API xử lý nghiệp vụ khám/chỉ định.

- backend/src/TiemChung.Api/VaccinationEndpoints.cs
- API xử lý ghi mũi: các gate nguồn/tiền/kho/clinical.

- backend/src/TiemChung.Api/AftercareEndpoints.cs
- API lưu theo dõi sau tiêm.

- backend/src/TiemChung.Api/ReminderEndpoints.cs
- API liên quan nhắc lịch; chỉ nói bằng chứng được xác minh.

F. Lời thuyết trình mẫu cho slide 7–12
Nên dùng 1 hình mỗi slide, ưu tiên ảnh giao diện thực hoặc sơ đồ rút gọn. Không chèn ảnh y tế minh họa mà không có nguồn/quyền sử dụng.
Slide 7 · Phần mềm Windows (50–60 giây)
Lời nói gợi ý: Bên cạnh Website dành cho khách hàng, nhóm xây dựng ứng dụng Windows bằng WPF để hỗ trợ các thao tác nội bộ của trung tâm. Giao diện tổ chức theo vai trò như lễ tân, bác sĩ, điều dưỡng, thu ngân và nhân viên kho. Ứng dụng không thao tác SQL trực tiếp mà gửi yêu cầu qua API, nơi kiểm tra đăng nhập, quyền và các điều kiện nghiệp vụ.
Slide 8 · Tiếp nhận và lịch hẹn (55–65 giây)
Lời nói gợi ý: Khi khách có hồ sơ và đăng ký dịch vụ, lễ tân sử dụng màn quản lý để đối chiếu thông tin và xử lý lịch hẹn. Hệ thống phân biệt đăng ký dịch vụ với lịch đã xác nhận. Điều này quan trọng vì ngày giờ khách mong muốn có thể được dùng làm gợi ý, nhưng dữ liệu lịch hẹn thực tế phải được quản lý riêng và tuân theo các kiểm tra ở API.
Slide 9 · Khám sàng lọc và chỉ định (65–75 giây)
Lời nói gợi ý: Trong nghiệp vụ tiêm chủng, khám sàng lọc là bước chuyên môn không thể thay bằng việc khách tự chọn vắc xin trên Website. Bác sĩ ghi nhận kết quả đánh giá và chỉ định. Hệ thống có nhiệm vụ lưu bằng chứng và kiểm tra mối liên hệ với hồ sơ, lịch và dịch vụ; không tự chẩn đoán hoặc tự quyết đủ điều kiện tiêm. Đây là một giới hạn chủ động của đề tài nhằm tránh nhầm phần mềm quản lý với phần mềm quyết định y khoa.
Slide 10 · Thanh toán (45–55 giây)
Lời nói gợi ý: Thanh toán là một phần nguồn nghiệp vụ có thể ảnh hưởng đến khả năng ghi nhận mũi tiêm. Trong phạm vi đã nghiệm thu, hệ thống có chức năng thanh toán lõi qua API và SQL Server; một số phần hiển thị có nhãn mô phỏng. Việc tích hợp payOS với nhà cung cấp thực tế thuộc nhánh riêng chưa phải kết quả hoàn tất trên main trong trạng thái kiểm tra, nên nhóm không trình bày vượt phạm vi.
Slide 11 · Ghi mũi và theo dõi sau tiêm (75–85 giây)
Lời nói gợi ý: Khi điều dưỡng thực hiện thao tác ghi mũi, hệ thống yêu cầu chỉ định và nguồn lịch/đăng ký thích hợp, kiểm tra dữ liệu vắc xin, lô, hạn dùng, tồn kho, thanh toán và quyền qua API. Khi thành công, ghi mũi, cập nhật tồn kho và lịch sử biến động phải nhất quán. Theo dõi sau tiêm là bản ghi nghiệp vụ riêng do điều dưỡng nhập, không được tự tạo để hoàn thành kịch bản. Đây là ví dụ cho việc hệ thống vừa quản lý dữ liệu vừa bảo đảm tính nhất quán.
Slide 12 · Nhắc lịch và kho (45–55 giây)
Lời nói gợi ý: Hệ thống có các chức năng về lịch, nhắc lịch và lịch sử liên hệ bằng email theo những lát đã hoàn thành. Ở kho, các chức năng cốt lõi gồm đọc lô/tồn, nhập một lô mới và lịch sử tiếp nhận. Nhóm không coi đây là một hệ thống quản trị kho đầy đủ. Các giới hạn được nêu rõ trong SCOPE và sẽ là hướng hoàn thiện nếu đề tài tiếp tục phát triển.
G. Demo Windows an toàn trong 2 phút
Trước buổi bảo vệ 1 ngày, mở đúng Compose project và chạy VAC-Check.cmd để xác nhận SQL/API/Web/WPF khỏe; chỉ dùng tài khoản Development có quyền. Không reset DB.
Chuẩn bị trước hồ sơ/lịch hoặc màn hình dữ liệu thật để mở đúng vai trò; không cố chạy chuỗi tạo nguồn lâm sàng giả trong 2 phút.
Trong demo, đi từ giao diện nhân viên đến một màn trọng tâm và nói rõ dữ liệu đến từ API/SQL. Nếu có đủ dữ liệu thật, chỉ trình diễn thao tác đã rehearsal.
Quay một video dự phòng 120 giây từ cùng build/dataset; tránh lộ tên thật, mật khẩu hoặc token. Nếu demo có lỗi, dùng video có nhãn dự phòng.
H. 14 câu phản biện – kèm đáp án mẫu
## 1. WPF là gì và vì sao chọn WPF?
Gợi ý trả lời: WPF là nền tảng giao diện ứng dụng Windows. Dự án dùng để phục vụ thao tác nghiệp vụ nhân viên; đây là client gọi ASP.NET Core API, không đọc SQL trực tiếp.
## 2. Vì sao mỗi nhân viên không có cùng quyền?
Gợi ý trả lời: Các công việc chuyên môn và giao dịch có trách nhiệm khác nhau. API kiểm tra actor/capability để ngăn người không đủ quyền thực hiện thao tác.
## 3. Lịch hẹn và đăng ký dịch vụ có giống nhau?
Gợi ý trả lời: Không. Đăng ký ghi nhận dịch vụ; lịch hẹn có nguồn và trạng thái xác nhận riêng. Ngày giờ mong muốn không phải lịch đã chốt.
## 4. Bác sĩ làm gì trong hệ thống?
Gợi ý trả lời: Ghi nhận nghiệp vụ khám sàng lọc/chỉ định theo thẩm quyền; phần mềm lưu và đối chiếu bằng chứng chứ không tự chẩn đoán.
## 5. Điều dưỡng được ghi mũi nếu thiếu chỉ định?
Gợi ý trả lời: Không được tự vượt gate. API yêu cầu clinical/source evidence hợp lệ theo contract trước khi ghi mũi.
## 6. Vì sao khi ghi mũi cần kiểm lô?
Gợi ý trả lời: Để chỉ ghi trên lô phù hợp về vắc xin, hạn, trạng thái và tồn; API phải kiểm lại tại thời điểm mutation.
## 7. Nếu hai điều dưỡng ghi tranh số tồn cuối?
Gợi ý trả lời: Hệ thống cần điều khiển đồng thời/transaction để tránh trừ âm hoặc ghi trùng; đây là rủi ro được kiểm trong thiết kế backend.
## 8. Tại sao ghi mũi phải đi cùng trừ tồn?
Gợi ý trả lời: Nếu ghi mũi thành công nhưng trừ tồn thất bại sẽ sai lệch dữ liệu. Transaction ràng buộc mũi, biến động tồn và audit nhất quán.
## 9. Ghi mũi và theo dõi sau tiêm khác nhau thế nào?
Gợi ý trả lời: Ghi mũi xác nhận việc tiêm đã diễn ra; theo dõi là bản ghi riêng về thông tin thực tế sau tiêm do nhân viên nhập.
## 10. Phần mềm có tự quyết định cho khách về không?
Gợi ý trả lời: Không. Theo dõi sau tiêm không phải hệ thống quyết định an toàn/cho về hoặc thiết bị giám sát sinh hiệu.
## 11. Nhắc lịch đã gửi SMS thật chưa?
Gợi ý trả lời: SCOPE có email reminder/lifecycle và follow-up; SMS/provider dùng chung nằm ở #173 parked, không khẳng định SMS thật đã có.
## 12. Kho đã hoàn chỉnh như ERP chưa?
Gợi ý trả lời: Chưa. X1 đọc kho, X2.1 nhập lô mới, X2.2.1 lịch sử; nhiều chức năng nâng cao của quy trình nhập kho vẫn parked.
## 13. Nếu POST ghi tiêm báo lỗi mạng thì nhấn lại được không?
Gợi ý trả lời: Không blind replay. Cần GET đối chiếu kết quả ghi trước; có thể server đã commit nhưng client mất phản hồi.
## 14. Nếu không chạy được demo thì báo cáo thế nào?
Gợi ý trả lời: Nêu đúng lý do và dùng video/ảnh từ lần chạy thật trước đó, không giả kết quả CI là kết quả runtime.
I. Bài tập bắt buộc trước tối 15/11
Mở 5 màn Windows chính, ghi tên actor và 1 nghiệp vụ mà màn hỗ trợ.
Vẽ luồng 6 ô: đăng ký – lịch – khám/chỉ định – thanh toán – ghi mũi – sau tiêm.
Tìm file VaccinationView.xaml và giải thích dữ liệu đầu vào được yêu cầu.
Tự nói 3 điều kiện phải kiểm trước khi ghi mũi; tuyệt đối không suy liều/eligibility.
Tập nói 6 slide dưới 6 phút; người 1 đặt 5 câu hỏi nghiệp vụ, người 3 đặt 5 câu kỹ thuật.
Demo hoặc video phải xem được mà không lệ thuộc Internet ngoài các thành phần thật sự cần thiết.
Nguồn học & điểm truy cập GitHub
- Nguồn
- Đọc để làm gì

- SCOPE.md trên main
- Trạng thái và phạm vi đã chốt. Ưu tiên hơn README khi thông tin tiến độ mâu thuẫn.

- AGENTS.md trên main
- Kiến trúc, nguyên tắc dữ liệu, clinical, security và cách đọc repo.

- Issue #182 / nhánh docs/thesis-r1-current-state
- Source of truth của báo cáo, 14 System UC và sơ đồ học thuật.

- apps/windows/AGENTS.md; ViewsV2/
- Cấu trúc UI và nguyên tắc quyền/runtime.

- README.md mục S5.5, S5.6, launcher
- Luồng ghi mũi, sau tiêm, chạy thử demo.

- backend/src/TiemChung.Api/*Endpoints.cs liên quan
- Đối chiếu điều kiện API khi bị hỏi sâu.

Đường dẫn repo: https://github.com/datngo1203/KLCN-QuanLyTiemChung
Nhánh main lúc đối chiếu: 92d26778777909daeea200089f9f4461be25cc84. Nhánh thesis lúc đối chiếu: 23da2c015eecb1848486097cd2d483b67628db0c. Ngày đối chiếu: 12/11/2026.
Đề cương CNTT-KLCN090 và mẫu báo cáo HUIT do nhóm cung cấp: dùng để đối chiếu mục tiêu, yêu cầu và thang điểm học thuật.
