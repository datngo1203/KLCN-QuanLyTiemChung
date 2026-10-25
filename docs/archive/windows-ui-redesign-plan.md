# Kế hoạch Thiết kế lại Giao diện Windows Desktop

> [!IMPORTANT]
> Tài liệu này là kế hoạch triển khai; dữ liệu/hành vi đối chiếu [Screen Spec](../desktop-screen-specs.md), màu và style đối chiếu [Design Spec](../design/design-spec-clinical-emerald.md), tiến độ đối chiếu [status.md](../status.md).
> Khi context bị compact hoặc bắt đầu phiên mới, đọc file này và [quy trình UI](../ui-workflow.md) trước khi tiếp tục.

## Mục tiêu

Bỏ hoàn toàn giao diện cũ (832 dòng code-behind `MainWindow.xaml.cs` sinh UI bằng C# thuần, 111 dòng XAML khung), thay bằng kiến trúc WPF hiện đại:
- Tách mỗi màn hình thành **UserControl XAML riêng** (Page/View)
- Áp dụng pattern **MVVM** (Model-View-ViewModel) đúng chuẩn WPF
- Dùng WPF UI và bộ Theme `CE.*` phù hợp; shell hiện dùng menu ngang hai cấp (nhóm → màn) và drawer dọc khi hẹp.
- Giao diện đẹp, chuyên nghiệp, responsive, sẵn sàng nối API thật

## Phân tích giao diện cũ cần bỏ

### Những file sẽ bị xóa/viết lại hoàn toàn
| File | Dòng | Vấn đề |
|---|---|---|
| `MainWindow.xaml.cs` | 832 | Toàn bộ 14 màn hình sinh bằng C# code-behind (AddSection, TablePlaceholder, ProcessRow...). Không tách View. Không ViewModel. Dữ liệu demo hardcode trong code. |
| `MainWindow.xaml` | 111 | Chỉ là khung Sidebar + Content placeholder, sidebar xây bằng StackPanel thủ công |
| `MainWindow.Api.cs` | 18 | Partial class cho nút "Kiểm tra API" — logic giữ lại nhưng chuyển vào ViewModel |
| `DemoLoginWindow` (trong MainWindow.xaml.cs) | ~62 | Login window xây hoàn toàn bằng code-behind — cần thành XAML riêng |

### Những file giữ nguyên
| File | Lý do |
|---|---|
| `TiemChung.Desktop.csproj` | Cấu hình project, thêm package nếu cần |
| `App.xaml` / `App.xaml.cs` | Giữ cấu hình theme WPF UI, có thể mở rộng DI |
| `AssemblyInfo.cs` | Metadata, không đổi |
| `Services/TiemChungApiClient.cs` | API client tốt, mở rộng thêm endpoint mới |

## Cấu trúc đang dùng và hướng mở rộng

Hiện có `apps/windows/ViewsV2/` (MainWindow, OverviewView A01, ScreeningView A02), `apps/windows/ViewModels/` (ShellViewModel, OverviewViewModel, ScreeningViewModel, ViewModelBase, RelayCommand), `apps/windows/ViewModels/Samples/` và `apps/windows/Themes/` (Colors, Typography, Controls, Cards). `apps/windows/_Legacy/` là tên thư mục dự kiến khi chuyển giao diện kế thừa; hiện file `MainWindow.xaml`/`.cs` cũ vẫn ở gốc `apps/windows/`, chưa chuyển. Login vẫn là `DemoLoginWindow`. Những màn khác chưa có View/ViewModel; các đường dẫn dưới đây là ý định cũ, không phải file đã tồn tại.

### Sơ đồ đề xuất ban đầu (lưu để đối chiếu; tên file chưa có không phải cấu trúc hiện tại)

```
apps/windows/
├── App.xaml                          # Theme + DI container
├── App.xaml.cs                       # Startup, ServiceProvider
├── TiemChung.Desktop.csproj
├── AssemblyInfo.cs
│
├── Models/                           # Data models (DTO / display models)
│   ├── EmployeeModel.cs
│   ├── VaccineModel.cs
│   └── ...
│
├── ViewModels/                       # MVVM ViewModels đang dùng; Sample tách trong Samples/
│   ├── Base/
│   │   ├── ViewModelBase.cs          # INotifyPropertyChanged
│   │   └── RelayCommand.cs           # ICommand implementation
│   ├── ShellViewModel.cs             # Shell navigation state đang dùng
│   ├── LoginViewModel.cs
│   ├── OverviewViewModel.cs          # A01 đang dùng
│   ├── ScreeningViewModel.cs         # A02
│   ├── CustomerViewModel.cs          # A03
│   ├── CatalogViewModel.cs           # A04
│   ├── InventoryViewModel.cs         # A05
│   ├── VaccinationViewModel.cs       # A06
│   ├── RegistrationViewModel.cs      # A07
│   ├── PaymentViewModel.cs           # A08
│   ├── ScheduleViewModel.cs          # A09
│   ├── PromotionViewModel.cs         # A10
│   ├── ReportViewModel.cs            # A11
│   └── AccountViewModel.cs           # A12
│
├── ViewsV2/                          # XAML UserControls đang dùng
│   ├── MainWindow.xaml / .cs         # Shell: menu ngang hai cấp + ContentControl; drawer dọc khi hẹp
│   ├── LoginWindow.xaml / .cs        # Login riêng
│   ├── Pages/
│   │   ├── DashboardPage.xaml / .cs
│   │   ├── ScreeningPage.xaml / .cs
│   │   ├── CustomerPage.xaml / .cs
│   │   ├── CatalogPage.xaml / .cs
│   │   ├── InventoryPage.xaml / .cs
│   │   ├── VaccinationPage.xaml / .cs
│   │   ├── RegistrationPage.xaml / .cs
│   │   ├── PaymentPage.xaml / .cs
│   │   ├── SchedulePage.xaml / .cs
│   │   ├── PromotionPage.xaml / .cs
│   │   ├── ReportPage.xaml / .cs
│   │   └── AccountPage.xaml / .cs
│   └── Dialogs/                      # Popup / modal forms
│       ├── VaccineFormDialog.xaml
│       ├── BatchFormDialog.xaml
│       └── ...
│
├── Services/                         # API + business services
│   ├── TiemChungApiClient.cs         # Giữ + mở rộng
│   ├── INavigationService.cs
│   └── NavigationService.cs
│
├── Converters/                       # Value converters
│   ├── StatusToColorConverter.cs
│   └── BoolToVisibilityConverter.cs
│
└── Themes/                           # ResourceDictionary CE.* đang dùng
    ├── Colors.xaml
    ├── Styles.xaml
    └── Icons.xaml
```

## Design System — Chuẩn phong cách Clinical Emerald / Mint Soft-Modern
> *(Tham chiếu chi tiết độc lập tại [design-spec-clinical-emerald.md](../design/design-spec-clinical-emerald.md))*

Giao diện áp dụng chuẩn phong cách MedTech / Y tế hiện đại mềm mại, loại bỏ hoàn toàn phong cách xám cứng cũ:

| Thành phần | Mã màu Hex / Giá trị | Ứng dụng cụ thể trong WPF |
|---|---|---|
| **App Canvas BG** | `#F6FAF7` | Nền toàn bộ cửa sổ / nội dung (Xanh xám phấn ngọc dịu mắt) |
| **Surface / Card BG** | `#FFFFFF` | Nền của các Card, Widget, DataGrid |
| **Primary Accent** | `#10B981`; nút chính `#047857` với chữ trắng (xấp xỉ 5,5:1) | Điểm nhấn, chọn mục; nền nút chính đạt tối thiểu 4,5:1 |
| **Primary Tint (Soft)** | `#ECFDF5` | Nền icon badge tiêu đề, nền hàng/menu được chọn (Active item) |
| **Secondary Accent** | `#F59E0B` / `#EAB308` | Chỉ số phụ, trạng thái cảnh báo, thanh gradient tiến độ |
| **Cảnh báo an toàn** | Đỏ `#B91C1C`/nền `#FEF2F2`; cam `#92400E`/nền `#FFFBEB` | Nguy cơ đã xác nhận / cần đánh giá; kèm nhãn, không tự chẩn đoán |
| **Trạng thái luồng** | Chờ cam; xử lý `#1D4ED8`/`#EFF6FF`; hoàn tất `#047857`/`#ECFDF5`; lỗi đỏ | Chỉ hiển thị theo dữ liệu xác thực; chưa rõ ghi “Chưa xác định” |
| **Vô hiệu** | Chữ `#64748B`, nền `#F1F5F9`, viền `#CBD5E1` | Điều khiển chưa thể thao tác; không dùng thay trạng thái lỗi |
| **Hết hạn** | Chữ `#B91C1C`, nền `#FEF2F2`, viền `#FECACA` | Lô hết hạn có nhãn riêng, không nhầm với điều khiển vô hiệu |
| **Text Primary** | `#1E293B` | Tiêu đề chính, số liệu quan trọng, chữ độ đậm cao |
| **Text Secondary** | `#475569` / `#64748B` | Nhãn form, mô tả trường, văn bản thông thường |
| **Text Muted** | `#94A3B8` | Placeholder, đơn vị đo, ngày giờ mờ |
| **Border / Divider** | `#E2E8F0` / `#D1FAE5` | Đường viền thẻ 1px siêu mảnh, thanh lịch |
| **Bo góc (CornerRadius)** | `14px - 16px` (Card), `8px` (Button/Input) | Bo góc mềm mại toàn bộ các thành phần |
| **Font Family** | `Segoe UI Variable`, `Segoe UI` | Cỡ chữ 20px (Title), 15px (Header), 13px (Body), 11px (Tag) |
| **Icon Style** | Outline mỏng (1.5px - 2px) | Đặt trong hộp nền màu pastel `#ECFDF5` bo tròn |

## Menu hiện tại và danh sách màn hình

Menu rộng gồm cấp 1 là nhóm ngang, cấp 2 là màn trong nhóm; dưới 1240 DIP mở drawer dọc. Tên hiển thị thực tế trong `ShellViewModel`:

| Nhóm cấp 1 | Mã — tên menu cấp 2 |
|---|---|
| Tổng quan | A01 — Tổng quan |
| Tiếp nhận | A03 — Hồ sơ khách hàng; A07 — Đăng ký dịch vụ; A08 — Thanh toán; A09 — Lịch hẹn & lịch sử |
| Chuyên môn | A02 — Khám sàng lọc; A06 — Tiêm & theo dõi |
| Danh mục & kho | A04 — Danh mục; A05 — Kho & lô vắc xin |
| Quản trị | A10 — Ưu đãi; A11 — Báo cáo; A12 — Nhân viên & tài khoản |

Login nằm ngoài menu. A13 Thông tin phiên và A14 Đổi mật khẩu nằm trong menu tài khoản (hiện chỉ phản hồi UI); không phải mục nhóm. Hiện A01 và A02 mở View mới; các mục khác chưa triển khai và chỉ trả phản hồi, không chuyển về giao diện cũ.

| Mã | Tên | Owner (Backend / UI Windows) | Mức ưu tiên | Ghi chú |
|---|---|---|---|---|
| Login | Đăng nhập | Backend: Đạt / UI Windows: Đạt (F01) | **P0** | Sẽ nối API Login thật sau |
| A01 | Tổng quan / Dashboard | Backend: Đạt / UI Windows: Đạt | P0 | KPI cards, bộ lọc Từ ngày/Đến ngày, lịch hẹn và trạng thái xử lý |
| A02 | Khám sàng lọc & Chỉ định | Backend: Đạt / UI Windows: Đạt (D03) | **P0** | Hàng đợi, form khám, quyết định chỉ định/hoãn |
| A03 | Khách hàng & Người được tiêm | Backend: Bảo / UI Windows: Đạt (B01) | P0 | Tabs KH/Người tiêm, CRUD, tìm kiếm |
| A04 | Danh mục tiêm chủng | Backend: Đạt / UI Windows: Đạt (D01) | **P0** | Tabs Loại tiêm/Vắc xin/Gói, CRUD |
| A05 | Kho & lô vắc xin | Backend: Đạt / UI Windows: Đạt (D02) | **P0** | Bảng lô, nhập lô, biến động kho |
| A06 | Thực hiện tiêm chủng | Backend: Đạt / UI Windows: Đạt (D03) | **P0** | Quy trình tiêm, ghi nhận mũi tiêm |
| A07 | Đăng ký dịch vụ | Backend: Bảo / UI Windows: Đạt (B02) | P0 | 5 bước tiếp nhận, danh sách đăng ký |
| A08 | Thanh toán dịch vụ | Backend: Bảo / UI Windows: Đạt (B03) | P0 | Phiếu thanh toán, đặt cọc/trả góp |
| A09 | Lịch tiêm & Nhắc lịch | Backend: Quý / UI Windows: Đạt (Q01) | P0 | Lịch hẹn trong ngày, gửi nhắc |
| A10 | Khuyến mãi & Ưu đãi | Backend: Bảo / UI Windows: Đạt (B04) | P1 | Chương trình, điều kiện |
| A11 | Thống kê & Báo cáo | Backend: Đạt / UI Windows: Đạt (D04) | P1 | Chỉ số, lọc thời gian, xuất báo cáo |
| A12 | Tài khoản & Phân quyền | Backend: Đạt / UI Windows: Đạt (F01) | **P0** | Tabs Nhân viên/Tài khoản/Quyền bổ sung; dropdown đúng một chức danh |
| A14 | Đổi mật khẩu | Backend: Đạt / UI Windows: Đạt (F01) | P1 | Form đổi mật khẩu |

> [!NOTE]
> Screen Spec ghi A13 là Thông tin phiên hiện tại. Giữ yêu cầu chức năng này trong menu tài khoản; việc có trang riêng hay chỉ panel phiên cần chốt khi triển khai. Login vẫn là cửa sổ riêng.

### Chức danh và quyền bổ sung — Phương án 1 ĐÃ DUYỆT 2026-10-22

Giữ sáu `ChucDanh`: `BacSi`, `DieuDuong`, `ThuNgan`, `LeTan`, `ThuKho`, `Admin`; mỗi nhân viên đúng một chức danh. Người dùng đã chọn giữ danh sách này và ánh xạ nhiệm vụ thành quyền bổ sung:

**Chức danh = quyền mặc định; mã quyền = phần cấp thêm.** Quyền hiệu lực là hợp của quyền mặc định theo chức danh và mã quyền được cấp trong phạm vi tác vụ/dữ liệu đã duyệt. Ô không điều kiện quyền mô tả quyền mặc định; ô có chú thích cần quyền hiệu lực tương ứng. A04 ánh xạ mặc định `BacSi` → `QuanLyDanhMucChuyenMon`, `Admin` → `QuanLyDanhMucGia`. Mã chỉ xem không thu hồi quyền ghi đã có.

**Admin kỹ thuật được seed đủ 12 mã quyền, gồm `QuanLyTaiKhoan` và cả hai quyền A04.** Đây là tài khoản kỹ thuật riêng, không phải tài khoản Giám đốc hoặc login mô phỏng `admin / 123`. Không coi tên tài khoản admin kỹ thuật là quyền ghi lâm sàng. Login là màn công khai, không nằm trong ma trận.

| Tên nhiệm vụ được thay thế | ChucDanh | Quyền bổ sung |
|---|---|---|
| Điều phối viên | `LeTan` | `XemTongQuan` |
| CSKH | `LeTan` | `QuanLyLichHen`, `GuiNhacLich` |
| Marketing | `LeTan` | `QuanLyKhuyenMai` |
| Kế toán trưởng | `ThuNgan` | `XemBaoCao`, `XemThanhToan` |
| Quản lý / Giám đốc | `Admin` | `XemBaoCao`, `XemTongQuan` |
| Bác sĩ trưởng ca | `BacSi` | `XemTongQuan` |

A12 dùng tab **Quyền bổ sung** thay tab Vai trò; các tab còn lại là Nhân viên và Tài khoản. `Admin` không mặc định có mọi quyền. **Giám đốc không được có quyền quản lý tài khoản**, không được cấp `QuanLyTaiKhoan`, không truy cập/quản trị A12, tạo/khóa tài khoản, đặt lại mật khẩu cho người khác hoặc gán/thu hồi quyền. A14 vẫn cho đổi mật khẩu chính mình. Cách nhận diện tài khoản Giám đốc và ngăn cấp nhầm quyền ở backend cần owner Identity thiết kế/review; không coi dữ liệu `BoPhan` hoặc tên hiển thị là cơ chế bảo vệ hiện có.

#### Danh sách mã quyền (MaQuyen) đã duyệt — cần seed ở backend

`QuyenHan.MaQuyen` hiện là `int`; tên dưới đây là mã quyền logic ĐÃ DUYỆT. Backend cần seed danh mục `QuyenHan`, ánh xạ tên logic với ID số ổn định và tên/mô tả (quy ước lưu trong `TenQuyen` cần owner làm rõ khi triển khai), rồi gán qua `TaiKhoan_QuyenHan`. Seed đủ quyền cho admin kỹ thuật, tài khoản khác nhận quyền mặc định và phần cấp thêm; không seed toàn quyền cho mọi tài khoản. Chưa sửa schema, seed hoặc thực thi auth trong lượt tài liệu này.

| Mã quyền logic | Phạm vi | Trạng thái |
|---|---|---|
| `XemTongQuan` | Đọc A01 | ĐÃ DUYỆT |
| `QuanLyLichHen` | Xem/tạo/sửa/hủy lịch A09 | ĐÃ DUYỆT |
| `GuiNhacLich` | Gửi nhắc A09, cần quyền truy cập lịch tương ứng | ĐÃ DUYỆT |
| `QuanLyKhuyenMai` | Xem/tạo/sửa chương trình A10 | ĐÃ DUYỆT |
| `XemBaoCao` | Xem/xuất A11 trong cùng phạm vi | ĐÃ DUYỆT |
| `XemThanhToan` | Chỉ xem A08, không ghi giao dịch | ĐÃ DUYỆT |
| `GhiThanhToan` | Bao gồm xem và ghi A08; không cần thêm XemThanhToan để đọc | ĐÃ DUYỆT |
| `QuanLyTaiKhoan` | A12, chỉ `Admin` được cấp; seed cho admin kỹ thuật, cấm Giám đốc | ĐÃ DUYỆT |
| `QuanLyDanhMucChuyenMon` | A04: liều lượng, thể tích, phản ứng sau tiêm, bệnh phòng ngừa, loại vắc xin, loại tiêm chủng, thành phần gói; gồm xem | ĐÃ DUYỆT; mặc định `BacSi` |
| `QuanLyDanhMucGia` | A04: giá, giá gói, trạng thái kinh doanh, thêm/ngừng vắc xin; gồm xem | ĐÃ DUYỆT; mặc định `Admin` |
| `XemKho` | Đọc A05 | ĐÃ DUYỆT |
| `XemHoSo` | Đọc A03 | ĐÃ DUYỆT |

Ma trận truy cập tại [Screen Spec — Ma trận truy cập](D:/Projects/KLCN-QuanLyTiemChung/docs/desktop-screen-specs.md#ma-trận-truy-cập) có **13 màn × 6 chức danh, toàn bộ 78 ô ĐÃ DUYỆT**; Login công khai đã bỏ khỏi bảng. A13 được quản lý riêng theo thông tin phiên. Ghi chú 5: `GhiThanhToan` gồm xem và ghi; `XemThanhToan` chỉ xem, không thu hồi quyền ghi từ nguồn khác. Kế toán trưởng nhận `ThuNgan` + `XemBaoCao`, `XemThanhToan`, không tự được cấp `GhiThanhToan` từ tên nhiệm vụ. Phê duyệt tài liệu chưa đồng nghĩa đã triển khai code/API/seed.

#### Quyền dữ liệu theo phạm vi

A02, A06, A07, A08 dùng endpoint đọc hẹp theo tác vụ, không cấp quyền xem màn nguồn. A02 tra phiếu đăng ký/hồ sơ người tiêm của lượt khám và danh mục + tình trạng tồn; A06 đọc hồ sơ/chỉ định của lượt tiêm và lô còn hạn; A07 đọc danh mục + tình trạng tồn phục vụ đăng ký; A08 tra phiếu đăng ký, dịch vụ/số tiền cần thu và hồ sơ tối thiểu của giao dịch. Chi tiết phạm vi tại [Screen Spec](D:/Projects/KLCN-QuanLyTiemChung/docs/desktop-screen-specs.md#quyền-dữ-liệu-theo-phạm-vi). Đây là yêu cầu endpoint/DTO chưa triển khai, không tự đặt route trong lượt này.

UI chỉ ẩn/khóa theo quyền cho tiện sử dụng; **API phải kiểm tra quyền ở mọi endpoint ghi**, cả nhóm trường A04 và phạm vi bản ghi. Endpoint đọc hẹp cũng xác thực, kiểm tra quyền tác vụ và đối tượng; không cho đổi ID để đọc ngoài phạm vi. Tồn/hạn lô/số tiền được kiểm tra lại ở API khi ghi. Thẻ chuyển hướng nhanh A01 làm mờ và vô hiệu khi thiếu quyền màn đích, kể cả thao tác bàn phím.

A04 chỉ sửa nhóm trường của quyền hiệu lực: `BacSi` với `QuanLyDanhMucChuyenMon`, `Admin` với `QuanLyDanhMucGia`; payload chạm cả hai nhóm cần cả hai quyền. Những trường ngoài nhóm đã liệt kê cần làm rõ ở contract khi triển khai. Admin kỹ thuật có cả hai quyền do seed đầy đủ; Giám đốc không được có `QuanLyTaiKhoan`.

## Checklist triển khai

**Chỉ định hiện tại:** chưa làm phần kiểm tra chức danh/quyền UI, chờ MainViewModel có dữ liệu quyền (`ShellViewModel` là lớp shell hiện tại). Khi triển khai từng ViewModel, để sẵn thuộc tính `CanXxx: bool` **mặc định `true`**, kèm `TODO: nối quyền từ MainViewModel khi có dữ liệu chức danh/quyền`. Chỉ tạo property cần cho thao tác thực tế. Chưa tính quyền từ chức danh, chưa lọc menu hoặc khóa thẻ A01 theo quyền ở giai đoạn này. Validation, chọn bản ghi, trạng thái đang tải/lưu và ngăn gửi trùng vẫn kiểm tra độc lập. Các mục kiểm tra quyền dưới đây là bước tích hợp sau, không phải điều kiện hoàn tất UI Sample hiện tại. Ma trận đã duyệt và trách nhiệm kiểm tra API vẫn giữ nguyên. Lượt này chỉ sửa tài liệu, chưa thêm CanXxx vào code.

### Phase 0: Dọn nền & Thiết lập kiến trúc
- [ ] **0.1** Đối chiếu cấu trúc đang dùng (`ViewsV2/`, `ViewModels/`, `ViewModels/Samples/`, `Themes/`); chỉ thêm thư mục khi màn mới thực sự cần.
- [ ] **0.2** Viết `ViewModelBase.cs` (INotifyPropertyChanged) và `RelayCommand.cs` (ICommand)
- [ ] **0.3** Rà bộ `Themes/Colors.xaml`, `Typography.xaml`, `Controls.xaml`, `Cards.xaml` và cập nhật token theo Design Spec khi được giao sửa code.
- [ ] **0.4** Sửa `App.xaml` — merge resource dictionaries, cấu hình DI (nếu dùng `Microsoft.Extensions.DependencyInjection`)
- [ ] **0.5** Sửa `App.xaml.cs` — khởi tạo ServiceProvider, override OnStartup hiện LoginWindow trước
- [ ] **0.6** Giữ `MainWindow` cũ tách khỏi shell `ViewsV2/MainWindow`; quyết định dọn file kế thừa sau khi kiểm tra phụ thuộc.
- [ ] **0.7** Chuyển logic cần thiết từ `MainWindow.Api.cs` theo từng màn khi tích hợp, tránh xóa trước khi kiểm tra.
- [ ] **0.8** Build thử — project phải compile thành công với shell trống

### Phase 1: Shell & Navigation
- [x] **1.1** Đã có `ViewsV2/MainWindow.xaml` — menu ngang hai cấp + `ContentControl`, drawer dọc khi hẹp.
- [x] **1.2** Đã có `ShellViewModel.cs` — navigation state và user info mô phỏng.
- [ ] **1.2a** MainViewModel nhận chức danh + quyền, chỉ hiện mục menu được phép. Trong cấu trúc hiện tại, trách nhiệm này nằm ở `ShellViewModel`; không đổi tên lớp chỉ để khớp tên trong Plan. Tính quyền hiệu lực từ quyền mặc định và phần cấp thêm theo ma trận đã duyệt; UI ẩn/khóa chỉ phục vụ thao tác, API kiểm tra quyền thật.
- [ ] **1.2b** Chuẩn bị CanXxx trong từng ViewModel khi dựng màn: bool mặc định true, kèm TODO nối MainViewModel. Mục 1.2a và kiểm tra phân quyền A01/A04/A12 chờ MainViewModel có dữ liệu quyền; không triển khai kiểm tra chức danh/quyền trước thời điểm đó.
- [ ] **1.3** Viết `LoginWindow.xaml` + `LoginViewModel.cs` — XAML đẹp, binding 2 chiều, command đăng nhập
- [x] **1.4** Luồng hiện tại: App → `DemoLoginWindow` cục bộ → `ViewsV2/MainWindow` → menu ngang hai cấp.
- [ ] **1.5** Kiểm tra lại Login end-to-end, menu rộng/hẹp và từng mục khi hoàn thiện shell.
- [ ] **1.6** Chốt commit theo thay đổi thực tế sau kiểm tra; không gắn nhãn Login mới khi chưa triển khai.

### Phase 2: Các trang Đạt sở hữu (module chính)
- [ ] **2.1** `OverviewView` (A01, đã có bản Sample) — bộ lọc khoảng ngày `TuNgay`/`DenNgay` thay `NgayLamViec` đơn; thẻ chuyển hướng nhanh làm mờ/vô hiệu khi thiếu quyền màn đích; đối chiếu Screen Spec + kiểm tra trường hợp biên + chụp trạng thái lỗi trước khi hoàn tất.
- [ ] **2.2** A04 Danh mục — Tabs Loại tiêm/Vắc xin/Gói; tách nhóm trường theo `QuanLyDanhMucChuyenMon` và `QuanLyDanhMucGia`, API kiểm tra từng nhóm payload; đối chiếu Screen Spec + kiểm tra trường hợp biên + chụp trạng thái lỗi.
- [ ] **2.3** A05 Kho & lô vắc xin — bảng lô, nhập lô, biến động; đối chiếu Screen Spec + kiểm tra trường hợp biên + chụp trạng thái lỗi.
- [ ] **2.4** `ScreeningView` (A02, đã có bản Sample) — hàng đợi, form khám và chỉ định; đối chiếu Screen Spec + kiểm tra trường hợp biên + chụp trạng thái lỗi trước khi hoàn tất.
- [ ] **2.5** A06 Tiêm & theo dõi — ghi nhận mũi tiêm; đối chiếu Screen Spec + kiểm tra trường hợp biên + chụp trạng thái lỗi.
- [ ] **2.6** A12 Nhân viên & tài khoản — tab Nhân viên/Tài khoản/Quyền bổ sung, đúng sáu chức danh; đối chiếu Screen Spec + kiểm tra trường hợp biên + chụp trạng thái lỗi. Kiểm tra `Admin` thiếu `QuanLyTaiKhoan` và Giám đốc đều không được truy cập/quản trị; backend kiểm tra quyền, không chỉ ẩn nút UI.
- [ ] **2.7** A11 Báo cáo — chỉ số, lọc thời gian, xuất; đối chiếu Screen Spec + kiểm tra trường hợp biên + chụp trạng thái lỗi.
- [ ] **2.8** Build + chạy toàn bộ 7 trang Đạt sở hữu — kiểm tra navigation, layout, responsive
- [ ] **2.9** Commit: `feat(windows): implement core pages (dashboard, catalog, inventory, screening, vaccination, accounts, reports)`

### Phase 3: Đạt dựng UI cho các module API của Bảo và Quý
- [ ] **3.1** A03 Hồ sơ khách hàng — khách hàng/người tiêm; đối chiếu Screen Spec + kiểm tra trường hợp biên + chụp trạng thái lỗi.
- [ ] **3.2** A07 Đăng ký dịch vụ — danh sách/form; đối chiếu Screen Spec + kiểm tra trường hợp biên + chụp trạng thái lỗi.
- [ ] **3.3** A08 Thanh toán — danh sách phiếu/trạng thái; đối chiếu Screen Spec + kiểm tra trường hợp biên + chụp trạng thái lỗi.
- [ ] **3.4** A09 Lịch hẹn & lịch sử — lịch/nhắc lịch; đối chiếu Screen Spec + kiểm tra trường hợp biên + chụp trạng thái lỗi.
- [ ] **3.5** A10 Ưu đãi — danh sách chương trình/form; đối chiếu Screen Spec + kiểm tra trường hợp biên + chụp trạng thái lỗi.
- [ ] **3.6** Build + kiểm tra điều hướng A01–A12, phân biệt View đã triển khai và mục chỉ có phản hồi.
- [ ] **3.7** Commit: `feat(windows): add placeholder pages for team members (customer, registration, payment, schedule, promotion)`

### Phase 4: Hoàn thiện & Polish
- [ ] **4.1** A14 Đổi mật khẩu — đối chiếu Screen Spec + kiểm tra trường hợp biên + chụp trạng thái lỗi; quyết định dialog/trang khi triển khai.
- [ ] **4.1a** A13 Thông tin phiên — đối chiếu Screen Spec + kiểm tra trường hợp biên + chụp trạng thái lỗi; quyết định panel/trang khi triển khai.
- [ ] **4.1b** Login — đối chiếu Screen Spec + kiểm tra trường hợp biên + chụp trạng thái lỗi khi thay Login mô phỏng.
- [x] **4.2** `OverviewViewModel` đã gọi `TiemChungApiClient.IsHealthyAsync` cho nút Kiểm tra API; kiểm tra lại lỗi/kết nối khi mở rộng.
- [ ] **4.3** Responsive: kiểm tra ở kích thước cửa sổ 980×620 (min) và 1920×1080 (full HD)
- [ ] **4.4** Accessibility: Tab order, keyboard navigation, tooltips
- [ ] **4.5** Giữ dữ liệu Sample tách tại `ViewModels/Samples/`, có cờ bật/tắt và nhãn nguồn rõ ràng. Tắt Sample khi kiểm tra luồng API thật; khi chưa có API, hiển thị trạng thái chưa có nguồn thay vì coi Sample là dữ liệu thật.
- [ ] **4.6** Cập nhật `apps/windows/README.md`
- [ ] **4.7** Chạy `dotnet build --configuration Release` — 0 warning, 0 error
- [ ] **4.8** Chạy thử trên máy — chụp ảnh kiểm tra LoginWindow + Dashboard + ít nhất 3 trang khác
- [ ] **4.9** Cập nhật `docs/status.md` — ghi kết quả kiểm chứng
- [ ] **4.10** Commit cuối: `feat(windows): complete UI redesign with MVVM architecture`

## Quy tắc khi triển khai

1. **Mỗi lần chỉ làm 1-2 mục** trong checklist, build kiểm tra rồi mới tiếp.
2. **Không viết logic nghiệp vụ trong code-behind** — code-behind chỉ có `InitializeComponent()` và tối đa là wire ViewModel nếu không dùng DI.
3. **DataContext = ViewModel** — binding mọi thứ qua XAML.
4. **Dữ liệu Sample (nếu cần)** đặt tại `ViewModels/Samples/`, có cờ bật/tắt và nhãn nguồn; không đặt trong XAML hay code-behind.
5. **Không nối SQL trực tiếp** — mọi dữ liệu qua `TiemChungApiClient`.
6. **Giữ file nhỏ** — mỗi Page XAML hướng tới dưới 200 dòng, mỗi ViewModel dưới 150 dòng. Khi Page vượt 200 dòng do nhiều vùng chức năng, tách `UserControl` con có trách nhiệm rõ ràng thay vì cắt bớt trường hoặc hành vi trong Screen Spec.
7. **Commit thường xuyên** theo từng Phase hoặc nhóm feature.

## Cách khôi phục context

Khi bắt đầu phiên mới hoặc sau `/compact`:
1. Đọc file này (`windows-ui-redesign-plan.md`)
2. Xem checklist — tìm mục `[x]` cuối cùng đã hoàn thành
3. Đọc file code đang dở (nếu có ghi chú ở checklist)
4. Tiếp tục từ mục tiếp theo chưa `[x]`

## Lịch sử cập nhật

| Ngày | Thay đổi |
|---|---|
| 2026-10-22 | Đưa Plan vào `docs/` để commit cùng Screen Spec/Design Spec; chuẩn hóa liên kết tài liệu, tên A05 “Kho & lô vắc xin”, ghi rõ `_Legacy/` mới là thư mục dự kiến và quy tắc tách UserControl con khi Page quá 200 dòng. Đồng bộ bộ lọc khoảng ngày A01 và màu/component trạng thái theo Design Spec. |
| 2026-10-22 | Theo chỉ định mới, hoãn kiểm tra chức danh/quyền UI đến khi MainViewModel có dữ liệu quyền. Khi triển khai ViewModel, để sẵn CanXxx bool mặc định true với TODO nối quyền; validation/trạng thái thao tác giữ độc lập. Ghi rõ lọc menu và kiểm tra quyền A01/A04/A12 thuộc bước tích hợp sau; chưa sửa code hoặc thay trạng thái ma trận đã duyệt. |
| 2026-10-21 | Tạo kế hoạch ban đầu, phân tích giao diện cũ, thiết kế kiến trúc MVVM, checklist 4 phase |
| 2026-10-22 | Đối chiếu hiện trạng ViewsV2/ViewModels/Themes và menu ngang hai cấp; cập nhật tên menu, màu tương phản/cảnh báo/trạng thái/vô hiệu, Phase 4.5 và bước QA từng màn; nêu 5 vai trò lệch ChucDanh cùng 2 phương án chưa chọn. |
| 2026-10-22 | Theo yêu cầu mới, giữ sáu ChucDanh và áp dụng bảng ánh xạ nhiệm vụ sang chức danh + quyền bổ sung; thay tab Vai trò bằng Quyền bổ sung. Liệt kê 11 mã quyền logic cần seed backend, phân biệt MaQuyen int; cấm Giám đốc quản lý tài khoản và không mặc định Admin toàn quyền. Rà Vai trò đủ 14 màn trong Screen Spec, thêm ma trận 14 × 6 đánh dấu mọi ô là đề xuất chờ duyệt. |
| 2026-10-22 | Người dùng duyệt Phương án 1 và toàn bộ 78 ô ma trận 13 màn, bỏ Login công khai. Chốt chức danh là quyền mặc định, mã quyền là phần cấp thêm; admin kỹ thuật seed đủ quyền, Giám đốc không có QuanLyTaiKhoan. Bổ sung endpoint đọc hẹp A02/A06/A07/A08, thẻ chuyển hướng A01, ghi chú quyền thanh toán và API kiểm tra mọi endpoint ghi; Phase 1 thêm MainViewModel/ShellViewModel nhận chức danh + quyền và chỉ hiện menu được phép. Tách A04 theo QuanLyDanhMucChuyenMon/QuanLyDanhMucGia, cập nhật danh sách seed thành 12 mã quyền. |
| 2026-10-22 | Chốt Đạt làm toàn bộ UI Windows, Bảo/Quý phụ trách API module tương ứng; thêm quy trình bắt buộc `docs/ui-workflow.md` và làm rõ cột Owner theo Backend/UI. |
