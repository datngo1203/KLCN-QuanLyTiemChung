# Quy trình làm giao diện Windows

Đọc [SCOPE.md](SCOPE.md), [giới hạn khóa luận](thesis-scope-rules.md), [phân công](team.md) và [trạng thái](status.md) trước mỗi phiên. SCOPE.md ưu tiên khi Screen Spec hoặc tài liệu lịch sử có nội dung khác.

## Giao diện và dữ liệu

- Làm trong `apps/windows/ViewsV2/`, tái sử dụng `apps/windows/Themes/` và token `CE.*`; không thiết kế lại UI ngoài lát được giao.
- Mỗi chức năng mới đi trọn DB → API → client gọi API thật. Chưa có endpoint thì chốt contract và làm endpoint trước; không tạo Sample mới để thay chức năng thật.
- Giữ nhãn **Dữ liệu mẫu** cho màn mẫu còn tồn tại; không coi kiểm tra UI mẫu là đã lưu API/SQL.
- Dùng mô hình phân quyền có sẵn ở backend theo SCOPE; không làm thêm UI quản trị quyền. API kiểm tra quyền và phạm vi hồ sơ; ẩn menu không thay kiểm tra quyền. Giữ validation, xử lý lỗi và ngăn gửi trùng đã có.
- Đối chiếu trường dữ liệu/command thuộc lát hiện hành; kiểm trạng thái bình thường, lỗi, rỗng, bàn phím và nút vô hiệu. Không bổ sung bước nghiệp vụ ngoài SCOPE.

## Phân công và hoàn tất

- Giữ quyền sở hữu module theo [team.md](team.md); thống nhất trước khi cùng sửa một vùng UI. Contract/schema cần review chéo.
- Build Windows và chạy kiểm tra đúng phần ảnh hưởng theo [testing.md](testing.md). Kiểm dữ liệu ghi/đọc lại và quyền khi nối API thật.
- Ghi một dòng tiến độ vào bảng SCOPE; ghi kết quả và phần chưa kiểm trong `docs/status.md`. Không tạo file status riêng hoặc ảnh review mới.
- Mỗi lát một nhánh `feat/sN-ten-lat`, PR nhỏ vào `main`; chỉ add tệp thuộc phạm vi. Không force-push hoặc tự merge nhánh chung. PR ghi thay đổi, lệnh/kết quả kiểm tra, giới hạn và issue liên quan; không yêu cầu ảnh review.
