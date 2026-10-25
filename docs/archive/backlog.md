# Backlog khởi tạo

Các việc dưới đây đều chưa hoàn tất nghiệp vụ thật; dùng để tạo issue và theo dõi.

| Mã | Mức | Việc | Owner | Điều kiện hoàn tất |
|---|---|---|---|---|
| F01 | P0 | Xác thực khách hàng/nhân viên, đổi mật khẩu, phân quyền | Đạt | Hash mật khẩu; mô hình tài khoản khách hàng; login/logout/refresh; test 401/403; không dùng admin/123 |
| B01 | P0 | Hồ sơ khách hàng/người tiêm/người bảo trợ | Bảo | CRUD API; quyền theo chủ hồ sơ; thông tin định danh nullable đúng; nối web/Windows |
| B02 | P0 | Đăng ký lẻ + gói và hàng đợi thiếu vắc xin | Bảo | Có thể phối hợp hai loại trong một đăng ký; không rỗng; giá snapshot; không cấp lịch tự động khi thiếu căn cứ |
| B03 | P0 | Đặt cọc/trả góp, ghi nhận thanh toán | Bảo | Transaction/idempotency; chống thu vượt/thu lặp; phân biệt pending/failed/success; chưa tích hợp ngân hàng thật |
| D01 | P0 | Danh mục bệnh/vắc xin/loại/gói | Đạt | CRUD + kiểm tra input; gói nhiều vắc xin; bệnh nhiều-nhiều; không đưa thông tin y tế giả vào dữ liệu thật |
| D02 | P0 | Nhập lô, biến động và tồn kho | Đạt | Ngày SX/hạn, số lượng, nguồn gốc; transaction, rowversion; không tồn âm; audit nhân viên |
| D03 | P0 | Phiếu khám/chỉ định và ghi nhận mũi tiêm | Đạt | Gắn người tiêm/đăng ký/vắc xin; phân quyền bác sĩ/điều dưỡng; test chặn ghi sai quan hệ |
| Q01 | P0 | Lịch hẹn và lịch sử tiêm | Quý | Đúng người tiêm; đổi/hủy lịch có điều kiện; nối mobile/Windows; khách hàng không đọc hồ sơ người khác |
| Q02 | P0 | Theo dõi sau tiêm | Quý | Gắn đúng mũi; ghi tình trạng/phản ứng/xử trí, phân quyền; không tự chẩn đoán |
| Q03 | P1 | Nhắc lịch | Quý | Lưu trạng thái, tránh gửi lặp; trước mắt mô phỏng gửi, chỉ tích hợp kênh ngoài sau khi thống nhất |
| B04 | P1 | Khuyến mãi | Bảo | Thời gian/điều kiện/mức giảm được nhóm chốt; snapshot lúc đăng ký |
| D04 | P1 | Báo cáo thống kê | Đạt | Công thức rõ; lọc thời gian; số liệu khớp DB/hoàn tiền; xuất file nếu đặc tả yêu cầu |
| T01 | P0 | Tích hợp cả ba client và kiểm thử demo cuối | Cả nhóm | Luồng hồ sơ → đăng ký → khám → thu tiền → tiêm → theo dõi/lịch; kiểm tra quyền và lỗi mạng |

Thứ tự: chạy bộ khung → thống nhất contract/schema/quyền → F01 + D01/B01 → đăng ký/tài chính/kho → khám/tiêm/lịch → tích hợp và báo cáo. P1 làm sau khi các luồng P0 có dữ liệu nhất quán. Không coi thứ tự này là quan hệ include/extend trong UML.
