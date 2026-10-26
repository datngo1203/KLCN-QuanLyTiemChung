# SQL Server

Schema thực thi nằm trong `backend/src/TiemChung.Infrastructure/Migrations/`. Chạy Compose theo README ở gốc để áp dụng migration tự động; không dùng bản SQL sinh cũ thay cho migration.

Lệnh từ thư mục gốc khi cần cập nhật DB thủ công, sau khi đặt connection string qua user-secrets hoặc biến môi trường:

```powershell
dotnet tool restore
dotnet ef database update --project backend/src/TiemChung.Infrastructure --startup-project backend/src/TiemChung.Api
```

`m1-sql-boundary-up.sql` là script thiết lập SQL operator boundary hiện có, không cần cho luồng demo seed Admin. Không tự chạy script này lên DB khác hoặc cấp quyền SQL/OS ngoài yêu cầu.

Không xóa migration đã dùng chung, drop/reset database hay đổi dữ liệu để vượt lỗi. Thay đổi schema do Đạt điều phối và cần review chéo. API xử lý quyền, validation, transaction và ghi dữ liệu; client không kết nối SQL trực tiếp.
