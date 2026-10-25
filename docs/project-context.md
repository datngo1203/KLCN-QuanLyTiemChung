# Bối cảnh và phạm vi

> Phạm vi hiện hành từ 25/10/2026: [SCOPE.md](SCOPE.md) được ưu tiên khi nội dung cũ xung đột; Mobile đóng băng, chỉ thanh toán một lần, kho/khám chi tiết/ma trận quyền hoãn.

Đề tài CNTT-KLCN090: Xây dựng hệ thống quản lý tiêm chủng tại Trung tâm tiêm chủng. Nhóm: Diệp Nguyễn Gia Bảo, Nguyễn Ngọc Quý, Ngô Thành Đạt. Nguồn dự án ghi GVHD Mạnh Thiên Lý; thông tin lịch ban đầu không được dùng làm deadline mới khi chưa đối chiếu.

Đề cương gốc có Website + Windows. Ngày 19/10/2026 người dùng yêu cầu thêm ứng dụng mobile cùng cả hai nền tảng đó; repo triển khai hướng **Web + Windows + Mobile**. Đây là quyết định khởi tạo kỹ thuật của nhóm; không tự khẳng định giảng viên đã duyệt thay đổi đề cương.

## Quyết định đã chốt

- Một nhân viên có đúng một chức danh.
- Một đăng ký có thể gồm cả vắc xin lẻ và gói; phải xác định người được tiêm.
- Có thể đặt cọc/thanh toán nhiều lần/trả góp.
- Quản lý cơ sở dữ liệu vắc xin, bệnh phòng ngừa, lô, ngày sản xuất/hạn, nguồn gốc, liều lượng, phản ứng sau tiêm, trạng thái.
- Khách hàng và người được tiêm là hai vai trò khác nhau; có thể cùng một người hoặc phụ huynh đăng ký cho trẻ. CCCD người được tiêm có thể thiếu; xử lý người bảo trợ cần hoàn thiện quy tắc lưu/kiểm tra.
- Phân quyền tại API, không chỉ ẩn nút trên giao diện.

## Bảy quy trình làm căn cứ

1. Nhập vắc xin từ nhà cung cấp.
2. Đăng ký thông tin khách hàng và người được tiêm.
3. Đăng ký dịch vụ tiêm chủng.
4. Khám sàng lọc và chỉ định.
5. Thanh toán dịch vụ.
6. Thực hiện tiêm chủng và theo dõi sau tiêm.
7. Quản lý lịch tiêm và nhắc lịch.

Nguồn hiện có còn nêu ưu đãi, báo cáo, loại tiêm, gói, tài khoản/phân quyền. Các nhóm này có vị trí trong backlog và giao diện kế thừa; chưa có endpoint ghi thực.

Thông tin VNVC công khai chỉ dùng tham khảo. Không coi repo là phần mềm nội bộ VNVC và không tự bổ sung tiêu chí y tế, phác đồ, giá, quyền hay chính sách tài chính như quy định chính thức.
