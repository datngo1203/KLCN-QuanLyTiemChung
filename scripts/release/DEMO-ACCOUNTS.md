# KLCN090 — Tài khoản demo (Development)

Danh sách tài khoản và mật khẩu demo hiện hành, dùng cho môi trường local `vac-final` và quá trình chạy minh chứng khóa luận.

| Vai trò | Tài khoản | Mật khẩu |
|---|---|---|
| Quản trị viên | `demo.admin` | `Demo@26Adm01` |
| Lễ tân | `demo.reception` | `Demo@26Let01` |
| Bác sĩ | `demo.doctor` | `Demo@26Bsi01` |
| Điều dưỡng | `demo.nurse` | `Demo@26Ddu01` |
| Thu ngân | `demo.cashier` | `Demo@26Tng01` |
| Thủ kho | `demo.warehouse` | `Demo@26Tkh01` |
| Khách hàng (Customer) | `demo.customer@example.invalid` | `Demo@26Cus01` |

- **Windows (WPF):** Đăng nhập bằng 6 tài khoản nhân viên.
- **Website:** Đăng nhập bằng tài khoản Customer tại `http://localhost:5173/#account`.
- File cấu hình local cho script `scripts/release/Finish-FinalDemo.ps1`: `%LOCALAPPDATA%\VAC-Demo\demo-accounts.local.json`.
- Customer được đánh dấu đã xác minh email **chỉ trong Development**; không có email xác minh thực tế được gửi.
- Đây là thông tin tài khoản của bộ dữ liệu demo, không phải cấu hình tài khoản production.

Ghi nhận từ lần chạy `Finish-FinalDemo.ps1` ngày 10/11/2026, backup đã VERIFYONLY: `%LOCALAPPDATA%\VAC-Demo\Submission\20261110-144611-6b1114a9\FinalDemo.bak`.
