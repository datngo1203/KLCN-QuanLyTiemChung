# SCOPE — KLCN090: Hệ thống quản lý tiêm chủng

**Nguồn sự thật hiện hành về phạm vi và trạng thái.** Chi tiết acceptance/test/CI nằm ở active issue, PR và Git history; không lưu lại toàn bộ lịch sử ở đây.

Cập nhật: **09/11/2026**. Hạn nộp theo kế hoạch R01: **08/11/2026**.

## Current State

### THESIS session routing — hard rule
- **Đây là THESIS session, không implement code; chỉ dùng `main` để verify claim implementation.**
- Source of truth cho **báo cáo/UML/checkpoint THESIS** là nhánh thesis hiện hành (hiện tại: `docs/thesis-r1-current-state`).
- Source of truth để **xác minh capability implementation** là `main` mới nhất hoặc active implementation branch nếu feature chưa merge.
- Không sửa source code ứng dụng, không mở feature implementation, không cherry-pick/rebase/merge code trong THESIS session.
- Không dùng trạng thái code của nhánh thesis để kết luận capability nếu nhánh thesis đang behind `main`; phải verify claim trên implementation HEAD hiện hành.
- **Evidence-first / no invention:** không tự bịa, không suy diễn, không “điền chỗ trống” capability. Mọi claim về hệ thống hiện có trong prose/UML/spec phải truy vết được tới **code/contract/test trên exact `main` HEAD** hoặc checkpoint **DONE đã merge** và vẫn còn hiệu lực.
- **ACTIVE/NEXT/PARKED ≠ implemented.** Issue/roadmap/menu/module name chỉ cho biết ý định/phạm vi; không được dùng làm bằng chứng rằng chức năng đã tồn tại.
- Không suy capability từ tên endpoint, màn hình, class, permission code hoặc dữ liệu nếu chưa kiểm semantic/behavior liên quan. Khi bằng chứng chỉ hỗ trợ một phần, claim phải thu hẹp đúng phần đó.
- Trước mỗi audit System UC/Sequence/Class có claim implementation đáng kể, ghi nhận **exact `main` HEAD**; nếu `main` đổi trong lúc audit, re-check phần bị ảnh hưởng trước khi khóa semantic.
- Chat điều phối **CODE** không được recovery từ nhánh thesis; chat điều phối **THESIS** không được dùng `main` làm nơi lưu rule/checkpoint báo cáo.


### DONE gần nhất
- **REGIMEN-R1 #180 / PR #181** — Catalog hỗ trợ calendar interval theo mũi neo với `Days|Weeks|Months|Years`, giữ legacy day intervals; Windows A04 quản trị structured regimen; Customer Web hiển thị timeline + provenance tham khảo. Native A04 full-shell smoke PASS sau fix hiển thị selector; CI #387 PASS đủ backend/windows/web. Squash main: `04b932e8e4df6ec865ea90f19ad2ea731c1d77ac`.
- **RF-05G #170 / PR #179** — tự động nhắc trước lịch hẹn 48/24 giờ và liên hệ sau lịch hẹn 3/7 ngày trên appointment hiện hành; DB idempotency + retry/revalidation + A09 history/wording người dùng. CI #382 PASS; native WPF keyboard smoke PASS ở HEAD trước wording-only patch. Squash main: `19f8218da0e6a505bf11ed3ad5daf8b087abe72b`.
- **RF-05F.1 #175 / PR #178** — Customer Web lưu ngày+giờ mong muốn với provenance `CoGioDuKien`; A09 dùng preference làm gợi ý nhưng `LichHenTiem` xác nhận độc lập; confirm/reschedule/cancel tự enqueue lifecycle Email. CI #377 và Web/WPF runtime gates PASS. Squash main: `90873bbf42853ad9f00810d600ae96da9705e3bb`.
- **RF-05E #166 / PR #172** — email reminder thật, outbox/dispatcher, Windows A09; real Development SMTP acceptance đã PASS.
- **RF-05F #167 / PR #174** — reschedule/cancel cùng `LichHenTiem`, RowVersion, clinical lock, reminder reconciliation, Customer Web `DaHuy`, outcome-unknown reconcile. Squash main: `bfd6ac0be813e549d15ff799ba7d9c972547c0c1`.

Các nền tảng/core trước đó đã tích hợp: S0–S7 theo phạm vi đã nghiệm thu, AUTHZ-R1, AUTH-R2, DATA-R1, RF-05B/C/D, S6 integration, X1 kho đọc, X2.1 nhập lô mới, X2.2.1 lịch sử tiếp nhận. Bằng chứng chi tiết giữ trong issue/PR tương ứng.

### ACTIVE
- **THESIS-R1 #182** — đồng bộ báo cáo khóa luận với trạng thái repo hiện tại. **Patch 2D.2 DONE (text baseline):** audit implementation hiện hành và khóa tạm tập **13 System Use Case** ở mức mục tiêu nghiệp vụ/phần mềm, không giữ máy móc 10 UC cũ. **Course-source re-audit 09/11/2026 đã mở lại riêng semantic gate của Hình 2.9 trước khi redraw** để kiểm thêm `single-session / interactive / complete` và special actor `Thời gian`; chưa coi bộ 13 UC là diagram-level PASS cho tới khi gate này được chốt. Toàn bộ bảng đặc tả 2.2.2 dùng format bài giảng **Tên Use Case / Tác nhân chính / Tổng quan / Mối quan hệ / Dòng sự kiện chính / Dòng sự kiện phụ / Tiền điều kiện / Hậu điều kiện**. Đã loại các overclaim cũ về mobile app/chứng nhận điện tử, quét mã vắc xin, cảnh báo khẩn tự động, điều chuyển/điều chỉnh kho, cổng thanh toán production và reminder chỉ mô phỏng; hệ thống không tự quyết định eligibility y khoa. Toàn bộ prose của 13 bảng đã Việt hóa các thuật ngữ implementation như reschedule/cancel/reconcile/provider/clinical/fail-closed/audit; các mốc reminder được định nghĩa ngay lần đầu: **48 giờ trước lịch hẹn (T-48), 24 giờ trước lịch hẹn (T-24), 3 ngày sau lịch hẹn (D+3), 7 ngày sau lịch hẹn (D+7)**. 10 Sequence Diagram cũ đã loại khỏi 2.2.2 vì không còn khớp text; **Hình Use Case hệ thống tổng thể và Sequence Diagram mới vẫn PARKED để redraw sau**. **Micro-patch 1.6 DONE:** mở rộng mục Công cụ và công nghệ sử dụng theo nguyên tắc **công nghệ dùng ở đâu → vì sao phù hợp với kiến trúc/phạm vi đề tài**, giữ ranh giới không lặp nội dung cài đặt của Chương 4. **Micro-patch print QA DONE:** sửa Danh mục hình/bảng để dòng xuống hàng không còn kế thừa first-line indent thân bài; đặt left/first-line = 0, chừa right indent 0,5 cm và tab leader phải tại 14,5 cm để caption dài không tràn vùng số trang. Sửa cỡ chữ dòng thành viên **Nguyễn Ngọc Quý** trên cả hai trang bìa từ 13 pt về 14 pt cho đồng nhất với hai thành viên còn lại; làm sạch cached pagination markers ở phần đầu để tránh trang trắng phát sinh khi Word repaginate/refresh field. **Micro-patch print layout DONE:** xóa số trang bị dính ở caption thật nhưng giữ số trang trong Danh mục hình/bảng; phóng Hình 1.3a lên hết chiều rộng vùng nội dung để giảm khoảng trắng mà vẫn đọc rõ; trang Nhận xét của giảng viên hướng dẫn được kéo dài vùng dòng chấm gần trọn trang và đưa khối chữ ký xuống thấp; bìa ngoài dùng khung đôi với họa tiết hình học nhẹ ở bốn góc, cạnh trái lùi 18 mm để chừa vùng đóng gáy, các cạnh còn lại 10 mm. Latest working artifact `CNTT_KLCN090_THESIS-R1_Patch2D2_PrintLayout_Final.docx`, SHA-256 `d29920def22a3ccde28bed8d62a713d78028de232b729eba37f7a31d01a44cda`. Render QA **87 trang PASS**; so với baseline CaptionLayout QA chỉ trang 1, 4, 28 thay đổi, 84 trang còn lại pixel-identical. **NEXT:** Patch 2D.3 — khóa/redraw Business Use Case tổng thể + 7 Activity Diagram theo 7 đặc tả đã PASS.

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

### THESIS-R1 — HUIT UML Visual & Semantic Profile v2

Nguồn và ưu tiên:
- **Ngữ nghĩa mô hình** bám bộ bài giảng PTTKHTTT của HUIT đã được dùng để khóa Chương 2.
- **Ký hiệu trực quan** ưu tiên cách biểu diễn HUIT/Rational Rose khi không xung đột UML.
- Khi ví dụ công cụ/bài thực hành xung đột với UML chuẩn, **UML chuẩn được ưu tiên**. PlantUML chỉ là renderer, không phải Source of Truth nghiệp vụ.
- UML trong luận văn dùng **PlantUML source-first**, giữ source `.puml` để sửa/re-render có kiểm soát; hình chèn vào luận văn ưu tiên SVG/PNG sắc nét, monochrome, không shadow/gradient.

- **Course-source audit 09/11/2026:** profile v2 đã được đối chiếu lại trực tiếp với bộ bài giảng HUIT người dùng cung cấp, không chỉ kế thừa chat cũ. Khi cần spot-check shape/semantic, ưu tiên đúng phần sau:
  - Chương 4 — Mô hình hóa nghiệp vụ, **tr. 49–60**: Business Worker, Business Entity, Business Object Diagram, Activity symbols/Object Flow/Swimlane.
  - Chương 5 — Mô hình hóa chức năng, **tr. 3–21 và 27–28**: system boundary/actor/use case, quality gate, special actors, include/extend/generalization.
  - Bài giảng thực hành PTTKHTTT, **tr. 22–24**: Sequence Diagram.
  - Chương 6 — Mô hình hóa cấu trúc, **tr. 2–16, 24–30, 47–50, 77–82**: Class Analysis và truy vết từ Business Entity/Use Case/Sequence.
  - Chương 7 — Thiết kế lớp, **tr. 21–30**: tinh chế thuộc tính/mối kết hợp/phương thức ở Class Design.
- Nếu các file bài giảng cùng nội dung xuất hiện với nhiều tên, chỉ cần đọc một bản đã xác minh; không lặp retrieval chỉ vì tên file khác.

Use Case:

**Nguyên tắc chọn ký hiệu — phải biết WHEN/WHY trước khi biết HOW:**
- Không vẽ theo kiểu “có ký hiệu này trong bài giảng nên đưa vào”. Mỗi node/quan hệ phải trả lời được 3 câu: **(1) Nó biểu diễn khái niệm gì? (2) Điều kiện nào khiến nó xuất hiện? (3) Vì sao không dùng ký hiệu gần giống khác?**
- Nếu không giải thích được ba câu trên từ đặc tả/bài giảng/implementation thì **không vẽ phần tử đó**.
- **Decision vs Fork:** Decision dùng khi chỉ **một nhánh loại trừ** được chọn theo guard; Fork dùng khi **nhiều nhánh thực sự chạy song song**. Merge gom các nhánh loại trừ; Join đồng bộ các nhánh song song. Không chọn Fork/Join chỉ vì layout đẹp.
- **Control Flow vs Object Flow:** Control Flow trả lời “bước nào diễn ra tiếp theo”; Object Flow trả lời “đối tượng/dữ liệu nào được truyền, đọc hoặc cập nhật giữa các hoạt động”. Vì vậy Activity có tuần tự nhưng không có dữ liệu trao đổi thì chỉ cần Control Flow.
- **Activity Final vs Flow Final:** Activity Final kết thúc **toàn bộ activity**; Flow Final chỉ kết thúc **một dòng/nhánh**, trong khi nhánh khác có thể tiếp tục.
- **Include vs Extend:** `include` chỉ dùng khi UC A **luôn cần** thực hiện hành vi UC B như phần bắt buộc có thể tái sử dụng; `extend` chỉ dùng khi hành vi bổ sung xảy ra **có điều kiện** tại điểm mở rộng của UC cơ sở. Dependency dữ liệu, tiền điều kiện hoặc “A xảy ra trước B” không phải include/extend.
- **Generalization Actor:** dùng khi actor chuyên biệt thật sự **kế thừa toàn bộ vai trò/UC chung** của actor tổng quát và có thể có thêm UC riêng. Không tạo actor cha chỉ để giảm số đường nối.
- **Business Worker vs Business Actor:** Worker là vai trò **bên trong quy trình nghiệp vụ** thực hiện công việc; Business Actor đứng **ngoài ranh giới nghiệp vụ** và trao đổi giá trị với nghiệp vụ. Cùng là con người nhưng khác vị trí/trách nhiệm nên khác ký hiệu.
- **System Actor:** chỉ xuất hiện khi thực thể thật sự **tương tác trực tiếp với phần mềm** qua giao diện/giao tiếp hệ thống. Một Business Worker chỉ trở thành System Actor khi bước nghiệp vụ của họ được đưa vào phạm vi tự động hóa.
- **Business Entity/Object:** dùng khi một thực thể/thông tin **được worker xử lý hoặc sử dụng** và việc thấy nó giúp hiểu cấu trúc/luồng dữ liệu nghiệp vụ; không thêm nếu chỉ là danh từ phụ hoặc dữ liệu suy ra không cần thiết.
- **Sequence Return Message:** chỉ vẽ khi kết quả trả về có giá trị làm rõ luồng; không vẽ phản hồi cho mọi call. **Self Message** chỉ khi cùng một object gọi xử lý khác của chính nó/đệ quy. **Destruction X** chỉ khi vòng đời object thật sự kết thúc trong interaction.
- **Association vs Aggregation vs Composition:** Association là quan hệ ngữ nghĩa thông thường; Aggregation chỉ khi có quan hệ whole–part nhưng part vẫn tồn tại độc lập/chia sẻ được; Composition chỉ khi whole quản lý vòng đời part và part không tồn tại độc lập theo mô hình. Không dùng diamond để trang trí.
- **Association Class:** chỉ dùng khi **chính mối quan hệ** giữa hai lớp có thuộc tính/hành vi riêng; nếu dữ liệu thuộc về một lớp độc lập thì mô hình thành lớp thường.
- Mọi lựa chọn ký hiệu phải truy vết được về **đặc tả nghiệp vụ/System UC/Sequence/Class Analysis** tương ứng; layout không được thay đổi semantic.

**Phân tầng thuật ngữ bắt buộc — Business ≠ System:**
- **Tầng nghiệp vụ (Business):** mô tả **quy trình của trung tâm tiêm chủng ngoài góc nhìn phần mềm**. Dùng các khái niệm **Business Actor, Business Worker, Business Entity, Business Use Case, Business Activity/Business Object**. Câu hỏi chính: *nghiệp vụ diễn ra thế nào, ai làm gì, xử lý đối tượng/thông tin nào?*
- **Tầng hệ thống (System):** mô tả **phần mềm hỗ trợ/tự động hóa phần nào của nghiệp vụ**. Dùng **System Actor, System Use Case, System Boundary**, sau đó đặc tả/Sequence ở mức hệ thống. Câu hỏi chính: *ai sử dụng phần mềm và phần mềm phải cung cấp chức năng gì?*
- **Không dùng thuật ngữ lai** như “Business System Use Case”, “Business Actor hệ thống”, “Business Worker hệ thống” nếu không trích nguyên văn nguồn. Trong khóa luận phải gọi rõ **Use Case nghiệp vụ** hoặc **Use Case hệ thống**.
- Một **Business Worker** có thể trở thành **System Actor** khi vai trò đó trực tiếp sử dụng phần mềm, nhưng đó là **chuyển tầng mô hình**, không phải cùng một phần tử UML được giữ nguyên shape.
- Một **Business Actor** không tự động trở thành System Actor. Chỉ trở thành System Actor nếu trong phạm vi tự động hóa, đối tượng đó thật sự tương tác trực tiếp với phần mềm.
- Một **Business Use Case** có thể được hỗ trợ bởi **một hoặc nhiều System Use Case**; không ép quan hệ 1–1 và không mang nguyên tên/shape business sang sơ đồ hệ thống.
- **Business Entity** là đối tượng/thông tin nghiệp vụ; khi sang Class Analysis có thể trở thành ứng viên lớp nếu hệ thống cần quản lý thông tin về nó, nhưng không được coi hai phần tử là cùng loại ký hiệu.

- **Business boundary**: hình chữ nhật có tên phạm vi nghiệp vụ ở phía trên; Business Use Case nằm trong, Business Actor nằm ngoài.
- **Business Actor**: dùng ký hiệu business actor của PlantUML/Rational Rose (PlantUML `actor/` hoặc `:Tên:/`) khi renderer hỗ trợ; chỉ dùng cho đối tượng ngoài ranh giới nghiệp vụ cung cấp đầu vào/nhận đầu ra.
- **Business Use Case**: ellipse business có nét chéo theo HUIT/Rational Rose (PlantUML `usecase/`); tên là cụm động từ.
- **System Actor / System Use Case**: dùng actor và ellipse UML thường, **không dùng nét business**; system boundary là phạm vi phần mềm.
- **Association Actor–Use Case**: **đường liền, không mũi tên**. Với PlantUML dùng `--`, không dùng `-->`. Đây là rule khóa cho cả Business Use Case và System Use Case.
- `<<include>>`: dependency nét đứt, mũi tên từ use case cơ sở tới use case được bao gồm.
- `<<extend>>`: dependency nét đứt, mũi tên từ use case mở rộng tới use case cơ sở.
- Generalization: đường liền + tam giác rỗng hướng về phần tử tổng quát.
- **System UC quality gate theo Chương 5 HUIT:** một System Use Case tốt phải có mục tiêu/kết quả trọn vẹn, **single-session**, có **System Actor tương tác** và cho kết quả nhất quán. Xử lý chỉ tồn tại như fragment trong ngữ cảnh của xử lý khác thì không tách thành UC riêng. CRUD có thể gom khi cùng một mục tiêu quản lý.
- System Use Case là mô hình chức năng, không đặt tên theo endpoint, màn hình, framework hay công nghệ giao tiếp.
- **Primary/Secondary Actor là phân loại ngữ nghĩa, không phải shape khác nhau:** đều dùng System Actor thường. Primary Actor trực tiếp dùng/thực hiện chức năng; Secondary Actor cung cấp/nhận dịch vụ hỗ trợ.
- HUIT cho phép **tác nhân đặc biệt `Thời gian`** đối với chức năng chạy theo lịch/định kỳ. Chỉ dùng khi một hành vi phần mềm thực sự được kích hoạt bởi thời điểm; không dùng `Thời gian` để biểu diễn thứ tự quy trình.
- Không dùng đường/mũi tên giữa các use case chỉ để diễn tả thứ tự nghiệp vụ; thứ tự thuộc Activity/Sequence/specification.

Business Object Diagram:
- Khi báo cáo yêu cầu **Sơ đồ đối tượng nghiệp vụ**, phải phân biệt đúng ba loại: **Business Actor** (đối tượng ngoài nghiệp vụ), **Business Worker/thừa tác viên** (vai trò thực hiện công việc bên trong) và **Business Entity/thực thể nghiệp vụ** (sự vật/thông tin được worker xử lý).
- **Business Worker shape theo HUIT/Rational Rose:** biểu tượng người hình que nằm trong oval/tròn có nét chéo đặc trưng. Worker biểu diễn **vai trò**, không phải sao chép tên chức vụ máy móc; không thay bằng System Actor thường hoặc class box.
- **Business Entity shape theo HUIT/Rational Rose:** hình tròn/oval có nét chéo ở góc dưới phải, đặt trên đường đáy, tên bên dưới. Dùng cho thực thể thông tin (hồ sơ, chứng từ, báo cáo, tập tin/CSDL...) hoặc thực thể vật thể khi thật sự tham gia nghiệp vụ.
- Association trong Business Object Diagram là đường liền; đặt **multiplicity ở hai đầu** khi đặc tả nghiệp vụ hỗ trợ. Không tự suy ra bản số chỉ để hình “đủ”.
- Nếu PlantUML không có primitive giữ đúng silhouette HUIT, dùng helper/macro/source-first riêng để tái tạo shape; **không hạ cấp shape** thành actor/class thông thường vì renderer tiện hơn.
- Business Object Diagram không phải ERD/Class Diagram: chỉ đưa actor/worker/entity trực tiếp tham gia Business Use Case tương ứng.

Activity Diagram:
- Dùng initial node chấm đen, **activity final** vòng tròn kép, **flow final** vòng tròn có dấu X, action hình chữ nhật bo góc, decision/merge hình thoi. Activity final kết thúc toàn activity; flow final chỉ kết thúc một nhánh/dòng khi activity còn tiếp tục.
- **Control Flow = nét liền có mũi tên**; **Object Flow = nét đứt có mũi tên** theo bảng ký hiệu Chương 4. Không dùng cùng line style cho hai loại flow.
- Nhánh decision phải có guard rõ như `[Hợp lệ]`, `[Không hợp lệ]`; merge dùng khi cần gom các nhánh loại trừ.
- Fork/Join dùng **thanh đen dày** và chỉ khi có xử lý song song thực sự; Decision/Merge dùng cho các nhánh loại trừ.
- Swimlane biểu diễn **trách nhiệm thực hiện hành động**; không tạo lane rỗng chỉ vì một vai trò được nhắc tới.
- Trong Business Activity, vai trò nội bộ được hiểu là **Business Worker/thừa tác viên**; Business Actor không mặc định trở thành swimlane.
- Business Entity/Object Node chỉ đưa vào khi giúp hiểu luồng dữ liệu nghiệp vụ (ví dụ hồ sơ, lịch hẹn, lô vắc xin, kết quả khám); không biến Activity Diagram thành ERD.
- Không dùng lane “Hệ thống” để kéo implementation automation vào mô hình nghiệp vụ nếu đặc tả Business Use Case không có vai trò nghiệp vụ tương ứng.
- **Layout chuẩn được lấy từ Hình 2.2 compact-fixed đã duyệt:** không dùng `left to right direction` với Activity Diagram có swimlane kiểu `|...|`; luồng chính ưu tiên top-to-bottom.
- Nhánh thay thế/ngoại lệ phải **merge về luồng chính ngay khi điều kiện đã được xử lý**, tránh lặp lại cả chuỗi action và tránh connector dài chạy vòng trang.
- Ưu tiên connector **dọc/ngang ngắn, ít giao cắt**; không chấp nhận line xéo dài, line chụm/dính khó đọc hoặc auto-routing tạo vòng cung không cần thiết.
- Toàn bộ Activity Diagram nghiệp vụ phải có **outer frame đầy đủ bốn cạnh (trên/dưới/trái/phải)**. Outer frame là **khung ngoài duy nhất**: sau render phải loại hai biên dọc ngoài cùng do PlantUML swimlane tự sinh, chỉ giữ **separator giữa các lane**. Hình một lane (ví dụ Bác sĩ) không giữ hai biên swimlane bên trong; chỉ còn outer frame để đồng nhất batch.
- Nội dung activity giữ ngắn gọn, tránh nhồi một câu đặc tả dài vào một node; semantic vẫn phải truy vết được về dòng cơ bản/dòng thay thế tương ứng.
- **Activity Diagram ưu tiên thể hiện flow, không lặp lại bảng đặc tả.** Bảng đặc tả giữ chi tiết nghiệp vụ; sơ đồ chỉ giữ các hành động/điểm rẽ có giá trị để nhìn được luồng. Được phép gộp các bước liên tiếp cùng chủ thể/cùng mục đích thành một action nếu không làm mất decision, trách nhiệm hoặc kết quả nghiệp vụ.
- Khi tối ưu mật độ trang, **không giảm font để nhồi hình**. Ưu tiên theo thứ tự: rút gọn wording action → gộp action cùng mục đích → giảm khoảng cách node/rank hợp lý → chỉ sau đó mới cân nhắc scale hình. Mục tiêu là giảm khoảng trắng nhưng vẫn đọc rõ khi in A4.
- **HUIT data/object-flow notation:** khi cần thể hiện dữ liệu nghiệp vụ được đọc/ghi trong Activity Diagram, thêm **Business Entity/Object** theo ký hiệu bài giảng HUIT/Rational Rose: hình tròn có nét chéo ở góc dưới phải, đặt trên một đường ngang, tên đối tượng bên dưới. Ví dụ: `Hồ sơ người được tiêm`, `Đăng ký dịch vụ`, `Lịch hẹn`, `Kết quả khám sàng lọc`, `Giao dịch thanh toán`, `Lô vắc xin`, `Mũi tiêm`.
- Nối Business Entity/Object với activity bằng **ObjectFlow** để biểu diễn việc **truy xuất hoặc cập nhật dữ liệu** như bài thực hành. Quy ước của luận văn: `Business Entity → Activity` khi activity đọc/nhận dữ liệu; `Activity → Business Entity` khi activity tạo/ghi/cập nhật dữ liệu. Đây là quy ước hướng hóa để làm rõ read/write; bài giảng xác nhận ObjectFlow dùng cho truy xuất/cập nhật nhưng không bắt buộc cách ghi nhãn hướng.
- Chỉ thêm Business Entity/Object khi nó làm rõ **đầu vào/đầu ra dữ liệu quan trọng** của flow; không thêm cho mọi biến/trạng thái và không biến Activity Diagram thành ERD. Mặc định ưu tiên khoảng **1–3 entity quan trọng/sơ đồ**, nhiều hơn chỉ khi thực sự cần.
- **Entity justification gate:** mỗi Business Entity/Object phải giải thích được rõ “activity nào đọc/nhận nó?” hoặc “activity nào tạo/ghi/cập nhật nó?”. Nếu không trả lời được bằng một activity cụ thể trong đặc tả thì **không vẽ entity đó**.
- **ObjectFlow geometry:** đặt entity gần activity liên quan nhất, ưu tiên cùng cao độ; connector đi **thẳng từ rìa entity tới rìa activity**. Chỉ dùng một góc vuông khi không thể nối thẳng; không dùng gutter/vòng ngoặt chỉ để routing, không nối vào tâm shape, không để ObjectFlow cắt qua activity/decision khác.
- Entity không thuộc swimlane nào về mặt trách nhiệm; vị trí trái/phải chỉ phục vụ độ rõ. Có thể đặt các entity của cùng một sơ đồ ở hai phía khác nhau nếu giúp đường nối ngắn và không giao cắt.
- Với dữ liệu có thể suy ra từ entity khác, chỉ giữ cả hai khi mỗi entity có ý nghĩa nghiệp vụ riêng và activity thực sự đọc/ghi chúng; tránh thêm entity chỉ để “đủ dữ liệu”.

Sequence Diagram:
- Ký hiệu gốc HUIT: **Actor**, Object/lifeline head, lifeline nét đứt theo trục thời gian từ trên xuống, **execution occurrence/activation** là thanh chữ nhật hẹp trên lifeline, message giữa các participant; Return Message dùng nét đứt, Self Message quay về chính lifeline, destruction marker là `X` khi object thực sự kết thúc vòng đời.
- Không vẽ return cho mọi call; chỉ thêm return/self/destruction khi làm rõ luồng hoặc kết quả. Thứ tự dọc là thứ tự thời gian, không routing connector trái với thời gian chỉ để hình đẹp.
- Theo góp ý giảng viên đã khóa ở THESIS-R1, sequence được đặt ngay sau bảng đặc tả System UC dù mẫu Khoa gốc đặt Sequence ở Chương 3. Khi nằm cạnh đặc tả Chương 2, sequence giữ **mức phân tích/logical interaction**: không vẽ endpoint/service/repository/database như implementation diagram.
- Participant phải có trách nhiệm rõ trong UC; actor ở ngoài hệ thống, object là đối tượng logic tham gia xử lý. Tên message dùng động từ/nghiệp vụ dễ hiểu; chi tiết code chỉ giữ khi cần traceability.
- Sequence phải nhất quán với Class Analysis: object/message có thể là evidence để phát hiện lớp/phương thức, nhưng không tự tạo lớp chỉ để làm sequence cân đối.

Class Diagram mức phân tích:
- Dùng **class box UML**; không dùng Business Worker/Business Entity icon và không tự áp mô hình `boundary/control/entity` nếu bài giảng/đặc tả hiện hành không yêu cầu. Chương 6 xác định Class Analysis từ thực thể nghiệp vụ, cụm danh từ, phân loại và hoạt động Use Case.
- Class Analysis ưu tiên tên lớp + thuộc tính đủ diễn đạt trạng thái ở mức phân tích; không nhồi kiểu cài đặt/visibility/foreign key. Phương thức chỉ thêm khi trách nhiệm/message từ Use Case/Sequence hỗ trợ.
- Association là đường liền; ghi multiplicity và tên/vai trò khi giúp rõ ngữ nghĩa. **Association Class** chỉ dùng khi chính mối kết hợp có thuộc tính/hoạt động riêng.
- Aggregation dùng **diamond rỗng ở phía whole** cho whole–part chia sẻ; Composition dùng **diamond đặc ở phía whole** khi whole quản lý vòng đời part; Generalization dùng tam giác rỗng hướng về lớp tổng quát.
- Aggregation/Composition/Generalization chỉ dùng khi quan hệ miền nghiệp vụ thực sự hỗ trợ, không dùng trang trí.
- Class Analysis là mô hình khái niệm/domain, **không sao chép schema, EF entity, DTO hoặc API contract**.

Class Diagram mức thiết kế:
- Chỉ tinh chế từ Class Analysis khi bước thiết kế ACTIVE. Có thể bổ sung kiểu thuộc tính, reference, method và visibility. Theo Chương 7: `+` public/toàn cục, `#` protected/bảo vệ, `-` private/cục bộ; thuộc tính mặc định ưu tiên private/cục bộ nếu không có lý do khác.
- Có thể chuyển association class thành lớp thiết kế độc lập khi cần reference/implementation; giữ multiplicity/whole-part/generalization theo semantic, không biến sơ đồ thiết kế thành bản sao CSDL.
- Thiết kế lớp ưu tiên cohesion cao, coupling thấp; không gom lớp chỉ để sơ đồ ít box hơn.

Trình bày/in ấn:
- Sơ đồ đen–trắng, nền trắng, nét rõ; không gradient, icon trang trí hoặc màu phụ thuộc máy in.
- Ưu tiên bố cục đọc được trên A4 ở kích thước in thực tế; không thu nhỏ để nhồi quá nhiều phần tử.
- Mỗi tiểu mục giữ adjacency đã khóa: **heading → bảng đặc tả → sơ đồ/caption**, không thêm prose lặp lại nội dung bảng/hình.
- Một sơ đồ chỉ đưa các thành phần có ý nghĩa cho mục tiêu của sơ đồ; ưu tiên rõ ràng hơn “đầy đủ mọi object”.
- Khi chèn PNG/SVG vào DOCX phải **khóa aspect ratio**; chỉ scale theo một chiều giới hạn rồi suy ra chiều còn lại, không đặt width/height độc lập làm méo ellipse/actor/frame.
- Business Use Case ellipse phải giữ hình học chuẩn sau khi chèn Word; nếu preview PlantUML đúng nhưng DOCX bị méo thì lỗi nằm ở insertion/scale và phải sửa ở bước chèn, không sửa shape trong source để “bù méo”.
- Không dùng page break cưỡng bức chỉ để giữ hình trên một trang; tối ưu source/layout/kích thước trước, chỉ chấp nhận hình sang trang mới khi cần để giữ khả năng đọc khi in.
- Giữa **bảng đặc tả và sơ đồ ngay sau nó** phải có khoảng trắng thị giác rõ ràng; paragraph chứa hình đặt **Before tối thiểu 6 pt** (ưu tiên 8–12 pt nếu trang vẫn cân), không để outer frame chạm hoặc dính sát đường viền dưới của bảng.
- **Rule persistence:** mọi visual/layout rule đã được người dùng chốt phải được checkpoint vào `SCOPE.md` (và issue nếu là gate lớn) **trước khi áp sang các hình/phần tiếp theo**, để recovery session sau không lặp lại lỗi đã sửa.
- **UML density optimization:** giữ Hình 2.1 nếu bố cục tổng thể đang rõ; với Activity ưu tiên compact wording/merge/gộp action trước khi đổi hướng bố cục. Không đổi sang landscape hoặc hai cột chỉ để tiết kiệm trang nếu làm giảm khả năng đọc. Mục tiêu thực tế là giảm khoảng trắng thị giác và số trang khi có thể, không ép đạt một số trang cụ thể.
- **Adaptive figure sizing / final-size readability:** HUIT không khóa một kích thước cm cố định cho hình; hình được scale theo **kích thước cuối khi in**, không ép tất cả cùng width/height. Với A4 và lề hiện hành, vùng nội dung ngang tối đa khoảng 15 cm; chỉ dùng gần full-width khi sơ đồ thực sự cần.
- Mục tiêu nội bộ cho UML: ưu tiên chiều rộng khoảng **10,5–13,5 cm**, chiều cao khoảng **10–15 cm**; **soft cap ~15,5 cm**. Chỉ cho phép 16–18 cm khi giảm thêm làm chữ/ký hiệu khó đọc; 19 cm trở lên chỉ khi thật sự cần.
- Khi scale DOCX phải **khóa aspect ratio** và chỉ thay đổi theo một hệ số đồng nhất. Không chỉnh width/height độc lập.
- Chọn kích thước bằng render QA thực tế: chỉ giữ mức scale nếu chữ/guard/entity/caption vẫn đọc rõ ở bản in và pagination/khoảng trắng thực sự tốt hơn. Không giảm font UML hoặc phá bảng chỉ để đạt một số trang cụ thể.
- Rule này áp dụng lại cho các sơ đồ sau: sau khi semantic/layout của hình PASS, tối ưu **kích thước chèn** như một bước riêng trước checkpoint cuối.

### THESIS-R1 — Patch 2D.3g Adaptive figure sizing DONE

- **Semantic/UML source giữ nguyên 2D.3f**; patch này chỉ tối ưu **kích thước chèn DOCX** theo rule final-size readability, không sửa nội dung, entity set hoặc ObjectFlow.
- Hình 2.1–2.8 được scale theo một hệ số đồng nhất/ảnh, **khóa aspect ratio**:
  - Hình 2.1: ~11,33 × 14,50 cm.
  - Hình 2.2: ~9,84 × 13,50 cm.
  - Hình 2.3: ~11,55 × 11,50 cm.
  - Hình 2.4: ~9,02 × 14,50 cm.
  - Hình 2.5: ~10,43 × 10,80 cm.
  - Hình 2.6: ~11,56 × 9,00 cm.
  - Hình 2.7: ~12,13 × 10,20 cm.
  - Hình 2.8: ~10,26 × 13,50 cm.
- Đây là **adaptive sizing**, không phải kích thước cứng áp cho mọi sơ đồ sau. Rule tái sử dụng: chọn final size bằng render QA; ưu tiên ~10,5–13,5 cm rộng / 10–15 cm cao, soft cap ~15,5 cm, nhưng readability quyết định cuối cùng.
- Working artifact: `CNTT_KLCN090_THESIS-R1_Patch2D3g_AdaptiveFigureSizing_Final.docx`, SHA-256 `bcbb73be5e27edf6351dec62e66e02e8b848ada1b6dcfdb77ac21dbf050737b2`.
- Render QA: **86 trang PASS**, giảm **2 trang** so với 2D.3f mà không giảm font trong source UML.
  - Trang 1–35 pixel-identical baseline 2D.3f.
  - Trang 36–47 là vùng reflow Business UML; đã kiểm trực quan từng trang ở 100%.
  - Trang 48–86: toàn bộ **thân trang pixel-identical** với baseline 2D.3f trang 50–88 sau khi bù dịch +2 trang; System UC/đặc tả hệ thống không bị sửa semantic/layout.
- Khoảng trắng còn lại ở một số trang bảng đặc tả được chấp nhận có chủ đích vì nguyên nhân là row lớn/cantSplit; không phá bảng hoặc thu nhỏ UML thêm chỉ để ép số trang.
- **Shape-aware ObjectFlow rule của 2D.3f vẫn giữ nguyên và bắt buộc cho các sơ đồ sau có entity/ObjectFlow.**
- **System UML vẫn PARKED**; chưa redraw/chỉnh.
- **Rule persistence:** mọi visual/layout rule người dùng chốt phải được ghi vào `SCOPE.md` trước khi áp sang phần tiếp theo.


### THESIS-R1 — session handoff after Business UML / before System UML

- **Current thesis artifact:** `CNTT_KLCN090_THESIS-R1_Patch2D3g_AdaptiveFigureSizing_Final.docx`.
- SHA-256: `bcbb73be5e27edf6351dec62e66e02e8b848ada1b6dcfdb77ac21dbf050737b2`.
- Render QA: **86 trang PASS**.
- Business UML `Hình 2.1–2.8` đã qua semantic/layout/data-flow/shape-aware/adaptive-size cleanup và là baseline hiện hành.
- HUIT UML Visual & Semantic Profile v2 + Activity density + Business Entity/ObjectFlow + shape-aware anchoring + adaptive figure sizing đều đã checkpoint trong `SCOPE.md`.
- **Fork/Join rule:** chỉ dùng Fork/Join bar khi có xử lý song song thực sự; Decision/Merge dùng cho các nhánh loại trừ. Không đổi Merge sang Join chỉ để hình gọn/đẹp.
- **System UML hiện vẫn PARKED/NEXT**, chưa redraw trong Business UML patches.
- **NEXT candidate:** phục hồi context rồi audit/chốt **System Use Case Diagram tổng thể** trước; sau khi PASS mới đi từng System Use Case + Sequence. Không sửa Sequence/Class Analysis trước khi System Use Case set/actors/relations được khóa.
- Khi mở session mới, đọc theo thứ tự: `AGENTS.md` → `SCOPE.md` → issue #182 → artifact Word hiện hành khi cần layout/Word evidence → code/contracts chỉ khi cần xác minh claim hệ thống.
- Binary DOCX không được coi là lưu trong GitHub chỉ vì tên/hash đã checkpoint; khi session mới cần sửa/chèn Word, **đính kèm lại artifact hiện hành**.

### THESIS-R1 — System Use Case presentation lock v1 — 09/11/2026

**Mục tiêu:** giữ nguyên System Use Case Model 21 UC đã khóa nhưng trình bày thành nhiều view để đọc được trên A4. Không merge/xóa/invent UC chỉ vì layout.

#### Cấu trúc mục 2.2.1
Đổi cách hiểu từ “một hình tổng thể chứa mọi UC” sang **một System Use Case Model được trình bày qua 1 summary view + 4 detail views**.

1. **Hình 2.9 — Sơ đồ Use Case hệ thống tổng quan**
   - Chỉ dùng **10 UC xương sống của hành trình tiêm chủng**:
     - UC09 Quản lý hồ sơ khách hàng/người được tiêm
     - UC10 Tra cứu dịch vụ tiêm chủng
     - UC11 Đăng ký dịch vụ tiêm chủng
     - UC12 Quản lý lịch hẹn
     - UC14 Tự động nhắc lịch và liên hệ theo lịch
     - UC15 Ghi nhận khám sàng lọc và chỉ định
     - UC16 Thanh toán dịch vụ
     - UC17 Ghi nhận mũi tiêm
     - UC18 Ghi nhận theo dõi sau tiêm
     - UC19 Theo dõi hành trình tiêm chủng
   - Actor hiển thị: **Khách hàng, Lễ tân, Bác sĩ, Thu ngân, Điều dưỡng, Thời gian**.
   - Không hiển thị actor hierarchy, Dịch vụ thư điện tử, Admin, Nhân viên kho hay các UC account/admin/support ở summary; các quan hệ đó được thể hiện ở detail view.
   - Không dùng mũi tên UC→UC để kể thứ tự. Vị trí chỉ phục vụ đọc từ trái/phải và trên/xuống.

2. **Hình 2.10 — Sơ đồ Use Case nhóm tài khoản và hồ sơ**
   - UC01 Đăng ký tài khoản khách hàng
   - UC02 Đăng nhập hệ thống
   - UC03 Đổi mật khẩu
   - UC04 Đăng xuất hệ thống
   - UC05 Quản lý chức danh tài khoản nhân viên
   - UC09 Quản lý hồ sơ khách hàng/người được tiêm
   - Actor: **Người dùng, Khách hàng, Nhân viên, Lễ tân, Quản trị viên**.
   - Đây là view ưu tiên thể hiện actor generalization cần thiết cho shared authentication; không kéo các chức danh lâm sàng/kho vào chỉ để minh họa hierarchy. Các vai trò còn lại được mô tả là chuyên biệt của Nhân viên trong phần xác định actor và xuất hiện tại detail view tương ứng.

3. **Hình 2.11 — Sơ đồ Use Case nhóm dịch vụ, đăng ký, lịch hẹn và nhắc lịch**
   - UC10 Tra cứu dịch vụ tiêm chủng
   - UC11 Đăng ký dịch vụ tiêm chủng
   - UC12 Quản lý lịch hẹn
   - UC13 Gửi nhắc lịch
   - UC14 Tự động nhắc lịch và liên hệ theo lịch
   - UC19 Theo dõi hành trình tiêm chủng
   - UC20 Tra cứu lịch sử tiêm chủng
   - Actor: **Khách hàng, Lễ tân, Thời gian, Dịch vụ thư điện tử**.
   - Dịch vụ thư điện tử là secondary actor cho UC13/UC14; `Thời gian` chỉ kích hoạt UC14.

4. **Hình 2.12 — Sơ đồ Use Case nhóm khám, thanh toán, tiêm và theo dõi sau tiêm**
   - UC15 Ghi nhận khám sàng lọc và chỉ định
   - UC16 Thanh toán dịch vụ
   - UC17 Ghi nhận mũi tiêm
   - UC18 Ghi nhận theo dõi sau tiêm
   - Actor: **Khách hàng, Bác sĩ, Thu ngân, Điều dưỡng**.
   - Không thể hiện dependency nghiệp vụ giữa 4 UC bằng include/extend; precondition/gate giữ trong đặc tả.

5. **Hình 2.13 — Sơ đồ Use Case nhóm quản trị danh mục, kho và báo cáo**
   - UC06 Quản lý danh mục tiêm chủng
   - UC07 Quản lý ưu đãi
   - UC08 Quản lý lô và tồn kho vắc xin
   - UC21 Báo cáo hoạt động
   - Actor: **Quản trị viên, Nhân viên kho**.

#### Coverage/readability rules
- **4 detail views partition đủ 21 UC đúng một lần**; Hình 2.9 là summary nên được phép lặp lại 10 UC đã có ở detail views.
- Một UC lặp ở summary và detail vẫn là **cùng một model element**, không phải UC mới.
- Summary ưu tiên **orientation/readability**, detail views ưu tiên **coverage/traceability**.
- Không cố thể hiện mọi actor-generalization ở mọi view. Quan hệ hierarchy dùng ở view nơi nó giúp giảm lặp; các actor cụ thể vẫn giữ semantic role xuyên model.
- Target layout mỗi hình: khoảng **4–10 UC**, 2–6 actor, association ngắn, hạn chế crossing; monochrome, A4-readable.
- Nếu PlantUML auto-layout làm crossing/xô lệch, được phép dùng hidden links/layout helpers **không mang semantic**, nhưng không đổi association/actor/UC để phục vụ renderer.
- Mẫu HUIT mục 2.2.1 được hiểu là **Sơ đồ Use Case hệ thống** (có thể nhiều hình/view), không bắt buộc một diagram duy nhất chứa toàn model.
- Chưa đổi Word/numbering tại checkpoint này; renumber/caption chỉ làm sau khi 5 diagram semantic + render PASS.

#### Scope stop
- Không mở lại 21-UC semantic lock chỉ vì một detail view xấu.
- Nếu một view vẫn quá rối, **chia view**, không merge UC semantic đúng.
- NEXT: viết PlantUML source-first cho **Hình 2.9 summary** trước; render engine thật và QA. Chỉ sau Hình 2.9 PASS mới tiếp tục 2.10–2.13.

### THESIS-R1 — TopDev Use Case Diagram supplementary review — 09/11/2026

Nguồn bổ trợ đã đọc toàn bộ: TopDev, **“Use Case Diagram và 5 sai lầm thường gặp”**. Đây là **guideline thực hành/BA**, không phải chuẩn UML chính thức và **không override** bài giảng HUIT hoặc UML semantic profile.

#### Rule bổ sung được áp dụng
- Use Case phải thể hiện **tương tác giữa actor và hệ thống nhằm đạt một mục đích cụ thể**, không phải inventory endpoint/chức năng kỹ thuật.
- Actor có thể là **con người hoặc hệ thống bên ngoài**; chỉ đưa actor vào khi có interaction trực tiếp có evidence.
- Tên Actor ưu tiên **danh từ**; tên Use Case ưu tiên **Động từ + Danh từ**, ngắn, rõ, không dùng thuật ngữ implementation nếu stakeholder không cần biết.
- Không biến Use Case Diagram thành **phân rã chức năng**; tránh các UC “Quản lý X” quá rộng nếu không thể nói rõ end-user đạt kết quả gì. Tuy nhiên `Quản lý X` vẫn được phép khi nó thực sự gom CRUD/operations cùng một mục tiêu người dùng và đặc tả nói rõ phạm vi thao tác.
- Không trộn các UC ở **khác cấp độ requirement** trong cùng view; summary view phải nhất quán high-level, detail view phải nhất quán mức chi tiết.
- **Readability heuristic:** một diagram sạch thường nên quanh **10 UC**. Đây là heuristic, không phải UML hard limit. Khi vượt ngưỡng đọc A4, chia thành nhiều diagram/view theo actor/domain/subsystem nhưng giữ cùng một Use Case Model.
- Không tách CRUD thành Create/Read/Update/Delete riêng chỉ để đủ chức năng; chỉ tách khi mỗi thao tác là goal độc lập có giá trị với actor.
- `include` chỉ nên tách khi hành vi bắt buộc đủ lớn và/hoặc thực sự tái sử dụng; không dùng chỉ để làm sơ đồ “logic hơn”.
- `extend` biểu diễn hành vi optional/conditional; nếu dùng phải giải thích được điều kiện/extension point trong đặc tả, dù không nhất thiết ghi hết comment lên overall diagram.
- Khi vẽ, ưu tiên câu hỏi **WHAT** actor muốn đạt được; tránh rơi vào **HOW** implementation.
- Layout: association không chồng chéo; được phép lặp lại cùng actor ở hai vị trí trong **cùng view** nếu chỉ nhằm giảm crossing và có nhãn y hệt, nhưng không tạo actor semantic mới.

#### Điểm của bài TopDev KHÔNG khóa thành rule
- “Use Case cùng kích thước”, “Extend phải vẽ to hơn”, “bắt buộc ghi Use Case ID trên hình”, “nên tô màu” chỉ là style suggestion của bài viết; **không phải UML/HUIT requirement**. Thesis tiếp tục giữ adaptive sizing, monochrome, A4 readability.
- Ví dụ của bài dùng “Đăng nhập” như `include` trong một số flow; **không tự áp dụng** cho thesis. Với hệ thống này, login có thể phục vụ nhiều UC trong cùng session nên tiếp tục được xem là precondition/session concern trừ khi đặc tả cụ thể chứng minh mỗi execution luôn thực hiện UC login như một hành vi con.
- Heuristic “khoảng 10 UC” không được dùng để merge/xóa UC semantic đúng; chỉ dùng để **chia view trình bày**.

#### Hệ quả cho Hình 2.9
- Không cố nhét đủ 21 UC vào một diagram.
- Ưu tiên **1 summary view khoảng 8–10 UC thật** + các detail views theo nhóm chức năng; mọi UC vẫn thuộc cùng một System Use Case Model.
- Không tạo “UC cha” giả chỉ để gom nhóm. Việc gom nhóm dùng **diagram/view/boundary/package**, không bịa relationship.
- Chỉ sau khi khóa decomposition presentation mới render PlantUML.

### THESIS-R1 — System Use Case semantic lock v3 / 21 UC — 09/11/2026

**Evidence base:** HUIT System UC theory + exact implementation evidence on `main` HEAD `1fa336942559047a75400e3c863c08a3b294416e`. Commit từ `20a4ce2c...` → `1fa3369...` chỉ đổi Copilot docs/prompt, **không đổi implementation code**.

#### Exact System Use Case set — 21 UC
1. **Đăng ký tài khoản khách hàng**
2. **Đăng nhập hệ thống**
3. **Đổi mật khẩu**
4. **Đăng xuất hệ thống**
5. **Quản lý chức danh tài khoản nhân viên**
6. **Quản lý danh mục tiêm chủng**
7. **Quản lý ưu đãi**
8. **Quản lý lô và tồn kho vắc xin**
9. **Quản lý hồ sơ khách hàng/người được tiêm**
10. **Tra cứu dịch vụ tiêm chủng**
11. **Đăng ký dịch vụ tiêm chủng**
12. **Quản lý lịch hẹn**
13. **Gửi nhắc lịch**
14. **Tự động nhắc lịch và liên hệ theo lịch**
15. **Ghi nhận khám sàng lọc và chỉ định**
16. **Thanh toán dịch vụ**
17. **Ghi nhận mũi tiêm**
18. **Ghi nhận theo dõi sau tiêm**
19. **Theo dõi hành trình tiêm chủng**
20. **Tra cứu lịch sử tiêm chủng**
21. **Báo cáo hoạt động**

#### Actor set và generalization
- Abstract/general actors: **Người dùng**, **Nhân viên**.
- Concrete human actors: **Khách hàng, Lễ tân, Bác sĩ, Thu ngân, Điều dưỡng, Nhân viên kho, Quản trị viên**.
- Special/external actors: **Thời gian**, **Dịch vụ thư điện tử**.
- Generalization:
  - Khách hàng → Người dùng.
  - Nhân viên → Người dùng.
  - Lễ tân/Bác sĩ/Thu ngân/Điều dưỡng/Nhân viên kho/Quản trị viên → Nhân viên.
- Business/System separation vẫn giữ: tên vai trò có thể giống nhau ngoài đời nhưng phần tử System Actor dùng **System Actor shape**, không dùng Business Worker/Business Actor shape.

#### Actor–Use Case associations
- **Người dùng** — UC02, UC03, UC04.
- **Khách hàng** — UC01, UC09, UC10, UC11, UC12, UC16, UC19, UC20; khách hàng là recipient của UC13/UC14 trong đặc tả.
- **Lễ tân** — UC09, UC11, UC12, UC13.
- **Bác sĩ** — UC15.
- **Thu ngân** — UC16.
- **Điều dưỡng** — UC17, UC18.
- **Nhân viên kho** — UC08.
- **Quản trị viên** — UC05, UC06, UC07, UC21.
- **Thời gian** — UC14.
- **Dịch vụ thư điện tử** — UC13, UC14 với vai trò secondary/external delivery actor; không nêu provider production cụ thể.

#### Quan hệ Use Case tổng thể
- **0 `<<include>>`, 0 `<<extend>>`, 0 generalization giữa các UC** ở Hình 2.9.
- Dependency/precondition/thứ tự như đăng ký → lịch hẹn → khám → thanh toán → tiêm → theo dõi **không** được chuyển thành include/extend.
- Đăng nhập là precondition của các UC protected, **không include Đăng nhập** vì một phiên đăng nhập có thể phục vụ nhiều UC.
- UC13 manual reminder và UC14 automatic reminder tách riêng vì khác trigger/actor/session: Lễ tân chủ động gửi vs **Thời gian** kích hoạt scheduler.
- UC19 hành trình và UC20 lịch sử tách riêng vì implementation có endpoint/page/result semantics riêng.

#### Evidence map — implementation hiện hành
- UC01–UC04: `IdentityEndpoints.cs`; register/login/change-password/logout. Email verification/password reset có implementation nhưng **không promote thành top-level Hình 2.9**; registration UC kết thúc ở trạng thái đăng ký nhất quán/pending verification, các flow hỗ trợ giữ trong đặc tả identity khi cần.
- UC05: `EmployeeAccessEndpoints.cs` + contract A12: chỉ xem tài khoản nhân viên có sẵn và đổi **chức danh**; không claim permission-matrix/provision/reset/lock HTTP.
- UC06: `CatalogEndpoints.cs`: public read + Admin CRUD danh mục vaccine/package/disease/technology.
- UC07: `PromotionEndpoints.cs`: quản lý ưu đãi; public valid promotions chỉ là read/supporting data cho customer flow.
- UC08: `InventoryEndpoints.cs` + `InventoryReceiptEndpoints.cs`: đọc lot/movement/history + tiếp nhận lô mới; **không claim arbitrary adjustment/transfer/full ERP**.
- UC09: `ProfileEndpoints.cs`: Customer/Lễ tân read/create/update theo ownership/capability.
- UC10: `CatalogEndpoints.cs` public vaccine/package list/detail. RF-02C #186 đang ACTIVE chỉ mở rộng/hoàn thiện Web UI; không claim UI RF-02C đã DONE.
- UC11: `RegistrationEndpoints.cs`: Customer hoặc nhân viên có Registrations capability.
- UC12: `AppointmentEndpoints.cs`: Reception create/reschedule/cancel; Customer đọc lịch thuộc ownership.
- UC13: `ReminderEmailEndpoints.cs` + legacy `ReminderEndpoints.cs`: Lễ tân gửi/ghi nhắc thủ công theo capability `GuiNhacLich`.
- UC14: `ReminderScheduler.cs` + contract RF-05G: scheduler T-48/T-24/D+3/D+7, actor `Thời gian`; delivery qua reminder dispatcher/email path.
- UC15: `ClinicalEndpoints.cs`: Bác sĩ ghi assessment/indication; hệ thống không tự quyết eligibility.
- UC16: `PaymentEndpoints.cs`: Customer/Cashier payment state/recording; **không claim production payment gateway**.
- UC17: `VaccinationEndpoints.cs`: Điều dưỡng ghi mũi với clinical/payment/inventory gates.
- UC18: `AftercareEndpoints.cs`: Điều dưỡng ghi follow-up sau mũi; gate 30 phút, không claim emergency automation.
- UC19: `VaccinationJourneyEndpoints.cs`: Customer đọc registration/payment/appointment/progress/shots theo ownership.
- UC20: `VaccinationHistoryEndpoints.cs`: Customer đọc lịch sử mũi đã thực hiện; endpoint/page riêng với journey.
- UC21: `ReportEndpoints.cs`: Admin đọc revenue/dose/registration-status reports.

#### Scope stop
- Hình 2.9 cũ và preview v1/v2 **STALE**.
- Không thêm UC chỉ vì endpoint/menu/class tồn tại; top-level set này là thesis-level decomposition theo HUIT, không phải endpoint inventory.
- Không tự thêm Payment Gateway/SMS/ZNS/push/nhà cung cấp/Người được tiêm làm System Actor khi chưa có interaction trực tiếp được evidence hỗ trợ.
- Không sửa bảng 2.2.2/Word/Sequence trước khi **Hình 2.9 semantic + PlantUML render PASS**.
- NEXT: thiết kế layout source-first cho 21 UC → PlantUML engine render → visual QA A4 → checkpoint Hình 2.9.

### THESIS-R1 — implementation-evidence gate 09/11/2026

- User requirement: **mọi mô hình hệ thống phải bám chức năng thực sự hiện có trên GitHub; không tự bịa hoặc suy luận capability**.
- Exact `main` HEAD tại checkpoint: `20a4ce2c686e87d09a8978d9bc9e41819f437f88`.
- So với main HEAD đã audit trước `d59868c0459e0361e952785df01260a1060fb5a8`, có **2 commit mới nhưng chỉ thay tài liệu/routing** (`.github/copilot-instructions.md`, `AGENTS.md`, `SCOPE.md`); không có source-code implementation change.
- `RF-02C #186` hiện **ACTIVE** trên main: Active-only public catalog + search/filter/detail/package detail là phạm vi đang triển khai, **không được ghi như capability DONE** cho tới khi có code/test/merge evidence trên main.
- Bộ **19 System UC** vừa đề xuất trong chat là **DRAFT, chưa khóa**. Phải re-audit từng UC/actor theo evidence gate này trước khi checkpoint semantic Hình 2.9.
- Quy tắc khi thiếu evidence: **thu hẹp claim → đánh dấu chưa xác minh → hoặc bỏ khỏi UML/prose**; không suy diễn từ nghiệp vụ mong muốn, đề cương, tên module, UI/menu hay issue title.
- Bài giảng HUIT quyết định **phương pháp mô hình hóa**; repo `main` quyết định **capability thực tế đã có**. Hai nguồn không thay thế cho nhau.

### THESIS-R1 — HUIT course-source re-audit 09/11/2026

- Người dùng đã cung cấp lại bộ bài giảng PTTKHTTT; coordinator đã đọc trực tiếp các phần shape/notation liên quan thay vì chỉ dựa vào chat cũ.
- **Profile v2** ở trên là bản chưng cất bền vững dùng cho mọi session sau. `AGENTS.md` bắt buộc recovery THESIS/UML phải đọc profile này.
- **Business UML Hình 2.1–2.8 vẫn là baseline hiện hành và chưa tự sửa lại.** Tuy nhiên mẫu báo cáo Khoa yêu cầu mỗi Business Use Case có **Sơ đồ hoạt động + Sơ đồ đối tượng nghiệp vụ**, trong khi artifact hiện hành chỉ có Hình 2.1 tổng thể + Hình 2.2–2.8 Activity. Đây là **PARKED compliance gap**; không tự mở lại Business UML nếu người dùng chưa reprioritize.
- **System Use Case semantic gate được mở lại trước khi vẽ Hình 2.9:** bộ 13 UC của Patch 2D.2 vẫn là text baseline, nhưng phải audit thêm theo rule HUIT `single-session / interactive / complete` và special actor `Thời gian`. Ít nhất UC “Xác thực và phân quyền truy cập” và “Quản lý lịch hẹn và nhắc lịch” cần re-check vì có nguy cơ gộp nhiều mục tiêu/phiên/actor.
- Preview Hình 2.9 dựng thủ công trong chat **không phải PlantUML engine render PASS**. Source `.puml` phải được render bằng PlantUML engine/CI rồi mới visual PASS.
- **Không sửa Word tại checkpoint này.** Sau khi semantic System UC PASS mới render Hình 2.9; compliance gap Business Object vẫn PARKED trừ khi người dùng đổi ưu tiên.
- Recovery bắt buộc: `AGENTS.md → SCOPE.md (Current State + HUIT UML Visual & Semantic Profile v2 + checkpoint này) → issue #182 → artifact Word hiện hành khi cần → code/contracts/tests chỉ khi cần xác minh claim`.

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
