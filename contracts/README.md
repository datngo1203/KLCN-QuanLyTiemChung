# API dùng chung · v1

Base URL phát triển: `http://localhost:5080`. JSON camelCase, tiền decimal đơn vị VND, ngày `YYYY-MM-DD`, thời điểm ISO 8601 UTC. ID số nguyên; lỗi dùng Problem Details. OpenAPI thực thi: `GET /openapi/v1.json` ở Development.

## Đã có

Đối chiếu `Program.cs`, `IdentityEndpoints.cs`, `ProfileEndpoints.cs`; OpenAPI ở `/openapi/v1.json` trong Development. S1 nối Web/Windows vào Identity; Mobile đóng băng. Các màn nghiệp vụ mẫu chưa đồng nghĩa đã tích hợp SQL.

| Method/path | Nội dung | Auth |
|---|---|---|
| GET `/health` | Liveness tiến trình; không xác nhận SQL sẵn sàng | Không |
| GET `/api/v1/catalog/vaccines?search=` | `{ source: "demo" hoặc "database", items: [{ id, name, manufacturer, origin, price, vaccineTypeId, injectionTypeId }] }` | Không; danh mục công khai |
| GET `/api/v1/modules` | Module/người phụ trách/trạng thái kế hoạch cũ | Không |
| GET `/openapi/v1.json` | Mô tả endpoint | Không; chỉ Development |
| POST `/api/v1/auth/register` | Đăng ký email/password, trả 202 verificationPending khi mail sink sẵn sàng | Không; rate limit và mail readiness |
| POST `/api/v1/auth/email-verifications/resend` | Gửi lại xác minh | Không; rate limit và mail readiness |
| POST `/api/v1/auth/email-verifications/confirm` | Xác nhận token email | Không; rate limit |
| POST `/api/v1/auth/login` | identifier/password/clientType; cấp access/refresh | Không; kiểm credential, trạng thái và rate limit |
| POST `/api/v1/auth/refresh` | Đổi token phiên | Refresh token; Web thêm cookie, Origin và CSRF |
| POST `/api/v1/auth/logout` | Thu hồi phiên | Refresh token; Web thêm cookie, Origin và CSRF |
| POST `/api/v1/auth/change-password` | `{ currentPassword, newPassword }`; mật khẩu mới 12–128 ký tự, khác hiện tại; trả 204, thu hồi mọi phiên, đăng nhập lại | Bearer; kiểm mật khẩu hiện tại, transaction/hash/audit; sai mật khẩu 401 |
| GET `/api/v1/auth/me` | Tài khoản, phạm vi và capabilities hiện hành | Bearer access token |
| GET `/api/v1/customers?search=&page=&pageSize=` | Danh sách khách hàng, có phân trang | Bearer; nhân viên có quyền đọc hoặc khách hàng theo liên kết hồ sơ Active |
| POST `/api/v1/customers` | Tạo khách hàng | Bearer; nhân viên LeTan theo code hiện tại |
| GET `/api/v1/recipients?customerId=&search=&page=&pageSize=` | Danh sách người được tiêm, có phân trang | Bearer; nhân viên có quyền đọc hoặc khách hàng theo liên kết hồ sơ Active |
| POST `/api/v1/recipients` | Tạo người được tiêm gắn khách hàng | Bearer; nhân viên LeTan theo code hiện tại |
| PUT `/api/v1/recipients/{id}` | Route đã có nhưng chưa cập nhật: trả 409 identityApprovalRequired sau kiểm quyền/tồn tại/validation | Bearer; nhân viên LeTan |

S1 dùng nguyên mô hình chức danh/quyền backend, không làm thêm UI quản trị quyền. Web S1 và Windows dùng `clientType=native` để nhận access/refresh trong response và chỉ giữ trong bộ nhớ; fetch Web dùng `credentials=omit`, không lưu browser storage/cookie. Luồng `clientType=web` cookie/Origin/CSRF hiện có được giữ nguyên. Không phục hồi phiên sau tải lại/đóng ứng dụng; logout gửi refresh token để thu hồi tại API. Chưa có HTTP quản lý tài khoản. Có endpoint không đồng nghĩa CRUD Profiles đã hoàn tất.

Đăng ký demo chỉ nhận địa chỉ `@example.invalid`, vẫn cần xác minh email. Cấu hình Development mail pickup ngoài Git theo `docs/setup.md`; token không được trả qua HTTP hoặc ghi log. Chưa gửi email thật.

Admin đầu tiên: lệnh `--seed-admin` chỉ ở Development, nhận `DevelopmentSeed:Username`/`DevelopmentSeed:Password` từ cấu hình local hoặc biến môi trường; không có credential cố định trong repo. Lệnh tạo Admin/Technical và quyền backend trong transaction, không đổi account/hash/quyền nếu đã có Admin; Production bị từ chối. Cách chạy Compose và sổ credential ngoài Git xem [setup](../docs/setup.md#s1--admin-đầu-tiên-và-đăng-ký-khách-hàng-demo). Đây là lệnh khởi tạo local, không phải endpoint HTTP.

## Dự kiến — chưa có endpoint thực thi

| Module | Endpoint dự kiến | Người làm |
|---|---|---|
| Identity | Quản lý accounts/permissions (hoãn ngoài S1) | Đạt |
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

## S2 — Catalog

Các route `/api/v1/catalog/{injection-types|vaccine-types|vaccines|packages}` hỗ trợ GET danh sách công khai (`{ source: "database", items: [...] }`), POST tạo (201), PUT `/{id}` cập nhật (200), DELETE `/{id}` xóa (204). Không phân trang trong danh mục nhỏ của khóa luận. GET nhận `search`; vaccines nhận thêm `vaccineTypeId`, `injectionTypeId`, `minPrice`, `maxPrice`; packages nhận cùng bộ lọc loại (ít nhất một thành phần khớp cả hai loại nếu chọn) và giá.

- Loại: `{ id, name, description }`; POST/PUT bỏ id. Tên bắt buộc tối đa 120, mô tả tối đa 500 (chuỗi rỗng được phép).
- Vắc xin: `{ id, name, manufacturer, origin, price, vaccineTypeId, injectionTypeId }`; POST/PUT bỏ id. Tên/nhà sản xuất bắt buộc tối đa 150, xuất xứ 100; hai mã loại phải tồn tại.
- Gói: `{ id, name, price, items: [{ vaccineId, quantity, vaccineName }] }` (vaccineName chỉ bổ sung trong GET, POST/PUT chỉ cần vaccineId/quantity); POST/PUT bỏ id. Tên tối đa 150; 1–100 vắc xin khác nhau, quantity 1–100; mã vắc xin phải tồn tại. PUT thay toàn bộ thành phần trong transaction.
- Giá là số nguyên VND không âm, tối đa 9.999.999.999.999.999. Không ghi trường y tế/kho trong S2; giữ dữ liệu trường cũ khi sửa.
- Ghi yêu cầu Bearer của nhân viên Active có `QuanLyDanhMucChuyenMon`; vaccines/packages yêu cầu thêm `QuanLyDanhMucGia` vì form ghi cả giá. API kiểm quyền hiện hành trong transaction; không lấy quyền từ client. Thiếu phiên 401, thiếu quyền 403, đầu vào sai 400 `validation`, không tồn tại 404 `resourceNotFound`, FK ngăn xóa 409 `catalogReferenced`. Xóa gói chỉ xóa thành phần sở hữu; nếu đăng ký đang tham chiếu gói thì rollback toàn bộ. Mỗi ghi thành công có audit người thực hiện/thời điểm.

Seed `--seed-catalog` chỉ ở Development, thêm dữ liệu `[DEMO]` vào SQL thật, không reset hay ghi đè dữ liệu đã có. Mobile đóng băng; trường vaccine DTO mới bổ sung tương thích trường cũ. Contract S2 cần Bảo hoặc Quý review chéo trước merge.
