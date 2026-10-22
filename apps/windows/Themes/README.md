# Clinical Emerald — nền giao diện WPF

Bốn dictionary được viết mới trong Giai đoạn 2, theo [Design Spec](../../../docs/design/design-spec-clinical-emerald.md). Không kế thừa key/style/template từ các dictionary cũ của dự án. Toàn bộ key mới có tiền tố `CE.`.

## Nạp tài nguyên

`App.xaml` đã merge WPF UI Light + ControlsDictionary, sau đó Colors → Typography → Cards → Controls; giữ nguyên thứ tự này. Colors và Typography là phụ thuộc của hai dictionary còn lại. Không merge mỗi dictionary nhiều lần hoặc nạp Controls độc lập mà thiếu các phụ thuộc.

Đợt này chỉ tạo nền Theme. Style có key, cần chỉ định tại màn mới; không tự áp toàn cục lên các màn cũ. Việc thấy bố cục/màu cũ khi mở app chưa có nghĩa Theme mới không nạp. Các màn sẽ được dựng riêng ở giai đoạn sau.

## Phân chia

| File | Nội dung |
|---|---|
| Colors.xaml | Color/Brush ngữ nghĩa, canvas/surface/mint/Emerald, trạng thái, radius, padding, khoảng cách và gradient tùy chọn |
| Typography.xaml | Segoe UI Variable → Segoe UI; kiểu chữ trang/section/body/label/caption/metric/error; chữ số metric tabular |
| Controls.xaml | Nút chuẩn và WPF UI, ô nhập/mật khẩu, ComboBox, DatePicker, CheckBox/RadioButton, pill chọn, DataGrid, focus/validation/disabled |
| Cards.xaml | Card thường/gọn/mint/nổi nhẹ, vùng icon, badge/callout success/warning/danger/info |

## Các key dùng cho màn mới

```xml
<Border Style="{StaticResource CE.Card}">
    <StackPanel>
        <TextBlock Text="Thông tin người được tiêm"
                   Style="{StaticResource CE.Text.Section}" />
        <TextBox Style="{StaticResource CE.TextBox}"
                 Margin="0,16,0,0" AutomationProperties.Name="Họ và tên" />
        <TextBlock Style="{StaticResource CE.Text.Error}" />
        <Button Content="Tiếp tục" Style="{StaticResource CE.Button.Primary}"
                Margin="0,16,0,0" />
    </StackPanel>
</Border>
```

- Chữ: `CE.Text.PageTitle`, `.Section`, `.Body`, `.Secondary`, `.Label`, `.Caption`, `.Metric`, `.Error`. Caption 10 DIP chỉ dành chú thích không thiết yếu; không dùng thay nhãn trường.
- Nút WPF: `CE.Button` (trung tính), `.Primary`, `.PrimaryStrong`, `.Quiet`, `.Danger`.
- Nút WPF UI: `CE.UiButton.Default`, `.Primary`, `.PrimaryStrong`, `.Quiet`, `.Danger`. Template mới giữ `Icon`, Content và access key. Khi dùng style này, chọn biến thể bằng key; không dựa vào `Appearance` để thay đổi màu.
- Ô nhập: `CE.TextBox`, `CE.TextBox.Multiline`, `CE.PasswordBox`; wrapper WPF UI: `CE.UiTextBox`, `CE.UiPasswordBox`.
- Chọn: `CE.ComboBox`, `CE.ComboBoxItem`, `CE.DatePicker`, `CE.CheckBox`, `CE.RadioButton`, `CE.FilterPill`.
- Bảng: `CE.DataGrid` + `.ColumnHeader`, `.Row`, `.Cell`. Màn quyết định cột, read-only/edit, selection mode và quyền thêm/xóa; Theme không tự quyết định nghiệp vụ.
- Card: `CE.Card`, `.Compact`, `.Mint`, `.Raised`; `CE.IconContainer`. Card mặc định không bóng; Raised dùng blur 12, opacity 0,05 và offset 2.
- Nhãn/trạng thái: `CE.Badge` và các biến thể `.Success`, `.Warning`, `.Danger`, `.Info`; `CE.Callout.Success/Warning/Danger/Info`. Nội dung cần có chữ/icon phù hợp, không chỉ màu. Theme không gán trạng thái lâm sàng.

## Màu và hành vi

- `CE.Button.Primary` giữ Emerald `#10B981`, hover `#059669`, pressed `#047857` đúng hướng spec gốc. `PrimaryStrong` là lựa chọn xanh đậm `#047857` để tăng tương phản; không tự xem tinh chỉnh đó đã được duyệt cho toàn bộ màn.
- Chữ chính `#1E293B`, body `#475569`, phụ `#64748B`; token `Muted` có sẵn nhưng không dùng mặc định cho nội dung cần đọc. Nền `#F6FAF7`, card trắng, lựa chọn mint.
- Dùng DIP, không đặt kích thước cửa sổ/responsive trong Theme. Không có ngưỡng y tế, trạng thái đăng ký, quyền, xác thực hoặc cách tính tiền trong dictionary.
- Input WPF có template mới và đúng `PART_ContentHost`. ComboBox có popup/list/selection và `PART_EditableTextBox`, hỗ trợ chế độ editable.
- Các wrapper WPF UI giữ template thư viện để bảo toàn placeholder/clear/reveal, chỉ override các thuộc tính được hỗ trợ; đây là template của dependency WPF-UI 4.3.0, không phải ResourceDictionary cũ của dự án. TextBox/PasswordBox WPF UI không có property CornerRadius riêng, nên không gán một property không tồn tại. Các control này giữ chi tiết template/bo góc do thư viện cung cấp.
- DatePicker, CheckBox, RadioButton và phần khung DataGrid giữ template nền tảng/thư viện để bảo toàn hành vi bàn phím/calendar/scroll. Nút, input WPF, ComboBox, pill và cell dùng template CE mới.
- Focus bàn phím có viền. Input và cell có viền lỗi khi `Validation.HasError`; màn phải bổ sung thông báo chữ cạnh trường bằng `CE.Text.Error` cùng thông tin accessibility. Theme không thay validator hoặc tự viết tooltip chứa dữ liệu nhạy cảm.

## Kiểm tra Giai đoạn 2 — 21/10/2026

- `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release --no-restore`: thành công, 0 warnings, 0 errors (.NET SDK 10.0.401).
- Chương trình kiểm tra WPF STA tạm ngoài repo nạp tài nguyên qua `App.InitializeComponent`, khởi tạo và measure/arrange các style: **4 dictionary, 145 resource, 50 style**. Kiểm tra thêm input validation đỏ, ComboBox editable và pill được chọn: pass.
- Chưa đánh giá hiển thị trực quan toàn bộ control, mở popup bằng bàn phím hoặc DPI 125/150%. Build và kiểm tra nạp/template không thay thế kiểm thử các màn hoàn chỉnh.
- Không sửa ViewModel, code-behind, Service, MainWindow hoặc các tài liệu nguồn. `App.xaml` đã có merge đúng nên không cần chỉnh thêm.
