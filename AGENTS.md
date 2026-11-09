# KLCN090 — agent map

> Mục đích của file này là **định tuyến context**, không phải lưu toàn bộ lịch sử dự án.
> Khi có xung đột: **SCOPE.md hiện hành → active feature issue → contract/code/test liên quan → lịch sử issue/PR**.

## 1. Luật bắt buộc toàn repo

- Đề tài: hệ thống quản lý tiêm chủng, kiến trúc **React Web + WPF Windows → ASP.NET Core API → SQL Server**.
- Chỉ làm phạm vi trong `SCOPE.md`. Mọi mở rộng nghiệp vụ phải được người dùng chốt trước.
- Tại một thời điểm chỉ có **một business/refinement slice ACTIVE**. Không tự mở NEXT/PARKED.
- Mỗi feature là một vertical slice khép kín khi cần: DB/migration → API → client đúng actor → test/runtime.
- Không dùng Sample/fallback để giả hoàn thành chức năng mới.
- API là nguồn quyết định cho auth, quyền, tiền, tồn kho, clinical validation và các mutation nghiệp vụ; Web/WPF không truy cập SQL trực tiếp.
- Không bịa quy trình nội bộ VNVC, quy tắc y khoa, lịch tiêm, chỉ định, giá, quyền hay dữ liệu thực tế để làm demo chạy.
- Dữ liệu cá nhân test/dev được phép synthetic. Dữ liệu y tế/vaccine hiển thị demo phải có căn cứ công khai đáng tin cậy khi nội dung y khoa có ý nghĩa.
- Secret/credential thật chỉ ở local environment; không Git/PR/log. Mật khẩu ứng dụng luôn hash tại backend.
- Không reset/reseed/xóa DB hoặc rewrite migration đã áp dụng nếu chưa có chỉ định rõ. Migration additive/backward-compatible khi có thể.
- Một migration/schema writer tại một thời điểm. Các vùng dùng chung phải giữ diff nhỏ nhất cần thiết.
- Không force-push branch của người khác, không giả review/APPROVE. Giữ attribution lịch sử.
- Trong refinement/integration hiện tại, Đạt/Codex được sửa các phần S6/S7 đã tích hợp qua branch/PR mới; không cần owner review hình thức, nhưng vẫn bắt buộc exact-head review + regression/CI/runtime gate theo rủi ro.

## 2. Context routing — đọc ít nhất đủ dùng

### Mặc định khi bắt đầu task/session mới

Đọc theo thứ tự:
1. `AGENTS.md` này.
2. `SCOPE.md` — chỉ phần Current State/Core Scope/Queue.
3. **Active feature issue**.
4. Nested `AGENTS.md` của vùng code sẽ sửa, nếu có.
5. Code/test hiện hành mà task chạm tới.

**Riêng THESIS/UML:** đây là **THESIS session, không implement code; chỉ dùng `main` để verify claim implementation**. **Không tự bịa hoặc suy luận capability hệ thống:** mọi claim về chức năng/trạng thái hiện có phải có evidence trực tiếp từ `main` hiện hành (code/contract/test) hoặc checkpoint DONE đã merge và còn đúng trên `main`; nếu evidence thiếu/mơ hồ thì ghi *chưa xác minh* hoặc loại claim khỏi báo cáo/UML. Không coi tên module/menu/issue, kế hoạch ACTIVE/NEXT/PARKED hay “hợp lý về nghiệp vụ” là bằng chứng đã triển khai. Mọi chỉnh sửa báo cáo/UML/checkpoint thực hiện trên nhánh thesis hiện hành; không sửa source code ứng dụng trong session này. Khi cần xác minh capability thật, đọc `main`/active implementation branch mới nhất làm evidence nhưng không chuyển source of truth THESIS sang nhánh code. Sau bước 2 phải đọc mục `THESIS-R1 — Theory Source Registry & precedence v1`, sau đó đọc `THESIS-R1 — HUIT UML Visual & Semantic Profile v2` và checkpoint/handoff THESIS-R1 mới nhất trong `SCOPE.md` trước khi audit/vẽ. Registry quyết định **nguồn nào cần đọc cho phần nào**; profile quyết định page anchors/semantic/notation đã khóa. Không dùng chat memory để nhớ shape/notation. Khi bắt đầu **một loại sơ đồ hoặc phần viết mới**, chỉ spot-check đúng source/chương/trang được registry/profile định tuyến; không tải lại toàn bộ bộ bài giảng và không đọc lặp các file duplicate đã khóa.

Chỉ đọc thêm khi cần:
- `backend/contracts/README.md`: khi endpoint/DTO/error/contract liên quan.
- `README.md`: khi cần launcher, seed, local runtime, account/demo instructions.
- issue coordinator #76: khi cần phục hồi checkpoint sau gián đoạn hoặc SCOPE/issue chưa đủ rõ.
- issue #64: chỉ khi có giao thoa owner/module.
- closed issue/PR cũ: chỉ khi cần rationale, regression history hoặc migration compatibility.

**Không** mặc định đọc toàn README, toàn contracts, toàn closed issues hoặc lịch sử module DONE trước mọi task.

## 3. Vai trò ChatGPT / Codex

- **ChatGPT = coordinator/advisor**: audit yêu cầu, khóa acceptance/scope-stop, chọn model/skill, review exact remote HEAD/diff/CI/runtime, checkpoint/merge.
- **Codex = coder chính** của feature ACTIVE: audit code → implement → test → runtime/visual → Draft PR → handoff.
- Coordinator chỉ direct-edit micro-patch cơ học/docs/test assertion/import/config/presentation helper hẹp sau khi phạm vi đã rõ. Không direct-implement feature chính ở schema/migration/auth/RBAC/clinical/money/inventory write/concurrency/cross-module contract/UI flow lớn.
- Codex không merge và không tự đóng issue. Coordinator merge khi exact-head review PASS, CI đúng SHA xanh, runtime/visual gate cần thiết đã chạy và không còn blocker.

Quy trình:
`audit main → lock acceptance → implement branch sạch → test/runtime → Draft PR → exact-head review → patch blocker nếu có → merge → close issue → SCOPE/checkpoint → STOP`.

Sau module lớn hoặc khi session nhầm SHA/branch/acceptance/lặp lỗi: checkpoint rồi mở session mới.

## 4. Model / skill routing

Mỗi prompt Codex đáng kể ghi rõ model + effort:
- feature rõ, nhiều file: **GPT-6.1 Sol Medium**;
- cross DB/API/Web/Windows, migration, auth/mail, money, inventory, clinical, concurrency: **GPT-6.1 Sol High**;
- patch cơ học nhỏ: **GPT-6.1 Terra Medium**;
- Astra Medium/High chỉ khi ambiguity/architecture/race/bug khó mà Sol chưa đủ.

Prompt feature dùng thứ tự:
**Source of Truth → Skill Gate → Scope/Acceptance → Audit → Implementation → Test → Visual/Runtime → PR/Handoff**.

Kiểm skill có sẵn trước khi dùng và đọc `SKILL.md`. Với code/refactor/review, dùng **Ponytail** nếu environment có skill phù hợp; nếu không có thì tiếp tục theo các rule tối giản ở repo. Skill không được tự mở rộng nghiệp vụ.

## 5. Context & change budget

Mục tiêu là **progressive disclosure**, không phải nhét toàn repo vào prompt.

- Issue ACTIVE là feature spec; không copy lại thành một file feature-doc khác chỉ để AI đọc.
- Knowledge bền vững, dùng cho nhiều feature sau mới đáng chưng cất thành domain doc.
- Prompt không paste code/docs mà Codex có thể tự mở đúng file.
- Ưu tiên một cohesive vertical slice/PR. Vài trăm LOC thường dễ review.
- Nếu diff dự kiến vượt khoảng **800–1200 LOC**, chạm quá nhiều vùng độc lập hoặc >10–15 file có trách nhiệm khác nhau, phải kiểm lại xem slice có đang quá rộng không. Đây là **soft budget**, không phải luật fail.
- Không tách class/file chỉ để đạt số LOC; cohesion và source-of-truth quan trọng hơn số dòng.
- Không refactor diện rộng ngoài acceptance hiện tại.

## 6. Quality gates

- Test đúng phần bị ảnh hưởng + regression tại điểm giao.
- Với ghi dữ liệu: validate, quyền, transaction/audit và lưu/đọc lại SQL khi phù hợp.
- Tiền/tồn kho/clinical/concurrency cần test fail-closed, rollback/race theo rủi ro thực.
- UI phải có loading/empty/error/permission/submit state cần thiết; giữ input khi lỗi nếu hợp lý.
- **Build/CI không chứng minh runtime UI.** Web thay đổi UI cần browser smoke desktop/mobile phù hợp; WPF thay đổi UI cần full-shell/native interaction phù hợp.
- Mọi phần chưa chạy ghi `UNVERIFIED`, không suy PASS.
- Unknown mutation outcome: reconcile bằng GET/state hiện hành trước khi retry; không blind replay.

## 7. Y tế và clinical invariant

- Mỗi lượt chuẩn bị tiêm vẫn cần clinical flow theo source-of-truth hiện hành; hệ thống không tự quyết định đủ điều kiện tiêm.
- Ghi tiêm chỉ thành công khi API kiểm đúng lịch/người/vaccine, chỉ định cần thiết, actor/quyền, lô hợp lệ/còn hạn/còn tồn và điều kiện thanh toán hiện hành.
- Ghi mũi + trừ tồn + biến động + audit cùng transaction; chống ghi trùng/tranh liều cuối.
- Không tạo chỉ định/lô/dữ liệu y tế giả để vượt gate.

## 8. Git, tiến độ và bằng chứng

- Mỗi feature một branch từ latest `main`, một PR nhỏ, ưu tiên squash merge.
- Không commit thẳng vào `main` cho feature implementation.
- `SCOPE.md` là **status/source-of-truth hiện hành**, không phải nhật ký chi tiết.
- Issue/PR/Git history giữ acceptance, review, CI và evidence chi tiết. Không tạo sổ tiến độ thứ hai.
- Kết thúc mỗi lát: tóm tắt **DONE / ACTIVE / NEXT / PARKED**, exact main/PR khi liên quan, model đề xuất.
- Không chuyển NEXT thành ACTIVE nếu người dùng chưa xác nhận.

## 9. Test/dev accounts

Synthetic dev/test accounts được phép. Tái sử dụng account local đang hoạt động trước khi tạo mới; không reset password chỉ để đẹp mẫu. Manual test ledger hiện hành: `C:/Users/DAT/Documents/Codex/KLCN090-Coordination/TEST-ACCOUNTS.local.md` (ngoài Git). Credential thật/production không được đưa vào ledger hoặc repo.

## 10. Nested rules

Khi sửa vùng sau, đọc thêm:
- `backend/AGENTS.md` — API, DB, contract, migration, transaction/security.
- `apps/web/AGENTS.md` — React customer UX, VNVC reference, responsive/browser gates.
- `apps/windows/AGENTS.md` — WPF employee UX, CE theme, keyboard/full-shell gates.

Các nested rules chỉ bổ sung cho vùng tương ứng; không thay SCOPE/active issue.
