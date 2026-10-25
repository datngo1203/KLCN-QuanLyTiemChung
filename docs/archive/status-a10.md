# UI A10 Khuyến mãi

Nhánh `ui/a04-a05-a10`, commit riêng A10.

- Bảng/form có tên, nội dung, thời gian bắt đầu/kết thúc, mức ưu đãi; validation kết thúc sau bắt đầu, mức không âm; badge trạng thái.
- `IPromotionService` / `SamplePromotionService`; thao tác CRUD chỉ cập nhật fixture. Xóa bị khóa cho chương trình mẫu đã gán đăng ký.
- Chưa có API/contract ghi khuyến mãi trong checkout; không tự thêm route hoặc thay contract/schema. Quy tắc xóa trên đăng ký thật cần API xác nhận.
- Build Release `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release --no-restore`: 0 warning, 0 error.
- Ảnh fixture WPF STA: `docs/ui-review/ui-a04-a05-a10/a10-{normal,error,empty}.png`; render View độc lập, chưa kiểm chứng DPI/thiết bị.
