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

## E. Thứ tự nghiệp vụ và phối hợp song song — quy tắc tiếp tục qua các phiên (03/11/2026)

> Mục này **nhắc lại và làm rõ QD-001, QD-008** cùng quy tắc bàn giao trong `AGENTS.md`/`SCOPE.md`; không phải quyết định QD mới, **không phải sổ tiến độ**, không cấp quyền sửa code hay mở rộng phạm vi. Khi có xung đột, `SCOPE.md` quyết định tính năng được triển khai, `AGENTS.md` quyết định cách phối hợp.

### E1. Phân biệt thứ tự quy trình khảo sát và thứ tự sửa hệ thống
- **Bảy quy trình nghiệp vụ đã chốt trong báo cáo** (không tự tách/gộp/đổi thứ tự): (1) nhập vắc xin từ nhà cung cấp; (2) đăng ký thông tin khách hàng/người được tiêm; (3) đăng ký dịch vụ tiêm chủng; (4) khám sàng lọc và chỉ định; (5) thanh toán; (6) thực hiện tiêm và theo dõi sau tiêm; (7) quản lý lịch tiêm và nhắc lịch. Đây là trình tự **mô tả nghiệp vụ**, không có nghĩa phải xây thêm module nhập kho đầy đủ hoặc buộc code/PR theo thứ tự 1–7.
- **Thứ tự rà soát và nâng cấp theo phụ thuộc dữ liệu đã chốt ở QD-001**: **A04 / danh mục loại vắc xin, vắc xin trước → cấu thành gói và thông tin công bố → kiểm tra ảnh hưởng tới hồ sơ/đăng ký dịch vụ/thanh toán hiện có → A02 khám sàng lọc/chỉ định (tiêu thụ dữ liệu A04) → phần tương thích ghi tiêm S5 khi cần → hồi quy/tích hợp**. Chỉ chỉnh những lát có vấn đề hoặc có phạm vi được giao; S3/S4/S5 đã đạt tiêu chí thì không viết lại vì xuất hiện trong sơ đồ.
- Với A04: trước tiên rà soát các class/bảng/API/Windows/Web hiện có, loại trùng, dữ liệu thiếu và hợp đồng A04→A02 theo QD-002–005; sau đó mới chốt lát nhỏ để phát triển. Không nhảy sang sửa A02 trước khi thống nhất dữ liệu và hợp đồng A04 cung cấp. Những rule y khoa chưa có chứng cứ/kiểm duyệt chỉ là DEMO, không có quyền tự quyết định chuyên môn.

### E2. Không phải đợi S6/S7/S8 mới được tiếp tục A04
- **Được làm song song**: Đạt/phiên Codex riêng có thể **rà soát, đặc tả, kiểm thử đọc-only A04 ngay** trong lúc Bảo làm S6, Quý làm S7 và nhóm chuẩn bị S8. Các sửa lỗi **đã nằm trong lát được giao/phạm vi hiện hành** có thể xử lý bằng PR riêng, không cần chờ S6/S7/S8; nếu là tính năng **mở rộng ngoài SCOPE** thì phải có phê duyệt phạm vi và gói triển khai riêng trước khi sửa code/SQL.
- Đừng hiểu **ưu tiên hoàn tất S6/S7/S8 trước hạn nộp** là phụ thuộc bắt buộc để *bắt đầu* phân tích A04. S8 có thể kiểm tra từng phần đã tích hợp; **nghiệm thu S8 cuối cùng cần chạy lại trên phiên bản tích hợp thực tế** sau các PR liên quan.
- **Ranh giới song song**: Bảo sở hữu S6/Registration/Payment liên quan, Quý sở hữu S7/Reports; không sửa file/nhánh/PR của họ. Các file dùng chung, API contract, migration và tích hợp S5 phải có điều phối/review chéo; **một migration tại một thời điểm**, không dùng chung DB test đang có người kiểm. S5 đã nghiệm thu bị đóng băng, chỉ mở qua gói sửa lỗi/tương thích được giao rõ và kiểm thử hồi quy.
- Đối với phần A04/A02 còn ở trạng thái thiết kế theo QD-008, ghi kết luận/đề xuất trong chat hoặc sổ BA; **không phát lệnh Codex viết code** cho tới khi có gói triển khai được phê duyệt.

### E3. Điểm vào bắt buộc của phiên ChatGPT/Codex mới
1. Đọc `README.md`, `AGENTS.md`, `SCOPE.md` ở `main`; đọc `BUSINESS_DECISIONS.md` trên **nhánh `docs/business-decisions-20261103` (Draft PR #63)** cho đến khi PR được merge. Không được ngầm cho rằng file trên Draft PR đã nằm trong `main`.
2. Kiểm tra trạng thái mới nhất của module/PR và owner trước khi giao việc. Nếu có thay đổi trong nguồn sau lần ghi sổ, **đối chiếu lại**, không dùng trạng thái cũ làm chứng cứ.
3. Tóm tắt cho người dùng **module đang làm, module đã đóng băng, phần còn chờ, phạm vi file, điểm giao và test**. **Không tự tạo QD-018 hoặc đổi thứ tự** vì một đoạn chat vừa phân tích sâu A02.
4. Tiếp tục **A04 trước A02** đối với kế hoạch nâng cấp. Các quyết định QD-009–017 về A02 đã được bảo lưu, **không phải chỉ thị chuyển sang thực hiện A02 trước**.
5. Cập nhật **tiến độ chỉ ở cột Trạng thái trong `SCOPE.md` khi được phép**, không mở thêm sổ tiến độ hay file thiết kế riêng. Tài liệu này chỉ ghi quyết định BA và quy tắc điều phối; giữ Draft PR cho tới khi người dùng duyệt.

**Câu mở đầu dùng lại khi chuyển chat/Codex:** `Đọc README.md, AGENTS.md, SCOPE.md ở main và BUSINESS_DECISIONS.md tại docs/business-decisions-20261103 (PR #63). Tuân thủ mục E: nâng cấp theo phụ thuộc nghiệp vụ, A04 trước A02; S6/S7/S8 được chạy song song với ranh giới owner; chưa sửa code hoặc mở rộng SCOPE khi chưa có gói được duyệt. Trước khi làm, báo trạng thái và đề xuất đúng một lát tiếp theo.`


### E4. Rà soát toàn bộ chuỗi nghiệp vụ và các chức năng liên quan (03/11/2026) — NHẬN XÉT, CHƯA CHỐT THAY ĐỔI
> **Tránh thiên lệch A04/A02:** mọi lần rà soát hệ thống phải đi qua **đủ 7 quy trình BA** đã chốt và các chức năng đề cương (danh mục, gói, khuyến mãi, báo cáo, Web/Windows, quyền, kiểm thử); không mặc định làm thêm tính năng cho mọi chỗ trống. Nội dung này ghi **hiện trạng quan sát được trên main/SCOPE tại thời điểm rà**, không phải bảng tiến độ, không thay cột Trạng thái trong `SCOPE.md`. Trước khi thực hiện phải kiểm tra trạng thái nhánh/PR mới nhất.

| Quy trình BA trong Chương 1 | Nghiệp vụ cần rà | Liên hệ hiện trạng/main | Vấn đề cần đối chiếu (không tự thêm phạm vi) |
|---|---|---|---|
| (1) Nhập vắc xin từ nhà cung cấp | Nhà cung cấp → nhận/lô/hạn/tồn → sử dụng | S5 chỉ có lô/tồn synthetic, xuất theo ghi mũi; **không có module nhập kho đầy đủ trong SCOPE** | Phải phân biệt quy trình khảo sát/UML với chức năng demo; không gọi seed là nhập kho thật. Xem nguồn lô, số lô/hạn dùng/biến động, xử lý trường hợp vắc xin có sẵn tại khu vực tiêm; nếu cần nhập kho đầy đủ phải xin mở phạm vi |
| (2) Đăng ký thông tin khách hàng/người được tiêm | Định danh, nhiều người được tiêm, chủ hồ sơ, cập nhật, phân quyền riêng tư | S3 hồ sơ qua API/SQL Web/Windows đã nghiệm thu theo SCOPE | Rà liên kết khách hàng/người được tiêm và tác động sửa hồ sơ lên lịch/tiêm đã ghi; không làm lại toàn bộ S3 |
| (3) Đăng ký dịch vụ tiêm chủng | Tra cứu danh mục/vắc xin/gói, chọn số lượng, snapshot giá, đăng ký | S2 danh mục/gói và S4 đăng ký đã có; Web cho khách, Windows cho nhân viên | Rà ý nghĩa danh mục/loại vắc xin/gói; thành phần và số lượng gói, snapshot giá, việc thay đổi danh mục sau đăng ký; **A04 là một mắt xích, không phải toàn bộ nghiệp vụ** |
| (4) Khám sàng lọc và chỉ định | Lịch/đối tượng, phiếu khám, căn cứ, kết luận, chỉ định theo vắc xin | S5 A02 tối thiểu đã có; QD-009–017 là BA mở rộng chưa code | Rà đúng điều kiện của từng đối tượng, phân quyền Bác sĩ, chỉ định gắn đúng người/lịch/vắc xin và cổng ghi tiêm. Không tạo quyết định y khoa tự động |
| (5) Thanh toán dịch vụ | Tổng phải thu, ưu đãi, người ghi nhận, thanh toán, trạng thái | S4 thanh toán **mô phỏng toàn bộ một lần** đã có; S6 ưu đãi do Bảo phụ trách | Rà nhất quán snapshot số tiền từ đăng ký qua S6→Payment→S5; không tự sửa payment/đổi tổng 0/áp giá mới lên đăng ký cũ |
| (6) Thực hiện tiêm và theo dõi sau tiêm | Chỉ định → kiểm lô/tồn/quyền/thanh toán → ghi mũi → theo dõi | S5 đã nghiệm thu, với transaction mũi+trừ tồn+biến động+audit; theo dõi một lần mỗi mũi | Rà điều kiện trước khi ghi và dữ liệu sau tiêm; không sửa lịch sử mũi đã tiêm; xem trường hợp có vắc xin ngay khu vực tiêm trong đặc tả nghiệp vụ, không ép nhân viên kho cho mọi ca |
| (7) Quản lý lịch tiêm và nhắc lịch | Tạo lịch từ đăng ký, xem lịch và lịch sử, gửi/ghi nhận nhắc lịch | S5 lập lịch, Web xem lịch, nhắc qua **log mô phỏng**; không có scheduler hay SMS/email thật | Rà quan hệ lịch với dòng đăng ký/gói, thời điểm/giờ địa phương, nhắc trước lịch chưa tiêm; phân biệt log nội bộ với thông báo thật |

**Chức năng nằm ngoài bảy quy trình mô tả nhưng vẫn phải rà theo đề cương:** 
- **Nền/quyền S0–S1**: đăng nhập, đổi mật khẩu, phân quyền và giới hạn truy cập trên hai client/API.
- **Danh mục, gói A04/S2**: dữ liệu nền, cấu phần gói, nội dung Web, không tự coi dữ liệu demo là nguồn y khoa.
- **Khuyến mãi S6 (Bảo)**: giảm VND hợp lệ, một ưu đãi, snapshot khi đăng ký, tác động thanh toán — chưa được tự động sửa module chủ sở hữu.
- **Thống kê/báo cáo S7 (Quý)**: doanh thu từ thanh toán thành công, mũi từ `MuiTiem` thật, đăng ký theo trạng thái, không nhân đôi do JOIN.
- **S8 + tài liệu**: đối chiếu Use Case/Activity/Sequence/class/database với luồng chạy thực tế; Web↔API↔SQL↔Windows; kiểm quyền, lỗi mạng, lưu/đọc lại, rollback/đồng thời tại điểm tiền/tồn. S8 có thể kiểm dần theo module; **nghiệm thu tích hợp cuối phải kiểm lại sau các PR tương thích**.

**Sửa cách hiểu thứ tự:** bảy mục BA là các **đầu mục khảo sát/đặc tả**, không có nghĩa các API chạy tuyến tính 1→2→3→4→5→6→7. Trên main, từ đăng ký S4 có thể tạo lịch S5 và ghi kết luận A02; thanh toán S4 cũng từ đăng ký, còn **ghi tiêm** mới yêu cầu thanh toán đạt điều kiện, chỉ định hợp lệ và lô hợp lệ. Nhắc lịch là nhánh xuất phát từ lịch **trước khi tiêm**; báo cáo đọc xuyên suốt. Đề cương khảo sát mô tả khám trước thanh toán cần đối chiếu với **luồng hệ thống hiện chạy**, không tự đổi code để khớp thứ tự liệt kê hay khẳng định đây là quy trình nội bộ chính thức VNVC.

**Thứ tự rà soát toàn diện gợi ý (không phải lệnh triển khai tuần tự):** (i) nguồn dữ liệu và danh mục/gói; (ii) hồ sơ; (iii) đăng ký & snapshot giá; (iv) ưu đãi & thanh toán; (v) lịch & nhắc lịch; (vi) khám/chỉ định; (vii) nguồn lô/tồn và ghi tiêm/theo dõi; (viii) báo cáo; (ix) phân quyền, Web/Windows, UML, kiểm thử tích hợp. **Không bỏ sót nhóm chỉ vì chat đi sâu A02.**

**Quy tắc đánh giá không chiều theo mọi ý tưởng:** mỗi đề xuất đánh dấu `(A) lỗi/tồn tại trong phạm vi`, `(B) thiếu chức năng thuộc đề cương/SCOPE`, `(C) tính năng mở rộng vượt SCOPE`, hoặc `(D) chỉ có trong khảo sát/UML`. Loại C/D không mặc định giao Codex code. Trước khi đề xuất PR phải nêu căn cứ, tác động, người sở hữu, mức ưu tiên, rủi ro và phép thử.

### E5. Kết quả đối chiếu tĩnh đợt 1 và các phép thử cần kiểm — danh mục/gói → hồ sơ → đăng ký → thanh toán (03/11/2026)

> **Bản ghi phân tích nghiệp vụ, KHÔNG phải sổ tiến độ hoặc nghiệm thu mới.** Đối chiếu code trên `main` với `SCOPE.md`; **chưa chạy API/SQL/UI test mới**, chưa đọc/đối chiếu hết nhánh riêng của Bảo/Quý. Không tự cập nhật cột Trạng thái `SCOPE.md` hoặc mở thêm tính năng.

| Điểm rà | Bằng chứng mã nguồn đã xem | Đánh giá, rủi ro và thứ tự xử lý |
|---|---|---|
| Danh mục/gói | `backend/src/TiemChung.Api/CatalogEndpoints.cs` (CRUD, lọc, kiểm quyền, giá, gói; `PUT /packages` chặn sửa thành phần khi gói đã có trong đăng ký); `backend/src/TiemChung.Contracts/CatalogContracts.cs` | **Đã có nền tảng S2**, không làm lại. Các trường chuyên môn `LieuLuong`, `TheTich`, `PhanUngSauTiem` đã tồn tại trong entity `VacXin` nhưng DTO danh mục chưa khai thác; nâng cao có kiểm duyệt thuộc **(C) mở rộng A04**, không cấp quyền tự code. Test sau: thay giá/đổi thành phần sau khi có đăng ký, dữ liệu khách nhìn thấy và giá snapshot |
| Hồ sơ | `backend/src/TiemChung.Api/ProfileEndpoints.cs` (thêm, sửa, xóa có kiểm quyền, scope và audit; CCCD khách hàng chống trùng; người được tiêm gắn khách hàng) | **Đã có S3**, không viết lại. Phát hiện **(A) P2: thông báo validation số điện thoại ghi “tối đa 15 ký tự” trong khi kiểm 10–11 chữ số** (cùng file, `ValidateCustomer`); chỉ sửa bằng gói nhỏ có owner/review, không tự sửa. Kiểm tiếp hồ sơ sau khi có lịch/tiêm, liên kết khách/người được tiêm và UI lỗi mạng |
| Đăng ký dịch vụ | `backend/src/TiemChung.Api/RegistrationEndpoints.cs` (kiểm liên kết khách/người được tiêm, quyền, vaccine/package Active, đối chiếu `ExpectedUnitPrice` và `ExpectedComponents`, giá dòng `GiaTaiThoiDiemDangKy`, transaction/audit); `RegistrationContracts.cs` | **Đã có snapshot đơn giá S4**, tuyệt đối không giao làm lại. **(B) kiểm thử tích hợp** xem giá/đăng ký cũ sau đổi danh mục và chặn package component; `Detail` đọc thành phần từ `GoiVacXin_VacXin` hiện hành nhưng hiện có chặn thay đổi sau khi gói đã được tham chiếu, do đó **chưa có bằng chứng lỗi** |
| Thanh toán | `backend/src/TiemChung.Api/PaymentEndpoints.cs`, `PaymentContracts.cs` (`ThuNgan`, thu toàn bộ một lần, transaction, uniqueness, audit, kiểm tổng, không ghi trùng); `TiemChungDbContext.cs` unique index `ThanhToan.MaDangKy` | **Đã có contract S4**, không viết lại. `Summaries` hiện từ chối `MaKhuyenMai != null` là **ràng buộc của contract chưa có S6**, không phải bug đã chứng minh. **(B) P1 điểm giao S6→Payment→S5:** chỉ mở PR tương thích sau khi Bảo chốt cách tính/snapshot và có review. Không thay đổi tổng 0 hoặc nới điều kiện ghi tiêm khi chưa chốt |
| Dữ liệu và ranh giới | `SCOPE.md`, `AGENTS.md`, đề cương đề tài | S6 thuộc **Bảo** (Promotion + Registration/Payment giao thoa); S7 thuộc **Quý** (báo cáo read-only); Đạt không tự chỉnh branch/PR người khác; file dùng chung/contract/migration chỉ sửa có review, **một migration tại một thời điểm**. Các mở rộng A04/A02 vẫn ở trạng thái thiết kế cho đến khi người dùng/nhóm duyệt thay phạm vi |

**Bộ kiểm chứng cần thực hiện sau này (không đánh dấu PASS khi chưa chạy):**
1. `T01`: Giá và thành phần gói hiển thị tại thời điểm chọn; nếu catalog thay trước khi đăng ký, API trả xung đột và không tạo đăng ký.
2. `T02`: Gói đã được đăng ký không cho thay thành phần trái quy tắc; đơn giá trên đăng ký cũ không thay theo giá danh mục mới.
3. `T03`: Tài khoản khách không thể đọc/ghi hồ sơ hoặc đăng ký thuộc khách khác; người được tiêm phải thuộc đúng khách hàng của đăng ký.
4. `T04`: Thu toàn bộ thành công chỉ một lần; request đồng thời/lặp không tạo hai thanh toán hoặc audit thành công giả.
5. `T05`: Trường hợp ưu đãi S6 (khi có contract) tính **giá sau giảm nhất quán** ở Registration → Payment → S5; không giả lập trạng thái PASS từ main chưa tích hợp.
6. `T06`: Xóa/sửa hồ sơ đã tham chiếu không làm mất lịch, chỉ định, mũi tiêm; phản hồi lỗi chính xác.
7. `T07`: Xác minh thông báo điện thoại và UI Web/Windows; không thay đổi quy tắc số điện thoại theo suy đoán trước khi chốt.

**Điểm tiếp tục trong vòng rà soát toàn diện (E4), theo thứ tự và KHÔNG phụ thuộc phải chờ S6/S7:**
- **Đợt 2:** lịch hẹn & nhắc lịch (Q07) → khám/chỉ định tối thiểu (Q04) → ghi tiêm, lô/tồn và theo dõi sau tiêm (Q06); chỉ đọc code S5, bảo toàn các test đã nghiệm thu.
- **Đợt 3:** thống kê/báo cáo S7 (phối hợp owner Quý, chỉ đọc chưa sửa) → phân quyền S1, Web/Windows → rà UML/báo cáo ↔ code ↔ SCOPE → ma trận kiểm thử S8.
- Khi phát hiện lỗi thực tế, tách **lỗi đã có chứng cứ** khỏi **rủi ro cần kiểm** và **tính năng mới ngoài phạm vi**; chỉ đề xuất **một lát sửa có owner, file, test, tác động** sau khi có căn cứ. Không thêm QD-018 nếu người dùng chưa chốt quyết định nghiệp vụ mới.


### E6. Đối chiếu tĩnh đợt 2 — lịch hẹn/nhắc lịch → khám/chỉ định → ghi tiêm/kho/theo dõi (03/11/2026)

> Tiếp tục **rà soát tĩnh trên `main`**, không sửa các module S5 đã nghiệm thu và không tự coi đã chạy kiểm thử mới. Báo cáo **rủi ro khi tích hợp thay đổi tương lai** khác với kết luận phát hiện lỗi đang chạy. Dùng các mã ca thử chỉ để chuẩn bị đối chiếu với test có sẵn sau này.

| Nhóm nghiệp vụ | Căn cứ code | Kết luận và các điểm phải giữ |
|---|---|---|
| Lịch hẹn | `AppointmentEndpoints.cs` POST `/api/v1/appointments`: quyền `LeTan` + `QuanLyLichHen`, thời gian tương lai, liên kết đúng dòng đăng ký/đúng người, ngăn trùng cùng nguồn và thời điểm, lock đăng ký + transaction/audit | **Đã có S5**, không làm lại. Chưa có bằng chứng thiếu phạm vi. Test tương lai: lịch gắn vắc xin lẻ hoặc gói; thời gian UTC ↔ UTC+7; hai request trùng; quyền khách và liên kết khác chủ. Không tự suy ra lịch phác đồ từ danh mục |
| Nhắc lịch | `ReminderEndpoints.cs`: `Channel=LogMoPhong`, chỉ một log cho một lịch, có nội dung ghi rõ **chưa gửi khách**, chỉ ghi cho lịch tương lai chưa có mũi; khóa lịch và audit | **Đúng phạm vi mô phỏng S5.** Không tuyên bố đã gửi Email/SMS; những kênh ngoài SCOPE thuộc (C). CẦN RÀ: lịch có nhiều vắc xin nhưng mới tiêm một phần, cách hiểu “đã tiêm” cho nhắc lịch là cấp lịch hay cấp mũi? Chưa xác nhận là bug, cần đối chiếu yêu cầu đã giao |
| Khám/chỉ định tối thiểu | `ClinicalEndpoints.cs`, `ClinicalContracts.cs`: actor `BacSi`, một phiếu `PhieuKhamSangLoc` theo lịch, chỉ định `ChiDinhTiem` cho các `VaccineIds` được duyệt, không tạo chỉ định khi kết luận Tạm hoãn/Chống chỉ định | **Đã có S5**; QD-009–017 là (C) mở rộng BA, không được tự sửa schema/UI. Phải giữ hành vi “không có chỉ định khi Tạm hoãn/Chống chỉ định” và không tự động kết luận |
| Ghi tiêm và tồn | `VaccinationEndpoints.cs`: actor `DieuDuong`, kiểm nguồn chỉ định/lịch/đăng ký, payment, lô usable, khóa chỉ định/đăng ký/lịch, trừ tồn điều kiện, ghi `MuiTiem` + `BienDongKho` + audit cùng transaction | **S5 đã nghiệm thu theo S4**; không tự mở lại. P1 **điểm giao S6**: tổng sau giảm phải tương thích Payment và không nới điều kiện an toàn. P1 **điểm giao A02 tương lai**: chỉ định bị tạm giữ hoặc vô hiệu hóa phải chặn ghi tiêm tại server cùng kiểm soát tranh chấp |
| Theo dõi sau tiêm | `AftercareEndpoints.cs`: actor `DieuDuong`, một bản `TheoDoiSauTiem` liên kết `MuiTiem`, chống trùng, audit. Nguồn danh sách đang dùng `VaccinationEndpoints.Sources(db)` | **RỦI RO TÍCH HỢP TƯƠNG LAI**, không phải lỗi S5 đã chứng minh: nếu A02 bổ sung lọc chỉ định mất hiệu lực vào `VaccinationEndpoints.Sources` rồi `AftercareEndpoints.Sources` tái sử dụng cùng bộ lọc, **mũi đã tiêm trước đính chính có thể bị ẩn khỏi theo dõi**. Phải tách kiểm tra “được phép GHI TIÊM MỚI” khỏi “đã tiêm và CẦN THEO DÕI” |
| Lịch sử khách hàng | `VaccinationHistoryEndpoints.cs` có chú thích rõ truy vấn lịch sử không áp lại điều kiện chỉ định/thanh toán/lô **ở thời điểm hiện tại** | **Thiết kế đã tránh đúng rủi ro hồi tố**; giữ nguyên nguyên tắc khi A02 thay đổi và kiểm thử lại với chỉ định bị vô hiệu hóa |
| Nguồn lô/tồn | `DevelopmentInventorySeed.cs`: lô synthetic `[DEMO]` chỉ dùng Development, được tạo bằng luồng seed riêng, không phải phiếu nhập thực tế | **(D) quy trình nhập kho đầy đủ trong báo cáo** chưa nằm trong phạm vi hiện thực S5; không đánh đồng seed với nhập hàng nhà cung cấp, không tự tạo module nhập kho khi thiếu phê duyệt |

**Ca thử tích hợp cần đối chiếu với suite S5 trước khi tuyên bố PASS:**
1. `L01` tạo lịch đúng dòng đăng ký, cả vắc xin lẻ/gói; thời gian theo múi giờ; không tạo hai lịch trùng nguồn/thời điểm khi concurrent.
2. `L02` log nhắc lịch chỉ tạo một lần, không ghi khi đã quá giờ hoặc đã có mũi; hiển thị rõ “mô phỏng, chưa gửi khách”.
3. `C01` không tạo `ChiDinhTiem` khi Tạm hoãn/Chống chỉ định; `Đủ điều kiện` chỉ tạo ID được Bác sĩ chọn.
4. `V01` thiếu thanh toán/nhầm người/nhầm vaccine/lô hết hạn hoặc hết tồn/khác role đều bị từ chối; ghi thành công thì đúng một mũi, một xuất tồn, một audit.
5. `V02` đồng thời tranh lô cuối và hai request cùng chỉ định không ghi vượt tồn/ghi trùng.
6. `F01` mũi đã ghi luôn còn trên lịch sử và màn theo dõi dù **về sau** kết luận/chỉ định bị đính chính/vô hiệu hóa (là **test thiết kế cho A02 tương lai**, chưa được chạy với S5 hiện hành).
7. `F02` không xóa hoặc ghi đè mũi đã tiêm trong bất kỳ thao tác đính chính A02 nào; audit và số lô giữ nguyên.

**Hướng rà soát kế tiếp, vẫn theo E4 và không đợi S6/S7 hoàn tất:** (1) S7/Reports theo code nhánh có thể truy cập và contract của Quý — không sửa code người khác; (2) S1 phân quyền/vùng truy cập, API/Windows/Web xuyên chuỗi; (3) đối chiếu đề cương + bảy Activity/UC/Sequence với thực tế; (4) lập test matrix S8 cuối. Khi phát hiện giao thoa API/contract/schema, chỉ đề xuất thay đổi riêng kèm owner, kiểm thử hồi quy và yêu cầu phê duyệt phạm vi.


### E7. Đối chiếu đợt 3 — S7, quyền và hai client; kiểm chứng tích hợp còn phải làm (03/11/2026)

> **Chỉ đọc mã nguồn** `main` và các **nhánh S7 của Quý** để nắm tiến độ phụ thuộc. Nhánh có code **không đồng nghĩa đã merge, chạy test hoặc nghiệm thu**. Không sửa nhánh, tác giả, file hoặc PR của Quý; không tự cập nhật trạng thái S7 trong `SCOPE.md`.

| Nhóm | Căn cứ được kiểm | Kết luận và các phép thử còn cần |
|---|---|---|
| S7 báo cáo — không bị bỏ quên | Trên `main` chưa có `MapReportEndpoints()`. Đã đọc `ReportEndpoints.cs` và `Program.cs` ở các nhánh `feat/s7-1-monthly-revenue`, `feat/s7-2-vaccine-doses`, `feat/s7-3-registration-status`. Nhánh cuối có ba GET `/monthly-revenue`, `/vaccine-doses`, `/registration-statuses` và quyền `XemBaoCao` | **Có triển khai trên nhánh**, chưa chứng minh merge/CI/Windows UI/SQL test; không gọi là “chưa làm”. Doanh thu lấy các `ThanhToan.TrangThai=ThanhCong` và cộng `SoTien`, không JOIN qua các dòng dịch vụ; liều đếm `MuiTiem.TrangThai=DaTiem`; trạng thái đăng ký nhóm theo giá trị đang có; khoảng ngày quy về UTC+7. Kiểm tiếp **owner Quý**: lọc ngày, quyền, dữ liệu 0, tổng chính xác, trùng JOIN, UI Windows, PR và test |
| S1 + bảo mật | `IdentityEndpoints.cs` trên `main`: đăng ký, xác minh email, đăng nhập, refresh, logout, đổi mật khẩu, `/me`; các endpoint nghiệp vụ tự kiểm chức danh/quyền/ownership | S1 vẫn có phần nghiệm thu chưa đóng theo `SCOPE.md`. **Chưa audit sâu** hash/session/refresh/CSRF và WPF login; không tự kết luận PASS. Kiểm quyền trên API thực và dữ liệu hồ sơ ngoài phạm vi tài khoản trước khi tuyên bố toàn hệ thống an toàn |
| Website | `apps/web/src/App.jsx`, `Catalog.jsx`, `Registrations.jsx`, `Payments.jsx`, `VaccinationHistory.jsx` (đều có trên `main`) | Có các màn giới thiệu/tìm kiếm danh mục, đăng ký, xem thanh toán mô phỏng và lịch sử tiêm; không tự nhận UI đầy đủ ở mọi kích thước hoặc mọi lỗi mạng. Test Web→API→SQL có đăng nhập, phân quyền, lỗi mạng, giá/tiền, đối chiếu Windows |
| Windows / vai trò nhân viên | Giao diện A02 `ScreeningViewModel.cs` đã đọc; S5/S2/S3/S4 được ghi trong `SCOPE.md` với một số giới hạn kiểm thử UI thực | **Chưa đọc hết toàn bộ ViewModels/ViewsV2 trong đợt này**; cần rà từng actor và thao tác thực từ WPF, không chỉ dựa trên nút hoặc trạng thái mô phỏng. Giữ nguyên thành quả đã nghiệm thu, không dùng audit này làm lý do refactor UI diện rộng |
| Đề cương/UML/S8 | Đề cương CNTT-KLCN090 yêu cầu Windows + Website + API + kiểm thử và bộ UML/thiết kế dữ liệu. `SCOPE.md` xác định 7 quy trình BA trong nội dung khảo sát và S8 kiểm tích hợp | **CẦN ĐỐI CHIẾU** tài liệu chương 1, Use Case, Activity, Sequence, ERD và demo: phân biệt khảo sát (7 quy trình) với phần code thực hiện (S0–S8). Luồng khảo sát có nhập kho đầy đủ không được báo như chức năng runtime nếu chỉ có seed lô |

**S8 — checklist xuyên module nên lập/chạy trên bản tích hợp, chưa có kết quả mới:**
- `I01` khách hàng đăng nhập → tạo hồ sơ và người được tiêm → chọn vắc xin/gói → đăng ký → đọc lại SQL và Web/Windows thấy cùng dữ liệu.
- `I02` giá danh mục thay đổi sau khi có đăng ký → đơn giá snapshot không bị thay; S6 nếu đã merge chốt số tiền sau giảm, payment ghi đúng một lần, S5 kiểm đúng tổng.
- `I03` Lễ tân tạo lịch từ đăng ký → Bác sĩ chỉ định đúng sản phẩm → Điều dưỡng ghi mũi bằng lô hợp lệ → tồn và `BienDongKho` nhất quán → theo dõi → lịch sử khách thấy mũi đã tiêm.
- `I04` nhắc lịch log mô phỏng trên lịch tương lai chưa tiêm, không suy diễn đã gửi SMS/Email.
- `I05` S7 (khi được review và tích hợp) thống kê đúng thanh toán thành công, mũi đã ghi, đăng ký theo trạng thái; quyền `XemBaoCao` và bộ lọc khoảng ngày Việt Nam.
- `I06` kiểm chéo quyền Khách hàng / Lễ tân / Bác sĩ / Điều dưỡng / Thu ngân / nhân viên kho, kể cả ID đoán được và quyền trên API, không chỉ trên client.
- `I07` lỗi mạng/timeout sau commit: GET đối chiếu, không tạo đăng ký, thanh toán, mũi tiêm hoặc ghi theo dõi trùng; kiểm rollback/tranh tồn cuối.
- `I08` đối chiếu màn demo và sơ đồ UML với từng phần **đã triển khai / chỉ khảo sát / hướng phát triển**; tránh khẳng định quá mức.

**Cách nối bước tiếp theo (không cần xin lại lựa chọn thứ tự):** rà chi tiết S7 khi Quý có PR/CI và đối chiếu Windows role views, sau đó lấy **kết quả test S8 thực** để xác định lỗi thuộc phạm vi (A/B), giao owner và mở PR nhỏ. Các ý tưởng A04/A02 mở rộng loại (C) vẫn nằm trong quyết định BA, không tự động triển khai. Mọi lần tiếp tục phải kiểm `main`, nhánh/PR và trạng thái `SCOPE.md` mới nhất; không suy rằng các phát hiện đọc tĩnh là kết quả QA runtime.

### E8. Checklist điều phối ngắn và điểm dừng bắt buộc (03/11/2026) — DÙNG KHI TIẾP TỤC CHAT/CODEX

> **Đây là checklist cho công việc RÀ SOÁT, không thay cho cột `Trạng thái` của `SCOPE.md` và KHÔNG xác nhận module đã nghiệm thu.** Mọi dấu [x] bên dưới chỉ nghĩa là **đã đọc/đối chiếu tĩnh** và ghi phát hiện ở E5–E7. Test thực chưa chạy phải để [ ]. Trước mỗi phiên, đọc `AGENTS.md`/`SCOPE.md` ở `main`, `BUSINESS_DECISIONS.md` tại nhánh PR #63; sau đó kiểm lại nhánh và PR mới.

**Mục tiêu cố định:** Rà đủ **7 quy trình nghiệp vụ** và các chức năng đi kèm, xác định lỗi có căn cứ, kiểm giao thoa S6→S5, chuẩn bị test S8; **không** đào sâu A04/A02 chỉ vì đã có nhiều QD, không xây tính năng C/D ngoài `SCOPE.md` nếu chưa duyệt.

#### Checkpoint khảo sát đọc code — không phải PASS/FAIL runtime
- [x] **R1. Danh mục/vắc xin/gói**: đã đọc Catalog/Contracts/EF và xác minh CRUD, giá, giới hạn đổi cấu phần gói; **chưa** kiểm DB thực hoặc dữ liệu y khoa nguồn (E5).
- [x] **R2. Hồ sơ khách hàng/người được tiêm**: đã đọc Profile/Registration scope, validation/ownership; phát hiện thông báo điện thoại 10–11 khác “tối đa 15” (E5), **chưa chạy test mới**.
- [x] **R3. Đăng ký + tiền**: đã đọc Registration/Payment; có snapshot đơn giá và payment tổng một lần, contract S6 khuyến mãi đang là điểm giao; **chưa chạy test mới** (E5).
- [x] **R4. Lịch + nhắc lịch**: đã đọc Appointment/Reminder, log chỉ **mô phỏng**, chưa gửi khách; cần test UTC+7/trùng lịch (E6).
- [x] **R5. Khám/tiêm/kho/theo dõi**: đã đọc Clinical/Vaccination/Aftercare/History/Development seed; S5 đóng băng. **Phát hiện nguy cơ cho A02 tương lai:** lọc chỉ định vô hiệu có thể làm ẩn mũi đã tiêm khỏi Aftercare nếu tái dùng nguồn lọc (E6).
- [x] **R6. S7 báo cáo (đọc nhánh, không phải nghiệm thu)**: `main` còn `SampleReportService` ở Windows, `ReportViewModel.CanViewReports => true`; nhánh `feat/s7-3-registration-status` có `ReportApiService`, 3 báo cáo, quyền `XemBaoCao` tại API. **Nhánh đã có code không chứng minh PR merge hoặc test PASS**. Cần review hiển thị DEMO, kiểm role và SQL thật (E7).
- [x] **R7. Bản đồ Web + Identity mức khái quát**: đã đọc App/Web các màn danh mục, đăng ký, thanh toán, lịch sử; IdentityEndpoints có đăng nhập/refresh/logout/password; **chưa audit toàn bộ security hoặc UI thật** (E7).
- [x] **R8. Windows mức ViewModel/role sơ bộ**: đã đọc ShellViewModel, Screening/Catalog/Payment/Vaccination/Report; main Report vẫn dùng `SampleReportService`; phần khác có API/gating. **Chưa xem toàn bộ ViewsV2, route/UX và chạy app**; chưa xác nhận toàn bộ 6 vai trò thao tác thành công.

#### Việc còn mở — mỗi lần tiếp tục CHỈ CHỌN MỘT mục có giới hạn
- [ ] **N1 (ngay kế tiếp):** rà **Windows sáu vai trò** bằng cách đọc navigation/menu, ViewModel + service *các màn còn thiếu*, lập ma trận actor→màn→đọc/ghi→API. **Giới hạn:** chỉ lập bảng luồng còn thiếu; không đi sâu refactor UI hay thiết kế lại toàn bộ; không tuyên bố đã chạy WPF.
- [ ] **N2:** xác minh **trạng thái PR/CI/S6 của Bảo và S7 của Quý** bằng thông tin PR mới nhất hoặc phản hồi owner, không suy từ nhánh đơn lẻ. Kiểm contract ưu đãi/số tiền chỉ khi thực sự có code/PR; phân loại điểm giao Registration→Payment→S5.
- [ ] **N3:** đối chiếu **7 nghiệp vụ khảo sát ↔ UML/báo cáo ↔ phạm vi/demo S0–S8**; không ép quy trình nhập kho đầy đủ vào code khi `SCOPE.md` không có.
- [ ] **N4:** kiểm thử **S8 end-to-end trên bản tích hợp thật**, gồm Web→API→SQL→Windows, quyền, snapshot giá, thanh toán/ưu đãi, lịch→bác sĩ→điều dưỡng→theo dõi, báo cáo, thời gian, đồng thời/tranh tồn, rollback; chỉ đánh dấu PASS sau khi có bằng chứng chạy test.
- [ ] **N5:** khi N1–N4 đủ căn cứ, **tổng hợp danh sách lỗi/thiếu sót theo A/B/C/D**, ưu tiên P0/P1 và một PR nhỏ đúng owner cho từng lỗi, không làm một cục. Nếu không có lỗi thật, ghi **“chưa chứng minh lỗi”**, không tự tạo việc.

**Điểm dừng để chống sa đà:**
1. Mỗi lượt **một nhóm rà soát**, nêu tối đa **3–5 phát hiện quan trọng** và test đề xuất; nếu không có lỗi có bằng chứng, **chuyển nhóm**, không đào tiếp vô hạn.
2. Chỉ tạo QD mới khi người dùng chốt **quyết định nghiệp vụ mới**; các checkpoint R/N và ghi nhận code chỉ cập nhật E8/E9... Không chỉnh `SCOPE.md` chỉ để đánh dấu các bước đọc code.
3. Giữ ưu tiên hoàn thành S6/S7/S8 trước hạn nộp nhưng **không bắt các nhóm công việc độc lập chờ nhau**. A04→A02 là trật tự phụ thuộc cho *nâng cấp có duyệt*, không phải lý do chỉ rà hai module đó.
4. Không tự sửa code/SQL/migration, AGENTS/SCOPE, không merge PR; Bảo sở hữu S6, Quý sở hữu S7; S5 đóng băng, bất kỳ tương thích phải có owner/review/test hồi quy.
5. Cuối mỗi lượt: **cập nhật đúng checkbox rà soát và điểm tiếp tục trong file này**; ghi commit/PR; trước lượt mới đối chiếu `main` và `SCOPE.md` thật. Nếu phần chưa thể kiểm vì cần chạy môi trường, để [ ] và không tự tuyên bố PASS.

**Câu resume ngắn:** `Đọc AGENTS.md, SCOPE.md trên main và BUSINESS_DECISIONS.md nhánh docs/business-decisions-20261103, mục E8. Tiếp tục mục N đầu tiên chưa đánh dấu, kiểm trạng thái PR mới, rà trong phạm vi, cập nhật E8 sau lượt; tuyệt đối không tự mở rộng A04/A02 hay sửa code/SQL/S6/S7.`

