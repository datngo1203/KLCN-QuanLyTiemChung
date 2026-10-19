# Bảo mật và giới hạn của khung

Chưa có xác thực/phân quyền backend hoàn chỉnh; API hiện chỉ cung cấp dữ liệu công khai. Không đưa repo lên sử dụng với bệnh nhân hoặc dữ liệu thật trước khi hoàn tất module Identity và kiểm thử quyền. `admin / 123` chỉ là login mô phỏng của UI Windows và phải được gỡ khi tích hợp.

Mật khẩu phải hash với cơ chế chuẩn; không lưu rõ, không dùng tài khoản chia sẻ. Không lưu JWT/bí mật trong Git hoặc biến VITE_*. API phải tự kiểm tra quyền từng bản ghi, chống overposting, tách khách hàng/nhân viên và audit thao tác. Không ghi CCCD/thông tin y tế vào log.

Dùng user-secrets hoặc biến môi trường cho connection string; `.env` chỉ ở máy cá nhân. Đặt HTTPS và cấu hình CORS đúng origin khi triển khai. Chưa có gateway thanh toán, SMS, email hoặc push thật. Không nhập token Github vào file để chia sẻ nhóm.

Nếu phát hiện lộ bí mật: thu hồi/đổi bí mật trước, báo chủ repo bằng kênh riêng; không mở issue công khai có token hoặc dữ liệu cá nhân. Repo private không thay thế phân quyền API hay xử lý dữ liệu an toàn.
