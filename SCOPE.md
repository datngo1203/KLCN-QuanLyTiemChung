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
- **THESIS-R1 #182** — đồng bộ báo cáo khóa luận với trạng thái repo hiện tại. **Patch 2C DONE:** đã viết lại Lời cảm ơn, Mở đầu và Chương 1 theo writing guardrail, dependency spine và nguồn công khai đã xác minh; Mở đầu bổ sung phương pháp thực hiện + cấu trúc báo cáo; Chương 1 phân biệt rõ khảo sát thực tế với quy tắc đồ án, sắp 7 quy trình theo dependency, cập nhật yêu cầu chức năng/phi chức năng và thêm nguồn VNVC [13]–[16]. Working artifact `CNTT_KLCN090_THESIS-R1_Patch2C.docx`, SHA-256 `6608ca0e48b231629e7afa5f90874d2cb0f6e53ebd5b34b7a0ff33d75b0fbefe`. Render QA 83 trang PASS, DOCX ZIP integrity PASS; format hard-lock PASS cho styles/body/table/margins. **ACTIVE tiếp theo:** Patch 2D viết lại Chương 2 theo dependency spine; chưa redraw UML cho tới khi đặc tả text PASS.

### THESIS-R1 — khóa quy tắc văn phong học thuật, chống đạo văn và sử dụng AI

Nguồn tinh chế: tài liệu người dùng cung cấp `Hướng dẫn giảm đạo văn và AI trong văn bảng học thuật.pdf` (08/11/2026). Mục tiêu là **tính học thuật, tính nguyên gốc và khả năng truy vết nguồn**, không tối ưu để "qua" AI detector hoặc chạy theo một ngưỡng Similarity/AI Detection cụ thể.

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
2. Đăng ký thông tin khách hàng/người được tiêm.
3. Đăng ký dịch vụ tiêm chủng.
4. Khám sàng lọc và chỉ định.
5. Thanh toán dịch vụ.
6. Thực hiện tiêm chủng và theo dõi sau tiêm.
7. Quản lý lịch tiêm và nhắc lịch.

### 6 actor nghiệp vụ
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
