# SCREEN SPECIFICATION — HỆ THỐNG QUẢN LÝ TIÊM CHỦNG (WPF DESKTOP)
> **Phiên bản**: 1.2 (ma trận và quy tắc quyền đã duyệt ngày 2026-10-22; chưa triển khai code)
> **Căn cứ dữ liệu**: `TiemChung.Domain.Entities`, `RegistrationRules`, `CatalogContracts`, `TiemChungApiClient`.
> **Nguyên tắc**: KHÔNG kế thừa bố cục, vị trí hay hình thức từ XAML cũ. Chỉ đặc tả mục đích, dữ liệu, hành vi, trạng thái và kiểm tra ràng buộc.

---

## 1. MÀN HÌNH ĐĂNG NHẬP (AUTH / LOGIN)

### 1.1. Mục đích & Vai trò
* **Mục đích**: Xác thực định danh nhân viên y tế / quản trị trước khi cấp quyền truy cập hệ thống; thiết lập phiên làm việc và ngữ cảnh chức danh.
* **Vai trò**: Login là màn công khai, không yêu cầu chức danh/quyền để mở biểu mẫu và không nằm trong ma trận truy cập. Đăng nhập thành công mới thiết lập phiên của `BacSi`, `DieuDuong`, `ThuNgan`, `LeTan`, `ThuKho` hoặc `Admin`.

### 1.2. Dữ liệu hiển thị & Nhập liệu
| Tên trường | Kiểu dữ liệu | Hướng | Bắt buộc | Mô tả |
|---|---|---|:---:|---|
| `TenDangNhap` | `string` | Nhập | Có | Tên tài khoản định danh nhân viên (`TaiKhoan.TenDangNhap`) |
| `MatKhau` | `string / SecureString` | Nhập | Có | Mật khẩu chưa băm (xử lý băm tại backend) |
| `ThongBaoLoi` | `string` | Hiển thị | Không | Thông điệp phản hồi khi sai thông tin hoặc tài khoản bị khóa |
| `DangXuLy` | `bool` | Hiển thị | Có | Cờ trạng thái trong lúc gọi API xác thực |

### 1.3. Commands & Điều kiện bật/tắt
* **`LoginCommand`**:
  * *Hành động*: Gửi thông tin đăng nhập lên API xác thực, lưu Token/Claims, tải chức danh và quyền.
  * *Điều kiện (`CanExecute`)*: `!string.IsNullOrWhiteSpace(TenDangNhap) && !string.IsNullOrWhiteSpace(MatKhau) && !DangXuLy`.
* **`ThoatCommand`**:
  * *Hành động*: Đóng ứng dụng an toàn.
  * *Điều kiện*: Luôn bật.

### 1.4. Trạng thái đặc biệt
* **Đang xác thực (`DangXuLy = true`)**: Vô hiệu hóa nút đăng nhập và ô nhập liệu, hiển thị chỉ báo tải.
* **Tài khoản bị khóa (`TrangThai == "BiKhoa"`)**: Ngăn truy cập kèm thông báo cụ thể từ hệ thống.
* **Lỗi mạng / API ngoại tuyến**: Hiển thị thông báo không kết nối được dịch vụ xác thực.

### 1.5. Quy tắc kiểm tra dữ liệu (Validation Rules)
* Tên đăng nhập không chứa ký tự khoảng trắng đầu/cuối; độ dài tối thiểu 3 ký tự.
* Mật khẩu không được để trống.

### 1.6. Thuộc tính Domain/ViewModel bị giao diện cũ bỏ quên
* Giao diện cũ hardcode kiểm tra `admin / 123` cục bộ trong code-behind.
* Hoàn toàn bỏ quên: `MatKhauHash`, `TaiKhoan.TrangThai`, `NhanVien.ChucDanh`, `NhanVien.MaNhanVien`, danh sách `QuyenHan` (`MaQuyen`, `TenQuyen`).

---

## 2. MÀN HÌNH A01: TỔNG QUAN (DASHBOARD)

### 2.1. Mục đích & Vai trò
* **Mục đích**: Tổng hợp số liệu vận hành trong ngày, theo dõi lưu lượng bệnh nhân qua từng khâu (chờ khám, chờ nộp tiền, chờ tiêm, theo dõi), và giám sát liveness của API backend.
* **Vai trò**: `Admin`, `LeTan`, `BacSi` với quyền hiệu lực `XemTongQuan` theo bảng ánh xạ và ma trận đã duyệt; các nhiệm vụ Điều phối viên/Bác sĩ trưởng ca nhận mã quyền này bổ sung vào chức danh tương ứng.

### 2.2. Dữ liệu hiển thị & Nhập liệu
| Tên trường | Kiểu dữ liệu | Hướng | Bắt buộc | Mô tả |
|---|---|---|:---:|---|
| `TuNgay` | `DateOnly` | Nhập/Chọn | Có | Từ ngày của khoảng xem tổng quan (mặc định hôm nay) |
| `DenNgay` | `DateOnly` | Nhập/Chọn | Có | Đến ngày của khoảng xem tổng quan (mặc định hôm nay) |
| `TongSoDangKyTrongNgay` | `int` | Hiển thị | Có | Tổng số phiếu đăng ký mới phát sinh trong ngày |
| `TongSoLichHenHomNay` | `int` | Hiển thị | Có | Lịch hẹn có `ThoiGianHen` trong ngày |
| `TongSoMuiTiemHoanThanh`| `int` | Hiển thị | Có | Số mũi tiêm có trạng thái "Đã tiêm" trong ngày |
| `TongDoanhThuTrongNgay` | `decimal` | Hiển thị | Có | Tổng tiền các khoản thanh toán thành công trong ngày |
| `SoLuongChoKham` | `int` | Hiển thị | Có | Lượt có đăng ký nhưng chưa có phiếu khám sàng lọc |
| `SoLuongChoThanhToan` | `int` | Hiển thị | Có | Lượt chưa hoàn tất thanh toán/đặt cọc |
| `SoLuongChoTiem` | `int` | Hiển thị | Có | Lượt đã có chỉ định tiêm nhưng chưa thực hiện mũi tiêm |
| `SoLuongDangTheoDoi` | `int` | Hiển thị | Có | Mũi tiêm đang trong 30 phút theo dõi phản ứng |
| `TrangThaiApiBackend` | `bool` | Hiển thị | Có | Kết quả kiểm tra từ endpoint `/health` |
| `DanhSachLichHenSapToi` | `IReadOnlyList<LichHenTiem>` | Hiển thị | Không | Danh sách các cuộc hẹn dự kiến tiếp theo |

### 2.3. Commands & Điều kiện bật/tắt
* **`LamMoiDuLieuCommand`**:
  * *Hành động*: Tải lại toàn bộ chỉ số từ API.
  * *Điều kiện*: `!DangTai`.
* **`KiemTraKetNoiApiCommand`**:
  * *Hành động*: Gọi endpoint `/health` kiểm tra kết nối dịch vụ.
  * *Điều kiện*: `!DangKiemTraApi`.
* **`ChuyenHuongNhanhCommand(string screenCode)`**:
  * *Hành động*: Chuyển nhanh tới khâu xử lý tương ứng (khám, thu ngân, bàn tiêm).
  * *Điều kiện*: Có quyền tương ứng với phân hệ đó.
  * *Hiển thị*: Thẻ chuyển hướng nhanh làm mờ và vô hiệu khi thiếu quyền màn đích; có giải thích thiếu quyền, không thực hiện điều hướng khi nhấn hoặc dùng bàn phím.

### 2.4. Trạng thái đặc biệt
* **API Offline / Mất kết nối**: Hiển thị cảnh báo mất kết nối dịch vụ backend; chỉ số hiển thị giá trị cảnh báo hoặc dữ liệu cache.
* **Khoảng ngày chưa có dữ liệu (Rỗng)**: Tất cả chỉ số hiển thị bằng 0 kèm thông báo danh sách trống.

### 2.5. Quy tắc kiểm tra dữ liệu
* Bộ lọc **Từ ngày/Đến ngày** cần ngày hợp lệ và `TuNgay <= DenNgay`; mặc định cả hai là hôm nay. API tổng hợp theo khoảng ngày đã chọn; không cộng lại số liệu ngày trên client. Giới hạn khoảng tra cứu cần chốt cùng API khi triển khai.

### 2.6. Thuộc tính Domain/ViewModel bị giao diện cũ bỏ quên
* Giao diện cũ fix cứng các chuỗi `"128"`, `"95"`, `"64"`, `"58.450.000 đ"`, `"18"`, `"07"`, `"12"`, `"09"`.
* Bỏ quên: Sự liên kết dữ liệu thực giữa `DangKyDichVu`, `PhieuKhamSangLoc`, `ChiDinhTiem`, `MuiTiem`, `ThanhToan` và trạng thái kết nối API động.

---

## 3. MÀN HÌNH A02: KHÁM SÀNG LỌC & CHỈ ĐỊNH (CLINICAL SCREENING)

### 3.1. Mục đích & Vai trò
* **Mục đích**: Tiếp nhận người được tiêm, đo lường và ghi nhận chỉ số sinh tồn, rà soát tiền sử dị ứng, đưa ra kết luận đủ điều kiện tiêm hoặc hoãn tiêm, lập chỉ định danh mục vắc xin được tiêm.
* **Vai trò**: `BacSi` khám sàng lọc và chỉ định theo quyền mặc định đã duyệt; đọc dữ liệu liên quan qua endpoint hẹp của lượt khám, không cần quyền xem A03/A04/A07.

### 3.2. Dữ liệu hiển thị & Nhập liệu
| Tên trường | Kiểu dữ liệu | Hướng | Bắt buộc | Mô tả |
|---|---|---|:---:|---|
| `HangDoiKham` | `IReadOnlyList<HangDoiKhamItem>` | Hiển thị | Có | Danh sách người được tiêm chờ khám |
| `MaDangKy` | `int` | Hiển thị | Có | Mã đăng ký dịch vụ của lượt khám |
| `ThongTinNguoiTiem` | `NguoiDuocTiem` | Hiển thị | Có | Họ tên, ngày sinh, giới tính, CCCD |
| `ThongTinKhachHang` | `KhachHang` | Hiển thị | Có | Tên người bảo trợ/người liên hệ, SĐT |
| `DichVuDangKyBanDau`| `IReadOnlyList<string>` | Hiển thị | Có | Tên các vắc xin lẻ / gói đã đăng ký trước |
| `Mach` | `int` | Nhập | Có | Nhịp tim (lần/phút), nằm trong `ChiSoSinhTon` |
| `HuyetApTamThu` | `int` | Nhập | Có | mmHg, nằm trong `ChiSoSinhTon` |
| `HuyetApTamTruong` | `int` | Nhập | Có | mmHg, nằm trong `ChiSoSinhTon` |
| `NhietDo` | `decimal` | Nhập | Có | Thân nhiệt (°C), nằm trong `ChiSoSinhTon` |
| `TienSuDiUng` | `string` | Nhập | Có | Tiền sử phản vệ, dị ứng thức ăn/thuốc (`TienSuBenhLy`) |
| `BenhLyManTinh` | `string` | Nhập | Không | Bệnh nền tim mạch, hen, suy giảm miễn dịch (`TienSuBenhLy`) |
| `KetLuan` | `string` | Chọn | Có | "Đủ điều kiện", "Tạm hoãn", hoặc "Chống chỉ định" |
| `LyDoHoan` | `string` | Nhập | **Có (nếu Hoãn)**| Lý do cụ thể khi kết luận khác "Đủ điều kiện" |
| `DanhSachChiDinh` | `IList<ChiDinhTiemItem>` | Nhập/Chọn | **Có (nếu Đủ ĐK)**| Các vắc xin được phê duyệt tiêm (`MaVacXin`, `GhiChu`) |
| `GhiChuBacSi` | `string` | Nhập | Không | Dặn dò theo dõi hoặc lưu ý đường tiêm |

### 3.3. Commands & Điều kiện bật/tắt
* **`LuuPhieuKhamVaChiDinhCommand`**:
  * *Hành động*: Tạo đồng thời `PhieuKhamSangLoc` và các dòng `ChiDinhTiem`.
  * *Điều kiện*:
    * Người dùng có chức danh Bác sĩ.
    * Đã chọn lượt khám từ hàng đợi.
    * `ChiSoSinhTon` hợp lệ.
    * Nếu `KetLuan == "Tạm hoãn"` hoặc `"Chống chỉ định"`: Bắt buộc có `LyDoHoan` và `DanhSachChiDinh` phải rỗng.
    * Nếu `KetLuan == "Đủ điều kiện"`: Bắt buộc `DanhSachChiDinh.Count > 0`.
* **`LamMoiHangDoiCommand`**:
  * *Hành động*: Tải lại danh sách bệnh nhân chờ khám.
  * *Điều kiện*: `!DangTai`.

### 3.4. Trạng thái đặc biệt
* **Cảnh báo Chống chỉ định / Dị ứng (`TienSuBenhLy` phát hiện từ khóa nguy hiểm)**: Hiển thị cảnh báo trực quan khẩn cấp cho Bác sĩ, yêu cầu xác nhận 2 lần trước khi kết luận.
* **Thân nhiệt sốt cao (`NhietDo >= 38.0°C`)**: Hệ thống tự động gợi ý kết luận "Tạm hoãn".
* **Hàng đợi trống**: Thông báo không có bệnh nhân chờ khám trong ca làm việc.

### 3.5. Quy tắc kiểm tra dữ liệu
* Thân nhiệt nằm trong khoảng sinh lý cho phép: `35.0 <= NhietDo <= 42.0`.
* Mạch: `40 <= Mach <= 200`.
* Huyết áp tâm thu > Huyết áp tâm trương (`HuyetApTamThu > HuyetApTamTruong`).
* Nếu `KetLuan != "Đủ điều kiện"` mà `LyDoHoan` rỗng $\rightarrow$ Chặn lưu dữ liệu.

### 3.6. Thuộc tính Domain/ViewModel bị giao diện cũ bỏ quên
* Giao diện cũ chỉ có một ô ghi chú text note tự do và 3 nút radio button (`Chỉ định tiêm`, `Tạm hoãn`, `Không chỉ định`).
* Hoàn toàn bỏ quên: `ChiSoSinhTon` (Mạch, Thân nhiệt, Huyết áp), trường `TienSuBenhLy`, trường `LyDoHoan`, danh sách thực thể `ChiDinhTiem` (`MaVacXin`, `KetQuaChiDinh`, `GhiChu`, `ThoiGian`), mã bác sĩ lập phiếu `MaNhanVien`.

---

## 4. MÀN HÌNH A03: HỒ SƠ KHÁCH HÀNG & NGƯỜI ĐƯỢC TIÊM (PROFILES)

### 4.1. Mục đích & Vai trò
* **Mục đích**: Tiếp nhận, tìm kiếm, tạo mới và quản lý hồ sơ định danh của Người thanh toán / Người liên hệ (`KhachHang`) và Người thụ hưởng mũi tiêm (`NguoiDuocTiem`), phục vụ tiêm cá nhân hoặc tiêm cho người thân/trẻ em.
* **Vai trò**: `LeTan` quản lý hồ sơ theo quyền mặc định; `Admin` chỉ xem khi có `XemHoSo`. ĐÃ DUYỆT.

### 4.2. Dữ liệu hiển thị & Nhập liệu
| Tên trường | Kiểu dữ liệu | Hướng | Bắt buộc | Mô tả |
|---|---|---|:---:|---|
| **Tab 1: Khách hàng** | | | | |
| `TuKhoaTimKiemKhachHang`| `string` | Nhập | Không | Tìm theo Họ tên, CCCD, hoặc Số điện thoại |
| `KhachHang.HoTen` | `string` | Nhập/Sửa | Có | Họ và tên khách hàng (người bảo trợ) |
| `KhachHang.CCCD` | `string` | Nhập/Sửa | Có | Số CCCD đúng 12 chữ số |
| `KhachHang.SoDienThoai` | `string` | Nhập/Sửa | Có | Số điện thoại nhận SMS/nhắc lịch |
| `KhachHang.Email` | `string` | Nhập/Sửa | Có | Email nhận hóa đơn và thông báo |
| **Tab 2: Người được tiêm** | | | | |
| `NguoiDuocTiem.MaKhachHang` | `int` | Chọn | Có | Khách hàng liên kết / chịu trách nhiệm |
| `NguoiDuocTiem.HoTen` | `string` | Nhập/Sửa | Có | Họ tên người được tiêm |
| `NguoiDuocTiem.CCCD` | `string?` | Nhập/Sửa | **Không** | CCCD (Cho phép null nếu là trẻ em) |
| `NguoiDuocTiem.NgaySinh` | `DateOnly` | Nhập/Sửa | Có | Ngày tháng năm sinh |
| `NguoiDuocTiem.GioiTinh` | `string` | Chọn | Có | "Nam", "Nữ", hoặc "Khác" |
| `NguoiDuocTiem.QuanHe` | `string` | Chọn/Nhập | Có | "Bản thân", "Con", "Bố/Mẹ", "Vợ/Chồng"... |

### 4.3. Commands & Điều kiện bật/tắt
* **`TimKiemKhachHangCommand`**:
  * *Hành động*: Gọi API tìm kiếm theo CCCD / SĐT / Tên.
  * *Điều kiện*: Luôn bật.
* **`LuuKhachHangCommand`**:
  * *Hành động*: Thêm mới hoặc cập nhật thông tin `KhachHang`.
  * *Điều kiện*: Họ tên, CCCD, SĐT, Email hợp lệ và không rỗng.
* **`ThemNguoiDuocTiemCommand`**:
  * *Hành động*: Thêm hồ sơ người được tiêm gắn với khách hàng hiện tại.
  * *Điều kiện*: Đã chọn một khách hàng bảo trợ hợp lệ, Họ tên và Ngày sinh người tiêm không rỗng.

### 4.4. Trạng thái đặc biệt
* **Trẻ em dưới 14 tuổi chưa có CCCD**: Cho phép bỏ trống CCCD, tự động hiển thị tag "Trẻ phụ thuộc - Cần người bảo trợ".
* **Trùng số CCCD khách hàng**: Báo lỗi nghiệp vụ không cho tạo bản ghi trùng lặp.
* **Không tìm thấy kết quả**: Hiển thị trạng thái rỗng và nút hành động nhanh "Tạo hồ sơ mới".

### 4.5. Quy tắc kiểm tra dữ liệu
* CCCD của `KhachHang`: Đúng định dạng 12 chữ số số học.
* Số điện thoại: Định dạng từ 10-11 chữ số hợp lệ tại Việt Nam.
* Email: Đúng cú pháp RFC email.
* Ngày sinh của người được tiêm: `NgaySinh <= DateOnly.FromDateTime(DateTime.Today)`.

### 4.6. Thuộc tính Domain/ViewModel bị giao diện cũ bỏ quên
* Giao diện cũ hiển thị dữ liệu dạng mảng 4 cột text giả định: `("KH-DEMO-001", "0900 000 101", "15/10/2026", "Đang hoạt động")`.
* Bỏ quên: Quy tắc `CCCD` nullable của `NguoiDuocTiem`, kiểu `DateOnly` của `NgaySinh`, trường `Email` của khách hàng, trường `GioiTinh`, và mối quan hệ khóa ngoại 1-N giữa `KhachHang` và danh sách `NguoiDuocTiem`.

---

## 5. MÀN HÌNH A04: DANH MỤC TIÊM CHỦNG (CATALOG)

### 5.1. Mục đích & Vai trò
* **Mục đích**: Quản lý toàn bộ danh mục dịch vụ y tế của trung tâm: Loại vắc xin, Loại tiêm chủng, Bệnh phòng ngừa, Vắc xin đơn lẻ và Gói vắc xin.
* **Vai trò**: `BacSi` có `QuanLyDanhMucChuyenMon`; `Admin` có `QuanLyDanhMucGia`. ĐÃ DUYỆT sửa theo từng nhóm trường, không cấp quyền sửa toàn bộ danh mục từ một mã quyền.

### 5.2. Dữ liệu hiển thị & Nhập liệu
| Tên trường | Kiểu dữ liệu | Hướng | Bắt buộc | Mô tả |
|---|---|---|:---:|---|
| **Vắc xin (`VacXin`)** | | | | |
| `MaVacXin` | `int` | Hiển thị | Có | Khóa chính tự sinh |
| `TenVacXin` | `string` | Nhập/Sửa | Có | Tên thương mại của vắc xin |
| `MaLoaiVacXin` | `int` | Chọn | Có | Khóa ngoại tới `LoaiVacXin` |
| `MaLoaiTiemChung` | `int` | Chọn | Có | Khóa ngoại tới `LoaiTiemChung` |
| `NhaSanXuat` | `string` | Nhập/Sửa | Có | Tên công ty / viện sản xuất |
| `XuatXu` | `string` | Nhập/Sửa | Có | Quốc gia xuất xứ (Việt Nam, Pháp, Bỉ...) |
| `LieuLuong` | `string` | Nhập/Sửa | Có | Mô tả liều lượng tiêm (ví dụ: 0.5ml) |
| `TheTich` | `decimal` | Nhập/Sửa | Có | Thể tích đóng lọ (ml) |
| `PhanUngSauTiem` | `string` | Nhập/Sửa | Có | Các phản ứng thông thường và cảnh báo |
| `Gia` | `decimal` | Nhập/Sửa | Có | Đơn giá bán niêm yết (VNĐ) |
| `TrangThai` | `string` | Chọn | Có | "Đang kinh doanh", "Tạm ngừng", "Hết hàng" |
| `DanhSachMaBenh` | `IList<int>` | Chọn nhiều | Có | Các bệnh phòng ngừa (`VacXin_Benh`) |
| **Gói vắc xin (`GoiVacXin`)**| | | | |
| `TenGoi` | `string` | Nhập/Sửa | Có | Tên gói (ví dụ: Gói sơ sinh 0-6 tháng) |
| `GiaGoi` | `decimal` | Nhập/Sửa | Có | Giá trọn gói ưu đãi |
| `ChiTietGoi` | `IList<GoiVacXin_VacXin>` | Nhập/Sửa | Có | Danh sách vắc xin trong gói và số lượng mũi |

### 5.3. Commands & Điều kiện bật/tắt

**Phân tách trường đã duyệt:** `QuanLyDanhMucChuyenMon` cho sửa liều lượng (`LieuLuong`), thể tích (`TheTich`), phản ứng sau tiêm (`PhanUngSauTiem`), bệnh phòng ngừa (`Benh`, `VacXin_Benh`), loại vắc xin, loại tiêm chủng và thành phần gói (`GoiVacXin_VacXin`). `QuanLyDanhMucGia` cho sửa giá (`Gia`), giá gói (`GiaGoi`), trạng thái kinh doanh và thêm/ngừng vắc xin. `BacSi` được ánh xạ quyền chuyên môn, `Admin` được ánh xạ quyền giá. Một yêu cầu cập nhật nhiều nhóm trường cần đủ các quyền tương ứng; API kiểm tra từng nhóm trường, không nhận toàn bộ payload chỉ vì được mở A04. Tài khoản admin kỹ thuật được seed đầy đủ các mã quyền theo quy ước bên dưới. Các trường ngoài hai nhóm cần được chỉ rõ trong contract khi triển khai, không mặc định cho một trong hai quyền.

* **`LuuVacXinCommand`**:
  * *Hành động*: Thêm mới hoặc cập nhật vắc xin kèm danh sách bệnh liên kết.
  * *Điều kiện*: Tên vắc xin không rỗng, `Gia >= 0`, `TheTich > 0`, đã chọn ít nhất 1 bệnh phòng ngừa.
* **`LuuGoiVacXinCommand`**:
  * *Hành động*: Tạo/cập nhật gói vắc xin kèm chi tiết thành phần mũi tiêm.
  * *Điều kiện*: Tên gói không rỗng, `GiaGoi >= 0`, gói có ít nhất 1 loại vắc xin với số lượng > 0.
* **`LocDanhMucCommand`**:
  * *Hành động*: Lọc danh mục theo loại tiêm, loại vắc xin hoặc trạng thái.
  * *Điều kiện*: Luôn bật.

### 5.4. Trạng thái đặc biệt
* **Vắc xin tạm ngưng kinh doanh**: Hiển thị badge màu xám cảnh báo không cho phép chọn khi tạo đăng ký mới.
* **Danh sách bệnh phòng ngừa rỗng**: Cảnh báo yêu cầu bổ sung thông tin phòng bệnh trước khi lưu.

### 5.5. Quy tắc kiểm tra dữ liệu
* Đơn giá: `Gia >= 0`.
* Giá gói: `GiaGoi >= 0`.
* Thể tích lọ: `TheTich > 0`.
* Số lượng mũi tiêm trong gói: `SoLuong > 0`.

### 5.6. Thuộc tính Domain/ViewModel bị giao diện cũ bỏ quên
* Giao diện cũ chỉ hiển thị tên vắc xin và giá giả lập trong bảng 4 cột.
* Hoàn toàn bỏ quên: `NhaSanXuat`, `XuatXu`, `LieuLuong`, `TheTich`, `PhanUngSauTiem`, bảng liên kết n-n `VacXin_Benh` với thực thể `Benh`, bảng thành phần `GoiVacXin_VacXin` (`SoLuong` mũi của từng `MaVacXin` trong gói).

---

## 6. MÀN HÌNH A05: QUẢN LÝ KHO & LÔ VẮC XIN (INVENTORY)

### 6.1. Mục đích & Vai trò
* **Mục đích**: Quản lý nhập lô vắc xin từ nhà cung cấp, giám sát số lượng tồn kho theo số lô, kiểm soát hạn sử dụng, cảnh báo cận date/hết hàng và ghi nhật ký mọi biến động xuất/nhập kho.
* **Vai trò**: `ThuKho` quản lý kho theo quyền mặc định; `Admin` chỉ xem khi có `XemKho`. ĐÃ DUYỆT. `XemTongQuan` không cấp quyền xem chi tiết kho.

### 6.2. Dữ liệu hiển thị & Nhập liệu
| Tên trường | Kiểu dữ liệu | Hướng | Bắt buộc | Mô tả |
|---|---|---|:---:|---|
| **Lô vắc xin (`LoVacXin`)** | | | | |
| `SoLo` | `string` | Nhập/Hiển thị| Có | Mã số lô sản xuất (Khóa chính) |
| `MaVacXin` | `int` | Chọn | Có | Thuộc vắc xin nào trong danh mục |
| `NgaySanXuat` | `DateOnly` | Nhập | Có | Ngày sản xuất in trên bao bì |
| `HanSuDung` | `DateOnly` | Nhập | Có | Hạn dùng của lô |
| `SoLuongBanDau` | `int` | Nhập | Có | Số lượng nhập kho ban đầu |
| `SoLuongTon` | `int` | Hiển thị | Có | Số lượng còn tồn thực tế |
| `TrangThaiLo` | `string` | Chọn | Có | "Còn hàng", "Sắp hết", "Hết hạn", "Tạm khóa" |
| `RowVersion` | `byte[]` | Hệ thống | Có | Token kiểm soát ghi đồng thời |
| **Biến động kho (`BienDongKho`)** | | | | |
| `MaBienDong` | `int` | Hiển thị | Có | Khóa chính tự sinh |
| `LoaiBienDong` | `string` | Chọn | Có | "Nhập kho", "Xuất tiêm", "Hủy hỏng", "Điều chuyển" |
| `SoLuongBienDong` | `int` | Nhập | Có | Số lượng thay đổi |
| `MaNhanVienThucHien` | `int` | Hệ thống | Có | Nhân viên thao tác xuất/nhập |
| `ThoiDiemBienDong` | `DateTime` | Hệ thống | Có | Thời điểm ghi nhận giao dịch kho |

### 6.3. Commands & Điều kiện bật/tắt
* **`NhapLoMoiCommand`**:
  * *Hành động*: Tạo bản ghi `LoVacXin` mới và tự động sinh bản ghi `BienDongKho` loại "Nhập kho".
  * *Điều kiện*:
    * Người dùng có chức danh Thủ kho.
    * `SoLo` không rỗng và chưa tồn tại.
    * `NgaySanXuat < HanSuDung`.
    * `SoLuongBanDau > 0`.
* **`DieuChinhTonKhoCommand` (Xuất hủy / Hao hụt)**:
  * *Hành động*: Giảm `SoLuongTon` và tạo bản ghi `BienDongKho` tương ứng.
  * *Điều kiện*: `SoLuongXuat > 0 && SoLuongXuat <= SoLuongTon`.

### 6.4. Trạng thái đặc biệt
* **Lô quá hạn sử dụng (`HanSuDung < DateOnly.FromDateTime(DateTime.Today)`)**: Hiển thị trạng thái "Hết hạn", KHÔNG cho phép xuất dùng để tiêm.
* **Lô cận hạn (còn dưới 30 ngày)**: Hiển thị cảnh báo cận date để ưu tiên sử dụng trước (nguyên tắc FEFO: First Expired, First Out).
* **Tồn kho cạn kiệt (`SoLuongTon == 0`)**: Hiển thị nhãn "Hết hàng".
* **Xung đột ghi đồng thời (`DbUpdateConcurrencyException`)**: Báo lỗi dữ liệu tồn kho đã bị thay đổi bởi người khác, tự động tải lại dữ liệu mới nhất.

### 6.5. Quy tắc kiểm tra dữ liệu
* **Ràng buộc thời gian**: `NgaySanXuat < HanSuDung`.
* **Ràng buộc số lượng**: `SoLuongBanDau > 0`, `SoLuongTon >= 0`.
* **Tuyệt đối không để tồn kho âm**: Mọi thao tác trừ kho phải kiểm tra điều kiện `SoLuongTon >= SoLuongXuat` trong Transaction.

### 6.6. Thuộc tính Domain/ViewModel bị giao diện cũ bỏ quên
* Giao diện cũ hiển thị 2 dòng tĩnh `"24 liều · mẫu"` và `"8 liều · mẫu"`.
* Hoàn toàn bỏ quên: Trường `NgaySanXuat`, kiểu `DateOnly` của `HanSuDung`, trường `SoLuongBanDau`, thực thể `BienDongKho` (`MaBienDong`, `LoaiBienDong`, `SoLuong`, `ThoiDiem`), trường kiểm soát tranh chấp `RowVersion`.

---

## 7. MÀN HÌNH A06: THỰC HIỆN TIÊM CHỦNG & THEO DÕI (VACCINATION & AFTERCARE)

### 7.1. Mục đích & Vai trò
* **Mục đích**: Tiếp nhận người đã có chỉ định tiêm và đã thanh toán, đối chiếu số lô vắc xin, thực hiện tiêm và ghi nhận vị trí tiêm; theo dõi phản ứng tại trung tâm trong 30 phút sau tiêm.
* **Vai trò**: `DieuDuong` thực hiện tiêm và theo dõi theo quyền mặc định đã duyệt; đọc hồ sơ lượt tiêm và lô còn hạn qua endpoint hẹp, không cần quyền xem A03/A05.

### 7.2. Dữ liệu hiển thị & Nhập liệu
| Tên trường | Kiểu dữ liệu | Hướng | Bắt buộc | Mô tả |
|---|---|---|:---:|---|
| **Lượt tiêm (`MuiTiem`)** | | | | |
| `MaNguoiDuocTiem` | `int` | Hiển thị | Có | Người được tiêm mũi này |
| `HoTenNguoiTiem` | `string` | Hiển thị | Có | Họ tên để điều dưỡng đối chiếu |
| `ChiDinhHienTai` | `ChiDinhTiem` | Hiển thị | Có | Chỉ định vắc xin từ bác sĩ khám |
| `SoLoChonTiem` | `string` | Chọn | Có | Lô vắc xin lấy từ kho để tiêm (`MuiTiem.SoLo`) |
| `ViTriTiem` | `string` | Chọn/Nhập | Có | "Bắp tay trái", "Bắp tay phải", "Đùi trái"... |
| `ThoiDiemTiem` | `DateTime` | Hệ thống | Có | Thời gian hoàn tất mũi tiêm |
| `TrangThaiMuiTiem` | `string` | Hiển thị | Có | "Đã tiêm", "Hủy tiêm" |
| **Theo dõi sau tiêm (`TheoDoiSauTiem`)** | | | | |
| `ThoiDiemBatDauTheoDoi`| `DateTime` | Hiển thị | Có | Bắt đầu tính giờ 30 phút |
| `ThoiGianConLai` | `TimeSpan` | Hiển thị | Có | Đếm ngược 30 phút theo dõi |
| `TinhTrangPhanUng` | `string` | Nhập/Chọn | Có | "Bình thường", "Sốt nhẹ", "Sưng đỏ tại chỗ", "Khó thở"... |
| `MucDoPhanUng` | `string` | Chọn | Có | "Không có", "Nhẹ", "Trung bình", "Nặng / Cấp cứu" |
| `HuongXuTri` | `string` | Nhập | Có | Biện pháp xử lý của điều dưỡng/bác sĩ |

### 7.3. Commands & Điều kiện bật/tắt
* **`XacNhanTiemCommand`**:
  * *Hành động*: Tạo bản ghi `MuiTiem`, trừ 1 liều tồn kho của `LoVacXin`, chuyển sang trạng thái theo dõi 30 phút.
  * *Điều kiện*:
    * Người dùng là Điều dưỡng.
    * Đã chọn lượt có chỉ định tiêm hợp lệ.
    * Đã chọn `SoLo` còn hạn và tồn kho > 0.
    * `ViTriTiem` không rỗng.
* **`HoanTatTheoDoiCommand`**:
  * *Hành động*: Lưu bản ghi `TheoDoiSauTiem` và cho phép bệnh nhân ra về.
  * *Điều kiện*: Đã ghi nhận `TinhTrangPhanUng`, `MucDoPhanUng` và `HuongXuTri`.

### 7.4. Trạng thái đặc biệt
* **Cảnh báo phản ứng nặng / Phản vệ (`MucDoPhanUng == "Nặng"` hoặc `"Cấp cứu"`)**: Kích hoạt trạng thái cảnh báo y tế khẩn cấp, yêu cầu báo ngay bác sĩ trực cấp cứu.
* **Lô vắc xin đã hết hạn hoặc hết tồn**: Khóa không cho chọn lô đó để tiêm.
* **Đang đếm ngược 30 phút**: Cảnh báo nếu điều dưỡng bấm cho về trước thời gian quy định.

### 7.5. Quy tắc kiểm tra dữ liệu
* Lô vắc xin được chọn phải đúng loại vắc xin mà Bác sĩ đã chỉ định trong `ChiDinhTiem`.
* Không thể ghi nhận tiêm nếu người tiêm chưa có kết luận "Đủ điều kiện" từ phiếu khám sàng lọc.

### 7.6. Thuộc tính Domain/ViewModel bị giao diện cũ bỏ quên
* Giao diện cũ chỉ hiển thị danh sách quy trình 4 bước và nút "Ghi nhận (minh họa)".
* Hoàn toàn bỏ quên: Liên kết thực thể `MuiTiem` với `SoLo` kho thật, trường `ViTriTiem`, toàn bộ thực thể `TheoDoiSauTiem` (`TinhTrang`, `MucDoPhanUng`, `HuongXuTri`, `ThoiDiem`), mã điều dưỡng `MaNhanVien`.

---

## 8. MÀN HÌNH A07: QUẢN LÝ ĐĂNG KÝ DỊCH VỤ (REGISTRATION)

### 8.1. Mục đích & Vai trò
* **Mục đích**: Tiếp nhận yêu cầu tiêm chủng tại quầy; lập phiếu đăng ký kết hợp cả vắc xin lẻ và gói vắc xin; hỗ trợ chuyển vào hàng đợi nếu tạm hết vắc xin.
* **Vai trò**: `LeTan` đăng ký dịch vụ theo quyền mặc định đã duyệt; tra danh mục và tình trạng tồn qua endpoint đọc hẹp, không cần quyền xem A04/A05.

### 8.2. Dữ liệu hiển thị & Nhập liệu
| Tên trường | Kiểu dữ liệu | Hướng | Bắt buộc | Mô tả |
|---|---|---|:---:|---|
| `MaKhachHang` | `int` | Chọn | Có | Khách hàng đăng ký / thanh toán |
| `MaNguoiDuocTiem` | `int` | Chọn | Có | Người trực tiếp tiêm |
| `HinhThucTiem` | `string` | Chọn | Có | "Tại trung tâm" hoặc "Tại nhà" |
| `ThoiGianDuKien` | `DateTime` | Nhập | Có | Ngày giờ dự kiến tiêm |
| `DanhSachVacXinLe` | `IList<ChiTietVacXinLe>` | Nhập/Chọn | *Tối thiểu 1 trong 2* | Danh sách mũi lẻ (`MaVacXin`, `SoLuong`, `Gia`) |
| `DanhSachGoiVacXin` | `IList<ChiTietGoiVacXin>` | Nhập/Chọn | *Tối thiểu 1 trong 2* | Danh sách gói (`MaGoi`, `SoLuongGoi`, `Gia`) |
| `MaKhuyenMai` | `int?` | Chọn | Không | Mã ưu đãi áp dụng nếu có |
| `TongTienTamTinh` | `decimal` | Hiển thị | Có | Tính tự động theo quy tắc `RegistrationRules` |
| `TrangThaiDangKy` | `string` | Hiển thị | Có | "Chờ khám", "Đang khám", "Hoàn tất", "Đã hủy" |

### 8.3. Commands & Điều kiện bật/tắt
* **`TaoDangKyCommand`**:
  * *Hành động*: Tạo bản ghi `DangKyDichVu`, các dòng `ChiTietVacXinLe`, `ChiTietGoiVacXin`, chốt snapshot giá tại thời điểm đăng ký.
  * *Điều kiện*:
    * Đã chọn `KhachHang` và `NguoiDuocTiem`.
    * Phải có **ít nhất 1 mục** trong danh sách lẻ hoặc danh sách gói (`lines.Length > 0`).
    * Số lượng mỗi mục > 0 và đơn giá không âm (`RegistrationRules.CalculateTotal`).
* **`ThemVaoHangDoiCommand`**:
  * *Hành động*: Ghi nhận vào `HangDoiVacXin` khi vắc xin khách cần đang tạm hết trong kho.
  * *Điều kiện*: Vắc xin có số lượng tồn = 0.

### 8.4. Trạng thái đặc biệt
* **Đăng ký không có dịch vụ nào**: Chặn submit, báo lỗi theo quy tắc `RegistrationRules`.
* **Vắc xin đã chọn tạm hết hàng trong kho**: Tự động gợi ý đưa khách vào `HangDoiVacXin`.

### 8.5. Quy tắc kiểm tra dữ liệu
* Căn cứ `RegistrationRules.CalculateTotal`: Bắt buộc tổng số dòng lẻ + gói $\ge 1$; mọi dòng có `Quantity > 0` và `UnitPrice >= 0`.
* `ThoiGianDuKien` phải từ thời điểm hiện tại trở đi.

### 8.6. Thuộc tính Domain/ViewModel bị giao diện cũ bỏ quên
* Giao diện cũ hiển thị danh sách quy trình 5 bước và các dòng tĩnh `"DK-DEMO-001"`.
* Hoàn toàn bỏ quên: Bảng `ChiTietVacXinLe` (`SoLuong`, `GiaTaiThoiDiemDangKy`), bảng `ChiTietGoiVacXin` (`SoLuongGoi`, `GiaTaiThoiDiemDangKy`), liên kết `MaKhuyenMai`, logic `HangDoiVacXin`, thực thi hàm kiểm tra tính tiền `RegistrationRules.CalculateTotal`.

---

## 9. MÀN HÌNH A08: THU NGÂN & THANH TOÁN (PAYMENTS)

### 9.1. Mục đích & Vai trò
* **Mục đích**: Thu tiền dịch vụ tiêm chủng; hỗ trợ thanh toán nhiều lần, đặt cọc giữ vắc xin, hoặc trả góp theo tiến độ; kiểm soát không thu vượt tổng tiền nợ.
* **Vai trò**: `ThuNgan` có `GhiThanhToan` được xem và ghi; chỉ có `XemThanhToan` thì chỉ xem. Mã quyền là phần cấp thêm trên quyền mặc định theo chức danh, xét theo ghi chú 5 của ma trận đã duyệt. Tra phiếu đăng ký/hồ sơ của giao dịch qua endpoint đọc hẹp, không cần quyền mở A03/A07.

### 9.2. Dữ liệu hiển thị & Nhập liệu
| Tên trường | Kiểu dữ liệu | Hướng | Bắt buộc | Mô tả |
|---|---|---|:---:|---|
| `MaDangKy` | `int` | Chọn/Tìm | Có | Phiếu đăng ký cần thanh toán |
| `TongTienPhaiThu` | `decimal` | Hiển thị | Có | Tổng giá trị sau khi đã trừ khuyến mãi |
| `DaThanhToan` | `decimal` | Hiển thị | Có | Tổng các khoản thanh toán thành công trước đó |
| `SoTienConLai` | `decimal` | Hiển thị | Có | Tính theo `RegistrationRules.RemainingAmount` |
| `LichSuCacLanThu` | `IReadOnlyList<ThanhToan>`| Hiển thị | Có | Danh sách các lần đã thanh toán của phiếu |
| `SoTienThanhToanLanNay` | `decimal` | Nhập | Có | Số tiền thu trong giao dịch hiện tại |
| `PhuongThuc` | `string` | Chọn | Có | "Tiền mặt", "Chuyển khoản", "Thẻ ngân hàng" |
| `LoaiThanhToan` | `string` | Chọn | Có | "Đặt cọc", "Trả góp / Một phần", "Toàn bộ" |
| `TrangThaiGiaoDich` | `string` | Chọn | Có | "Thành công", "Đang xử lý", "Thất bại" |

### 9.3. Commands & Điều kiện bật/tắt
* **`XacNhanThanhToanCommand`**:
  * *Hành động*: Tạo bản ghi `ThanhToan` mới, cập nhật lại công nợ còn lại.
  * *Điều kiện*:
    * Người dùng là Thu ngân.
    * `SoTienThanhToanLanNay > 0`.
    * `SoTienThanhToanLanNay <= SoTienConLai` (chống thu vượt theo `RegistrationRules`).
* **`InBienLaiCommand`**:
  * *Hành động*: Xuất phiếu thu / hóa đơn cho lần thanh toán được chọn.
  * *Điều kiện*: Giao dịch có `TrangThai == "Thành công"`.

### 9.4. Trạng thái đặc biệt
* **Đã thanh toán đủ (`SoTienConLai == 0`)**: Hiển thị badge "Đã hoàn tất", khóa ô nhập thu tiền.
* **Đặt cọc giữ chỗ (`LoaiThanhToan == "DatCoc"`)**: Gắn cờ ưu tiên giữ lô vắc xin cho khách.
* **Cố tình nhập thu vượt nợ**: Báo lỗi ngay lập tức tại ViewModel theo quy tắc `RemainingAmount`.

### 9.5. Quy tắc kiểm tra dữ liệu
* Căn cứ `RegistrationRules.RemainingAmount`: Khoản thanh toán phải > 0; tổng các lần thanh toán thành công không được vượt quá `TongTienPhaiThu`.
* Giao dịch thất bại không được tính vào số tiền đã trừ nợ.

### 9.6. Thuộc tính Domain/ViewModel bị giao diện cũ bỏ quên
* Giao diện cũ chỉ có một biến cờ boolean `_demoPaymentPaid` chuyển đổi giữa "Đã thanh toán" và "Chờ thanh toán".
* Hoàn toàn bỏ quên: Bản chất 1 Đăng ký có **nhiều bản ghi `ThanhToan`**, trường `PhuongThuc`, `LoaiThanhToan`, `ThoiDiem`, `SoTien`, quy tắc tính nợ còn lại `RegistrationRules.RemainingAmount`.

---

## 10. MÀN HÌNH A09: QUẢN LÝ LỊCH TIÊM & NHẮC LỊCH (APPOINTMENTS & REMINDERS)

### 10.1. Mục đích & Vai trò
* **Mục đích**: Quản lý lịch hẹn tái chủng cho khách hàng theo đúng phác đồ tiêm; theo dõi trạng thái đến hẹn; gửi tin nhắn nhắc lịch (SMS/Zalo/Email) và ghi nhật ký thông báo.
* **Vai trò**: `LeTan` có `QuanLyLichHen` để quản lý lịch hẹn; gửi nhắc còn cần `GuiNhacLich`.

### 10.2. Dữ liệu hiển thị & Nhập liệu
| Tên trường | Kiểu dữ liệu | Hướng | Bắt buộc | Mô tả |
|---|---|---|:---:|---|
| **Lịch hẹn (`LichHenTiem`)** | | | | |
| `MaNguoiDuocTiem` | `int` | Chọn/Tìm | Có | Người được hẹn tiêm |
| `ThoiGianHen` | `DateTime` | Nhập/Chọn | Có | Ngày giờ hẹn tiêm mũi tiếp theo |
| `DiemTiem` | `string` | Nhập/Chọn | Có | Địa điểm hẹn (Phòng tiêm / Cơ sở) |
| `TrangThaiLichHen` | `string` | Chọn | Có | "Đã xếp lịch", "Đã đến", "Hủy hẹn", "Quá hạn" |
| **Nhắc lịch (`NhacLich`)** | | | | |
| `DanhSachNhacLich` | `IReadOnlyList<NhacLich>` | Hiển thị | Có | Lịch sử các lần gửi tin nhắc cho lịch này |
| `KenhGui` | `string` | Chọn | Có | "SMS", "Zalo ZNS", "Email", "Gọi điện" |
| `ThoiDiemGui` | `DateTime` | Hệ thống | Có | Thời điểm gửi thông báo |
| `TrangThaiGui` | `string` | Hiển thị | Có | "Đã gửi", "Thất bại", "Đang chờ" |

### 10.3. Commands & Điều kiện bật/tắt
* **`TaoLichHenCommand`**:
  * *Hành động*: Tạo lịch hẹn mới cho người được tiêm theo phác đồ bác sĩ dặn.
  * *Điều kiện*: `ThoiGianHen > DateTime.Now`.
* **`GuiNhacLichCommand`**:
  * *Hành động*: Ghi nhận bản ghi `NhacLich` mới và kích hoạt dịch vụ thông báo.
  * *Điều kiện*: Lịch hẹn có trạng thái "Đã xếp lịch" và chưa quá hạn.
* **`DoiLichHenCommand`**:
  * *Hành động*: Cập nhật thời gian hẹn mới khi khách gọi báo dời lịch.
  * *Điều kiện*: Lịch chưa diễn ra.

### 10.4. Trạng thái đặc biệt
* **Lịch hẹn quá hạn (`ThoiGianHen < DateTime.Now && TrangThai == "DaXepLich"`)**: Tự động chuyển hiển thị sang badge "Quá hạn" (dùng màu ngữ nghĩa riêng).
* **Đã gửi nhắc lịch trong vòng 24h qua**: Hiển thị trạng thái "Đã nhắc gần đây" để tránh gửi tin nhắn rác làm phiền khách hàng.

### 10.5. Quy tắc kiểm tra dữ liệu
* `ThoiGianHen` phải nằm trong khung giờ làm việc của trung tâm (ví dụ 07:30 - 17:30).
* Kênh gửi phải thuộc danh mục các kênh hệ thống hỗ trợ.

### 10.6. Thuộc tính Domain/ViewModel bị giao diện cũ bỏ quên
* Giao diện cũ chỉ có 1 nút toggle boolean `_demoReminderSent` và chuỗi string `"08:15 · LH-DEMO-001"`.
* Hoàn toàn bỏ quên: Mối quan hệ giữa `LichHenTiem` và các lần `NhacLich`, trường `KenhGui`, `ThoiDiemGui`, `DiemTiem`.

---

## 11. MÀN HÌNH A10: KHUYẾN MÃI & ƯU ĐÃI (PROMOTIONS)

### 11.1. Mục đích & Vai trò
* **Mục đích**: Thiết lập các chương trình ưu đãi, giảm giá khi đăng ký gói hoặc nhân dịp đặc biệt; kiểm soát thời gian hiệu lực và mức tiền giảm.
* **Vai trò**: `LeTan` hoặc `Admin` có `QuanLyKhuyenMai` theo ma trận ĐÃ DUYỆT. `XemBaoCao` và `XemTongQuan` không cấp quyền khuyến mãi.

### 11.2. Dữ liệu hiển thị & Nhập liệu
| Tên trường | Kiểu dữ liệu | Hướng | Bắt buộc | Mô tả |
|---|---|---|:---:|---|
| `MaKhuyenMai` | `int` | Hiển thị | Có | Khóa chính tự sinh |
| `TenKhuyenMai` | `string` | Nhập/Sửa | Có | Tên chương trình (ví dụ: Ưu đãi Tháng 10) |
| `NoiDung` | `string` | Nhập/Sửa | Có | Điều kiện áp dụng, chi tiết chương trình |
| `ThoiGianBatDau` | `DateTime` | Nhập/Sửa | Có | Ngày giờ bắt đầu áp dụng |
| `ThoiGianKetThuc` | `DateTime` | Nhập/Sửa | Có | Ngày giờ kết thúc chương trình |
| `MucUuDai` | `decimal` | Nhập/Sửa | Có | Số tiền giảm trực tiếp (VNĐ) |

### 11.3. Commands & Điều kiện bật/tắt
* **`LuuKhuyenMaiCommand`**:
  * *Hành động*: Tạo mới hoặc sửa thông tin chương trình khuyến mãi.
  * *Điều kiện*:
    * Tên khuyến mãi không rỗng.
    * `ThoiGianKetThuc > ThoiGianBatDau`.
    * `MucUuDai >= 0`.
* **`XoaKhuyenMaiCommand`**:
  * *Hành động*: Ngừng hoặc xóa chương trình nếu chưa có đăng ký nào áp dụng.
  * *Điều kiện*: Chương trình chưa từng được gán vào `DangKyDichVu`.

### 11.4. Trạng thái đặc biệt
* **Chương trình đã hết hạn (`ThoiGianKetThuc < DateTime.Now`)**: Đánh dấu "Hết hiệu lực", không cho phép áp dụng vào các đăng ký mới.
* **Chương trình đang áp dụng**: Hiển thị badge xanh "Đang hoạt động".

### 11.5. Quy tắc kiểm tra dữ liệu
* `ThoiGianKetThuc > ThoiGianBatDau`.
* Mức ưu đãi `MucUuDai >= 0`.

### 11.6. Thuộc tính Domain/ViewModel bị giao diện cũ bỏ quên
* Giao diện cũ hiển thị 2 dòng tĩnh `"KM-DEMO-01"` và `"KM-DEMO-02"`.
* Bỏ quên: Kiểu `DateTime` thực sự của `ThoiGianBatDau` và `ThoiGianKetThuc`, so khớp hiệu lực với `DangKyDichVu.MaKhuyenMai`.

---

## 12. MÀN HÌNH A11: THỐNG KÊ & BÁO CÁO (REPORTS)

### 12.1. Mục đích & Vai trò
* **Mục đích**: Báo cáo số liệu phục vụ quản lý: doanh thu theo khoảng thời gian, thống kê mũi tiêm theo loại vắc xin, tỷ lệ phản ứng sau tiêm, báo cáo tồn kho cận hạn dùng.
* **Vai trò**: `Admin` hoặc `ThuNgan` có `XemBaoCao`; đây là quyền đọc báo cáo, không cấp quyền sửa giao dịch hay quản lý tài khoản.

### 12.2. Dữ liệu hiển thị & Nhập liệu
| Tên trường | Kiểu dữ liệu | Hướng | Bắt buộc | Mô tả |
|---|---|---|:---:|---|
| `TuNgay` | `DateOnly` | Nhập/Chọn | Có | Mốc bắt đầu kỳ báo cáo |
| `DenNgay` | `DateOnly` | Nhập/Chọn | Có | Mốc kết thúc kỳ báo cáo |
| `BaoCaoDoanhThuTong` | `decimal` | Hiển thị | Có | Tổng tiền thu từ các phiếu hợp lệ trong kỳ |
| `SoLuongMuiTiemTheoLoai`| `IDictionary<string, int>` | Hiển thị | Có | Số mũi đã tiêm gom nhóm theo tên vắc xin |
| `TyLePhanUngSauTiem` | `decimal` | Hiển thị | Có | Phần trăm ca có phản ứng / tổng số mũi tiêm |
| `DanhSachLoSapHetHan` | `IReadOnlyList<LoVacXin>` | Hiển thị | Có | Các lô vắc xin cận date cần giải phóng |

### 12.3. Commands & Điều kiện bật/tắt
* **`TaoBaoCaoCommand`**:
  * *Hành động*: Truy vấn dữ liệu tổng hợp từ backend theo khoảng thời gian.
  * *Điều kiện*: `TuNgay <= DenNgay`.
* **`XuatFileBaoCaoCommand(string format)`**:
  * *Hành động*: Xuất dữ liệu báo cáo ra file Excel / PDF / CSV.
  * *Điều kiện*: Đã có dữ liệu báo cáo được tạo.

### 12.4. Trạng thái đặc biệt
* **Khoảng thời gian không có phát sinh dữ liệu**: Hiển thị bảng rỗng kèm gợi ý chọn khoảng thời gian khác.

### 12.5. Quy tắc kiểm tra dữ liệu
* `TuNgay <= DenNgay`.
* `DenNgay <= DateOnly.FromDateTime(DateTime.Today)` (không thống kê tương lai).

### 12.6. Thuộc tính Domain/ViewModel bị giao diện cũ bỏ quên
* Giao diện cũ fix cứng 3 thẻ: `"42"`, `"36"`, `"28"`.
* Hoàn toàn bỏ quên: Bộ lọc thời gian động, công thức tính toán doanh thu từ `ThanhToan`, tỷ lệ phân loại phản ứng từ `TheoDoiSauTiem`, và lọc cảnh báo hạn từ `LoVacXin.HanSuDung`.

---

## 13. MÀN HÌNH A12: TÀI KHOẢN & PHÂN QUYỀN (IDENTITY & ACCESS CONTROL)

### 13.1. Mục đích & Vai trò
* **Mục đích**: Quản lý danh sách nhân viên (`NhanVien`), tài khoản đăng nhập (`TaiKhoan`), gán chức danh duy nhất và phân quyền hạn chức năng (`QuyenHan`) cho từng tài khoản.
* **Vai trò**: `Admin` có `QuanLyTaiKhoan` theo ma trận ĐÃ DUYỆT; admin kỹ thuật được seed quyền này. Giám đốc không được có `QuanLyTaiKhoan`, dù có chức danh `Admin`.

### 13.2. Dữ liệu hiển thị & Nhập liệu

Các tab: **Nhân viên**, **Tài khoản**, **Quyền bổ sung**. Chức danh là dropdown chọn đúng một trong sáu giá trị; không tạo tab “Vai trò” hoặc danh mục chức danh mới. Tab Quyền bổ sung quản lý các liên kết `TaiKhoan_QuyenHan`, không cho tự nhập mã quyền chưa có trong danh mục backend.

| Tên trường | Kiểu dữ liệu | Hướng | Bắt buộc | Mô tả |
|---|---|---|:---:|---|
| **Nhân viên (`NhanVien`)** | | | | |
| `HoTenNhanVien` | `string` | Nhập/Sửa | Có | Họ tên nhân viên |
| `CCCD` | `string` | Nhập/Sửa | Có | CCCD 12 số |
| `SoDienThoai` | `string` | Nhập/Sửa | Có | SĐT liên hệ nội bộ |
| `Email` | `string` | Nhập/Sửa | Có | Email công vụ |
| `BoPhan` | `string` | Chọn | Có | "Y tế", "Lễ tân", "Kế toán", "Kho", "Ban giám đốc" |
| `ChucDanh` | `string` | Chọn | Có | **Đúng một chức danh**: "BacSi", "DieuDuong", "ThuNgan", "LeTan", "ThuKho", "Admin" |
| `TrangThaiNhanVien` | `string` | Chọn | Có | "Đang làm việc", "Nghỉ việc", "Tạm nghỉ" |
| **Tài khoản (`TaiKhoan`)** | | | | |
| `TenDangNhap` | `string` | Nhập | Có | Tên tài khoản không trùng lặp |
| `TrangThaiTaiKhoan` | `string` | Chọn | Có | "HoatDong", "BiKhoa" |
| `DanhSachQuyen` | `IList<int>` | Chọn nhiều | Có | Các quyền cấp cho tài khoản (`TaiKhoan_QuyenHan`) |

### 13.3. Commands & Điều kiện bật/tắt
* **`TaoTaiKhoanCommand`**:
  * *Hành động*: Tạo nhân viên và tài khoản đăng nhập tương ứng.
  * *Điều kiện*:
    * Người dùng có `ChucDanh == "Admin"` và `QuanLyTaiKhoan`, được API xác nhận; tài khoản Giám đốc bị loại khỏi quyền này.
    * Tên đăng nhập không rỗng và chưa tồn tại.
    * CCCD nhân viên đúng định dạng và chưa đăng ký.
    * Chọn đúng một chức danh.
* **`KhoaTaiKhoanCommand`**:
  * *Hành động*: Đổi trạng thái `TaiKhoan.TrangThai` thành "BiKhoa" (vô hiệu hóa tức thì).
  * *Điều kiện*: Có `ChucDanh == "Admin"` và `QuanLyTaiKhoan`, không thuộc tài khoản Giám đốc; không được tự khóa tài khoản của chính mình.
* **`DatLaiMatKhauCommand`**:
  * *Hành động*: Cấp lại mật khẩu tạm thời cho nhân viên.
  * *Điều kiện*: Có `ChucDanh == "Admin"` và `QuanLyTaiKhoan`, không thuộc tài khoản Giám đốc; API kiểm tra quyền trên mỗi yêu cầu.

* **Gán/thu hồi quyền bổ sung**: Cần `Admin` + `QuanLyTaiKhoan`; không cho tự nâng quyền. Giám đốc không được xem/quản trị A12, tạo/khóa tài khoản, đặt lại mật khẩu cho người khác hoặc gán/thu hồi quyền. Đổi mật khẩu chính mình tại A14 vẫn được phép.

### 13.4. Trạng thái đặc biệt
* **Tài khoản đang bị khóa**: Hiển thị badge đỏ "Đã khóa", nút hành động chuyển thành "Mở khóa".
* **Mỗi nhân viên chỉ có 1 chức danh**: Khóa chọn nhiều chức danh, chỉ cho phép chọn duy nhất 1 radio/dropdown chức danh theo đúng quy tắc dự án.

### 13.5. Quy tắc kiểm tra dữ liệu
* Tên đăng nhập: Tối thiểu 4 ký tự, không dấu, không khoảng trắng.
* CCCD nhân viên: 12 chữ số.
* Một nhân viên chỉ gắn với tối đa một tài khoản (`Index Unique MaNhanVien`).

### 13.6. Thuộc tính Domain/ViewModel bị giao diện cũ bỏ quên
* Giao diện cũ chỉ có một biến toggle boolean `_demoAccountEnabled` và danh sách string tĩnh.
* Hoàn toàn bỏ quên: Bảng `NhanVien` với các trường `BoPhan`, `ChucDanh` (ràng buộc 1 chức danh duy nhất), `CCCD`, `SoDienThoai`, bảng liên kết n-n `TaiKhoan_QuyenHan` với danh mục `QuyenHan` (`MaQuyen`, `TenQuyen`, `MoTa`).

### 13.7. Danh mục quyền bổ sung đã duyệt — cần seed backend

Danh sách 12 mã quyền logic: `XemTongQuan`, `QuanLyLichHen`, `GuiNhacLich`, `QuanLyKhuyenMai`, `XemBaoCao`, `XemThanhToan`, `GhiThanhToan`, `QuanLyTaiKhoan`, `QuanLyDanhMucChuyenMon`, `QuanLyDanhMucGia`, `XemKho`, `XemHoSo`. **Các quyền này cần được seed ở backend**, ánh xạ với `QuyenHan.MaQuyen` kiểu `int`; tài khoản admin kỹ thuật được seed đủ danh sách, gồm `QuanLyTaiKhoan`. Chưa có seed trong lượt này. Phạm vi được mô tả trong [danh sách mã quyền](#danh-sách-mã-quyền-maquyen-đã-duyệt--cần-seed-ở-backend) và [ma trận truy cập](#ma-trận-truy-cập). API phải xác nhận quyền của người cấp và tài khoản nhận; tuyệt đối không cấp `QuanLyTaiKhoan` cho Giám đốc.

---

## 14. MÀN HÌNH A14: ĐỔI MẬT KHẨU CÁ NHÂN (CHANGE PASSWORD)

### 14.1. Mục đích & Vai trò
* **Mục đích**: Cho phép nhân viên đang đăng nhập tự đổi mật khẩu tài khoản của mình nhằm bảo mật định kỳ.
* **Vai trò**: `BacSi`, `DieuDuong`, `ThuNgan`, `LeTan`, `ThuKho`, `Admin` đang có phiên hợp lệ; chỉ đổi mật khẩu của chính mình.

### 14.2. Dữ liệu hiển thị & Nhập liệu
| Tên trường | Kiểu dữ liệu | Hướng | Bắt buộc | Mô tả |
|---|---|---|:---:|---|
| `MatKhauHienTai` | `string` | Nhập | Có | Mật khẩu cũ để kiểm tra |
| `MatKhauMoi` | `string` | Nhập | Có | Mật khẩu mới cần đổi |
| `XacNhanMatKhauMoi` | `string` | Nhập | Có | Nhập lại mật khẩu mới để đối chiếu |
| `ThongBaoKetQua` | `string` | Hiển thị | Không | Thông điệp đổi thành công hoặc lỗi |

### 14.3. Commands & Điều kiện bật/tắt
* **`XacNhanDoiMatKhauCommand`**:
  * *Hành động*: Gửi mật khẩu cũ và mới lên backend để băm và cập nhật vào `TaiKhoan.MatKhauHash`.
  * *Điều kiện*:
    * Cả 3 trường mật khẩu đều không rỗng.
    * `MatKhauMoi == XacNhanMatKhauMoi`.
    * `MatKhauMoi != MatKhauHienTai`.
    * Độ dài `MatKhauMoi >= 6` ký tự.
* **`HuyBoCommand`**:
  * *Hành động*: Xóa trắng các trường nhập liệu.
  * *Điều kiện*: Luôn bật.

### 14.4. Trạng thái đặc biệt
* **Mật khẩu cũ không chính xác**: API trả về lỗi xác thực, ViewModel hiển thị thông báo lỗi cụ thể.

### 14.5. Quy tắc kiểm tra dữ liệu
* Mật khẩu mới phải khác mật khẩu hiện tại.
* Mật khẩu mới và xác nhận mật khẩu mới phải hoàn toàn trùng khớp.
* Tối thiểu 6 ký tự.

### 14.6. Thuộc tính Domain/ViewModel bị giao diện cũ bỏ quên
* Giao diện cũ hiển thị 3 ô password box nhưng khi bấm đổi chỉ hiện toast thông báo: `"Biểu mẫu demo hợp lệ. Mật khẩu đăng nhập không thay đổi."`.
* Hoàn toàn bỏ quên: Xác thực mật khẩu cũ với `MatKhauHash`, cập nhật chuỗi băm mới và cơ chế thu hồi refresh token của phiên.

---

## Quy ước chức danh và quyền bổ sung

### Tiến độ tích hợp quyền vào UI — chờ MainViewModel có quyền

Theo chỉ định mới ngày 2026-10-22, **chưa triển khai kiểm tra chức danh/quyền ở UI**, chờ MainViewModel nhận được dữ liệu quyền (trách nhiệm shell hiện nằm ở `ShellViewModel`). Khi triển khai từng ViewModel, để sẵn thuộc tính `CanXxx` kiểu `bool`, **mặc định `true`**, kèm `TODO: nối quyền từ MainViewModel khi có dữ liệu chức danh/quyền`. Ví dụ tên thuộc tính: `CanView`, `CanEdit`, `CanManageAccounts`, `CanEditCatalogClinical`, `CanEditCatalogPricing`; chỉ thêm thuộc tính tương ứng thao tác thực tế của màn, không coi ví dụ là danh sách bắt buộc.

`CanXxx` ở giai đoạn này là điểm nối UI tạm, chưa tính quyền, chưa lọc menu hoặc làm mờ thẻ A01 theo chức danh. Điều kiện validation, đã chọn bản ghi, đang tải/lưu và ngăn gửi trùng vẫn áp dụng độc lập; `CanXxx = true` không làm nút ghi luôn bật. Ma trận 13 màn đã duyệt và yêu cầu kiểm tra quyền API bên dưới vẫn là đặc tả cho bước tích hợp sau. Lượt này chỉ cập nhật tài liệu, chưa thêm property vào code ViewModel.

Giữ đúng sáu giá trị `ChucDanh`: `BacSi`, `DieuDuong`, `ThuNgan`, `LeTan`, `ThuKho`, `Admin`. Mỗi nhân viên có đúng một chức danh. Bảng sau ghi nhận ánh xạ do người dùng chọn ngày 2026-10-22; các tên ở cột đầu là mô tả nhiệm vụ, không còn là chức danh trong mục Vai trò của màn hình.

**Phương án 1 ĐÃ DUYỆT: chức danh = quyền mặc định; mã quyền = phần cấp thêm.** Quyền hiệu lực là hợp của quyền mặc định theo chức danh và mã quyền được cấp cho tài khoản, trong giới hạn nghiệp vụ/phạm vi dữ liệu. Ô không có điều kiện quyền ở ma trận mô tả quyền mặc định; ô có chú thích mã quyền cần quyền hiệu lực tương ứng. Với A04, ánh xạ mặc định đã duyệt là `BacSi` → `QuanLyDanhMucChuyenMon`, `Admin` → `QuanLyDanhMucGia`. Các mã quyền còn lại trong bảng ánh xạ nhiệm vụ là phần cấp thêm. Mã chỉ xem không thu hồi quyền ghi đã có từ nguồn khác.

**Tài khoản admin kỹ thuật phải được seed đủ 12 mã quyền bên dưới, gồm `QuanLyTaiKhoan`.** Đây là tài khoản kỹ thuật riêng, không đồng nhất với tài khoản Giám đốc hoặc login UI mô phỏng `admin / 123`. Chức danh `Admin` mặc định không cấp `QuanLyTaiKhoan`; quyền này cần gán rõ cho admin kỹ thuật hoặc tài khoản quản trị được phép. Không suy ra quyền ghi lâm sàng chỉ từ tên admin kỹ thuật.

UI chỉ ẩn/khóa theo quyền cho tiện sử dụng. **API phải kiểm tra quyền ở mọi endpoint ghi**, gồm quyền chức năng, nhóm trường và phạm vi bản ghi; không tin actor/quyền gửi từ client. Endpoint đọc hẹp cũng phải xác thực và kiểm tra phạm vi dữ liệu tương ứng.

| Tên nhiệm vụ được thay thế | ChucDanh | Quyền bổ sung theo yêu cầu |
|---|---|---|
| Điều phối viên | `LeTan` | `XemTongQuan` |
| CSKH | `LeTan` | `QuanLyLichHen`, `GuiNhacLich` |
| Marketing | `LeTan` | `QuanLyKhuyenMai` |
| Kế toán trưởng | `ThuNgan` | `XemBaoCao`, `XemThanhToan` |
| Quản lý / Giám đốc | `Admin` | `XemBaoCao`, `XemTongQuan` |
| Bác sĩ trưởng ca | `BacSi` | `XemTongQuan` |

**Giám đốc không được có quyền quản lý tài khoản.** Không suy ra toàn quyền từ `ChucDanh == "Admin"`. Quyền của từng tài khoản phải được API kiểm tra qua `TaiKhoan_QuyenHan`; tài khoản dùng cho nhiệm vụ Giám đốc không được cấp `QuanLyTaiKhoan`. Cách backend nhận diện tài khoản này, kiểm soát việc gán quyền và ngăn cấp nhầm cần owner Identity thiết kế/review; không dùng tên hiển thị hay `BoPhan` làm cơ chế bảo vệ đã được xác nhận. Đây là yêu cầu tài liệu, chưa có thực thi auth/backend.

### Danh sách mã quyền (MaQuyen) đã duyệt — cần seed ở backend

Trong Domain hiện tại, `QuyenHan.MaQuyen` là `int`; các tên dưới đây là **mã quyền logic ĐÃ DUYỆT**, chưa phải giá trị chuỗi có thể ghi vào `MaQuyen`. Backend cần seed các bản ghi `QuyenHan`, ánh xạ từng mã logic với ID số ổn định và tên/mô tả; quy ước lưu tên logic trong `TenQuyen` cần được owner làm rõ khi triển khai. Không đổi schema hoặc gán ID số trong tài liệu này. Seed danh mục và seed đủ quyền cho admin kỹ thuật là yêu cầu đã duyệt, chưa được triển khai; tài khoản khác nhận quyền theo chức danh và phần cấp thêm.

| Mã quyền logic | Phạm vi | Trạng thái |
|---|---|---|
| `XemTongQuan` | Xem A01; không ghi nghiệp vụ | ĐÃ DUYỆT |
| `QuanLyLichHen` | Xem/tạo/sửa/hủy lịch A09; không tự cấp gửi nhắc | ĐÃ DUYỆT |
| `GuiNhacLich` | Gửi nhắc A09 khi có quyền truy cập lịch tương ứng | ĐÃ DUYỆT |
| `QuanLyKhuyenMai` | Xem/tạo/sửa chương trình A10 | ĐÃ DUYỆT |
| `XemBaoCao` | Xem/xuất A11 trong cùng phạm vi dữ liệu | ĐÃ DUYỆT |
| `XemThanhToan` | Chỉ xem A08, không ghi giao dịch | ĐÃ DUYỆT |
| `GhiThanhToan` | Bao gồm xem và ghi A08; không cần cấp thêm `XemThanhToan` để đọc | ĐÃ DUYỆT |
| `QuanLyTaiKhoan` | Quản trị A12 với `Admin`; seed cho admin kỹ thuật, cấm Giám đốc | ĐÃ DUYỆT |
| `QuanLyDanhMucChuyenMon` | A04: liều lượng, thể tích, phản ứng sau tiêm, bệnh phòng ngừa, loại vắc xin, loại tiêm chủng, thành phần gói; gồm xem | ĐÃ DUYỆT; mặc định `BacSi` |
| `QuanLyDanhMucGia` | A04: giá, giá gói, trạng thái kinh doanh, thêm/ngừng vắc xin; gồm xem | ĐÃ DUYỆT; mặc định `Admin` |
| `XemKho` | Chỉ xem A05 | ĐÃ DUYỆT |
| `XemHoSo` | Chỉ xem A03 | ĐÃ DUYỆT |

### Quyền dữ liệu theo phạm vi

A02, A06, A07, A08 dùng **endpoint đọc hẹp** theo tác vụ đang được phép, không cần cấp quyền xem màn nguồn. “Không” ở ma trận nghĩa không được mở/quản trị màn nguồn, không cấm đọc dữ liệu tối thiểu cần cho tác vụ được duyệt. Không tự mở toàn bộ danh sách nguồn vì cần một bản ghi. Đây là yêu cầu thiết kế API chưa triển khai, không khẳng định đã có route/DTO.

| Màn tác vụ | Dữ liệu đọc hẹp | Phạm vi bắt buộc | Không phát sinh quyền màn nguồn |
|---|---|---|---|
| A02 — Khám sàng lọc | Tra phiếu đăng ký, hồ sơ người tiêm của lượt khám, danh mục và tình trạng tồn phục vụ chỉ định | Chỉ lượt khám được phép xử lý, đúng đăng ký/người tiêm và trường cần cho khám/chỉ định | A03, A04, A05, A07 |
| A06 — Tiêm & theo dõi | Hồ sơ người tiêm/chỉ định của lượt tiêm, lô còn hạn và đủ điều kiện sử dụng | Chỉ lượt được phép xử lý và lô ứng với vắc xin chỉ định; API kiểm tra lại hạn/tồn khi ghi | A02, A03, A04, A05, A07 |
| A07 — Đăng ký | Danh mục + tình trạng tồn cho dịch vụ lựa chọn | Chỉ trường cần cho đăng ký; tình trạng tồn không thay thế kiểm tra tồn tại API khi ghi | A04, A05 |
| A08 — Thanh toán | Tra phiếu đăng ký, dịch vụ/số tiền cần thu và thông tin người tiêm tối thiểu của giao dịch | Chỉ đăng ký/giao dịch được phép xem/thu; API xác định lại số tiền, không tin số tiền client | A03, A07 |

API kiểm tra quyền tác vụ và đối tượng trên từng request đọc hẹp, không cho đổi ID tùy ý để đọc ngoài phạm vi. Mọi endpoint ghi vẫn kiểm tra quyền và trạng thái hiện hành; dữ liệu đã đọc trên UI không phải bằng chứng được phép ghi.

### Ma trận truy cập

**Toàn bộ 78 ô ĐÃ DUYỆT ngày 2026-10-22**, thể hiện trực tiếp trong từng ô. Bảng gồm 13 màn: A01–A12 và A14. **Login là màn công khai**, đã bỏ khỏi ma trận; A13 thông tin phiên được Plan quản lý riêng. Phê duyệt tài liệu không có nghĩa code/API/seed đã triển khai.

`Xem` = đọc trong phạm vi được cấp; `Sửa` = gồm Xem và hành động ghi được duyệt, không mặc định cho phép xóa hoặc sửa mọi trường; `Không` = chức danh không có quyền mặc định mở màn. Mã quyền cấp thêm được xét theo các chú thích và phạm vi đã duyệt; không bỏ qua giới hạn chức danh chuyên môn hoặc lệnh cấm Giám đốc có `QuanLyTaiKhoan`. API kiểm tra quyền hiệu lực, điều kiện nghiệp vụ và phạm vi bản ghi. A04 có mức Sửa theo nhóm trường tại chú thích 3.

| Màn hình | BacSi | DieuDuong | ThuNgan | LeTan | ThuKho | Admin |
|---|---|---|---|---|---|---|
| A01 — Tổng quan | Xem (1) — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Xem (1) — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Xem (1) — ĐÃ DUYỆT |
| A02 — Khám sàng lọc & chỉ định | Sửa — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT |
| A03 — Hồ sơ khách hàng & người tiêm | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Sửa — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Xem (2) — ĐÃ DUYỆT |
| A04 — Danh mục | Sửa (3) — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Sửa (3) — ĐÃ DUYỆT |
| A05 — Kho & lô vắc xin | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Sửa — ĐÃ DUYỆT | Xem (4) — ĐÃ DUYỆT |
| A06 — Tiêm & theo dõi | Không — ĐÃ DUYỆT | Sửa — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT |
| A07 — Đăng ký dịch vụ | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Sửa — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT |
| A08 — Thanh toán | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Sửa (5) — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT |
| A09 — Lịch & nhắc lịch | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Sửa (6) — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT |
| A10 — Khuyến mãi & ưu đãi | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Sửa (7) — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Sửa (7) — ĐÃ DUYỆT |
| A11 — Báo cáo | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Xem (8) — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Xem (8) — ĐÃ DUYỆT |
| A12 — Nhân viên, tài khoản & quyền bổ sung | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Không — ĐÃ DUYỆT | Sửa (9) — ĐÃ DUYỆT |
| A14 — Đổi mật khẩu cá nhân | Sửa (10) — ĐÃ DUYỆT | Sửa (10) — ĐÃ DUYỆT | Sửa (10) — ĐÃ DUYỆT | Sửa (10) — ĐÃ DUYỆT | Sửa (10) — ĐÃ DUYỆT | Sửa (10) — ĐÃ DUYỆT |

1. Cần `XemTongQuan`; áp dụng cho `LeTan`, `BacSi`, `Admin` trong bảng ánh xạ.
2. Cần `XemHoSo`.
3. `BacSi` có `QuanLyDanhMucChuyenMon`: sửa liều lượng, thể tích, phản ứng sau tiêm, bệnh phòng ngừa, loại vắc xin, loại tiêm chủng, thành phần gói. `Admin` có `QuanLyDanhMucGia`: sửa giá, giá gói, trạng thái kinh doanh, thêm/ngừng vắc xin. Hai quyền đều bao gồm xem; A04 chỉ Sửa đúng nhóm trường tương ứng. Admin kỹ thuật được seed cả hai; API kiểm tra theo từng nhóm trường trong payload.
4. Cần `XemKho`.
5. **`GhiThanhToan` bao gồm xem và ghi; `XemThanhToan` chỉ xem.** Ô Sửa áp dụng khi quyền hiệu lực có `GhiThanhToan`. Chỉ có `XemThanhToan` thì Xem, thiếu cả hai thì Không. Tài khoản Kế toán trưởng nhận `ThuNgan` + `XemBaoCao`, `XemThanhToan`; không tự cấp `GhiThanhToan` từ tên nhiệm vụ. Quyền cấp thêm chỉ cộng, không dùng `XemThanhToan` để thu hồi một quyền ghi đã có.
6. Cần `QuanLyLichHen`; gửi nhắc cần thêm `GuiNhacLich`.
7. Cần `QuanLyKhuyenMai`; `Admin` có hai quyền xem của nhiệm vụ Quản lý/Giám đốc vẫn **Không** vào A10 nếu chưa được cấp riêng quyền này.
8. Cần `XemBaoCao`; không có quyền sửa giao dịch từ màn báo cáo.
9. Cần `QuanLyTaiKhoan`; **Giám đốc luôn Không ở A12**, không được cấp quyền này dù cùng chức danh `Admin`. Các tài khoản `Admin` khác thiếu quyền cũng Không.
10. Chỉ đổi mật khẩu chính mình trong phiên hợp lệ; không phải đặt lại mật khẩu tài khoản khác. Login công khai chỉ cho mở biểu mẫu xác thực, không cho đọc dữ liệu nghiệp vụ trước đăng nhập.

## Lịch sử cập nhật

| Ngày | Thay đổi |
|---|---|
| 2026-10-22 | Hoãn kiểm tra chức danh/quyền UI đến khi MainViewModel có dữ liệu quyền. Khi triển khai ViewModel, để sẵn CanXxx bool mặc định true kèm TODO nối quyền; giữ validation/trạng thái tải/lưu độc lập. Ma trận đã duyệt là căn cứ tích hợp sau; chưa sửa code ViewModel. |
| 2026-10-22 | Giữ sáu chức danh; thay vai trò ngoài danh sách bằng chức danh và quyền bổ sung theo yêu cầu. Rà Vai trò đủ 14 màn, làm rõ A12/Quyền bổ sung và cấm Giám đốc quản lý tài khoản. Thêm danh mục mã quyền logic cần seed backend và ma trận 14 × 6 với toàn bộ ô đánh dấu đề xuất chờ duyệt. |
| 2026-10-22 | Người dùng duyệt Phương án 1 và toàn bộ 78 ô của ma trận 13 màn (bỏ Login công khai); chốt quyền mặc định/cấp thêm, seed đầy đủ quyền cho admin kỹ thuật, cấm Giám đốc có QuanLyTaiKhoan. Bổ sung endpoint đọc hẹp A02/A06/A07/A08, thẻ chuyển hướng A01, quyền xem/ghi thanh toán và trách nhiệm kiểm tra API. Tách A04 thành QuanLyDanhMucChuyenMon/QuanLyDanhMucGia theo nhóm trường, danh sách seed còn 12 mã quyền. |

## BẢNG TỔNG KẾT: CÁC THUỘC TÍNH VÀ QUY TẮC BỊ GIAO DIỆN CŨ BỎ QUÊN

| Phân hệ / Màn hình | Các trường / Quan hệ trong Domain bị bỏ quên | Hậu quả của giao diện cũ |
|---|---|---|
| **Khám sàng lọc (A02)** | `ChiSoSinhTon` (Mạch, Nhiệt độ, Huyết áp), `TienSuBenhLy`, `LyDoHoan`, danh sách `ChiDinhTiem`. | Không lưu được hồ sơ y tế chuẩn, không có căn cứ sinh tồn để ra chỉ định. |
| **Hồ sơ bệnh nhân (A03)** | `CCCD` cho phép null (trẻ em), `NgaySinh` (`DateOnly`), `GioiTinh`, quan hệ Khách hàng $\leftrightarrow$ Người tiêm. | Không xử lý được nghiệp vụ phụ huynh đăng ký cho trẻ nhỏ. |
| **Danh mục vắc xin (A04)** | `NhaSanXuat`, `XuatXu`, `TheTich`, `LieuLuong`, `PhanUngSauTiem`, quan hệ n-n `VacXin_Benh`, thành phần gói `GoiVacXin_VacXin`. | Không quản lý được thông số dược phẩm và gói nhiều mũi tiêm. |
| **Kho vắc xin (A05)** | `NgaySanXuat`, `HanSuDung` (`DateOnly`), `SoLuongBanDau`, bảng `BienDongKho`, cơ chế `RowVersion`. | Không kiểm soát được hạn sử dụng (FEFO), nguy cơ tồn kho âm và tranh chấp dữ liệu. |
| **Tiêm & Theo dõi (A06)** | `ViTriTiem`, `MuiTiem.SoLo`, toàn bộ bảng `TheoDoiSauTiem` (`TinhTrang`, `MucDoPhanUng`, `HuongXuTri`). | Không theo dõi được phản ứng sau tiêm 30 phút, mất dấu vết số lô tiêm. |
| **Đăng ký dịch vụ (A07)** | `ChiTietVacXinLe`, `ChiTietGoiVacXin`, `HangDoiVacXin`, quy tắc `RegistrationRules.CalculateTotal`. | Không thể đăng ký kết hợp mũi lẻ + gói vắc xin, không chốt được giá snapshot. |
| **Thu ngân (A08)** | Danh sách nhiều lần `ThanhToan`, `PhuongThuc`, `LoaiThanhToan`, quy tắc `RegistrationRules.RemainingAmount`. | Không hỗ trợ đặt cọc, trả góp nhiều lần, không chặn được thu vượt nợ. |
| **Lịch & Nhắc lịch (A09)** | Thực thể `NhacLich` (`KenhGui`, `ThoiDiemGui`, `TrangThai`), `LichHenTiem.DiemTiem`. | Không lưu được nhật ký nhắc lịch, không đổi/hủy lịch được. |
| **Tài khoản & Phân quyền (A12)**| Ràng buộc `NhanVien.ChucDanh` duy nhất, bảng n-n `TaiKhoan_QuyenHan`, mã hóa `MatKhauHash`. | Không có phân quyền theo chức danh y tế, bảo mật bằng tài khoản giả lập `admin/123`. |
