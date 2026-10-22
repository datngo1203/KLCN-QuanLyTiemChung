# Tài liệu Cấu trúc Feature-First & Giao diện Mobile (apps/mobile)

Tài liệu này ghi nhận chi tiết các thay đổi về kiến trúc, thư viện mạng và toàn bộ giao diện người dùng trên ứng dụng Mobile Flutter của hệ thống Quản lý Tiêm chủng KLCN090.

---

## 1. Tái cấu trúc theo kiến trúc Feature-First

Ứng dụng đã được chuyển đổi hoàn toàn từ cấu trúc phẳng/tập trung sang mô hình **Feature-First Architecture** chuẩn của Flutter:

```text
apps/mobile/lib/
├── core/
│   ├── constants/
│   │   └── app_constants.dart          # Cấu hình API_BASE_URL, timeouts (8s), danh mục vắc xin
│   ├── network/
│   │   ├── api_endpoints.dart          # Định nghĩa các endpoint (/health, /api/v1/catalog/vaccines, /api/v1/modules)
│   │   ├── api_exceptions.dart         # Chuẩn hóa lỗi Dio (Timeout, Connection, 404, 500) sang tiếng Việt
│   │   └── dio_client.dart             # Dio wrapper: BaseOptions, timeouts, headers JSON, hỗ trợ custom adapter
│   ├── theme/
│   │   ├── app_colors.dart             # Bảng màu chuyên biệt y tế (Cerulean Blue, Medical Teal, Slate)
│   │   └── app_theme.dart              # Material 3 Theme thống nhất: Card, Input, AppBar, Chip, NavigationBar
│   └── widgets/
│       ├── app_badge.dart              # Badge trạng thái (Có sẵn, Chuẩn GSP, Đã xác nhận, Mũi tiêm)
│       ├── app_search_field.dart       # Ô tìm kiếm bo góc có icon kính lúp và nút xóa nhanh
│       ├── empty_state_view.dart       # Giao diện trống thân thiện có hình minh họa và nút hành động
│       └── error_view.dart             # Giao diện lỗi kết nối mạng có nút thử lại
├── features/
│   ├── catalog/                        # Phân hệ Danh mục Vắc xin
│   │   ├── data/
│   │   │   ├── models/vaccine_model.dart             # Model vắc xin + trích xuất dữ liệu y khoa từ API
│   │   │   └── repositories/catalog_repository_impl.dart # Triển khai gọi API qua Dio
│   │   ├── domain/
│   │   │   ├── models/vaccine.dart                   # Thực thể Vaccine + định dạng tiền tệ VND (xxx.xxx đ)
│   │   │   └── repositories/catalog_repository.dart  # Abstract interface repository
│   │   └── presentation/
│   │       ├── screens/catalog_screen.dart           # Màn hình danh mục: Tìm kiếm, bộ lọc chip, kéo làm mới
│   │       └── widgets/
│   │           ├── category_filter_bar.dart          # Thanh chọn danh mục đối tượng (Trẻ em, Người lớn, Gói)
│   │           ├── vaccine_card.dart                 # Thẻ vắc xin hiển thị bệnh phòng ngừa, giá niêm yết
│   │           └── vaccine_detail_sheet.dart         # BottomSheet chi tiết bảo quản GSP, trả góp, đăng ký
│   ├── schedule/                       # Phân hệ Lịch tiêm, Nhắc lịch, Theo dõi sau tiêm (Quý phụ trách)
│   │   ├── domain/
│   │   │   └── models/
│   │   │       ├── appointment.dart                  # Model lịch hẹn tiêm chủng
│   │   │       ├── aftercare_record.dart             # Model theo dõi phản ứng sau tiêm
│   │   │       └── vaccine_reminder.dart             # Model cấu hình nhắc hẹn tiêm
│   │   └── presentation/
│   │       ├── screens/schedule_screen.dart          # Màn hình 4 tab: Sắp tới, Lịch sử, Theo dõi, Nhắc lịch
│   │       └── widgets/
│   │           ├── appointment_card.dart             # Thẻ lịch hẹn mốc thời gian, phòng tiêm, bác sĩ
│   │           ├── aftercare_card.dart               # Thẻ theo dõi 30p, tiến trình, thân nhiệt, nút cấp cứu
│   │           └── reminder_card.dart                # Thẻ nhắc hẹn đếm ngược, bật/tắt, chọn kênh SMS/Zalo
│   └── profile/                        # Phân hệ Hồ sơ & Người được tiêm (Bảo & Quý kết nối)
│       ├── domain/
│       │   └── models/patient_profile.dart           # Model hồ sơ bệnh nhân, nhóm máu, mã tiêm cá nhân
│       └── presentation/
│           └── screens/profile_screen.dart           # Sổ tiêm điện tử có QR, chuyển đổi hồ sơ gia đình
├── app.dart                            # Shell điều hướng chính + Top AppBar thương hiệu y tế
└── main.dart                           # Điểm khởi chạy ứng dụng
```

---

## 2. Chuyển đổi thư viện mạng sang Dio

1. **Loại bỏ `http`**: Đã gỡ bỏ thư viện `http` khỏi `pubspec.yaml`.
2. **Tích hợp `dio: ^5.11.1`**:
   - `DioClient` cấu hình `BaseOptions`: `connectTimeout: 8s`, `receiveTimeout: 8s`, mặc định trỏ về `http://10.0.2.2:5080` (hoặc biến môi trường `API_BASE_URL`).
   - Tự động gắn header `Content-Type: application/json` và `Accept: application/json`.
   - `ApiException` chuyển toàn bộ các mã lỗi HTTP và lỗi ngắt kết nối mạng sang thông báo tiếng Việt dễ hiểu.
   - Hỗ trợ `HttpClientAdapter` cho phép unit test độc lập không phụ thuộc môi trường mạng thật.

---

## 3. Chi tiết thiết kế lại Giao diện người dùng (UI/UX)

Giao diện được thiết kế lại toàn diện theo phong cách ứng dụng y tế hiện đại, chuyên nghiệp, không sử dụng các layout AI sơ sài:

### 3.1. Bảng màu & Kiểu dáng thương hiệu
- **Màu chủ đạo (Primary):** Xanh dương y tế (`#0284C7`), Xanh ngọc Teal (`#0D9488`).
- **Nền & Bề mặt:** Slate-50 (`#F8FAFC`) và trắng tinh khiết (`#FFFFFF`), viền nhẹ Slate-200 (`#E2E8F0`).
- **Độ bo góc:** 12px (nút bấm, ô nhập) và 16px (thẻ vắc xin, thẻ lịch hẹn).
- **Huy hiệu (AppBadge):** Màu nền nhạt kèm chữ đậm tương phản cao cho trạng thái: *Có sẵn*, *Chuẩn GSP (2-8°C)*, *Đã xác nhận*, *Đang theo dõi 30p*.

### 3.2. Màn hình Danh mục vắc xin (`CatalogScreen`)
- **Header thương hiệu:** Hiển thị tên trung tâm, cơ sở (Cơ sở 1 • TP.HCM) và chuông thông báo có chấm đỏ.
- **Thanh tìm kiếm:** Ô tìm kiếm vắc xin theo tên hoặc bệnh phòng ngừa, có nút xóa nhanh.
- **Thanh trượt danh mục:** Lọc nhanh theo đối tượng: *Tất cả, Trẻ em, Người lớn, Phụ nữ dự định/mang thai, Gói vắc xin*.
- **Thẻ vắc xin (`VaccineCard`):**
  - Hiển thị tên vắc xin, nhà sản xuất và nước xuất xứ (Pháp, Bỉ, Việt Nam...).
  - Dòng chỉ định bệnh phòng ngừa có biểu tượng khiên bảo vệ.
  - Giá niêm yết định dạng tiền tệ VNĐ rõ ràng (ví dụ: `450.000 đ`).
  - Huy hiệu "Có sẵn" và nút "Chi tiết".
- **Modal chi tiết y khoa (`VaccineDetailSheet`):**
  - Xem thông tin bảo quản dây chuyền lạnh GSP (2-8°C).
  - Chính sách hỗ trợ trả góp 0% / đặt cọc giữ vắc xin theo quy định đồ án.
  - Chi tiết phác đồ tiêm tham khảo và đối tượng khuyến nghị.
  - Nút đăng ký tiêm chủng.
- **Kéo để làm mới (`RefreshIndicator`)** và màn hình trạng thái trống/lỗi kết nối kèm nút "Thử lại".

### 3.3. Màn hình Lịch tiêm & Theo dõi sau tiêm (`ScheduleScreen`)
Được mở rộng thành 4 tab nghiệp vụ chuyên sâu theo đúng phân công module `followup`:
1. **Tab "Sắp tới":** Thẻ lịch hẹn với ngày giờ, phòng tiêm, bác sĩ, số thứ tự mũi (ví dụ: *Mũi 4 nhắc lại*) và lưu ý chuẩn bị trước tiêm.
2. **Tab "Lịch sử":** Nhật ký các mũi đã hoàn thành kèm kết quả theo dõi sau tiêm an toàn.
3. **Tab "Theo dõi sau tiêm":**
   - **Đồng hồ đếm ngược 30 phút tại trung tâm:** Thanh tiến trình thời gian thực (`18/30 phút`), đo thân nhiệt (36.8°C), ghi chú y tế của bác sĩ trực.
   - **Nút "Báo triệu chứng":** Mở bottom sheet ghi nhận các phản ứng (sốt nhẹ, sưng đau, quấy khóc, phát ban).
   - **Nút "Hỗ trợ khẩn cấp":** Gọi trực tiếp hotline cấp cứu phản vệ 1900 6868.
   - **Cẩm nang 24–48h tại nhà:** Hướng dẫn phân biệt phản ứng thông thường và dấu hiệu nguy hiểm cần nhập viện ngay.
4. **Tab "Nhắc lịch":**
   - Đếm ngược số ngày đến mũi tiêm dự kiến (ví dụ: *Còn 7 ngày*).
   - Công tắc Bật/Tắt nhắc hẹn cho từng mũi tiêm.
   - Dropdown chọn kênh nhận nhắc: *Thông báo App, Tin nhắn SMS, Zalo ZNS*.

### 3.4. Màn hình Hồ sơ & Sổ tiêm chủng điện tử (`ProfileScreen`)
- **Chuyển đổi thành viên gia đình:** Hỗ trợ chuyển đổi nhanh giữa các hồ sơ (Chủ tài khoản, Con nhỏ, Người thân) phù hợp với quy tắc: người đăng ký và người được tiêm có thể khác nhau.
- **Thẻ Sổ tiêm chủng điện tử:** Thiết kế dạng thẻ y tế sang trọng có mã QR, mã số tiêm cá nhân (`#TC-2026-0901`), số mũi đã tiêm và nhóm máu.
- **Danh mục tiện ích:** Quản lý lịch sử thanh toán & đặt cọc/trả góp, phiếu khám sàng lọc, tổng đài tư vấn.

---

## 4. Kiểm tra chất lượng mã nguồn

- **Phân tích tĩnh (`flutter analyze`):** `No issues found!` (0 errors, 0 warnings, 0 lints).
- **Bộ kiểm thử tự động (`flutter test`):**
  - `api_client_test.dart`: Kiểm tra gọi đúng endpoint API qua Dio và phân tích giá thập phân; kiểm tra xử lý lỗi HTTP 503 qua `MockDioAdapter`.
  - `widget_test.dart`: Kiểm tra tải danh mục vắc xin và điều hướng tab Lịch tiêm ngoại tuyến.
  - Kết quả: `All tests passed! (3/3 tests pass)`.
