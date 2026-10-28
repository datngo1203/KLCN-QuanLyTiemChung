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
Chỉ người sở hữu module sửa module đó; thay đổi contract/schema cần người khác review. Không tự thêm GitHub username hoặc quyền cộng tác khi chưa có thông tin.
Sau thay đổi: chạy kiểm tra đúng phần bị ảnh hưởng; báo đúng kết quả và các phần chưa kiểm tra. Tiến độ chỉ cập nhật cột Trạng thái trong SCOPE.md; không tạo file tiến độ khác.

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

## Phân công và Git

| Thành viên | GitHub | Phụ trách |
|---|---|---|
| Bảo | `giabao9373` | Web; API và Windows cho hồ sơ, đăng ký, thanh toán, ưu đãi. |
| Quý | `ngocquydev` | API và Windows cho lịch, nhắc lịch, lịch sử, theo dõi sau tiêm. |
| Đạt | `datngo1203` | Nền Windows/API/DB/CI; danh mục, kho, khám, Identity, báo cáo theo SCOPE. |

Đạt điều phối migration; chỉ một người sửa schema/migration tại một thời điểm. Contract/schema cần người khác review; Bảo/Quý review chéo client, Đạt review dữ liệu/quyền. Không tự cấp quyền GitHub hoặc gán username. GitHub Issues là nguồn việc.
G1 (phiên/quyền client) và G2 (đăng ký) có thể phát triển song song khi contract, quyền, actor và schema liên quan đã thống nhất và được review chéo. Không yêu cầu toàn bộ G1 xong mới bắt đầu G2; vẫn tách nhánh/PR và vùng sửa, không tự quyết contract/schema mâu thuẫn hoặc sửa migration đồng thời.
Mỗi lát dùng nhánh `feat/sN-ten-lat` từ `main`, PR nhỏ vào `main`, squash merge sau review. Không commit thẳng/force-push nhánh chung hoặc tự merge. CI xanh phải có bằng chứng từ lần chạy thật. Chưa xác minh branch protection; không khẳng định đã bật. Chưa có giấy phép nguồn mở; không mặc định cho phép tái phân phối.
R00 là gói thuần tài liệu trên nhánh `feat/r00-scope-alignment` từ `main` sạch; chỉ sửa AGENTS.md, SCOPE.md, README.md và backend/contracts/README.md. Không tự commit, push, merge hoặc triển khai gói tiếp theo.

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
8. Giữ nhận diện VAC, tái sử dụng style/token có sẵn; thống nhất typography, khoảng cách và icon phù hợp tác vụ. Không thêm thư viện lớn chỉ để làm đẹp.
9. Khi hoàn thành module, bắt buộc kiểm chứng UX: Web kiểm desktop/mobile, bàn phím, focus và overflow; Windows kiểm kích thước cửa sổ làm việc thực tế, cuộn, nút thao tác và keyboard navigation. Cả hai kiểm normal/empty/loading/error, phân quyền và kết quả API thật. Nếu chưa có runtime hoặc screenshot thực tế, phải báo rõ phần chưa kiểm; không đánh dấu PASS bằng build/CI thay cho tương tác chưa chạy.
10. Không áp cùng một bố cục cho mọi module. UX-01 là ví dụ về tư duy thiết kế theo tác vụ, không phải template bắt buộc.
11. Không dùng dữ liệu Sample hoặc state UI để chứng minh nghiệp vụ thật. Màn mock phải có nhãn rõ và chỉ chuyển thành màn thật trong gói nghiệp vụ tương ứng, gọi API thật theo phạm vi đã giao.
12. Áp dụng các quy tắc UX trên ngay trong quá trình phát triển từng gói G2–G8. Mỗi gói vẫn tuân thủ phạm vi, tiêu chí nghiệm thu và điểm dừng đã giao; các quy tắc này không tự cấp phép triển khai gói tiếp theo.

## Bí mật và môi trường

Không lưu secret/JWT trong Git hoặc biến VITE_*. Dùng user-secrets, biến môi trường hoặc `.env` local; không ghi CCCD/thông tin y tế vào log. Khi triển khai cần HTTPS và CORS đúng origin. Khi lộ secret, thu hồi/đổi secret trước; không tự xóa lịch sử Git hoặc dữ liệu để che sự cố.
