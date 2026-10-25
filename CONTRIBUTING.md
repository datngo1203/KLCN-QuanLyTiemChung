# Cách đóng góp

1. Đọc AGENTS.md, docs/SCOPE.md, docs/thesis-scope-rules.md và phân công trong docs/team.md. Chỉ làm lát được giao; Mobile đóng băng.
2. Mỗi lát dùng nhánh `feat/sN-ten-lat` từ `main`, giữ thay đổi trong module được phân công.
3. Làm DB → API → Web/Windows gọi API thật; chưa có endpoint thì làm theo contracts trước. Không tạo Sample mới cho chức năng mới.
4. Thay đổi DTO/schema/quyền phải mô tả ảnh hưởng client và nhờ review chéo. Phân quyền hai vai trò theo SCOPE; API kiểm tra quyền và phạm vi hồ sơ.
5. Build và chạy kiểm tra phù hợp trong docs/testing.md; ghi kết quả/giới hạn trong docs/status.md và tiến độ một dòng trong bảng SCOPE. Không tạo status riêng hoặc ảnh review mới.
6. Tạo PR nhỏ vào `main`, nêu kết quả và phần chưa kiểm chứng, nhờ thành viên khác review.

Không commit secret, dữ liệu bệnh nhân, file build hoặc thay migration đã chia sẻ. Không tự thêm quyền cộng tác hay gán người từ tên gọi chưa có username. Chưa có giấy phép nguồn mở; mã được dùng cho nhóm khóa luận và không mặc định cấp phép tái phân phối công khai.
