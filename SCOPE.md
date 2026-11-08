# SCOPE — KLCN090: Hệ thống quản lý tiêm chủng

**Nguồn sự thật hiện hành về phạm vi và trạng thái.** Chi tiết acceptance/test/CI nằm ở active issue, PR và Git history; không lưu lại toàn bộ lịch sử ở đây.

Cập nhật: **08/11/2026**. Hạn nộp theo kế hoạch R01: **08/11/2026**.

## Current State

### DONE gần nhất
- **REGIMEN-R1 #180 / PR #181** — Catalog hỗ trợ calendar interval theo mũi neo với `Days|Weeks|Months|Years`, giữ legacy day intervals; Windows A04 quản trị structured regimen; Customer Web hiển thị timeline + provenance tham khảo. Native A04 full-shell smoke PASS sau fix hiển thị selector; CI #387 PASS đủ backend/windows/web. Squash main: `04b932e8e4df6ec865ea90f19ad2ea731c1d77ac`.
- **RF-05G #170 / PR #179** — tự động nhắc trước lịch hẹn 48/24 giờ và liên hệ sau lịch hẹn 3/7 ngày trên appointment hiện hành; DB idempotency + retry/revalidation + A09 history/wording người dùng. CI #382 PASS; native WPF keyboard smoke PASS ở HEAD trước wording-only patch. Squash main: `19f8218da0e6a505bf11ed3ad5daf8b087abe72b`.
- **RF-05F.1 #175 / PR #178** — Customer Web lưu ngày+giờ mong muốn với provenance `CoGioDuKien`; A09 dùng preference làm gợi ý nhưng `LichHenTiem` xác nhận độc lập; confirm/reschedule/cancel tự enqueue lifecycle Email. CI #377 và Web/WPF runtime gates PASS. Squash main: `90873bbf42853ad9f00810d600ae96da9705e3bb`.
- **RF-05E #166 / PR #172** — email reminder thật, outbox/dispatcher, Windows A09; real Development SMTP acceptance đã PASS.
- **RF-05F #167 / PR #174** — reschedule/cancel cùng `LichHenTiem`, RowVersion, clinical lock, reminder reconciliation, Customer Web `DaHuy`, outcome-unknown reconcile. Squash main: `bfd6ac0be813e549d15ff799ba7d9c972547c0c1`.

Các nền tảng/core trước đó đã tích hợp: S0–S7 theo phạm vi đã nghiệm thu, AUTHZ-R1, AUTH-R2, DATA-R1, RF-05B/C/D, S6 integration, X1 kho đọc, X2.1 nhập lô mới, X2.2.1 lịch sử tiếp nhận. Bằng chứng chi tiết giữ trong issue/PR tương ứng.

### ACTIVE
- **THESIS-R1 #182** — đồng bộ báo cáo khóa luận với trạng thái repo hiện tại. **Patch 2D.2 DONE:** audit implementation hiện hành và khóa tập **13 System Use Case** ở mức mục tiêu nghiệp vụ/phần mềm, không giữ máy móc 10 UC cũ. Toàn bộ bảng đặc tả 2.2.2 dùng format bài giảng **Tên Use Case / Tác nhân chính / Tổng quan / Mối quan hệ / Dòng sự kiện chính / Dòng sự kiện phụ / Tiền điều kiện / Hậu điều kiện**. Đã loại các overclaim cũ về mobile app/chứng nhận điện tử, quét mã vắc xin, cảnh báo khẩn tự động, điều chuyển/điều chỉnh kho, cổng thanh toán production và reminder chỉ mô phỏng; hệ thống không tự quyết định eligibility y khoa. Toàn bộ prose của 13 bảng đã Việt hóa các thuật ngữ implementation như reschedule/cancel/reconcile/provider/clinical/fail-closed/audit; các mốc reminder được định nghĩa ngay lần đầu: **48 giờ trước lịch hẹn (T-48), 24 giờ trước lịch hẹn (T-24), 3 ngày sau lịch hẹn (D+3), 7 ngày sau lịch hẹn (D+7)**. 10 Sequence Diagram cũ đã loại khỏi 2.2.2 vì không còn khớp text; **Hình Use Case hệ thống tổng thể và Sequence Diagram mới vẫn PARKED để redraw sau**. **Micro-patch 1.6 DONE:** mở rộng mục Công cụ và công nghệ sử dụng theo nguyên tắc **công nghệ dùng ở đâu → vì sao phù hợp với kiến trúc/phạm vi đề tài**, giữ ranh giới không lặp nội dung cài đặt của Chương 4. **Micro-patch print QA DONE:** sửa Danh mục hình/bảng để dòng xuống hàng không còn kế thừa first-line indent thân bài; đặt left/first-line = 0, chừa right indent 0,5 cm và tab leader phải tại 14,5 cm để caption dài không tràn vùng số trang. Sửa cỡ chữ dòng thành viên **Nguyễn Ngọc Quý** trên cả hai trang bìa từ 13 pt về 14 pt cho đồng nhất với hai thành viên còn lại; làm sạch cached pagination markers ở phần đầu để tránh trang trắng phát sinh khi Word repaginate/refresh field. **Micro-patch print layout DONE:** xóa số trang bị dính ở caption thật nhưng giữ số trang trong Danh mục hình/bảng; phóng Hình 1.3a lên hết chiều rộng vùng nội dung để giảm khoảng trắng mà vẫn đọc rõ; trang Nhận xét của giảng viên hướng dẫn được kéo dài vùng dòng chấm gần trọn trang và đưa khối chữ ký xuống thấp; bìa ngoài dùng khung đôi với họa tiết hình học nhẹ ở bốn góc, cạnh trái lùi 18 mm để chừa vùng đóng gáy, các cạnh còn lại 10 mm. Latest working artifact `CNTT_KLCN090_THESIS-R1_Patch2D2_PrintLayout_Final.docx`, SHA-256 `d29920def22a3ccde28bed8d62a713d78028de232b729eba37f7a31d01a44cda`. Render QA **87 trang PASS**; so với baseline CaptionLayout QA chỉ trang 1, 4, 28 thay đổi, 84 trang còn lại pixel-identical. **NEXT:** Patch 2D.3 — khóa/redraw Business Use Case tổng thể + 7 Activity Diagram theo 7 đặc tả đã PASS.

### THESIS-R1 — khóa quy tắc văn phong học thuật, chống đạo văn và sử dụng AI

Nguồn tinh chế: tài liệu người dùng cung cấp `Hướng dẫn giảm đạo văn và AI trong văn bảng học thuật.pdf` (08/11/2026). Mục tiêu là **tính học thuật, tính nguyên gốc và khả năng truy vết nguồn**, không tối ưu để "qua" AI detector hoặc chạy theo một ngưỡng Similarity/AI Detection cụ thể.

#### G. Tránh meta-signposting máy móc
- Không mở liên tiếp các tiểu mục bằng các câu như “Phần này trình bày…”, “Trong phần này…”, “Mục này giới thiệu…”.
- Chỉ dùng một câu định hướng ở **đầu chương** hoặc tại điểm chuyển mạch thực sự cần thiết.
- Với tiểu mục như 1.1, 1.2, 1.3…, ưu tiên đi thẳng vào bối cảnh/nội dung cụ thể hoặc dùng một câu nối có thông tin thực chất.
- Nếu cần chuyển ý, câu chuyển phải bổ sung quan hệ logic giữa phần trước và phần sau, không chỉ mô tả cấu trúc văn bản.
- Rule này áp dụng xuyên toàn bộ khóa luận để tránh giọng văn cơ học và lặp công thức.

#### A. Nguyên tắc bắt buộc khi viết/sửa
- Viết theo **ý nghĩa và bằng chứng**, không thay từ đồng nghĩa trên cùng một cấu trúc câu rồi coi là paraphrase.
- Với nội dung lấy từ nguồn ngoài: **đọc hiểu → tách khỏi câu chữ nguồn → viết lại theo cách hiểu của nhóm → vẫn phải trích dẫn nguồn**. Paraphrase không làm mất nghĩa vụ trích dẫn.
- Không sao chép nguyên cấu trúc lập luận, thứ tự mệnh đề hoặc chuỗi từ đặc trưng của nguồn nếu có thể diễn đạt lại mà không mất nghĩa.
- Khi cần viết lại, được phép: đổi cấu trúc câu, gộp/tách câu, đổi thứ tự nguyên nhân-kết quả, đổi chủ động-bị động/chủ thể, khái quát hóa/cụ thể hóa — **chỉ khi ngữ nghĩa và quan hệ nghiệp vụ vẫn đúng**.
- Mỗi đoạn có một ý chính rõ; ưu tiên mạch: **bối cảnh/đầu vào → diễn biến hoặc bằng chứng → nhận xét/ý nghĩa → liên kết sang bước tiếp theo**.
- Nhịp câu được phép đa dạng tự nhiên (ngắn/vừa/dài), tránh lặp một mẫu câu máy móc hoặc kết luận giống nhau ở mọi đoạn; **không cố tình làm câu ngẫu nhiên chỉ để đánh lừa detector**.
- Hạn chế khẳng định tuyệt đối khi nguồn hoặc implementation không hỗ trợ. Dùng các mức độ như "trong phạm vi đề tài", "theo khảo sát", "có thể", "phù hợp với", "kết quả kiểm thử cho thấy" khi đúng ngữ cảnh.
- Với fact đã được repo/test/contract xác minh, diễn đạt dứt khoát và chính xác; không "hedge" làm mờ sự thật kỹ thuật.
- Giữ thuật ngữ nghiệp vụ/kỹ thuật, tên bảng, field, trạng thái, API contract khi độ chính xác quan trọng; **không paraphrase thuật ngữ kỹ thuật chỉ để khác chữ**.
- Văn phong phải mang dấu ấn của đề tài này: nêu **quyết định thiết kế, giới hạn phạm vi, evidence từ implementation/test, và quan hệ giữa các bước nghiệp vụ** thay vì các câu học thuật chung chung có thể dùng cho mọi đề tài.

#### B. "Giọng người nghiên cứu" chỉ được dùng khi có căn cứ
- Có thể dùng các cách diễn đạt như "Trong phạm vi đề tài, nhóm lựa chọn...", "Từ kết quả kiểm thử...", "Qua đối chiếu với implementation...", "Theo nội dung khảo sát công khai...".
- **Không bịa trải nghiệm cá nhân, phỏng vấn, quan sát thực địa, ý kiến chuyên gia hoặc dữ liệu thực nghiệm** để làm văn bản "giống người".
- Các cụm kiểu "qua phỏng vấn", "trong quá trình quan sát thực tế", "các bác sĩ cho biết..." chỉ dùng khi có bằng chứng nguồn thật trong tài liệu dự án.

#### C. Quy tắc chống đạo văn
- Không dùng kiểu thay vài từ đồng nghĩa nhưng giữ nguyên cấu trúc nguồn.
- Ưu tiên "hiểu rồi viết lại": xác định ý cốt lõi, viết lại từ memory/logic của chính đoạn, sau đó quay lại kiểm tra độ chính xác và gắn citation.
- Khi nguồn cung cấp fact/số liệu/quy trình/chính sách: citation phải đứng gần claim tương ứng.
- Có thể thêm phân tích của nhóm để tạo giá trị mới, nhưng phải phân biệt rõ **fact từ nguồn** và **nhận xét/thiết kế của đồ án**.
- Nếu một claim không có nguồn hoặc evidence repo đủ mạnh, không viết như một fact đã được chứng minh.

#### D. Quy tắc sử dụng AI khi chỉnh khóa luận
- AI được dùng để hỗ trợ cấu trúc, diễn đạt, rà logic và kiểm tính nhất quán; **không được dùng làm nguồn sự thật**.
- Mọi đoạn do AI hỗ trợ phải qua 4 gate: **Source-of-Truth → semantic accuracy → citation/evidence → human academic voice**.
- Không đưa vào báo cáo nguồn tham khảo, số liệu, quy trình, policy hoặc kết quả test do AI tự sinh mà chưa xác minh.
- Không đặt mục tiêu "AI Detection < X%" hoặc "Similarity < X%" như acceptance của THESIS-R1. Các con số minh họa trong tài liệu hướng dẫn không thay thế quy định chính thức của trường/công cụ.
- Nếu Khoa/Trường có quy định khai báo việc sử dụng AI, phải tuân theo quy định chính thức đó; không tự suy đoán chính sách.

#### F. Tone chuẩn cho phần Mở đầu
- Phần Mở đầu phải có nhịp dẫn mềm: **bối cảnh thực tế → đặc điểm hành trình tiêm chủng → vấn đề dữ liệu/phối hợp → nhu cầu hệ thống thông tin → lý do chọn đề tài → giải pháp của nhóm**.
- Không mở đầu bằng kiến trúc kỹ thuật hoặc danh sách chức năng ngay lập tức.
- Mỗi đoạn vẫn phải có nhiệm vụ rõ ràng và bổ sung thông tin mới; không kéo dài bằng câu sáo hoặc lặp ý.
- Tone dùng cho Mở đầu là học thuật nhưng tự nhiên, có giải thích “vì sao” trước khi đi vào “xây dựng gì”.

#### E. Checklist cho mỗi đoạn được sửa
1. Đoạn này trả lời câu hỏi gì trong mạch nghiệp vụ/hệ thống?
2. Claim nào là fact từ repo/tài liệu? Claim nào là phân tích/thiết kế của nhóm?
3. Fact từ nguồn ngoài đã có citation chưa?
4. Có câu nào quá giống cấu trúc nguồn hoặc chỉ đổi từ đồng nghĩa không?
5. Câu dài/ngắn có tự nhiên và phục vụ ý nghĩa không?
6. Có khẳng định tuyệt đối vượt quá evidence không?
7. Có chi tiết "giống trải nghiệm thật" nhưng thực tế không có nguồn không?
8. Thuật ngữ kỹ thuật/clinical/business có bị paraphrase làm sai nghĩa không?
9. Đoạn có nối logic với bước trước/sau trong dependency spine không?
10. Đọc lại như người viết khóa luận: có cụ thể cho KLCN090 hay vẫn là văn bản generic?

### THESIS-R1 — khóa bố cục đặc tả và sơ đồ

Theo góp ý giảng viên, **mỗi sơ đồ phải đặt ngay sau bảng đặc tả tương ứng để người đọc quan sát liên tục**:
- Phần nghiệp vụ: mỗi bảng đặc tả Use Case nghiệp vụ → ngay sau đó là sơ đồ hoạt động/luồng nghiệp vụ tương ứng.
- Phần hệ thống: mỗi bảng đặc tả Use Case hệ thống → ngay sau đó là sơ đồ tuần tự tương ứng.
- Không gom toàn bộ activity diagram hoặc sequence diagram thành một cụm xa bảng đặc tả.
- Thứ tự các cặp **đặc tả → sơ đồ** phải đi theo dependency của quy trình thực tế, không theo mã UC cũ nếu mã cũ làm sai luồng.
- Chương 3 vẫn dành cho thiết kế tổng thể/dữ liệu/lớp và các nội dung thiết kế không gắn trực tiếp với một bảng đặc tả Use Case cụ thể.
- Quy tắc này ưu tiên tính dễ theo dõi theo góp ý giảng viên, kể cả khi một sequence diagram về mặt UML có thể được xem là nội dung thiết kế.

### THESIS-R1 — khóa định dạng bảng

- Nội dung bảng: **Times New Roman 12 pt**, line spacing **Multiple 1.3**, Before **0 pt**, After **0 pt**.
- Header bảng: **đậm, canh giữa**, lặp lại khi bảng sang trang.
- Căn chữ trong ô theo ngữ nghĩa; với bảng danh mục/ngắn, ưu tiên **căn giữa hoặc trái**, **không dùng Justify** vì dễ tạo khoảng trắng bất thường giữa các từ.
- Chiều cao hàng để tự động theo nội dung; không ép fixed height nếu không cần.
- Giữ padding ô vừa phải và vertical alignment giữa để bảng cân khi in.
- Không dùng direct formatting rời rạc khiến cùng một bảng có font/cỡ/spacing khác nhau.

### THESIS-R1 — khóa định dạng paragraph trong bảng

Áp dụng cho **mọi bảng hiện tại và các bảng tạo mới**:
- Paragraph trong cell phải đặt trực tiếp **left indent = 0, right indent = 0, first-line indent = 0**; không để kế thừa thụt đầu dòng của style thân bài.
- Không dùng tab/space đầu dòng hoặc cuối dòng để canh nội dung trong cell; canh bằng paragraph alignment và cell alignment.
- Xóa tab stop không cần thiết trong paragraph của bảng.
- Nội dung bảng: **Times New Roman 12 pt**, line spacing **Multiple 1.3**, Before **0 pt**, After **0 pt**.
- Header bảng: đậm, canh giữa; nội dung còn lại canh theo ngữ nghĩa của cột, nhưng phải đặt alignment rõ ràng thay vì dựa vào khoảng trắng.
- Khi nội dung cần xuống dòng, để Word wrap tự nhiên; không chèn space/tab để “đẩy” chữ.
- Với bảng nhiều trang, giữ repeat header row theo quy định mẫu Khoa.
- Khi QA, phải kiểm trực quan xem chữ có bị lệch trái/phải do inherited indent hay không.

### THESIS-R1 — khóa thể thức trình bày

Khi chỉnh `CNTT_KLCN090_Tuan12.docx`, **bắt buộc bám đúng mẫu chính thức của Khoa; không tự đổi format**:
- Toàn bộ nội dung dùng **Times New Roman**.
- Khổ A4; lề **trái 3,5 cm; phải 2,5 cm; trên 2,5 cm; dưới 2,5 cm**.
- Cấp chương: **18 pt, IN HOA, đậm, canh giữa**.
- Đề mục cấp 1 kiểu `1.1`: **14 pt, IN HOA, đậm**.
- Đề mục cấp 2/3 kiểu `1.1.1`, `1.1.1.1`: **13 pt, in thường, đậm**.
- Caption hình: đánh số theo chương (`Hình 1.1`, `Hình 1.2`, ...), **đặt dưới hình, canh giữa**.
- Caption bảng: đánh số theo chương, **đặt trên bảng, canh giữa**.
- Nội dung bảng: **12 pt**, line spacing **Multiple 1.3**, Before **0 pt**, After **0 pt**; lặp lại tiêu đề bảng khi bảng sang trang.
- Nội dung thường: **13 pt**, line spacing **Multiple 1.3**, Before **6 pt**, After **6 pt**, **canh đều hai bên**, thụt đầu dòng từng đoạn.
- Bullet cấp 1 dùng dấu `-`: Multiple 1.3, Before/After 6 pt, canh đều; Position **0,3 cm**, Text indent **0,8 cm**.
- Bullet cấp 2 dùng dấu `+`: Position **0,8 cm**, Text indent **1,3 cm**; paragraph giống bullet cấp 1.
- Số trang đặt **giữa cuối trang**; **không đánh số hai trang bìa**; từ Lời cảm ơn đến Danh mục bảng dùng **i, ii, iii, ...**; từ Mở đầu dùng **1, 2, 3, ...**.
- **Mục lục phải tạo tự động**; danh mục hình/bảng cũng phải đồng bộ theo caption/field, không giữ danh mục gõ tay.
- Đây là **hard acceptance** của THESIS-R1: nếu nội dung đúng nhưng format lệch mẫu Khoa thì chưa được coi là hoàn thành.

### THESIS-R1 — khóa thứ tự chỉnh sửa báo cáo trước Chương 2

Trước khi viết lại Chương 2, phải hoàn thiện theo thứ tự:
1. **Lời cảm ơn** — viết lại ngắn gọn, tự nhiên, đúng vai trò học thuật; tránh sáo ngữ/generic và tránh claim trải nghiệm không có thật.
2. **Mở đầu** — khóa lý do chọn đề tài, mục tiêu, phạm vi, đối tượng, phương pháp/thực hiện, cấu trúc báo cáo; mọi claim về hệ thống phải khớp repo hiện tại.
3. **Chương 1: Khảo sát hệ thống** — viết lại theo nguồn khảo sát/tài liệu thật, phân biệt rõ quy trình thực tế tham khảo với quy tắc thiết kế của đồ án; yêu cầu chức năng/phi chức năng phải bám đề cương và core thesis scope.
4. Chỉ khi ba phần trên PASS writing guardrail mới chuyển sang **Chương 2: Phân tích hệ thống**.

Mục tiêu của thứ tự này:
- khóa giọng văn và thuật ngữ trước khi vào đặc tả;
- tránh Chương 2 dùng khái niệm/phạm vi chưa được định nghĩa ở Mở đầu/Chương 1;
- đảm bảo dependency spine đã xuất hiện từ Chương 1 và tiếp tục nhất quán qua Chương 2–5.

### THESIS-R1 — khóa mục đích từng phần, chống lặp ngữ nghĩa

- **Mở đầu** chỉ trả lời: vì sao chọn bài toán, mục tiêu gì, đối tượng/phạm vi nào, phương pháp thực hiện ra sao và báo cáo được tổ chức thế nào.
- **Chương 1** chỉ trả lời: bối cảnh khảo sát cụ thể, vai trò nghiệp vụ, quy trình thực tế, biểu mẫu/dữ liệu và các yêu cầu rút ra từ khảo sát. Không kể lại lý do chọn đề tài/mục tiêu/phạm vi như Mở đầu.
- **Chương 2** trả lời: từ nghiệp vụ đã khảo sát, hệ thống cần hỗ trợ những chức năng nào và chúng được đặc tả/mô hình hóa ra sao.
- **Chương 3** trả lời: hệ thống được thiết kế như thế nào để đáp ứng Chương 2.
- **Chương 4** trả lời: các thiết kế đã được cài đặt thành sản phẩm/giao diện như thế nào.
- **Chương 5** trả lời: sản phẩm được kiểm thử/triển khai và có bằng chứng gì.
- **Kết luận** đối chiếu mục tiêu ban đầu với kết quả thật, hạn chế và hướng phát triển; không lặp mô tả chức năng theo kiểu Chương 4.
- Mỗi claim/ý giải thích chính chỉ có một “home section”. Phần khác nếu cần nhắc lại chỉ tham chiếu ngắn để nối mạch, không diễn giải lại cùng một ý bằng câu chữ khác.

### THESIS-R1 — khóa nguyên tắc làm giàu khảo sát bằng chi tiết thực tế

Khi mở rộng Chương 1, chỉ thêm chi tiết thực tế nếu chi tiết đó **tác động trực tiếp đến nghiệp vụ hoặc dữ liệu cần quản lý**.

#### Relevance gate
Một chi tiết chỉ được đưa vào phần khảo sát nếu trả lời được ít nhất một câu hỏi:
- Nó làm thay đổi **cách tiếp nhận/kiểm tra**?
- Nó làm thay đổi **đơn vị tính, số lượng, đóng gói hoặc tồn kho**?
- Nó làm thay đổi **điều kiện bảo quản/vận chuyển**?
- Nó ảnh hưởng **truy vết lô, hạn dùng, nhà sản xuất hoặc nguồn gốc**?
- Nó ảnh hưởng **cách chuẩn bị/cấp phát/thực hiện tiêm**?
- Nó tạo ra **trường dữ liệu** mà hệ thống cần lưu hoặc đối chiếu?

#### Ví dụ phù hợp cho nghiệp vụ vắc xin/kho
Có thể khảo sát và trình bày khi có nguồn chính thức:
- tên vắc xin, nhà sản xuất, dạng bào chế/presentation;
- đơn vị đóng gói: hộp/thùng/lọ/bơm tiêm;
- số lọ trong hộp/thùng nếu tài liệu sản phẩm công bố;
- số liều trong một lọ;
- thể tích mỗi liều (mL);
- lọ đơn liều hay đa liều;
- số lô, hạn dùng;
- dung môi kèm theo nếu có;
- điều kiện nhiệt độ/bảo quản và yêu cầu dây chuyền lạnh;
- quy định sau mở lọ/hoàn nguyên nếu có;
- công nghệ/nền tảng vắc xin **chỉ khi nó có ý nghĩa đối với phân loại, bảo quản, chuẩn bị hoặc sử dụng trong nghiệp vụ**.

#### Cảnh báo
- **Không viết “một lô có X lọ” như một quy tắc chung.** “Lô” là batch sản xuất/định danh truy vết; số lượng lọ giao trong một lần nhận phụ thuộc shipment/packaging, còn số lọ mỗi hộp/thùng phụ thuộc từng sản phẩm.
- Không suy rộng thông số của một vắc xin cho tất cả vắc xin.
- Không thêm kiến thức sinh học/công nghệ vắc xin nếu không liên quan đến actor, quyết định nghiệp vụ, dữ liệu hoặc điều kiện xử lý.
- Chi tiết sản phẩm phải lấy từ WHO, Bộ Y tế, nhà sản xuất/tờ hướng dẫn chính thức hoặc nguồn công khai đáng tin cậy; ghi rõ đó là **ví dụ sản phẩm**, không phải quy tắc chung của trung tâm.
- Source thực tế làm giàu **khảo sát nghiệp vụ**; repo vẫn là Source of Truth cho **hệ thống của đồ án thực sự hỗ trợ gì**.

### THESIS-R1 — khóa chuẩn mô tả quy trình nghiệp vụ theo mẫu Khoa

Áp dụng cho Chương 1 và phần Use Case nghiệp vụ ở Chương 2:
- Trước phần chi tiết phải có **tổng quan đầy đủ các Use Case/quy trình nghiệp vụ** và sắp theo đúng quan hệ trước–sau; không sắp theo mã hoặc theo loại sơ đồ.
- Mỗi quy trình phải làm rõ đủ các câu hỏi của mẫu Khoa:
  1. **Khi nào quy trình bắt đầu?**
  2. **Những hoạt động nào xảy ra và actor cụ thể nào thực hiện từng hoạt động?**
  3. **Actor đọc dữ liệu gì, ghi/tạo/cập nhật dữ liệu gì?**
  4. **Có nhánh điều kiện/thay thế nào không?**
  5. **Khi kết thúc, kết quả của quy trình mở sang quy trình nào hoặc được dùng ở đâu?**
- Trong văn bản chính, các ý trên phải được **lồng vào văn xuôi tự nhiên**, không biến mỗi quy trình thành một checklist hỏi–đáp máy móc.
- Không dùng actor mơ hồ như “bộ phận tiếp nhận”, “nhân viên phụ trách” nếu đã biết chính xác là Lễ tân/Bác sĩ/Thu ngân/Điều dưỡng/Nhân viên kho.
- Khi nguồn công khai chỉ chứng minh một phần của quy trình, chỉ khẳng định phần đó. Quy tắc riêng của đồ án phải ghi rõ là **mô hình/phạm vi của đề tài**, không gán thành quy trình nội bộ VNVC.
- Với nguồn thực tế, ưu tiên VNVC chính thức, văn bản Chính phủ/Bộ Y tế hoặc nguồn chính thức tương đương; không lấy bản cũ của sinh viên làm Source of Truth.
- P1 **Nhập vắc xin từ nhà cung cấp** là luồng hỗ trợ; P2→P7 là hành trình phục vụ chính; P4 lịch hẹn và P6 thanh toán có thể là nhánh điều kiện.
- Nội dung biểu mẫu ở 1.4 được xử lý ở patch riêng; không dùng việc chỉnh 1.1–1.3 để âm thầm thay đổi nội dung biểu mẫu.

### THESIS-R1 — khóa xử lý hình biểu mẫu mục 1.4

Patch 2C.5 chỉ chỉnh **hình, caption và bố cục hình** trong mục 1.4; không sửa semantic nghiệp vụ.

Quy tắc:
- Ưu tiên lấy lại/raster lại ảnh nguồn ở độ phân giải cao; không dùng sharpen giả để bù cho nguồn quá thấp.
- Crop bỏ vùng trắng thừa trước khi chèn vào Word; ảnh phải được **căn giữa theo vùng nội dung thật**, không căn theo canvas trắng khiến hình nhìn lệch phải.
- Kích thước ảnh phải phù hợp vùng in A4 với lề khóa 3,5/2,5/2,5/2,5 cm; không kéo giãn ảnh vượt tỷ lệ gốc.
- Nếu một biểu mẫu nhiều trang không thể ghép hợp lý thành một ảnh, dùng hậu tố **a/b** và ghi rõ **trang 1 / trang 2** trong caption.
- Với biểu mẫu hiện mang số Hình 1.3: dùng **Hình 1.3a** và **Hình 1.3b** cho hai trang tương ứng.
- Với Hình 1.4: crop bỏ phần trống không mang thông tin và ghép lại phần nội dung vào ảnh chính nếu việc ghép không làm sai bố cục hoặc mất thông tin.
- Sau khi chỉnh phải render QA và kiểm ở kích thước in; acceptance là ảnh rõ, không lệch, caption đúng, không vỡ trang.
- Không sửa nội dung biểu mẫu, không thay dữ liệu trong ảnh.

### THESIS-R1 — khóa cách gọi nghiệp vụ trong văn xuôi và citation navigation

- Các ký hiệu kỹ thuật nội bộ như `P1`…`P7` chỉ được dùng trong planning/audit nội bộ; **không xuất hiện trong văn xuôi khóa luận**. Trong báo cáo phải dùng tên nghiệp vụ đầy đủ hoặc cách gọi ngắn đã rõ ngữ cảnh.
- Các mốc automation riêng của hệ thống như T-48/T-24, D+3/D+7 **không đặt trong phần khảo sát nghiệp vụ thực tế**. Nếu cần, chúng thuộc yêu cầu/chức năng hệ thống hoặc phần cài đặt/kiểm thử.
- Citation số dạng `[n]` trong thân bài phải là **internal hyperlink** tới đúng mục `[n]` trong Tài liệu tham khảo khi file DOCX cho phép.
- Mỗi mục Tài liệu tham khảo giữ bookmark ổn định để citation có thể nhảy tới đúng nguồn; không dùng hyperlink để thay thế nghĩa vụ trích dẫn hoặc nội dung nguồn.
- Khi citation nằm cạnh nhau, có thể giữ dạng `[20][21]` hoặc chuẩn hóa sau theo format citation cuối cùng; mỗi số phải trỏ đúng bookmark tương ứng.

### THESIS-R1 — khóa từ điển nghiệp vụ, actor và dữ liệu canonical

Từ Patch tiếp theo, tên gọi trong toàn bộ báo cáo phải dùng **một từ điển canonical duy nhất**. Không đổi thuật ngữ giữa Chương 1–5 chỉ để tránh lặp từ.

#### 1. Bảy quy trình nghiệp vụ canonical
1. **Nhập vắc xin từ nhà cung cấp**.
2. **Tiếp nhận và quản lý hồ sơ khách hàng/người được tiêm**.
3. **Đăng ký dịch vụ tiêm chủng**.
4. **Quản lý lịch hẹn và nhắc lịch**.
5. **Khám sàng lọc và chỉ định tiêm chủng**.
6. **Thanh toán dịch vụ**.
7. **Thực hiện tiêm chủng và theo dõi sau tiêm**.

Quy tắc:
- P1 là luồng hỗ trợ tạo nguồn lô/tồn, không phải bước bắt buộc phát sinh trong mỗi lượt khách.
- P2→P7 là hành trình phục vụ chính; P4 và P6 có thể là nhánh điều kiện tùy trường hợp nghiệp vụ.
- Không đổi lại sang các tên cũ như “đăng ký thông tin…”, “quản lý lịch tiêm…”, “khám/chỉ định” nếu không có lý do nghiệp vụ cụ thể.

#### 2. Sáu vai trò nghiệp vụ cốt lõi
- **Khách hàng**
- **Lễ tân**
- **Bác sĩ**
- **Thu ngân**
- **Điều dưỡng**
- **Nhân viên kho**

Quy tắc:
- Đây là **sáu vai trò nghiệp vụ cốt lõi của đề tài**, không đồng nghĩa cả sáu đều là Business Actor trong UML.
- Trong **Business Use Case**, Business Actor là đối tượng **bên ngoài ranh giới nghiệp vụ** cung cấp đầu vào/nhận đầu ra. Khách hàng là Business Actor; Nhà cung cấp có thể là Business Actor của nghiệp vụ nhập vắc xin. Các vai trò nội bộ như Lễ tân, Bác sĩ, Thu ngân, Điều dưỡng, Nhân viên kho được hiểu là **Business Worker/thừa tác viên** khi mô hình hóa quy trình.
- Trong **System Use Case**, bất kỳ người/hệ thống nào trực tiếp tương tác với phần mềm đều có thể là **System Actor**; vì vậy các vai trò nội bộ nêu trên có thể là System Actor ở mục 2.2.
- Không dùng các cụm mơ hồ như “bộ phận tiếp nhận”, “nhân viên tiếp nhận”, “nhân viên phụ trách”, “bộ phận chuyên môn” nếu đã xác định được vai trò cụ thể.
- Khi một thao tác do automation thực hiện, ghi rõ **Hệ thống** hoặc thành phần kỹ thuật phù hợp; không gán cho một vai trò nghiệp vụ không thực hiện thao tác đó.
- **Người được tiêm** là đối tượng nghiệp vụ, không mặc định là actor riêng.
- CSKH/Admin/CNTT nếu cần nhắc tới chỉ là vai trò hỗ trợ, không làm thay đổi sáu vai trò nghiệp vụ cốt lõi.

#### 3. Thuật ngữ dữ liệu canonical
- **Khách hàng**: người sở hữu tài khoản hoặc thực hiện đăng ký cho mình/người khác.
- **Người được tiêm**: chủ thể thực sự nhận mũi tiêm.
- **Hồ sơ người được tiêm**: hồ sơ nghiệp vụ của chủ thể nhận tiêm; không gọi chung là “hồ sơ khách hàng” khi thực tế đang nói về người được tiêm.
- **Đăng ký dịch vụ tiêm chủng**: nhu cầu/dịch vụ đã được ghi nhận; không đồng nghĩa với lịch hẹn.
- **Thời gian mong muốn**: ngày/giờ khách mong muốn trong đăng ký; không gọi là lịch hẹn chính thức.
- **Lịch hẹn tiêm chủng** / **lịch hẹn**: appointment đã được xác nhận trong `LichHenTiem`; không dùng “lịch tiêm” nếu dễ nhầm với phác đồ/lịch sử.
- **Phác đồ tham khảo** / **lịch phác đồ tham khảo**: timeline tham khảo; không phải lịch hẹn và không tự tạo appointment.
- **Kết quả khám sàng lọc và chỉ định**: bằng chứng chuyên môn do Bác sĩ ghi nhận; hệ thống không tự suy diễn eligibility.
- **Giao dịch thanh toán** / **trạng thái thanh toán**: dữ liệu tài chính gắn với nguồn đăng ký; không mô tả như quyết định y khoa.
- **Vắc xin**: danh mục sản phẩm/vắc xin.
- **Lô vắc xin**: batch cụ thể có số lô/hạn dùng và dữ liệu tiếp nhận.
- **Tồn kho**: số lượng khả dụng theo dữ liệu kho; không đồng nghĩa với lô.
- **Mũi tiêm đã thực hiện**: sự kiện tiêm thực tế đã ghi nhận.
- **Theo dõi sau tiêm**: dữ liệu phát sinh sau mũi đã thực hiện.
- **Lịch sử tiêm chủng**: tập hợp các mũi đã thực hiện; không bao gồm lịch hẹn tương lai hoặc phác đồ tham khảo.
- **Ưu đãi**: dữ liệu hỗ trợ tính/hiển thị giá theo phạm vi module promotion.
- **Báo cáo**: dữ liệu tổng hợp từ nghiệp vụ đã phát sinh, không phải source-of-truth độc lập.

#### 4. Quy tắc viết chủ thể-hành động-dữ liệu
Mỗi bước nghiệp vụ phải trả lời rõ:
- **Ai** thực hiện?
- **Làm gì**?
- **Trên dữ liệu nào**?
- **Kết quả tạo ra/cập nhật dữ liệu gì**?
- **Kết quả đó mở sang bước nào**?

Ví dụ đạt:
- “Lễ tân tra cứu hồ sơ người được tiêm và cập nhật thông tin khi có thay đổi.”
- “Bác sĩ ghi nhận kết quả khám sàng lọc và chỉ định tiêm chủng.”
- “Thu ngân ghi nhận giao dịch thanh toán cho đăng ký dịch vụ.”
- “Điều dưỡng đối chiếu chỉ định và lô vắc xin trước khi ghi nhận mũi tiêm.”
- “Nhân viên kho tiếp nhận và ghi nhận lô vắc xin từ nhà cung cấp.”

Ví dụ không đạt:
- “Bộ phận tiếp nhận xử lý thông tin.”
- “Nhân viên kiểm tra dữ liệu.”
- “Hệ thống cập nhật trạng thái” nếu không nói trạng thái gì, nguồn nào và do sự kiện nào.

Quy tắc canonical này áp dụng cho **Mở đầu, Chương 1–5, bảng đặc tả, caption, UML, test case và kết luận**.

### THESIS-R1 — khóa thứ tự luồng nội dung

Mọi nội dung nghiệp vụ, Use Case, sequence diagram, mô tả cài đặt và test scenario phải được sắp theo **cùng một dependency spine**, không sắp tùy ý theo mã UC hoặc loại sơ đồ.

Thứ tự xương sống:
1. **Nền tảng truy cập và dữ liệu nền:** xác thực/phân quyền; danh mục vắc xin, dịch vụ/gói, ưu đãi; lô/tồn kho phải tồn tại trước các thao tác phụ thuộc.
2. **Hồ sơ:** xác định/tra cứu/tạo khách hàng và người được tiêm.
3. **Tra cứu và lựa chọn dịch vụ:** chọn vắc xin/gói phù hợp với nhu cầu; không tự quyết eligibility y khoa.
4. **Đăng ký dịch vụ:** gắn đúng người được tiêm, lưu lựa chọn và thời gian mong muốn.
5. **Lịch hẹn xác nhận và nhắc lịch:** lễ tân xác nhận lịch chính thức; reschedule/cancel/reminder bám appointment hiện hành. Reminder T-48/T-24 xảy ra trước lượt đến; D+3/D+7 là nhánh recall nếu lịch đã qua mà chưa có mũi hoàn tất.
6. **Khám sàng lọc và chỉ định:** mọi lượt đến để tiêm phải qua clinical flow; hệ thống không tự quyết định đủ điều kiện tiêm.
7. **Thanh toán:** xử lý theo điều kiện dịch vụ; có thể xảy ra ở thời điểm khác nhau nhưng nếu lượt tiêm yêu cầu thanh toán thì trạng thái hợp lệ phải được đáp ứng trước ghi mũi.
8. **Chuẩn bị/đối chiếu lô và tồn:** là nhánh nguồn lực hỗ trợ; trước ghi mũi phải có lô đúng vắc xin, còn hạn và còn tồn. Không ép phát sinh cấp kho tại mọi ca nếu vắc xin đã sẵn sàng ở khu vực tiêm.
9. **Ghi nhận tiêm chủng:** đối chiếu người được tiêm + appointment/registration + clinical evidence + payment condition + lô/tồn; ghi mũi và biến động tồn theo transaction hiện hành.
10. **Theo dõi sau tiêm:** ghi nhận kết quả theo dõi sau mũi đã thực hiện; không tự thay thế xử trí chuyên môn.
11. **Lịch sử và mũi/lịch tiếp theo:** lịch sử tiêm là đầu vào cho theo dõi hành trình; regimen chỉ là tham khảo, lịch hẹn thực tế vẫn do `LichHenTiem` xác nhận.
12. **Báo cáo:** tổng hợp từ dữ liệu đã phát sinh ở các bước trước; đặt cuối luồng.

Quy tắc trình bày:
- Nếu một nghiệp vụ là **cross-cutting** (RBAC, catalog, promotion, inventory, audit), đặt nó trước điểm phụ thuộc hoặc ở tiểu mục nền tảng/hỗ trợ; không giả thành một bước bắt buộc của mọi lượt khách.
- Nếu có nhánh điều kiện, trình bày main flow trước rồi mới alternative/exception ngay sau điểm rẽ.
- Cùng một thứ tự phải được dùng lại xuyên Chương 1 → Chương 2 → Chương 3 → Chương 4 → Chương 5 để người đọc theo được một hành trình duy nhất.
- Không đổi thứ tự chỉ vì mã UC cũ. Mã UC/số hình sẽ được đánh lại sau khi khóa nội dung.

### THESIS-R1 — checkpoint Patch 2D.2 / chuyển sang 2D.3

- **Repo hiện hành là Source of Truth cho capability đã triển khai; bộ bài giảng PTTKHTTT là chuẩn phương pháp cho cách phân tích và mô hình hóa UML.** Ví dụ trong bài giảng không tự mở rộng chức năng của đồ án.
- Patch 2D.1 đã khóa 7 Business Use Case và format đặc tả nghiệp vụ; Patch 2D.2 đã khóa tập 13 System Use Case và toàn bộ text đặc tả hệ thống.
- System Use Case phải có mục tiêu/kết quả nhất quán; không tách vụn theo endpoint, nhưng cũng không gộp các mục tiêu độc lập chỉ để giữ mã UC cũ.
- Đặc tả System Use Case dùng **Tác nhân chính, Tổng quan, Mối quan hệ, Dòng sự kiện chính, Dòng sự kiện phụ, Tiền điều kiện, Hậu điều kiện**; các biến thể/ngoại lệ phải bám điểm rẽ thực sự của luồng.
- Trong prose khóa luận ưu tiên thuật ngữ tiếng Việt; tên kỹ thuật/code chỉ giữ khi cần traceability. Quy ước reminder nếu dùng ký hiệu: lần đầu phải viết đầy đủ **48 giờ trước lịch hẹn (T-48), 24 giờ trước lịch hẹn (T-24), 3 ngày sau lịch hẹn (D+3), 7 ngày sau lịch hẹn (D+7)**.
- Các Sequence Diagram cũ không còn được coi là evidence sau khi tập UC thay đổi; redraw chỉ bắt đầu sau khi text tương ứng đã PASS.
- Mục **1.6 Công cụ và công nghệ sử dụng** chỉ giải thích vai trò của công nghệ trong kiến trúc và lý do lựa chọn; không đưa version/package/configuration/command hoặc chi tiết triển khai thuộc Chương 4.
- Recovery phiên tiếp theo: `AGENTS.md → SCOPE.md → Issue #182 → CNTT_KLCN090_THESIS-R1_Patch2D2_TechRationale_Final.docx → code/test/contract chỉ khi sơ đồ cần xác minh chi tiết`.
- **Patch 2D.3 NEXT:** redraw Business Use Case tổng thể + 7 Activity Diagram theo text Business UC đã khóa; renumber/caption đồng bộ trong cùng patch.

### NEXT
1. **#168 UI-F03**.
2. **RF-02C**.
3. **PAY-R1**.
4. **DATA-R2**.
5. **S8** final integration/end-to-end.

### PARKED sau core/S8
- **REGIMEN-WORDING follow-up** — rà và chuẩn hóa các wording người dùng nhìn thấy như “phác đồ tham khảo”, “đối tượng tham khảo”, disclaimer lịch/chỉ định theo quy định/hướng dẫn chính thức còn hiệu lực; không tự mở lại clinical rule engine.
- **#173 NOTIFY-R1** — shared Email/SMS delivery + provider SMS thật.
- AUTH-PWLESS.
- ADDR-R1.
- INV-R1.
- WEB-IA-R1.
- Các expansion khác chỉ mở khi người dùng reprioritize.

## Core thesis scope

### Nền tảng
- Customer: **React Web**.
- Nhân viên: **Windows WPF**.
- Shared backend: **ASP.NET Core API**.
- Database: **SQL Server**.
- Kiến trúc: modular monolith; không microservice hóa nếu không có nhu cầu đã chốt.

### 7 quy trình nghiệp vụ
1. Nhập vắc xin từ nhà cung cấp.
2. Tiếp nhận và quản lý hồ sơ khách hàng/người được tiêm.
3. Đăng ký dịch vụ tiêm chủng.
4. Quản lý lịch hẹn và nhắc lịch.
5. Khám sàng lọc và chỉ định tiêm chủng.
6. Thanh toán dịch vụ.
7. Thực hiện tiêm chủng và theo dõi sau tiêm.

### 6 vai trò nghiệp vụ cốt lõi
Khách hàng, Lễ tân, Bác sĩ, Điều dưỡng, Thu ngân, Nhân viên kho.

### Invariant nghiệp vụ
- Web/Windows dùng chung API + SQL, không có client-side source-of-truth riêng cho dữ liệu nghiệp vụ.
- Customer ownership/privacy kiểm tại API.
- Employee permission kiểm tại API theo actor/capability hiện hành.
- Appointment, registration, clinical, payment, inventory và vaccination phải liên kết nguồn thật; không tạo dữ liệu giả để vượt gate.
- Mọi lượt tiêm cần clinical evidence theo contract hiện hành; không tự quyết y khoa.
- Tiền/tồn kho/multi-row critical write dùng transaction/concurrency phù hợp và audit.
- Vaccine cấp/xuất từ kho có thể là bước tùy trường hợp trong mô tả nghiệp vụ; không ép nhân viên kho tham gia mọi ca nếu vaccine đã có sẵn tại khu vực tiêm.
- Reminder/email đã gửi là historical evidence; reschedule/cancel không được rewrite provider outcome cũ.

## Module summary

| Module | Trạng thái hiện hành |
|---|---|
| S0 nền SQL/API/CI | DONE |
| S1 Identity/Auth | Core DONE; expansion parked |
| S2 Catalog | Core/Windows refinement DONE; RF-02C Web còn NEXT |
| S3 Customer/Recipient profiles | DONE |
| S4 Registration/Payment core | DONE; PAY-R1 NEXT |
| S5 Appointment/Clinical/Vaccination/Aftercare/Reminder | Core DONE; RF-05F/RF-05F.1/RF-05G #170 DONE |
| S6 Promotion | DONE |
| S7 Reports | DONE |
| S8 Integration | NOT STARTED |
| X1 Inventory read | DONE |
| X2.1 Receive new lot | DONE |
| X2.2.1 Receive history | DONE |
| X2.2.2 advanced receive/supplier | PARKED |

## Scope limits

Mức đích là **khóa luận cử nhân chạy được và giải thích được**, không mặc định production hospital platform.

Không tự mở:
- microservices, HA/DR, cloud/multi-region, production-scale ops;
- medical advice engine/auto eligibility;
- auto-create clinical indication or fake inventory;
- customer self-service reschedule/cancel nếu chưa có slice riêng;
- slot/capacity/room/staff scheduling engine trong #175;
- refund/partial payment/installment ngoài slice đã chốt;
- full supplier/warehouse ERP;
- SMS/ZNS/push provider ngoài #173;
- phone OTP/passwordless ngoài AUTH-PWLESS;
- general-purpose permission matrix UI;
- module/status/table mới chỉ để làm UI tiện hơn khi có thể derive từ dữ liệu thật.

## Development / data rules

- Synthetic personal data được phép cho test/dev; không dùng thông tin người thật.
- Demo vaccine/medical content có ý nghĩa phải lấy từ nguồn công khai đáng tin cậy; unit/integration logic có thể dùng fixture synthetic tối giản nếu nội dung y khoa không phải đối tượng kiểm thử.
- Secret/real credential ngoài Git.
- Không reset/reseed/migrate demo DB tùy tiện; backup trước migration có rủi ro.
- Demo hiện hành dùng launcher/scripts trong README; operational instructions chỉ đọc khi task cần runtime.

## Status discipline

- Chỉ một business slice ACTIVE.
- `SCOPE.md` giữ trạng thái hiện hành, **không** lưu toàn bộ test transcript/CI history.
- Active issue giữ acceptance/scope-stop.
- PR giữ diff, test/runtime evidence và review.
- #76 chỉ dùng checkpoint/recovery khi cần, không phải tài liệu bắt buộc phải đọc trong mọi task.
- Sau mỗi merge: cập nhật DONE/ACTIVE/NEXT/PARKED rồi dừng chờ người dùng xác nhận.
