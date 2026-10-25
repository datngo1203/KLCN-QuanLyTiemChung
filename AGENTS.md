# KLCN090 — quy tắc làm việc

## Khóa phạm vi (ưu tiên cao nhất)
- Chỉ làm các mục trong docs/SCOPE.md. Muốn thêm gì phải hỏi người dùng trước.
- Không tạo thêm tài liệu thiết kế, giai đoạn, ma trận hay ảnh review mới.
- Mỗi thay đổi là một lát dọc: DB → API → client gọi API thật. Không dùng Sample cho chức năng mới.
- Chưa có endpoint thì làm endpoint theo contracts/ trước, rồi mới sửa UI.
- Giữ luồng ở mức tối thiểu, không thêm trạng thái, bước hay ngoại lệ ngoài SCOPE.md.
- Mỗi PR một chức năng; build và test trước khi báo xong.

Quyết định 25/10/2026: docs/SCOPE.md thay phạm vi cũ bên dưới khi có xung đột; Mobile đóng băng, đặt cọc/trả góp hoãn. Quy tắc bảo mật, dữ liệu và quyền thao tác vẫn giữ.

Đọc README, docs/project-context.md, docs/team.md, docs/status.md trước khi sửa.
Mỗi phiên làm giao diện Windows phải đọc docs/ui-workflow.md trước khi sửa.
Phạm vi hiện tại: Website React, Windows WPF, Mobile Flutter; dùng chung ASP.NET Core API và SQL Server.
Không sửa tài liệu gốc/ảnh tham chiếu để hợp thức hóa nghiệp vụ. docs/diagrams là bản chụp mô hình tham khảo; thay đổi phải giải thích trong PR.
Mỗi nhân viên có đúng một chức danh; đăng ký có thể gồm vắc xin lẻ và gói; hỗ trợ nhiều lần thanh toán/đặt cọc/trả góp.
Không tự suy diễn dữ liệu hoặc quy trình nội bộ VNVC. Dữ liệu mẫu chỉ dùng phát triển, không đưa thông tin khách hàng thật vào Git.
Mật khẩu thật phải hash tại backend. admin/123 trong Windows là đăng nhập UI mô phỏng, chưa phải cơ chế bảo vệ.
Client không truy cập SQL trực tiếp. Mọi kiểm tra quyền, tiền, tồn kho và ghi dữ liệu phải nằm ở API.
Chỉ người sở hữu module sửa module đó; thay đổi contracts/schema cần người khác review. Không tự thêm GitHub username hoặc quyền cộng tác khi chưa có thông tin.
Sau thay đổi: chạy kiểm tra đúng phần bị ảnh hưởng; báo đúng kết quả và các phần chưa kiểm tra. Tiến độ chỉ cập nhật cột Trạng thái trong docs/SCOPE.md; không ghi mục mới vào docs/status.md.

## Giới hạn mức khóa luận — quyết định người dùng 24/10/2026

Đọc và tuân thủ `docs/thesis-scope-rules.md` trước khi lập kế hoạch, triển khai hoặc review. Mục tiêu là khóa luận có các luồng nghiệp vụ chạy thật, giải thích được và demo lặp lại trên môi trường phát triển. Ưu tiên giải pháp đơn giản đáp ứng yêu cầu đã chốt; không tự nâng chuẩn thành hệ thống vận hành production/doanh nghiệp.

Giữ hash mật khẩu backend, quyền API/phạm vi hồ sơ, validation và tính đúng dữ liệu/transaction thiết yếu. Admin khởi tạo bằng seed đơn giản, quản lý tài khoản trong phần mềm; không mặc định yêu cầu operator riêng, OS account/ACL, legacy recovery, hạ tầng phân tán hoặc dịch vụ ngoài cho demo. Chỉ thêm độ phức tạp khi có tình huống thuộc phạm vi chứng minh cần thiết và giải pháp đơn giản không đáp ứng.

Mỗi gói chốt luồng demo, tiêu chí đạt và phần hoãn; review chặn lỗi thực thuộc phạm vi đó. Yêu cầu production/legacy chưa được giao ghi hướng phát triển, không tự biến thành gate nghiệm thu khóa luận. Khi đạt tiêu chí thì bàn giao, không mở rộng kiểm chứng vô hạn. Không tự xóa code/dữ liệu/evidence hoặc tháo kiểm tra bảo mật đã có; đồng bộ phạm vi trong tài liệu đang dùng trước thay đổi thiết kế. Quy tắc này bổ sung, không bỏ các quy tắc dữ liệu/nghiệp vụ/review và quyền thao tác ở trên.

## Ponytail cho dự án

Dùng duy nhất skill chung tại `C:/Users/DAT/.agents/skills/ponytail/SKILL.md`; không cài bản sao trong repo. Áp dụng khi viết/sửa/refactor/review code; dùng mức `full` mặc định để chọn thay đổi nhỏ nhất giải quyết đủ yêu cầu, tái sử dụng code/framework và tránh làm trước cho tương lai. Không tự chuyển ultra, chạy audit toàn repo hoặc xóa code ngoài gói. Quy tắc khóa luận/nghiệp vụ/quyền thao tác trong AGENTS và `docs/thesis-scope-rules.md` vẫn có hiệu lực; Ponytail không cho phép bỏ validation/quyền/test cần thiết. Không cần cài hooks hoặc dịch vụ phụ để dùng skill này.

## Tài khoản test — quyết định người dùng 24/10/2026

Người dùng cho phép các chat tạo, sử dụng và lưu rõ username/password synthetic chỉ dùng development/test, không cần xin phép lại việc ghi sổ. Sổ dùng chung trên máy: `C:/Users/DAT/Documents/Codex/KLCN090-Coordination/TEST-ACCOUNTS.local.md` (ngoài Git). Đọc mục phù hợp trước khi test; khi tạo/đổi tài khoản, cập nhật môi trường/DB, vai trò, username, password, trạng thái dự kiến/đã áp dụng/đã kiểm và ngày cập nhật. Không tự đặt lại password đang có chỉ để khớp sổ; giá trị chưa biết phải ghi chưa biết.

Được ghi plaintext credential synthetic vào sổ local này và hiển thị mật khẩu test cho người dùng khi cần. Không coi quy định không tiết lộ secret thật là lý do chặn việc lưu/sử dụng credential test đã được cho phép. Không đưa sổ local hoặc mật khẩu môi trường test này vào Git/PR/log công khai. Quyết định này không tắt hash mật khẩu tại backend, validation, phân quyền, audit hoặc kiểm thử bảo mật của sản phẩm; không tự mở rộng quyền SQL/OS hay áp dụng cho credential thật/production. Không gửi tin sang chat khác nếu chưa có yêu cầu trực tiếp.
