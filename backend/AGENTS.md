# Backend rules — KLCN090

Áp dụng cho `backend/**`. Root `AGENTS.md` và `SCOPE.md` vẫn có ưu tiên cao hơn.

## Context
- Chỉ đọc section contract liên quan trong `backend/contracts/README.md`; không cần đọc toàn file nếu task không chạm contract khác.
- Audit entity/DbContext/endpoint/test liên quan trước khi tạo abstraction hoặc schema mới.
- Reuse source-of-truth hiện có; không tạo bảng/trạng thái song song nếu entity hiện hành đã sở hữu nghiệp vụ.

## API / authorization
- API kiểm auth, role/capability, ownership và business invariant; client không phải security boundary.
- Employee runtime dùng matrix/capability hiện hành. Admin không tự bypass actor clinical/cashier/warehouse/receptionist nếu contract không cho phép.
- Error code/HTTP status phải tương thích contract; thay contract phải cập nhật đúng section và regression hai client bị ảnh hưởng.
- Mutation outcome không xác định phải hỗ trợ reconcile; không dựa vào blind retry.

## SQL / migration
- Một migration writer tại một thời điểm.
- Không sửa migration đã áp dụng; migration mới additive/legacy-safe khi có thể.
- Không reset/reseed demo DB để làm test pass.
- Migration test trên DB disposable khi schema thay đổi; kiểm pending-model/diff nếu project hiện có gate đó.
- Tiền, tồn kho, clinical và multi-row write dùng transaction/locking/concurrency phù hợp rủi ro; audit cùng transaction khi contract yêu cầu.
- Provider/network call không nằm trong business SQL transaction trừ khi semantics hiện hành bắt buộc một lock ngoài transaction và đã được acceptance khóa.

## Healthcare / inventory
- Không tự suy medical eligibility, regimen, dose, contraindication hoặc clinical result.
- Ghi tiêm fail-closed nếu thiếu appointment/source/clinical/payment/inventory evidence cần thiết.
- Ghi mũi, tồn kho movement và audit phải nhất quán; chống duplicate/race.

## Test
- Targeted tests trước, full affected regression sau.
- Schema/contract/concurrency/high-risk mutation cần integration SQL/API test.
- Không tuyên bố UI PASS từ backend/CI.
