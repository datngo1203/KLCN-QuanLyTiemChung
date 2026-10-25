# API dùng chung · v1

Base URL phát triển: `http://localhost:5080`. JSON camelCase, tiền decimal đơn vị VND, ngày `YYYY-MM-DD`, thời điểm ISO 8601 UTC. ID số nguyên; lỗi dùng Problem Details. OpenAPI thực thi: `GET /openapi/v1.json` ở Development.

## Đã có

| Method/path | Nội dung | Auth |
|---|---|---|
| GET `/health` | Trạng thái tiến trình API; không kiểm tra DB | Không |
| GET `/api/v1/catalog/vaccines?search=` | `{ source: "demo" hoặc "database", items: [{ id, name, manufacturer, origin, price }] }`; tối đa 100 bản ghi | Không; dữ liệu công khai |
| GET `/api/v1/modules` | Module/người phụ trách/trạng thái planned | Không |

## Dự kiến — chưa có endpoint thực thi

**Đính chính hiện trạng Profiles:** code tại `ProfileEndpoints.cs` đã map GET/POST customers, GET/POST recipients và PUT recipients/{id}, gọi SQL nhưng chưa auth/phạm vi. Không sử dụng với dữ liệu thật. [Gói đặc tả M1 chờ review](m1-identity-profiles.md) đề nghị bảo vệ năm route; chưa sửa code/DTO hoặc triển khai Identity. Dòng Profiles dưới đây giữ để truy vết danh mục kế hoạch, không phải bằng chứng route chưa tồn tại.

| Module | Endpoint dự kiến | Người làm |
|---|---|---|
| Identity | POST auth/login, auth/refresh, auth/logout; POST auth/change-password; quản lý accounts/permissions | Đạt |
| Profiles | GET/POST customers, recipients; PUT recipients/{id} | Bảo |
| Registration | GET/POST registrations, POST registrations/{id}/items; hỗ trợ lẻ + gói | Bảo |
| Finance | POST registrations/{id}/payments; GET payments; trạng thái đặt cọc/trả góp | Bảo |
| Promotions | GET/POST promotions | Bảo |
| Follow-up | GET/POST appointments; PUT appointments/{id}; GET recipients/{id}/history; POST reminders | Quý |
| Aftercare | GET/POST vaccinations/{id}/follow-ups | Quý |
| Clinical | POST screenings; POST indications; POST vaccinations | Đạt |
| Inventory | CRUD vaccines, packages, batches; POST inventory-movements; GET reports | Đạt |

Endpoint nghiệp vụ chỉ được mở sau khi có xác thực và kiểm tra quyền tại API. Khách hàng chỉ đọc hồ sơ mình/người được tiêm được ủy quyền; vai trò nhân viên tách theo chức danh. Không lấy quyền từ trường do client gửi lên.

Tên endpoint dự kiến có thể đổi khi review. Sửa DTO, lỗi, nullable hoặc ID phải cập nhật cả ba client; ghi thay đổi vào PR và nhờ một người khác review. Thanh toán/nhập kho cần idempotency key và transaction trước khi nối form ghi thật. Không tự kết nối ngân hàng, SMS hoặc gửi email thật.
