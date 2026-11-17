# KLCN DEMO

- Mở KLCN DEMO trên Desktop. Chọn cảnh hoặc **Chạy toàn bộ**. Dùng F8 để qua từng bước, F9 để tạm dừng hoặc tiếp tục và Esc để dừng. **Quay về menu** dừng điều khiển trong Edge hoặc WPF.
- Chạy toàn bộ mở danh mục và kho trước, sau đó tới Web rồi các cảnh WPF còn lại. Nếu thiếu cửa sổ WPF, Edge vẫn vào được phần Web. Giữ cửa sổ WPF của dự án mở sẵn để điều khiển các cảnh nhân viên.
- F8 trên Web chỉ đưa bản nháp người được tiêm demo tới Xem lại. Nút **Ghi đăng ký người được tiêm demo…** yêu cầu xác nhận riêng và gửi tối đa một đăng ký trong phiên. A07 có thêm bước mở đăng ký mới nếu lần ghi trong phiên đã được xác nhận. Đăng ký lịch sử #1 thuộc người được tiêm lịch sử.
- Kho, ưu đãi, lịch, khám/chỉ định, tiêm, theo dõi, báo cáo và tài khoản dùng dữ liệu đã lưu hoặc màn xem trước. Hàng đợi nhắc lịch và email hiện trống.
- **RESET KLCN DEMO** là shortcut riêng. Nhập RESET để khôi phục TiemChung về baseline. Chỉ dùng khi muốn bỏ dữ liệu tạo từ sau baseline.

Nhật ký nằm trong demo.log. WPF cần nghiệm thu giao diện của các cảnh mới trên cửa sổ thực.

## Cấu hình bản review

- Dùng Node.js 24, Edge và Windows PowerShell. Chạy npm install trong thư mục này để lấy Playwright tương ứng bản local.
- Sao chép 01_DATA_DEMO.example.json thành 01_DATA_DEMO.json và điền cấu hình máy cùng dữ liệu demo được phép sử dụng. Ngày sinh và ngày mong muốn dùng định dạng YYYY-MM-DD. Giờ mong muốn dùng HH:mm.
- Trỏ credentialsSourceRelativePath đến tệp tài khoản riêng tư đặt ngoài repo, có cột vai trò, tên đăng nhập và mật khẩu. Tên đăng nhập và mật khẩu nằm trong backtick Markdown. Điền wpf.lotNumber và employeeUsernames cho từng vai trò và employeeOnlyForWpfReadOnly cho bước WPF của lượt rehearsal cũ.
- Chạy node demo.cjs khi runtime vac-final và cửa sổ WPF đúng repo đã sẵn sàng. Bản review giữ các mã bản ghi và nhãn đối chiếu của fixture demo. Kiểm tra cấu hình và dataset trước khi chạy.
- Nút xác nhận của ứng dụng được khóa trong bước Xem lại. Nút ghi riêng trên panel mở xác nhận và cấp quyền gửi một yêu cầu tạo đăng ký. Shortcut RESET và dữ liệu runtime được quản lý trên máy demo.
