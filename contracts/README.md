# API dùng chung · v1

Base URL phát triển: `http://localhost:5080`. JSON camelCase, tiền decimal đơn vị VND, ngày `YYYY-MM-DD`, thời điểm ISO 8601 UTC. ID số nguyên; lỗi dùng Problem Details. OpenAPI thực thi: `GET /openapi/v1.json` ở Development.

## Đã có

Đối chiếu `Program.cs`, `IdentityEndpoints.cs`, `ProfileEndpoints.cs`; OpenAPI ở `/openapi/v1.json` trong Development. Các route dưới đây đã được map, không đồng nghĩa Web/Windows đã tích hợp đăng nhập thật: Windows vẫn dùng SampleAuthService, Web chưa có luồng login backend; Mobile đóng băng.

| Method/path | Nội dung | Auth |
|---|---|---|
| GET `/health` | Liveness tiến trình; không xác nhận SQL sẵn sàng | Không |
| GET `/api/v1/catalog/vaccines?search=` | `{ source: "demo" hoặc "database", items: [{ id, name, manufacturer, origin, price }] }`; tối đa 100 mục | Không; danh mục công khai |
| GET `/api/v1/modules` | Module/người phụ trách/trạng thái kế hoạch cũ | Không |
| GET `/openapi/v1.json` | Mô tả endpoint | Không; chỉ Development |
| POST `/api/v1/auth/register` | Đăng ký email/password, trả 202 verificationPending khi mail sink sẵn sàng | Không; rate limit và mail readiness |
| POST `/api/v1/auth/email-verifications/resend` | Gửi lại xác minh | Không; rate limit và mail readiness |
| POST `/api/v1/auth/email-verifications/confirm` | Xác nhận token email | Không; rate limit |
| POST `/api/v1/auth/login` | identifier/password/clientType; cấp access/refresh | Không; kiểm credential, trạng thái và rate limit |
| POST `/api/v1/auth/refresh` | Đổi token phiên | Refresh token; Web thêm cookie, Origin và CSRF |
| POST `/api/v1/auth/logout` | Thu hồi phiên | Refresh token; Web thêm cookie, Origin và CSRF |
| GET `/api/v1/auth/me` | Tài khoản, phạm vi và capabilities hiện hành | Bearer access token |
| GET `/api/v1/customers?search=&page=&pageSize=` | Danh sách khách hàng, có phân trang | Bearer; nhân viên có quyền đọc hoặc khách hàng theo liên kết hồ sơ Active |
| POST `/api/v1/customers` | Tạo khách hàng | Bearer; nhân viên LeTan theo code hiện tại |
| GET `/api/v1/recipients?customerId=&search=&page=&pageSize=` | Danh sách người được tiêm, có phân trang | Bearer; nhân viên có quyền đọc hoặc khách hàng theo liên kết hồ sơ Active |
| POST `/api/v1/recipients` | Tạo người được tiêm gắn khách hàng | Bearer; nhân viên LeTan theo code hiện tại |
| PUT `/api/v1/recipients/{id}` | Route đã có nhưng chưa cập nhật: trả 409 identityApprovalRequired sau kiểm quyền/tồn tại/validation | Bearer; nhân viên LeTan |

Phân quyền backend hiện vẫn dùng chức danh/quyền M1; chưa nghiệm thu mô hình hai vai trò S1. Chưa có endpoint đổi mật khẩu hoặc HTTP quản lý tài khoản. Có endpoint không đồng nghĩa CRUD Profiles đã hoàn tất.

## Dự kiến — chưa có endpoint thực thi

| Module | Endpoint dự kiến | Người làm |
|---|---|---|
| Identity | POST auth/change-password; quản lý accounts/permissions | Đạt |
| Profiles | Hoàn thiện cập nhật hồ sơ và CRUD còn thiếu theo S3; không coi PUT đang trả 409 là đã cập nhật được | Bảo |
| Registration | GET/POST registrations, POST registrations/{id}/items; hỗ trợ lẻ + gói | Bảo |
| Finance | POST registrations/{id}/payments; GET payments; trạng thái đặt cọc/trả góp | Bảo |
| Promotions | GET/POST promotions | Bảo |
| Follow-up | GET/POST appointments; PUT appointments/{id}; GET recipients/{id}/history; POST reminders | Quý |
| Aftercare | GET/POST vaccinations/{id}/follow-ups | Quý |
| Clinical | POST screenings; POST indications; POST vaccinations | Đạt |
| Inventory | CRUD vaccines, packages, batches; POST inventory-movements; GET reports | Đạt |

Endpoint nghiệp vụ chỉ được mở sau khi có xác thực và kiểm tra quyền tại API. Khách hàng chỉ đọc hồ sơ mình/người được tiêm được ủy quyền; vai trò nhân viên tách theo chức danh. Không lấy quyền từ trường do client gửi lên.

Tên endpoint dự kiến có thể đổi khi review. Sửa DTO, lỗi, nullable hoặc ID phải cập nhật cả ba client; ghi thay đổi vào PR và nhờ một người khác review. Thanh toán/nhập kho cần idempotency key và transaction trước khi nối form ghi thật. Không tự kết nối ngân hàng, SMS hoặc gửi email thật.
