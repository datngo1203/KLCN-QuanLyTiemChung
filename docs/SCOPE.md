# SCOPE: KLCN090 – Hệ thống quản lý tiêm chủng

Nguồn sự thật duy nhất về phạm vi. Cập nhật: 25/10/2026. Hạn nộp: 08/11/2026 (theo đề cương, cần xác nhận lại với GVHD).

## Nguyên tắc

- Chỉ làm các lát bên dưới. Muốn thêm gì phải được nhóm đồng ý.
- Mỗi lát đi trọn đường: DB → API → Web và Windows gọi API thật. Không dùng dữ liệu Sample cho chức năng mới.
- Giữ luồng ở mức tối thiểu theo đề cương: không thêm trạng thái, bước hay ngoại lệ ngoài bảng này.
- Phân quyền chỉ có 2 vai trò: `NhanVien` và `KhachHang`. Kiểm tra tại API, không chỉ ẩn nút trên UI.

## Các lát (làm theo thứ tự)

| # | Lát | Phụ trách | Xong khi | Trạng thái |
|---|-----|-----------|----------|------------|
| S0 | Nền: SQL Server thật, áp dụng migration, CI xanh | Đạt | API đọc/ghi được DB thật; migration chạy sạch | ☑ 25/10: Compose SQL/API thật, 2 migrations, SQL/HTTP 42/42; CI xanh |
| S1 | Đăng nhập, đăng xuất, đổi mật khẩu, phân quyền 2 vai trò (F01) | Đạt | Mật khẩu có hash; login thật trên Web và Windows; test 401/403 | ☐ |
| S2 | Danh mục: loại tiêm chủng, loại vắc xin, vắc xin, gói (D01) | Đạt | CRUD qua API; Windows quản lý; Web giới thiệu + tìm kiếm/lọc | ☐ |
| S3 | Hồ sơ khách hàng và người được tiêm/bé (B01) | Bảo | CRUD qua API; Web xem/đăng ký hồ sơ; Windows quản lý thông tin bé | ☐ |
| S4 | Đăng ký dịch vụ (lẻ + gói) và thanh toán một lần (B02, B03) | Bảo | Tạo đăng ký, chọn vắc xin/gói, chốt giá lúc đăng ký, ghi thanh toán (mô phỏng); Web + Windows | ☐ |
| S5 | Lịch tiêm, ghi nhận mũi đã tiêm, theo dõi sau tiêm, nhắc lịch (Q01–Q03) | Quý | Sinh lịch các mũi; ghi nhận đã tiêm; theo dõi sau tiêm; nhắc qua Email hoặc log mô phỏng; Web xem lịch | ☐ |
| S6 | Khuyến mãi, ưu đãi (B04) | Bảo | Mức giảm + thời gian hiệu lực; áp lúc đăng ký | ☐ |
| S7 | Thống kê, báo cáo (D04) | Đạt | 2–3 báo cáo: doanh thu theo tháng, số mũi theo vắc xin, đăng ký theo trạng thái | ☐ |
| S8 | Tích hợp và kiểm thử (T01) | Cả nhóm | Chạy trọn luồng hồ sơ → đăng ký → thanh toán → lịch trên máy sạch; có bảng test case | ☐ |

## Không làm (hoặc chỉ nằm trong sơ đồ/báo cáo)

- Mobile Flutter: đóng băng, chỉ làm nếu còn thời gian sau S8.
- 6 chức danh, 12 mã quyền, ma trận 78 ô.
- Kho/lô, biến động tồn kho.
- Phiếu khám sàng lọc chi tiết, sinh tồn, chỉ định.
- Đặt cọc, trả góp, idempotency.
- SMS, tích hợp ngân hàng, xuất file báo cáo.
- Thiết kế lại UI, thêm tài liệu giai đoạn/ảnh review mới.

## Quy ước làm việc

- `contracts/README.md` là chuẩn API; sửa contract phải báo cả nhóm vì ba client phụ thuộc vào đó.
- Mỗi lát một nhánh (`feat/sN-ten-lat`), PR nhỏ, merge vào `main` sớm.
- Chỉ một người sửa schema/migration tại một thời điểm.
- Trạng thái ghi một dòng vào bảng trên; không tạo file status mới.
