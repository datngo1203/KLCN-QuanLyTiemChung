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
Chỉ người sở hữu module sửa module đó; **thay đổi contract/schema có tác động tới module khác cần owner liên quan review; phần bổ sung độc lập không cần chờ review chéo hình thức** (xem quy tắc chi tiết dưới). Quyết định phân công ngày 02/11/2026: Đạt tiếp tục S5, Bảo triển khai S6, Quý triển khai S7; cả nhóm nghiệm thu S8. Ngoại lệ ngày 31/10/2026 cho phép Đạt dùng Codex làm thay chỉ giải thích giai đoạn trước, không áp dụng mặc định cho công việc giao mới. Giữ nguyên lịch sử đóng góp, không ghi nhận review thay người khác. Không tự cấp hoặc đổi quyền cộng tác GitHub.
Sau thay đổi: chạy kiểm tra đúng phần bị ảnh hưởng; báo đúng kết quả và các phần chưa kiểm tra. Tiến độ chỉ cập nhật cột Trạng thái trong SCOPE.md; không tạo file tiến độ khác.

## Phối hợp mở rộng nghiệp vụ — cập nhật 03/11/2026

Người dùng **cho phép mở rộng phạm vi theo từng lát có đầu ra chạy được**, 03/11 ủy quyền review kỹ thuật/merge X1 và sau PR #74 yêu cầu **bỏ bước review chéo dư thừa khi một lát độc lập đã có Codex test, CI đúng head thành công và điều phối đã review**. Với lát chỉ bổ sung DTO/endpoint/tài liệu riêng một module, không thay contract tiêu thụ bởi module khác, có thể **điều phối tự review và merge**, ghi rõ đó là self-review theo ủy quyền, không giả nhận Bảo/Quý APPROVE. **Vẫn cần review của owner liên quan khi thực sự thay giao diện API/DTO/schema dùng chung có tác động tới module khác, tiền, tồn kho ghi, quyền nhạy cảm hay nghiệp vụ lâm sàng**; không dùng CI xanh để bỏ qua blocker. `SCOPE.md` quyết định phạm vi; chỉ mở một lát đã giao, có code/test/UI theo đúng actor và không áp sổ QD-001–017 tự động.

- **Chỉ một lát tại một thời điểm của Đạt:** X1 đọc kho merge PR #66 (`9ff1101`); X2.1 nhập một lô mới merge PR #71 (`9eba7ee`); **X2.2.1 lịch sử tiếp nhận chỉ đọc merge PR #74 (`c098155`), CI #196 success**. Ba lát đã đóng, không làm lại nếu không có lỗi tái hiện. **X2.2.2 nhập bổ sung lô cũ/quản lý nhà cung cấp/phiếu nhập nâng cao và A02/A04 chưa được triển khai hoặc phê duyệt tiếp**. Không đụng `VaccinationEndpoints` và lưu tiền/ưu đãi ngoài PR tương thích rõ ràng. ChatGPT chỉ điều phối/review/merge/tài liệu; **Codex mới thực thi và sửa code**.
- **Owner giữ quyền kiểm soát nhánh:** Bảo giữ S6 và những chỉnh sửa Registration/Payment của S6; Quý giữ S7 Reports; Đạt giữ X1/S5 và điều phối tích hợp. Không code thay/force-push lên branch của Bảo/Quý. Khi phát sinh mâu thuẫn, ghi trong [issue #64](https://github.com/datngo1203/KLCN-QuanLyTiemChung/issues/64), yêu cầu owner review diff; nếu Đạt được giao sửa thay, làm **PR tương thích riêng**, không tái định danh tác giả, không merge không có review.
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
- Chủ sở hữu module được sửa file riêng của module. File dùng chung (`Program.cs`, `Entities.cs`, `TiemChungDbContext.cs`, `TiemChungDbContextModelSnapshot.cs`, `ShellViewModel.cs`, `NavigationRegistry.cs`, `apps/web/src/App.jsx`, `backend/contracts/README.md`) phải giới hạn diff nhỏ nhất; **review chéo owner khi thay đổi có tác động thật tới contract/schema/quyền/luồng module khác**. Chỉ bổ sung DTO/API/tài liệu đọc lịch sử riêng kho mà không đổi các module khác không tự động phải chờ Bảo/Quý review. Không sửa file người khác để 'làm cho chạy' ngoài gói.
- Đạt điều phối migration: chỉ **một người** thay schema/migration tại một thời điểm, không sửa migration đã áp dụng, không tự migrate/reset/reseed DB demo hoặc dùng chung DB test đang có người khác kiểm. Quý làm S7 trên truy vấn chỉ đọc, không cần migration theo phạm vi đã chốt.
- Đạt duy trì `AGENTS.md`, `SCOPE.md`, `README.md` chung khi cần đồng bộ. Tiến độ chỉ cập nhật cột Trạng thái trong `SCOPE.md`; không mở thêm sổ tiến độ. GitHub Issues là nguồn giao việc, nội dung issue cũ xung đột `SCOPE.md` phải được cập nhật trước khi triển khai.
- Mỗi lát dùng branch riêng từ `main` sạch, PR nhỏ vào `main`, Codex thực hiện code/test; ChatGPT review kỹ thuật và có thể tự merge **lát độc lập đã có test + CI trên đúng head, không blocker** theo ủy quyền người dùng. **Review độc lập chỉ bắt buộc khi thay đổi thực sự ảnh hưởng contract/schema/quyền/luồng module khác hoặc có rủi ro quan trọng đã xác định**, và phải do đúng người thực hiện; không gán review hộ. Không commit thẳng/force-push vào main; không merge khi CI fail, chưa có test phù hợp hoặc có lỗi P0/P1. Giữ rõ các ca WPF chưa chạy thay vì giả nhận PASS.
- Phân công 02/11 thay phần giao module trước đây nhưng **không viết lại lịch sử tác giả của commit/PR**. Đề cương và SCOPE chốt nghiệp vụ; không xem tính năng cũ trong GitHub Issues (đặt cọc/trả góp, mobile, đổi/hủy lịch, nhập kho đầy đủ) là yêu cầu hiện hành.

## Chuẩn tham chiếu VNVC và quy tắc skill khi làm giao diện (03/11/2026)

**Quyết định người dùng:** Website khách hàng của khóa luận phải **bám sát ngôn ngữ thiết kế và trải nghiệm VNVC**; ứng dụng nhân viên **Windows/WPF được thiết kế khác** để ưu tiên thao tác nghiệp vụ, nhưng giữ sự nhất quán về màu, chữ và trạng thái. **Tính năng mở rộng đến đâu phải làm UI thật của chính tính năng ấy đến đó**, không gom nợ giao diện đến cuối.

### Tham chiếu bắt buộc — trước mỗi lát có giao diện
- Website tham chiếu: `https://vnvc.vn/` và **trang đặt/chọn mua** `https://vax.vnvc.vn/vaccine`. Chọn đúng màn trang thực tương ứng nghiệp vụ đang sửa; dùng ảnh/chụp màn hình và CSS/computed styles thực tế nếu Codex có browser. Đối chiếu **font family/weight/size**, xanh chủ đạo, cam nhấn, logo/biểu tượng, khoảng trắng, header/navigation, card, bảng, form, nút, thông báo và responsive; **không tự gán font hoặc hex rồi tuyên bố là màu/font chính thức của VNVC** khi chưa kiểm nguồn. Thời điểm và URL tham chiếu cần ghi ngắn ngay trong PR.
- Ưu tiên tái tạo **phong cách thị giác + cấu trúc tương tác phù hợp phạm vi KLCN**, không copy nguyên HTML/JS/CSS hay tự tuyên bố phần mềm là hệ thống VNVC chính thức. Chỉ dùng logo/ảnh/font nguồn VNVC khi có quyền sử dụng phù hợp hoặc đầu vào người dùng có quyền; ghi nguồn, và giữ nhãn dễ thấy **“Hệ thống mô phỏng phục vụ khóa luận – không phải website chính thức VNVC”** ở phiên bản công khai/demo để tránh nhầm lẫn. Không mang dữ liệu thật của VNVC/khách hàng vào mã nguồn.
- Website React: bám sát trải nghiệm VNVC khi thực hiện các lát Web; thống nhất thành phần/tokens **trên trang đang sửa**. Không tự redesign toàn site khi công việc là một module; dần thay Sample hoặc màn cũ khi tới đúng module. Windows WPF: giữ khung VAC, `ViewsV2/`, `Themes/`, `CE.*`, ưu tiên nghiệp vụ của từng vai trò, readability, keyboard, tốc độ, trạng thái SQL; có thể thiết kế khác Web.

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

## Bí mật và môi trường

Không lưu secret/JWT trong Git hoặc biến VITE_*. Dùng user-secrets, biến môi trường hoặc `.env` local; không ghi CCCD/thông tin y tế vào log. Khi triển khai cần HTTPS và CORS đúng origin. Khi lộ secret, thu hồi/đổi secret trước; không tự xóa lịch sử Git hoặc dữ liệu để che sự cố.
