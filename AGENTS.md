# KLCN090 — quy tắc làm việc

## Khóa phạm vi (ưu tiên cao nhất)
- Chỉ làm các mục trong SCOPE.md. Muốn thêm gì phải hỏi người dùng trước.
- Không tạo thêm tài liệu thiết kế, giai đoạn, ma trận hay ảnh review mới.
- Mỗi thay đổi nghiệp vụ là một lát dọc: DB → API → client gọi API thật. Không dùng Sample cho chức năng mới.
- Chưa có endpoint thì làm endpoint theo backend/contracts/ trước, rồi mới sửa UI.
- Giữ luồng ở mức tối thiểu, không thêm trạng thái, bước hay ngoại lệ ngoài SCOPE.md.
- Mỗi PR một chức năng; build và test phần bị ảnh hưởng trước khi báo xong. Gói thuần tài liệu như R00 chỉ kiểm nội dung, liên kết và diff; không cần chạy build/test ứng dụng.
- Mỗi gói phải được giao riêng, chốt phạm vi tệp, tiêu chí nghiệm thu và kiểm thử trên nhánh/PR riêng. Hoàn thành thì dừng bàn giao; lộ trình R01 không cấp phép tự triển khai tiếp G1–G8.

Quyết định 25/10/2026: SCOPE.md thay phạm vi cũ bên dưới khi có xung đột; đặt cọc/trả góp hoãn. Quy tắc bảo mật, dữ liệu và quyền thao tác vẫn giữ.
Quyết định R00 ngày 27/10/2026: bổ sung kết luận/chỉ định và lô/tồn tối thiểu để ghi tiêm an toàn theo SCOPE.md; ưu đãi giảm tiền VND, không cộng dồn. Không mở module khám/nhập kho đầy đủ.

Đọc README.md, SCOPE.md và AGENTS.md trước khi sửa.
Khi làm giao diện Windows, tuân thủ mục Giao diện Windows bên dưới.
Phạm vi hiện tại: Website React và Windows WPF (quyết định người dùng 26/10/2026); dùng chung ASP.NET Core API và SQL Server.
Không sửa tài liệu gốc/ảnh tham chiếu để hợp thức hóa nghiệp vụ. Thay đổi mô hình phải giải thích trong PR.
Mỗi nhân viên có đúng một chức danh; đăng ký có thể gồm vắc xin lẻ và gói; thanh toán toàn bộ một lần, có nhãn mô phỏng. Đặt cọc/trả góp tiếp tục hoãn.
Không tự suy diễn dữ liệu hoặc quy trình nội bộ VNVC. Dữ liệu mẫu chỉ dùng phát triển, không đưa thông tin khách hàng thật vào Git.
Mật khẩu phải hash tại backend. Web/Windows đã dùng Identity API; không dùng đăng nhập mẫu để thay xác thực thật. Ghi hồ sơ bằng tài khoản nhân viên yêu cầu chức danh LeTan theo API hiện tại; không nới quyền Admin để demo chạy.
Client không truy cập SQL trực tiếp. Mọi kiểm tra quyền, tiền, tồn kho và ghi dữ liệu phải nằm ở API.
Ghi tiêm phải kiểm chứng chỉ định chuyên môn, đúng lịch/người/vắc xin, quyền nhân viên, lô được phép sử dụng, hạn dùng, tồn dương và điều kiện thanh toán. Ghi mũi, trừ tồn, biến động và audit cùng transaction; chống ghi trùng/tranh liều cuối. Thiếu bằng chứng thì từ chối, không fallback Sample hoặc tự tạo chỉ định/lô.
Chỉ người sở hữu module sửa module đó trong giai đoạn triển khai ban đầu. **Quyết định người dùng 05/11/2026:** từ giai đoạn refinement/integration hiện tại, các phần S6 của Bảo và S7 của Quý được xem là nguồn implementation ban đầu; Đạt/Codex được phép sửa, tích hợp, bổ sung test và nghiệm thu kỹ thuật trực tiếp trong PR refinement mà **không cần chờ Bảo/Quý review lại**. Vẫn giữ attribution/lịch sử đóng góp, không ghi giả APPROVE của người khác và không force-push branch owner. Thay đổi contract/schema/tiền/quyền/clinical vẫn phải qua exact-head coordinator review + test/CI/runtime gate tương ứng trước merge. Không tự cấp hoặc đổi quyền cộng tác GitHub.
Sau thay đổi: chạy kiểm tra đúng phần bị ảnh hưởng; báo đúng kết quả và các phần chưa kiểm tra. Tiến độ chỉ cập nhật cột Trạng thái trong SCOPE.md; không tạo file tiến độ khác.

## Điều phối ChatGPT/Codex — quy tắc lâu dài

- **Vai trò:** ChatGPT là coordinator/advisor: audit yêu cầu và code hiện tại, khóa acceptance, viết prompt Codex, review exact-head, điều phối Git/CI/checkpoint. **Codex mới thực thi/sửa code**. Không dùng chat điều phối để âm thầm code thay một feature khi chưa có chỉ định riêng.
- **Một lát ACTIVE:** tại một thời điểm chỉ có **một** feature/slice refinement của Đạt ở trạng thái ACTIVE. Việc ngoài lát hiện tại đưa vào QUEUED/PARKED; không bắt đầu lát kế tiếp cho đến khi lát hiện tại đã được coordinator nghiệm thu/merge/checkpoint và người dùng xác nhận chuyển tiếp.
- **Nguồn phục hồi khi mở chat/session mới:** đọc theo thứ tự mặc định **AGENTS.md → SCOPE.md → issue coordinator #76 → active feature issue → active PR và exact remote HEAD/CI → backend/contracts/README.md nếu có contract liên quan → issue #64 chỉ khi có giao thoa owner**. Không dựa vào trí nhớ hội thoại nếu repo/issue đã có nguồn chính thức mới hơn.
- **Không tạo sổ tiến độ thứ hai:** tiến độ chính thức chỉ cập nhật cột Trạng thái trong `SCOPE.md`. Issue/PR comment dùng làm checkpoint/acceptance/review evidence; không tạo file kiểu AI state/progress/checklist lâu dài khác trừ khi người dùng yêu cầu rõ.
- **Quy trình một lát:** audit current main + nguồn nghiệp vụ → khóa acceptance/scope stop → chọn model/effort + skill → Codex implement trên branch sạch từ latest main → targeted/full tests phù hợp → runtime/visual gate nếu có UI → push Draft PR → coordinator kiểm exact remote HEAD/diff/CI/evidence → patch blocker hẹp nếu có → merge → đóng issue → SCOPE/checkpoint → dừng chờ xác nhận lát tiếp.
- **Không tin handoff đơn thuần:** coordinator phải đối chiếu GitHub remote trước khi chấp nhận: exact HEAD, changed files/diff, review/thread, CI đúng SHA và main/base hiện tại. Với UI, build/CI **không** thay visual/full-shell smoke; phần chưa chạy phải ghi `UNVERIFIED`.
- **Git/merge:** mỗi feature một branch/PR riêng từ `main`; Codex không merge và không tự đóng issue. Khi lát độc lập đạt acceptance, CI đúng exact head xanh và không thuộc trường hợp cần owner review theo AGENTS, coordinator có thể self-review/merge bằng expected-head lease. Không merge khi CI fail/pending, SHA đã đổi sau review hoặc còn blocker.
- **Không chia vụn task vô lý:** khi acceptance đã rõ và môi trường cho phép, giao Codex một feature slice hoàn chỉnh để tự đi **audit → implement → test → visual → PR → handoff** trong cùng session. Chỉ tách thành prompt tiếp theo khi có blocker thật, dependency ngoài scope hoặc patch review hẹp. Không dùng model nhẹ cho cả feature chỉ để tiết kiệm từng lượt nếu làm tăng số vòng handoff/reload context.
- **Model routing hiện hành:** mỗi prompt Codex đáng kể phải ghi model + thinking effort. Mặc định feature rõ, nhiều file: **GPT-6.1 Sol Medium**. Patch cơ học/wording/regression nhỏ: **GPT-6.1 Terra Medium**. Cross DB/API/Web/Windows, auth/mail, money, inventory, clinical, migration hoặc concurrency khó: **GPT-6.1 Sol High**. Chỉ dùng **Astra Medium/High** khi có ambiguity/architecture/race/bug khó mà Sol chưa đủ. Tên model có thể đổi theo sản phẩm; coordinator được cập nhật mapping nhưng phải giữ nguyên nguyên tắc chọn model theo độ khó và tránh overkill.
- **Phân vai code để tiết kiệm usage — quyết định người dùng 05/11/2026:** **Codex là coder chính của mọi lát ACTIVE** và chịu trách nhiệm implementation chính, chạy test/runtime, migration và handoff. ChatGPT coordinator vẫn là bộ não điều phối/reviewer; chỉ được sửa trực tiếp qua GitHub như **micro-patch phụ trợ sau handoff** khi thay đổi cơ học, phạm vi rất hẹp, dễ review bằng diff và không cần tái thiết kế. Ví dụ được phép: typo/wording, docs, test assertion nhỏ, import/config hiển nhiên, rename presentation, một helper hiển thị nhỏ, cleanup debug text. **Không dùng coordinator direct-edit để làm feature chính**, không tự code thay Codex ở DB/schema/migration/auth/RBAC nghiệp vụ, tiền, tồn kho ghi, clinical, concurrency, contract chéo module hoặc UI flow đáng kể. Nếu blocker chạm các vùng này thì trả về chính session Codex của lát. Coordinator không sửa song song khi Codex đang code cùng file; chỉ patch sau handoff/exact-head review hoặc trên PR phụ riêng để tránh conflict. Sau mọi direct-edit vẫn phải có exact-head CI; nếu thay đổi runtime/UI thì cần runtime/visual gate tương ứng trước merge.
- **ChatGPT model/thinking:** khi bắt đầu module lớn hoặc mở chat mới, coordinator phải nói ngắn model/thinking phù hợp cho người dùng ChatGPT Plus; audit/điều phối phức tạp ưu tiên reasoning cao, câu hỏi/patch đơn giản không cần overthink.
- **Context management:** sau một module lớn đã merge/checkpoint, hoặc khi chat/session xuất hiện lặp lỗi, nhầm SHA/branch/acceptance, hay context đã quá dài, ưu tiên mở chat/session mới. Trước khi chuyển phải có checkpoint ngắn ở #76/SCOPE để phiên mới phục hồi theo source hierarchy ở trên. Không kéo implementation context của module cũ sang session Codex mới nếu feature mới đã bắt đầu.
- **Y tế/sức khỏe:** không bịa quy trình, dữ liệu, lịch tiêm, chỉ định hay quy tắc y khoa để làm demo chạy. Khi acceptance phụ thuộc thông tin y tế/tiêm chủng thực tế, ưu tiên nguồn công khai/đáng tin và phân biệt rõ quy tắc dự án với khuyến cáo/chính sách thực tế.
- **Kết thúc mỗi lát:** coordinator luôn tóm tắt **DONE / ACTIVE / NEXT / PARKED**, exact main/PR khi liên quan, và model đề xuất cho bước tiếp. Không tự chuyển NEXT thành ACTIVE nếu chưa có xác nhận người dùng.

## Phối hợp mở rộng nghiệp vụ — cập nhật 03/11/2026

Người dùng **cho phép mở rộng phạm vi theo từng lát có đầu ra chạy được**, 03/11 ủy quyền review kỹ thuật/merge X1 và sau PR #74 yêu cầu bỏ review chéo hình thức. **Bổ sung quyết định 05/11/2026:** trong giai đoạn refinement hiện tại, Đạt/ChatGPT/Codex là cổng nghiệm thu cuối cho cả các phần S6/S7 đã được Bảo/Quý bàn giao; không còn gate bắt buộc owner review lại, kể cả khi PR refinement chạm contract/schema/tiền của chính S6/S7. Điều này không hạ chuẩn kỹ thuật: các thay đổi tiền, tồn kho ghi, quyền nhạy cảm, schema/migration hoặc clinical vẫn phải exact-head review, targeted/full tests, CI và runtime/visual gate phù hợp. Giữ attribution, không giả nhận APPROVE của Bảo/Quý. `SCOPE.md` quyết định phạm vi; chỉ mở một lát đã giao, có code/test/UI theo đúng actor và không áp sổ QD-001–017 tự động.

- **Chỉ một lát tại một thời điểm của Đạt:** X1 đọc kho merge PR #66 (`9ff1101`); X2.1 nhập một lô mới merge PR #71 (`9eba7ee`); **X2.2.1 lịch sử tiếp nhận chỉ đọc merge PR #74 (`c098155`), CI #196 success**. Ba lát đã đóng, không làm lại nếu không có lỗi tái hiện. **X2.2.2 nhập bổ sung lô cũ/quản lý nhà cung cấp/phiếu nhập nâng cao và A02/A04 chưa được triển khai hoặc phê duyệt tiếp**. Không đụng `VaccinationEndpoints` và lưu tiền/ưu đãi ngoài PR tương thích rõ ràng. ChatGPT chỉ điều phối/review/merge/tài liệu; **Codex mới thực thi và sửa code**.
- **Giữ nhánh owner nhưng refinement do điều phối sở hữu:** không force-push/rewrite branch lịch sử của Bảo/Quý. Mọi sửa tiếp theo của S6/S7 làm trên branch/PR refinement riêng từ latest main, ghi rõ source/attribution. Theo quyết định 05/11/2026, các PR refinement này **không cần Bảo/Quý review lại**; coordinator exact-head review + test/CI/runtime gate là cổng merge.
- **Dùng contract nhỏ, tương thích ngược:** tránh sửa DTO/endpoint hiện hữu dùng bởi S6/S7 khi không cần. `Program.cs`, `TiemChungDbContext`, migration, `ShellViewModel`, `NavigationRegistry` là vùng chung phải điều phối; **một migration tại một thời điểm**.
- **Không có integration branch kéo dài làm nguồn sự thật thứ hai:** mỗi lát dùng branch mới từ `main`, PR nhỏ vào `main`, yêu cầu CI/test/review rồi mới squash merge. Sau mỗi merge, các nhánh chưa merge cần đồng bộ `main` và kiểm lại diff trước khi PR; không cherry-pick mù các thay đổi giao thoa.
- **Kiểm thử:** làm xong lát nào test DB/API/client và regression chỗ liên quan của lát đó; S8 tổng thể chạy sau các lát dự kiến nộp đã tích hợp. Test của S5 theo S4 cũ **không chứng minh** S6 ưu đãi tương thích, và test S7 trên branch chưa chứng minh đã merge.
- **Khi bắt đầu phiên Codex/chat mới:** đọc `AGENTS.md`, `SCOPE.md` trên `main`; nếu PR cập nhật này chưa merge, đọc thêm branch `docs/coordination-scope-x1-20261103`, cùng issue #64. Xác nhận module owner, contract, diff/test và **một** lát tiếp theo trước khi code. Tình trạng đã nghiệm thu vẫn chỉ được ghi vào cột `Trạng thái` của `SCOPE.md`.

## Giới hạn triển khai ở mức khóa luận tốt nghiệp

Quyết định trực tiếp của người dùng ngày 24/10/2026. Áp dụng cho chat điều phối, tác giả và reviewer của Web, Windows, API và SQL. Mục tiêu: sản phẩm đủ tốt để giải thích thiết kế, kiểm chứng chức năng và demo nghiệp vụ thật với dữ liệu giả. Không mặc định nhận yêu cầu vận hành hệ thống y tế production.

### 1. Chọn giải pháp vừa đủ

Dùng kiến trúc Modular Monolith React/WPF → một ASP.NET Core API → một SQL Server database, giữ bốn project backend hiện có. Tái sử dụng thư viện/framework hiện có. Không thêm service, lớp trừu tượng, công cụ vận hành hoặc cơ chế tùy biến nếu cách đơn giản đã đáp ứng luồng hiện tại. Ưu tiên nghiệp vụ trước 08/11/2026; không refactor diện rộng hoặc thiết kế trước mọi tình huống tương lai.

### 2. Chốt đầu ra trước khi làm

Mỗi gói ghi ngắn: luồng người dùng cần chạy, dữ liệu cần lưu, tiêu chí đạt, kiểm thử liên quan và phần hoãn. Ưu tiên một vài luồng hoàn chỉnh từ UI đến API/SQL thay vì nhiều màn chỉ có mock hoặc hạ tầng chưa được người dùng sử dụng. Dữ liệu mẫu được ghi rõ; không trình bày mock là nghiệp vụ thật.

### Quy tắc dữ liệu synthetic cho test/dev — 05/11/2026

- **Dữ liệu cá nhân dùng trong test/dev là synthetic hoàn toàn và được phép tự sinh.** Codex có thể tự tạo họ tên, ngày sinh, giới tính, số điện thoại, email, CCCD/định danh, địa chỉ, người liên hệ và các trường hồ sơ khác để đủ điều kiện validator/test. Không cần né tránh vì “có thể lộ dữ liệu” khi chính Codex vừa tạo dữ liệu giả đó.
- Không lấy/copy dữ liệu cá nhân của người thật, không dùng credential thật, không commit secret thật. Synthetic credential theo sổ test/local policy hiện có vẫn được phép.
- Ưu tiên dữ liệu ngắn, hợp lệ theo format và dễ đọc trong test. Ví dụ tên `Nguyễn Test A`, email `test.a@example.invalid`, số điện thoại/CCCD là chuỗi số đúng độ dài mà validator yêu cầu. Không cần thêm prefix `[DEMO]`, “mô phỏng”, “dữ liệu giả lập” vào từng giá trị nếu bản thân ngữ cảnh đã là test/dev.
- Mỗi test cần uniqueness thì sinh giá trị mới/deterministic theo test id; không dùng random khó tái lập nếu không cần.
- **Không dùng text filler mô tả** kiểu “mô tả test”, “abc”, “dữ liệu mô phỏng”, “nội dung demo” cho các trường nghiệp vụ nếu trường đó cần nội dung có ý nghĩa. Với dữ liệu không-y-tế có thể tạo nội dung ngắn, tự nhiên và đủ validator.
- **Dữ liệu y tế/vắc xin là ngoại lệ:** tên vắc xin, bệnh phòng ngừa, công nghệ, đường dùng, đối tượng, liều lượng, phác đồ, phản ứng sau tiêm, nguồn tham khảo… nếu dùng làm dữ liệu trình diễn/seed phải lấy từ nguồn công khai đáng tin cậy (ưu tiên VNVC, Bộ Y tế, WHO, nhà sản xuất/tờ hướng dẫn đã công khai) và điền đủ trường có căn cứ. Không bịa để “cho có”.
- Test unit/integration thuần logic vẫn có thể dùng vaccine synthetic tối giản khi test không phụ thuộc nội dung y khoa; nhưng dữ liệu seed/demo nhìn thấy trên UI phải ưu tiên dữ liệu y tế có nguồn, không dùng catalog `[DEMO] Vắc xin A/B` lâu dài.

### 3. Giữ chất lượng thiết yếu

Bắt buộc hash mật khẩu tại backend; API kiểm quyền và phạm vi hồ sơ; validate đầu vào; giữ ràng buộc dữ liệu; xử lý lỗi rõ ràng. Với tiền/tồn kho và ghi nhiều bảng, dùng transaction/kiểm tra cạnh tranh khi cần để tránh sai nghiệp vụ. Ghi nhận người thực hiện/thời điểm cho thao tác quan trọng. Không bịa nghiệp vụ y tế, tài chính hoặc quyền đại diện để làm demo chạy được. Credential test ghi sổ theo AGENTS; không đưa dữ liệu người thật vào Git.

### 4. Tài khoản Admin ở mức khóa luận

Đích mặc định là một lệnh seed/khởi tạo có kiểm tra tồn tại, tạo Admin demo, hash bằng backend và gán quyền đã chốt. Một Admin mẫu đủ cho demo, không cần khóa mô hình chỉ có một Admin. Sau đó quản lý tài khoản nhân viên bằng chức năng phần mềm phù hợp phạm vi. Tài khoản Admin ứng dụng và SQL login kết nối backend là hai khái niệm riêng.

Không mặc định yêu cầu offline operator riêng, Windows SID/ACL cố định, SQL operator role riêng, manifest/ledger cứu hộ, nâng cấp tài khoản legacy hay quy trình khôi phục cấp doanh nghiệp. Các cơ chế đã làm được giữ nguyên trong lúc đối chiếu; không tự tháo/xóa hoặc đổi hash/ledger/quyền để vượt lỗi. Nếu cần đơn giản hóa phần hiện có, tác giả chốt một diff thiết kế hẹp, review phần thay đổi rồi triển khai trong cùng gói, không khởi động lại toàn bộ M1.

### 5. Giới hạn môi trường và tích hợp ngoài

Mặc định demo trên máy Windows hiện có, SQL test/development và client phù hợp. VM, cloud, HA, đa vùng, DR, tải lớn và hạ tầng quản trị chuyên dụng thuộc hướng phát triển, trừ khi được giao cụ thể. Email/SMS/thanh toán có thể dùng adapter sandbox hoặc mô phỏng ghi nhãn; không bắt buộc mua dịch vụ thật. Vẫn kiểm chứng API/SQL và quyền thật cho luồng đã tuyên bố hoàn thành. Khi email xác minh nằm trong luồng demo, phải có cách nhận token test hoạt động, không bỏ xác minh âm thầm.

### 6. Phân loại góp ý review theo phạm vi

Chặn khi luồng đã chốt không chạy, sai dữ liệu/nghiệp vụ, mất dữ liệu, lộ hồ sơ trái quyền hoặc có lỗ hổng trực tiếp trong luồng đó. Ghi cải tiến tùy chọn cho tối ưu, cứu hộ/vận hành production, khả năng mở rộng và tình huống legacy ngoài phạm vi. Không dùng nhận xét “chưa production-ready” làm lý do tự động chặn demo hoặc nghiệm thu chức năng khóa luận. Đọc tài liệu cũ theo giới hạn này; tác giả cập nhật checklist đang dùng, ghi rõ mục hoãn và căn cứ, không nhận PASS cho ca chưa chạy.

### 7. Kiểm thử và điểm dừng

Chạy kiểm tra đúng phần ảnh hưởng: luồng chính, dữ liệu không hợp lệ, thiếu quyền/phạm vi dữ liệu và lưu/đọc lại SQL khi gói có ghi dữ liệu. Test concurrency/rollback tập trung vào nơi có nguy cơ sai tiền/tồn kho hoặc ghi dở transaction; không áp toàn ma trận deployment cho mọi CRUD. Giữ test đã có, chạy regression phù hợp khi sửa. Build/CI không thay runtime cần thiết; không lặp toàn bộ audit/tests khi không có thay đổi liên quan. Khi đạt tiêu chí gói, cập nhật kết quả/giới hạn và bàn giao review.

### 8. Kiểm soát phần phát sinh

Trước khi thêm cơ chế ngoài gói, nêu tình huống hiện tại cần nó, tác động và vì sao giải pháp đơn giản không đủ. Nếu chỉ phục vụ tương lai, ghi một mục hướng phát triển rồi tiếp tục gói hiện tại. Chỉ xin quyết định khi thật sự thay phạm vi/nghiệp vụ/contract hoặc cần quyền mới; không xin lại cho sửa lỗi thường lệ đã được giao. Quyết định này không tự cấp phép merge, xóa/tái tạo DB, đổi quyền SQL/OS hoặc gửi tin sang chat khác.

Ví dụ đạt mức khóa luận: đăng nhập → kiểm quyền → tạo hồ sơ → lưu SQL → mở lại vẫn thấy → tài khoản không có quyền bị từ chối. Kế hoạch nên ưu tiên hoàn thành chuỗi này trước khi thêm quy trình cứu hộ Admin hoặc tối ưu vận hành.

## Ponytail cho dự án

Dùng skill Ponytail chung nếu có sẵn trong môi trường, theo danh sách skill hoặc vị trí cài đặt trên máy đang làm việc; không phụ thuộc đường dẫn tuyệt đối của một thành viên và không cài bản sao trong repo. Nếu không có skill, tiếp tục theo các quy tắc tối giản ở đây. Áp dụng khi viết/sửa/refactor/review code; dùng mức `full` mặc định để chọn thay đổi nhỏ nhất giải quyết đủ yêu cầu, tái sử dụng code/framework và tránh làm trước cho tương lai. Không tự chuyển ultra, chạy audit toàn repo hoặc xóa code ngoài gói. Quy tắc khóa luận/nghiệp vụ/quyền thao tác trong AGENTS vẫn có hiệu lực; Ponytail không cho phép bỏ validation/quyền/test cần thiết. Không cần cài hooks hoặc dịch vụ phụ để dùng skill này.

## Tài khoản test — quyết định người dùng 24/10/2026

Người dùng cho phép các chat tạo, sử dụng và lưu rõ username/password synthetic chỉ dùng development/test, không cần xin phép lại việc ghi sổ. Sổ dùng chung trên máy: `C:/Users/DAT/Documents/Codex/KLCN090-Coordination/TEST-ACCOUNTS.local.md` (ngoài Git). Đọc mục phù hợp trước khi test; khi tạo/đổi tài khoản, cập nhật môi trường/DB, vai trò, username, password, trạng thái dự kiến/đã áp dụng/đã kiểm và ngày cập nhật. Không tự đặt lại password đang có chỉ để khớp sổ; giá trị chưa biết phải ghi chưa biết.

Được ghi plaintext credential synthetic vào sổ local này và hiển thị mật khẩu test cho người dùng khi cần. Không coi quy định không tiết lộ secret thật là lý do chặn việc lưu/sử dụng credential test đã được cho phép. Không đưa sổ local hoặc mật khẩu môi trường test này vào Git/PR/log công khai. Quyết định này không tắt hash mật khẩu tại backend, validation, phân quyền, audit hoặc kiểm thử bảo mật của sản phẩm; không tự mở rộng quyền SQL/OS hay áp dụng cho credential thật/production. Không gửi tin sang chat khác nếu chưa có yêu cầu trực tiếp.

### Quy ước tên đăng nhập và mật khẩu test dễ nhập — 05/11/2026

- Mục tiêu: tài khoản **test/dev local, synthetic** cần **dễ nhìn, nhớ, gõ và phân biệt theo chức danh**; không tạo username bốc thăm, chuỗi dài vô nghĩa hay password random khó nhập cho các tài khoản cần thao tác tay.
- Staff: ưu tiên username không dấu, viết thường, theo chức danh + số thứ tự: `admin01`, `letan01`, `bacsi01`, `dieuduong01`, `thungan01`, `thukho01`; hiển thị họ tên synthetic tự nhiên. Customer đăng nhập bằng email: ví dụ `khach01@example.invalid` nếu validator/local mail sink hỗ trợ. Với tài khoản khác cần uniqueness thì tăng số thứ tự.
- Mật khẩu **chỉ cho tài khoản test local**: chọn chuỗi dễ nhớ, nhập tay, **ít nhất 12 ký tự và đáp ứng validator hiện hành**, có thể kết hợp tên vai trò + năm + cụm test + ký tự đặc biệt; ví dụ **minh họa định dạng, không phải mật khẩu đã cấp**: `LeTan@2026Test!`. Ưu tiên khác nhau theo vai trò. Không dùng `123456`, `password`, mật khẩu quá ngắn hoặc credential production.
- Trước khi tạo mới: đọc `TEST-ACCOUNTS.local.md`; **tái sử dụng tài khoản đang hoạt động** nếu có, không reset/đổi mật khẩu chỉ cho đẹp mẫu. Chỉ ghi trong sổ local giá trị **đã thực sự tạo và xác minh đăng nhập được**; mục chưa kiểm phải phân biệt rõ.
- Khi bàn giao test tay: nêu **vai trò + username/email dễ nhận diện + vị trí sổ local** (không đưa mật khẩu vào Git/PR), và chỉ dẫn cách đăng nhập ngắn. Không tự phát minh credential chưa tồn tại.
- Không hardcode các mật khẩu/test account này vào production code, migration, dữ liệu công khai hoặc default đăng nhập. Dùng test fixture/seed Development-only hiện có; giữ hash, validation và kiểm quyền tại API. Nếu môi trường không hoàn toàn local/disposable, dùng credential riêng không chia sẻ, không sử dụng công thức mật khẩu dễ đoán.

## Phân công và Git — quyết định bàn giao 02/11/2026

| Thành viên | GitHub | Phụ trách từ 02/11/2026 |
|---|---|---|
| Đạt | `datngo1203` | S5 (lịch, bác sĩ chỉ định, lô/tồn tối thiểu, ghi mũi, theo dõi sau tiêm, nhắc lịch), điều phối tích hợp và nền tảng dùng chung. |
| Bảo | `giabao9373` | S6 (ưu đãi, snapshot khi đăng ký, tương thích thanh toán); tiếp quản lỗi liên quan S3/S4 khi được giao riêng. |
| Quý | `ngocquydev` | S7 (thống kê, báo cáo bằng API/SQL chỉ đọc và Windows); không tự sửa nghiệp vụ S5/S6. |
| Cả nhóm | — | S8: kiểm thử tích hợp trên hai client Web/Windows, dùng chung ASP.NET Core API/SQL Server. |

- Ba module S5, S6, S7 **không phụ thuộc tiến độ lẫn nhau** để bắt đầu: S5 dùng contract thanh toán S4 đã có; S6 dùng Registration/Payment S4; S7 đọc dữ liệu SQL hiện hành, không chờ S6. Không bắt thành viên khác hoàn thành nửa chức năng trước khi mình làm tiếp.
- Mỗi người tự làm một lát dọc trong module của mình (DB nếu cần → API → Windows/Web theo phạm vi → test). Không giao tách backend/frontend của một lát cho hai người. S6.1 quản lý ưu đãi riêng; S6.2/6.3 chốt snapshot và thanh toán qua các PR tương thích, không tạo trạng thái giữa chừng trên main khiến đăng ký có ưu đãi nhưng Payment vẫn tính giá cũ.
- S7 chỉ truy vấn các bảng đã có: doanh thu từ tiền giao dịch mô phỏng thành công (không SUM sau JOIN sang nhiều dòng dịch vụ), số mũi từ mũi đã ghi, số đăng ký theo trạng thái có thật; lọc thời gian nhất quán, không tạo bảng/migration hoặc trạng thái mới để làm báo cáo. Nếu cần schema ngoài SCOPE, dừng xin chốt.
- **Đóng băng S5 từ 03/11/2026:** đã nghiệm thu theo contract S4. Các tác vụ Codex S6 (Bảo) và S7 (Quý) phải loại S5 khỏi phạm vi ghi/chỉnh sửa, chỉ đọc code S5 khi cần đối chiếu contract. Không 'sửa tiện' S5, không xóa hoặc thêm `.gitignore` cho mã nguồn đã theo dõi. Chỉ mở S5 lại khi có lỗi tái lập được hoặc PR tương thích S6 → S5 do Đạt điều phối, review và chạy regression độc lập; S5 vẫn thuộc Đạt.
- Điểm giao S6 → S5 về số tiền sau giảm là **gói tương thích tích hợp riêng**, do Đạt điều phối sau khi Bảo chốt contract và kiểm thử S6. Không bắt S5 đợi S6; khi S6 chưa merge, S5 kiểm theo contract S4. Không cho phép tự nới điều kiện ghi tiêm; tổng 0 vẫn fail-closed cho đến khi được quyết định riêng.
- File dùng chung (`Program.cs`, `Entities.cs`, `TiemChungDbContext.cs`, `TiemChungDbContextModelSnapshot.cs`, `ShellViewModel.cs`, `NavigationRegistry.cs`, `apps/web/src/App.jsx`, `backend/contracts/README.md`) phải giới hạn diff nhỏ nhất. Trong refinement/integration do Đạt điều phối, có thể sửa phần S6/S7 và file dùng chung khi acceptance của lát yêu cầu; **không cần owner review lại**, nhưng bắt buộc coordinator exact-head review và regression các module bị ảnh hưởng. Không sửa ngoài gói chỉ để 'làm cho chạy'.
- Đạt điều phối migration: chỉ **một người** thay schema/migration tại một thời điểm, không sửa migration đã áp dụng, không tự migrate/reset/reseed DB demo hoặc dùng chung DB test đang có người khác kiểm. Quý làm S7 trên truy vấn chỉ đọc, không cần migration theo phạm vi đã chốt.
- Đạt duy trì `AGENTS.md`, `SCOPE.md`, `README.md` chung khi cần đồng bộ. Tiến độ chỉ cập nhật cột Trạng thái trong `SCOPE.md`; không mở thêm sổ tiến độ. GitHub Issues là nguồn giao việc, nội dung issue cũ xung đột `SCOPE.md` phải được cập nhật trước khi triển khai.
- Mỗi lát dùng branch riêng từ `main` sạch, PR nhỏ vào `main`, Codex thực hiện code/test; ChatGPT review kỹ thuật và có thể tự merge **lát độc lập đã có test + CI trên đúng head, không blocker** theo ủy quyền người dùng. **Review độc lập chỉ bắt buộc khi thay đổi thực sự ảnh hưởng contract/schema/quyền/luồng module khác hoặc có rủi ro quan trọng đã xác định**, và phải do đúng người thực hiện; không gán review hộ. Không commit thẳng/force-push vào main; không merge khi CI fail, chưa có test phù hợp hoặc có lỗi P0/P1. Giữ rõ các ca WPF chưa chạy thay vì giả nhận PASS.
- Phân công 02/11 thay phần giao module trước đây nhưng **không viết lại lịch sử tác giả của commit/PR**. Đề cương và SCOPE chốt nghiệp vụ; không xem tính năng cũ trong GitHub Issues (đặt cọc/trả góp, mobile, đổi/hủy lịch, nhập kho đầy đủ) là yêu cầu hiện hành.

## Chuẩn tham chiếu VNVC và quy tắc skill khi làm giao diện (03/11/2026)

**Quyết định người dùng:** Website khách hàng của khóa luận phải **bám sát ngôn ngữ thiết kế và trải nghiệm VNVC**; ứng dụng nhân viên **Windows/WPF được thiết kế khác** để ưu tiên thao tác nghiệp vụ, nhưng giữ sự nhất quán về màu, chữ và trạng thái. **Tính năng mở rộng đến đâu phải làm UI thật của chính tính năng ấy đến đó**, không gom nợ giao diện đến cuối.

### Tham chiếu bắt buộc — trước mỗi lát có giao diện
- Website tham chiếu: `https://vnvc.vn/` và **trang đặt/chọn mua** `https://vax.vnvc.vn/vaccine`. Chọn đúng màn trang thực tương ứng nghiệp vụ đang sửa; dùng ảnh/chụp màn hình và CSS/computed styles thực tế nếu Codex có browser. Đối chiếu **font family/weight/size**, xanh chủ đạo, cam nhấn, logo/biểu tượng, khoảng trắng, header/navigation, card, bảng, form, nút, thông báo và responsive; **không tự gán font hoặc hex rồi tuyên bố là màu/font chính thức của VNVC** khi chưa kiểm nguồn. Thời điểm và URL tham chiếu cần ghi ngắn ngay trong PR.
- Ưu tiên tái tạo **phong cách thị giác + cấu trúc tương tác VNVC phù hợp phạm vi KLCN**, không copy nguyên HTML/JS/CSS. Theo quyết định người dùng/GVHD từ 05/11/2026, Website khóa luận được phép dùng tên hiển thị **“Trung tâm tiêm chủng VNVC”** và logo VNVC do người dùng cung cấp; mục tiêu presentation là trông như một sản phẩm VNVC trong phạm vi khóa luận. **Không tự thêm nhãn runtime kiểu “mô phỏng theo…”, “phục vụ khóa luận…” hoặc “không phải website chính thức…” chỉ để phân biệt thương hiệu.** Vẫn không được bịa dữ liệu/quy trình nội bộ VNVC, official API/integration/endorsement hoặc đưa dữ liệu khách hàng thật vào mã nguồn; tài liệu kỹ thuật/PR phải phân biệt rõ dữ liệu khảo sát công khai với dữ liệu nội bộ chưa được xác minh.
- Website React: bám sát trải nghiệm VNVC khi thực hiện các lát Web; thống nhất thành phần/tokens **trên trang đang sửa**. Không tự redesign toàn site khi công việc là một module; dần thay Sample hoặc màn cũ khi tới đúng module. Windows WPF: giữ khung VAC, `ViewsV2/`, `Themes/`, `CE.*`, ưu tiên nghiệp vụ của từng vai trò, readability, keyboard, tốc độ, trạng thái SQL; có thể thiết kế khác Web.

### Skill gate bắt buộc cho prompt Codex
- Với mọi **feature slice hoặc task Codex đáng kể** (không phải patch cơ học rất hẹp), prompt do chat điều phối tạo phải có mục **SKILL GATE** ngay sau phần Source of Truth và **trước audit/implementation**. Không được chỉ viết “đọc AGENTS.md” rồi mặc định Codex sẽ tự nhớ hoặc tự gọi skill.
- Ở đầu mỗi session/task, Codex phải **kiểm các skill hiện có**, đọc mô tả/`SKILL.md` của skill liên quan trước khi dùng, rồi mới quyết định skill nào áp dụng. Không gọi skill chỉ vì nó tồn tại.
- Với tác vụ có UI, `baoyu-design` là skill bắt buộc theo mục kế tiếp: kiểm/cài local cho Codex nếu thiếu, đọc hướng dẫn, dùng ở giai đoạn phương án UX/UI rồi mới triển khai React/WPF thật. Không để skill thay đổi phạm vi, nghiệp vụ, quyền, tiền, tồn kho hay lâm sàng.
- Với Ponytail hoặc skill khác có sẵn trong Codex environment: chỉ dùng khi mô tả thực sự phù hợp với lát đang làm; phải đọc hướng dẫn trước, không giả định API/cách dùng. Nếu skill không phù hợp thì bỏ qua.
- Handoff Codex phải ghi ngắn: **skill nào đã kiểm**, **skill nào đã dùng và dùng cho phần nào**, hoặc **UNAVAILABLE/không phù hợp** nếu không dùng được. Skill lỗi/cài thất bại không tự động block nghiệp vụ nếu AGENTS đã có fallback rõ.
- Chat điều phối ở các phiên sau khi viết prompt Codex phải giữ thứ tự mặc định: **Source of Truth → Skill Gate → Scope/Acceptance → Audit → Implementation → Test → Visual → PR/Handoff**. Đây là quy tắc lâu dài của project, không cần người dùng nhắc lại.

### Skill Codex `baoyu-design` — chỉ kích hoạt cho tác vụ UI
- Nguồn: `https://github.com/JimLiu/baoyu-design`. Hỗ trợ Codex Agent; skill tạo **HTML prototype** chứ không tự biến thành **WPF XAML/React đang hoạt động**. Với tác vụ **thiết kế/sửa UI**, kiểm `.agents/skills/baoyu-design/SKILL.md`; nếu chưa có, tại **thư mục gốc dự án** chạy `npx skills add JimLiu/baoyu-design --skill baoyu-design --agent codex` (Node/npm phải có), đọc `SKILL.md` và `references/codex.md`, sau đó gọi `$baoyu-design` khi lập phương án UI. Kiểm lại nguồn/code skill trước khi chạy lệnh bên thứ ba; nếu cài thất bại, tiếp tục UX theo quy tắc ở đây và báo rõ, không block nghiệp vụ.
- **Không cài global và không tự commit/vendor toàn bộ thư mục skill, HTML prototype, ảnh chụp nghiên cứu hoặc design artifacts vào PR nghiệp vụ.** Chỉ giữ file thực sự cần cho ứng dụng sau khi đã triển khai. Nếu cần xem trước, dùng prototype tạm và chuyển hóa có kiểm tra sang component React hoặc WPF XAML, không để prototype thay thế dữ liệu/API thật.
- Lát **backend/SQL/test thuần túy** không cần gọi skill; lát có UI nên dùng sau khi biết actor, mục tiêu, màn gốc, API/contract. Không để skill tự thay đổi phạm vi, diễn giải lâm sàng, quyền API, thanh toán hoặc tồn kho.

### Definition of Done cho mỗi lát nghiệp vụ mới/mở rộng
1. Cùng một lát/PR có **DB/migration khi cần → API kiểm quyền, validate, giao dịch/audit → Web và/hoặc Windows theo actor sử dụng → test**. Không đánh dấu hoàn thành khi DB/API đã chạy nhưng màn liên quan còn Sample hoặc không hiển thị/truy cập được.
2. UI phải thể hiện bình thường/loading/rỗng/lỗi/thiếu quyền, trạng thái đang lưu, thông báo thành công/thất bại, chống gửi trùng, dữ liệu từ API thật, và lưu/đọc lại SQL; giữ input khi lỗi. Với ghi kho/tiêm phải xác thực server và chống cạnh tranh, không biến UI thành nguồn sự thật.
3. Test hẹp từng lát + hồi quy điểm giao; Web kiểm desktop/mobile, Windows kiểm full shell tại kích thước thường dùng và keyboard. **Build/CI không chứng minh đã test UI tương tác**; chưa chạy thì ghi rõ.
4. Khi module hoàn thành thì **khóa module và chuyển lát tiếp theo**. Không chỉnh lại toàn bộ giao diện các module đã nghiệm thu chỉ để đồng bộ phong cách, trừ khi được giao một PR UX chuyên biệt.

## Giao diện Windows

Làm trong `apps/windows/ViewsV2/`, tái sử dụng `Themes/` và token `CE.*`. Thống nhất vùng sửa khi cùng làm một màn hình. Giữ nhãn Dữ liệu mẫu cho màn mẫu; không coi state UI là dữ liệu SQL. Không kết nối SQL từ WPF.
Kiểm tra bình thường/lỗi/rỗng, bàn phím và nút vô hiệu; giữ validation, xử lý lỗi và ngăn gửi trùng. Build Windows và kiểm dữ liệu lưu/đọc lại cùng quyền API khi nối nghiệp vụ. Báo lệnh/kết quả/phần chưa kiểm trong bàn giao hoặc PR; tiến độ chỉ cập nhật SCOPE.

### Nguyên tắc UX dùng chung Web và Windows

1. Trước khi triển khai UI của mỗi module, phân tích ngắn ngay trong kế hoạch thực hiện: actor và mục tiêu sử dụng; luồng thao tác chính; thông tin cần thấy ngay và thông tin chỉ hiển thị khi cần; các hành động xem, tạo, sửa, xóa hoặc xác nhận; quyền thao tác và trạng thái dữ liệu; loại bố cục phù hợp. Không tạo tài liệu thiết kế riêng cho phần phân tích này.
2. Tổ chức giao diện theo tác vụ và thứ tự sử dụng. Không thiết kế bằng cách đơn giản liệt kê tất cả input, nút bấm và form trên cùng một trang.
3. Mặc định ưu tiên xem/tra cứu thông tin, mở biểu mẫu thêm/sửa khi người dùng cần. Đây là nguyên tắc định hướng, không bắt buộc với màn chuyên môn cần nhập liệu liên tục; chọn cách phù hợp luồng thực tế.
4. Chọn mẫu giao diện theo nghiệp vụ: Website khách hàng cần dễ hiểu, hướng dẫn rõ và responsive; Windows nhân viên cần thao tác nhanh, dễ tra cứu và phù hợp sử dụng liên tục. Dùng dialog cho tác vụ ngắn; trang hoặc workspace riêng cho tác vụ dài, nhiều dữ liệu hoặc cần xác nhận nghiệp vụ; bảng/danh sách kết hợp chi tiết cho quản lý nhiều bản ghi.
5. Thiết kế rõ các trạng thái: chưa chọn dữ liệu, đang tải, không có dữ liệu, xem/chỉnh sửa/tạo mới, thiếu quyền, đang lưu, thành công/thất bại, mất kết nối hoặc cần thử lại. Quyền chỉ xem phải thể hiện bằng thông tin hoặc trường read-only và lời giải thích ngắn; vẫn cho tìm/chọn/xem. Đặt thông báo kết quả gần thao tác.
6. Bảo toàn dữ liệu người dùng: API lỗi phải giữ bản nháp để thử lại; không ghi đè nhầm bản ghi đang chọn, gửi trùng yêu cầu hoặc liên kết sai khách hàng/người được tiêm. Phân biệt rõ tạo mới và chỉnh sửa; reset lựa chọn và bản nháp khi người dùng chọn tạo tiếp. Có xác nhận dễ hiểu, phù hợp cho hành động không thể hoàn tác.
7. Với giao diện chuyên môn y tế và tài chính, ưu tiên đúng dữ liệu, quyền và điều kiện nghiệp vụ hơn việc tối giản thao tác. Không bỏ validation/kiểm quyền để rút ngắn luồng; không tự thêm quy trình y tế hoặc trạng thái thanh toán ngoài SCOPE.md.
8. Website khách hàng tham chiếu ngôn ngữ thị giác VNVC theo mục “Chuẩn tham chiếu VNVC”; Windows nhân viên giữ khung VAC và tái sử dụng token/style hiện có. Thống nhất typography, khoảng cách và icon; không thêm thư viện lớn chỉ để làm đẹp.
9. Khi hoàn thành module, bắt buộc kiểm chứng UX: Web kiểm desktop/mobile, bàn phím, focus và overflow; Windows kiểm kích thước cửa sổ làm việc thực tế, cuộn, nút thao tác và keyboard navigation. Cả hai kiểm normal/empty/loading/error, phân quyền và kết quả API thật. Nếu chưa có runtime hoặc screenshot thực tế, phải báo rõ phần chưa kiểm; không đánh dấu PASS bằng build/CI thay cho tương tác chưa chạy.
10. Không áp cùng một bố cục cho mọi module. UX-01 là ví dụ về tư duy thiết kế theo tác vụ, không phải template bắt buộc.
11. Không dùng dữ liệu Sample hoặc state UI để chứng minh nghiệp vụ thật. Màn mock phải có nhãn rõ và chỉ chuyển thành màn thật trong gói nghiệp vụ tương ứng, gọi API thật theo phạm vi đã giao.
12. Áp dụng các quy tắc UX trên ngay trong quá trình phát triển từng gói G2–G8. Mỗi gói vẫn tuân thủ phạm vi, tiêu chí nghiệm thu và điểm dừng đã giao; các quy tắc này không tự cấp phép triển khai gói tiếp theo.
13. Với **ô nhập tiền VND editable** trên Web/Windows, hiển thị phân nhóm hàng nghìn bằng **dấu cách ngay trong ô khi người dùng nhập** (ví dụ `300000000` → `300 000 000`). Không dùng dấu `.` hoặc `,` làm dấu phân nhóm trong input. Dấu cách chỉ là presentation; ViewModel/request/API/SQL vẫn dùng giá trị số nguyên VND chính xác. Implementation phải xử lý caret/backspace/delete/paste hợp lý, không âm thầm diễn giải decimal hoặc dấu phân nhóm khác, và phải tái sử dụng formatter/behavior thay vì copy logic từng ô.

14. **Không để chi tiết kỹ thuật/debug lọt ra giao diện nghiệp vụ.** Text người dùng nhìn thấy phải dùng ngôn ngữ nghiệp vụ, không hiển thị tên class/record/DTO/ViewModel, enum/code nội bộ, object `ToString()`, stack trace, raw JSON, hoặc các cụm kỹ thuật như HTTP/SQL/API trong nút, trạng thái, dialog hay biên nhận, trừ màn quản trị kỹ thuật được giao rõ. Chi tiết kỹ thuật để ở log/test/handoff.

15. **Mô phỏng là giới hạn tích hợp, không phải nhãn nghiệp vụ.** Với chức năng mô phỏng như thanh toán/email/SMS, UI vẫn trình bày như quy trình nghiệp vụ hoàn chỉnh. **Không tự thêm thông báo “mô phỏng”, “demo”, “phục vụ khóa luận” hoặc disclaimer thương hiệu vào màn chức năng thật**, trừ khi người dùng yêu cầu riêng cho một màn. Dữ liệu test phải synthetic; không prepend `[DEMO]` vào từng tên chỉ để chứng minh dữ liệu giả. Màn mock/Sample chưa nối nghiệp vụ thật vẫn giữ nhãn Dữ liệu mẫu theo quy tắc hiện có.

16. **Control bind object phải có presentation contract rõ.** ComboBox/ListBox/DataGrid khi bind object phải chỉ định `DisplayMemberPath`, `ItemTemplate`, converter hoặc property hiển thị phù hợp; không dựa vào `ToString()` mặc định của record/class. Trước handoff phải kiểm không có chuỗi kiểu `TypeName { Property = ... }`, enum/code nội bộ hoặc object representation xuất hiện trên UI.

17. **Lỗi UI đã phát hiện phải trở thành regression guard khi khả thi.** Nếu một lỗi có thể kiểm tự động bằng component/UI/XAML test (ví dụ ComboBox hiển thị object thay vì Label, wording kỹ thuật bị lộ, nút không disable khi submit), PR sửa lỗi phải thêm assertion phù hợp thay vì chỉ sửa bằng mắt. Không tạo test snapshot khổng lồ; test đúng invariant đã từng hỏng.

18. **Visual handoff gate bắt buộc cho mọi PR có UI.** Trước khi báo DONE, mở màn thật/full shell và kiểm ít nhất normal state + form/action chính + confirmation/result/receipt nếu có. Soát: text bị cắt/tràn, object/debug text, dữ liệu test gây hiểu nhầm, binding sai, label/code nội bộ, focus/keyboard, scroll, disable/loading/error. Có screenshot/runtime evidence cho màn đã sửa; build/CI không thay thế bước này. Nếu chưa chạy visual smoke thì phải ghi `UNVERIFIED` và không được claim UX hoàn tất.

## Bí mật và môi trường

Không lưu secret/JWT trong Git hoặc biến VITE_*. Dùng user-secrets, biến môi trường hoặc `.env` local; không ghi CCCD/thông tin y tế vào log. Khi triển khai cần HTTPS và CORS đúng origin. Khi lộ secret, thu hồi/đổi secret trước; không tự xóa lịch sử Git hoặc dữ liệu để che sự cố.