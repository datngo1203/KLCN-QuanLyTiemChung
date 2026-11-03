# QUYẾT ĐỊNH NGHIỆP VỤ — KLCN090

> **Sổ quyết định BA được người dùng xác nhận**, lập 03/11/2026. Chỉ phục vụ phân tích và thiết kế; **không tự cấp phép triển khai**.
>
> Thứ tự ưu tiên: đề cương khóa luận + `SCOPE.md` quy định phạm vi được triển khai, `AGENTS.md` quy định cách làm; tài liệu này ghi **ý định nghiệp vụ đã chốt** và các vấn đề còn mở. Nếu vượt SCOPE, chỉ làm sau khi thay đổi phạm vi được phê duyệt và giao một lát riêng. Không tự động đưa các đề xuất thành code.

## A. Quyết định ĐÃ CHỐT (03/11/2026)

### QD-001 — Chiến lược rà soát toàn hệ thống
- Rà lại nghiệp vụ của Website và phần mềm Windows, làm sâu chức năng thực sự có ích, theo **thứ tự phụ thuộc dữ liệu**, bắt đầu quản lý vắc xin/danh mục → gói vắc xin → các nghiệp vụ phía sau.
- Có thể **đơn giản hóa trải nghiệm đăng nhập/đăng ký tài khoản**, nhưng không giảm bảo mật, quyền API, kiểm tra an toàn hoặc các quy tắc của đăng ký **dịch vụ tiêm**, tiền/tồn/chỉ định.
- Không sửa toàn bộ một lần; làm từng lát nhỏ và nghiệm thu.

### QD-002 — A04 (Catalog) hướng thiết kế mức C
- Mục tiêu BA: hồ sơ vắc xin đầy đủ; bệnh phòng ngừa; thông tin chuyên môn có căn cứ; lịch tiêm tham khảo có cấu trúc; tài liệu nguồn, phiên bản, kiểm duyệt; nội dung công khai trên Website.
- Đây là **chốt hướng nghiệp vụ**, chưa chốt mọi trường, schema, thuật toán hay migration và chưa đồng ý sửa code ngay.
- Không coi nội dung tham khảo là lệnh chỉ định cá thể; dữ liệu chưa được xác minh phải thể hiện rõ chưa xác minh.

### QD-003 — A04 → A02: cảnh báo mặc định + chặn giới hạn
- Chọn **B: tra cứu + cảnh báo hỗ trợ** làm mặc định. Cảnh báo phân mức `thông tin / lưu ý / nghiêm trọng`, có căn cứ, nội dung và hành động đề xuất. Bác sĩ tự đánh giá, hệ thống không tự kết luận đủ điều kiện.
- Kết hợp **C: chặn có ràng buộc** với một nhóm nhỏ tình huống chống chỉ định thực sự được xác nhận và có quy trình chuyên môn duyệt trước.
- Phân biệt mức độ cảnh báo với hành động thực thi. Thiếu dữ liệu ≠ bình thường ≠ đã xác nhận chống chỉ định; không đưa mọi cảnh báo nặng thành chặn.

### QD-004 — Xử lý chặn và ngoại lệ an toàn
- Quy tắc chặn phải gắn đúng sản phẩm/điều kiện, nguồn, phiên bản và phê duyệt, có lịch sử đánh giá.
- **Chỉ nhập lý do ngoại lệ và ghi audit KHÔNG đủ để vượt chống chỉ định xác nhận.** Chỉ có thể bổ sung/xác minh lại dữ liệu, xem lại điều kiện chuyên môn hoặc chuyển hướng xử lý hợp lệ; không tiêm vắc xin bị chặn khi điều kiện vẫn đúng.
- Chưa triển khai chặn lâm sàng thực trên người bệnh. Quy tắc mức C chỉ được mô phỏng/kiểm thử khi chưa có thẩm định thực tế.

### QD-005 — Phê duyệt quy tắc chuyên môn mô phỏng
- Người dùng chọn **quy trình phê duyệt mô phỏng trong khóa luận**. Người dùng vai trò Bác sĩ được phân quyền duyệt trong môi trường demo, lưu người duyệt, thời gian, phiên bản, log.
- Không gọi đây là phê duyệt của hội đồng chuyên môn thực tế. Dữ liệu/quy tắc chặn thử nghiệm chỉ sử dụng synthetic trong Development/Test.

### QD-006 — Phạm vi đối tượng A02
- A02 phục vụ **cả trẻ em và người lớn**; bảng kiểm và điều kiện hiển thị phải phù hợp nhóm tuổi/tình trạng, có nhóm mục riêng cho thai phụ khi liên quan.
- Không dùng toàn bộ bảng kiểm trẻ em cho người lớn. Trường dữ liệu cụ thể/validation vẫn **chưa chốt**, phải đối chiếu biểu mẫu và văn bản hiện hành.
- Khi có lịch sử tiêm, hiển thị được thông tin, nguồn chứng cứ và mức độ xác minh; không suy đoán mũi thứ mấy.

### QD-007 — Hướng cải thiện A02
- Bác sĩ cần nhìn được danh tính/ngữ cảnh khám, tiền sử/tình trạng hiện tại có liên quan, lịch sử tiêm, chi tiết cảnh báo (thiếu dữ liệu gì, lý do, mức độ, hậu quả và hành động), nội dung quy tắc/căn cứ, người ghi và thời điểm.
- Không được chọn sẵn kết luận. Khi tạm hoãn/chống chỉ định cần lý do; cảnh báo quan trọng phải được Bác sĩ xem xét/xác nhận theo quy tắc đã chốt. Giữ dấu vết thay đổi.
- **Đối chiếu code 03/11:** A02 đang để kết luận trống, bắt buộc lý do cho tạm hoãn/chống chỉ định, đã hiển thị Bác sĩ/thời điểm và chỉ đọc sau lưu. Không giao làm lại những phần đã tồn tại. API A02 hiện tạo kết luận qua `Conclusion, Reason, VaccineIds`, chưa thu thập sinh tồn/tiền sử theo cấu trúc và chưa có engine cảnh báo.
- Các cột/bảng/phương án điều chỉnh hồ sơ và validation chi tiết là **đang đặc tả**, chưa coi là chốt triển khai.

### QD-008 — Cách phối hợp nhóm và ranh giới triển khai
- **Bảo sở hữu S6** và phần Registration/Payment có liên quan; **Quý sở hữu S7/Reports**. Không chỉnh code/contract/schema/migration/shared files hoặc PR của hai người trong lúc họ triển khai.
- **S5 đã nghiệm thu theo contract S4**, không tự mở lại. Nếu CAT/A02 ảnh hưởng S5 hoặc S6/S7, lập một gói tương thích riêng và review chéo.
- Giai đoạn hiện tại: **chỉ phân tích/thiết kế**; chưa cho phép Codex sửa code, database, `SCOPE.md` hay vận hành quy tắc chặn.

### QD-009 — A02 lưu bản nháp phiếu khám xuống SQL (03/11/2026)
- Người dùng **chốt phương án B**: khi khám sàng lọc, Bác sĩ có thể lưu **bản nháp xuống SQL Server** qua API, rời màn hình/đăng nhập lại rồi tiếp tục hoàn tất sau.
- **Bản nháp không phải kết luận đã xác nhận, không được tạo chỉ định tiêm hợp lệ, không cấp quyền ghi mũi tiêm, không tự qua cổng an toàn S5**. Phải có thao tác xác nhận hoàn tất riêng của Bác sĩ và kiểm tra nghiệp vụ tại API.
- Không đồng nhất bản nháp với `PhieuKhamSangLoc` đã xác nhận trong logic S5 hiện hành. Cần thiết kế rõ cách lưu tách biệt/đánh dấu và thay đổi đường đọc để **bản nháp không bị hiểu nhầm thành kết luận thật**.
- Chưa chốt số trạng thái DB, cơ chế nhiều Bác sĩ đồng thời, quyền sửa/chuyển giao, thời hạn nháp, nội dung từng field, chính sách đính chính hoặc phương án migration. Tất cả vẫn thuộc phần **đang đặc tả**, không tự triển khai.
- Quyết định này chỉ được **ghi nhận trong tài liệu BA**; không cấp phép sửa S5, API, database, migration hoặc công việc S6/S7. Chỉ tạo PR code riêng sau khi đánh giá tác động, được giao và phê duyệt thay đổi phạm vi.

### QD-010 — A02: chuyển giao bản nháp giữa Bác sĩ có kiểm toán (03/11/2026)
- Người dùng **chốt phương án B**: Bác sĩ có quyền được **tiếp nhận bản nháp của Bác sĩ khác theo quy trình chuyển giao có audit**, thay vì chỉ người tạo được sửa hoặc mọi Bác sĩ tùy ý cùng sửa.
- Mục tiêu: hỗ trợ đổi ca/đổi Bác sĩ, xác định rõ người tạo, người bàn giao, người tiếp nhận, thời điểm và dấu vết thao tác. **Không được ghi đè không kiểm soát hoặc chuyển giao âm thầm.**
- Quyền Bác sĩ và phiên đăng nhập hợp lệ phải được kiểm chứng tại API. Việc bàn giao/nhận bàn giao **không xác nhận phiếu**, không tạo `ChiDinhTiem`, không bỏ qua bước Bác sĩ xác nhận kết luận theo QD-009.
- **ĐÃ CHỐT**: cho phép tiếp nhận có kiểm soát và ghi audit; **CHƯA CHỐT**: Bác sĩ gửi hay người có quyền chủ động nhận, cần bước chấp nhận hay không, vai trò nào được thu hồi/chuyển lại, cách xử lý người tạo vắng mặt, quyền đọc khi đã bàn giao, version/lock đồng thời, lý do bắt buộc và TTL/chính sách thông báo. Các chi tiết là phương án BA cần người dùng duyệt, không tự code.
- Tiếp tục nguyên tắc của QD-008: chỉ đặc tả; không sửa S5, S6/S7, schema, API, quyền hệ thống hay `SCOPE.md` trước khi được phép triển khai.

## B. Hiện trạng có căn cứ (không phải tính năng mới đã làm)

- Đề cương CNTT-KLCN090 nêu Windows quản lý loại tiêm chủng/loại vắc xin/vắc xin, đăng ký, lịch tiêm; Website giới thiệu, tìm kiếm, đăng ký. Phần nâng cấp mức C vượt chi tiết đề cương nên cần duyệt tác động.
- A04: `CatalogEndpoints` và WPF A04 đã CRUD. Entity `VacXin` có `LieuLuong`, `TheTich`, `PhanUngSauTiem`, `TrangThai`; tồn tại bảng `Benh`, `VacXin_Benh`, nhưng API contract/UI chưa khai thác đầy đủ. Không tạo cột trùng trước khi rà ý nghĩa.
- A02: `ClinicalEndpoints` ghi kết luận/chỉ định tối thiểu. `PhieuKhamSangLoc` có trường sinh tồn/tiền sử nhưng luồng tạo kết luận hiện chưa nhập và lưu các trường đó. WPF A02 chưa có bảng lịch sử tiêm hay engine quy tắc mức C.
- Không trình bày dữ liệu/vắc xin DEMO như thông tin y tế được xác minh; không tự suy diễn quy trình nội bộ VNVC.

## C. ĐANG ĐẶC TẢ / CẦN BỔ SUNG (chưa chốt, không được tự code)

1. **A02:** ĐÃ CHỐT lưu bản nháp SQL (QD-009) và cho Bác sĩ có quyền tiếp nhận bản nháp qua chuyển giao có audit (QD-010). **CHƯA CHỐT**: các bước bàn giao/nhận cụ thể, phân quyền chi tiết, concurrency/lock, các trạng thái phụ, timeout, đính chính phiếu đã xác nhận; danh sách trường/validation chính xác cho sơ sinh, trẻ em, người lớn, thai phụ; cách biểu diễn Chưa hỏi/Không rõ/Không áp dụng; nguồn dữ liệu; bằng chứng lịch sử và thao tác xác nhận cảnh báo.
2. **A04:** định nghĩa phân loại, bệnh phòng ngừa, thông tin chuyên môn, phiên bản, phác đồ cấu trúc, phê duyệt/công bố và ảnh hưởng tới đăng ký đã có.
3. **Y khoa:** cần đối chiếu **Quyết định 1575/QĐ-BYT (2023)** cho trẻ em; **Quyết định 3160/QĐ-BYT (10/10/2026)** cho người lớn và phụ nữ có thai; phải đọc phụ lục và biểu mẫu gốc **trước khi chốt từng câu hỏi/logic**. Không coi phần mô tả BA này là checklist y tế chính thức.
4. **SQL/kiến trúc:** Web/WPF → API ASP.NET Core → EF Core/SQL Server. Mọi migration, DTO hay repository mới là đề xuất, chưa chốt refactor hoặc cho phép làm khi S6/S7 đang hoạt động.
5. **CẦN BỔ SUNG:** nguồn chính thức cho từng quy tắc sản phẩm; người thực hiện phê duyệt mô phỏng đáp ứng quyền backend nào; phê duyệt thay đổi SCOPE bởi nhóm/GVHD; tiêu chí nghiệm thu và test của từng lát.

## D. Cách duy trì tài liệu qua các chat/Codex session

- Mỗi lần người dùng xác nhận rõ **“chốt / tôi chọn”**, ghi thêm `QD-xxx` với ngày, nội dung quyết định và ranh giới. Ý tưởng chưa chốt nằm mục C, không coi là yêu cầu triển khai.
- Nếu thay đổi quyết định đã chốt, tạo mã mới ghi `Thay thế QD-xxx`; không xóa lịch sử.
- **SCOPE.md** vẫn là nguồn sự thật về phạm vi triển khai và cột trạng thái; **không dùng tài liệu này làm sổ tiến độ**.
- Codex chỉ đọc quyết định BA để chuẩn bị kế hoạch; muốn sửa code phải có gói công việc được giao riêng, kiểm tra tác động contract, schema và owner, tạo nhánh/PR đúng quy tắc.
- Cập nhật file này qua PR tài liệu riêng, không tự merge hoặc sửa main khi chưa review.
