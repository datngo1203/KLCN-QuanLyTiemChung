# UI A04 Danh mục

Nhánh `ui/a04-a05-a10`, commit riêng A04.

- ViewsV2 có bảng/form vaccine, trường mô tả theo Screen Spec, danh sách bệnh nhiều lựa chọn, kiểm tra giá/thể tích/bệnh, nhãn tạm ngừng màu trung tính; có 5 nút tab danh mục.
- `ICatalogService` / `SampleCatalogService` tách nguồn mẫu. `CanEditChuyenMon` và `CanEditGia` mặc định true, có TODO quyền; các ô liều/thể tích/phản ứng và giá/trạng thái khóa theo nhóm cờ.
- API GET catalog hiện trả một tập con (id/tên/nhà sản xuất/xuất xứ/giá), không đủ để dựng form/quan hệ bệnh; CRUD vẫn là state mẫu, chưa ghi API. Không sửa contract/schema.
- Chưa có form thành phần gói và CRUD cho ba tab tra cứu; năm tab mới là điều hướng khung. Đây là khoảng trống so với Screen Spec.
- Build Release: `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release --no-restore` — 0 warning, 0 error.
- Ảnh tạo từ harness WPF STA với fixture mẫu: `docs/ui-review/ui-a04-a05-a10/a04-{normal,error,empty}.png`; không có dữ liệu khách hàng thật. Ảnh là render View độc lập, không gồm shell điều hướng.
