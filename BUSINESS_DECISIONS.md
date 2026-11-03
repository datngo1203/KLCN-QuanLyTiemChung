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
- **ĐÃ CHỐT**: cho phép tiếp nhận có kiểm soát và ghi audit; **CHƯA CHỐT**: Bác sĩ gửi hay người có quyền chủ động nhận, cần bước chấp nhận hay không, vai trò nào được thu hồi/chuyển lại, cách thực hiện tiếp quản khi người phụ trách vắng mặt theo QD-011, quyền đọc khi đã bàn giao, version/lock đồng thời, lý do bắt buộc và TTL/chính sách thông báo. Các chi tiết là phương án BA cần người dùng duyệt, không tự code.
- Tiếp tục nguyên tắc của QD-008: chỉ đặc tả; không sửa S5, S6/S7, schema, API, quyền hệ thống hay `SCOPE.md` trước khi được phép triển khai.

### QD-011 — A02: tiếp quản bản nháp khi Bác sĩ phụ trách vắng mặt, có người phê duyệt (03/11/2026)
- Người dùng **chốt phương án B**: khi Bác sĩ đang phụ trách bản nháp **vắng mặt, không thể tự bàn giao**, chỉ cho **tiếp quản có kiểm soát thông qua người quản lý được chỉ định có thẩm quyền phê duyệt**, với lý do và audit. Không cho Bác sĩ khác tự chiếm quyền hay sửa trực tiếp bản nháp.
- Phê duyệt **chuyển quyền phụ trách hồ sơ**, không phải phê duyệt chuyên môn/chỉ định tiêm; người tiếp nhận vẫn là Bác sĩ có quyền, chịu trách nhiệm tự xem xét dữ liệu và xác nhận kết luận theo QD-009/QD-010.
- Không mặc định tài khoản **Admin kỹ thuật** có quyền phê duyệt tiếp quản; không tự bổ sung actor/chức danh `Quản lý` vào đề cương. Người có thẩm quyền đã chốt tại **QD-012** là Bác sĩ phụ trách chuyên môn được ủy quyền; **CẦN BỔ SUNG** cơ chế ủy quyền và quyền backend tương ứng.
- Dù có phê duyệt, bản nháp vẫn **không phải chỉ định hợp lệ**, không tạo `ChiDinhTiem`, không được qua cổng ghi tiêm S5. Kiểm quyền, danh tính, trạng thái và kiểm soát đồng thời phải ở API; lịch sử cũ bất biến, không xóa/ghi đè.
- **Chưa chốt**: ai khởi tạo yêu cầu khi người phụ trách vắng mặt; chứng cứ vắng mặt; lý do bắt buộc và độ dài; yêu cầu người tiếp nhận chấp nhận hay không; chính sách ngăn tự duyệt; xử lý người quản lý/nhân sự không sẵn sàng; cơ chế timeout, khóa, từ chối/hủy, mức độ hiển thị dữ liệu sức khỏe và thông báo. Các mục này là **đang đặc tả**.
- Chỉ ghi quyết định BA trên PR tài liệu; chưa cho phép thay đổi code/SQL/schema/role/`SCOPE.md`. Giữ ranh giới S6 của Bảo, S7 của Quý và đóng băng phần S5 đã nghiệm thu.

### QD-012 — A02: Bác sĩ phụ trách chuyên môn được ủy quyền duyệt tiếp quản (03/11/2026)
- Người dùng **chốt**: người có thẩm quyền phê duyệt tiếp quản bản nháp khi Bác sĩ phụ trách vắng mặt là **Bác sĩ phụ trách chuyên môn được ủy quyền**. Đây là một người dùng thuộc actor **Bác sĩ hiện có**, **không bổ sung actor/chức danh thứ bảy** và không mặc định Admin kỹ thuật hoặc nhân sự quản lý hành chính có thẩm quyền này.
- Quyền **duyệt chuyển người phụ trách bản nháp** phải tách biệt với quyền thông thường **khám/kết luận/chỉ định**; chỉ người được ủy quyền hợp lệ mới phê duyệt tại API. Không tự cấp quyền mới trong quá trình ghi sổ BA; cơ chế cấp/thu hồi và biểu diễn quyền backend còn phải thiết kế, kiểm duyệt phạm vi.
- Phê duyệt chỉ hợp thức hóa **việc chuyển quyền phụ trách** theo QD-011; không đồng nghĩa phê duyệt y khoa, không đánh giá bệnh nhân đủ điều kiện, không tự tạo `ChiDinhTiem`, không thay thế thao tác xác nhận kết luận của Bác sĩ tiếp nhận.
- Ghi audit người yêu cầu, người duyệt, người tiếp nhận, lý do, thời điểm và kết quả; giới hạn đọc dữ liệu sức khỏe theo nhiệm vụ. **Chống tự duyệt**, xử lý đồng thời, yêu cầu chứng cứ vắng mặt và chi tiết cấp quyền là đề xuất cần chốt riêng, chưa tự động coi là quyết định triển khai.
- Quyết định này chỉ cập nhật tài liệu phân tích trên PR #63; không thay đổi `SCOPE.md`, code, SQL, quyền người dùng thực tế hoặc phạm vi đã nghiệm thu S5; không ảnh hưởng công việc S6/S7.

### QD-013 — A02: kết luận Tạm hoãn/Chống chỉ định sớm có điều kiện (03/11/2026)
- Người dùng **chốt phương án B**: Bác sĩ có thể **xác nhận kết luận "Tạm hoãn" hoặc "Chống chỉ định" trước khi hoàn tất toàn bộ bảng kiểm**, khi đã có **căn cứ chuyên môn đủ để đưa ra kết luận tương ứng**. Phải ghi rõ kết quả phát hiện, căn cứ/lý do kết luận, những mục **chưa đánh giá**, người xác nhận và thời điểm.
- Các mục chưa đánh giá **giữ nguyên là chưa đánh giá**, không tự chuyển thành `Không`, `Không áp dụng` hoặc `Bình thường`. Kết luận sớm **không tạo `ChiDinhTiem`**, không chỉ định vắc xin được tiêm, không cho phép ghi mũi, không bỏ qua an toàn S5. Phải phân biệt lưu bản nháp theo QD-009 với **xác nhận kết luận Tạm hoãn/Chống chỉ định** đã hoàn tất có ghi nhận riêng.
- **KHÔNG áp dụng kết luận sớm cho "Đủ điều kiện"**: trường hợp này phải hoàn thành các nội dung sàng lọc bắt buộc theo bảng kiểm áp dụng và xử lý cảnh báo liên quan trước khi Bác sĩ tự xác nhận.
- Không biến một dấu hiệu, câu trả lời `Có` hoặc dữ liệu chưa rõ thành chẩn đoán/chống chỉ định tuyệt đối tự động. Khi thiếu căn cứ xác định chống chỉ định đối với sản phẩm cụ thể, giữ kết luận/thao tác phù hợp để Bác sĩ đánh giá thêm; quy tắc chặn thực tế chỉ dùng với nguồn chuyên môn và phê duyệt hợp lệ (QD-003–005).
- **ĐÃ CHỐT nguyên tắc nghiệp vụ**, **CHƯA CHỐT**: tiêu chí lâm sàng chi tiết xác định "đủ căn cứ", biểu mẫu/bằng chứng bắt buộc cho từng loại lý do, mức phân loại mục chưa đánh giá, hình thức lưu snapshot, quy trình đính chính, API/DTO/schema và các test case cụ thể. Phải đối chiếu biểu mẫu Bộ Y tế theo từng nhóm tuổi trước khi cố định ngưỡng y khoa.
- Chỉ ghi vào sổ BA trên PR #63. Không tự sửa `SCOPE.md`, code, SQL, migrations, quyền đã chốt, S5, S6 của Bảo hoặc S7 của Quý.

### QD-014 — A02: một phiếu khám chung, đánh giá và chỉ định riêng theo từng vắc xin (03/11/2026)
- Người dùng **chốt**: mỗi buổi khám/lịch hẹn có **một hồ sơ khám sàng lọc chung**, nhưng **Bác sĩ kết luận riêng cho từng vắc xin được xem xét**; chỉ vắc xin được Bác sĩ chủ động duyệt mới được tạo **chỉ định tiêm hợp lệ**. Không suy từ kết luận tổng quát hoặc từ việc thiếu cảnh báo rằng toàn bộ các vắc xin trong gói đều được phép tiêm.
- Cần phân biệt rõ kết quả đánh giá từng sản phẩm (ví dụ: được chỉ định, tạm hoãn, chống chỉ định, chưa đủ thông tin) với tóm tắt buổi khám; các nhãn, ràng buộc và chi tiết lưu vẫn **chưa chốt**.
- **Giữ nguyên QD-013**: nếu Bác sĩ **xác nhận Tạm hoãn/Chống chỉ định sớm khi bảng kiểm chưa hoàn tất**, không tạo **bất kỳ** `ChiDinhTiem` nào cho buổi đó. Xử lý **đánh giá hỗn hợp** (một vắc xin được chỉ định, vắc xin khác tạm hoãn/chống chỉ định) chỉ sau khi đủ điều kiện xác nhận phần được tiêm, không được lách việc hoàn thành bảng kiểm.
- Chỉ định theo từng vắc xin vẫn phải đáp ứng đúng lịch hẹn, đăng ký, quyền Bác sĩ và cổng kiểm tra an toàn ghi tiêm của S5; không tự tạo lại lịch, đổi cấu phần gói, giá, thanh toán hay ưu đãi.
- **TÁC ĐỘNG cần review trước khi code:** `PhieuKhamSangLoc.KetLuan`, `ChiDinhTiem`, API `ClinicalEndpoints` và `VaccinationEndpoints` hiện kiểm kết luận `Đủ điều kiện`; không tự thêm giá trị tổng hợp mới khiến S5 hiểu sai hoặc từ chối nhầm. Thiết kế schema/DTO/migration/API/UI và tương thích S5 là đề xuất chưa duyệt.

### QD-015 — A02: đính chính phiên bản, giữ bản gốc và hiệu lực chỉ định (03/11/2026)
- Người dùng **chốt**: cho phép **đính chính phiếu khám đã xác nhận bằng phiên bản mới**, giữ nguyên bản gốc, người thực hiện, thời gian, lý do, sự khác biệt và đường dẫn truy vết. **Không cập nhật đè hoặc xóa lịch sử đã xác nhận**.
- Đính chính có thể thay đổi dữ liệu giải trình hoặc, khi được đánh giá và phê duyệt hợp lệ, thay đổi kết luận/chỉ định. Nếu thay đổi hiệu lực chỉ định, **chỉ định cũ không còn hợp lệ phải bị loại khỏi luồng ghi tiêm**; API phải kiểm tra **chỉ định đang có hiệu lực tại thời điểm ghi tiêm**, không chỉ sự tồn tại của `ChiDinhTiem` đã tạo.
- **Mũi tiêm đã thực hiện không được xóa, đảo ngược hoặc thay đổi hồi tố vì một bản đính chính**. Nếu có sai sót liên quan mũi đã tiêm, cần lưu hồ sơ và xử lý theo quy trình chuyên môn/đính chính riêng; không tự sửa tồn kho hay biến động kho để tạo cảm giác chưa tiêm.
- Không tự sửa giá, gói dịch vụ, ưu đãi hoặc thanh toán khi đính chính; các hệ quả thương mại phải được phân tích và thống nhất với người phụ trách S4/S6 trước khi mở phạm vi.
- **ĐÃ CHỐT nguyên tắc**, **CHƯA CHỐT**: ai được tạo/phê duyệt đính chính, điều kiện trước/sau khi đã tiêm, quy trình thu hồi/vô hiệu chỉ định, cách hiển thị phiên bản hiệu lực, chống cạnh tranh, chính sách lưu snapshot, dữ liệu/contract/schema, và test case cụ thể. Không kích hoạt đường đính chính thay đổi chỉ định khi S5 chưa có kiểm tra hiệu lực tương ứng.
- Chỉ cập nhật sổ BA, không tự cho phép sửa code, SQL, migration, `SCOPE.md`, S5 đã nghiệm thu hay module S6/S7.

### QD-016 — A02: Bác sĩ phụ trách đề nghị đính chính; phê duyệt chuyên môn khi ảnh hưởng kết luận/chỉ định (03/11/2026)
- Người dùng **chốt phương án B**: **Bác sĩ đang chịu trách nhiệm phiếu khám** được tạo **yêu cầu đính chính** phiếu đã xác nhận. Trường hợp đính chính **làm thay đổi kết luận chuyên môn hoặc hiệu lực chỉ định tiêm**, phải được **Bác sĩ phụ trách chuyên môn được ủy quyền phê duyệt** trước khi bản đính chính chính thức có hiệu lực.
- Người phê duyệt thuộc **actor Bác sĩ hiện có** theo QD-012; quyền phê duyệt đính chính có ảnh hưởng chỉ định phải được xác thực riêng, **không mặc định trao cho Admin kỹ thuật, Bác sĩ thông thường hoặc người khởi tạo**. Cơ chế phân quyền cụ thể/việc ngăn tự phê duyệt đang đặc tả, chưa triển khai.
- Giữ nguyên QD-015: tạo **phiên bản mới**, bảo lưu bản gốc và toàn bộ lịch sử, ghi người yêu cầu, người duyệt, căn cứ/lý do, thời điểm, khác biệt và tác động từng vắc xin. Không ghi đè trực tiếp; **không sửa/xóa/đảo ngược mũi tiêm đã thực hiện**.
- Đính chính ảnh hưởng chỉ định phải được kiểm tra cùng luồng **ghi tiêm S5**: chỉ định cũ không còn hợp lệ không được dùng để ghi mũi mới, kể cả khi Điều dưỡng đã mở màn hình trước đó. **Không triển khai cơ chế đính chính thay đổi chỉ định cho đến khi cổng kiểm tra hiệu lực ở API được thiết kế, kiểm thử và phê duyệt.**
- **ĐÃ CHỐT** người yêu cầu, người phê duyệt đối với thay đổi chuyên môn, và nguyên tắc lưu phiên bản/hiệu lực. **CHƯA CHỐT**: quyền tự hoàn tất thay đổi không ảnh hưởng kết luận/chỉ định, trạng thái yêu cầu, cơ chế hiện thực tạm giữ chỉ định theo QD-017, thời hạn duyệt, lý do bắt buộc, xử lý cấp bách, kiểm soát đồng thời, quyền sửa sau bàn giao, quy trình sai sót khi đã tiêm, DTO/schema/UI và kiểm thử.
- Chỉ ghi quyết định BA trên PR #63; không cho phép sửa code, SQL, migration, `SCOPE.md`, S5 đã nghiệm thu, S6 của Bảo hay S7 của Quý.

### QD-017 — A02: tạm giữ chỉ định liên quan khi có yêu cầu đính chính ảnh hưởng an toàn (03/11/2026)
- Người dùng **chốt phương án B**: khi có **yêu cầu đính chính đã gửi** có thể ảnh hưởng **an toàn hoặc hiệu lực chỉ định tiêm**, chỉ định của **những vắc xin liên quan** phải được **tạm giữ**, không được dùng để ghi mũi mới cho đến khi vấn đề được giải quyết hợp lệ. Không tự động tạm giữ mọi chỉ định không liên quan trong buổi khám; nếu chưa xác định chắc phạm vi ảnh hưởng phải xử lý an toàn, đánh giá và làm rõ trước khi cho phép tiêm.
- **Tạm giữ** là trạng thái hạn chế sử dụng trong khi chưa giải quyết rủi ro, **không phải vô hiệu hóa/hủy chỉ định** và không tự thay đổi kết luận chuyên môn, gói, giá hay số lượng đã tiêm. Chỉ định **bị vô hiệu hóa** theo QD-015 không được tự khôi phục như chỉ định cũ còn hợp lệ.
- Mốc có hiệu lực an toàn: **yêu cầu đính chính ảnh hưởng an toàn được API tiếp nhận hợp lệ và ghi nhận tạm giữ phải cùng một transaction**, không tồn tại khoảng thời gian yêu cầu đã ghi nhưng chỉ định còn được ghi tiêm. API ghi tiêm S5 phải **kiểm tra lại hiệu lực và tình trạng tạm giữ ngay khi ghi**, gồm xử lý tranh chấp đồng thời với thao tác đính chính. Không chỉ ẩn nút phía Windows.
- Khi Bác sĩ được ủy quyền **phê duyệt** đính chính ảnh hưởng kết luận/chỉ định theo QD-016, cần cập nhật phiên bản kết luận, hiệu lực chỉ định và trạng thái tạm giữ một cách nhất quán. **Từ chối hoặc rút yêu cầu không tự đồng nghĩa chỉ định an toàn**; chỉ bỏ tạm giữ khi đã có kết quả giải quyết và xác nhận chuyên môn hợp lệ theo quy trình được duyệt.
- **Không thay đổi/xóa/đảo ngược các mũi đã tiêm trước thời điểm tạm giữ**; nếu ghi tiêm được commit trước khi lệnh tạm giữ bắt đầu có hiệu lực, ghi nhận sự kiện lịch sử và xử lý tiếp theo bằng quy trình riêng, không sửa hồi tố.
- Mọi yêu cầu/tạm giữ/giải quyết phải có **người thực hiện, thời gian server, phiếu và phiên bản, chỉ định/vắc xin liên quan, lý do, trạng thái, quyết định phê duyệt và audit**. Không mặc định người tạo được tự gỡ giữ.
- **ĐÃ CHỐT nguyên tắc và phạm vi tạm giữ liên quan**, **CHƯA CHỐT**: các trạng thái và bảng/DTO cụ thể, cách xác định phạm vi ảnh hưởng khi không chắc, thẩm quyền bỏ giữ đối với từng tình huống, các kết quả sau từ chối/rút, idempotency và chiến lược khóa/transaction tại SQL. Đây là đề xuất thiết kế cần review và kiểm thử, không phải chức năng đã chạy.
- **CẢNH BÁO PHỤ THUỘC:** S5 trên main hiện ghi tiêm từ `PhieuKhamSangLoc` và `ChiDinhTiem` nhưng chưa có logic tạm giữ được duyệt. **Không triển khai tính năng gửi yêu cầu đính chính ảnh hưởng an toàn nếu API ghi tiêm chưa thể chặn hiệu lực chỉ định cùng transaction và chưa được nghiệm thu hồi quy S5.** Chỉ thêm quyết định BA trên PR tài liệu, không sửa code/SQL/`SCOPE.md` hoặc công việc S6/S7.

## B. Hiện trạng có căn cứ (không phải tính năng mới đã làm)

- Đề cương CNTT-KLCN090 nêu Windows quản lý loại tiêm chủng/loại vắc xin/vắc xin, đăng ký, lịch tiêm; Website giới thiệu, tìm kiếm, đăng ký. Phần nâng cấp mức C vượt chi tiết đề cương nên cần duyệt tác động.
- A04: `CatalogEndpoints` và WPF A04 đã CRUD. Entity `VacXin` có `LieuLuong`, `TheTich`, `PhanUngSauTiem`, `TrangThai`; tồn tại bảng `Benh`, `VacXin_Benh`, nhưng API contract/UI chưa khai thác đầy đủ. Không tạo cột trùng trước khi rà ý nghĩa.
- A02: `ClinicalEndpoints` ghi kết luận/chỉ định tối thiểu. `PhieuKhamSangLoc` có trường sinh tồn/tiền sử nhưng luồng tạo kết luận hiện chưa nhập và lưu các trường đó. WPF A02 chưa có bảng lịch sử tiêm hay engine quy tắc mức C.
- Không trình bày dữ liệu/vắc xin DEMO như thông tin y tế được xác minh; không tự suy diễn quy trình nội bộ VNVC.

## C. ĐANG ĐẶC TẢ / CẦN BỔ SUNG (chưa chốt, không được tự code)

1. **A02:** ĐÃ CHỐT lưu bản nháp SQL (QD-009), chuyển giao có audit giữa Bác sĩ (QD-010), và tiếp quản khi người phụ trách vắng mặt chỉ qua người quản lý có thẩm quyền phê duyệt (QD-011). ĐÃ CHỐT người phê duyệt là Bác sĩ phụ trách chuyên môn được ủy quyền thuộc actor Bác sĩ (QD-012); **cho phép xác nhận sớm Tạm hoãn/Chống chỉ định có căn cứ nhưng không tạo chỉ định, lưu rõ mục chưa đánh giá** (QD-013). ĐÃ CHỐT **một phiếu chung, kết quả theo từng vắc xin, chỉ tạo chỉ định được Bác sĩ duyệt** (QD-014); **đính chính theo phiên bản, giữ bản gốc, kiểm soát hiệu lực chỉ định và không thay đổi mũi đã tiêm** (QD-015). **CHƯA CHỐT**: cơ chế cấp/thu hồi quyền phê duyệt tại backend, ai gửi yêu cầu, chấp nhận của người tiếp nhận, chống tự duyệt, các bước và lý do/chứng cứ, phân quyền chi tiết, concurrency/lock, trạng thái phụ, timeout; ĐÃ CHỐT Bác sĩ phụ trách tạo yêu cầu đính chính và thay đổi kết luận/hiệu lực chỉ định cần Bác sĩ chuyên môn được ủy quyền duyệt (QD-016). ĐÃ CHỐT tạm giữ chỉ định liên quan khi yêu cầu đính chính ảnh hưởng an toàn, không cho ghi tiêm cho đến khi giải quyết hợp lệ (QD-017). **CHƯA CHỐT** chi tiết luồng đính chính không ảnh hưởng chỉ định, trạng thái yêu cầu, thẩm quyền gỡ giữ, cách phát hiện phạm vi rủi ro, concurrency và kiểm tra hiệu lực tích hợp S5; danh sách trường/validation chính xác cho sơ sinh, trẻ em, người lớn, thai phụ; cách biểu diễn Chưa hỏi/Không rõ/Không áp dụng; nguồn dữ liệu; bằng chứng lịch sử và thao tác xác nhận cảnh báo.
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
