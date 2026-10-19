# Checklist triển khai giao diện KLCN090

## Mục tiêu và phạm vi

- Triển khai giao diện có thể thao tác cục bộ cho Website khách hàng bằng ReactJS và ứng dụng desktop nội bộ bằng C# WPF + WPF UI.
- Dùng thiết kế Stitch `KLCN090 - Windows Management UI` làm tham khảo hình ảnh và bố cục.
- Không triển khai backend, API, SQL Server, xác thực thật hoặc ghi dữ liệu thật. Nút chỉ cập nhật trạng thái cục bộ, hiển thị thông báo, lọc dữ liệu demo được ghi nhãn hoặc điều hướng trong giao diện.
- Không sửa, đổi tên, di chuyển hoặc xóa nội dung nguồn. `sources/` không có trong workspace; user đã thêm gói Stitch export `stitch_klcn090_windows_management_ui/` ở root, đây cũng là tài liệu chỉ đọc.
- Dùng dữ liệu hư cấu để giao diện có thể thao tác như sản phẩm mẫu. Theo yêu cầu mới nhất, bỏ nhãn “Dữ liệu minh họa” lặp lại trong từng vùng UI; checklist ghi rõ dữ liệu là giả lập.
- Trường chưa có căn cứ được điền ví dụ hình dung khi phù hợp; quy tắc và chuyên môn chưa chốt ghi “Cần xác nhận theo đề cương”. Bỏ các dòng nhắc “Dữ liệu minh họa” lặp lại khỏi giao diện.
- Desktop mở đầu ở màn đăng nhập mô phỏng cục bộ `admin / 123`. Đây không phải xác thực thật và không bảo vệ dữ liệu.

## Công nghệ và cấu trúc đã xác nhận

- Workspace hiện tại: `D:\Projects\Tiêm chủng`.
- Workspace không có Git repository hoặc thư mục `sources/` theo tên đó. Có `Tổng quan dự án.pdf` và bản DOCX khóa luận hiện tại `CNTT_KLCN090_Tuan08.docx` mới được thêm ở root; người dùng nói DOCX còn chưa hoàn chỉnh/chính xác nên dùng làm bản thảo tham khảo, chưa xem là đặc tả cuối. Gói Stitch export cục bộ `stitch_klcn090_windows_management_ui/` có 21 artboard HTML/PNG, DESIGN.md (2 artboard lặp là biến thể homepage và screening). `AGENTS.md` được tạo theo yêu cầu. Desktop dùng .NET 10 WPF + WPF-UI 4.3.0; Website React + Vite 8.3.4.
- Đề cương PDF xác nhận hai nền tảng Website + Windows, và các nhóm chức năng khái quát: tra cứu/giới thiệu dịch vụ, đăng ký, thanh toán, theo dõi lịch trên Website; quản lý hồ sơ, loại tiêm/vắc xin, đăng ký, thanh toán, lịch, ưu đãi và báo cáo trên Windows.
- Do không có cấu trúc mã sẵn, đã tạo cấu trúc tối thiểu: `website/` (ReactJS + Vite 8.3.4) và `desktop/` (WPF .NET 10 + WPF-UI 4.3.0). `README.md` ghi lệnh chạy/build.
- Đã dừng việc đọc liên kết Stitch theo yêu cầu. Dùng trực tiếp các `code.html`, `screen.png` và `clinical_fluent/DESIGN.md` trong folder export. Design system xác nhận Segoe UI, primary `#2563EB`, teal `#0D9488`, neutral `#64748B`, canvas sáng, card trắng, sidebar desktop khoảng 240px, spacing bội số 8px.
- Đối chiếu nguồn: export có quy trình cụ thể hơn PDF, ví dụ 5 bước tiếp nhận đăng ký, 4 bước thanh toán, nhập kho theo kết quả kiểm tra, lịch/nhắc theo lịch sử và phác đồ. Áp dụng phần được nêu rõ; giá, giờ, mẫu trường và tiêu chí y tế vẫn ghi chờ/xác nhận.

## Quyết định, giới hạn và điểm chờ

- Không suy diễn quy trình, biểu mẫu y tế, danh mục vắc xin, mức giá, khung giờ, chính sách, trạng thái nghiệp vụ, quyền người dùng hoặc dữ liệu khách hàng.
- Dùng dữ liệu hư cấu, có nhãn minh họa; không dùng thông tin cá nhân trích nguyên từ artboard Stitch.
- Có thể dùng ví dụ trường, giá, giờ, trạng thái và vai trò để người xem hình dung. Chỉ đánh dấu “Cần xác nhận theo đề cương” cho quy tắc/nghiệp vụ chưa chốt; không lặp nhãn dữ liệu minh họa trong UI.
- Tài khoản desktop: `admin / 123`, kiểm tra cục bộ trong ứng dụng, không backend, không lưu phiên hoặc thông tin thật.
- Không kết nối dịch vụ ngoài. Reload có thể đặt lại trạng thái UI.
- Điểm nguồn chưa khớp/thiếu: Stitch export có nội dung và dữ liệu vận hành ví dụ chi tiết hơn PDF; `kh_m_s_ng_l_c_v_ch_nh_1` có hồ sơ/điện thoại cụ thể hơn phiên bản `_2`, nên thông tin cá nhân được thay bằng bản ghi hư cấu. A05 có quy trình nhưng trường phiếu/tiêu chí kiểm tra vẫn đánh dấu cần bổ sung. A08/A09/A11/A12 có quy trình/nhóm chức năng nhưng giá, nhắc lịch, chỉ số báo cáo và ma trận quyền vẫn chờ. Folder `i_m_t_kh_u` có nội dung Đổi mật khẩu dù `<title>` lại ghi Đăng nhập; phân loại theo nội dung body, ghi nhận lỗi metadata export.
- Bản thảo DOCX mới làm rõ các quy trình hồ sơ, đăng ký, sàng lọc, thanh toán, thực hiện tiêm/theo dõi sau tiêm, kho và lịch nhắc. Theo UC hệ thống trong bản thảo, khách hàng cần đăng nhập để tự đăng ký; chức năng xem lịch sử yêu cầu đăng nhập và hồ sơ tiêm chủng được liên kết, có thể gồm người thân phụ thuộc. Đây là căn cứ tạm thời cho câu hỏi về tài khoản khách hàng, nhưng cần xác nhận vì người dùng nói tài liệu chưa hoàn chỉnh/chính xác.
- Chênh lệch phạm vi: bản thảo ghi nền tảng Web/Mobile và Flutter; phạm vi triển khai đã được người dùng chốt trong công việc này là Website React + Desktop WPF. Không mở thêm Mobile. Bản thảo cũng yêu cầu backend ASP.NET Core API/SQL Server, nhưng phạm vi hiện tại loại trừ backend, API và xác thực thật.
- Chênh lệch thiết kế/đặc tả tài khoản khách hàng: UC đăng ký trong DOCX yêu cầu khách hàng đăng nhập, và UC tra cứu lịch/hồ sơ yêu cầu đăng nhập để chọn hồ sơ bản thân/người thân. W03/W05 trong Stitch và Website hiện tại chưa có màn đăng nhập/đăng ký tài khoản khách hàng. Hiện chưa thêm màn đó: cần chốt với người dùng nếu họ muốn điều chỉnh thiết kế; giới hạn hiện tại vẫn không có xác thực thật.
- Người dùng nêu phương án không dùng tài khoản khách hàng: khi đăng ký thu thập định danh; nếu trẻ chưa có định danh riêng thì dùng định danh cha/mẹ/người giám hộ; thanh toán và lịch tiêm tra cứu theo hồ sơ gắn với định danh. Ghi nhận đây là hướng đang được cân nhắc, chưa thay mã Website cho tới khi được chốt.
- Nếu áp dụng hướng trên, cần tách người liên hệ/thanh toán (khách hàng hoặc người giám hộ) khỏi người được tiêm. Hồ sơ trẻ nên có mã hồ sơ nội bộ riêng và quan hệ tới người giám hộ, vì một mã định danh người lớn có thể liên kết nhiều trẻ. Giao dịch nên liên kết với mã đăng ký/hóa đơn; lịch sử/lịch tiêm liên kết với hồ sơ người được tiêm. Không coi việc biết số định danh là đủ để mở dữ liệu sức khỏe; cần cân nhắc bước xác minh qua số điện thoại đã đăng ký hoặc mã tra cứu.
- Mâu thuẫn cần chốt: bản thảo DOCX yêu cầu đăng nhập cho tự đăng ký và tra cứu lịch sử, còn phương án người dùng vừa nêu có thể bỏ tài khoản và tra cứu bằng định danh. Phạm vi hiện tại vẫn không có xác thực thật/backend; chỉ có thể mô phỏng form và tra cứu bằng dữ liệu cục bộ.
- Chất lượng bản thảo: có Track Changes đang bật (798 chèn, 42 xóa theo OOXML), không có comments; thông tin bìa/tác giả còn placeholder, danh mục hình/bảng nhắc đánh số trang, và còn số mục/figure chưa hoàn thiện như `1.4..`, `2.2..`, `Hình…`. Một số Use Case hệ thống bị trùng mã UC và cách mô tả luồng thanh toán/điều kiện trả tiền chưa thống nhất với quy trình nghiệp vụ linh hoạt theo từng dịch vụ. Không tự chọn phiên bản nghiệp vụ để biến thành quy tắc chạy thật.
- Kiểm tra DOCX: đọc nội dung đoạn và bảng có tracked insertions, xem contact sheet các hình được nhúng. Chưa render toàn trang để đánh giá dàn trang vì môi trường không có LibreOffice bundled (`soffice.exe`); file DOCX không bị sửa.
- `AGENTS.md` mới được tạo trong workspace. `sources/` không tồn tại nên hiện chưa thể đối chiếu thêm tài liệu nguồn. W06–W08 không có artboard trong gói export và không được đề cương PDF nêu; W06 hiện đã được bổ sung theo yêu cầu người dùng, W07–W08 vẫn ngoài phạm vi.

## Danh sách màn hình Website

| Mã | Màn hình | Stitch | Trạng thái | Ghi chú |
|---|---|---|---|---|
| W01 | Trang chủ / khám phá dịch vụ | Đọc được | Đã triển khai; build và kiểm tra trình duyệt | Hero, tìm kiếm, danh mục mẫu, hành trình; tên nhóm mẫu có nhãn. |
| W02 | Danh mục loại tiêm và vắc xin | Đọc được | Đã triển khai; build/lint và kiểm tra tab tra cứu bệnh–vắc xin | Bộ lọc bệnh & vắc xin nằm trong Danh mục; liên kết nguồn WHO.
| W03 | Đăng ký tiêm | Đọc được | Đã triển khai; kiểm tra chuyển bước | Form khách hàng/người tiêm riêng, chọn ngày/giờ, summary và submit cục bộ. |
| W04 | Thanh toán đăng ký | Đọc được | Đã triển khai; kiểm tra khóa mã sai/đúng và nội dung sau mở khóa | Chỉ hồ sơ đăng ký cục bộ khớp định danh người tiêm hoặc người giám hộ mới mở nội dung.
| W05 | Theo dõi lịch tiêm / lịch sử | Đọc được | Đã triển khai; kiểm tra khóa truy cập và chuyển tab sau mở khóa | Phải khớp định danh của hồ sơ người tiêm hoặc người giám hộ; tab lịch/lịch sử ẩn trước đó.
| — | Ngoài phạm vi: kết quả/chứng nhận, tin tức/cẩm nang, portal khách hàng | Không có artboard trong gói export | Không triển khai | Chỉ là gợi ý ở hội thoại trước; không có căn cứ trong PDF. |

## Danh sách màn hình Desktop

| Mã | Màn hình | Stitch | Trạng thái | Ghi chú |
|---|---|---|---|---|
| A01 | Tổng quan | HTML + PNG đọc được | Đã triển khai; build pass; chưa click WPF | KPI/bảng/trạng thái/lịch sắp đến dùng dữ liệu mẫu và nhãn minh họa. |
| A02 | Khám sàng lọc / chỉ định | Có 2 biến thể; nội dung đọc được | Đã triển khai; build pass; chưa click WPF | Hàng đợi, hồ sơ giả, ghi chú và 3 quyết định; không lưu hồ sơ y tế. |
| A03 | Khách hàng / người được tiêm | HTML + PNG đọc được | Đã triển khai; build pass; chưa click WPF | Tabs, lọc, thêm/sửa/xem cục bộ. |
| A04 | Danh mục loại tiêm / vắc xin / gói | HTML + PNG đọc được | Đã triển khai; build pass; chưa click WPF | Tabs, lọc, thêm/sửa cục bộ; mức phí cần xác nhận. |
| A05 | Nhập kho vắc xin | HTML + PNG đọc được | Đã triển khai quy trình và bảng lô mẫu; build pass; chưa click WPF | Phiếu và tiêu chí kiểm tra cần bổ sung. |
| A06 | Thực hiện tiêm chủng | HTML + PNG đọc được | Đã triển khai; build pass; chưa click WPF | Quy trình và thao tác ghi nhận chỉ ở phiên demo. |
| A07 | Quản lý đăng ký dịch vụ | HTML + PNG đọc được | Đã triển khai; build pass; chưa click WPF | Quy trình 5 bước, bảng và thêm/sửa cục bộ. |
| A08 | Thanh toán dịch vụ | HTML + PNG đọc được | Đã triển khai 4 bước và trạng thái cục bộ; build pass; chưa click WPF | Không giao dịch; giá/phương thức là mẫu. |
| A09 | Lịch tiêm / nhắc lịch | HTML + PNG đọc được | Đã triển khai 4 bước, lịch và nhắc cục bộ; build pass; chưa click WPF | Không gửi tin nhắn; chu kỳ/kênh/thời điểm cần xác nhận. |
| A10 | Khuyến mãi / ưu đãi | HTML + PNG đọc được | Đã triển khai; build pass; chưa click WPF | Bản ghi/giá trị giảm giả; thêm/sửa và lọc cục bộ. |
| A11 | Thống kê / báo cáo | HTML + PNG đọc được | Đã triển khai chỉ số/khoảng thời gian mẫu; build pass; chưa click WPF | Công thức/chỉ số/định dạng xuất cần xác nhận. |
| A12 | Tài khoản / phân quyền | HTML + PNG đọc được | Đã triển khai tab tài khoản/quyền, tìm kiếm, thêm/sửa và trạng thái demo; build pass; chưa click WPF | Vai trò/quyền chỉ là ví dụ. |
| A13 | Đăng nhập nội bộ | HTML + PNG đọc được | Là màn đầu; demo `admin / 123`; build pass; chưa click WPF | Mô phỏng cục bộ, không phải xác thực thật. |
| A14 | Đổi mật khẩu | HTML body là Đổi mật khẩu, title metadata sai | Đã triển khai form và kiểm tra trường cục bộ; build pass; chưa click WPF | Không thay đổi mật khẩu thật. |

## Thứ tự triển khai

1. Hoàn thiện Website W01–W05 theo luồng khách hàng, với dữ liệu demo gắn nhãn.
2. Hoàn thiện Desktop A01–A14 theo nghiệp vụ có trong gói export; giữ điểm thiếu chuyên môn/đặc tả ở trạng thái chờ.
3. Chạy build và kiểm tra trực quan/tương tác hai nền tảng.

## Tiến độ, trở ngại và bước tiếp theo

- Hoàn tất: kiểm kê workspace; đọc PDF; đọc trực tiếp HTML/DESIGN.md và danh sách PNG của gói Stitch export. Gói có 21 artboard HTML: 19 màn logic (5 Website, 14 desktop) và 2 biến thể lặp cho homepage/screening.
- Hoàn tất mốc Website theo export: `npm run build` pass; đã kiểm tra trình duyệt các màn W01–W05; tìm kiếm/lọc catalog, submit form demo → thanh toán, xác nhận trạng thái demo, lịch hẹn cập nhật và tab lịch sử hoạt động. Không gửi/lưu dữ liệu ra ngoài.
- Mốc Desktop theo export: màn đầu đăng nhập demo, đăng xuất, dữ liệu giả, modal thêm/sửa, tìm kiếm và các trạng thái trong phiên; dựng A01–A14, theme nền/sidebar Clinical Fluent theo DESIGN.md. Build cuối chạy sau đợt cập nhật cuối. WPF chưa được bấm thử trực quan bằng CUA: môi trường không cung cấp danh sách/cửa sổ app desktop.
- Hoàn tất tài liệu chạy: tạo `README.md`, `.gitignore` và `AGENTS.md`.
- Đã đọc bản DOCX `CNTT_KLCN090_Tuan08.docx` ở root theo yêu cầu; không sửa file nguồn. Ghi nhận nội dung quy trình chi tiết, yêu cầu đăng nhập khách hàng ở UC hệ thống, phạm vi Web/Mobile/Flutter khác với phạm vi UI được giao, các mã UC trùng và dấu hiệu bản thảo còn sửa dở. Việc đọc nội dung đã bao gồm tracked insertion; chưa kiểm tra layout toàn trang do thiếu LibreOffice bundled.
- Yêu cầu mới nhất về trình bày: người dùng yêu cầu bỏ các nhãn/lời nhắc lặp như “Dữ liệu minh họa” khỏi giao diện; họ biết đây là bản demo. Giữ dữ liệu giả đã điền để dễ hình dung; checklist là nơi ghi nhận tính chất giả lập. Quy tắc/nghiệp vụ chưa chốt vẫn ghi “Cần xác nhận theo đề cương”.
- Người dùng bổ sung ảnh banner/ghi chú nêu bản ghi giả, thiếu thông tin y tế thật, hoặc nhắc xác nhận và yêu cầu bỏ toàn bộ thông tin kiểu đó khỏi UI. Website đã ẩn `DemoNotice`, footnote, chú thích form/thanh toán; Desktop bỏ banner chung và ẩn các dòng/hộp chú thích chứa lời giải thích hoặc cảnh báo. Các điểm thiếu căn cứ vẫn được lưu ở checklist.
- Đã thêm mô tả dịch vụ và thay các chỗ chờ dữ liệu bằng ví dụ lịch sử, phiếu nhập, lượt tiêm, lịch nhắc và chỉ số báo cáo. Đã ẩn các banner/ghi chú giải thích lặp theo ảnh người dùng cung cấp; Desktop bỏ banner tổng quát và các hộp chú thích lặp, giữ lại ví dụ dữ liệu và bố cục.
- Kiểm tra sau cập nhật cuối: `website`: `npm run build` pass, `npm run lint` pass; `desktop`: `dotnet build` pass, 0 warning, 0 error. Đây là kiểm tra build sau thay đổi copy/hiển thị; luồng Website đã được click test trước đó và không đổi xử lý tương tác.
- Đang chờ kiểm tra: CUA hiện chỉ cung cấp API trình duyệt; `listWindows` không có trong runtime nên không thể mở và click thử WPF trong phiên này. Build pass chỉ xác nhận biên dịch, chưa xác nhận visual/runtime interactions.
- Trở ngại: không có thư mục `sources/`; DOCX mới là bản thảo và có nhiều Track Changes chưa chấp thuận, mã Use Case trùng; renderer không tìm thấy LibreOffice bundled nên chưa kiểm tra layout trang. Stitch export đôi chỗ lặp và một HTML title sai so với body.
- Bước tiếp theo: chạy ứng dụng WPF trên máy Windows có thể hiển thị cửa sổ để kiểm tra đăng nhập `admin / 123`, điều hướng, modal và các thao tác ở A01–A14. Trước khi thêm Website login cho khách hàng, xác nhận có áp dụng UC đăng nhập trong bản thảo vào giao diện đợt này không; giữ xác thực thật ngoài phạm vi.

## Rà soát giao diện bổ sung — 2026-10-15

- Đã chọn font hỗ trợ tiếng Việt: Website dùng `system-ui, Segoe UI, Arial, sans-serif`; các tiêu đề cũng có fallback hệ thống, không tải font từ mạng. WPF đặt `Segoe UI` ở cửa sổ chính và các `TextBlock` tạo động.
- Đã tái hiện lỗi ở ảnh người dùng trong trình duyệt đang mở: tại vùng tablet, thanh điều hướng ngang bị cắt mục cuối/nút tài khoản. Chuyển Website sang thanh điều hướng dưới cố định ở breakpoint 900px; các mục đều hiển thị, tài khoản và nút menu nằm ở header. Đã chụp kiểm tra thực tế trang chủ và trang đăng ký tại kích thước đang mở (805px chiều rộng ảnh hiển thị).
- Đã bỏ các câu giải thích lặp như “mẫu để minh họa”, “cần xác nhận theo đề cương”, thông báo giả lập và nhãn hỗ trợ chờ khỏi các vùng Website đã rà. Giữ form, giá trị ví dụ và cấu trúc trường; không bỏ nhóm trường.
- Đã bỏ lời nhắc trình diễn/không lưu dữ liệu khỏi header và màn đăng nhập Desktop; giữ sẵn tài khoản `admin` và mật khẩu `123`. Các trạng thái được thông báo ngắn gọn.
- Kiểm tra sau cập nhật: Website `npm run build` pass; `npm run lint` pass; Desktop `dotnet build --no-restore` pass, 0 warning, 0 error. Trình duyệt thực tế xác nhận dấu tiếng Việt, thanh điều hướng tablet, form đăng ký; WPF chỉ xác nhận qua build vì môi trường không có cửa sổ desktop để chụp và bấm thử.
- Phạm vi rà trực quan hiện tại: Website W01 trang chủ và W03 đăng ký tại kích thước trình duyệt đang mở; các luồng W01–W05 đã có kiểm tra thao tác trước đó. Chưa có xác nhận bằng ảnh ở kích thước mobile CSS độc lập hoặc các màn WPF khi chạy.
- Bước tiếp theo để kết thúc rà soát trực quan toàn nền tảng: mở ứng dụng WPF trên máy có thể hiển thị cửa sổ và kiểm tra A01–A14; kiểm tra Website thêm ở viewport 360px/390px và desktop rộng. Không đánh dấu những lần kiểm tra này là hoàn tất cho tới khi chạy thật.
- Sau khi thêm breakpoint tablet, đã rà cây trợ năng của W02 (Danh mục), W04 (Thanh toán), W05 (Lịch tiêm/Lịch sử) và khôi phục W01 làm trang cuối trong tab Website. Đã bỏ nhãn ví dụ lặp khỏi tên dịch vụ, phương thức thanh toán và bản ghi lịch; vẫn giữ dữ liệu và trạng thái để người dùng thao tác.
- Thử khởi chạy `TiemChung.Desktop.exe`: tiến trình mở cửa sổ “Đăng nhập | Tiêm chủng”, nhưng runtime CUA của phiên này không có `listWindows`/API desktop nên không thể chụp hoặc click WPF. Đã đóng tiến trình do nó khóa apphost; build sạch lại sau đó pass, 0 warning/0 error.

## Bổ sung tra cứu bệnh và bước xác nhận định danh — 2026-10-15

- Người dùng chốt luồng Website không cần tài khoản: tại Thanh toán và Lịch tiêm/Lịch sử, nhập CCCD/mã định danh của người được tiêm hoặc người giám hộ trước khi mở nội dung. Đây là kiểm tra giao diện cục bộ, không phải xác thực hay bảo vệ dữ liệu ở backend; không gửi/lưu mã định danh ra ngoài.
- W04 và W05 đã thêm màn xác nhận riêng. Trường nhập không điền sẵn; chỉ chấp nhận mã trùng một trong hai định danh đã ghi trong hồ sơ đăng ký hiện tại (người đăng ký/người giám hộ hoặc người được tiêm). Sai mã hiện lỗi và không hiện thông tin. Kiểm thử trình duyệt: mã sai bị chặn; mã hồ sơ cục bộ `ID-1001` mở thanh toán và lịch; tab Lịch sử chuyển được.
- W02 có thêm tab con “Bệnh & vắc xin” để tra cứu mà không thêm mục điều hướng cấp cao trùng với Danh mục. Các quan hệ hiển thị ở mức tổng quát và kèm nguồn WHO; không đưa lịch tuổi, liều hoặc chỉ định riêng. Nguồn: WHO về [sởi](https://www.who.int/teams/immunization-vaccines-and-biologicals/diseases/measles), [cúm mùa](https://www.who.int/news-room/fact-sheets/detail/influenza-(seasonal)), [HPV](https://www.who.int/teams/immunization-vaccines-and-biologicals/diseases/human-papillomavirus-vaccines-%28HPV%29/hpv-clearing-house/vaccines-safety) và [chương trình tiêm chủng thiết yếu](https://www.who.int/teams/immunization-vaccines-and-biologicals/essential-programme-on-immunization).
- Đối chiếu nghiệp vụ: cách tra cứu bằng định danh theo yêu cầu người dùng hiện khác UC trong bản DOCX (yêu cầu tài khoản khi đăng ký/xem lịch sử). Áp dụng yêu cầu mới nhất ở mô hình UI; thay đổi backend, xác minh danh tính/ủy quyền, liên kết nhiều trẻ và bảo vệ dữ liệu còn ngoài phạm vi đợt này.
- Website `npm run build` và `npm run lint` pass sau thay đổi. Trình duyệt đã hiển thị nội dung tra cứu, chặn mã sai, mở nội dung bằng mã khớp, và chuyển được tab lịch sử. Desktop không đổi trong mốc này.
- Bước tiếp theo: rà kích thước 360/390px và desktop rộng cho các màn mới; chức năng hiện có mới kiểm tra ở trình duyệt desktop đang mở. Tiếp tục kiểm tra WPF trực quan trên máy có API cửa sổ desktop.- Rà thêm nội dung Website theo yêu cầu bỏ lời nhắc demo: đã gỡ nhãn `DEMO-001` khỏi phiếu thanh toán; các nhãn thành phần CSS như `demo-field` chỉ là tên kỹ thuật, không hiển thị trong giao diện.

## Điều chỉnh luồng Thanh toán từ Đăng ký — 2026-10-15

- Theo yêu cầu mới nhất, khi bấm “Tiếp tục đăng ký” sau khi hoàn tất form, Website chuyển thẳng sang nội dung Thanh toán mà không hỏi CCCD/mã định danh lần nữa.
- Mở mục Thanh toán trực tiếp từ thanh điều hướng vẫn hiển thị bước nhập mã; mở lại Thanh toán từ thanh điều hướng sau luồng đăng ký cũng cần mã. Lịch tiêm/Lịch sử tiếp tục yêu cầu mã trước khi xem.
- Kiểm tra trình duyệt: truy cập trực tiếp Thanh toán vẫn bị khóa; submit đăng ký mở nội dung thanh toán ngay; chọn lại Thanh toán từ điều hướng quay về bước xác nhận mã.
- Website `npm run build` và `npm run lint` pass sau điều chỉnh.

## Đăng ký cho nhiều người được tiêm — 2026-10-15

- W03 mục “Thông tin Người được tiêm” nay chuyển sang danh sách khi chọn đăng ký cho người thân. Nút “＋ Thêm người được tiêm” thêm form riêng cho từng người; cho phép xóa người khi có từ hai hồ sơ trở lên. Các trường hiện có được giữ cho từng hồ sơ.
- Mỗi hồ sơ người thân có lựa chọn vắc xin/dịch vụ riêng từ danh mục hiện có. Tóm tắt và lịch ghi từng tên kèm lựa chọn; Thanh toán liệt kê từng dòng phí theo giá đã hiển thị trong danh mục và tính tổng các lựa chọn cho nhóm.
- Tra cứu lịch chấp nhận mã của người đăng ký/người giám hộ hoặc bất kỳ người được tiêm nào trong danh sách. Khi chọn người đăng ký là người được tiêm, không dùng định danh còn lưu từ các hồ sơ người thân đã ẩn.
- Kiểm tra trình duyệt: chọn “Đăng ký cho người thân”, thêm hồ sơ thứ hai, nhập hai tên, thấy cả hai trong tóm tắt; tiếp tục sang Thanh toán và thấy đủ tên; mã giám hộ mở Lịch và lịch hiển thị cả hai tên.
- Chưa xác định trong nguồn: nghiệp vụ thanh toán nhiều người, lịch hẹn riêng từng người và quy tắc một hay nhiều giao dịch. Tổng nhóm hiện là phép cộng giá của từng lựa chọn danh mục trong giao diện; cần xác nhận quy tắc thật khi triển khai backend. Cả nhóm hiện dùng chung ngày và khung giờ hẹn.

## Chọn vắc xin riêng cho từng người — 2026-10-15

- Theo trao đổi mới nhất, trong chế độ đăng ký cho người thân, mỗi hồ sơ có thêm trường chọn vắc xin/dịch vụ riêng. Giá trị mặc định lấy từ dịch vụ đã chọn trước đó; người dùng có thể thay đổi độc lập cho từng hồ sơ.
- Thanh toán hiện liệt kê từng người, lựa chọn và giá danh mục; tổng nhóm cộng giá các lựa chọn. Lịch hiển thị cùng ngày/giờ nhóm và ghi từng cặp người–vắc xin. Phép tính chỉ là hành vi UI dựa trên các mức giá đang có trong danh mục; quy tắc nghiệp vụ thanh toán cho nhiều người cần xác nhận trước khi gắn backend.
- Kiểm tra trình duyệt: chọn Vắc xin B cho người thứ nhất, Vắc xin C cho người thứ hai; form giữ hai lựa chọn, Thanh toán hiển thị hai dòng và tổng 1.130.000 đ, Lịch hiển thị đúng tên và vắc xin tương ứng.
- Website `npm run build` và `npm run lint` pass.
## Điều hướng responsive bằng menu ba gạch — 2026-10-15

- Theo phản hồi từ ảnh giao diện, đã bỏ thanh điều hướng Website cố định ở đáy tại breakpoint 900px. Nút ba gạch mở menu dọc ngay dưới header; nhấn lần nữa sẽ đóng, chọn trang sẽ tự đóng menu.
- Nút có trạng thái truy cập `aria-expanded`, tên thay đổi giữa mở/đóng và liên kết với menu. Footer không còn chừa khoảng trống giả cho thanh điều hướng cố định.
- Kiểm tra trình duyệt ở viewport thu gọn: menu dọc mở được, hiển thị đủ các mục điều hướng, chọn Danh mục điều hướng đúng và tự đóng menu. WPF không đổi.
- Website `npm run build` và `npm run lint` pass.

## Mục Ưu đãi cho khách hàng — 2026-10-15

- Theo yêu cầu người dùng, thêm W06 “Ưu đãi” vào điều hướng Website và footer. Đây là màn mới ngoài các artboard có trong gói Stitch.
- Màn có tìm kiếm theo tên/mã, lọc “Tất cả”/“Đang áp dụng”, thẻ ưu đãi, hộp thoại chi tiết và nút đi tới Danh mục; mọi thao tác chỉ đổi trạng thái cục bộ.
- Nội dung hiện dùng thông tin ưu đãi đang áp dụng `THANG10`, giảm 10%, thời gian 01/10/2026–31/10/2026 từ màn quản lý desktop A10 trong gói Stitch. Không hiển thị `CHAOHE` vì mẫu đó ở trạng thái nháp. Điều kiện áp dụng và chính sách ưu đãi chưa có căn cứ nên chưa tự thêm.
- Kiểm tra trình duyệt: Ưu đãi xuất hiện ở điều hướng; màn hiển thị tên, mã, mức giảm và thời gian; “Chi tiết” mở hộp thoại; CTA trong hộp thoại chuyển sang Danh mục và hiện thông báo.
- Website `npm run build` và `npm run lint` pass. Desktop không đổi.
- Cần thay nội dung mẫu bằng chương trình và điều kiện ưu đãi chính thức khi có dữ liệu được duyệt.

## Sửa cửa sổ và khả năng co giãn Desktop — 2026-10-15

- Ảnh người dùng cho thấy cửa sổ chính không có cụm nút hệ thống và nội dung chưa co theo khung. Nguyên nhân trong XAML: `FluentWindow` chưa có `ui:TitleBar` dù nội dung dùng vùng tiêu đề mở rộng.
- Đã thêm `ui:TitleBar` của WPF UI với nút thu nhỏ, phóng to/khôi phục và đóng; giữ khả năng resize. Kích thước mặc định đổi thành 1360×820, tối thiểu 980×620; sidebar giảm còn 232, ô tìm kiếm header còn 200 và phần nội dung giảm padding. Nhãn menu dài tự xuống dòng.
- Kiểm tra thật: `dotnet build --no-restore` thành công, 0 warning/0 error. Mở cửa sổ đăng nhập `admin / 123`, vào trang A01, chụp kiểm tra cửa sổ và bố cục. Dùng UI Automation bấm trực tiếp các nút thu nhỏ, phóng to/khôi phục và đóng; cả ba phản hồi đúng, sau đó chạy lại app. Thu nhỏ cửa sổ còn khoảng 1250×780 pixel cho thấy nội dung không tràn khung và menu dài xuống dòng.
- WPF UI xác nhận cách dùng `FluentWindow.ExtendsContentIntoTitleBar` cùng `TitleBar` trong ví dụ chính thức: [WPF UI README](https://github.com/lepoco/wpfui).
- Ứng dụng desktop hiện đang mở để người dùng kiểm tra. Các màn A02–A14 chưa được rà trực quan ở kích thước này; bước tiếp theo là tiếp tục rà các màn desktop và thử kích thước màn hình thực tế của máy người dùng.

## Sửa menu và bổ sung bảng màu Website/Desktop — 2026-10-15

- Ảnh phản hồi cho thấy các nút menu Desktop có chiều rộng theo nội dung nên bị lệch, còn các thẻ trạng thái và lịch đều cùng một màu xanh nhạt. Đã ràng buộc chiều rộng danh sách điều hướng theo vùng nhìn của ScrollViewer; menu dùng nút toàn chiều ngang, bỏ viền mặc định, giữ trạng thái chọn màu tím nhạt và cho nhãn dài tự xuống dòng.
- Desktop dùng màu theo nhóm trạng thái: khám tím, thanh toán vàng cam, tiêm xanh dương, theo dõi xanh ngọc; KPI tổng quan cũng tách màu theo loại chỉ số. Thay đổi ở `desktop/MainWindow.xaml` và `desktop/MainWindow.xaml.cs`.
- Website thêm bảng màu đồng bộ gồm chàm/tím, xanh ngọc, vàng cam và san hô; áp dụng vào nút chính, thẻ dịch vụ, minh họa danh mục, quy trình, trạng thái, lịch, thanh tiến trình và các điểm nhấn. Không thay đổi nội dung nghiệp vụ. Thay đổi ở `website/src/theme.css`, được import từ `website/src/App.jsx`.
- Kiểm tra Desktop: `dotnet build --no-restore` pass, 0 warning/0 error; chạy cửa sổ thật ở kích thước mặc định và khoảng 1250×780 pixel, kiểm tra trang A01 và A10, xác nhận nút menu cùng hàng thẻ KPI/hoạt động lên màu và không tràn ngang; menu dài xuống dòng. Tìm “Khách hàng” vẫn lọc ra mục A03 và bấm mở được màn hồ sơ. Ở cửa sổ thấp, danh sách menu vẫn cuộn dọc.
- Kiểm tra Website: `npm run build` và `npm run lint` pass; chụp kiểm tra W01 và W02 tại trình duyệt hiện tại, màu thẻ dịch vụ và tab điều hướng phân biệt rõ.
- Các màn desktop còn lại chưa được chụp rà riêng cho màu mới; tiếp tục kiểm tra ở các màn A02–A14 khi rà trực quan toàn ứng dụng.
