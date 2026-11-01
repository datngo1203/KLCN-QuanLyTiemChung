# SCOPE: KLCN090 – Hệ thống quản lý tiêm chủng

Nguồn sự thật duy nhất về phạm vi. Cập nhật: 27/10/2026 theo R00. Phạm vi nền tảng: Website React và Windows WPF, dùng chung ASP.NET Core API và SQL Server. Hạn nộp: 08/11/2026 theo yêu cầu R01.

## Nguyên tắc

- Chỉ làm các lát bên dưới. Muốn thêm gì phải được nhóm đồng ý.
- Mỗi lát nghiệp vụ đi trọn đường DB → API → client phù hợp vai trò: Web cho khách hàng, Windows cho nhân viên. Hai client dùng chung dữ liệu SQL; không dùng Sample cho chức năng mới. Gói thuần tài liệu như R00 chỉ kiểm tài liệu, không triển khai nghiệp vụ.
- Giữ luồng ở mức tối thiểu theo đề cương: không thêm trạng thái, bước hay ngoại lệ ngoài phạm vi này.
- Phân quyền: dùng mô hình phân quyền có sẵn ở backend, không làm thêm UI quản trị quyền. Kiểm tra tại API, không chỉ ẩn nút trên UI.

## Các lát (triển khai theo phụ thuộc)

| # | Lát | Phụ trách | Xong khi | Trạng thái |
|---|-----|-----------|----------|------------|
| S0 | Nền: SQL Server thật, áp dụng migration, CI xanh | Đạt | API đọc/ghi được DB thật; migration chạy sạch | ☑ 25/10: Compose SQL/API thật, 2 migrations, SQL/HTTP 42/42; CI xanh |
| S1 | Đăng nhập, đăng xuất, đổi mật khẩu, phân quyền (F01) | Đạt | Mật khẩu có hash; đăng nhập/đăng xuất/đổi mật khẩu thật trên Web và Windows; dùng mô hình phân quyền có sẵn ở backend, không làm thêm UI quản trị quyền; test 401/403 | ◐ 27/10 G1 + UX: Web đổi mật khẩu qua form PASS theo người dùng; UX mới mở/Hủy/hiện-ẩn, xóa bản nháp, bàn phím và desktop/mobile đã kiểm browser. WPF Profiles: sửa PropertyChanged/reset, chỉ xem và Lưu và tạo tiếp; cửa sổ test dùng control thật đã kiểm read-only/reset, tạo liên tiếp kiểm qua SQL test riêng. Backend 64/64, Web 11/11 + lint/build, Windows build đạt. Chưa gửi form Web bản UX mới, chưa lưu qua cửa sổ WPF/full shell; capabilities chờ review chéo |
| S2 | Danh mục: loại tiêm chủng, loại vắc xin, vắc xin, gói (D01) | Đạt | CRUD qua API; Windows quản lý; Web giới thiệu + tìm kiếm/lọc | ☑ 25/10: CRUD SQL/API, quyền/validation/chặn xóa tham chiếu; seed [DEMO] lặp lại; Windows command HTTP/SQL 4 danh mục; Web browser tìm/lọc/lỗi/thử lại; backend 53/53, Web 4/4, build/lint đạt. Chưa kiểm tương tác cửa sổ WPF; contract chờ review chéo |
| S3 | Hồ sơ khách hàng và người được tiêm/bé (B01) | Bảo | CRUD qua API; Web xem/đăng ký hồ sơ; Windows quản lý thông tin bé | ☑ 25/10: tái dùng Profiles M1, CRUD SQL/API + liên kết chủ hồ sơ, CCCD nullable/nhiều bé; Web browser tạo/sửa/tải lại/lỗi mạng/thử lại thật; Windows command HTTP/SQL CRUD; backend 55/55, Web 5/5, build/lint đạt. Chưa kiểm tương tác cửa sổ WPF; contract/schema chờ review chéo |
| S4 | Đăng ký dịch vụ (lẻ + gói) và thanh toán một lần (B02, B03) | Bảo | Tạo đăng ký, chọn vắc xin/gói, chốt giá lúc đăng ký, ghi thanh toán (mô phỏng); Web + Windows | ◐ 30/10: G2B–G2E đã nghiệm thu theo xác nhận người dùng; G3A đã duyệt, G3B/G3C đã merge; G3D đang bàn giao Website chỉ đọc Payment API/SQL thật, Web 33/33 + lint/build, Edge desktop/mobile và đối chiếu SQL test riêng đạt. Chưa làm G3E; S4 chưa hoàn tất |
| S5 | Lịch tiêm, ghi nhận mũi đã tiêm, theo dõi sau tiêm, nhắc lịch (Q01–Q03) | Quý; Đạt phụ trách kết luận/chỉ định, lô/tồn tối thiểu | Tạo lịch theo dịch vụ đã chốt và thời gian nhân viên nhập, không tự suy phác đồ; ghi tiêm theo điều kiện an toàn tối thiểu bên dưới; theo dõi gắn mũi thật; nhắc qua Email hoặc log mô phỏng; Web xem lịch/lịch sử | ◐ 01/11: S5.1–S5.3 đã merge. S5.4 seed lô/tồn SQL synthetic riêng Development, 3 lô hợp lệ/hết hạn/hết tồn, mã [DEMO] cố định; giữ schema, không tự seed catalog. Transaction + khóa SQL chống seed đồng thời; chỉ thêm lô thiếu, giữ nguyên tồn/ngày/trạng thái/số lượng ban đầu và RowVersion cũ, conflict rollback. SQL/CLI 11/11 đạt, gồm bảo toàn lịch/phiếu khám/chỉ định/thanh toán đã tồn tại qua API thật; backend regression 168 passed/0 failed/5 opt-in skipped, Release build đạt. Bàn giao Draft PR để review; chưa làm S5.5 ghi tiêm/trừ tồn/theo dõi/nhắc lịch; S5 chưa hoàn tất |
| S6 | Khuyến mãi, ưu đãi (B04) | Bảo | Giảm tiền cố định VND không âm + thời gian hiệu lực; một ưu đãi mỗi đăng ký, không cộng dồn; chốt khoản giảm lúc đăng ký, không làm tổng phải thu âm | ☐ |
| S7 | Thống kê, báo cáo (D04) | Đạt | 2–3 báo cáo: doanh thu theo tháng, số mũi theo vắc xin, đăng ký theo trạng thái | ☐ |
| S8 | Tích hợp và kiểm thử (T01) | Cả nhóm | Chạy trọn luồng hồ sơ → đăng ký → ưu đãi → thanh toán → lịch → chỉ định → ghi tiêm → theo dõi → báo cáo trên máy sạch; kiểm quyền, lưu/đọc lại SQL, rollback và cạnh tranh tiền/tồn; test case/kết quả trong test source và PR, không tạo tài liệu mới | ☐ |

## An toàn tối thiểu của S5 / G5 — chưa triển khai

- Bác sĩ ghi kết luận/chỉ định chuyên môn cho đúng buổi tiêm; điều dưỡng ghi tiêm/theo dõi theo quyền. API không tự quyết định y khoa hoặc tin điều kiện đủ tiêm do client gửi.
- Nguồn lô/tồn là một lệnh seed riêng chỉ cho Development/Test, sẽ xây trong G5, ghi dữ liệu synthetic `[DEMO]` vào SQL qua backend. Lô gắn vắc xin tồn tại, có số lô, ngày sản xuất, hạn dùng, số lượng ban đầu/tồn và trạng thái rõ ràng.
- Seed chạy lại không nhân bản, không bù tồn đã tiêu hao, không đổi hạn dùng hoặc ghi đè dữ liệu hiện hữu. DB test có fixture riêng cho lô hợp lệ, hết hạn và hết tồn. Không làm nhập hàng/nhà cung cấp hoặc UI chỉnh tồn thủ công.
- Chỉ ghi tiêm thành công khi API kiểm chứng đúng lịch/người/vắc xin, kết luận/chỉ định hợp lệ cho buổi tiêm, nhân viên Active đúng quyền/chức danh, lô phù hợp được phép sử dụng/còn hạn/tồn dương và điều kiện thanh toán đã chốt.
- Ghi mũi, trừ tồn, biến động và audit phải cùng transaction, chống ghi trùng và tranh liều cuối. Thiếu bằng chứng thì từ chối; không fallback Sample hoặc tự tạo chỉ định/lô để vượt kiểm tra.
- R00 chỉ đồng bộ tài liệu; không tạo lệnh seed, dữ liệu, API hoặc migration. S5 chỉ hoàn tất sau khi các điều kiện trên được triển khai và kiểm thử thật.

## Không làm (hoặc chỉ nằm trong sơ đồ/báo cáo)

- Không xây lại 6 chức danh/12 mã quyền đã có ở backend; hoãn UI quản trị quyền và ma trận 78 ô.
- Module nhập kho đầy đủ, nhà cung cấp và UI chỉnh tồn; chỉ làm lô/tồn/biến động tối thiểu phục vụ ghi tiêm theo S5.
- Phiếu khám sàng lọc chi tiết, sinh tồn và thuật toán quyết định y khoa; vẫn bắt buộc ghi nhận kết luận/chỉ định chuyên môn tối thiểu theo S5.
- Đặt cọc, trả góp và cơ chế idempotency tổng quát; vẫn bắt buộc transaction và chống ghi trùng tại nghiệp vụ tiền/tồn kho.
- SMS, tích hợp ngân hàng, xuất file báo cáo.
- Thiết kế lại UI ngoài yêu cầu trực tiếp 26/10/2026: chỉnh Web theo ảnh mẫu, responsive trên điện thoại, dùng ảnh thật và đồng bộ logo VAC Web/Windows; bổ sung nội dung trang chủ, danh mục đọc API, liên hệ do người dùng cung cấp và màu Web theo theme Windows. Không thêm tài liệu giai đoạn/ảnh review mới.

## Quy ước làm việc

- `backend/contracts/README.md` là chuẩn API; sửa contract phải nêu trong PR và có review chéo vì hai client Web/Windows phụ thuộc vào đó. Không tự gửi tin sang chat khác.
- Mỗi gói phải được giao riêng và chốt phạm vi tệp, nghiệm thu, kiểm thử. Mỗi lát một nhánh (`feat/sN-ten-lat`) từ `main` sạch, PR nhỏ vào `main`, squash merge sau review; không tự merge. R00 dùng `feat/r00-scope-alignment`, chỉ sửa bốn tài liệu đã duyệt.
- G1 (phiên/quyền client) và G2 (đăng ký) được phát triển song song khi contract, quyền, actor và schema liên quan đã thống nhất và được review chéo; không cần đợi toàn bộ G1. Tách vùng sửa, không tự quyết contract/schema mâu thuẫn.
- Đạt điều phối schema/migration; chỉ một người sửa tại một thời điểm. Không viết lại migration đã áp dụng hoặc reset DB.
- Ưu tiên nghiệp vụ trước 08/11/2026; không refactor diện rộng hoặc thêm tầng kiến trúc không cần thiết. Xong gói thì dừng bàn giao, không tự động làm gói tiếp theo trong R01/G1–G8. R00 không tự commit, push hoặc merge.
- Trạng thái ghi một dòng vào bảng trên; không tạo file status mới.
