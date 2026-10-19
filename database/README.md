# SQL Server và mô hình dữ liệu

EF Core migrations trong `backend/src/TiemChung.Infrastructure/Migrations` là nguồn schema thực thi. `initial-schema.sql` được sinh từ migration để xem/review; không chạy lại lên DB đã có migration. 27 bảng bắt nguồn từ mô hình quan hệ đã vẽ, có PK/FK/unique/check constraints. Không có dữ liệu bệnh nhân hoặc tài khoản mặc định.

Các bảng liên kết `TaiKhoan_QuyenHan`, `VacXin_Benh`, `GoiVacXin_VacXin` dùng khóa ghép. `NhanVien.ChucDanh` là một trường bắt buộc, không có bảng nhiều chức danh/nhân viên. `TaiKhoan.MaNhanVien` unique. `MatKhau` trong sơ đồ được đổi thành `MatKhauHash` trong code; chưa có login API. `SoLo` tạm được xem duy nhất toàn hệ thống theo sơ đồ; cần review trước nhập dữ liệu thực.

Đăng ký có hai bảng chi tiết lẻ và gói. Một đăng ký phải có ít nhất một dòng trong hai bảng; đây là ràng buộc nhiều bảng, không chỉ là CHECK trên một cột. `RegistrationRules` đã có quy tắc tính tổng/không rỗng; service ghi thật phải gọi quy tắc trong transaction. Nhiều dòng `ThanhToan` cho đặt cọc/trả góp; chỉ các khoản thành công mới được trừ công nợ. Không cho tổng thanh toán vượt số cần thu. Tổng tiền chốt/giảm giá cần lưu thành snapshot khi hoàn thiện module tài chính.

`LoVacXin`/`DangKyDichVu` có SQL rowversion hỗ trợ phát hiện ghi đè. Xuất kho cần transaction và cập nhật có điều kiện để tránh tồn âm; khung này chưa triển khai service xuất kho. Tất cả quan hệ đang Restrict delete để tránh xóa dây chuyền hồ sơ y tế. CCCD người được tiêm nullable theo mô hình.

## Tạo DB mới

```powershell
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:TiemChung" "Server=localhost;Database=TiemChung;Integrated Security=True;TrustServerCertificate=True" --project backend/src/TiemChung.Api
dotnet ef database update --project backend/src/TiemChung.Infrastructure --startup-project backend/src/TiemChung.Api
```

Với SQL container, thay server thành `localhost,14333` và dùng thông tin đăng nhập riêng qua user-secrets. Muốn API đọc DB thật: đặt `Catalog__UseDemoData=false` trước khi chạy.

## Quan hệ cần hoàn thiện trước khi nối nghiệp vụ thật

Mũi tiêm cần liên kết rõ chỉ định/chi tiết dịch vụ đã được duyệt; mô hình khởi tạo hiện chỉ giữ các FK từ sơ đồ cũ. Quy tắc người bảo trợ, tài khoản khách hàng, số tiền cần thu sau ưu đãi và tính duy nhất của số lô phải được thống nhất trước migration tiếp theo. Đó là công việc thiết kế module, không phải lỗi được che bằng dữ liệu mẫu.

## Khi sửa schema

```powershell
dotnet ef migrations add TenThayDoi --project backend/src/TiemChung.Infrastructure --startup-project backend/src/TiemChung.Api
dotnet ef migrations script --idempotent --project backend/src/TiemChung.Infrastructure --startup-project backend/src/TiemChung.Api --output database/migrations-review.sql
```

Người sở hữu module viết thay đổi; Đạt điều phối migration. Không xóa migration đã dùng chung, không tự drop database. Checkpoint quan trọng: mô hình tài khoản hiện gắn nhân viên; **cần bổ sung tài khoản khách hàng và cơ chế ủy quyền người được tiêm trước khi bật đăng nhập web/mobile**. Hình UML là tham khảo thiết kế, chưa phải chứng nhận chuẩn hóa hay quy trình VNVC.
