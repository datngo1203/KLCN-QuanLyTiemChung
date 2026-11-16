# Kịch bản DEMO 08:30 — trình diễn rộng, ít thao tác, đúng ranh giới nghiệp vụ

**Lưu ý:** Đây là ngân sách thời gian, **chưa đo trên máy Windows**. Tất cả thao tác trên WPF phụ thuộc kiểm chứng runtime. Một chặng lỗi thì dùng hình/clip chứng cứ và đi tiếp, không chạy lại từ đầu.

| Mốc | Nội dung | Bằng chứng | Lời dẫn ngắn | Cách rút gọn |
|---|---|---|---|---|
| 00:00–00:20 | Giới thiệu Web/Windows dùng API + SQL chung | Web+WPF đang mở; slide kiến trúc | “Hệ thống tách giao diện khách hàng và nhân viên, nghiệp vụ xử lý tại API.” | Chỉ slide nếu WPF unavailable |
| 00:20–02:45 | Web: danh mục 15 vaccine → Avaxim → hồ sơ phụ huynh/bé → đăng ký → nhập liên hệ, thời gian → Xem lại | **Giữ F8 #1–#6 đã PASS**; Nguyễn Minh An/Gia Hân | “Khách hàng chọn dịch vụ, hệ thống kiểm soát hồ sơ liên kết và tạm tính theo dữ liệu hiện hành.” | Nếu quá thời gian, nhảy ngay tới ảnh `06-xem-lai.png`; không submit |
| 02:45–03:45 | Windows Tiếp nhận: A03 + A07 + A09 | WPF login Lễ tân, tra cứu hồ sơ/bản ghi đăng ký/lịch đã tồn tại | “Nhân viên nhìn thấy dữ liệu qua quyền được cấp; thời gian mong muốn chưa là lịch xác nhận.” | Dùng ảnh/clip thật đã capture nếu đổi tài khoản mất thời gian |
| 03:45–05:05 | Thanh toán mô phỏng → Khám và chỉ định → Ghi tiêm → Theo dõi, nhắc lịch | Clips/screens của A08, A02, A06, A06F, A09 và bản ghi SQL phù hợp | “Các thao tác bị ràng buộc bởi quyền, trạng thái, chỉ định và tồn kho; demo không tự xác nhận y khoa.” | 1 clip tổng 80s hoặc 4 ảnh có chú thích rõ nguồn, không claim end-to-end |
| 05:05–06:10 | Kho: xem lô, hạn, tồn, biến động, lịch sử nhập | WPF A05, dữ liệu SQL, form nhập lô | “Kho lưu vết lô và biến động. Nhập mới tách quyền với tra cứu và lưu chứng từ.” | Chỉ xem 1 lô + 1 biến động, không ghi mới |
| 06:10–07:20 | Ưu đãi: Windows A10 → Web Offers → ứng dụng giảm trên đăng ký | **1 khuyến mãi hợp lệ phải có**; WPF A10 và Web `#promotions`; phần Xem lại chọn ưu đãi | “Nhân viên cấu hình ưu đãi giảm VND; API xác thực thời hạn và tính khoản giảm lúc đăng ký.” | Nếu Web đăng ký đang ở màn Xem lại, dùng ảnh/clip đối chiếu hai client, không quay về gõ lại |
| 07:20–08:10 | A04 Danh mục, A11 3 báo cáo, A12 phân quyền | WPF/ảnh màn và slide bằng chứng | “Cùng dữ liệu nguồn phục vụ danh mục, quản trị quyền và thống kê.” | Chỉ 3 tab báo cáo hoặc montage kiểm chứng, không đi sâu CRUD |
| 08:10–08:30 | Kết luận | Slide kiến trúc/CI | “Các nhóm chức năng bám yêu cầu đề cương; S8 và thanh toán QR thật chưa nghiệm thu nên không trình bày là hoàn tất.” | Kết thúc dứt khoát |

## Chính sách video và screenshot

- Có thể dùng clip quay từ **runtime thật** để rút thời gian cho các quy trình đổi vai trò; phải gọi đó là video minh chứng, không giả là tương tác live.
- Một chức năng chưa có record thì không tự tạo record y khoa, nhập kho hoặc thanh toán để quay cho đẹp.
- Nếu muốn live hơn: giữ Web F8 #1–#6 và A05 read-only; các đoạn còn lại phát bằng chứng đã kiểm tra.
- Đo tổng đúng 08:30; nếu trễ, cắt các đoạn tùy chọn trước, không tăng tốc nhập Web đến mức mắt không theo kịp.

## Ưu đãi dữ liệu chuẩn (chỉ là ví dụ dữ liệu vận hành nội bộ, KHÔNG phải chương trình VNVC)

- Tên: **Ưu đãi chăm sóc sức khỏe gia đình – Tháng 11/2026**
- Nội dung: **Giảm 50.000 đồng khi khách hàng chọn áp dụng ưu đãi cho đăng ký dịch vụ. Mức giảm và thời hạn được hệ thống kiểm tra tại thời điểm đăng ký.**
- Ngày bắt đầu: **01/11/2026**.
- Ngày kết thúc: **31/12/2026**.
- Mức giảm: **50.000 VND**, bật áp dụng.
- Chỉ tạo nếu chưa có chương trình cùng tên. Dùng Windows A10 theo quyền `QuanLyKhuyenMai`, reload và kiểm `GET /api/v1/promotions/valid`/Web hiển thị. Không tự suy các điều kiện tối thiểu, theo nhóm vaccine hoặc theo người được tiêm vì S6 chưa có rule đó.
- Khi demo Avaxim 80U: giá sản phẩm snapshot trong curated seed là **685.000 VND**; **tổng cuối cùng phải đọc từ API/UI hiện tại**, gồm phí khám/tư vấn và mức giảm tương ứng; không mặc định ra 635.000 VND.

## Phím/nút

- **F8**: chạy một chặng Web đã có (Codex xác nhận #1–#6). **F9**: tạm dừng. **Esc**: thoát automation.
- Không yêu cầu Codex thêm hàng loạt phím cho mỗi screen WPF. Chuyển sang WPF bằng bản ghi/ảnh/clip đã kiểm chứng.
- Nếu API báo `BLOCKED`, dừng thao tác và chuyển evidence/PowerPoint; không reset SQL.