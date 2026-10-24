# Trạng thái khung — 19/10/2026

## SQL test run m1br_20261024_01 — 24/10/2026

- G2 Recover đã đối chiếu hash/actor PASS trước Reset; Reset terminal exit0, Staff hash match/SecurityVersion1→2, status/duty/title/extra-permissions giữ nguyên, audit Success/ledger1. HTTP application host thật với app SQL credential: Admin và Staff login/me/refresh/logout và hậu logout401 PASS. [Source probe và gate](m1br_20261024_01-acceptance.md). Ca access/refresh tồn tại trước CLI reset đang chuẩn bị bằng live probe (valid me200, giữ signing key/refresh/expiry guard); chưa PASS cho CLI revoke, chưa cạnh tranh/rollback/cleanup hoặc đủ nghiệm thu. Không báo 42/42 mới.

- Theo chấp thuận riêng: đã archive manifest g1 trước Apply ở AppData Codex/Windows PowerShell (cùng Local path; Roaming không có), backup COPY_ONLY/CHECKSUM + VERIFYONLY đạt rồi thay duy nhất DB legacy generation2. Boundary/login/password/state giữ nguyên theo evidence/actual connection. G2 grants/auth boundary Release mới **21/21**; Admin Locked/None/ledger0. [Record và gate tiếp](m1br_20261024_01-legacy-rebuild-request.md). Mật khẩu Recover đã chuẩn bị ngoài Git, chưa áp dụng; phải đối chiếu hash/actor ngay sau Recover trước Reset. Backup/evidence g1 giữ nguyên, chưa restore/cleanup/mode rollback; không 42/42 mới hoặc đủ nghiệm thu.

- Recovery terminal g1 đã thành công; reset bị từ chối vì credential đề xuất không khớp hash, người dùng không nhớ credential thực. Diagnostic paste đúng 23 ký tự/0 control, không suy là lỗi transport. [Script/backup/rollback tái tạo riêng fixture legacy g2 chờ chấp thuận](m1br_20261024_01-legacy-rebuild-request.md); chỉ parser và SQL read-only Check đã chạy. Chưa backup/drop/create/restore/đổi hash/quota hoặc tests SQL/auth mới. Giữ DB/login/mode và chứng cứ g1.

- Bootstrap terminal thật đã thành công và đối chiếu DB/audit/12 quyền; recovery lỗi trước password vì PowerShell 5.1 serialize ngày dạng legacy. Sửa writer ISO-8601, giữ instant/operation/target/version, normalize file local có backup, không gia hạn hoặc thay quota. Boundary regression có powershell.exe thực **21/21**; recovery/reset vẫn chờ terminal, ledger legacy/audit recovery 0. [Checkpoint](m1br_20261024_01-acceptance.md). Không cleanup/rollback mode hoặc đủ nghiệm thu.

- Apply elevated của người dùng thành công; preflight mới đạt Mixed Mode/Windows admin/encryption/quyền/tên trống, sa disabled. Đã provision đúng hai DB/hai login/run users/grants/guards/fixtures. Sửa lỗi DDL IMPERSONATE không hợp lệ và đường EF audit INSERT cần SELECT bằng append-only INSERT+SCOPE_IDENTITY, không nới quyền/schema. **BoundaryAcceptance login thật 20/20**, regression Release SQL/auth mới **42/42** sau sửa. [Mapping/evidence/giới hạn](m1br_20261024_01-acceptance.md). Positive CLI terminal/concurrency/rollback còn thiếu, chưa cleanup/rollback mode; DB/login giữ cho acceptance, service Running/Mixed. Không báo đủ nghiệm thu. Các dòng chưa provision bên dưới là lịch sử.

- Người dùng đã cho phép đổi đúng SQLEXPRESS LoginMode 1→2/restart và rollback sau cleanup. Baseline đọc lại: đúng instance, 0 DB người dùng/0 phiên khác, sa disabled, service Running; agent không elevated nên chưa apply/restart. [Script apply/rollback và tác động/khôi phục](m1br_20261024_01-auth-mode-runbook.md) đã chuẩn bị cho Administrator, có fresh checks và evidence trước write; parser/driver SQL read-only/diff đạt. Chưa tạo đối tượng/CLI/BoundaryAcceptance hoặc test Release mới; không thay gate nghiệm thu. Dòng “không có quyền đổi mode” phía dưới là lịch sử đã được chấp thuận thêm phạm vi, không phải trạng thái quyền hiện tại.

- Sau ngoại lệ test TLS được người dùng cho phép: `-N -C` chỉ tới `.\SQLEXPRESS`/run này, encryption TRUE, không xác minh chứng chỉ. Preflight read-only xác nhận Windows-only authentication (`IntegratedOnly=1`), quyền deployment ALTER ANY LOGIN/CREATE ANY DATABASE đều 1, tên hai DB/hai login chưa có. Exit 1/error 51030; không provisioning/Mixed Mode/restart/config changes. Sửa class truy vấn HAS_PERMS_BY_NAME server-level sang NULL và chạy lại; không sửa quyền SQL. Manifest created rỗng, service Running; CLI/BoundaryAcceptance vẫn chưa chạy. Các dòng TLS chặn phía dưới là lịch sử trước ngoại lệ.

- Người dùng đã chạy script start; agent xác nhận MSSQL$SQLEXPRESS Running. Preflight read-only `-E -N` dừng ở ODBC 17 TLS: certificate chain không được tin cậy. Chưa thiết lập SQL session nên auth mode/quyền/collision đều chưa xác minh; chưa tạo DB/login/grant/guard/fixture.
- [Evidence lỗi TLS và điều kiện còn thiếu](m1br_20261024_01-preflight-evidence.md), [manifest rỗng](m1br_20261024_01-manifest.json). Không bỏ kiểm chứng chỉ hoặc đổi instance/trust/ACL/quyền SQL. Service giữ Running vì chưa xác minh ảnh hưởng nếu stop. Boundary acceptance/CLI chưa chạy; lượt này chỉ kiểm tài liệu/JSON/diff, không SQL/auth PASS mới. Chưa đủ nghiệm thu.

## SQL boundary sau chấp thuận ad6a7f7 — 24/10/2026

- Bỏ Windows-only checker/expected-secret, thêm SQL context/role/effective permissions/guards gate, audit server identity, DDL migration bảo mật riêng với 2 triggers CALLER/grants. DDL chưa apply, chưa provisioning roles/logins/grants. Batch/delivery không đổi; không M2–M4.
- [Mapping design/code/tests và giới hạn](m1-sql-boundary-implementation.md); build boundary acceptance riêng đạt, suite regression Release hiện 42/42 (bỏ 2 tests Windows cũ), không gọi đó là least-privilege/CLI PASS. Còn cần DBA/user duyệt [danh sách provisioning giới hạn](m1-sql-boundary-provision-request.md). Chưa start SQLEXPRESS/tạo login/grant/đổi OS/ACL hoặc SQL chung. HEAD/CI xem PR #33; chưa đủ nghiệm thu.

## Đổi boundary operator — một diff chờ review trước code

- Người dùng chốt backend offline CLI độc lập nền tảng chức năng, không OS account/SID/ACL cố định hoặc VM. [Contract/spec/runbook/checklist thay thế](../contracts/m1-offline-operator-db-boundary-review.md) chờ reviewer kiểm một lượt trước code/DDL: DB credential do DBA cấp, object grants + guard Employee/audit, actor reset/invariant recovery giữ nguyên.
- Chỉ tài liệu, chưa code/schema/OS/SQL/grants; batch/delivery giữ nguyên. Chưa positive CLI mới, không báo đủ nghiệm thu hoặc dùng 44/44 cũ thay bằng chứng boundary mới. Không Goal/Plan/merge/approve/comment/gửi tin.


## ACL deployment follow-up — tài liệu chờ review

- Khảo sát read-only xác nhận CheckChain quá rộng: C:\ TrustedInstaller/unprotected, AU create-subdirectory và inherit-only; ProgramData Users 0x116 có create và metadata writes; Program Files Users RX, TrustedInstaller owner. Chưa có bằng chứng máy này không thể an toàn; không đổi ACL/provision hoặc code.
- [Diff contract/thiết kế/test/quyền provision hẹp](../contracts/m1-operator-acl-scope-review.md): app subtree Program Files cố định, mutation từng cạnh/owner/deny/inheritance/reparse/race thay “mọi ancestor chỉ SYSTEM/Admin/protected”. Cần review trước code; VM/root ACL không còn được coi yêu cầu mặc định. Batch 48171fe giữ nguyên. Chỉ kiểm tài liệu/link/diff, chưa SQL/auth/CLI mới; không báo PASS/đủ nghiệm thu.

## Batch eligibility và positive CLI sau review 517211c

- IdentityMailSink đẩy điều kiện verification used/invalidated/expiry vào SQL trước Order/Take(25), kiểm lại trong transaction trước delivery. Regression host thật với 25 message invalidated do resend/used/expired phía trước: worker tự chuyển message hợp lệ; assert sink/outbox, không helper drain thay lifecycle. Release SQL/auth mới **44/44**, 0 failed/skipped; HEAD/CI cuối xem PR #33.
- [Kế hoạch deployment Windows test và quyền còn thiếu](m1-operator-positive-deployment-check.md): khảo sát read-only token không elevated, policy cố định chưa có. Chưa chạy positive CLI, chưa thay quyền OS/SQL/policy chung. Đã ghi đường dẫn/owner/ACL/principal/DB/lệnh/tác động/khôi phục để quản trị kiểm. Service tests/CI không thay CLI PASS; M1 chưa đủ nghiệm thu.

## Sửa implementation hai P1 sau chấp thuận 8a6a0e4

- Đã thay operator env trust bằng Windows SID/native fixed policy + ACL toàn chuỗi + Integrated SQL; nối hosted mail dispatcher/readiness, production intake 503, retry/race/dedup theo sink instance. Không schema mới hoặc email người thật.
- [Mapping finding/file/test, runbook và giới hạn](m1-p1-implementation-verification.md). Release SQL/auth local 41/41, không skip. Chưa kiểm positive CLI qua deployment policy/ACL/SQL principal quản trị; không thay OS/DB grants chung, không gọi service test là ACL positive. HEAD/CI cuối xem PR #33; không dùng 35/35 cũ. Chưa nghiệm thu/production-ready, không approve/merge.

## P2 contract delivery tại 7b2c1c7 — sửa giới hạn dedup

- Chỉ sửa lời hứa delivery và acceptance/checklist: dedup cùng sink instance/lifetime; message ID từ outbox đã commit giữ nguyên qua retry. Delivery không rollback cùng SQL; sink host độc lập/restart có thể nhận lại, application lock không bảo đảm exactly-once. Không thay operator, lifecycle/production 503 hoặc thêm schema/code.
- Test dự kiến phân biệt shared instance với hai sink độc lập, count nhận và trạng thái outbox, token auth một lần. Chưa chạy SQL/auth/test delivery; kiểm diff/link/nhất quán tài liệu, không gọi 35/35 cũ là bằng chứng mới. Giữ #33 Draft/chờ review contract.

## Hai P1 implementation #33 tại 9fa35ab — chờ review bổ sung hẹp

- Xác nhận operator tin env allowlist/expected secret/connection caller đặt và mail chưa có dispatcher trong lifecycle. M1 chưa được coi ready/nghiệm thu; 35/35/CI cũ không bao phủ hai nguyên nhân này.
- Chuẩn bị [contract boundary/delivery](../contracts/m1-p1-operator-delivery-review.md) cùng cảnh báo runbook và acceptance checklist; đây là thay đổi contract thực chất về deployment/operator và mức bảo đảm delivery. Chưa code phụ thuộc trước review phần đổi, không thay schema hoặc Goal mới, không mở M2–M4.
- Gói này chỉ tài liệu: kiểm diff/allowlist/link; chưa chạy SQL/auth suite mới hoặc khẳng định test P1 PASS. Sau approval mới triển khai/chạy tests và CI theo HEAD code mới. Không approve/merge/comment hoặc gửi tin thành viên/reviewer.

## M1 implementation đang thực hiện — 23/10/2026

- Reset tests mới: suite 35/35 đạt với SQL active-session revocation, actor negatives trực tiếp Reset, Customer target/inactive target, hash/revoke/audit rollback và child-process stdin redirect/no-secret-output. Chờ CI HEAD cuối trước bàn giao ready; không dùng 33/33 cũ thay bằng chứng reset.

- Reset đã triển khai theo `f30641b` được người dùng chấp thuận. Release local toàn suite 33/33 chạy lại với code reset: legacy login qua Admin recovery, snapshot actor stale, target ID/version negatives, audit failure rollback, hai SQL connection chỉ một reset commit, mật khẩu cũ bị thay/Locked vẫn chặn và ledger không đổi. Có runbook handoff; chưa xác nhận CI HEAD code mới, chưa ready.

- Người dùng chấp thuận contract reset `f30641b6463858a2c10691054cf9090f3927521f` và báo cáo review qua chat điều phối ngày 23/10/2026 (ủy quyền nguyên văn chuyển về chat triển khai). Gate phần mới đã đáp ứng, không phải approval GitHub/thành viên. Đang triển khai operator reset, chưa có test reset PASS; phải kiểm SQL/CI HEAD code mới trước ready. Các dòng chờ review phía dưới là lịch sử.

- Bổ sung reset-employee-password: người dùng đã duyệt phạm vi, contract/đặc tả/checklist mới vẫn chờ review độc lập. Chỉ cập nhật tài liệu trên nhánh hiện tại, chưa viết lệnh hoặc schema; chưa có kết quả test reset. Giữ actor offline Admin active + QuanLyTaiKhoan, không Director/self-reset; target ID/version bất biến, hash/revoke/version/audit nguyên tử, không đổi status/duty/quyền hoặc mở lại recovery. Không complete/approve/merge. Sau approval mới triển khai và chạy SQL suite/CI theo HEAD code mới; kết quả 33/33 cũ không bao phủ reset.

- Release local mới 33/33 đạt, thêm CCCD race/rowversion/FK, outbox rollback, biên midnight Saigon và HTTP length/page/search. Khoảng trống reset legacy vẫn chờ quyết định/review contract, chưa code. CI HEAD cuối chưa kết luận.

- Phát hiện thiếu đường cấp lại mật khẩu legacy cho account nhân viên hiện có ngoài Admin recovery; đã đề xuất lệnh offline reset để người dùng quyết định/review contract trước code phần đó. Chưa triển khai hoặc tự sửa contract. CI run `39930119878` tại `a93cf99` đang chạy ở lần kiểm; không coi run cũ bị cancel là PASS.

- Release suite 32/32 đạt trên SQL LocalDB: preflight duplicate legacy trước DDL, dependency 503, SecurityVersion/session missing/refresh expired/verification expired bổ sung. Đang đối chiếu checklist cuối; CI phải xem SHA mới, chưa ready.

- Bổ sung test local: register hai connection cùng normalized email chỉ một account/token/outbox; email 254 không cắt username, SQL chặn customer duty sai/employee thiếu FK; operator auth negative và mock coordinator. Chưa đồng nhất HEAD cuối/CI; không báo hoàn tất.

- Release suite mới 24/24 đạt: thêm revoke/write hai thứ tự, confirm cạnh tranh, recovery manifest negative, backup/restore ledger, validation HTTP; publish API không chứa operator/tests. Vẫn chưa đủ checklist trước ready; xem verification hiện hành.

- Working tree bổ sung test đạt 21/21: audit rollback Profiles/refresh, bootstrap/admin negative và mail retry/resend. CI implementation `670b599` backend SQL/web/Windows success ở lần kiểm; chưa xác nhận mobile và chưa áp cho test mới. Chi tiết/ca thiếu trong tài liệu verification.

- Bổ sung kiểm chứng: recovery audit-trigger failure rollback/hai operator, rotation/logout hai thứ tự, HTTP sáu chức danh/Admin XemHoSo/PUT bất biến/account locked/scope A-B/fixture guard đã chạy đạt trong suite 19 test. [Cách chạy và giới hạn hiện hành](m1-implementation-verification.md); chưa đủ checklist, chưa ready.

- Nhánh `codex/m1-implementation` phụ thuộc PR #32; đặc tả kỹ thuật được người dùng chấp thuận tại `20a9a72b`, gate hành chính commit `053ee86`. Chưa hoàn tất, chưa PR sẵn sàng review; không merge PR tài liệu hoặc các PR khác.
- Đã có model/mapping/migration bổ sung `20261023073816_M1Identity`, Identity services/routes, Profiles auth/scope/transaction, operator executable riêng và SQL tests. Chưa coi scaffold/build hoặc số test hiện có là đủ toàn checklist.
- Kiểm thực tế trên working tree hiện tại: `dotnet test backend/TiemChung.slnx --no-restore` với `M1_SQL_TEST_CONNECTION` trỏ instance `(localdb)\KLCN_M1_Test` riêng: 19 passed, 0 failed, 0 skipped. SQL Server LocalDB 17.0.4025.3; test tạo DB synthetic `KLCN_M1_Test_<guid>`, không dùng dữ liệu thật. Bao phủ DB sạch/constraint, upgrade Admin cũ/recovery/stale-version/retry, rotation hai connection/replay/revoke, HTTP anonymous/unlinked/web CSRF và unit token/model/rate-limit.
- Chưa kiểm đủ: toàn ma trận Profiles/fixture guard, audit failure và rollback/cạnh tranh mọi thứ tự, bootstrap/operator negative cases, mail sink retry, backup/restore, multi-tab coordinator mock, production publish và CI mới. GitHub CI SQL được cấu hình nhưng chưa chạy theo SHA implementation. Không báo SQL/auth M1 hoàn tất.

## Đặc tả M1 Profiles + Identity — 23/10/2026

- Quyết định trực tiếp của người dùng trong chat ngày 23/10/2026 thay gate riêng M1: dùng báo cáo kỹ thuật từ chat độc lập tại `20a9a72b42c660bb642cac213045616a111de8ee` và chấp thuận contract/nghiệp vụ của người dùng thay Bảo/Quý xác nhận trước code. Được tiếp tục implementation nhánh riêng phụ thuộc #32; không phải approval thành viên/GitHub, không đổi owner/branch protection. Contract/schema mới đổi thực chất vẫn cần review độc lập phần đổi. Nội dung kỹ thuật không đổi, các ghi nhận chờ approval phía dưới là lịch sử trước quyết định này. SQL/auth/runtime M1 chưa kiểm chứng.

- Sửa recovery theo review `427f52a9`/base `f1880843`: bind RowVersion sau migration trong maintenance, kiểm lại trong transaction; ledger unique DB/mốc M1 và retry trả kết quả đã commit, không reset theo secret/manifest. Checklist bổ sung stale-version/rollback/retry/manifest mới/admin khóa lại/hai operator. Chỉ kiểm tài liệu; SQL/auth/runtime chưa chạy.

- Sửa review `ff1294b9`/base `f1880843`: recovery nâng cấp manifest/ledger/audit, invariant revoke credential, Web Locks/CSRF và bearer web memory-only; checklist có ánh xạ finding → sửa → ca kiểm dự kiến. Chỉ tài liệu, chưa chạy SQL/auth/runtime; tự rà không thay review độc lập.

- Người dùng duyệt phạm vi M1 và cho phép PR Draft tài liệu từ develop; [gói review](m1-identity-review.md) gồm contract/quyền năm route, schema đề xuất, quyết định kỹ thuật và checklist SQL/auth. Contract/schema còn chờ review độc lập, chưa được coi đã chấp thuận triển khai.
- Chỉ sửa tài liệu/đặc tả; không migration, DTO/API/client, nguồn/ảnh gốc; không kéo PR #29/#30/#31 chưa tích hợp hoặc commit 38c4f84/fa027ee. Không merge, không Goal code M1. M2–M4 giữ lộ trình.
- Kiểm tra tài liệu: git diff --check, liên kết Markdown mới và allowlist sáu file thay đổi. Rà toàn status phát hiện năm link lịch sử đã hỏng trên base (analysis/wpf-phase0-screen-spec.md, design/wpf-phase1-layout-proposal.md, analysis/wpf-phase3-binding-preflight.md, apps/windows/ViewsV2/README.md, screenshots/a02-screen.png); không sửa ngoài phạm vi. Không chạy build/test/SQL/auth/runtime cho gói chỉ tài liệu; checklist SQL thật là kế hoạch chưa thực thi, CI của PR cần xem riêng. Tự rà tài liệu không thay review độc lập.

## Windows Login, A06, A09, A11, A12, A14 — 2026-10-22

- PR #26 implementa Login riêng và các màn A06 Tiêm & theo dõi, A09 Lịch & nhắc lịch, A11 Báo cáo, A12 Nhân viên & tài khoản, A14 Đổi mật khẩu. Mỗi màn có service/sample service, ảnh review và status riêng; không sửa Theme chung, MainWindow hay menu.
- Login dùng `IAuthService`/`SampleAuthService`; A06/A09/A11/A12/A14 chỉ thao tác dữ liệu mẫu, chưa có endpoint thật cho các luồng này. README ghi backend chưa có xác thực. Mật khẩu không lưu ở client và cần hash tại backend.
- Đã reset ViewModel tài khoản/đổi mật khẩu khi bắt đầu phiên mới để không giữ state người dùng trước. A11 xóa snapshot/kết quả xuất khi thay bộ lọc cho đến khi tạo báo cáo lại.
- Kiểm tra: `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release` — thành công, 0 warning, 0 error; CI của PR #26 thành công tại lần kiểm tra hiện tại. Các ảnh normal/error/empty và giới hạn từng màn ở `docs/ui-review/ui-login-a06-a09-a11-a12-a14/` và `docs/status-login.md`, `docs/status-a06.md`, `docs/status-a09.md`, `docs/status-a11.md`, `docs/status-a12.md`, `docs/status-a14.md`.
- Chưa kiểm chứng API, phân quyền phiên, ghi dữ liệu nghiệp vụ, gửi nhắc thật, xuất file thật, xác thực/đổi mật khẩu thật hoặc DPI khác.

## Sửa góp ý review phase3 PR #22 — 2026-10-22

- Nhánh `ui/phase3-review-fixes` xử lý 5 góp ý: bỏ khởi tạo kép `StartupUri`/shell; chuyển nguyên cột kết luận/chỉ định A02 khi thu hẹp; điều hướng nhóm đơn đến màn của nhóm; tải lại snapshot A01 (bảng, luồng, chỉ số) theo cùng khoảng ngày và đặt số liệu về 0 khi rỗng; bind nhãn nguồn A02.
- Kiểm tra Release build 0 warning/error và harness WPF STA tạm ngoài repo cho 5 luồng đều PASS; ảnh A01/A02, lệnh và giới hạn tại [status riêng](status-phase3-review-fixes.md). Chưa kiểm tra đăng nhập thủ công, DPI/màn hình hoặc backend thật.

## Quy trình UI Windows và phân công tài liệu — 2026-10-22

- Nhánh `ui/docs` bổ sung `docs/ui-workflow.md` làm tài liệu bắt buộc đầu phiên UI; Plan ghi Đạt làm toàn bộ UI Windows, Bảo/Quý phụ trách API module tương ứng. Design Spec bổ sung `SegmentedButton` và `Card clickable`; không sửa code, contract, schema hoặc tài liệu nguồn.
- Kiểm tra: rà liên kết/tệp tài liệu, `git diff --check` và kiểm tra phạm vi file thay đổi. Các kiểm tra build/test trong `docs/testing.md` và ảnh UI không chạy cho lượt chỉ sửa tài liệu; CI của PR cần xem sau khi mở, chưa được xem là đạt.
- Tài liệu về quyền, seed và endpoint chỉ là đặc tả; backend và UI chưa được kiểm chứng thực thi. Các tệp chưa theo dõi của `ui/phase3` được giữ nguyên, không đưa vào commit tài liệu.

## Chốt tài liệu vai trò, ma trận và token UI — 2026-10-22

- Trên nhánh `ui/docs`, ghi nhận Phương án 1 với sáu chức danh, 12 mã quyền logic cần backend seed, ma trận A01–A12/A14 gồm 78 ô ĐÃ DUYỆT; Login công khai. A04 tách quyền chuyên môn/giá theo nhóm trường, A12 dùng tab Quyền bổ sung. Đây là đặc tả, chưa có seed hoặc kiểm tra quyền chạy thật.
- A01 đổi đặc tả bộ lọc sang Từ ngày/Đến ngày; Design Spec thêm nhãn cho sáu trạng thái luồng, hết hạn và component Alert.Safety, Alert.Info, Badge, Tab, trạng thái chọn danh sách. Plan được lưu tại `docs/windows-ui-redesign-plan.md` để version cùng các đặc tả; bản ngoài repo được đồng bộ.
- Kiểm tra tài liệu: rà đủ 13 hàng × 6 cột ma trận, 12 mã quyền và `git diff --check`. Chưa build/chạy UI hoặc kiểm tra API vì chỉ sửa tài liệu. Các thay đổi code có sẵn trong working tree không thuộc commit tài liệu này.

## Hoãn tích hợp quyền UI — 2026-10-22

- Theo chỉ định mới, chưa kiểm tra chức danh/quyền UI; chờ MainViewModel có dữ liệu quyền (shell hiện tại là `ShellViewModel`). Screen Spec và Plan ghi yêu cầu khi triển khai ViewModel: để sẵn `CanXxx: bool` mặc định `true`, kèm TODO nối quyền. Validation, chọn bản ghi, trạng thái tải/lưu và ngăn gửi trùng vẫn độc lập.
- Ma trận 13 màn/78 ô vẫn ĐÃ DUYỆT; lọc menu và khóa/ẩn theo quyền A01/A04/A12 thuộc bước tích hợp sau. Các yêu cầu kiểm tra quyền API vẫn giữ nguyên.
- Lượt này chỉ cập nhật tài liệu và đồng bộ bản Screen Spec ngoài repo; chưa thêm `CanXxx` vào code ViewModel. Kiểm tra nội dung và `git diff --check`; không build/chạy UI.

## Duyệt ma trận truy cập và phân quyền A04 — 2026-10-22

- Người dùng duyệt Phương án 1: sáu chức danh là quyền mặc định, mã quyền là phần cấp thêm. [Screen Spec](desktop-screen-specs.md#ma-trận-truy-cập) có 13 màn × 6 chức danh, toàn bộ 78 ô ĐÃ DUYỆT; Login là màn công khai, không thuộc ma trận. Các mục đề xuất/chờ duyệt ở các bản ghi lịch sử phía dưới phản ánh trạng thái trước lần duyệt này.
- A04 tách `QuanLyDanhMucChuyenMon` cho `BacSi` và `QuanLyDanhMucGia` cho `Admin`, Sửa theo từng nhóm trường; cập nhật A12 và danh sách seed thành 12 mã quyền logic. Admin kỹ thuật phải được seed đủ quyền, gồm `QuanLyTaiKhoan`; Giám đốc không được có quyền này. `GhiThanhToan` gồm xem và ghi; `XemThanhToan` chỉ xem.
- Bổ sung quyền đọc dữ liệu theo phạm vi cho A02/A06/A07/A08 mà không cấp xem màn nguồn; A01 làm mờ/khóa thẻ chuyển hướng thiếu quyền. UI ẩn/khóa cho tiện; API kiểm tra quyền tại mọi endpoint ghi và phạm vi dữ liệu của endpoint đọc hẹp.
- Plan ngoài repo cập nhật Phase 1: MainViewModel (trách nhiệm hiện thuộc `ShellViewModel`) nhận chức danh + quyền, chỉ hiện mục menu được phép; ghi lịch sử phê duyệt. Đồng bộ Screen Spec trong repo và thư mục Antigravity.
- Chỉ sửa tài liệu. Đã kiểm tra 13 hàng/6 cột, đủ 78 ô ghi ĐÃ DUYỆT, không còn mã quyền A04 cũ trong nội dung hiện hành, danh sách 12 mã quyền khớp Screen Spec/Plan và `git diff --check`. Hash 143 file ngoài tài liệu trước/sau không đổi. Chưa triển khai/kiểm thử UI phân quyền, seed, auth hoặc endpoint; cách backend nhận diện Giám đốc/ngăn cấp nhầm và chi tiết contract vẫn cần làm rõ khi triển khai.

## Cập nhật tài liệu chức danh và quyền bổ sung — 2026-10-22

- Theo yêu cầu mới, [Screen Spec 14 màn](desktop-screen-specs.md) giữ sáu `ChucDanh`, áp dụng bảng ánh xạ nhiệm vụ sang chức danh và quyền bổ sung; rà mục Vai trò của cả 14 màn và đổi A12 thành tab Quyền bổ sung. Plan ngoài repo được cập nhật cùng quy ước và lịch sử. Bản Screen Spec trong thư mục Antigravity được đồng bộ với bản trong repo.
- Giám đốc không được quản lý tài khoản; `Admin` không mặc định có quyền A12. Tách quyền xem thanh toán khỏi quyền ghi; liệt kê 11 mã quyền logic đề xuất cần backend seed/ánh xạ với `MaQuyen` kiểu `int`. Chưa seed, sửa schema, code hoặc triển khai auth/chính sách quyền.
- Thêm ma trận 14 màn × 6 chức danh; tất cả 84 ô đánh dấu đề xuất chờ người dùng duyệt. Ánh xạ do người dùng chỉ định đã được ghi nhận; mức truy cập và các quyền đề xuất thêm chưa được coi là đã chốt.
- Kiểm chứng tài liệu: đủ 14 mục Vai trò dùng sáu chức danh hợp lệ, đủ 14 hàng/6 cột và dấu đề xuất, danh sách quyền Screen Spec/Plan khớp nhau; `git diff --check` đạt. Hash 143 file ngoài tài liệu trước/sau không đổi. Không build/chạy UI hoặc kiểm tra quyền API; cách backend nhận diện tài khoản Giám đốc và ngăn cấp nhầm còn cần thiết kế/review.

## Cập nhật tài liệu UI — 2026-10-22

- Chỉ chỉnh tài liệu: `docs/design/design-spec-clinical-emerald.md` và Plan `C:\Users\DAT\.gemini\antigravity\brain\631625ed-19eb-4eb5-8495-83b298a03164\windows-ui-redesign-plan.md` (Plan nằm ngoài repo). Plan đối chiếu `ViewsV2/`, `ViewModels/`, `Themes/`, menu ngang hai cấp, tên mục hiện tại, Sample bật/tắt, bước QA từng màn và 5 vai trò chưa khớp `ChucDanh` với 2 phương án chờ quyết định.
- Design Spec bổ sung màu cảnh báo an toàn/trạng thái luồng/vô hiệu và nền nút chính `#047857` cho chữ trắng. Tỷ lệ tương phản tính từ hai mã màu xấp xỉ 5,5:1; chưa kiểm tra render WPF hoặc trạng thái hover/pressed/disabled.
- Đã kiểm tra nội dung bằng đối chiếu Screen Spec, `ShellViewModel`, `ViewsV2/README.md` và `git diff --check`. Không build/chạy UI vì không sửa code. Các thay đổi code/local có sẵn trong working tree không thuộc lượt chỉnh tài liệu này.

| Phần | Trạng thái thực tế |
|---|---|
| Web React | Có UI kế thừa, client HTTP và nút kiểm tra API; lint/build pass tại máy khởi tạo |
| Windows WPF | Shell + A01 và A02 trong ViewsV2; A02 có validation sinh tồn/kết luận và chỉ định Sample, chưa lưu API. Release build 21/10/2026 pass, 0 cảnh báo/lỗi; đã chụp ảnh toàn màn hình. Chưa hoàn tất các màn còn lại hoặc kiểm tra DPI |
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

## Phân tích WPF — Giai đoạn 0, 2026-10-21

- Đã lập [Screen Spec dữ liệu và hành vi](analysis/wpf-phase0-screen-spec.md) cho Login, A01–A14 và ngữ cảnh phiên/điều hướng. Mỗi màn có mục đích/vai trò, trường dữ liệu/kiểu/bắt buộc, hành động/điều kiện, trạng thái đặc biệt, validation và dữ liệu chưa được UI kết nối.
- Căn cứ: 27 thực thể Domain, mapping EF, RegistrationRules, CatalogContracts, Windows API client, endpoint hiện có, test source và logic handler C#. Không dùng nội dung XAML cũ để lấy bố cục hoặc style; chưa viết giao diện mới.
- Chưa có ViewModel/ICommand trong Windows: không thể liệt kê property ViewModel bị UI bỏ quên như thể các lớp đó đã tồn tại. Tài liệu ghi riêng các trường Domain/DTO chưa nối, trạng thái code-behind đã dùng và phần transient state chỉ là đề xuất.
- Các điểm cần review trước triển khai: dữ liệu dị ứng/chống chỉ định, liên kết chỉ định–mũi tiêm–đăng ký, unique theo dõi mỗi mũi, công thức/đơn vị ưu đãi, báo cáo và chính sách Identity. Các validator không có bằng chứng được ghi là đề xuất/chưa xác nhận.
- Giữ nguyên file tham khảo `docs/desktop-screen-specs.md`, theme và các thay đổi local đã có. Đợt này chỉ thêm tài liệu phân tích và ghi trạng thái; không build/chạy UI hoặc thay schema/contract. Kiểm tra tài liệu bằng đối chiếu tên trường, đủ màn/mục, liên kết nguồn và hash code trước/sau.
- Bước tiếp theo: review những quy tắc/contract còn thiếu theo owner; chỉ bắt đầu thiết kế Clinical Emerald và viết UI khi chuyển sang giai đoạn tiếp theo.

## Đề xuất bố cục WPF — Giai đoạn 1, 2026-10-21

- Đã lập [đề xuất bố cục mới](design/wpf-phase1-layout-proposal.md) cho khung ứng dụng, LOGIN và A01–A14. Mỗi màn có wireframe ASCII, lý do gom cụm, hành vi/trạng thái và cách chuyển bố cục khi thu hẹp. Trạng thái toàn bộ đề xuất: **chờ người dùng duyệt**, chưa triển khai giao diện.
- Đề xuất điều hướng theo năm nhóm công việc; A13/A14 truy cập từ hồ sơ cá nhân. Các màn dùng cấu trúc theo nhiệm vụ: bàn khám, sổ lô, ca tiêm, đăng ký ba bước, sổ giao dịch, agenda và báo cáo theo tham số. Không dùng XAML cũ làm nguồn bố cục/style.
- Đã đưa bản sao nguyên nội dung [Design Spec Clinical Emerald](design/design-spec-clinical-emerald.md) vào dự án; SHA256 khớp file gốc trong thư mục Antigravity. Bản gốc không thay đổi. Tinh chỉnh màu nút chính/chữ để dễ đọc được ghi riêng là đề xuất cần duyệt, không sửa Design Spec gốc.
- Giữ nguyên các giới hạn Giai đoạn 0: thiếu ViewModel và contract nghiệp vụ; không tự suy luận công thức ưu đãi, dị ứng/chống chỉ định, quan hệ chỉ định–mũi tiêm, nhiều người trong một đăng ký hoặc chính sách quyền. Duyệt bố cục không đồng nghĩa duyệt thay schema/contract.
- Kiểm tra: đủ 15/15 màn có wireframe/lý do/trạng thái/quy tắc thu hẹp; hai liên kết tài liệu trong đề xuất đều tồn tại; SHA256 của 33 file Windows/backend/test và hai Screen Spec trước/sau không đổi; `git diff --check` không báo lỗi. Giữ nguyên các thay đổi local `App.xaml`/Themes đã có từ trước lượt này.
- Chưa kiểm tra: giao diện chạy thật, DPI, tương tác hoặc build của UI mới vì giai đoạn này chỉ tạo tài liệu. **Bước tiếp theo: chờ người dùng duyệt mục 5 trong đề xuất; sửa đề xuất theo phản hồi trước khi bắt đầu XAML.**

## Nền Theme WPF — Giai đoạn 2, 2026-10-21

- Theo yêu cầu Giai đoạn 2, đã thay nội dung bốn file `apps/windows/Themes/Colors.xaml`, `Typography.xaml`, `Controls.xaml`, `Cards.xaml` bằng ResourceDictionary viết mới, không kế thừa dictionary cũ của dự án. Key mới `CE.*`, style có key để dùng cho màn mới; chưa đổi bố cục màn hiện tại.
- Có token màu/khoảng cách/bo góc, Segoe UI Variable/Segoe UI, nút chuẩn/WPF UI, input/password, ComboBox, các control chọn/ngày, DataGrid, card/badge/callout và trạng thái hover/pressed/selected/focus/disabled/validation tùy control. [Hướng dẫn dùng và giới hạn](../apps/windows/Themes/README.md).
- `App.xaml` đã merge đủ bốn file sau dictionary WPF UI nên giữ nguyên. Template mới không dựa vào theme cũ; wrapper cho input WPF UI giữ template thư viện để không mất placeholder/reveal/clear. Không sửa ViewModel, C#, Service, schema, source hoặc MainWindow.
- Giữ màu Primary theo Design Spec gốc; cung cấp biến thể `PrimaryStrong` riêng cho phương án tăng tương phản, không tự áp dụng toàn cục hoặc coi bố cục Giai đoạn 1 đã được duyệt toàn bộ.
- Kiểm tra: Release build bằng .NET SDK 10.0.401 thành công, **0 warnings, 0 errors**. Kiểm tra WPF STA tạm ngoài repo nạp qua App, tạo/measure/arrange **145 resource, 50 style thuộc 4 dictionary** thành công; kiểm tra viền validation input, ComboBox editable và pill selected pass. Lỗi property CornerRadius không tồn tại trên input WPF UI ở build đầu đã được bỏ và build lại thành công.
- Chưa kiểm tra: visual hoàn chỉnh, popup bằng thao tác bàn phím, DPI/work area và các màn nghiệp vụ. Chỉ kiểm tra Theme; không báo hoàn tất thiết kế lại toàn bộ app.
- Bước tiếp theo: nhận chỉ định màn/nhóm màn để dựng mới theo bố cục được duyệt, sử dụng bộ `CE.*`; kiểm tra visual, tương tác, cửa sổ và DPI trên từng nhóm.

## Chuẩn bị Giai đoạn 3 — 2026-10-21

- Đã kiểm tra lại cấu trúc Windows và contract binding: chưa có ViewModel/ICommand/DataContext trong C# được kiểm kê; yêu cầu còn để `[tên màn]`. Chi tiết và danh sách property/Command shell còn thiếu tại [kiểm tra binding Giai đoạn 3](analysis/wpf-phase3-binding-preflight.md).
- Tuân thủ yêu cầu hỏi trước khi thêm property/logic: chưa tạo binding giả, ViewModel, dữ liệu hoặc màn mới. Không mở XAML trong `_Legacy/` hay XAML màn cũ. Không sửa runtime; chưa có build/kiểm tra UI mới cho Giai đoạn 3.
- Bước tiếp theo: người dùng chọn mã/tên màn và cung cấp đường dẫn ViewModel, hoặc cho phép tạo ViewModel UI tối thiểu. Sau đó mới chốt mapping và triển khai shell + màn đó trong ViewsV2.

## Shell + A01 mới — Giai đoạn 3, 2026-10-21

- Người dùng chọn A01 và cho phép tạo ViewModel UI tối thiểu trong `ViewModels/`, một file RelayCommand chung; Sample/TODO riêng, không thay nghiệp vụ/service/data access. Đã liệt kê file dự kiến trước khi làm.
- Đã viết mới `ViewsV2/MainWindow` và `ViewsV2/OverviewView`, dùng Theme CE. Shell có nhóm ngang/menu dọc khi hẹp, tìm chức năng, menu tài khoản và phản hồi cho màn chưa triển khai. A01 có chọn ngày, lọc/tìm danh sách, trạng thái công việc, tóm tắt và health API riêng. [Mapping binding, cách chạy và giới hạn](../apps/windows/ViewsV2/README.md).
- Tạo đúng `ShellViewModel`, `OverviewViewModel`, `ViewModelBase` và `RelayCommand.cs`; chỉ Sample phục vụ A01 tại `ViewModels/Samples/OverviewSampleData.cs`, có badge/nhãn nguồn/TODO. Không tạo VM cho màn khác; không thêm endpoint, quy tắc tiền/y tế hoặc truy cập SQL.
- App startup gọi lại Login cục bộ hiện có, sau đăng nhập mở shell V2. Đăng xuất gọi lại Login cũ; chưa thiết kế lại Login. `MainWindow.xaml`/code-behind cũ vẫn giữ nguyên. Không mở XAML trong `_Legacy/` hoặc dùng XAML cũ làm bố cục.
- Kiểm tra: Release build 0 warnings/0 errors; harness WPF STA **26 checks pass**, không có warning/error binding trong các trạng thái đã chạy. Đã kiểm tra lọc/tìm/ngày không hợp lệ/CanExecute, phản hồi điều hướng, menu/focus, cửa sổ rộng 1440×900 và hẹp 980×620 DIP, summary đổi vị trí, các SystemCommands thu nhỏ/phóng to/khôi phục. Đã rà ảnh render rộng/hẹp/menu/cuối trang.
- Nhánh health thành công/thất bại và ngăn gửi trùng được kiểm tra bằng endpoint fixture tạm qua client HTTP hiện có; không phải kiểm thử backend/SQL thật. Hash 37 file gốc Windows/backend/contracts trước/sau: chỉ hai App file thay để tích hợp, 35 file khác không đổi. `git diff --check` không báo lỗi.
- Chưa kiểm tra: Login end-to-end tự động, calendar/toàn bộ thao tác bàn phím, DPI 125/150%, nhiều màn hình, quyền/xác thực thật và các màn ngoài A01. Không coi dữ liệu Sample hay UI pass là nghiệp vụ hoàn chỉnh.
- **Bước tiếp theo:** người dùng review shell/A01 và chọn màn tiếp theo; chỉ tạo ViewModel của màn đó khi bắt đầu. Các mục chưa rõ nghiệp vụ vẫn cần review theo owner/contract.

## A02 ViewsV2 — hoàn thiện theo mục 3 Screen Spec — 2026-10-21

- **3.1–3.2:** có hàng đợi, chọn lượt, mã đăng ký, thông tin người được tiêm/khách hàng, dịch vụ đăng ký, bốn trường sinh tồn, tiền sử dị ứng, bệnh mãn tính, kết luận/lý do, danh sách chỉ định chọn vaccine và ghi chú bác sĩ. Dữ liệu hồ sơ/catalog là `Sample`, có TODO trong `ViewModels/Samples/ScreeningSampleData.cs`. Mã phiếu, nhân viên, thời điểm và danh mục thật chưa có nguồn; ghi chú bác sĩ hiện là bản nháp UI.
- **3.3:** `LuuPhieuKhamVaChiDinhCommand` CanExecute theo lượt đã chọn, sinh tồn hợp lệ, dị ứng bắt buộc, kết luận, lý do khi hoãn/chống chỉ định, không còn chỉ định khi kết luận không đủ điều kiện, và tối thiểu một vaccine đã chọn khi đủ điều kiện. Làm mới chỉ áp dụng lại bộ lọc Sample. Chưa xác nhận vai trò bác sĩ hoặc lưu API vì chưa có auth/service; lệnh chỉ đưa phản hồi UI.
- **3.4:** có trạng thái hàng đợi rỗng, alert dùng `CE.Alert.Safety`, và gợi ý “Tạm hoãn” ở nhiệt độ ≥38°C mà không đổi kết luận. Chưa phát hiện từ khóa dị ứng hoặc yêu cầu xác nhận hai lần vì chưa có danh sách từ khóa/chính sách chuyên môn được xác nhận; alert không tự chẩn đoán.
- **3.5:** kiểm tra mạch 40–200, nhiệt độ 35–42, tâm thu > tâm trương; thông báo lỗi cạnh trường. Không áp thêm giới hạn huyết áp ngoài quan hệ tâm thu > tâm trương được yêu cầu.
- **3.6:** các trường bị giao diện cũ bỏ quên đã có vị trí nhập/hiển thị tương ứng. `MaNhanVien`, `MaPhieuKham`, ID catalog thật và timestamp vẫn chờ phiên/API; không cho người dùng nhập actor tùy ý.
- Reuse các style `CE.*`; chỉ bổ sung `CE.Alert.Safety` trong `Themes/Cards.xaml`. Không sửa backend, contract, schema hoặc XAML `_Legacy/`.
- Build Release `dotnet build apps/windows/TiemChung.Desktop.csproj --configuration Release --no-restore`: thành công, **0 warnings, 0 errors**. Harness STA ngoài repo kiểm tra các biên mạch/nhiệt độ, quan hệ huyết áp, gợi ý sốt, dị ứng bắt buộc, chỉ định bắt buộc/chọn danh mục, lý do bắt buộc và CanExecute Lưu. Đã chụp [ảnh toàn màn hình A02](screenshots/a02-screen.png) ở trạng thái form Sample hợp lệ. Chưa kiểm chứng API/auth/lưu thật, luồng Login end-to-end hoặc DPI.
