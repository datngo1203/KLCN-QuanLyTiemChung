# API dùng chung · v1

Base URL phát triển: `http://localhost:5080`. JSON camelCase, tiền decimal đơn vị VND, ngày `YYYY-MM-DD`, thời điểm ISO 8601 UTC. ID số nguyên; lỗi dùng Problem Details. OpenAPI thực thi: `GET /openapi/v1.json` ở Development.

## Đã có

Đối chiếu `Program.cs`, `IdentityEndpoints.cs`, `ProfileEndpoints.cs`; OpenAPI ở `/openapi/v1.json` trong Development. S1 nối Web/Windows vào Identity. Các màn nghiệp vụ mẫu chưa đồng nghĩa đã tích hợp SQL.

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
| GET `/api/v1/customers?search=&page=&pageSize=` | Danh sách khách hàng, có phân trang | Bearer; nhân viên Active hoặc khách hàng theo liên kết hồ sơ Active |
| PUT `/api/v1/customers/{id}` | Cập nhật khách hàng | Bearer; LeTan hoặc chủ hồ sơ Active |
| DELETE `/api/v1/customers/{id}` | Xóa nếu không bị tham chiếu | Bearer; LeTan hoặc chủ hồ sơ Active |
| DELETE `/api/v1/recipients/{id}` | Xóa nếu không bị tham chiếu | Bearer; LeTan hoặc chủ hồ sơ Active |
| POST `/api/v1/customers` | Tạo khách hàng | Bearer; LeTan hoặc khách hàng tạo hồ sơ của chính mình |
| GET `/api/v1/recipients?customerId=&search=&page=&pageSize=` | Danh sách người được tiêm, có phân trang | Bearer; nhân viên Active hoặc khách hàng theo liên kết hồ sơ Active |
| POST `/api/v1/recipients` | Tạo người được tiêm gắn khách hàng | Bearer; LeTan hoặc khách hàng tạo hồ sơ của chính mình |
| PUT `/api/v1/recipients/{id}` | Cập nhật thông tin người được tiêm | Bearer; LeTan hoặc khách hàng có liên kết hồ sơ Active |
| POST `/api/v1/appointments` | Tạo buổi hẹn từ dòng dịch vụ đăng ký; 201 + Location | Bearer; LeTan Active + QuanLyLichHen |
| GET `/api/v1/appointments` | Danh sách lịch SQL đã liên kết nguồn; phân trang/bộ lọc theo S5.1 | Bearer; cùng quyền tạo lịch |
| GET `/api/v1/appointments/{id}` | Chi tiết buổi hẹn, thời gian UTC | Bearer; cùng quyền tạo lịch |
| POST `/api/v1/registrations` | Tạo đầu đăng ký và toàn bộ chi tiết lẻ/gói trong một transaction; 201 + Location | Bearer; Customer có cả hai liên kết Active hoặc nhân viên Active LeTan |
| GET `/api/v1/registrations?customerId=&recipientId=&page=&pageSize=` | Danh sách đăng ký SQL trong phạm vi hiện hành | Bearer; cùng quyền G2 |
| GET `/api/v1/registrations/{id}` | Chi tiết đăng ký, đơn giá đã chốt và thành phần gói | Bearer; cùng quyền G2 |
| GET `/api/v1/payment-registrations?search=&customerId=&recipientId=&page=&pageSize=` | Tra cứu đăng ký SQL và tiền phải thu | Bearer; ThuNgan Active + XemThanhToan |
| GET `/api/v1/registrations/{id}/payment` | Dịch vụ, giá snapshot và kết quả thanh toán mô phỏng | Bearer; ThuNgan + XemThanhToan hoặc Customer đủ ownership G2 |
| POST `/api/v1/registrations/{id}/payment` | JSON `{}`; ghi nhận toàn bộ một lần, 201 + Location | Bearer; ThuNgan Active + XemThanhToan + GhiThanhToan |

S1 dùng nguyên mô hình chức danh/quyền backend, không làm thêm UI quản trị quyền. Web S1 và Windows dùng `clientType=native` để nhận access/refresh trong response và chỉ giữ trong bộ nhớ; fetch Web dùng `credentials=omit`, không lưu browser storage/cookie. Luồng `clientType=web` cookie/Origin/CSRF hiện có được giữ nguyên. Không phục hồi phiên sau tải lại/đóng ứng dụng; logout gửi refresh token để thu hồi tại API. Chưa có HTTP quản lý tài khoản. Profiles S3 đã nối Web/Windows theo mục S3 bên dưới.

G1 sửa giá trị capabilities của `/auth/me` cho khớp API Profiles S3, không thêm trường/endpoint: `profilesRead=true` cho khách hàng đã xác minh và nhân viên Active; `profilesCreate=true` cho khách hàng (tạo hồ sơ bản thân/người được tiêm theo điều kiện S3) hoặc nhân viên `LeTan`; `profilesUpdate=true` cho khách hàng có liên kết Active hoặc nhân viên `LeTan`. Đây là khả năng theo tài khoản, API vẫn kiểm từng hồ sơ/liên kết khi ghi/sửa/xóa; Admin và các chức danh khác không được ghi hồ sơ. `selfClaim`/`representatives` vẫn false. Windows dùng capabilities cho ghi hồ sơ và hai mã quyền danh mục hiện có cho các nút ghi; mỗi lần đổi người dùng tạo shell và phiên bộ nhớ mới.

Đăng ký demo chỉ nhận địa chỉ `@example.invalid`, vẫn cần xác minh email. Cấu hình Development mail pickup ngoài Git theo `README.md` ở gốc dự án; token không được trả qua HTTP hoặc ghi log. Chưa gửi email thật.

Admin đầu tiên: lệnh `--seed-admin` chỉ ở Development, nhận `DevelopmentSeed:Username`/`DevelopmentSeed:Password` từ cấu hình local hoặc biến môi trường; không có credential cố định trong repo. Lệnh tạo Admin/Technical và quyền backend trong transaction, không đổi account/hash/quyền nếu đã có Admin; Production bị từ chối. Cách chạy Compose và sổ credential ngoài Git xem [README](../../README.md#tạo-dữ-liệu-và-tài-khoản-demo). Đây là lệnh khởi tạo local, không phải endpoint HTTP.

G1 thêm `--seed-reception` chỉ ở Development, cùng cấu hình local `DevelopmentSeed:Username`/`Password`: tạo nhân viên `[DEMO]` `LeTan`/Active và tài khoản Employee/None/Active, không có quyền mở rộng, hash/transaction/audit tại backend. Lặp lại theo username chuẩn hóa không đổi hash/trạng thái/dữ liệu; username thuộc chức danh khác bị từ chối. Production/Test không chạy lệnh này; test SQL có fixture riêng. Không đổi schema/migration hoặc quyền endpoint. Lệnh `provision-employee` trong IdentityOperator vẫn yêu cầu bản ghi nhân viên có sẵn và kiểm tra operator/Admin; không thêm HTTP quản trị.

## Dự kiến — chưa có endpoint thực thi

| Module | Endpoint dự kiến | Người làm |
|---|---|---|
| Identity | Quản lý accounts/permissions (hoãn ngoài S1) | Đạt |
| Promotions | GET/POST promotions; giảm tiền VND không âm trong thời gian hiệu lực, một ưu đãi mỗi đăng ký, không cộng dồn; chốt khoản giảm lúc đăng ký, không làm tổng âm | Bảo |
| Follow-up | PUT appointments/{id}; GET recipients/{id}/history; POST reminders (tạo/xem lịch đã có trong S5.1 bên dưới) | Quý |
| Aftercare | GET/POST vaccinations/{id}/follow-ups đã có trong S5.6 bên dưới | Quý |
| Clinical — ghi tiêm | POST vaccinations; kết luận/chỉ định đã có S5.3, ghi tiêm S5.5 bên dưới; không khám chi tiết | Đạt: kết luận/chỉ định; Quý: ghi tiêm |
| Lô/tồn tối thiểu | Đọc lô phù hợp, trừ tồn và ghi biến động cùng thao tác ghi tiêm; nguồn seed Development/Test dự kiến trong G5, không mở module nhập kho | Đạt |
| Reports | GET reports; doanh thu theo tháng, số mũi theo vắc xin, đăng ký theo trạng thái; hoãn xuất file | Đạt |

Endpoint nghiệp vụ chỉ được mở sau khi có xác thực và kiểm tra quyền tại API. Khách hàng chỉ đọc hồ sơ mình/người được tiêm được ủy quyền; vai trò nhân viên tách theo chức danh. Không lấy quyền từ trường do client gửi lên.

Tên endpoint dự kiến có thể đổi khi review. Sửa DTO, lỗi, nullable hoặc ID phải cập nhật cả hai client Web/Windows; ghi thay đổi vào PR và nhờ một người khác review. Thanh toán và ghi tiêm/trừ tồn cần transaction, chống ghi trùng và kiểm thử cạnh tranh/rollback tại nghiệp vụ; hoãn cơ chế idempotency tổng quát. Không tự kết nối ngân hàng, SMS hoặc gửi email thật.

G5 phải kiểm chứng đúng lịch/người/vắc xin, kết luận/chỉ định hợp lệ cho buổi tiêm, nhân viên Active đúng quyền/chức danh, lô phù hợp được phép sử dụng/còn hạn/tồn dương và điều kiện thanh toán. Ghi mũi, trừ tồn, biến động và audit cùng transaction; thiếu bằng chứng phải từ chối, không fallback Sample hoặc tự tạo chỉ định/lô. Seed lô/tồn tương lai chỉ ghi SQL synthetic `[DEMO]`, có số lô/vắc xin/ngày sản xuất/hạn dùng/số lượng ban đầu và tồn/trạng thái; chạy lại không nhân bản, bù tồn đã dùng, đổi hạn dùng hoặc ghi đè. Fixture hợp lệ/hết hạn/hết tồn nằm trong DB test theo [SCOPE](../../SCOPE.md#an-toàn-tối-thiểu-của-s5--g5--chưa-triển-khai).

G1/G2 có thể phát triển song song sau khi contract, quyền, actor và schema liên quan thống nhất và được review chéo; Đạt điều phối migration, không sửa đồng thời. R00 chỉ sửa mô tả phạm vi dự kiến; không đổi contract thực thi, chốt DTO mới hoặc triển khai endpoint/seed. Mỗi gói được giao riêng, xong thì dừng bàn giao.

## S2 — Catalog

Các route `/api/v1/catalog/{injection-types|vaccine-types|vaccines|packages}` hỗ trợ GET danh sách công khai (`{ source: "database", items: [...] }`), POST tạo (201), PUT `/{id}` cập nhật (200), DELETE `/{id}` xóa (204). Không phân trang trong danh mục nhỏ của khóa luận. GET nhận `search`; vaccines nhận thêm `vaccineTypeId`, `injectionTypeId`, `minPrice`, `maxPrice`; packages nhận cùng bộ lọc loại (ít nhất một thành phần khớp cả hai loại nếu chọn) và giá.

- Loại: `{ id, name, description }`; POST/PUT bỏ id. Tên bắt buộc tối đa 120, mô tả tối đa 500 (chuỗi rỗng được phép).
- Vắc xin: `{ id, name, manufacturer, origin, price, vaccineTypeId, injectionTypeId }`; POST/PUT bỏ id. Tên/nhà sản xuất bắt buộc tối đa 150, xuất xứ 100; hai mã loại phải tồn tại.
- Gói: `{ id, name, price, items: [{ vaccineId, quantity, vaccineName }] }` (vaccineName chỉ bổ sung trong GET, POST/PUT chỉ cần vaccineId/quantity); POST/PUT bỏ id. Tên tối đa 150; 1–100 vắc xin khác nhau, quantity 1–100; mã vắc xin phải tồn tại. PUT thay thành phần trong transaction nếu tập ID/số lượng thực sự đổi. G2: nếu đã có `ChiTietGoiVacXin` tham chiếu thì thay thành phần trả 409 `catalogReferenced`, không đổi giá/tên/thành phần một phần; cùng thành phần (không phụ thuộc thứ tự) vẫn cho đổi giá/tên. Catalog PUT và Registration khóa cùng bản ghi gói trước khi kiểm tra/cập nhật, giữ tới commit.
- Giá là số nguyên VND không âm, tối đa 9.999.999.999.999.999. Không ghi trường y tế/kho trong S2; giữ dữ liệu trường cũ khi sửa.
- Ghi yêu cầu Bearer của nhân viên Active có `QuanLyDanhMucChuyenMon`; vaccines/packages yêu cầu thêm `QuanLyDanhMucGia` vì form ghi cả giá. API kiểm quyền hiện hành trong transaction; không lấy quyền từ client. Thiếu phiên 401, thiếu quyền 403, đầu vào sai 400 `validation`, không tồn tại 404 `resourceNotFound`, FK ngăn xóa 409 `catalogReferenced`. Xóa gói chỉ xóa thành phần sở hữu; nếu đăng ký đang tham chiếu gói thì rollback toàn bộ. Mỗi ghi thành công có audit người thực hiện/thời điểm.

Seed `--seed-catalog` chỉ ở Development, thêm dữ liệu `[DEMO]` vào SQL thật, không reset hay ghi đè dữ liệu đã có. Trường vaccine DTO mới bổ sung tương thích trường cũ. Contract S2 cần Bảo hoặc Quý review chéo trước merge.

## S3 — Profiles

Tái sử dụng Profiles M1 và AccountProfileLink. Nhân viên Active đọc được tất cả hồ sơ; ghi nhân viên vẫn yêu cầu chức danh LeTan. Khách hàng chỉ đọc/sửa/xóa hồ sơ có liên kết Active của chính tài khoản. Không tra cứu hoặc tự nhận hồ sơ cũ bằng CCCD/email.

- POST `/customers`: `{ fullName, identityNumber?, phoneNumber, email }`; khách hàng chưa có hồ sơ được tạo một hồ sơ và liên kết Active trong cùng transaction. Đã có trả 409 `profileAlreadyExists`. PUT `/customers/{id}` cùng body, trả CustomerDto. CCCD nullable/rỗng được lưu NULL; nếu có phải đúng 12 chữ số ASCII và duy nhất giữa khách hàng. Tên bắt buộc tối đa 150, điện thoại 10–11 chữ số, email hợp lệ tối đa 254.
- POST `/recipients`: `{ customerId, fullName, identityNumber?, dateOfBirth, gender, relationshipToCustomer }`. Khách hàng phải sở hữu customerId; API tạo liên kết Active cho bé mới. Có thể tạo nhiều bé. PUT `/recipients/{id}` cùng body bỏ customerId, không chuyển chủ hồ sơ. Ngày sinh YYYY-MM-DD không trong tương lai; gender Nam/Nữ/Khác; quan hệ bắt buộc tối đa 50. Không thêm quy tắc người bảo trợ.
- DELETE `/customers/{id}` và `/recipients/{id}`: 204; xóa liên kết cùng transaction. FK nghiệp vụ đang tham chiếu chặn xóa với 409 `profileReferenced`, rollback giữ hồ sơ/liên kết. Không xóa dây chuyền hồ sơ bé hay lịch sử tiêm.
- Danh sách giữ phân trang page/pageSize (tối đa 100); chỉ liên kết người được tiêm trực tiếp cho phép khách hàng xem bé, không tự nhận các hồ sơ bé đã có từ liên kết khách hàng.
- Ghi kiểm phiên/quyền/phạm vi ở API trong transaction, lưu audit người thực hiện/thời điểm. Thiếu phiên 401, trái phạm vi/quyền 403, dữ liệu sai 400 `validation`, thiếu hồ sơ 404, CCCD khách hàng trùng 409 `duplicateIdentity`.

Web: Tài khoản → Hồ sơ của tôi, tạo/sửa hồ sơ và người được tiêm. Windows: Hồ sơ → tải/tìm, tạo/sửa/xóa khách hàng và bé qua API thật. Nullable CustomerDto.IdentityNumber là thay đổi contract cần review chéo trước merge.

## G2B — Registration (backend, chưa nối Web/Windows)

Một đăng ký gồm một khách hàng, một người được tiêm và 1–100 dòng vắc xin lẻ/gói. Không mở sửa/xóa/hủy/chuyển trạng thái hoặc `POST registrations/{id}/items`. Không có giỏ hàng SQL, giữ tồn, hàng đợi, lịch, thanh toán, khám, ghi tiêm hoặc ưu đãi trong G2. Thành công chỉ có nghĩa đã lưu đăng ký, không có nghĩa đủ điều kiện tiêm.

### Tạo đăng ký

`POST /api/v1/registrations` nhận:

```json
{
  "customerId": 12,
  "recipientId": 34,
  "items": [
    { "kind": "vaccine", "serviceId": 5, "quantity": 2, "expectedUnitPrice": 150000 },
    { "kind": "package", "serviceId": 3, "quantity": 1, "expectedUnitPrice": 350000,
      "expectedComponents": [ { "vaccineId": 5, "quantity": 1 }, { "vaccineId": 6, "quantity": 1 } ] }
  ]
}
```

- ID nguyên dương; `kind` chỉ `vaccine` hoặc `package`; số lượng nguyên 1–100. Không trùng `(kind, serviceId)` trong request. Không tự gộp/xóa dịch vụ chỉ vì một vắc xin cũng nằm trong gói.
- `expectedUnitPrice` bắt buộc kể cả giá 0, số nguyên VND từ 0 đến 9.999.999.999.999.999. Tổng tiền cũng không vượt giới hạn này. Các giới hạn số dòng/số lượng là giới hạn kỹ thuật của request, không phải phác đồ hoặc điều kiện y khoa.
- Dòng gói bắt buộc `expectedComponents`: 1–100 vaccine ID khác nhau, quantity 1–100; so sánh tập ID/số lượng không phụ thuộc thứ tự. Dòng lẻ bỏ trường này hoặc gửi null. Thuộc tính JSON không được định nghĩa bị từ chối ở cả request, dòng và thành phần.
- Các trường expected chỉ kiểm nội dung người dùng đã xem. API đọc giá/trạng thái/thành phần SQL trong transaction; giá gói lấy `GiaGoi`, không cộng giá thành phần. Giá/thành phần khác expected trả 409 `catalogChanged`; không tạo đăng ký. Client tải lại danh mục, giữ bản nháp và cho người dùng xem/xác nhận lại; không tự retry POST khi timeout.
- Dịch vụ và vắc xin thành phần phải tồn tại, có trạng thái `Active`; gói không rỗng, số lượng thành phần hợp lệ. Không kiểm tồn/tuổi/phác đồ/chỉ định trong G2. Danh mục mẫu qua `Catalog:UseDemoData` không được dùng làm nguồn đăng ký; POST luôn đọc SQL, không fallback.
- Server tự gán `id`, `createdAt` UTC, `status="DaDangKy"`, `employeeId` từ phiên nhân viên hoặc null với Customer. `HinhThucTiem`, `ThoiGianDuKien`, `MaKhuyenMai` để NULL. Không nhận nhân viên, quyền, trạng thái, tổng tiền, thời điểm, hồ sơ mới hoặc ưu đãi từ request.
- Đầu đăng ký, hai loại chi tiết có `GiaTaiThoiDiemDangKy` và `SecurityAudit` (`registrations.created`, ActorAccountId, SessionId, OccurredAt, ResourceType/ResourceId) commit cùng nhau. Lỗi xác thực/validation/danh mục/ghi/audit rollback toàn bộ. Dùng transaction Serializable hiện có; gói được khóa theo thứ tự ID và Catalog PUT dùng cùng khóa gói, giá vắc xin được giữ ổn định tới commit. Chưa có idempotency tổng quát.

201 trả `Location: /api/v1/registrations/{id}` và `RegistrationDto`:

```text
{ id, createdAt, status,
  customer: { id, fullName }, recipient: { id, fullName }, employeeId: int|null,
  items: [{ id, kind, serviceId, name, quantity, unitPrice, lineTotal,
            components: null | [{ vaccineId, quantity, vaccineName }] }],
  subtotal, currency: "VND" }
```

`id` của dòng là khóa chi tiết tương ứng; cần đi cùng `kind` vì hai bảng có thể trùng ID số. Các dòng trả lẻ trước, gói sau, mỗi loại theo ID tăng dần. `unitPrice` đọc giá đã lưu; `lineTotal=quantity*unitPrice`; `subtotal` cộng các dòng, không lấy giá Catalog mới. `components.quantity` là số lượng trong **một** gói. Tên khách hàng/người tiêm/dịch vụ vẫn là tên hiện hành, không có snapshot tên. Thành phần gói được bảo toàn bằng chặn sửa gói đã tham chiếu, không thêm bảng snapshot.

### Đọc và phân quyền

- Customer: tài khoản Active, email đã xác minh, phiên hợp lệ; cần liên kết `AccountProfileLink` Active trực tiếp với **cả** Customer và Recipient, và `NguoiDuocTiem.MaKhachHang` đúng khách hàng đăng ký. Không tự nhận hồ sơ bằng CCCD/email, không suy quyền từ liên kết Customer sang mọi Recipient.
- Nhân viên Active chức danh `LeTan` được tạo/xem mọi đăng ký phục vụ tiếp nhận; các chức danh khác, kể cả Admin, bị 403. Không thêm mã quyền/capabilities Identity. Client dùng `accountType`/`employee.title` hiện có để thể hiện UI; API vẫn kiểm hiện hành.
- Quyền xem theo liên kết hiện tại, không theo tài khoản từng tạo: khách hàng đủ liên kết xem được đăng ký do Lễ tân tạo; mất một liên kết thì đăng ký biến mất khỏi danh sách, detail bị từ chối.
- GET danh sách trả `{ items: [{ id, createdAt, status, customer, recipient, employeeId, subtotal, currency }] }`. Bộ lọc customerId/recipientId chỉ thu hẹp phạm vi. Mặc định page=1/pageSize=25, pageSize tối đa 100, thứ tự ID giảm dần; trang rỗng trả `items: []`. Không trả count tổng hoặc chi tiết dịch vụ trong danh sách.
- GET detail trả cùng `RegistrationDto` như POST. Customer gặp ID không tồn tại hoặc ngoài phạm vi đều nhận 403 `permissionDenied` để không phân biệt sự tồn tại. Lễ tân gặp ID không tồn tại nhận 404. ID/query không hợp lệ trả 400. Response Registration đặt `Cache-Control: no-store`.

Lỗi dùng `{ type, title, status, code, traceId, errors? }`: 400 `validation` (kể cả JSON sai/trường lạ), 401 `invalidSession`, 403 `permissionDenied`/`emailVerificationRequired`, 404 `resourceNotFound` (hồ sơ/dịch vụ thiếu trong phạm vi được phép), 409 `catalogChanged`; lỗi phụ thuộc SQL/ghi dữ liệu trả 503 `dependencyUnavailable` không chứa SQL/PII/credential. Không tạo audit thành công ngoài transaction đã rollback.

### Schema và ranh giới tích hợp

Migration mới `20261028172727_G2RegistrationNullableFields` chỉ cho phép NULL ở ba cột `DangKyDichVu.MaNhanVien`, `HinhThucTiem`, `ThoiGianDuKien`, giữ FK và dữ liệu cũ; không thêm bảng/cột khác. Nâng cấp qua EF migrations trên DB test riêng; không tự áp dụng database demo. Down từ chối nếu còn bản ghi có NULL ở ba cột, không điền nhân viên/thời gian/chuỗi giả để rollback.

G3 dùng giá chi tiết đã chốt cho thanh toán một lần có nhãn mô phỏng; tổng 0 được phép ở G2 nhưng không tạo thanh toán 0. G4 dùng ID đăng ký/chi tiết và thời gian nhân viên nhập; chưa sửa `LichHenTiem` trong G2. G5 tự kiểm chỉ định/thanh toán/lô/tồn/quyền khi ghi tiêm. G6 sẽ thêm chọn ưu đãi và snapshot giảm VND lúc tạo; không hồi tố giá/ưu đãi cho đăng ký G2 cũ. G2B chưa thay UI hoặc đánh dấu S4 hoàn tất; contract/schema vẫn cần review chéo trước merge.

## G3B — Thanh toán toàn bộ một lần (mô phỏng)

Chỉ ghi nhận trên SQL để demo khóa luận, không xử lý hoặc xác nhận tiền thật. Không có đặt cọc, trả góp, thu một phần, hoàn tiền, cổng ngân hàng, hóa đơn thật, SMS hoặc hàng đợi. G3B chỉ nối backend; Web/Windows Payment vẫn là màn mẫu cho tới gói client tương ứng. Không có alias `/payments` hoặc đường dẫn `registrations/{id}/payments`.

### Quyền và ownership

- Mọi endpoint kiểm phiên/tài khoản/nhân viên hiện hành bằng Identity. Nhân viên phải Active. GET danh sách chỉ cho `ThuNgan` có `XemThanhToan`; GET chi tiết cũng cho Customer có email xác minh và đủ cả hai liên kết `AccountProfileLink` Active, đúng cặp Customer/Recipient theo G2.
- POST chỉ cho `ThuNgan` có đồng thời `XemThanhToan` và `GhiThanhToan`. Thu ngân chỉ có quyền xem vẫn tra cứu và đọc chi tiết, `canRecordPayment=false`. Admin, LeTan và chức danh khác bị 403 kể cả có các mã quyền này. Không thay seed/quyền/capabilities Identity hoặc quyền endpoint đăng ký G2.
- Customer không được gọi danh sách Thu ngân hoặc POST. GET chi tiết ngoài ownership và ID không tồn tại đều trả 403 `permissionDenied`; Thu ngân đủ quyền gặp ID không tồn tại nhận 404 `resourceNotFound`. Thu hồi một trong hai liên kết là mất quyền xem, không dựa vào người đã tạo đăng ký hay CCCD/email.
- Tất cả response tài chính, kể cả lỗi binding/validation, có `Cache-Control: no-store`.

### Tra cứu và DTO

`GET /api/v1/payment-registrations?search=&customerId=&recipientId=&page=&pageSize=`: search sau trim tối đa 100 ký tự, tìm mã đăng ký số hoặc `DK-0001`, tên Customer/Recipient hiện hành; bộ lọc customerId/recipientId chỉ thu hẹp kết quả. ID bộ lọc phải dương; page mặc định 1, pageSize mặc định 25, tối đa 100; chặn skip vượt int. Thứ tự ID giảm dần, trang rỗng trả `items: []`; không có tổng count.

```text
PaymentRegistrationListResponse = { items: PaymentRegistrationSummaryDto[] }
PaymentRegistrationSummaryDto = {
  id, createdAt, customer: { id, fullName }, recipient: { id, fullName },
  subtotal, totalDue, paidAmount, remainingAmount, paymentId: int|null, currency: "VND"
}
```

`GET /api/v1/registrations/{id}/payment` trả 200 `RegistrationPaymentDto`; đăng ký chưa có giao dịch vẫn trả 200 với `payment:null`:

```text
RegistrationPaymentDto = {
  registrationId, registrationStatus, customer, recipient,
  items: [{ id, kind, serviceId, name, quantity, unitPrice, lineTotal, components }],
  subtotal, totalDue, paidAmount, remainingAmount, currency: "VND",
  requiresPayment, canRecordPayment, isSimulation: true, message,
  payment: null | PaymentDto
}
PaymentDto = {
  id, registrationId, amount, employeeId, occurredAt,
  type: "ToanBo", method: "MoPhong", status: "ThanhCong", isSimulation: true
}
```

`customer`/`recipient` và `items` dùng DTO G2, cùng quy tắc ID dòng phải đi với kind, giá snapshot và thành phần gói. Tên là tên hiện hành, không tạo snapshot tên mới. `createdAt`/`occurredAt` là ISO UTC. Tiền là JSON decimal VND nguyên, từ 0 đến `9999999999999999` như G2; client Web phải giữ chính xác các trường tiền lớn hơn Number.MAX_SAFE_INTEGER.

Server tính `subtotal = Σ(SoLuong*GiaTaiThoiDiemDangKy) + Σ(SoLuongGoi*GiaTaiThoiDiemDangKy)`, không cộng lại giá thành phần gói, không đọc giá Catalog hiện hành. G3 có `totalDue=subtotal`; `paidAmount` là số tiền của một giao dịch G3 hợp lệ hoặc 0, `remainingAmount=totalDue-paidAmount`. `requiresPayment` chỉ true khi còn phải thu; `canRecordPayment` còn yêu cầu đúng quyền ghi. Hai cờ đều false sau thanh toán hoặc khi tổng 0.

Tổng 0: subtotal/totalDue/paidAmount/remainingAmount đều 0, payment=null, requiresPayment=false, canRecordPayment=false, message="Không phát sinh khoản thu."; không gọi đây là đã thanh toán. Nhãn mô phỏng có ở isSimulation, giao dịch và message. Không có trạng thái giao dịch Đặt cọc/Trả góp/Đang xử lý/Thất bại.

### Ghi nhận, cạnh tranh và lỗi

`POST /api/v1/registrations/{id}/payment` bắt buộc JSON object `{}`; không nhận thuộc tính khác (bao gồm amount, employeeId, status, method, type, discount, paidAt). Thành công trả 201, Location `/api/v1/registrations/{id}/payment` và `RegistrationPaymentDto` sau ghi. ID số phải dương; route `{id:int}` không khớp chuỗi không phải int.

Transaction Serializable dùng lại `IdentityTransactions`: kiểm/khóa account → session, quyền/nhân viên hiện hành → `DangKyDichVu WITH (UPDLOCK,HOLDLOCK)` → kiểm đúng cặp hồ sơ, trạng thái `DaDangKy`, chưa có ưu đãi chưa snapshot, 1–100 dòng, số lượng 1–100 và giá VND hợp lệ → kiểm giao dịch cũ → ghi ThanhToan → ghi SecurityAudit → commit. Không cập nhật trạng thái đăng ký hoặc tạo lịch/chỉ định/mũi tiêm. SecurityAudit có action `payments.recorded`, outcome `Success`, ResourceType `Payment`, ResourceId là payment ID, ActorAccountId/SessionId từ phiên. Lỗi thanh toán hoặc audit trước commit rollback cả hai.

Giao dịch cũ phải đúng chính xác loại/phương thức/trạng thái G3 và SoTien dương bằng toàn bộ tổng snapshot. Giao dịch cũ không tương thích, tổng vượt giới hạn/không nguyên, đăng ký thiếu dòng/sai cặp/trạng thái ngoài G2 hoặc có MaKhuyenMai chưa snapshot: từ chối 409 `paymentDataConflict`, không coi đã thanh toán, không ghi tiếp hoặc sửa dữ liệu. Danh sách có dòng mâu thuẫn trong trang cũng trả 409, không trả số tiền suy đoán hoặc bỏ qua dòng lỗi.

Đã có giao dịch hợp lệ trả 409 `paymentAlreadyRecorded`; tổng 0 trả 409 `paymentNotRequired`. Hai Thu ngân cùng ghi được tuần tự hóa theo khóa đăng ký; unique `IX_ThanhToan_MaDangKy` bảo vệ cuối cùng. Chỉ SqlException 2601/2627 xác định đúng index này được ánh xạ lỗi trùng; lỗi DbUpdateException khác trả 503 `dependencyUnavailable`.

Lỗi có `{ type, title, status, code, traceId, errors? }`: 400 `validation` cho query/ID/body sai; 401 `invalidSession`; 403 `permissionDenied`/`emailVerificationRequired`; 404 `resourceNotFound` theo quyền trên; 409 các mã tiền vừa nêu; 503 `dependencyUnavailable` cho phụ thuộc SQL/ghi/audit. Không trả SQL, credential/token hoặc thông tin hồ sơ qua lỗi.

Không retry POST tự động. Mất phản hồi/timeout sau commit là kết quả chưa xác định; G3C/G3D phải dùng GET theo registration ID để đối chiếu, giữ nguyên lựa chọn và chặn gửi trùng. Nếu đã nhận 201 thì giao dịch đã commit; GET tải mới lỗi không có nghĩa thanh toán thất bại. Không thêm idempotency tổng quát hoặc bảng theo dõi retry.

### Migration và ranh giới

`20261029000847_G3SinglePayment` chỉ đổi index `ThanhToan.MaDangKy` thành unique; giữ FK, CHECK SoTien>0 và toàn bộ dữ liệu. Preflight trước DDL từ chối nhiều thanh toán cùng đăng ký hoặc thanh toán cũ sai contract/số tiền/đăng ký snapshot. Không tự xóa/gộp/đổi trạng thái/backfill; lỗi yêu cầu review dữ liệu trước triển khai. Down chỉ trả index thường. Kiểm Up/Down/Up và lặp migration trên DB test riêng; không tự áp DB demo.

G5 với tổng dương phải kiểm giao dịch G3 hợp lệ, toàn bộ số tiền và đúng đăng ký; `DaDangKy` hay remainingAmount=0 không đủ làm bằng chứng. Điều kiện tiền khi tổng 0 chờ quyết định ở G5, không tự coi đủ điều kiện tiêm. G6 sẽ snapshot khoản giảm lúc đăng ký; G3 không đọc MucUuDai hiện hành, áp ưu đãi hoặc hồi tố giá/khoản giảm.


## S5.1 — Lịch hẹn từ dịch vụ đã chốt (Windows)

Chỉ tạo/xem buổi hẹn; không đồng nghĩa đủ điều kiện tiêm. Không tự bung số lượng/gói thành mũi, suy phác đồ, kiểm tiền trước khi xếp hẹn hoặc tạo chỉ định/lô. Không đổi/hủy lịch, ghi tiêm hoặc nhắc lịch. S5.2 mở Customer GET theo ownership bên dưới.

- POST yêu cầu phiên/tài khoản/nhân viên Active, chức danh `LeTan` và mã quyền hiện hữu `QuanLyLichHen`. GET cho Lễ tân cùng quyền hoặc Customer theo S5.2. Không cấp quyền qua seed. Admin và chức danh khác bị 403 dù có mã quyền. Customer không được POST/PUT/DELETE lịch. Chế độ xem chi tiết trên WPF là read-only, tạo là tác vụ riêng.
- `POST /api/v1/appointments`: `{ registrationId, itemKind: "vaccine"|"package", registrationItemId, scheduledAt, location }`. ID nguyên dương; registrationItemId là **ID dòng chi tiết**, không phải ID danh mục. Từ chối trường JSON lạ. API lấy người tiêm từ đăng ký, kiểm đúng cặp hồ sơ, trạng thái `DaDangKy` và dòng thuộc đăng ký; không nhận người tiêm/actor/status từ client.
- scheduledAt bắt buộc ISO `YYYY-MM-DDTHH:mm:ss[.fffffff]Z` hoặc có offset `±HH:mm`; sau thời điểm API nhận tạo, chuẩn hóa UTC chính xác tới 7 chữ số thập phân. Không áp giờ mở cửa/phác đồ mẫu. location sau trim dài 1–200. Trạng thái server gán `DaXepLich`.
- 201 + Location `/api/v1/appointments/{id}`, trả `AppointmentDto = { id, registrationId, customer: { id, fullName }, recipient: { id, fullName }, itemKind, registrationItemId, serviceId, serviceName, scheduledAt, location, status }`. GET trả cùng DTO; thời gian UTC `+00:00`, tên hiện hành. Không trả giá/thông tin khám.
- `GET /api/v1/appointments?registrationId=&recipientId=&page=&pageSize=` trả `{ items: AppointmentDto[] }`, ID giảm dần, mặc định 1/25, tối đa 100; query chỉ thu hẹp kết quả, không có tổng count. Đối chiếu POST dùng thêm `itemKind` + `registrationItemId` (đi cùng nhau) và `scheduledAt` bằng thời điểm UTC chính xác. Bộ lọc thời gian cho phép lịch quá khứ để đối chiếu.
- `GET /api/v1/appointments/{id}`: 200; Lễ tân đủ quyền gặp ID không tồn tại nhận 404 `resourceNotFound`. Customer gặp ID không tồn tại hoặc ngoài phạm vi đều nhận 403 `permissionDenied`. Lịch legacy thiếu nguồn giữ nguyên trong SQL, không xuất hiện trong API S5.1 và không được tự đoán nguồn.
- Ghi lịch và audit `appointments.created` / ResourceType `Appointment`, ResourceId ID lịch, actor/session/thời điểm thật cùng transaction Serializable. Khóa đăng ký tuần tự hóa yêu cầu; hai unique filtered indexes chặn cùng dòng tại cùng thời điểm UTC, cho phép đặt thời điểm khác. Không tạo payment/mũi/nhắc lịch hoặc đổi trạng thái đăng ký.
- Problem Details có code/traceId: 400 `validation`, 401 `invalidSession`, 403 `permissionDenied`, 404 `resourceNotFound` (đăng ký/lịch thiếu), 409 `appointmentAlreadyExists` hoặc `appointmentSourceConflict` (trạng thái/cặp hồ sơ nguồn không hợp lệ), 503 `dependencyUnavailable`. Mọi response kể cả lỗi binding có `Cache-Control: no-store`; không lộ SQL/PII trong lỗi.
- POST không retry tự động. Khi mất response/503 hoặc đọc 201 lỗi, giữ nguyên request và khóa tạo mới; GET với đúng nguồn/thời điểm trước khi thử lại. Nếu GET chưa thấy thì retry thủ công **cùng request**; unique DB vẫn bảo vệ kết quả POST cũ đến muộn. Đã nhận 201 không POST lại, chỉ tải lại bằng GET.
- Migration `S5AppointmentSource` thêm ba cột nullable `MaDangKy`, `MaChiTietVacXin`, `MaChiTietGoi`; CHECK cho phép toàn NULL (legacy) hoặc đúng một dòng chi tiết; FK ghép bảo đảm cùng đăng ký và người tiêm. Không backfill/xóa dữ liệu. Down từ chối khi còn lịch đã liên kết. Chỉ áp dụng trên DB test riêng trong nghiệm thu; DB demo cần giao triển khai riêng.

### S5.2 — Website khách hàng chỉ đọc lịch hẹn

- Identity kiểm tài khoản Customer Active, email đã xác minh, phiên chưa hết hạn/thu hồi và SecurityVersion hiện hành. Ownership dùng lại `RegistrationEndpoints.Scope`: AccountProfileLink Active trực tiếp tới cả Customer và Recipient của đăng ký nguồn, Recipient thuộc đúng Customer; người tiêm trên lịch phải trùng đăng ký. Không dựa vào tài khoản từng tạo đăng ký hoặc CCCD.
- SQL lọc phạm vi trước phân trang và trước projection DTO; lịch thiếu MaDangKy không xuất hiện. Mọi bộ lọc hiện có chỉ thu hẹp kết quả; accountId/ownerId từ client không cấp quyền. Danh sách không có lịch trả `{ items: [] }`. Giữ `Cache-Control: no-store` và Problem Details, không đổi DTO hoặc schema.
- Website dùng GET danh sách/chi tiết qua Identity client, token chỉ trong bộ nhớ, credentials omit/cache no-store. Danh sách → chi tiết, phân trang/tải lại, giờ địa phương từ UTC API; không có tra cứu bằng CCCD, lịch sử tiêm giả hoặc thao tác ghi lịch. Đổi phiên xóa dữ liệu cũ và loại bỏ phản hồi trễ; tải lại trang cần đăng nhập lại.

## S5.3 — Kết luận và chỉ định theo lịch hẹn (Windows A02)

Chỉ tài khoản Employee có phiên hợp lệ, account/nhân viên Active và chức danh BacSi được GET/POST. Identity kiểm phiên/security version hiện hành; không thêm mã quyền. Customer, Admin và chức danh khác bị từ chối. Không phải hàng đợi đã đến khám, không xác nhận đã tiêm.

- `GET /api/v1/clinical/appointments?search=&page=&pageSize=`: `{ items: AppointmentDto[] }` như S5.1, nguồn đăng ký DaDangKy đúng khách hàng/người tiêm/dòng lẻ hoặc gói có thành phần. Search tối đa 100 ký tự sau trim, tìm tên người tiêm hoặc mã lịch số/LH-0001. Page mặc định 1/25, pageSize tối đa 100, ID giảm dần. Chỉ thu hẹp SQL trước phân trang, không hiển thị legacy thiếu nguồn.
- `GET /api/v1/clinical/appointments/{id}`: `{ appointment: AppointmentDto, allowedVaccines: [{ id, name }] }`. Lẻ chỉ đúng vaccine của dòng; gói chỉ thành phần GoiVacXin_VacXin của gói trên dòng đăng ký. Không tự chọn thành phần hoặc suy số mũi.
- `POST /api/v1/clinical/appointments/{id}/assessment`: `{ conclusion, reason: string|null, vaccineIds: int[] }`. Conclusion đúng một trong `Đủ điều kiện`, `Tạm hoãn`, `Chống chỉ định`, do bác sĩ chọn. Đủ điều kiện yêu cầu 1–100 vaccine ID nguyên dương, không trùng, thuộc allowedVaccines; hai kết luận còn lại yêu cầu lý do 1–500 ký tự sau trim và vaccineIds rỗng. Reason nếu cung cấp tối đa 500. Từ chối trường lạ; không nhận actor, người tiêm, thời điểm hoặc dữ liệu sinh tồn từ client.
- Thành công 201 + Location tới `/api/v1/clinical/appointments/{id}/assessment`, trả ClinicalAssessmentDto `{ id, appointmentId, registrationId, recipientId, doctor: {id, fullName}, recordedAt, conclusion, reason, vitalSigns, medicalHistory, indications: [{ id, vaccineId, vaccineName, result, recordedAt }] }`. Server đặt bác sĩ/thời điểm UTC, kết quả chỉ định được chọn là `DuocTiem`. Hai kết luận khác không tạo dòng chỉ định. Sinh tồn/tiền sử không thu thập ở S5.3 nên NULL, không suy là bình thường.
- `GET /api/v1/clinical/appointments/{id}/assessment`: `{ assessment: ClinicalAssessmentDto|null }`; null nghĩa lịch hợp lệ chưa có kết luận. Không mở đọc phiếu legacy không gắn lịch hoặc phiếu của Customer. Không sửa/xóa kết luận.
- Ghi trong transaction Serializable: khóa account/session qua Identity, khóa lịch rồi kiểm nguồn hiện hành, chặn phiếu đã có, ghi phiếu + chỉ định + audit `clinical.assessed` (ResourceType `ClinicalAssessment`, ID phiếu) cùng commit. Unique MaLichHen và unique (MaPhieuKham,MaVacXin) bảo vệ ghi trùng. Không đổi lịch/đăng ký/thanh toán, không ghi MuiTiem/LoVacXin/BienDongKho.
- Problem Details/no-store: 400 validation, 401 invalidSession, 403 permissionDenied, 404 resourceNotFound cho lịch thiếu/legacy/nguồn không hợp lệ, 409 assessmentAlreadyRecorded cho phiếu đã tồn tại, 503 dependencyUnavailable. Nội dung kết luận/lý do không đưa vào lỗi/log. POST không retry tự động. Mất phản hồi/503 phải giữ đúng request, khóa gửi mới và GET đối chiếu; nếu GET đã có thì chỉ đọc, nếu chưa có mới cho retry thủ công cùng request. Đã nhận 201 thì chỉ GET, không POST lại.
- Migration mới S5ClinicalAssessment: MaLichHen/ThoiDiemKham nullable để giữ nguyên legacy, NULL sinh tồn/tiền sử, unique indexes và SQL composite FK (MaLichHen,MaDangKy,MaNguoiDuocTiem) tới lịch. FK SQL viết tường minh để giữ MaDangKy của lịch legacy nullable. Không backfill. Nếu chỉ định legacy trùng (phiếu,vaccine), Up từ chối để review dữ liệu, không xóa/gộp. Down từ chối nếu còn kết luận mới hoặc NULL sinh tồn/tiền sử; không điền giả. Chỉ migrate DB test mới trong gói này.

## S5.5 — Điều dưỡng ghi nhận một mũi đã thực hiện (Windows A06)

Chỉ Employee có phiên Identity hiện hành, account/nhân viên Active và đúng chức danh `DieuDuong` được GET/POST. Danh sách 12 quyền hiện hữu không có quyền ghi tiêm riêng; không dùng mã quyền không liên quan, không thêm/gán quyền mới. Customer và mọi chức danh khác (kể cả Admin/BacSi) bị từ chối. Đây là ghi nhận đã thực hiện, không cấp phép tiêm trên lâm sàng; bác sĩ chịu trách nhiệm kết luận/chỉ định S5.3.

- `GET /api/v1/vaccinations/eligible?search=&page=&pageSize=` trả `{ items: VaccinationIndicationDto[] }`: chỉ định `DuocTiem`, phiếu `Đủ điều kiện`, đúng lịch/nguồn lẻ hoặc thành phần gói và chưa có mũi. Search sau trim tối đa 100 ký tự, tìm tên người tiêm hoặc mã lịch số/LH-0001; page mặc định 1/25, pageSize tối đa 100, ID chỉ định giảm dần. Đây là danh sách để tiếp tục đối chiếu; không khẳng định đã đủ tiền hoặc còn lô. Không trả lý do khám/tiền sử/sinh tồn.
- `VaccinationIndicationDto = { id, appointmentId, registrationId, customer:{id,fullName}, recipient:{id,fullName}, vaccine:{id,name}, scheduledAt, location, doctor:{id,fullName}, assessedAt }`. Giờ API là UTC; không có số thứ tự mũi suy đoán.
- `GET /api/v1/vaccinations/indications/{id}` trả `{ indication: VaccinationIndicationDto, facilityDate, lots:[{number,manufacturedOn,expiresOn,stock}] }`. Kiểm nguồn/chỉ định/thanh toán hiện hành, chỉ trả lô cùng vắc xin, Active, ngày sản xuất không ở tương lai/không sau hạn dùng, hạn dùng >= facilityDate, ban đầu dương, 0 < tồn <= ban đầu. Không tự chọn FEFO. Không có lô trả mảng rỗng. Ngày cơ sở do server tính cố định UTC+07:00 (Việt Nam); hạn dùng đúng ngày cơ sở vẫn hợp lệ. UI không tự xác nhận điều kiện thay API.
- `POST /api/v1/vaccinations`: đúng ba trường `{ indicationId, lotNumber, injectionSite }`, ID nguyên dương, mã lô trim 1–80 ký tự, vị trí trim 1–100. Từ chối trường JSON lạ, không nhận vaccine/recipient/appointment/registration/doctor/employee/time/status hoặc cờ đủ điều kiện/thanh toán/tồn. Nhân viên, thời điểm UTC và trạng thái `DaTiem` do server xác định.
- 201 + Location `/api/v1/vaccinations/indications/{id}/result`, trả `VaccinationDto = { id, indicationId, appointmentId, registrationId, recipient:{id,fullName}, vaccine:{id,name}, lotNumber, injectionSite, nurse:{id,fullName}, administeredAt, status }`. `GET` Location trả `{ vaccination: VaccinationDto|null }`; null là chưa ghi, không tự retry POST. Kết quả đã ghi vẫn đọc được khi lô hết hạn/hết tồn hoặc điều kiện ghi hiện hành đổi; không mở lịch sử Customer.
- Transaction Serializable: khóa Identity account → session → chỉ định → đăng ký → lịch → lô (phiếu/nguồn đọc trong cùng transaction Serializable) theo thứ tự cố định. Đối chiếu đúng khách hàng/người tiêm/đăng ký DaDangKy/dòng lẻ hoặc thành phần gói, phiếu thuộc lịch và cùng nguồn, kết luận Đủ điều kiện/chỉ định DuocTiem. Không chấp nhận legacy thiếu nguồn hoặc tạo bằng chứng thay thế.
- Dùng lại phép đọc/kiểm tiền G3: giá và số lượng snapshot từ toàn bộ chi tiết đăng ký, tổng dương trong giới hạn VND, không MaKhuyenMai chưa snapshot; đúng một ThanhToan cùng đăng ký, ToanBo/MoPhong/ThanhCong, số tiền đúng tổng. Thiếu thanh toán bị từ chối; tổng 0 fail closed `vaccinationZeroTotalNotSupported`, không tạo ThanhToan 0 hoặc miễn phí giả.
- Một lần ghi thành công trừ đúng 1 tồn bằng UPDATE có điều kiện, insert MuiTiem có MaChiDinh, insert BienDongKho `XuatDoTiem`/số lượng 1 có MaMuiTiem/lô/điều dưỡng/thời điểm khớp mũi và audit `vaccinations.recorded` / ResourceType `Vaccination`, ResourceId ID mũi, actor/session; commit cùng nhau. Bất kỳ lỗi nào rollback cả bốn. Unique MaChiDinh và MaMuiTiem ngăn ghi trùng; tranh liều cuối chỉ một thành công.
- Problem Details/no-store cho mọi response kể cả lỗi binding: 400 `validation`, 401 `invalidSession`, 403 `permissionDenied`, 404 `resourceNotFound`, 409 `vaccinationSourceConflict`, `vaccinationNotIndicated`, `vaccinationAlreadyRecorded`, `vaccinationPaymentRequired`, `vaccinationZeroTotalNotSupported`, `paymentDataConflict`, `vaccinationLotUnavailable`; SQL/timeout/ghi/audit lỗi trả 503 `dependencyUnavailable`, không trả SQL/credential/nội dung khám.
- POST không retry tự động. Mất response/503 hoặc 201 không đọc được: giữ nguyên request, khóa thao tác mới, GET theo indicationId trước retry. GET có kết quả thì chỉ đọc; GET chưa thấy mới cho retry thủ công cùng request (unique DB bảo vệ POST cũ đến muộn). Đã nhận 201 chỉ GET, không POST lại. Đổi phiên xóa dữ liệu/draft/pending và bỏ response cũ.
- Migration mới `S5VaccinationRecord`: MuiTiem.MaChiDinh nullable/FK/unique filtered; BienDongKho.MaMuiTiem nullable/unique filtered và FK ghép (MaMuiTiem,SoLo,MaNhanVien) tới mũi. CHECK chỉ biến động gắn mũi phải XuatDoTiem/số lượng 1; legacy NULL giữ nguyên. Không backfill. Down từ chối khi còn liên kết mới, không bỏ dấu vết. Chỉ migrate DB test disposable trong gói này.

Khu vực Ghi nhận mũi tiêm A06 chỉ ghi/xem lại mũi, chọn lô từ SQL thật, xác nhận đã thực hiện trước lưu; không tự chạy timer, tạo TheoDoiSauTiem hoặc kết luận cho về. Theo dõi do Điều dưỡng ghi riêng ở S5.6 bên dưới. Nhắc lịch, sửa/xóa mũi, kho/nhập hàng/FEFO và Customer xem lịch sử thuộc gói khác.

## S5.6 — Điều dưỡng ghi kết quả theo dõi sau tiêm (Windows A06)

Chỉ phiên Identity hiện hành của Employee, account/nhân viên Active, đúng chức danh `DieuDuong`; không thêm mã quyền trong mô hình 12 quyền hiện hữu. Customer/Admin/chức danh khác không được GET/POST. Bản ghi là nội dung thực tế nhân viên nhập, không phải xác nhận an toàn/cho về hoặc theo dõi sinh hiệu liên tục.

- `GET /api/v1/vaccinations/follow-up-pending?search=&page=&pageSize=` trả `{ items: VaccinationDto[] }` (DTO mũi S5.5). Chỉ mũi DaTiem có MaChiDinh và nguồn S5.5 hợp lệ, đúng người/vắc xin/lô, chưa có TheoDoiSauTiem. Search trim tối đa 100, tìm tên người tiêm hoặc mã mũi số/MT-0001; page mặc định 1/25, pageSize tối đa 100; ID mũi giảm dần, lọc/phân trang tại SQL. Không phải danh sách người hiện ở cơ sở; chưa có check-in/out.
- `GET /api/v1/vaccinations/{id}/follow-ups` trả `{ vaccination: VaccinationDto, followUp: FollowUpDto|null }` cho mũi hợp lệ. `FollowUpDto = { id, vaccinationId, condition, severity, treatment, recordedBy:{id,fullName}|null, recordedAt }`; recordedBy null chỉ dữ liệu legacy chưa có liên kết người ghi. Không kiểm lại thanh toán/hạn dùng/tồn lô cho theo dõi; đọc/ghi được khi lô đã hết hạn/hết tồn. Không trả tiền sử/lý do khám.
- `POST /api/v1/vaccinations/{id}/follow-ups`: đúng ba trường `{ condition, severity, treatment }`, bắt buộc không rỗng sau trim, tối đa 100/100/500 ký tự. Từ chối JSON lạ; không nhận người tiêm/vắc xin/nhân viên/thời điểm/cờ an toàn hoặc cho về. 201 + Location trỏ GET cùng route, trả FollowUpDto. Nhân viên từ phiên, recordedAt server UTC không trước lúc tiêm; không có giá trị nội dung mặc định.
- Transaction Serializable: khóa Identity account → session → MuiTiem (UPDLOCK,HOLDLOCK), kiểm nguồn/mũi/chưa có theo dõi, insert TheoDoiSauTiem + audit `aftercare.recorded` (ResourceType `FollowUp`, ResourceId ID bản ghi, actor/session), cùng commit. Unique MaMuiTiem chặn hai request cùng mũi; lỗi ghi hoặc audit rollback cả hai. Không sửa mũi/lô/tồn/biến động/đăng ký/thanh toán/phiếu/chỉ định, không tạo timer/tác vụ tự động.
- Mọi response no-store và Problem Details: 400 validation, 401 invalidSession, 403 permissionDenied, 404 resourceNotFound (mũi thiếu), 409 followUpSourceConflict (legacy/trạng thái/nguồn sai/thời điểm tiêm ở tương lai), followUpAlreadyRecorded, 503 dependencyUnavailable. Không đưa nội dung theo dõi, SQL hoặc credential vào lỗi/log.
- Không retry POST tự động. Mất phản hồi/503 giữ nguyên request và GET đối chiếu trước retry; GET có bản ghi thì chỉ đọc. GET chưa có mới retry thủ công cùng request. Nhận 201 mà không đọc được body chỉ GET, không POST lại. Đổi phiên xóa dữ liệu/bản nháp/pending và bỏ response cũ.
- Migration mới S5PostVaccinationFollowUp chỉ bổ sung TheoDoiSauTiem.MaNhanVien nullable/FK Restrict; giữ unique MaMuiTiem và dữ liệu legacy, không backfill. Bản ghi API mới bắt buộc actor; Down từ chối nếu còn MaNhanVien không null. Chỉ áp trên DB test disposable trong gói này.

Windows thêm khu vực Theo dõi sau tiêm cạnh Ghi nhận mũi tiêm, chọn mũi → đối chiếu → nhập đủ ba nội dung → xem/xác nhận → lưu → đọc lại bằng MT-id. Không sửa/xóa, tự phân loại phản ứng, chỉ định thuốc, đổi trạng thái mũi hoặc quyết định cho về. Nhắc lịch/Customer xem lịch sử/báo cáo thuộc gói khác.
