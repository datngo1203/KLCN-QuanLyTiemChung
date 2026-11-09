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


### THESIS-R1 — HANDOFF: restart System Use Case presentation — 09/11/2026

**User intent:** mở chat mới để **làm lại phần sơ đồ Use Case hệ thống, ưu tiên chất lượng bố cục và kiểm từng cụm**. Đây là một **visual/design reset**, không phải xác nhận thay đổi 21-UC semantic model. Không mở implementation feature.

#### Trạng thái được phép kế thừa
- Branch THESIS: `docs/thesis-r1-current-state`; active issue `#182`; source-of-truth là `AGENTS.md → SCOPE.md → issue #182 → HUIT Chương 5 đúng trang → main exact HEAD code/contract chỉ khi claim implementation cần verify`.
- **21 top-level System UC** với tên chính thức theo **reconciliation v4** (không lấy danh sách tên cũ tại semantic lock v3 nếu mâu thuẫn). UC05 và UC14 đã đổi tên. **0 include, 0 extend, 0 generalization giữa các UC** theo evidence/semantic đã audit.
- Actor model: `Người dùng` và `Nhân viên` tổng quát; `Khách hàng` cùng các vai trò nhân viên; `Thời gian` cho UC14; `Dịch vụ thư điện tử` hỗ trợ UC13/14. Toàn model có **11 actor ký hiệu** nếu hiện cả 2 actor tổng quát; one-view prototypes chỉ hiển thị **10** vì không vẽ `Nhân viên` general actor. Không nhầm presentation omission thành việc xóa actor/generalization.
- **27 direct actor–UC associations** theo semantic lock v3. Generalization có **8 quan hệ** (`Khách hàng→Người dùng`, `Nhân viên→Người dùng`, 6 vai trò staff→`Nhân viên`), có thể thể hiện tại detail view nếu presentation policy giải thích rõ.
- Dạng nộp theo HUIT: `2.2.1` System UC diagram/view(s), `2.2.2` đặc tả từng System UC; Sequence ở `3.2` theo UC. Nếu giữ 21 UC thì cần chuẩn bị **21 bảng và 21 Sequence** theo cách đọc an toàn của mẫu, trừ khi GVHD cho phép khác; **artifact count không phải tiêu chí gộp/demote UC**.
- Hình 2.9 **10-UC summary cũ** đã từng final-render PASS về semantic/visual tại commit/workflow checkpoint (xem mục Hình 2.9 FINAL PASS); **vẫn tồn tại như phương án dự phòng**, không được xem là mô hình toàn 21 UC. Các Hình 2.10–2.13 mới ở planning/prototype, chưa final.

#### Tình trạng thực tế của full 21-UC one-view
- Người dùng gửi PDF tham khảo **`usecasethamkhao.pdf` (8 trang)** cùng ảnh một Use Case Diagram đông UC; điểm cần học là actor ở biên, UC rải thành cụm gần actor, fan-out hợp lý, có thể có một vài crossing nhẹ **nếu vẫn xác định được từng relation**.
- **Không copy semantic trong mẫu tham khảo**: các `include` bắt buộc `Đăng nhập` hoặc `extend` hàng loạt các bước CRUD không được tự áp dụng. Chương 5 HUIT yêu cầu single-session/interactivity/complete-result, grouping CRUD khi cohesive; include/extend theo đúng WHEN/WHY.
- Các nguyên mẫu thử: one-view v1–v2 có actor hierarchy quá lớn/dây dài; reference-inspired v3–v6 bố cục actor-centric nhưng còn khoảng trống và crossing; v7–v9 dùng orthogonal lanes làm hình giống mạch điện; v10 chuyển sang fan-out trực tiếp nhưng nhiều dây xuyên ellipse/label; **Bước A v11** thử riêng UC09/11/12/13/16 và actor Khách hàng/Lễ tân, có cải thiện cục bộ nhưng vẫn **visual FAIL**, có giao cắt và đường dài. `Hinh_System_Use_Case_21UC_BuocA_Prototype_v11.png` và `Hinh_System_Use_Case_21UC_BuocA_v11_zoom.png` là **local visual experiments**, không nằm trên repo/Word. Nếu không có file ở chat mới thì không giả định có, cũng **không cần khôi phục**; có thể dựng lại từ semantic matrix.
- **KHÔNG bản full 21-UC v1–v11 nào được user APPROVE/visual PASS/canonical/Word-inserted.** Hình v11 không phải baseline geometry mới.
- User phê bình đúng các lỗi lặp lại: khoảng trống trung tâm/trên còn lớn, cluster trái `Khách hàng/Lễ tân→UC09/11/12/13/16` quá rối; dây dài, chồng/cắt vào ellipse hoặc label, góc vuông dư thừa; cluster phải `UC17/18` với `UC08` và actor tương ứng chèn nhau; cụm đáy `UC13/14/Email/Thời gian` chưa sạch; khi thu A4 landscape khó đọc. **CI-render thành công không chứng minh visual PASS.**
- **Sai lầm đã rút:** vẽ liên tiếp các phiên bản mà chưa đo intersection/label intrusion/A4 readability, lấp khoảng trống bằng UC bất kể kết nối, chỉ chăm “ít crossing” nhưng bỏ qua chiều dài/độ rõ semantic, chuyển engine/layout style theo cảm tính. Không lặp lại.
- **Tối ưu bố cục không được thay đổi actor–UC association, actor inheritance, UC names hoặc thêm quan hệ UC→UC giả.** Tuyệt đối không duplicate actor trong cùng view chỉ để chữa routing.

#### Cách bắt đầu chat mới — làm từng bước, không vội vẽ final
1. Recovery đúng nguồn + verify exact THESIS HEAD/main HEAD; đọc spot-check HUIT Chương 5 **tr. 3–13, 19–24, 27–28** (và mẫu báo cáo 2.2/3.2 nếu cần). Không đọc lại tất cả lịch sử.
2. **Khóa mục tiêu của diagram**: so sánh **one-view đủ 21 UC** với phương án **1 summary 10 UC + 4 detail views**. Bản full một view là **EXPERIMENT**, chưa thay Hình 2.9/Word. Cần test A4 landscape để quyết định bằng visual evidence, không lấy hình tham khảo số UC lớn làm bằng chứng tự động rằng hình mình sẽ đọc được.
3. Lập **ma trận actor–UC/anchor/routing** đúng 27 associations + generalization scope; thiết kế **actor placement + phân cụm** trước, rồi từng đường; ưu tiên giao tiếp cùng actor đặt gần nhau, tránh đường đi qua ellipse/title/nhãn; nếu crossing bắt buộc, làm ở vùng trống với góc rõ và không trùng lặp khó phân biệt.
4. Layout quality gate **đo được**: missing/extra associations=0; missing/duplicate actor=0 theo presentation contract; **connector xuyên ellipse/label/title=0**; ghi nhận crossing count, maximum wire length, fan-out clarity; kiểm hình khi đặt vào trang **A4 ngang** ở cỡ in thực, không chỉ 4300 px tại zoom màn hình.
5. **Làm theo cụm nhỏ có checkpoint**: (A) nhóm Khách hàng/Lễ tân, (B) clinical/payment/inventory phía phải, (C) identity/admin/scheduler + balance tổng thể. Không “fix” một cụm khiến cụm khác hỏng mà không re-audit.
6. Chỉ khi prototype **human-approved** mới viết/chỉnh PlantUML source canonical + deterministic layout (nếu cần) + CI render artifact + visual QA. Giữ semantic source-first và fail-fast khi model mapping đổi. Không dùng mockup Python như final canonical.
7. **Scope stop:** đạt visual+semantic PASS cho một presentation choice, ghi chính xác branch/SHA/workflow/PNG digest, rồi mới chuyển Word/Hình 2.10/đặc tả.

#### Tài liệu tham khảo quan trọng
- HUIT Chương 5: System actor, System UC quality gate, CRUD grouping, special actor Thời gian, include/extend/generalization.
- `usecasethamkhao.pdf`: **tham khảo bố cục, không xem là UML authority**.
- `docs/thesis/uml/Hinh_2_9_System_Use_Case_Tong_Quan.puml` + `postprocess_system_usecase.py` chỉ dành cho **10-UC summary đã render**, chưa có canonical full 21-UC.

**Handoff stop:** user chưa chốt chọn 21-UC overall làm final; chat mới **không tự chuyển prototype v11 thành ACTIVE canonical**. Scope THESIS-R1 vẫn ACTIVE; task khởi động lại là System UC **presentation/visual audit**.

### THESIS-R1 — System Use Case full-21 portrait layout contract v1 — LOCKED 09/11/2026

**User decision:** chốt hướng trình bày thử nghiệm/full-model hiện tại theo **A4 dọc (portrait), actor-centric fan-out**, học cách tận dụng không gian từ hình tham khảo người dùng cung cấp nhưng **không sao chép semantic** của hình mẫu. Đây là **layout contract bắt buộc cho prototype tiếp theo**. Không quay lại grid/cột đối xứng cứng, orthogonal bus/mạch điện hoặc prototype v1–v11 cũ trừ khi user chủ động mở lại.

#### 1. Geometry tổng thể
- Trang mục tiêu: **A4 portrait** theo lề thesis hiện hành; tận dụng chiều cao để rải 21 ellipse.
- Actor đặt quanh **perimeter/biên ngoài** theo vùng tương tác, không ép thành hai cột đối xứng.
- 21 Use Case được rải thành khoảng **2–3 cột so le** trong system boundary; **không ép hàng/cột thẳng tuyệt đối** nếu làm dây chập hoặc dài.
- Layout ưu tiên **độ gần actor và routing clarity** hơn tính đối xứng hình học.
- UC chỉ có **1 actor trực tiếp** đặt gần actor đó.
- UC có **2 actor trực tiếp** đặt tại vùng trung gian/corridor giữa hai actor liên quan.
- Actor degree cao được dành nhiều không gian fan-out hơn actor degree 1–2.

#### 2. Sector/corridor dự kiến
- **Khách hàng:** vùng trái / giữa-trái; UC riêng như UC01, UC10, UC19, UC20 đặt gần phía Khách hàng.
- **Khách hàng + Lễ tân:** UC09, UC11, UC12 nằm trong shared corridor; không ép Khách hàng và Lễ tân cùng một phía nếu làm sáu association chụm vào một mép.
- **Lễ tân:** UC13 nằm gần Lễ tân hơn các Customer-only UC.
- **Quản trị viên:** UC05, UC06, UC07, UC21 tạo sector admin riêng ở phía phải/trên-phải.
- **Bác sĩ / Thu ngân / Điều dưỡng:** UC15 / UC16 / UC17–UC18 đặt gần actor tương ứng; UC16 là bridge **Khách hàng ↔ Thu ngân**, không nhét vào cluster Lễ tân.
- **Nhân viên kho:** UC08 đặt riêng gần Nhân viên kho; không chen vào cụm Điều dưỡng/clinical chỉ để lấp khoảng trống.
- **Reminder scheduler:** UC13/UC14 + Dịch vụ thư điện tử + Thời gian đặt ở vùng thấp phù hợp để dây ngắn, rõ.
- **Actor generalization:** đi ở **gutter/mép ngoài**; direct actor–UC association đi **vào trong boundary**. Không để hai loại đường tranh cùng corridor.

#### 3. Fan-out / connector contract — HARD GATE
- **Không có association trùng/chập lên nhau trên một đoạn đáng kể.**
- `overlapped association segments = 0`.
- Các association từ cùng actor phải **tách sớm gần actor**, không dùng một đường chung rồi tách muộn như bus.
- Nếu hai đường chạy gần nhau, phải có khoảng cách thị giác đủ để truy vết độc lập.
- `connector-through-ellipse = 0`.
- `connector-through-label/text = 0`.
- `connector-through-title/safe-zone = 0`.
- `ambiguous crossings = 0`.
- Crossing đơn lẻ chỉ được chấp nhận khi xảy ra ở **vùng trắng**, góc cắt rõ, không làm nhầm endpoint; crossing count phải được ghi khi QA.
- Association phải chạm rõ actor và ellipse; không để khoảng hở renderer làm người đọc tưởng disconnected.
- Không dùng đường cong lớn hoặc vòng tránh dài chỉ để đạt crossing=0.
- Không duplicate cùng một semantic actor trong cùng view để chữa routing.

#### 4. Semantic invariants không được đổi để cứu layout
- Giữ đúng **21 top-level System UC** theo reconciliation v4.
- Giữ đúng **27 direct actor–UC associations**.
- Giữ actor model/generalization đã khóa; presentation có thể chọn scope hiển thị hierarchy nhưng không được giả xóa semantic.
- **0 `include`, 0 `extend`, 0 UC→UC generalization** trừ khi semantic được re-audit riêng và user chốt.
- Không merge/xóa/đổi tên UC, đổi actor association hoặc invent UC cha chỉ vì hình khó vẽ.
- Reading order/position không tạo dependency hoặc workflow relation.

#### 5. Quy trình prototype tiếp theo
1. Dựng **actor placement + 21 ellipse placement trước, chưa nối dây**.
2. Audit mật độ, khoảng trắng, sector và corridor.
3. Nối association theo actor degree cao trước: **Khách hàng → Lễ tân → Quản trị viên → Điều dưỡng / Dịch vụ thư điện tử → actor degree 1**.
4. Sau mỗi nhóm, re-audit toàn hình; không fix cục bộ rồi bỏ qua regression ở cụm khác.
5. Kiểm bản thu ở **kích thước in A4 portrait thực tế**, không chỉ zoom màn hình.
6. Chỉ sau khi prototype human-approved mới chuyển sang PlantUML canonical/deterministic layout/CI/Word.

#### 6. Status của quyết định
- **LOCKED:** portrait + actor-centric fan-out + sector/corridor + no-overlap connector contract là hướng phải dùng ở bước tiếp theo.
- **KHÔNG phải final visual PASS:** full-21 chỉ trở thành hình cuối khi đủ semantic + visual + A4 readability gate.
- Summary + detail vẫn giữ như **fallback đã biết**, nhưng **không tự quay lại** trong session mới nếu user chưa yêu cầu hoặc full-21 portrait chưa được chứng minh FAIL bằng QA.
- Prototype v1–v11 trước đây vẫn **STALE**; không dùng làm geometry baseline.

### DONE gần nhất
- **REGIMEN-R1 #180 / PR #181** — Catalog hỗ trợ calendar interval theo mũi neo với `Days|Weeks|Months|Years`, giữ legacy day intervals; Windows A04 quản trị structured regimen; Customer Web hiển thị timeline + provenance tham khảo. Native A04 full-shell smoke PASS sau fix hiển thị selector; CI #387 PASS đủ backend/windows/web. Squash main: `04b932e8e4df6ec865ea90f19ad2ea731c1d77ac`.
- **RF-05G #170 / PR #179** — tự động nhắc trước lịch hẹn 48/24 giờ và liên hệ sau lịch hẹn 3/7 ngày trên appointment hiện hành; DB idempotency + retry/revalidation + A09 history/wording người dùng. CI #382 PASS; native WPF keyboard smoke PASS ở HEAD trước wording-only patch. Squash main: `19f8218da0e6a505bf11ed3ad5daf8b087abe72b`.
- **RF-05F.1 #175 / PR #178** — Customer Web lưu ngày+giờ mong muốn với provenance `CoGioDuKien`; A09 dùng preference làm gợi ý nhưng `LichHenTiem` xác nhận độc lập; confirm/reschedule/cancel tự enqueue lifecycle Email. CI #377 và Web/WPF runtime gates PASS. Squash main: `90873bbf42853ad9f00810d600ae96da9705e3bb`.
- **RF-05E #166 / PR #172** — email reminder thật, outbox/dispatcher, Windows A09; real Development SMTP acceptance đã PASS.
- **RF-05F #167 / PR #174** — reschedule/cancel cùng `LichHenTiem`, RowVersion, clinical lock, reminder reconciliation, Customer Web `DaHuy`, outcome-unknown reconcile. Squash main: `bfd6ac0be813e549d15ff799ba7d9c972547c0c1`.

Các nền tảng/core trước đó đã tích hợp: S0–S7 theo phạm vi đã nghiệm thu, AUTHZ-R1, AUTH-R2, DATA-R1, RF-05B/C/D, S6 integration, X1 kho đọc, X2.1 nhập lô mới, X2.2.1 lịch sử tiếp nhận. Bằng chứng chi tiết giữ trong issue/PR tương ứng.

### ACTIVE
- **THESIS-R1 #182** — báo cáo/UML, branch `docs/thesis-r1-current-state`. **System Use Case visual/re-presentation REOPENED theo yêu cầu ngày 09/11/2026**: làm lại cách trình bày từ đầu, **không lấy các prototype 21-UC v1–v11 làm bản chốt**.
- **Semantic baseline:** 21 System UC, actor/association/evidence map theo checkpoint reconciliation v4 bên dưới (UC05 = “Thay đổi chức danh tài khoản nhân viên”; UC14 = “Tự động gửi nhắc lịch”). Baseline **không bị hủy** chỉ vì vẽ khó; chỉ re-audit khi có evidence/lý do mô hình hóa cụ thể.
- **Presentation decision OPEN:** đã có Hình 2.9 summary 10-UC semantic+visual PASS trước đây, nhưng sau đó thử hướng **full 21 UC trên một view**; chưa có full-21 diagram PASS hoặc quyết định thay Word. Cần so sánh readability với phương án 1 summary + 4 detail views rồi mới chốt.
- **Current scope stop:** checkpoint handoff mới ngay dưới đây; **không chỉnh Word, không vẽ đặc tả/Sequence, không chuyển Hình 2.10 thành final** trước khi chốt System UC presentation và visual gate. Latest working report/artifact vẫn theo checkpoint DOCX riêng, không suy rằng UML mới đã chèn.
- **Main exact implementation evidence HEAD tại handoff:** `1fa336942559047a75400e3c863c08a3b294416e`. Chat mới phải fetch lại HEAD, không dùng SHA này như trạng thái luôn đúng.

### THESIS-R1 — Theory Source Registry & precedence v1 — 09/11/2026

**Mục đích:** đây là bản đồ nguồn bền vững cho toàn bộ THESIS-R1. Các file gốc có thể nằm ở Project/Library/chat attachment và **không cần commit binary vào repo**; `SCOPE.md` chỉ lưu identity, vai trò, thứ tự ưu tiên và route đọc để session sau không phải nhớ từ chat cũ hoặc tải lại toàn bộ tài liệu.

#### 1. Thứ tự ưu tiên nguồn
1. **Đề cương khóa luận + mẫu báo cáo chính thức của Khoa** quyết định phạm vi học thuật, deliverable bắt buộc, bố cục và thể thức báo cáo. Góp ý GVHD đã được checkpoint rõ trong `SCOPE.md` có thể override cách đặt/trình bày cục bộ của mẫu, nhưng không tự thay đổi ngữ nghĩa UML/HUIT.
2. **Bộ bài giảng PTTKHTTT HUIT** quyết định phương pháp phân tích/thiết kế, thuật ngữ và semantic/notation UML dùng trong luận văn.
3. **Exact implementation evidence trên `main`** quyết định hệ thống của đồ án **thực sự đã có capability gì**. Bài giảng quyết định cách mô hình hóa, không phải bằng chứng implementation.
4. **Nguồn thực tế công khai đáng tin cậy** (VNVC/Bộ Y tế/WHO/nhà sản xuất phù hợp) chỉ dùng để khảo sát nghiệp vụ/y tế thực tế khi cần; phải phân biệt rõ với rule/capability của đồ án.
5. **Tài liệu hướng dẫn giảm đạo văn/AI** chỉ quyết định cách viết, paraphrase và citation discipline; **không phải nguồn fact nghiệp vụ/kỹ thuật** và không tạo acceptance theo AI detector/Similarity.
6. **Use Case/reference diagram bên ngoài** chỉ dùng học bố cục khi đã ghi rõ là supplementary; không override HUIT/UML semantic hoặc evidence từ repo.

#### 2. Registry tài liệu đã người dùng cung cấp

| Nhóm | File / identity | Vai trò canonical trong THESIS-R1 |
|---|---|---|
| Yêu cầu đề tài | `Đề cương khóa luận.pdf` — SHA-256 `9579eed47a86e9261d4d93e58f206e0b122d6141f6500d7d04e3f71adb5fd591` | Khóa mục tiêu, phạm vi, rubric và các artifact bắt buộc của khóa luận; dùng để kiểm report có bao phủ đúng yêu cầu đề tài hay không. |
| Mẫu Khoa | `Mau bao cao_Do an_Khoa luan_Huong ung dung12092025.docx` — SHA-256 `c6abb6f6dd9f8504a1a25f0f8788ce59eb534edbae2184842d66dfba948576d6` | Khóa bố cục chương/mục, yêu cầu từng phần, format/caption/TOC và vị trí mặc định của các loại sơ đồ. |
| Writing | `Hướng dẫn giảm đạo văn và AI trong văn bảng học thuật.pdf` — SHA-256 `adf9fb5c1ac974b4ea7b9327620af40de74e23231dc49b5e349697b81b920b6a` | Nguồn tinh chế writing guardrail: hiểu rồi viết lại, đổi cấu trúc thật, researcher voice chỉ khi có evidence, chống overclaim, giữ citation. Không tối ưu né detector. |
| PTTKHTTT nền tảng | `Chuong 1_PTTKHTTT_Tong quan ve HTTT.pdf` — SHA-256 `c21a87b80f0f8e18e486a6f3c33b60bea5da492c2db51a95423956fbebc6e7c1` | Khái niệm hệ thống/HTTT, môi trường, tổ chức nghiệp vụ, dữ liệu/xử lý; dùng khi cần nền khái niệm cho khảo sát và phạm vi hệ thống. |
| PTTKHTTT yêu cầu | `Chuong 2_PTTKHTTT_Xac dinh yeu cau.pdf` — SHA-256 `6c3da354590cf2d8c1de4b3e1887beeee8e40323af1bf0dd02f394c3f49c16e5` | File-name ghi “Chuong 2” nhưng slide mở đầu ghi **Chương 3 — Xác định yêu cầu**. Dùng cho requirement, functional/non-functional requirement, mục tiêu/nội dung/đối tượng khảo sát và kỹ thuật thu thập yêu cầu. Không tự sửa tên file; route theo **nội dung**. |
| PTTKHTTT nghiệp vụ | `Chuong 4_PTTKHTTT_Mo hinh hoa nghiep vu.pdf` — SHA-256 `05e7115b8dc0e486f5b4f866ef737f818d7db31931f80ba4f16eca9736867875` | Authority HUIT cho Business Modeling: Business Use Case, Business Actor/Worker/Entity, Business Object, Activity, Swimlane, Control/Object Flow và truy vết nghiệp vụ. |
| PTTKHTTT chức năng | `Chuong 5_PTTKHTTT_Mo hinh hoa chuc nang.pdf` và `Chuong 5_PTTKHTTT_Mo hinh hoa chuc nang_GoiGV.pdf` — **cùng SHA-256** `fe26221f9057332144da9895487eb3e51f28dd5c77e6e1e708eac8adf7cb6a57` | Authority HUIT cho System Modeling: system boundary/actor/use case, single-session/interactivity/complete-result, CRUD grouping, special actor `Thời gian`, include/extend/generalization và format đặc tả System UC. Hai file là duplicate byte-for-byte; chỉ cần đọc một bản. |
| PTTKHTTT cấu trúc | `Chuong 6_PTTKHTTT_Mo hinh hoa cau truc(2).pdf`, `Chuong 6_PTTKHTTT_Mo hinh hoa cau truc.pdf`, `Chuong 6_PTTKHTTT_Mo hinh hoa cau truc_2.pdf` — **cùng SHA-256** `b813c33adef769d5bb35d90bd3a62d2fed44c2bd83b4a983e8ef981e538093cd` | Authority HUIT cho Class Analysis/structural modeling: class, association, multiplicity, association class, aggregation, composition, generalization và cách phát hiện/tinh chế lớp mức phân tích. Ba file là duplicate byte-for-byte; chỉ cần đọc một bản. |
| PTTKHTTT thiết kế lớp | `Chuong 7_PTTKHTTT_Thiet ke lop du lieu.pdf` — SHA-256 `7e3ab6fad77d817617ef74b3fb8406a27a68a28cdab8e0e9d8fc9db2c3fd6a6b` | Authority HUIT cho Class Design: tinh chế attribute/association/method/visibility, coupling/cohesion và chuyển từ phân tích sang thiết kế. |
| PTTKHTTT thực hành | `Bai giang TH PTTK HT_14DHTH.pdf` (Project/Library copy đã được re-audit trong THESIS-R1) | Nguồn **thực hành bổ trợ** cho Rational Rose, Sequence/Collaboration và ví dụ dựng diagram. Dùng để học ký hiệu/thao tác/bố cục; nếu ví dụ công cụ xung đột semantic HUIT/UML profile thì profile/semantic chuẩn được ưu tiên. |
| Visual reference | `usecasethamkhao.pdf` + dense sample image người dùng đã cung cấp ở phiên System UC trước | **Visual/layout reference only**: học actor placement, clustering, fan-out và cách tận dụng không gian. Không copy `include/extend` hoặc decomposition nếu không đạt HUIT semantic gate. |

#### 3. Route đọc theo phần khóa luận — progressive disclosure
- **Mở đầu / Chương 1:** Đề cương → Mẫu Khoa → Chương 1 HTTT → file Xác định yêu cầu → nguồn khảo sát thực tế đúng claim. Chỉ đọc Chương 4 nếu cần phân biệt Business role/process.
- **Chương 2.1 Business Modeling:** Chương 4 là authority; spot-check đúng trang/khái niệm theo HUIT profile v2. Không cần đọc Chương 5–7 nếu task không giao thoa.
- **Chương 2.2 System Modeling:** Chương 5 là authority; dùng practical lecture/reference diagram chỉ sau khi semantic đã khóa.
- **Chương 2.3 Class Analysis:** Chương 6 là authority; truy vết từ Business Entity/System UC/Sequence khi cần.
- **Chương 3 / Class Design:** Chương 6 + Chương 7; practical lecture dùng cho Sequence notation khi cần.
- **Mọi prose:** áp writing guardrail từ tài liệu giảm đạo văn/AI + canonical terminology trong `SCOPE.md`; không dùng AI làm nguồn fact.
- **Mọi claim implementation:** chỉ verify trên exact `main`/contract/test theo THESIS evidence gate; không suy từ bài giảng, đề cương hoặc mẫu Khoa.

#### 4. Quy tắc dedup/context
- Không tải/đọc lại toàn bộ bộ bài giảng ở mỗi session. Sau recovery `AGENTS.md → SCOPE.md → active issue`, đọc **registry này → đúng source/đúng trang cần cho task**.
- Hai file Chương 5 là cùng binary; ba file Chương 6 là cùng binary. **Không retrieval lặp các bản duplicate**.
- Khi một file mới được người dùng cung cấp và có giá trị bền vững, cập nhật registry này với **tên file + vai trò + SHA-256 nếu có raw bytes**; không tạo progress/source file riêng chỉ để AI đọc.
- Page anchors/shape rules cụ thể vẫn nằm ở **HUIT UML Visual & Semantic Profile v2** ngay dưới; registry này không lặp toàn bộ lý thuyết.
- Nếu nguồn không hỗ trợ một claim, ghi rõ *nguồn không đủ để kết luận*; không tự điền từ kiến thức chung.

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

### THESIS-R1 — Hình 2.9 System Use Case tổng quan — FINAL PASS 09/11/2026

- Semantic source: `docs/thesis/uml/Hinh_2_9_System_Use_Case_Tong_Quan.puml`.
- Deterministic layout post-process: `docs/thesis/uml/postprocess_system_usecase.py`.
- Workflow commit: `115ced359c70c3e1ce5b6bc3755e4cf91c5f6513`.
- GitHub Actions run: `42532788312` — **PASS**.
- Final PNG: **1800×1400 px**, SHA-256 `1b4fe668dd40ad71c5f34165ca5dcf8972ae0717c53c34e9909527eb9d90c957`.
- Visual contract: approved prototype v3.
- Final gates:
  - đúng 10 UC summary đã khóa;
  - đúng 5 actor, mỗi actor **một visible symbol duy nhất**;
  - trái boundary: Khách hàng, Lễ tân;
  - phải boundary: Bác sĩ, Thu ngân, Điều dưỡng;
  - title safe zone sạch;
  - không connector crossing;
  - không duplicate actor;
  - connector chạm actor và ellipse;
  - không curve lớn; association là các đoạn thẳng trực tiếp;
  - cụm clinical đọc top-down: UC15 → UC16 → UC17 → UC18, chỉ là reading order, **không tạo UC relation**;
  - 0 include / 0 extend / 0 UC generalization;
  - aspect ratio 1.286; khi chèn rộng 13.0 cm thì cao ~10.1 cm, phù hợp A4.
- `postprocess_system_usecase.py` fail-fast nếu actor/UC/association trong `.puml` lệch semantic lock; script chỉ dàn trang, không thay đổi semantic.
- Các bản Hình 2.9 trước final này = **STALE**; không dùng cho Word.
- Scope stop đạt. NEXT chỉ sau khi xác nhận: **Hình 2.10 — nhóm tài khoản và hồ sơ**.

### THESIS-R1 — Hình 2.9 PlantUML translation attempts — REJECTED 09/11/2026

- Approved visual contract remains **prototype v3**; không thay đổi.
- PlantUML translation commit `4c242009f22b17a9ee3401ba1ced8e666448361b`: CI render success nhưng visual **FAIL** — actor sides/layout bị đảo, nhiều đường dài, không bám prototype.
- PlantUML translation commit `2450d315a9bd09788f56958deb9a3985f3bfbcb8`: CI render success nhưng visual **FAIL** — quá ngang, title safe zone bị connector xuyên, actor placement không đúng contract.
- Cả hai commit là **renderer experiments / STALE**, không dùng cho Word và không được gọi PASS.
- Rule được tái xác nhận: **CI xanh ≠ visual PASS; renderer phải phục vụ prototype đã duyệt, không ngược lại.**

### THESIS-R1 — Hình 2.9 layout prototype v3 — APPROVED 09/11/2026

- User review: **APPROVED**.
- Prototype artifact: `Hinh_2_9_layout_prototype_v3.png`.
- Đây là **visual contract reference**, không phải final thesis artifact.
- Geometry khóa:
  - trái boundary: **Khách hàng** ở cụm trên; **Lễ tân** ở cụm dưới;
  - phải boundary: **Bác sĩ → Thu ngân → Điều dưỡng** theo top-down;
  - cụm customer: UC09 → UC10 → UC11 → UC19 top-down;
  - cụm reception: UC12 → UC13 top-down, đặt thấp hơn để không trộn dây với Customer;
  - cụm clinical: UC15 → UC16 → UC17 → UC18 top-down;
  - title safe zone sạch;
  - association đi trực tiếp actor↔UC, không crossing, không duplicate actor.
- NEXT: triển khai đúng geometry này trong PlantUML canonical; renderer/post-process chỉ được phép phục vụ geometry đã duyệt.

### THESIS-R1 — UML production discipline / visual gate v3 — 09/11/2026

Áp dụng cho toàn bộ UML thesis từ Hình 2.9 trở đi. Mục tiêu: **semantic quyết định hình; renderer chỉ thi công**, không để auto-layout làm thay đổi cách hiểu mô hình.

#### Thứ tự làm việc bắt buộc
1. **Source-of-truth recovery**: đọc SCOPE/active issue; nếu hình có claim implementation thì ghi exact `main` HEAD và chỉ dùng evidence trực tiếp.
2. **Semantic lock trước**: khóa actor/node/relation/WHEN-WHY; chưa khóa semantic thì chưa vẽ.
3. **Visual contract trước renderer**: xác định actor bên nào, cụm node nào, reading order, title safe zone, routing rule, target aspect ratio/A4 size.
4. **Layout prototype**: làm mockup hình học đơn giản để kiểm cognitive load; prototype không được dùng thay final PlantUML.
5. **Human QA trước khi code renderer**: tự xem như người chấm — có hiểu trong vài giây không, có phải lần dây không, vị trí có ngầm kể sai workflow không.
6. **PlantUML source-first**: chỉ sau khi visual contract PASS mới viết/chỉnh `.puml`.
7. **Render engine thật**: CI xanh chỉ chứng minh render thành công, **không đồng nghĩa visual PASS**.
8. **Compare render với visual contract**: nếu renderer phá contract, sửa layout/helper hoặc dùng post-process hình học deterministic; **không đổi semantic để chiều renderer**.
9. **Scope stop**: Hình hiện tại phải semantic + visual PASS rồi mới sang hình kế tiếp.

#### Hard visual gates
- **Một semantic actor = một visible actor symbol trong cùng diagram.** Không duplicate actor để giảm crossing.
- Actor đặt ngoài system boundary; UC bên trong; connector phải **chạm đúng actor và ellipse**.
- Không connector chạy qua title, label, actor, ellipse hoặc system-boundary title safe zone.
- Không connector crossing; tránh spline/curve lớn. Ưu tiên đường thẳng hoặc orthogonal ngắn.
- Không dùng chung một “bus/trunk” association nếu làm mơ hồ actor đang nối với UC nào.
- Title có safe zone riêng, không có connector đi xuyên.
- Layout phải hỗ trợ đọc, nhưng **không được ngầm tạo workflow sai**. Nếu các UC có trình tự nghiệp vụ rõ, thứ tự thị giác không được mâu thuẫn với trình tự đó dù không vẽ UC→UC.
- Không dùng `include`, `extend`, actor generalization, duplicate actor hay UC cha giả chỉ để chữa layout.
- Không thay đổi actor–UC mapping, tên UC hoặc mức decomposition vì renderer xấu.
- Summary view ưu tiên orientation; detail view ưu tiên coverage. Relation bị lược trên summary phải có rule/traceability rõ.
- A4 gate: font đọc được khi chèn khoảng 10.5–13.5 cm width; preferred height 10–15 cm, soft cap ~15.5 cm; aspect ratio locked.
- Monochrome, no shadow, Times New Roman; adaptive ellipse sizing theo label, không ép cùng kích thước nếu gây dư khoảng trắng.

#### Anti-patterns đã gặp — cấm lặp lại
- **Renderer-first / engine roulette**: đổi Graphviz → ELK → Smetana liên tục mà chưa khóa target layout.
- **CI-green = PASS**: không được.
- **Duplicate actor để giảm dây**: dù semantic nói là cùng actor, người đọc vẫn có thể hiểu thành hai actor.
- **Chỉ đếm crossing**: “không crossing” vẫn có thể khó đọc nếu dây dài, cong, đi qua title hoặc phải lần dây.
- **Layout-driven semantics**: không thêm/bớt UC, actor, include/extend vì muốn hình đẹp.
- **Tối ưu từng đường mà quên whole-page reading**: phải review toàn hình ở kích thước A4.
- **Chuyển NEXT quá sớm**: không sang diagram khác khi diagram hiện tại còn visual debt.

#### Post-process policy
- PlantUML vẫn là source/renderer canonical.
- Post-process chỉ được sửa **hình học trình bày** (crop, connector endpoint, ObjectFlow/association anchoring, padding), không thêm/bớt semantic element.
- Post-process phải deterministic, fail-fast theo expected base size/shape hints và được CI render lại.
- Nếu post-process làm thay đổi meaning hoặc không trace được về PlantUML source thì không dùng.

#### Hình 2.9 visual contract v2
- **Trái boundary:** Khách hàng, Lễ tân.
- **Phải boundary:** Bác sĩ, Thu ngân, Điều dưỡng.
- **Cụm tiếp nhận/dịch vụ:** UC09 Hồ sơ; UC10 Tra cứu; UC11 Đăng ký; UC12 Lịch hẹn; UC13 Gửi nhắc; UC19 Hành trình.
- **Cụm thực hiện tiêm:** UC15 Khám sàng lọc/chỉ định → UC16 Thanh toán → UC17 Ghi nhận mũi → UC18 Theo dõi sau tiêm theo reading order trên→dưới; đây chỉ là bố cục, không tạo UC→UC relation.
- Summary primary associations giữ nguyên: Khách hàng→UC09/10/11/19; Lễ tân→UC12/13; Bác sĩ→UC15; Thu ngân→UC16; Điều dưỡng→UC17/18.
- **0 include / 0 extend / 0 UC generalization** trên Hình 2.9.
- Title safe zone phải sạch hoàn toàn; mọi association ngắn, rõ, không curve/crossing.

### THESIS-R1 — Hình 2.9 layout prototype v3 — CANDIDATE 09/11/2026

- Đây là **renderer-independent layout prototype**, không phải final PlantUML artifact và không chèn Word.
- Prototype hiện đáp ứng:
  - một visible symbol cho mỗi actor;
  - actor bên trái: Khách hàng, Lễ tân; bên phải: Bác sĩ, Thu ngân, Điều dưỡng;
  - title safe zone sạch;
  - không connector crossing;
  - không curve/spline; association là các đoạn thẳng rõ;
  - clinical reading order top-down: UC15 khám → UC16 thanh toán → UC17 mũi tiêm → UC18 theo dõi sau tiêm;
  - customer/front-office cluster tách khỏi clinical cluster;
  - 10 UC và primary actor mapping giữ nguyên; 0 include / 0 extend.
- Human-review caveat: đây mới là **layout contract candidate**. Chỉ sau khi người dùng/QA chấp nhận cách đọc mới chuyển geometry này sang PlantUML + deterministic post-process nếu renderer không tự giữ được đường nối.
- File prototype trong conversation: `Hinh_2_9_layout_prototype_v3.png`.
- NEXT nếu prototype được chấp nhận: implement geometry vào canonical `.puml`, render CI thật, post-process connector geometry nếu cần, rồi visual QA A4.

### THESIS-R1 — Hình 2.9 visual candidate v15 — SUPERSEDED / STALE 09/11/2026

- Source commit: `77507a0a687224fe14c1c4ad4d21783d03b99edd`.
- Render CI: run `42530657648` — PASS.
- Renderer: PlantUML 1.2026.9 + ELK orthogonal layout.
- PNG: **1446×1433 px**; near-square, phù hợp A4 hơn các ELK thử nghiệm trước.
- Đạt:
  - đúng **một** ký hiệu cho mỗi actor;
  - không duplicate `Khách hàng`;
  - association orthogonal, không đường cong;
  - không connector crossing;
  - 10 UC summary và primary actor mapping không đổi.
- Chưa gọi final visual PASS vì ELK kết thúc association ở actor-node bounding box, tạo một khoảng hở nhỏ trước stick figure ở một số actor. Đây là renderer/layout artifact, không phải semantic disconnect.
- v15 **không còn là visual baseline** sau human review. Visual baseline mới phải theo `UML production discipline / visual gate v3` + `Hình 2.9 visual contract v2`.

### THESIS-R1 — Hình 2.9 System Use Case tổng quan — VISUAL REOPENED 09/11/2026

- Source: `docs/thesis/uml/Hinh_2_9_System_Use_Case_Tong_Quan.puml`.
- Exact source commit: `7767b899775f6224293bdbd44d160d667661dec1`.
- GitHub Actions render run: **#25 / 42529155748 — PASS**.
- Rendered PNG: **1245×1488 px**, monochrome, Times New Roman, no shadow.
- Final summary semantic:
  - 10 UC: UC09, UC10, UC11, UC12, UC13, UC15, UC16, UC19, UC17, UC18.
  - Không actor `Thời gian` ở summary; UC14 automatic reminder giữ ở detail Hình 2.11.
  - Chỉ primary/representative actor associations trên summary; full actor relation coverage nằm ở detail views.
  - **Không lặp actor trên cùng diagram.** Phản hồi review phát hiện việc lặp `Khách hàng` dù cùng semantic vẫn dễ bị hiểu thành hai actor khác nhau; Hình 2.9 phải dùng đúng **một** ký hiệu `Khách hàng`.
  - 0 include / 0 extend / 0 UC generalization; không dùng UC→UC để kể workflow.
- Visual QA cũ **bị rút PASS** do hai vấn đề trình bày: connector cong và duplicate actor `Khách hàng`.
- Gate mới: một actor `Khách hàng` duy nhất; connector ưu tiên straight/orthogonal, không đường cong; vẫn giữ A4 readability và không crossing.
- Hình 2.9 v1–v7 là intermediate/stale; không dùng trong Word.
- NEXT: Hình 2.10 — nhóm tài khoản và hồ sơ. Chưa sửa Word/renumber/caption cho tới khi 2.10–2.13 PASS.

### THESIS-R1 — Hình 2.10 semantic lock v1 / layout candidate v4 — 09/11/2026

**Evidence baseline:** exact `main` HEAD `1fa336942559047a75400e3c863c08a3b294416e`.

#### Semantic scope
Hình 2.10 — **Sơ đồ Use Case nhóm tài khoản và hồ sơ** gồm đúng 6 UC:
- UC01 **Đăng ký tài khoản khách hàng**
- UC02 **Đăng nhập hệ thống**
- UC03 **Đổi mật khẩu**
- UC04 **Đăng xuất hệ thống**
- UC05 **Thay đổi chức danh tài khoản nhân viên**
- UC09 **Quản lý hồ sơ khách hàng/người được tiêm**

Actor dùng trong view:
- general actors: **Người dùng**, **Nhân viên**
- concrete actors: **Khách hàng**, **Lễ tân**, **Quản trị viên**

Actor generalization:
- Khách hàng —|> Người dùng
- Nhân viên —|> Người dùng
- Lễ tân —|> Nhân viên
- Quản trị viên —|> Nhân viên

Actor–UC associations:
- Người dùng — UC02, UC03, UC04
- Khách hàng — UC01, UC09
- Lễ tân — UC09
- Quản trị viên — UC05

Relations:
- **0 `<<include>>`, 0 `<<extend>>`, 0 generalization giữa UC**.
- Actor generalization chỉ dùng để kế thừa các goal xác thực chung; không dùng để giảm dây một cách giả tạo.
- UC01 không include xác minh email; xác minh email là registration continuation/supporting flow.
- Password reset không xuất hiện như top-level UC trong view; vẫn là supporting Identity flow đã khóa ở reconciliation v4.
- UC05 chỉ là **thay đổi chức danh tài khoản nhân viên hiện có**; không claim provision/reset/lock/full permission matrix.

#### Visual contract candidate v4
- Boundary bên phải; actor hierarchy nằm riêng bên trái.
- Auth cluster ở phần trên: UC02 → UC03 → UC04 theo reading order.
- Customer registration nằm dưới auth cluster: UC01.
- Profile/admin cluster ở phần dưới: UC09 và UC05.
- `Người dùng` là parent của Khách hàng/Nhân viên; `Nhân viên` là parent của Lễ tân/Quản trị viên.
- Concrete actors được đặt gần UC riêng của mình để association đi sang phải; generalization đi về trái/trên.
- Title safe zone sạch; không connector đi qua actor/node khác; một actor = một visible symbol.
- Prototype candidate: `Hinh_2_10_layout_prototype_v4.png`.
- Prototype v1–v3 là intermediate, không canonical.
- Chưa viết PlantUML canonical cho Hình 2.10 cho tới khi human review chấp nhận candidate v4.

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
     - UC13 Gửi nhắc lịch
     - UC15 Ghi nhận khám sàng lọc và chỉ định
     - UC16 Thanh toán dịch vụ
     - UC17 Ghi nhận mũi tiêm
     - UC18 Ghi nhận theo dõi sau tiêm
     - UC19 Theo dõi hành trình tiêm chủng
   - Actor hiển thị: **Khách hàng, Lễ tân, Bác sĩ, Thu ngân, Điều dưỡng**.
   - Không hiển thị actor hierarchy, **Thời gian**, Dịch vụ thư điện tử, Admin, Nhân viên kho hay các UC account/admin/support ở summary; các quan hệ đó được thể hiện ở detail view.
   - UC14 **Tự động gửi nhắc lịch** vẫn thuộc model 21 UC nhưng chỉ xuất hiện ở Hình 2.11 chi tiết; summary dùng UC13 **Gửi nhắc lịch** để giữ góc nhìn actor–goal trực quan.
   - Không dùng mũi tên UC→UC để kể thứ tự. Vị trí chỉ phục vụ đọc từ trái/phải và trên/xuống.
   - **Summary association policy:** Hình 2.9 chỉ hiển thị **primary/representative actor** cho từng UC để giữ orientation/readability; các direct actor association còn lại vẫn thuộc model và được thể hiện đầy đủ ở detail views. Không được hiểu việc thiếu một association trên summary là actor đó mất quyền/capability.
   - Primary/representative mapping của summary: Khách hàng → UC09, UC10, UC11, UC19; Lễ tân → UC12, UC13; Bác sĩ → UC15; Thu ngân → UC16; Điều dưỡng → UC17, UC18.

2. **Hình 2.10 — Sơ đồ Use Case nhóm tài khoản và hồ sơ**
   - UC01 Đăng ký tài khoản khách hàng
   - UC02 Đăng nhập hệ thống
   - UC03 Đổi mật khẩu
   - UC04 Đăng xuất hệ thống
   - UC05 Thay đổi chức danh tài khoản nhân viên
   - UC09 Quản lý hồ sơ khách hàng/người được tiêm
   - Actor: **Người dùng, Khách hàng, Nhân viên, Lễ tân, Quản trị viên**.
   - Đây là view ưu tiên thể hiện actor generalization cần thiết cho shared authentication; không kéo các chức danh lâm sàng/kho vào chỉ để minh họa hierarchy. Các vai trò còn lại được mô tả là chuyên biệt của Nhân viên trong phần xác định actor và xuất hiện tại detail view tương ứng.

3. **Hình 2.11 — Sơ đồ Use Case nhóm dịch vụ, đăng ký, lịch hẹn và nhắc lịch**
   - UC10 Tra cứu dịch vụ tiêm chủng
   - UC11 Đăng ký dịch vụ tiêm chủng
   - UC12 Quản lý lịch hẹn
   - UC13 Gửi nhắc lịch
   - UC14 Tự động gửi nhắc lịch
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

### THESIS-R1 — System UC independent-review reconciliation v4 — 09/11/2026

**Review source:** independent Claude critique supplied by user, then re-verified against exact `main` HEAD `1fa336942559047a75400e3c863c08a3b294416e`.

#### Meta-rule corrected
- **Artifact count is NOT a semantic criterion.** “21 UC ⇒ 21 bảng/21 Sequence” is a consequence, not evidence that the model is too granular.
- Promote/demote/merge only by actor goal, trigger/session, complete result, abstraction level, cohesive flow/rules, HUIT semantics, and current-main evidence.
- Do not reduce UC count merely to reduce report workload.

#### Reconciliation of disputed UCs
- **UC04 Đăng xuất hệ thống — KEEP TOP-LEVEL.**
  - Evidence: `IdentitySessions.LogoutAsync` locks current session, calls `RevokeSessionAsync(...,"logout",...)`, and writes security audit action `logout`; web also deletes refresh/CSRF cookies.
  - HUIT gate: direct actor action, single session, consistent security result “phiên hiện tại bị thu hồi”; not merely UI cleanup.
- **UC05 — KEEP but RENAME to “Thay đổi chức danh tài khoản nhân viên”.**
  - Evidence: A12 only lists existing accounts/fixed roles and PUTs `/{id}/role`; role change revokes account sessions and audits `employeeRoleChanged`.
  - No claim full account lifecycle, provisioning, arbitrary permission matrix, lock/reset HTTP.
- **UC08 Quản lý lô và tồn kho vắc xin — KEEP grouped, but scope must be explicit.**
  - Evidence: X1 read lots/movements + X2.1 receive a completely new lot + X2.2.1 read receipt history.
  - Windows A05 exposes inventory browsing, “Nhập lô vắc xin”, and “Lịch sử tiếp nhận” in one warehouse goal area.
  - HUIT CRUD/operation grouping is acceptable when actor/domain/result family is cohesive; Sequence may model the primary write path with `alt/opt` for relevant branches, not every read-only screen.
  - Hard scope: no arbitrary stock adjustment/transfer, no top-up existing lot, no full supplier management.
- **UC14 — KEEP but RENAME to “Tự động gửi nhắc lịch”.**
  - Evidence: `ReminderScheduler` enqueues `ReminderEmail` at configured reminder kinds/times; actual implemented delivery concern is email. No separate implemented actor goal “liên hệ theo lịch”.
  - `Thời gian` remains special trigger actor in detail view under HUIT convention; email service remains secondary delivery actor.
- **UC19 Theo dõi hành trình tiêm chủng — KEEP.**
  - Evidence: journey aggregates registration ownership + payment + appointment schedule + registered-dose progress + completed shots for a registration.
- **UC20 Tra cứu lịch sử tiêm chủng — KEEP separately.**
  - Evidence: history endpoint projects completed vaccination records/shots, optionally filtered by recipient, ordered by vaccination time.
  - Different actor result from journey; do not merge merely because both are read-only.
- **Password recovery — KEEP AS SUPPORTING IDENTITY FLOW, not top-level thesis UC.**
  - Evidence: request creates a time-limited reset token/mail; confirm later validates token, changes password, revokes account sessions, audits reset.
  - Modeling rule: authenticated `Đổi mật khẩu` is a single-session self-service goal; token/email-mediated reset is a multi-step continuation that may leave/resume the interaction, so it stays supporting identity behavior under current HUIT single-session gate.
  - Email verification likewise remains registration continuation/supporting behavior, not an additional top-level UC.

#### Count after reconciliation
- Exact top-level System UC count remains **21**.
- This is **not** justified by wanting 21 artifacts; it is the result after semantic/evidence audit.
- Names changed without changing count:
  - UC05: `Quản lý chức danh tài khoản nhân viên` → **Thay đổi chức danh tài khoản nhân viên**.
  - UC14: `Tự động nhắc lịch và liên hệ theo lịch` → **Tự động gửi nhắc lịch**.
- UC04/08/19/20 remain top-level.
- 0 `<<include>>`, 0 `<<extend>>`, 0 UC generalization remains valid.

#### Consequence for report artifacts
- If the official HUIT template is followed literally, expect **21 System UC specification tables** and **21 Sequence Diagrams** unless GVHD explicitly authorizes a narrower Sequence subset.
- A Sequence represents the UC's primary processing; it does **not** require a separate sequence per CRUD branch/alternative flow. Important alternatives may use `alt/opt` fragments.
- Artifact burden may drive layout/appendix decisions, but never semantic merging.

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
