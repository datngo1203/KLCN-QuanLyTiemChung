# Mobile kế thừa PR #21

Owner: Quý (`ngocquydev`). Gói sửa được người dùng cho phép; Bảo review luồng khách hàng, Đạt review dữ liệu y tế/quyền trước merge.

- Giữ kiến trúc feature-first, Dio, giao diện danh mục/lịch/hồ sơ.
- Danh mục chỉ đọc id/name/manufacturer/origin/price và source từ API hiện hành. Không suy bệnh/phác đồ/đối tượng/tồn kho từ tên hoặc tin trường ngoài contract. Thiếu giá khác giá 0; thiếu nhà sản xuất/xuất xứ được hiển thị chưa có.
- Nhãn `API: Dữ liệu mẫu` cho source demo; database có nhãn riêng; thiếu/nguồn lạ ghi chưa xác định, kể cả danh sách rỗng. Không có dữ liệu dự phòng mẫu khi API lỗi.
- Sai envelope/kiểu trường/bản ghi/giá âm là lỗi dữ liệu; items rỗng hợp lệ là empty. Có retry, tìm tên và refresh; bỏ bộ lọc đối tượng suy diễn.
- Không khẳng định GSP, nhiệt độ bảo quản, phí bao gồm hoặc chính sách trả góp khi API chưa cung cấp. Đăng ký chỉ thông báo chưa tích hợp, không tạo lịch.
- Lịch/lịch sử/theo dõi/nhắc và hồ sơ là dữ liệu mẫu hư cấu cục bộ, có banner; chưa lưu/gửi báo cáo, nhắc, gọi tư vấn hay xác thực. Hướng dẫn chuyên môn chờ nội dung được duyệt.
- Không sửa backend, API contract hoặc schema. Chưa có endpoint nghiệp vụ/auth, không truy cập SQL.

Kết quả kiểm chứng và giới hạn theo SHA: [status](status-mobile-review.md).
