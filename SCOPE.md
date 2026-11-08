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
- **THESIS-R1 #182** — đồng bộ báo cáo khóa luận với trạng thái repo hiện tại. **Patch 1 DONE:** front matter + page numbering, TOC field, Mở đầu/Chương 1, skeleton 5 chương và danh mục hiện hành đã được chỉnh trên working artifact `CNTT_KLCN090_THESIS-R1_Patch1.docx` (SHA-256 `fca67915ee2ff96d26fbed30964358d2f0dc46a489174a81586495e8e39c44a2`). **ACTIVE tiếp theo:** Patch 2 rà Chương 2 Phân tích/Use Case; không sửa business feature trên branch báo cáo.

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
