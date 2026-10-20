# Trạng thái khung — 19/10/2026

| Phần | Trạng thái thực tế |
|---|---|
| Web React | Có UI kế thừa, client HTTP và nút kiểm tra API; lint/build pass tại máy khởi tạo |
| Windows WPF | Có UI kế thừa, client HTTP và nút kiểm tra API; Release build pass, 0 cảnh báo/lỗi; chưa bấm thử lại toàn bộ UI |
| Mobile Flutter | Có Android/iOS project, navigation, màn danh mục đọc API, API client; analyze pass và 3 tests pass ở máy khởi tạo lẫn GitHub Actions; bước build Android APK debug trên CI đã pass |
| Backend | API health/catalog/modules, OpenAPI, CORS và Problem Details; 11 tests pass |
| Database | 27 thực thể/mapping EF Core, migration InitialCreate và SQL sinh idempotent; model sinh DDL được; chưa chạy lên SQL Server thật |
| NuGet | Sau khi cập nhật dependency Pkcs, kiểm tra package trực tiếp và gián tiếp không báo lỗ hổng đã biết từ nguồn NuGet tại thời điểm kiểm tra |
| Kết nối HTTP | Gọi thật localhost:5080: health OK, danh mục demo lọc trả 1 mục, OpenAPI trả JSON; test CORS origin web pass |
| GitHub Actions | Có job backend/web/Windows/mobile; [lần kiểm tra code khởi tạo fab544a](https://github.com/datngo1203/KLCN-QuanLyTiemChung/actions/runs/39232542082), [develop](https://github.com/datngo1203/KLCN-QuanLyTiemChung/actions/runs/39232543473). Xem kết quả trực tiếp trong Actions. Các bổ sung tài liệu không thay code dùng commit skip CI để tránh build lại không cần thiết. |

Windows/Website vẫn dùng state cho form nghiệp vụ; login admin/123 trên Windows là mô phỏng. Khung chưa có backend login, thanh toán/ghi kho/ghi hồ sơ thật hoặc kênh gửi nhắc lịch thật. Lịch/hồ sơ mobile là màn khung. Không xem test qua là nghiệp vụ hoàn chỉnh.

Chưa kiểm chứng: chạy migration/transaction trên SQL Server, Docker Compose runtime (cấu hình đã validate, Docker daemon chưa chạy), chạy Android trên thiết bị, build/ký iOS trên macOS, review giao diện đầy đủ và kiểm thử quyền. CI có bước build Android debug; kết quả nằm ở các liên kết phía trên.

Phân công là đề xuất khởi tạo theo yêu cầu người dùng. Username nhóm đã cung cấp: Đạt `datngo1203`, Bảo `giabao9373`, Quý `ngocquydev`. GitHub hiện chỉ trả quyền đọc cho Bảo/Quý; thử gán issue chưa được lưu do tài khoản không đủ quyền assignee. Chưa tự thay đổi mức quyền cộng tác. Thông tin tiến độ nằm ở tài liệu mới này; `ui-checklist-inherited.md` phản ánh lịch sử đợt UI cũ.

Đã tạo 13 issue và 3 milestone; liên kết ở [bảng công việc](task-board.md). Repo hiện public theo yêu cầu ngày 20/10/2026. Branch protection chưa được kiểm tra lại sau khi repo chuyển public; xem [phân công](team.md). Có mẫu Dependabot, nhưng PR nâng phiên bản tự động tạm tắt (giới hạn 0), nhóm có thể bật sau khi bắt đầu tích hợp.
