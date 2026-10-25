# Giới hạn triển khai ở mức khóa luận tốt nghiệp

> Phạm vi hiện hành từ 25/10/2026: [SCOPE.md](SCOPE.md) được ưu tiên khi nội dung cũ xung đột; Mobile đóng băng, chỉ thanh toán một lần, kho/khám chi tiết/ma trận quyền hoãn.

Quyết định trực tiếp của người dùng ngày 24/10/2026. Áp dụng cho chat điều phối, tác giả và reviewer của Web, Windows, Mobile, API và SQL. Mục tiêu: sản phẩm đủ tốt để giải thích thiết kế, kiểm chứng chức năng và demo nghiệp vụ thật với dữ liệu giả. Không mặc định nhận yêu cầu vận hành hệ thống y tế production.

## 1. Chọn giải pháp vừa đủ

Dùng kiến trúc React/WPF/Flutter → ASP.NET Core API → SQL Server đã chốt. Tái sử dụng thư viện/framework hiện có. Không thêm service, lớp trừu tượng, công cụ vận hành hoặc cơ chế tùy biến nếu cách đơn giản đã đáp ứng luồng hiện tại. Không thiết kế trước mọi tình huống tương lai.

## 2. Chốt đầu ra trước khi làm

Mỗi gói ghi ngắn: luồng người dùng cần chạy, dữ liệu cần lưu, tiêu chí đạt, kiểm thử liên quan và phần hoãn. Ưu tiên một vài luồng hoàn chỉnh từ UI đến API/SQL thay vì nhiều màn chỉ có mock hoặc hạ tầng chưa được người dùng sử dụng. Dữ liệu mẫu được ghi rõ; không trình bày mock là nghiệp vụ thật.

## 3. Giữ chất lượng thiết yếu

Bắt buộc hash mật khẩu tại backend; API kiểm quyền và phạm vi hồ sơ; validate đầu vào; giữ ràng buộc dữ liệu; xử lý lỗi rõ ràng. Với tiền/tồn kho và ghi nhiều bảng, dùng transaction/kiểm tra cạnh tranh khi cần để tránh sai nghiệp vụ. Ghi nhận người thực hiện/thời điểm cho thao tác quan trọng. Không bịa nghiệp vụ y tế, tài chính hoặc quyền đại diện để làm demo chạy được. Credential test ghi sổ theo AGENTS; không đưa dữ liệu người thật vào Git.

## 4. Tài khoản Admin ở mức khóa luận

Đích mặc định là một lệnh seed/khởi tạo có kiểm tra tồn tại, tạo Admin demo, hash bằng backend và gán quyền đã chốt. Một Admin mẫu đủ cho demo, không cần khóa mô hình chỉ có một Admin. Sau đó quản lý tài khoản nhân viên bằng chức năng phần mềm phù hợp phạm vi. Tài khoản Admin ứng dụng và SQL login kết nối backend là hai khái niệm riêng.

Không mặc định yêu cầu offline operator riêng, Windows SID/ACL cố định, SQL operator role riêng, manifest/ledger cứu hộ, nâng cấp tài khoản legacy hay quy trình khôi phục cấp doanh nghiệp. Các cơ chế đã làm được giữ nguyên trong lúc đối chiếu; không tự tháo/xóa hoặc đổi hash/ledger/quyền để vượt lỗi. Nếu cần đơn giản hóa phần hiện có, tác giả chốt một diff thiết kế hẹp, review phần thay đổi rồi triển khai trong cùng gói, không khởi động lại toàn bộ M1.

## 5. Giới hạn môi trường và tích hợp ngoài

Mặc định demo trên máy Windows hiện có, SQL test/development và client phù hợp. VM, cloud, HA, đa vùng, DR, tải lớn và hạ tầng quản trị chuyên dụng thuộc hướng phát triển, trừ khi được giao cụ thể. Email/SMS/thanh toán có thể dùng adapter sandbox hoặc mô phỏng ghi nhãn; không bắt buộc mua dịch vụ thật. Vẫn kiểm chứng API/SQL và quyền thật cho luồng đã tuyên bố hoàn thành. Khi email xác minh nằm trong luồng demo, phải có cách nhận token test hoạt động, không bỏ xác minh âm thầm.

## 6. Phân loại góp ý review theo phạm vi

Chặn khi luồng đã chốt không chạy, sai dữ liệu/nghiệp vụ, mất dữ liệu, lộ hồ sơ trái quyền hoặc có lỗ hổng trực tiếp trong luồng đó. Ghi cải tiến tùy chọn cho tối ưu, cứu hộ/vận hành production, khả năng mở rộng và tình huống legacy ngoài phạm vi. Không dùng nhận xét “chưa production-ready” làm lý do tự động chặn demo hoặc nghiệm thu chức năng khóa luận. Đọc tài liệu cũ theo giới hạn này; tác giả cập nhật checklist đang dùng, ghi rõ mục hoãn và căn cứ, không nhận PASS cho ca chưa chạy.

## 7. Kiểm thử và điểm dừng

Chạy kiểm tra đúng phần ảnh hưởng: luồng chính, dữ liệu không hợp lệ, thiếu quyền/phạm vi dữ liệu và lưu/đọc lại SQL khi gói có ghi dữ liệu. Test concurrency/rollback tập trung vào nơi có nguy cơ sai tiền/tồn kho hoặc ghi dở transaction; không áp toàn ma trận deployment cho mọi CRUD. Giữ test đã có, chạy regression phù hợp khi sửa. Build/CI không thay runtime cần thiết; không lặp toàn bộ audit/tests khi không có thay đổi liên quan. Khi đạt tiêu chí gói, cập nhật kết quả/giới hạn và bàn giao review.

## 8. Kiểm soát phần phát sinh

Trước khi thêm cơ chế ngoài gói, nêu tình huống hiện tại cần nó, tác động và vì sao giải pháp đơn giản không đủ. Nếu chỉ phục vụ tương lai, ghi một mục hướng phát triển rồi tiếp tục gói hiện tại. Chỉ xin quyết định khi thật sự thay phạm vi/nghiệp vụ/contract hoặc cần quyền mới; không xin lại cho sửa lỗi thường lệ đã được giao. Quyết định này không tự cấp phép merge, xóa/tái tạo DB, đổi quyền SQL/OS hoặc gửi tin sang chat khác.

Ví dụ đạt mức khóa luận: đăng nhập → kiểm quyền → tạo hồ sơ → lưu SQL → mở lại vẫn thấy → tài khoản không có quyền bị từ chối. Kế hoạch nên ưu tiên hoàn thành chuỗi này trước khi thêm quy trình cứu hộ Admin hoặc tối ưu vận hành.
