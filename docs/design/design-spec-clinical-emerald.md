# DESIGN SPEC — CLINICAL EMERALD / MINT SOFT-MODERN
> Nguồn tham chiếu: Hình ảnh giao diện phong cách y tế hiện đại (MedTech / Clinical UI).
> Mục đích: Làm chuẩn phong cách UI duy nhất cho việc thiết kế lại toàn bộ giao diện WPF Desktop (`apps/windows`).

---

### 1. BẢNG MÀU (COLOR PALETTE)

* **Nền tổng thể (App Background / Canvas)**:
  * `#F6FAF7` (Xanh ngọc / xám phấn cực nhạt, dịu mắt, sạch sẽ, không dùng màu xám xỉn hay trắng gắt).
* **Bề mặt / Thẻ (Card & Container Surface)**:
  * `#FFFFFF` (Trắng thuần khiết, tạo độ tương phản nhẹ và nổi bật nội dung).
* **Màu chủ đạo & Điểm nhấn (Primary / Accent)**:
  * Primary Green / Emerald: `#10B981` dùng cho điểm nhấn, biểu đồ và nền nhạt; nền nút chính có chữ trắng dùng `#047857` (tỷ lệ tương phản xấp xỉ 5,5:1, đạt tối thiểu 4,5:1).
  * Hover / Active Primary: `#059669`.
  * Light Accent Tint (Background active / selection): `#ECFDF5` (nền xanh lá ngọc cực nhạt dùng cho item đang chọn).
* **Màu phụ trợ (Secondary Accents / Metrics)**:
  * Warning / Accent Gold: `#EAB308` hoặc `#F59E0B` (thanh gradient, chỉ số phụ).
  * Soft Secondary Tone: `#34D399` (đường biểu đồ mềm, timeline step).
* **Màu chữ (Typography Colors)**:
  * Text Heading / Chữ chính: `#1E293B` hoặc `#0F172A` (Slate tối đậm, độ tương phản cao, rõ nét).
  * Text Body / Chữ phụ: `#475569` hoặc `#64748B` (Slate xám trung tính vừa phải).
  * Text Muted / Chú thích / Label nhạt: `#94A3B8`.
* **Màu viền & Phân cách (Borders & Dividers)**:
  * Border Thẻ / Ô nhập mặc định: `#E2E8F0` hoặc viền hòa sắc ngọc `#D1FAE5` / `#E5E7EB` (mảnh, tinh tế).
  * Border Active / Highlight: `#10B981`.
* **Màu trạng thái (Semantic Status)**:
  * Success / Active: `#10B981` (Xanh ngọc).
  * Warning / Pending: `#F59E0B` (Hổ phách).
  * Danger / Error: `#EF4444` (Đỏ tươi nhẹ nhàng).
  * Cảnh báo an toàn lâm sàng: đỏ `#B91C1C` trên nền `#FEF2F2` cho nguy cơ/chống chỉ định đã xác nhận; cam `#92400E` trên nền `#FFFBEB` cho cảnh báo cần đánh giá. Có nhãn chữ và biểu tượng, không chỉ dựa vào màu; không tự suy diễn kết luận y khoa.
  * Trạng thái luồng: chờ `#92400E`/`#FFFBEB`, đang xử lý `#1D4ED8`/`#EFF6FF`, hoàn tất `#047857`/`#ECFDF5`, lỗi `#B91C1C`/`#FEF2F2`. Chỉ gán trạng thái khi có dữ liệu đáng tin cậy; trạng thái chưa rõ dùng nhãn “Chưa xác định”.
  * Nhãn luồng cụ thể: **Chờ khám** và **Chờ thanh toán** dùng cam `#92400E`/`#FFFBEB`; **Chờ tiêm** dùng xanh dương `#1D4ED8`/`#EFF6FF`; **Theo dõi** dùng xanh dương `#1D4ED8`/`#EFF6FF` kèm nhãn; **Đã khám** dùng xanh lá `#047857`/`#ECFDF5`; **Hoãn** dùng cam `#92400E`/`#FFFBEB` kèm lý do nếu dữ liệu có. Màu chỉ là hiển thị, không tự suy ra trạng thái nghiệp vụ.
  * Vô hiệu: chữ `#64748B`, nền `#F1F5F9`, viền `#CBD5E1`; giữ nhãn đọc được và không dùng màu vô hiệu để biểu diễn lỗi.
  * Hết hạn: chữ `#B91C1C`, nền `#FEF2F2`, viền `#FECACA`, nhãn **Hết hạn**; phân biệt rõ với điều khiển vô hiệu. Không cho chọn lô hết hạn trong thao tác tiêm.

---

### 2. TYPOGRAPHY (HỆ CHỮ)

* **Font Family**:
  * Sử dụng hệ font hình học / grotesque hiện đại: `Plus Jakarta Sans`, `Inter`, hoặc fallback chuẩn Windows `Segoe UI Variable / Segoe UI`.
* **Cấp bậc & Kích thước (Type Scale)**:
  * **Page Title**: `20px` – `22px` | `SemiBold` (`600`) | Line-height `28px`
  * **Card / Section Header**: `15px` – `16px` | `SemiBold` (`600`) | Line-height `22px`
  * **Key Metrics / Số liệu lớn**: `22px` – `26px` | `Bold` (`700`) | Line-height `32px`
  * **Body / Text nội dung**: `13px` – `14px` | `Regular` (`400`) hoặc `Medium` (`500`) | Line-height `20px`
  * **Small Text / Label phụ / Tag**: `11px` – `12px` | `Medium` (`500`) | Line-height `16px`
  * **Caption / Micro-copy**: `10px` | `Regular` (`400`)
* **Tracking & Spacing**:
  * Chữ số (Metrics) dùng `tabular-nums` (đều nhau dễ căn gióng).
  * Tiêu đề nhỏ (Section label) đôi khi dùng chữ hoa nhẹ với `letter-spacing: 0.5px`.

---

### 3. HÌNH KHỐI, VIỀN & ĐỔ BÓNG (SHAPES, BORDERS & SHADOWS)

* **Độ bo góc (Border Radius - Rất mềm mại & bo tròn)**:
  * **Card lớn / Bảng / Container chính**: `CornerRadius = 14px` đến `16px`.
  * **Thẻ con / Sub-card / Khối thống kê**: `CornerRadius = 10px` đến `12px`.
  * **Button, Input, Dropdown**: `CornerRadius = 8px` đến `10px`.
  * **Badge, Chip, Avatar, Icon container**: `CornerRadius = 999px` (Pill / Circular) hoặc `8px` bo tròn.
* **Viền (Borders)**:
  * Viền mỏng toàn diện: `BorderThickness = 1px`.
  * Nét viền nhạt thanh thoát, không dùng viền đôi hay viền dày.
* **Đổ bóng (Box Shadow - Soft Elevation)**:
  * Hầu như rất ít bóng đậm, ưu tiên **Flat Soft** dựa trên độ tương phản giữa Card `#FFFFFF` trên nền Canvas `#F6FAF7`.
  * Nếu dùng đổ bóng cho Card nổi bật: `Blur = 12px`, `Opacity = 0.04 - 0.06`, `Color = #000000`, `Offset-Y = 2px` (đổ bóng mờ mịn màng như sương).

---

### 4. KHOẢNG CÁCH & NHỊP LƯỚI (SPACING & GRID SYSTEM)

* **Nhịp lưới chuẩn**: Bội số **4px / 8px** (8px, 12px, 16px, 20px, 24px).
* **Card Padding**:
  * Thẻ lớn: `Padding = 20px` hoặc `24px`.
  * Thẻ thông tin nhỏ / ô chỉ số: `Padding = 12px` đến `16px`.
* **Gap / Spacing giữa các khối**:
  * Khoảng cách giữa các cột/hàng: `16px` đến `20px`.
* **Mật độ (Density)**:
  * Mật độ vừa phải đến thoáng (Comfortable density), không chật chội dồn cục, có khoảng thở (white-space) giữa các nhóm dữ liệu.

---

### 5. PHONG CÁCH CÁC THÀNH PHẦN (COMPONENTS STYLE)

* **Thẻ (Cards)**:
  * Nền trắng, bo tròn lớn (`16px`), viền mỏng `#E2E8F0`. Tiêu đề thẻ thường đi kèm 1 icon nhỏ nằm trong khối tròn/vuông bo góc màu nhạt `#ECFDF5`.
  * Có nút menu thao tác nhanh `...` (3 chấm) ở góc trên bên phải của từng thẻ.
* **Nút bấm (Buttons)**:
  * **Primary Button**: Nền `#047857`, chữ trắng (tương phản xấp xỉ 5,5:1), không viền, bo góc `8px`, thường đi kèm icon phía trước (`+`). Kiểm tra lại tối thiểu 4,5:1 ở trạng thái hover/pressed/disabled có chữ.
  * **Secondary / Neutral Button**: Nền trắng hoặc xám phấn nhạt `#F1F5F9`, chữ xám đậm, viền mỏng mờ.
  * **Pill Filter Button**: Cụm nút bấm lọc dạng viên thuốc (Pill tabs) với nền xám nhạt bo tròn bọc ngoài, nút active có nền trắng và viền nhẹ.
* **Ô nhập liệu (Inputs & Search)**:
  * Nền trắng hoặc xám ngọc siêu nhạt, bo góc `8px - 10px`, viền mỏng `#E2E8F0`.
  * Icon kính lúp bên trái, có thể kèm shortcut gợi ý bàn phím mờ mờ ở bên phải (như `⌘ K` / `Ctrl K`).
* **Iconography**:
  * Phong cách **Line Art / Outline mỏng** (stroke 1.5px - 2px), góc bo tròn, phong cách Feather / Lucide / Fluent regular.
  * Các icon tiêu đề thường được đặt trong hộp nền nhạt màu xanh mint (`#ECFDF5`) hoặc xanh teal pastel.
* **Biểu đồ & Tiến trình (Visual Charts & Progress)**:
  * Đường line mỏng, mượt mà (smooth curve), phối màu xanh mint + vàng mật ong.
  * Thanh đo/thanh tiến trình sử dụng gradient ngang dịu nhẹ (ví dụ vàng hổ phách nhạt loang màu).
* **Timeline / Step Flow**:
  * Các chấm tròn xanh mint kết nối bằng đường kẻ mảnh đứt đoạn hoặc liền nhạt, tạo cảm giác quy trình rõ ràng, thư thái.
* **Alert.Safety / Alert.Info**:
  * `Alert.Safety` dùng đỏ hoặc cam an toàn ở bảng màu, có tiêu đề, biểu tượng và mô tả hành động rõ ràng; không chỉ dùng màu để báo rủi ro. `Alert.Info` dùng xanh dương `#1D4ED8` trên nền `#EFF6FF` cho thông tin không khẩn cấp.
* **Badge / Tab / danh sách**:
  * `Badge` có nhãn trạng thái bằng chữ và màu ngữ nghĩa; không dùng màu làm dấu hiệu duy nhất. `Tab` có trạng thái thường, chọn, hover, focus và vô hiệu; tab chọn dùng nền `#ECFDF5`, chữ `#047857` và chỉ báo rõ nét.
  * Hàng danh sách được chọn dùng nền `#ECFDF5`, viền/chỉ báo `#047857`; focus bàn phím phải nhìn thấy và độc lập với hover. Hàng vô hiệu/hết hạn giữ nhãn tương ứng, không bị nhầm với hàng được chọn.
* **SegmentedButton / Card clickable**:
  * `SegmentedButton` nhóm các lựa chọn loại trừ nhau; phần chọn có nền và nhãn rõ, hỗ trợ focus bàn phím và trạng thái vô hiệu.
  * `Card clickable` có hover, focus, nhãn đích và vùng bấm rõ; khi không được phép mở đích thì làm mờ, vô hiệu cả chuột lẫn bàn phím và giữ tên thẻ để người dùng thấy tác vụ tồn tại.

---

### 6. CẢM GIÁC TỔNG THỂ (OVERALL VIBE)

* **Sạch sẽ (Pristine)**
* **Tươi mới (Fresh / Minty)**
* **Hiện đại (Contemporary)**
* **Dịu nhẹ (Soft & Gentle)**
* **Đáng tin cậy (Clinical Trust)**
* **Thoáng đãng (Spacious)**

---

### 7. ĐIỂM NHẬN DIỆN ĐẶC TRƯNG (SIGNATURE TRAITS)

1. **Sắc thái "Xanh lá y tế công nghệ" (Clinical Emerald / Mint Soft)**: Không phải xanh dương bệnh viện truyền thống khô khan, mà là sự hòa trộn giữa xanh ngọc lục bảo tươi tắn (`#10B981`) trên nền xám phấn ánh ngọc (`#F6FAF7`), mang lại cảm giác an tâm, trị liệu và tràn đầy sinh khí.
2. **Hình khối bo cong hào phóng (Generous Rounded Corners)**: Mọi góc cạnh đều được làm mềm từ 12px đến 16px, loại bỏ hoàn toàn các cạnh sắc nhọn cứng nhắc.
3. **Thẻ viền mỏng không bóng (Clean Bordered Surfaces)**: Tối giản bóng đổ gắt gỏng, sử dụng đường viền 1px mảnh mai siêu tinh tế tạo ranh giới các khối thông tin.
4. **Huy hiệu Icon bọc nền Pastel (Tinted Icon Enclosures)**: Các tiêu đề khối luôn có icon minh họa được đặt lồng trong khung nền màu mint pastel dịu mắt.
5. **Cấu trúc chia Card độc lập theo cụm thông tin (Modular Bento-Card Layout)**: Mọi dữ liệu được đóng gói thành từng thẻ widget riêng biệt, có nhịp thở và ranh giới rõ ràng.

## Lịch sử cập nhật

| Ngày | Thay đổi |
|---|---|
| 2026-10-22 | Bổ sung màu cảnh báo an toàn, trạng thái luồng, trạng thái vô hiệu; đổi nền nút chính sang `#047857` để chữ trắng đạt tương phản tối thiểu 4,5:1. |
