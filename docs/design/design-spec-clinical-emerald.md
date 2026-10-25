# DESIGN SPEC — CLINICAL NEUTRAL

> Nguồn phong cách duy nhất cho `apps/windows/Themes/Colors.xaml`. Giữ tên tệp để các liên kết từ Plan và quy trình UI không đổi. Áp dụng cho giao diện WPF desktop của KLCN090; không mô tả quy tắc nghiệp vụ.

## 1. Định hướng

Clinical Neutral là giao diện phần mềm y tế doanh nghiệp: rõ thứ bậc, mật độ vừa đủ cho bảng và biểu mẫu, bề mặt phẳng, viền mảnh, màu thương hiệu chỉ đánh dấu hành động chính và vị trí chọn. Không dùng nền mint, icon pastel, bóng đổ, gradient, pill hoặc thẻ bo lớn. Trạng thái luôn có nhãn chữ, không truyền đạt bằng màu đơn lẻ.

## 2. Token màu

| Vai trò | Mã màu |
|---|---|
| Canvas | `#F4F6F8` |
| Surface | `#FFFFFF` |
| Card border | `#E3E8ED` |
| Input border | `#BCC5CE` |
| Table header | `#EEF1F4` |
| Text primary | `#1B2733` |
| Text secondary | `#4A5866` |
| Text muted / placeholder | `#7A8794` |
| Brand | `#0B6B57` |
| Brand hover | `#095646` |
| Brand pressed | `#074437` |
| Selected row | `#E8F0EE` |
| Subtle hover | `#F8FAFB` |
| Disabled surface | `#F1F3F5` |

Hàng chọn có nền `#E8F0EE` và vạch trái 3px `#0B6B57`. Nút Primary có chữ trắng trên Brand/hover/pressed. Focus dùng viền Brand 1px, giữ độ dày để không xê dịch bố cục.

### Năm nhóm màu ngữ nghĩa

| Nhóm | Foreground | Background | Accent | Dùng cho |
|---|---|---|---|---|
| Neutral | `#4A5866` | `#F1F3F5` | `#7A8794` | Tạm ngừng, Dữ liệu mẫu, trạng thái chưa xác định |
| Info | `#1D4ED8` | `#EFF6FF` | `#2563EB` | Chờ tiêm, Theo dõi |
| Success | `#166534` | `#F0FDF4` | `#16A34A` | Đã khám, Đang hoạt động |
| Warning | `#92400E` | `#FFFBEB` | `#D97706` | Chờ khám, Chờ thanh toán, Hoãn |
| Danger | `#B91C1C` | `#FEF2F2` | `#DC2626` | Hết hạn, Đã khóa, lỗi, cảnh báo an toàn |

Các cặp chữ Foreground trên Background ở bảng trên phải đạt WCAG AA ≥ 4,5:1 cho chữ thường. Giữ nhãn “Hết hạn” độc lập với điều khiển vô hiệu; không suy ra trạng thái nghiệp vụ từ màu.

## 3. Chữ, kích thước và hình khối

- Segoe UI; nội dung 13px, nhãn 12px, tiêu đề mục 16px, tiêu đề trang 20px. Tiêu đề dùng `FontWeight=SemiBold` (600).
- Ô nhập và nút cao 32px, kể cả trạng thái vô hiệu. DataGrid hàng cao 36px.
- Control và Badge bo 4px; Card và khung bảng/nội dung bo 6px; không vượt 8px. Dùng cùng hệ góc cho Tab, DataGrid và các khung nội dung.
- Bề mặt không đổ bóng. Chỉ dùng viền 1px; focus đổi màu viền, không tăng độ dày.
- Nhịp khoảng cách 4/8px; vùng biểu mẫu và bảng ưu tiên trình bày thông tin rõ hơn khoảng trắng trang trí.
- Icon cùng một bộ nét outline 1,5px, kích thước 16px; không bọc nền pastel.

## 4. Component

- **TextBox, PasswordBox, ComboBox, DatePicker, CheckBox, RadioButton:** viền Input, hover tinh tế, focus Brand 1px, vô hiệu cùng chiều cao; lỗi viền Danger nhạt và thông báo dưới ô qua `Validation.ErrorTemplate`. Placeholder màu Text muted. DatePicker dùng `Language=vi-VN`, gợi ý “Chọn ngày”, icon lịch outline cùng bộ, không dùng icon mặc định Windows hay viền kép.
- **Button:** Primary nền Brand/chữ trắng; Secondary nền trắng/viền Input; Ghost nền trong suốt; Danger viền và chữ đỏ. Nền đỏ đặc chỉ dành cho xác nhận xóa. Icon button 32×32, vuông bo 4px, chỉ hiện viền khi hover. Nút xóa dòng dùng icon thùng rác, không dùng chữ X.
- **DataGrid/ListView:** header `#EEF1F4`, hàng cao 36px, không kẻ ô dày; hover Subtle hover; chọn Selected row và vạch Brand 3px. Focus bàn phím luôn nhìn thấy.
- **Badge:** nền rất nhạt, chữ đậm theo đúng một trong năm nhóm, không viền, bo 4px; có biến thể chấm màu + chữ cho bảng. Dữ liệu mẫu dùng Neutral, không dùng tím.
- **Alert:** Safety, Warning, Info, Success có nền trắng hoặc Background nhóm, viền trung tính 1px, vạch Accent 4px bên trái và icon cùng nhóm. Safety là thành phần duy nhất có nhấn mạnh đỏ và tiêu đề đậm; không tự suy luận nguy cơ y khoa.
- **Menu ngang cấp 1/2, Tab:** nền trắng, active gạch chân Brand 2px, không nền pill; hover nhẹ, focus rõ, disabled còn đọc được.
- **SegmentedButton:** nền Neutral, lựa chọn hiện tại nền trắng và viền 1px theo nhóm kết luận (Neutral/Info/Warning), giữ focus/disabled; không dùng viền dày hoặc nền màu đậm.
- **Card / Card clickable:** Surface, viền Card, bo 6px; clickable có hover nhẹ, con trỏ tay và focus rõ, disabled ngăn cả chuột/bàn phím.
- **Toast:** Surface hoặc màu chữ chính tùy ngữ cảnh, viền 1px, bo tối đa 6px; thông điệp ngắn có nhãn rõ.

## 5. Kiểm chứng

Gallery độc lập phải thể hiện trạng thái các component và khối “Bảng + bộ lọc” với tiêu đề trang, thanh công cụ, bảng 5 hàng đủ nhóm trạng thái, phân trang. Chụp ảnh phần trên và phần dưới tại `docs/ui-review/ui-themes/`, dưới 500 KB mỗi PNG. Build Windows Release và ghi lệnh/kết quả, giới hạn kiểm tra vào `docs/archive/status-themes.md`.

## Lịch sử cập nhật

| Ngày | Thay đổi |
|---|---|
| 2026-10-22 | Chuyển từ Clinical Emerald / Mint Soft-Modern sang Clinical Neutral; chốt token màu, năm nhóm ngữ nghĩa, kích thước, hình khối phẳng và quy tắc component doanh nghiệp. |
| 2026-10-22 | Bổ sung màu cảnh báo an toàn, trạng thái luồng, trạng thái vô hiệu; nền nút chính trước đây `#047857`. Dòng này là lịch sử của phiên bản cũ. |
