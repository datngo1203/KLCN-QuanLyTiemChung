# KLCN090 · Hệ thống quản lý tiêm chủng

> Phạm vi hiện hành: [docs/SCOPE.md](docs/SCOPE.md), S0–S8; Mobile đóng băng. Chạy nền SQL/API thật theo mục Docker Compose trong [docs/setup.md](docs/setup.md). Nội dung phạm vi cũ bên dưới chỉ là lịch sử nếu xung đột.

Khung dùng chung cho Bảo, Quý, Đạt: **Website React + ứng dụng Windows WPF + Mobile Flutter → ASP.NET Core Web API → SQL Server**.

Repo khởi tạo ngày 19/10/2026. Mobile được bổ sung theo yêu cầu mới của nhóm, giữ cả Website và Windows theo đề cương gốc. Đây là nền để phát triển khóa luận; chưa phải hệ thống quản lý tiêm chủng đã hoàn thiện.

## Bắt đầu

```powershell
git clone https://github.com/datngo1203/KLCN-QuanLyTiemChung.git
cd KLCN-QuanLyTiemChung
powershell -ExecutionPolicy Bypass -File scripts/bootstrap.ps1
```

Mở các terminal riêng:

```powershell
# Backend: http://localhost:5080 — chưa cần SQL khi chạy Development
dotnet run --project backend/src/TiemChung.Api
# Website: http://localhost:5173
npm --prefix apps/web run dev
# Windows: Windows + .NET 10 SDK
dotnet run --project apps/windows/TiemChung.Desktop.csproj
# Mobile: Android emulator + Flutter (xem docs/setup.md cho thiết bị thật/iOS)
cd apps/mobile
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080
```

S1: Windows đăng nhập/đổi mật khẩu/đăng xuất qua Identity API; Web đăng nhập/đăng ký khách hàng/xác minh email/đăng xuất qua API. Token chỉ giữ trong bộ nhớ; seed Admin Development và mail pickup synthetic xem [setup](docs/setup.md). Các form nghiệp vụ chưa tích hợp vẫn dùng trạng thái mẫu. Mobile đóng băng theo SCOPE.

## Phân công

| Thành viên | Đầu mối giao diện | Module backend và tích hợp |
|---|---|---|
| Diệp Nguyễn Gia Bảo | `apps/web` | Hồ sơ, đăng ký, thanh toán, ưu đãi |
| Nguyễn Ngọc Quý | `apps/mobile` | Lịch, lịch sử tiêm, nhắc lịch, theo dõi sau tiêm |
| Ngô Thành Đạt | `apps/windows` | Danh mục, kho, khám/chỉ định, tài khoản/phân quyền; nền API/DB/CI |

Đây là phân công khởi tạo để nhóm bắt đầu; chi tiết phạm vi và người review ở [docs/team.md](docs/team.md). Mỗi người làm cả phần API liên quan đến module mình, không dồn toàn bộ backend cho một thành viên.

## Cấu trúc

```text
apps/web/                    React + Vite; kế thừa UI đã làm
apps/windows/                C# WPF + WPF UI; kế thừa UI đã làm
apps/mobile/                 Flutter Android/iOS
backend/src/TiemChung.Api/    HTTP API, OpenAPI, CORS, xử lý lỗi
backend/src/TiemChung.Domain/ thực thể và quy tắc nghiệp vụ
backend/src/TiemChung.Infrastructure/ EF Core + SQL Server + migrations
backend/src/TiemChung.Contracts/ DTO dùng chung .NET
backend/tests/               unit tests và API smoke tests
contracts/                   danh sách endpoint và mẫu gọi HTTP
database/                    lược đồ SQL sinh từ migration
docs/                        phạm vi, phân công, roadmap, UML, hướng dẫn
scripts/                     cài dependencies, chạy và kiểm tra
infra/                       Docker Compose cho API + SQL Server phát triển
.github/                     CI, mẫu issue/PR, Dependabot
```

## Đọc tiếp

- [Cài đặt và chạy](docs/setup.md), [phân công và cách làm Git](docs/team.md), [backlog](docs/archive/backlog.md), [13 issue trên GitHub](docs/archive/task-board.md).
- [Phạm vi và quyết định nghiệp vụ](docs/project-context.md), [kiến trúc](docs/architecture.md), [hợp đồng API](contracts/README.md).
- [Database](database/README.md), [kiểm thử](docs/testing.md), [trạng thái kiểm chứng](docs/status.md).
- [UML tham khảo](docs/diagrams/README.md), [nguồn và nguồn gốc giao diện](docs/source-map.md).
- [Giới hạn bảo mật trước khi triển khai](SECURITY.md), [cách đóng góp](CONTRIBUTING.md).

Endpoint thực thi xem [contracts](contracts/README.md); OpenAPI Development: `/openapi/v1.json`. Identity kiểm quyền tại backend; Profiles còn giới hạn theo contracts. API tắt dữ liệu mẫu khi chạy Production; Admin demo chỉ seed ở Development theo cấu hình local.
