# Phân công khởi tạo và Git

| Thành viên | Sở hữu chính | Backend theo nghiệp vụ | Review chéo |
|---|---|---|---|
| Bảo — Diệp Nguyễn Gia Bảo | apps/web; hồ sơ/đăng ký/thu ngân/ưu đãi ở Windows khi nối module | Customers, Recipients, Registrations, Payments, Promotions | Quý review web/luồng khách hàng; Đạt review tiền/quyền/schema |
| Quý — Nguyễn Ngọc Quý | apps/mobile; lịch/nhắc/lịch sử/theo dõi ở Windows khi nối module | Appointments, Reminders, History, Aftercare | Bảo review mobile/luồng khách hàng; Đạt review dữ liệu y tế/quyền |
| Đạt — Ngô Thành Đạt | apps/windows; nền API/DB/CI | Catalog, Inventory, Clinical, Identity, Reports | Bảo hoặc Quý review contract/schema; Quý review ghi nhận tiêm/lịch sử |

Đạt là đầu mối tích hợp, không có nghĩa Đạt phải viết toàn bộ backend/Windows. Bảo/Quý thực hiện API và Windows module tương ứng; thống nhất vùng màn hình trước khi chỉnh cùng một file code-behind. Chia issue theo chức năng có đầu vào, kết quả và cách kiểm chứng rõ ràng.

Username chủ repo đã xác minh: `datngo1203` (Đạt). Username Bảo/Quý chưa có, nên chưa mời cộng tác hoặc gán assignee bằng cách đoán. Tên người phụ trách được ghi ngay trong issue; bổ sung GitHub Settings → Collaborators khi có username. CODEOWNERS tạm chỉ đặt reviewer cho các phần dùng chung, không gán nhầm người cho từng ứng dụng.

## Nhánh và review

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
