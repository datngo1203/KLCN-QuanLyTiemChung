# UI A05 Kho & lô

Nhánh `ui/a04-a05-a10`, commit riêng A05.

- Bảng lô hiển thị số lô, vaccine, NSX, hạn dùng, ban đầu/tồn và trạng thái; cận hạn dưới 30 ngày có nhãn FEFO, quá hạn/hết hàng có trạng thái. Quá hạn bị chặn điều chỉnh.
- Dialog nhập lô kiểm tra số lô trùng/rỗng, NSX trước hạn, số lượng nguyên dương. Điều chỉnh fixture kiểm tra `0 < số lượng <= tồn` và thêm dòng biến động.
- `IInventoryService` / `SampleInventoryService`; chưa có API ghi kho/contract để kiểm soát `RowVersion`. Xử lý xung đột đồng thời và tự tải lại dữ liệu mới chưa được thực thi; cần contract API.
- Build Release `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release --no-restore`: 0 warning, 0 error.
- Ảnh fixture WPF STA: `docs/ui-review/ui-a04-a05-a10/a05-{normal,error,empty}.png`; render View độc lập, chưa kiểm chứng DPI/thiết bị.
