# Trạng thái khung — 19/10/2026

| Phần | Trạng thái thực tế |
|---|---|
| Web React | Có UI kế thừa, client HTTP và nút kiểm tra API; lint/build pass tại máy khởi tạo |
| Windows WPF | Có UI kế thừa, client HTTP và nút kiểm tra API; Release build pass, 0 cảnh báo/lỗi; chưa bấm thử lại toàn bộ UI |
| Mobile Flutter | Có Android/iOS project, navigation, màn danh mục đọc API, API client; analyze pass và 3 tests pass |
| Backend | API health/catalog/modules, OpenAPI, CORS và Problem Details; 11 tests pass |
| Database | 27 thực thể/mapping EF Core, migration InitialCreate và SQL sinh idempotent; model sinh DDL được; chưa chạy lên SQL Server thật |
| NuGet | Sau khi cập nhật dependency Pkcs, kiểm tra package trực tiếp và gián tiếp không báo lỗ hổng đã biết từ nguồn NuGet tại thời điểm kiểm tra |
| Kết nối HTTP | Gọi thật localhost:5080: health OK, danh mục demo lọc trả 1 mục, OpenAPI trả JSON; test CORS origin web pass |
| GitHub Actions | Có job backend/web/Windows/mobile; kết quả từ xa sẽ kiểm tra sau khi push |

Windows/Website vẫn dùng state cho form nghiệp vụ; login admin/123 trên Windows là mô phỏng. Khung chưa có backend login, thanh toán/ghi kho/ghi hồ sơ thật hoặc kênh gửi nhắc lịch thật. Lịch/hồ sơ mobile là màn khung. Không xem test qua là nghiệp vụ hoàn chỉnh.

Chưa kiểm chứng: chạy migration/transaction trên SQL Server, Docker Compose runtime (Docker daemon chưa chạy), build/chạy Android trên thiết bị, build/ký iOS trên macOS, review giao diện đầy đủ và kiểm thử quyền. CI có bước build Android debug để kiểm tra từ xa.

Phân công là đề xuất khởi tạo theo yêu cầu người dùng. Đã biết GitHub Đạt; chưa có username Bảo/Quý để mời/gán assignee. Thông tin tiến độ nằm ở tài liệu mới này; `ui-checklist-inherited.md` phản ánh lịch sử đợt UI cũ.
