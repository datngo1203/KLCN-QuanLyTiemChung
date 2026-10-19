# Kiểm thử và tiêu chí tích hợp

```powershell
dotnet test backend/TiemChung.slnx
npm --prefix apps/web run lint
npm --prefix apps/web run build
dotnet build apps/windows/TiemChung.Desktop.csproj
cd apps/mobile
flutter analyze
flutter test
```

CI chạy các nhóm web, backend, Windows, Flutter trên runner phù hợp. Xem Actions cho kết quả, không xem có file workflow là CI đã xanh. Lần đầu thay dữ liệu thật cần kiểm thử tích hợp SQL riêng.

Test backend hiện bảo vệ đăng ký lẻ+gói, nhiều khoản thanh toán, không rỗng/âm/thu vượt; smoke API không cần DB; kiểm tra 27 bảng EF sinh được DDL. Mobile có test UI/HTTP, không phụ thuộc API đang chạy. Chưa có test giao dịch SQL cạnh tranh, thanh toán thật, login thật hoặc build iOS.

Mỗi module phải bổ sung: luồng đúng, input thiếu/sai, không có quyền, quyền khác chủ hồ sơ, lỗi mạng, ghi hai lần, rollback lỗi. Kiểm thử kho/tài chính trên SQL Server thật, không dùng InMemory provider làm bằng chứng giao dịch đúng.

Manual demo: API chạy → bấm Kiểm tra API trên web/Windows → mobile tải danh mục. Các form UI kế thừa vẫn cục bộ; chỉ đánh dấu một module đã tích hợp sau khi dữ liệu qua API và tồn tại sau tải lại.

Các bước trước phát hành: HTTPS, login/quyền, SQL migrations backup, dữ liệu hợp lệ, audit log, thông báo quyền riêng tư, dữ liệu demo tắt, không còn tài khoản hardcode, quy trình thanh toán/nhắc lịch được thống nhất. Chưa có deployment tự động tới dịch vụ trả phí.
