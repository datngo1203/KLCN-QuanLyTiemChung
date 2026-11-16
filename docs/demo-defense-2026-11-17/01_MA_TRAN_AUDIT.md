# KLCN090 — Ma trận audit đề cương → phần mềm → demo

**Ngày chốt:** 16/11/2026 (GMT+7). **Mục tiêu:** chuẩn bị phiên bảo vệ 17/11/2026. Đây là audit chức năng dựa trên đề cương và source GitHub, **không phải** báo cáo kiểm chứng database/runtime cục bộ.

## Nguồn đã kiểm tra

- `Đề cương khóa luận.pdf`, trang 2–4: mục tiêu, chức năng Windows/Website và điểm chấm.
- GitHub `main` tại `92d26778777909daeea200089f9f4461be25cc84`: `AGENTS.md`, `SCOPE.md`.
- Draft PR #204 / DATA-R2 tại `a92af63011eb2accddf402dfd7e6cdac402b49fe` (chưa merge): Web registration, promotion, WPF navigation, kho, báo cáo, contracts và tài khoản demo.
- Kết quả chạy được **Codex báo cáo**: Web F8 #1–#6 PASS; hồ sơ Nguyễn Gia Hân được tạo một lần; chưa gửi đăng ký. Chưa tự kiểm tra log và Windows runtime của người dùng.

### Quy ước bằng chứng

- **CODE:** tính năng hiện hữu trong code/contract; chưa có nghĩa runtime có dữ liệu.
- **RUNTIME-PASS:** đã có bằng chứng màn hình/chạy do Codex báo cáo.
- **DATA-UNKNOWN:** chưa xác minh bản ghi phù hợp trong SQL `vac-final`.
- **DATA-MISSING:** người dùng báo thiếu dữ liệu; phải bổ sung qua nghiệp vụ thật.
- **PARKED:** nằm ngoài phạm vi đã hoàn thành; không hứa trong demo.

| # | Tiêu chí từ đề cương / phạm vi khóa luận | Bằng chứng phần mềm đã xác định | Tình trạng dữ liệu runtime | Quyết định demo |
|---|---|---|---|---|
| 1 | Hai nền tảng + API + SQL | React Web, WPF, ASP.NET Core, SQL Server theo AGENTS/SCOPE | Web/API/SQL được Codex kiểm tra; WPF login còn UNVERIFIED | Nói 20 giây, mở Web và WPF; có sơ đồ slide dự phòng |
| 2 | Đăng ký/đăng nhập, phân quyền, đổi mật khẩu | S1 DONE; accounts 7 vai trò; Windows A12/A14 | WPF role-login UNVERIFIED | Nêu phân quyền, chiếu màn hình tài khoản nếu đã smoke; không bắt đổi 7 tài khoản trực tiếp |
| 3 | Danh mục loại tiêm chủng, vắc xin | Windows A04; Web Catalog tìm/lọc/chi tiết; DATA-R2 curated 15 vắc xin | 15 vaccine + chi tiết Avaxim RUNTIME-PASS | Live F8, minh họa 1 vaccine thật và tên hãng/giá snapshot |
| 4 | Gói vắc xin | Catalog/Registration hỗ trợ `package` | DATA-R2 không seed package không có đủ nguồn; tồn tại package runtime chưa chứng minh | Không giả rằng có gói thương mại thật; nếu trống, nói rõ giới hạn dataset |
| 5 | Quản lý khách hàng và người được tiêm | Windows A03, Web Profiles, API ownership | Nguyễn Minh An, Nguyễn Gia Hân RUNTIME-PASS từ Codex | Live Web; WPF tra cứu nếu được kiểm chứng |
| 6 | Website đăng ký dịch vụ | Web `Registrations.jsx` 4 bước; API/SQL | F8 tới màn Xem lại PASS; không POST | Live nhập theo F8, dừng trước xác nhận; có đăng ký cũ để minh họa lưu nếu có |
| 7 | Windows quản lý đăng ký dịch vụ | WPF A07 | DATA-UNKNOWN | Mở đăng ký đã có; nếu trống, chỉ trình bày luồng và ghi rõ thiếu minh chứng runtime |
| 8 | Thanh toán dịch vụ | Windows A08, Web payment flows, payment mô phỏng | DATA-UNKNOWN; payOS thật PARKED | Hiển thị giao dịch mô phỏng đã lưu; tuyệt đối không gọi là QR ngân hàng thật |
| 9 | Quản lý lịch tiêm/hẹn | Windows A09; Website mục `Tiêm chủng của tôi` | DATA-UNKNOWN | Mở lịch thực nếu có; phân biệt ngày mong muốn và lịch đã xác nhận |
| 10 | Nhắc lịch và nhật ký | A09, Email reminder/outbox theo RF-05E/G | DATA-UNKNOWN tại `vac-final` | Dùng lịch sử email/thông báo có nguồn; không gửi email bừa hoặc gọi SMS thật |
| 11 | Khám sàng lọc và chỉ định (core nghiệp vụ) | Windows A02 | DATA-UNKNOWN | Clip/capture của bản ghi đã lưu; không tự tạo kết luận y khoa để demo |
| 12 | Ghi nhận tiêm và theo dõi sau tiêm | A06 và A06F | DATA-UNKNOWN | Clip/capture với test record xác thực nếu có; không click xác nhận ghi tiêm |
| 13 | Kho: lô, tồn, hạn dùng, biến động | Windows A05/X1 | DATA-UNKNOWN | Ưu tiên tra cứu read-only; không bịa số lô và lượng tồn |
| 14 | Nhập lô mới, lịch sử tiếp nhận | X2.1 và X2.2.1; quyền ThuKho+NhapKho | DATA-UNKNOWN; quyền phải kiểm tra | Mở form + lịch sử SQL nếu có; không xác nhận nhập lô |
| 15 | Khuyến mãi/ưu đãi | Windows A10 CRUD, Web Offers/valid, giảm VND cố định trong Registration | **DATA-MISSING: người dùng xác nhận đang trống** | **ƯU TIÊN SỐ 1 bổ sung 1 ưu đãi hợp lệ qua WPF/API và kiểm Web hiển thị + review discount** |
| 16 | Báo cáo thống kê | Windows A11 gồm doanh thu mô phỏng theo tháng, số mũi/vaccine, số đăng ký/trạng thái | DATA-UNKNOWN | Trình diễn 3 tab, mô tả đúng nguồn số liệu; nếu trống không chế số |
| 17 | Tài liệu phân tích/thiết kế/kiểm thử | Đề cương trang 1–4; SCOPE/CI/test; slide của nhóm | Slide V13 đã có; CI PR #204 được báo PASS (cần đối chiếu exact SHA nếu viện dẫn) | Dùng slide sơ đồ và bằng chứng test, không cố đưa vào thao tác GUI |

## Vấn đề cần sửa ngay (theo thứ tự)

**P0 — EMPTY PROMOTIONS**: tạo đúng một chương trình nội bộ **hư cấu nhưng hợp lý** để chứng minh chức năng đã triển khai. Không gán là ưu đãi chính thức của VNVC. Kiểm tra không trùng tên trước khi tạo; kiểm thực sự xuất hiện ở Web và ô chọn khi Xem lại. Phải lấy tiền từ API, không hardcode giá/tổng trong công cụ.

**P1 — DATA COVERAGE**: trên local Windows, tra cứu theo quyền 7 vai trò để xác minh có/không có: đăng ký, thanh toán mô phỏng, lịch hẹn, nhắc lịch, bác sĩ/điều dưỡng, kho, A11. Chỉ thu thập evidence, không tạo chuỗi medical/payment/inventory mới để làm số liệu đẹp.

**P2 — DEMO REHEARSAL**: giữ nguyên Web F8 #1–#6. Chuẩn bị các màn Windows/clip kiểm chứng. Đo thời gian tổng. Không thêm automation WPF rời rạc sát giờ.

**PARKED/CHƯA ĐẠT:** S8 chưa bắt đầu; real payOS QR chưa nghiệm thu; SMS provider thật không được claim; package dữ liệu thật không được tự bịa; không có bằng chứng full end-to-end runtime từ đăng ký mới → mũi tiêm.

## Ranh giới kỹ thuật

- Không chạy lệnh restore/reseed/reset/delete trên `vac-final` hay `TiemChung`.
- Không chạy `VAC.ps1 -Mode start` khi runtime đã chạy: startup chưa được kiểm chứng migration-safe.
- Nếu cần tập lại Web F8 #1–#6 thì có thể lặp vì dừng trước POST; **không cần nút xóa dữ liệu**.
- Nút khôi phục toàn database chỉ phù hợp sau khi có baseline + môi trường tập **tách biệt**, và kiểm thử restore thành công. Không làm sát ngày bảo vệ.