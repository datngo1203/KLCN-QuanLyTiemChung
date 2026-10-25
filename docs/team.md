# Phân công khởi tạo và Git

> Phạm vi hiện hành từ 25/10/2026: [SCOPE.md](SCOPE.md) được ưu tiên khi nội dung cũ xung đột; Mobile đóng băng, chỉ thanh toán một lần, kho/khám chi tiết/ma trận quyền hoãn.

| Thành viên | GitHub | Sở hữu chính | Backend theo nghiệp vụ | Review chéo |
|---|---|---|---|---|
| Bảo — Diệp Nguyễn Gia Bảo | `giabao9373` | apps/web; hồ sơ/đăng ký/thu ngân/ưu đãi ở Windows khi nối module | Customers, Recipients, Registrations, Payments, Promotions | Quý review web/luồng khách hàng; Đạt review tiền/quyền/schema |
| Quý — Nguyễn Ngọc Quý | `ngocquydev` | apps/mobile; lịch/nhắc/lịch sử/theo dõi ở Windows khi nối module | Appointments, Reminders, History, Aftercare | Bảo review mobile/luồng khách hàng; Đạt review dữ liệu y tế/quyền |
| Đạt — Ngô Thành Đạt | `datngo1203` | apps/windows; nền API/DB/CI | Catalog, Inventory, Clinical, Identity, Reports | Bảo hoặc Quý review contract/schema; Quý review ghi nhận tiêm/lịch sử |

Đạt là đầu mối tích hợp, không có nghĩa Đạt phải viết toàn bộ backend/Windows. Bảo/Quý thực hiện API và Windows module tương ứng; thống nhất vùng màn hình trước khi chỉnh cùng một file code-behind. Chia issue theo chức năng có đầu vào, kết quả và cách kiểm chứng rõ ràng.

Các username do nhóm cung cấp: `giabao9373` (Bảo), `ngocquydev` (Quý), `datngo1203` (Đạt). GitHub hiện trả quyền đọc cho Bảo và Quý; việc gán issue yêu cầu quyền phù hợp nên các issue của hai bạn chưa có assignee. Chỉ chủ repo có thể nâng quyền trong Settings → Collaborators; sau khi quyền được cập nhật, gán Bảo vào #2–#4 và #11, Quý vào #8–#10. CODEOWNERS chưa gắn hai bạn vì GitHub yêu cầu quyền ghi để thực thi yêu cầu review.

## Nhánh và review

Từ S0: mỗi lát một nhánh `feat/sN-ten-lat`, PR vào `main` theo SCOPE.md; quy trình develop bên dưới là lịch sử. Giữ review chéo cho contract/schema.

- `main`: phiên bản tích hợp ổn định; `develop`: nhánh tích hợp thường ngày.
- Nhánh công việc từ develop: `feature/bao-registration`, `feature/quy-appointments`, `feature/dat-inventory`; fix tương tự.
- Tạo PR vào develop; ít nhất một thành viên khác review. Sửa contract hoặc migration phải có review chéo.
- Sau khi các module chạy chung và CI xanh, tạo PR develop → main. Không commit thẳng lên main sau mốc khởi tạo.
- Dùng squash merge và xóa nhánh sau merge; không force-push nhánh chung.

```powershell
git switch develop
git pull --ff-only
git switch -c feature/bao-registration
# sửa code, chạy kiểm tra
git add <cac-file-thuc-su-thay-doi>
git commit -m "feat(registration): support singles and packages"
git push -u origin feature/bao-registration
```

GitHub Issues là nguồn việc; docs/backlog.md là bản nhóm chức năng. Chưa đặt deadline giả hoặc xem phân công này là đã được các thành viên xác nhận. Repo không tự gửi tin nhắn hoặc mời ai khi thiếu username.

## Trạng thái bảo vệ nhánh

Ngày 19/10/2026, đã thử bật yêu cầu review và kiểm tra CI khi repo còn private; GitHub trả 403 do giới hạn gói tài khoản. Repo đã được chủ repo chuyển sang public ngày 20/10/2026 theo yêu cầu. Chưa kiểm tra lại branch protection sau khi đổi visibility; hiện không tuyên bố nhánh đã được bảo vệ. Quy tắc review phía trên là thỏa thuận nhóm cho tới khi xác minh cấu hình trên GitHub.
