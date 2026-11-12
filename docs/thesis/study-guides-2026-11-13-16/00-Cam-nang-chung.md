# Cẩm nang chung

> Bản đọc trực tiếp trên GitHub, chuyển từ tài liệu Word ôn tập ngày 12/11/2026. Đây là tài liệu học; khi có xung đột, đối chiếu SCOPE.md và implementation hiện hành.

Tài liệu chung – 3 người

KLCN090  |  TÀI LIỆU ÔN TẬP BẢO VỆ  |  13–16/11/2026

**BỘ TÀI LIỆU 00**
CẨM NANG CHUNG CHO CẢ NHÓM
Học từ số 0 • Bản đồ hệ thống • Lịch 4 ngày • Slide và phản biện

- Đối tượng
- Cả 3 thành viên

- Lịch học
- 13–16 tháng 11 năm 2026

- Buổi bảo vệ
- 20 phút trình bày + 20 phút hỏi đáp

Nguồn:  AGENTS.md + SCOPE.md main và thesis; Issue #182; đề cương khóa luận HUIT.
Lưu ý:  Đây là tài liệu ôn tập dựa trên bằng chứng của repo và đề cương; không thay thế việc kiểm tra demo thực tế trước ngày bảo vệ.

## 1. Chúng ta phải đạt điều gì?
Đích đến tối 16/11: mỗi người trình bày phần mình mà không đọc nguyên văn slide, cả 3 cùng giải thích được quy trình cốt lõi, có bản PowerPoint 18 slide, có kịch bản demo và luyện được phần lớn câu hỏi phổ biến. Không đặt mục tiêu học hết mã nguồn trong 4 ngày.
- Quy tắc 70/30
70% thời gian học sâu phần được giao; 30% dùng để hiểu cấu trúc toàn hệ thống và học chéo. Giảng viên có thể hỏi bất kỳ người nào về bất kỳ chương nào.

## 2. Hiểu hệ thống trong 60 giây
Dự án “Xây dựng hệ thống quản lý tiêm chủng tại Trung tâm tiêm chủng” tạo một hệ thống hỗ trợ khách hàng tra cứu, đăng ký và theo dõi dịch vụ; đồng thời hỗ trợ nhân viên quản lý hồ sơ, lịch hẹn, khám sàng lọc, chỉ định, thanh toán, ghi nhận tiêm, theo dõi sau tiêm, lô/tồn kho, nhắc lịch, ưu đãi và báo cáo trong phạm vi đã triển khai. Đây là ứng dụng quản lý, không phải hệ thống tự đưa ra quyết định y khoa.
## 3. Kiến trúc: phải giải thích được bằng miệng
- Thành phần
- Giải thích cho người mới
- Không được hiểu sai

- React Website
- Giao diện cho khách hàng tra cứu, gửi yêu cầu và xem dữ liệu của mình.
- Web không trực tiếp đọc/ghi SQL; không là nguồn quyết định quyền hay trạng thái nghiệp vụ.

- Windows WPF
- Ứng dụng máy tính cho nhân viên theo vai trò và quyền hiện hành.
- Không chạy nghiệp vụ độc lập bằng SQL cục bộ.

- ASP.NET Core API
- Nhận request, kiểm tra tài khoản/quyền, validate nghiệp vụ, thao tác SQL và trả response.
- Không mô tả API chỉ như đường truyền dữ liệu.

- SQL Server
- Cơ sở dữ liệu chung lưu các bản ghi nghiệp vụ, quan hệ và ràng buộc.
- Không coi dữ liệu hiển thị trên UI là trạng thái SQL đã được chốt.

Hai hướng đi cùng chung backend: Khách hàng → React Web → API → SQL Server; Nhân viên → WPF Windows → API → SQL Server. API trả phản hồi để hai giao diện hiển thị trạng thái tương ứng.
## 4. Bảy quy trình nghiệp vụ trong SCOPE.md
- #
- Tên quy trình
- Ví dụ phần mềm hỗ trợ

- 1
- Nhập vắc xin từ nhà cung cấp
- Quản lý lô và dữ liệu tồn kho theo chức năng thực có.

- 2
- Đăng ký thông tin khách hàng/người được tiêm
- Lưu và quản lý các hồ sơ liên quan.

- 3
- Đăng ký dịch vụ tiêm chủng
- Ghi nhận dịch vụ/vắc xin được chọn và yêu cầu đăng ký.

- 4
- Khám sàng lọc và chỉ định
- Bác sĩ ghi nhận kết luận/chỉ định, không để phần mềm tự quyết.

- 5
- Thanh toán dịch vụ
- Quản lý trạng thái thanh toán lõi qua API; không gộp với payOS chưa nghiệm thu.

- 6
- Thực hiện tiêm chủng và theo dõi sau tiêm
- Ghi mũi đã thực hiện và ghi nhận theo dõi từ dữ liệu thực.

- 7
- Quản lý lịch tiêm và nhắc lịch
- Lịch hẹn, cập nhật và email/nhắc lịch theo phạm vi đã hoàn thành.

## 5. Tác nhân, vai trò và thuật ngữ
- Tác nhân nghiệp vụ
- Vai trò thường gặp

- Khách hàng
- Tra cứu, đăng ký, theo dõi thông tin thuộc hồ sơ mình.

- Lễ tân
- Tiếp nhận hồ sơ, đăng ký và lịch hẹn theo quyền.

- Bác sĩ
- Khám sàng lọc và đưa ra chỉ định của người chuyên môn.

- Điều dưỡng
- Ghi nhận mũi tiêm đã thực hiện và theo dõi sau tiêm.

- Thu ngân
- Xử lý chức năng thanh toán theo quyền.

- Nhân viên kho
- Quản lý lô/tồn và tiếp nhận lô theo quyền.

Sơ đồ System Use Case trong báo cáo có 8 actor (thêm Quản trị viên và Dịch vụ thư điện tử). Đây không mâu thuẫn với 6 actor nghiệp vụ: hai mô hình đang mô tả hai góc nhìn khác nhau.
## 6. Mười bốn System Use Case được chốt ở nhánh báo cáo
- Nhóm
- Use Case

- Danh mục / quản lý
- Quản lý danh mục tiêm chủng; Quản lý ưu đãi; Quản lý lô và tồn kho vắc xin

- Khách hàng / đăng ký
- Quản lý hồ sơ khách hàng/người được tiêm; Tra cứu dịch vụ tiêm chủng; Đăng ký dịch vụ tiêm chủng

- Lịch hẹn / chuyên môn
- Quản lý lịch hẹn; Gửi nhắc lịch; Ghi nhận khám sàng lọc và chỉ định

- Thanh toán / tiêm
- Thanh toán dịch vụ; Ghi nhận mũi tiêm; Ghi nhận theo dõi sau tiêm

- Theo dõi / quản trị
- Theo dõi hành trình tiêm chủng; Báo cáo hoạt động

14 Use Case là mức trừu tượng của báo cáo, không phải danh sách tất cả màn hình hoặc endpoint. Các năng lực hỗ trợ như đăng nhập, đổi mật khẩu, phân quyền hay tra cứu lịch sử tiêm có thể được triển khai nhưng không là UC top-level độc lập trong Hình 2.9 đã chốt.
## 7. Bộ thuật ngữ 20 từ để không mất gốc
- Thuật ngữ
- Giải thích ngắn

- Frontend
- Giao diện người dùng: Web hoặc WPF.

- Backend
- Phần xử lý phía máy chủ, ở đây là ASP.NET Core.

- API
- Các điểm giao tiếp cho ứng dụng gửi yêu cầu và nhận kết quả.

- HTTP
- Giao thức giao tiếp mà Web/WPF dùng khi gọi API.

- GET
- Thông thường là yêu cầu đọc dữ liệu.

- POST
- Thường dùng tạo một nguồn dữ liệu/điều khiển mới.

- PUT/PATCH
- Thường dùng cập nhật dữ liệu, phải kiểm thực tế route.

- SQL Server
- Hệ quản trị CSDL quan hệ của đề tài.

- Entity
- Đối tượng dữ liệu nghiệp vụ được mô hình hóa trong mã.

- Khóa chính (PK)
- Định danh một bản ghi trong bảng.

- Khóa ngoại (FK)
- Trường liên kết một bảng với bản ghi của bảng khác.

- Use Case
- Chức năng/hoạt động mà tác nhân thực hiện với hệ thống.

- Actor
- Vai trò người hoặc hệ thống bên ngoài tương tác.

- Activity diagram
- Sơ đồ mô tả trình tự hoạt động và nhánh rẽ.

- Class diagram
- Sơ đồ lớp, thuộc tính, quan hệ và bản số.

- Sequence diagram
- Sơ đồ chuỗi thông điệp giữa các thành phần theo thời gian.

- RBAC/capability
- Cơ chế gắn quyền hành động vào vai trò/khả năng.

- Transaction
- Nhóm thao tác SQL cần thành công hoặc thất bại cùng nhau.

- Concurrency
- Các yêu cầu đồng thời có thể tranh chấp trên cùng dữ liệu.

- Regression test
- Kiểm thử đảm bảo sửa đổi không phá chức năng cũ.

## 8. Trạng thái chức năng phải nói cho đúng
- Có thể khẳng định theo SCOPE main
- Giới hạn cần nhắc khi hỏi

- S0–S7 core đã tích hợp và nghiệm thu các phạm vi đã chốt; Web React + WPF + API + SQL chạy theo kiến trúc chung.
- Không suy ra toàn bộ hệ thống đã final integration S8.

- Có Catalog, hồ sơ, đăng ký, thanh toán lõi, lịch hẹn, clinical, vaccination/aftercare/reminder, ưu đãi và báo cáo.
- Không biến payOS real-provider thành “đã tích hợp hoàn chỉnh”: PAY-R1 vẫn parked/draft ở checkpoint.

- X1 đọc kho; X2.1 nhập lô mới; X2.2.1 xem lịch sử tiếp nhận.
- Không gọi đó là full ERP kho/nhà cung cấp; advanced receive vẫn parked.

- DATA-R2 có seed curated vaccine và tiếp tục thay đổi sau ngày 09/11.
- Trước demo xác minh DB hiện chạy, dữ liệu thực và mức cập nhật; không khẳng định dữ liệu seed nào cũng hiện trên máy.

- Tránh 3 câu dễ mất điểm
Không nói “tự động quyết định đủ điều kiện tiêm”, “đã thanh toán payOS thật”, hoặc “đã triển khai toàn bộ hệ thống production”. Những tuyên bố này không phù hợp trạng thái nguồn đã đọc.

## 9. Kế hoạch từng ngày (13–16/11)
- Ngày
- Sáng
- Chiều
- Tối – bắt buộc có đầu ra

- 13/11
- Cả nhóm: đề tài, actor, 7 quy trình, kiến trúc.
- Mỗi người đọc 1 bộ học chuyên sâu và tìm 3 file thuộc vùng mình.
- Mỗi người nói lại 2 phút về cả hệ thống; chia 18 slide.

- 14/11
- Đọc luồng nghiệp vụ/nguồn mã đúng phần.
- Tạo slide và lời nói của phần mình.
- Ghép PPT đạt ≥80%; hỏi chéo 5 câu.

- 15/11
- Hoàn thiện PPT, kiểm hình UML + nguồn.
- Thử runtime demo, sao lưu ảnh/video dự phòng.
- Tập 20 phút + 20 phút hỏi; ghi lỗi cần cải thiện.

- 16/11
- Tổng duyệt 20 phút, bấm giờ.
- Hỏi chéo và sửa phần yếu; kiểm máy/file.
- Tổng duyệt cuối; không thay đổi kiến trúc/phạm vi.

## 10. Bố cục PowerPoint 18 slide trong 20 phút
- Slide
- Nội dung trên slide
- Trách nhiệm

- 1
- Tên đề tài, nhóm, giảng viên
- Người 1

- 2
- Lý do chọn đề tài/vấn đề
- Người 1

- 3
- Mục tiêu, phạm vi, giới hạn
- Người 1

- 4
- Các tác nhân & người dùng
- Người 1

- 5
- 7 quy trình nghiệp vụ (mô hình đơn giản)
- Người 1

- 6
- Web/customer journey + ảnh thực tế
- Người 1

- 7
- WPF: vai trò nhân viên, các khu vực chính
- Người 2

- 8
- Tiếp nhận hồ sơ, đăng ký, lịch hẹn
- Người 2

- 9
- Khám sàng lọc và chỉ định
- Người 2

- 10
- Thanh toán trong chuỗi nghiệp vụ
- Người 2

- 11
- Ghi nhận mũi tiêm và theo dõi sau tiêm
- Người 2

- 12
- Nhắc lịch, kho và báo cáo nhân viên (ngắn)
- Người 2

- 13
- Kiến trúc 4 thành phần
- Người 3

- 14
- Cách API kiểm quyền/xử lý
- Người 3

- 15
- Dữ liệu và sơ đồ lớp/quan hệ tiêu biểu
- Người 3

- 16
- Kiểm thử + chứng cứ, hạn chế bằng chứng
- Người 3

- 17
- Kết quả, giới hạn và hướng phát triển
- Người 3

- 18
- Demo ngắn + kết thúc
- Cả nhóm

Mốc thời gian tập: người 1 từ 00:00 đến 05:00; người 2 từ 05:00 đến 11:00; người 3 từ 11:00 đến 17:00; demo 17:00–19:00; kết thúc/dự phòng 19:00–20:00. Nếu không demo trực tiếp, dùng 2 phút cuối trình bày một luồng sản phẩm với ảnh minh chứng.
## 11. Kịch bản demo ngắn và phương án dự phòng
Chuẩn bị máy có SQL/API/Web/WPF đã khởi động và tài khoản Development phù hợp, tuyệt đối không đưa mật khẩu vào slide.
Hiện Website, mở danh mục; tìm một vắc xin/tra cứu dịch vụ đang có trong DB để chứng minh đọc dữ liệu thật.
Hiện phần Windows phù hợp vai trò, cho thấy thông tin hồ sơ/lịch hẹn/ghi nhận đã chuẩn bị sẵn; chỉ thực hiện mutation nếu đã thử trước trên DB demo và dữ liệu nguồn hợp lệ.
Giải thích yêu cầu đi qua API và dữ liệu được lưu chung trong SQL Server. Chỉ mô tả khả năng đã xác minh trên máy.
Nếu live demo hỏng: có video 2 phút đã quay từ đúng bản build và ảnh màn hình của dữ liệu thật; báo rõ đây là phương án dự phòng, không giả lỗi là PASS.
Không reset/reseed DB để sửa lỗi demo; không tạo chỉ định/lô/y tế giả để vượt gate. Launcher và môi trường có trong README; sử dụng dự án Compose đúng tên (README cảnh báo klcn-s0 và vac-demo là 2 volume khác nhau).
## 12. Mẫu trả lời câu hỏi phản biện
Công thức 4 bước: (1) Nêu đúng khái niệm; (2) liên hệ cụ thể với dự án; (3) nói bằng chứng là repo/slide/demo nào; (4) nếu không chắc thì nhận rõ giới hạn và hẹn kiểm tra lại, không tự bịa. Ví dụ: “API có trách nhiệm kiểm tra quyền ở server; trong dự án, cả Web và WPF đều gọi API, không sửa SQL trực tiếp. Phần kiểm tra quyền được xác định trong AGENTS.md và backend.”
## 1. Vì sao có cả Web và Windows?
Gợi ý trả lời: Vì có hai nhóm sử dụng khác nhau: khách hàng và nhân viên. Cả hai giao diện dùng API và SQL Server chung để thống nhất dữ liệu.
## 2. Vì sao không cho Web truy cập SQL?
Gợi ý trả lời: API chịu trách nhiệm bảo vệ dữ liệu, xác thực quyền và ràng buộc nghiệp vụ; truy cập SQL trực tiếp từ client sẽ khó kiểm soát.
## 3. Đây có phải hệ thống tự quyết đủ điều kiện tiêm?
Gợi ý trả lời: Không. Quyết định chuyên môn và chỉ định thuộc bác sĩ; hệ thống lưu/check bằng chứng nghiệp vụ theo contract, không thay quyết định y khoa.
## 4. Use Case nghiệp vụ khác System Use Case?
Gợi ý trả lời: Use Case nghiệp vụ mô tả hoạt động của tổ chức, kể cả bước thủ công; System Use Case mô tả tương tác với phần mềm trong phạm vi hệ thống.
## 5. Tại sao có 6 actor nghiệp vụ nhưng 8 actor System UC?
Gợi ý trả lời: Bởi vì mô hình System UC bổ sung vai trò phần mềm Quản trị viên và dịch vụ thư điện tử; góc nhìn và ranh giới khác nhau.
## 6. Tiến độ tới đâu?
Gợi ý trả lời: Các module cốt lõi được tích hợp theo SCOPE main; vẫn có S8 final integration và một số expansion/delivery/provider được parked. Chỉ nói kết quả CI/runtime nào đã có bằng chứng.
## 7. Khác nhau giữa đăng ký dịch vụ và lịch hẹn?
Gợi ý trả lời: Đăng ký là yêu cầu sử dụng dịch vụ; lịch hẹn là thông tin ngày/giờ tiếp nhận được quản lý/xác nhận riêng. Ngày giờ mong muốn không tự biến thành lịch xác nhận.
## 8. Có bảo đảm không trừ tồn kho sai khi ghi tiêm?
Gợi ý trả lời: API cần kiểm tra quyền, chỉ định/nguồn, thanh toán và lô/tồn; thao tác ghi mũi, trừ tồn, biến động và audit nhất quán trong transaction theo thiết kế hiện hành.
## 9. PowerPoint có đủ phản ánh báo cáo?
Gợi ý trả lời: Nó là bản tóm tắt tiêu điểm. Các sơ đồ và diễn giải chi tiết nằm trong Word; slide cần giữ đúng số lượng/mức trừu tượng và kết quả đã triển khai.
## 10. Nếu không trả lời được câu hỏi?
Gợi ý trả lời: Thừa nhận chính xác điểm chưa chắc, xác định nơi kiểm chứng. Không đoán nghiệp vụ y khoa hoặc khẳng định một tính năng thiếu bằng chứng.
## 13. Checklist cuối buổi 16/11
□ Mỗi người nói được 2 phút về toàn bộ hệ thống.
□ Mỗi người nói được 5–6 phút về phần riêng mà không đọc slide.
□ Cả 3 hiểu sự khác nhau giữa đăng ký, lịch hẹn, khám/chỉ định, ghi mũi và sau tiêm.
□ Đúng 14 System UC trong báo cáo; không dùng sơ đồ cũ 18/21 UC.
□ Nêu được ít nhất 3 giới hạn/trạng thái chưa nghiệm thu.
□ Có PPTX + bản PDF dự phòng và video/ảnh demo dự phòng.
□ Đã tập 2 lượt 20+20 và chốt thời gian.
Nguồn học & điểm truy cập GitHub
- Nguồn
- Đọc để làm gì

- SCOPE.md trên main
- Trạng thái và phạm vi đã chốt. Ưu tiên hơn README khi thông tin tiến độ mâu thuẫn.

- AGENTS.md trên main
- Kiến trúc, nguyên tắc dữ liệu, clinical, security và cách đọc repo.

- Issue #182 / nhánh docs/thesis-r1-current-state
- Source of truth của báo cáo, 14 System UC và sơ đồ học thuật.

- README.md, mục khởi động
- Chỉ dùng khi chuẩn bị demo hoặc xem cách chạy máy.

- docs/thesis/uml/Hinh_2_9_System_Use_Case_Tong_Quan.puml
- Sơ đồ System UC 14 chức năng đã chốt.

Đường dẫn repo: https://github.com/datngo1203/KLCN-QuanLyTiemChung
Nhánh main lúc đối chiếu: 92d26778777909daeea200089f9f4461be25cc84. Nhánh thesis lúc đối chiếu: 23da2c015eecb1848486097cd2d483b67628db0c. Ngày đối chiếu: 12/11/2026.
Đề cương CNTT-KLCN090 và mẫu báo cáo HUIT do nhóm cung cấp: dùng để đối chiếu mục tiêu, yêu cầu và thang điểm học thuật.
