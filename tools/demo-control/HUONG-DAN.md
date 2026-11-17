# KLCN DEMO

Mở **KLCN DEMO** và giữ cửa sổ WPF của repo mở. Chọn **Chạy toàn bộ**. Nhấn **F8** để qua từng màn, **F9** để tạm dừng hoặc tiếp tục, **Esc** hoặc **Stop** để dừng. **Quay về menu** trả lại danh sách cảnh trong Edge. Có thể mở từng cảnh từ menu.

| Cảnh | Lời dẫn |
|---|---|
| 01 · Admin · A04 · READ/PREVIEW | Em mở Prevenar 13, mã VX-PREVENAR-13, rồi chỉ các trường mã và tên trên form danh mục. |
| 02 · Thủ kho · A05 · PREVIEW/READ | Đây là form nhập lô. Tiếp theo em đối chiếu lô từ cấu hình máy, tồn 9 liều, lịch sử tiếp nhận và biến động đã lưu. |
| 03 · Admin · A10 · READ/PREVIEW | Chương trình ưu đãi hiện hành giảm 50.000 đồng. Em chỉ vào thời hạn và mức giảm. DK-0001 không áp dụng chương trình này. |
| 04 · Khách hàng · Web · PREVIEW | Em minh họa cách khách nhập và xem lại trước khi gửi, dùng hồ sơ người được tiêm lịch sử và Prevenar 13. Tiếp theo em mở hồ sơ DK-0001 đã lưu của chính người được tiêm lịch sử để giải thích các bước tiếp nhận đến theo dõi. |
| 05 · Lễ tân · A03/A07 · READ/PREVIEW | Em chỉ các trường của form đăng ký, rồi mở DK-0001 đã lưu để đối chiếu người được tiêm từ cấu hình demo và dịch vụ Prevenar 13. |
| 06 · Thu ngân · A08 · READ | Biên nhận DK-0001 ghi thanh toán mô phỏng thành công. Em chỉ số tiền và trạng thái giao dịch. |
| 07 · Lễ tân · A09 · READ | LH-0001 là lịch đã xếp của DK-0001. Tab nhắc lịch cho thấy chức năng quản lý, còn hàng đợi hiện trống. |
| 08 · Bác sĩ · A02 · READ | Đây là phiếu khám đã lưu của LH-0001. Em chỉ phần kết luận và chỉ định CD-0001. |
| 09 · Điều dưỡng · A06/A06F · READ | MT-0001 ghi Prevenar 13 và đúng lô đã tra ở kho. Em mở kết quả theo dõi đã lưu. Hệ thống yêu cầu mốc quan sát 30 phút trước khi được hoàn tất theo dõi. |
| 10 · Admin · A11 · READ | Em đối chiếu ba báo cáo về đăng ký, doanh thu mô phỏng và số mũi. Có thể mở A12 từ menu nếu cần trình bày vai trò và phân quyền. |

**Đăng ký mới người được tiêm mới** là cảnh WRITE tùy chọn. Bấm **Xác nhận đăng ký** ở bước Xem lại và đồng ý hộp thoại mới gửi một đăng ký. Đọc mã DK thực tế từ kết quả. **RESET KLCN DEMO** là shortcut riêng và yêu cầu nhập RESET để quay về baseline.

Nhật ký kiểm thử nằm trong demo.log.

## Cấu hình bản review

- Dùng Node.js 24, Edge và Windows PowerShell. Chạy npm install trong thư mục này để lấy Playwright tương ứng bản local.
- Sao chép 01_DATA_DEMO.example.json thành 01_DATA_DEMO.json và điền cấu hình máy cùng dữ liệu demo được phép sử dụng. Ngày sinh và ngày mong muốn dùng định dạng YYYY-MM-DD. Giờ mong muốn dùng HH:mm.
- Trỏ credentialsSourceRelativePath đến tệp tài khoản riêng tư đặt ngoài repo, có cột vai trò, tên đăng nhập và mật khẩu. Tên đăng nhập và mật khẩu nằm trong backtick Markdown. Điền wpf.lotNumber và employeeUsernames cho từng vai trò và employeeOnlyForWpfReadOnly cho bước WPF của lượt rehearsal cũ.
- Chạy node demo.cjs khi runtime vac-final và cửa sổ WPF đúng repo đã sẵn sàng. Bản review giữ các mã bản ghi và nhãn đối chiếu của fixture demo. Kiểm tra cấu hình và dataset trước khi chạy.
- Hành trình chính chỉ xem lại. Nút Xác nhận trong cảnh đăng ký riêng mở hộp thoại và cấp quyền gửi một yêu cầu tạo đăng ký. Shortcut RESET và dữ liệu runtime được quản lý trên máy demo.
