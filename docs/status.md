# Trạng thái khung — 19/10/2026

## Đặc tả M1 Profiles + Identity — 23/10/2026

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
