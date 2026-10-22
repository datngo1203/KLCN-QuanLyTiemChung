# KLCN090 — quy tắc làm việc

Đọc README, docs/project-context.md, docs/team.md, docs/status.md trước khi sửa.
Mỗi phiên làm giao diện Windows phải đọc docs/ui-workflow.md trước khi sửa.
Phạm vi hiện tại: Website React, Windows WPF, Mobile Flutter; dùng chung ASP.NET Core API và SQL Server.
Không sửa tài liệu gốc/ảnh tham chiếu để hợp thức hóa nghiệp vụ. docs/diagrams là bản chụp mô hình tham khảo; thay đổi phải giải thích trong PR.
Mỗi nhân viên có đúng một chức danh; đăng ký có thể gồm vắc xin lẻ và gói; hỗ trợ nhiều lần thanh toán/đặt cọc/trả góp.
Không tự suy diễn dữ liệu hoặc quy trình nội bộ VNVC. Dữ liệu mẫu chỉ dùng phát triển, không đưa thông tin khách hàng thật vào Git.
Mật khẩu thật phải hash tại backend. admin/123 trong Windows là đăng nhập UI mô phỏng, chưa phải cơ chế bảo vệ.
Client không truy cập SQL trực tiếp. Mọi kiểm tra quyền, tiền, tồn kho và ghi dữ liệu phải nằm ở API.
Chỉ người sở hữu module sửa module đó; thay đổi contracts/schema cần người khác review. Không tự thêm GitHub username hoặc quyền cộng tác khi chưa có thông tin.
Sau thay đổi: chạy kiểm tra đúng phần bị ảnh hưởng; ghi đúng kết quả và các phần chưa kiểm tra trong docs/status.md.
