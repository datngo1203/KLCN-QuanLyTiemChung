# Trạng thái Themes — 2026-10-22

Nhánh `ui/themes`, PR #19. Lượt này đổi phong cách sang **Clinical Neutral**. Phạm vi thay đổi: `docs/design/design-spec-clinical-emerald.md` (giữ tên tệp), `apps/windows/Themes/`, ảnh gallery trong `docs/ui-review/ui-themes/` và báo cáo này. Không sửa màn hình, ViewModel, API, contract hoặc schema; các tệp chưa theo dõi khác trong working tree được giữ nguyên.

## Đã làm

- Design Spec là nguồn màu duy nhất cho `Themes/Colors.xaml`: Canvas trung tính, viền mảnh, chữ rõ, Brand `#0B6B57`, năm nhóm ngữ nghĩa Neutral/Info/Success/Warning/Danger.
- Ô nhập/nút cao 32px; bảng cao 36px; góc bo control/badge 4px, card/khung 6px; không bóng, không nền mint/pill/tím. Badge có kiểu nền nhạt và biến thể chấm màu + chữ; Safety alert có vạch đỏ 4px.
- Menu/Tab active gạch chân; SegmentedButton phẳng; nút Danger dạng viền, nút xác nhận xóa đặc riêng. Icon outline 1,5px/16px. DatePicker có `Language=vi-VN`, gợi ý “Chọn ngày” và icon lịch cùng bộ.
- Gallery độc lập có “Bảng + bộ lọc” với 5 hàng minh họa, thanh công cụ, phân trang; thêm các component và trạng thái. Dữ liệu chỉ là minh họa giả, không có thông tin người thật, gallery không vào menu chính.

## Kiểm chứng

- `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release`: **đạt, 0 warning, 0 error**.
- Nạp gallery bằng WPF `XamlReader` với resource theme, render bằng `RenderTargetBitmap`, xem lại [ảnh phần trên](../ui-review/ui-themes/gallery.png) và [ảnh phần dưới](../ui-review/ui-themes/gallery-bottom.png). Ảnh lần lượt 98.361 và 75.093 byte, dưới 500 KB/tệp.
- Tính tương phản từ mã màu: Neutral 6,56:1; Info 6,16:1; Success 6,81:1; Warning 6,84:1; Danger 5,91:1. Chữ trắng trên Primary thường/hover/pressed: 6,45:1 / 8,64:1 / 11,10:1.
- `git diff --check`: đạt sau khi chuẩn hóa cuối tệp.

## Chưa kiểm chứng / giới hạn

- Gallery là dữ liệu và trạng thái minh họa; thanh lọc/phân trang không nối dữ liệu hay command nghiệp vụ. Chưa kiểm tra DPI, cửa sổ nhỏ, toàn bộ thao tác bàn phím hoặc validation binding thật của từng màn.
- Bảng “Bảng + bộ lọc” trong gallery là bản minh họa bố cục. Phần DataGrid/ListView riêng ở cuối gallery dùng style theme thật để kiểm tra hàng chọn và header.
- Không khẳng định các màn hiện có đã dùng hết style mới; các màn phải áp `CE.*` khi được chỉnh trong phạm vi riêng.
