# Mobile review kế thừa PR #21 — 23/10/2026

- PR Draft [#31](https://github.com/datngo1203/KLCN-QuanLyTiemChung/pull/31), nhánh `codex/mobile-review-ready`, kế thừa `3741e7a`, merge develop `f188084` bằng `70e4af8`. #21 giữ nguyên để truy vết; không sửa nhánh chung dev, không merge.
- Không đưa `38c4f84`/`fa027ee` hoặc các PR Windows #27/#29/#30 vào gói mobile. Đã kiểm ancestry hai commit bảo toàn: không thuộc HEAD. Diff vào develop chỉ mobile, tài liệu mobile/status và dòng ignore .agents kế thừa #21.
- Quý (`ngocquydev`) giữ owner mobile. Người dùng cho phép thực hiện gói sửa; cần Bảo review luồng khách hàng và Đạt review dữ liệu y tế/quyền trước merge. Chưa có review chéo được duyệt.
- Bỏ suy diễn bệnh/phác đồ/đối tượng/tồn kho và các khẳng định GSP/chính sách giá. API demo/database/nguồn chưa rõ được gắn nhãn kể cả empty. Null giá khác giá 0; malformed là lỗi, empty là thành công rỗng, lỗi mạng không thay bằng sample.
- Lịch/hồ sơ có banner mẫu hư cấu; hành động mô phỏng không báo gửi thật; bỏ hướng dẫn y tế/hotline không có nguồn. Backend/contract/schema không sửa.

## Kiểm chứng code `9c7d06f4a3dfa9534464c388c3da1bbc82bd9e8d`

- Flutter 3.47.7 / Dart 3.13.5: `flutter analyze --no-pub` PASS, không issue; `flutter test --no-pub` PASS **18 tests**. Bao gồm parsing decimal, thiếu optional, chống suy diễn/trường ngoài contract, malformed envelope/item/kiểu/giá âm, HTTP 503, timeout, source demo/database/unknown/empty, navigation và nội dung chi tiết.
- [CI run 39839857607](https://github.com/datngo1203/KLCN-QuanLyTiemChung/actions/runs/39839857607): cả **mobile/backend/web/windows PASS**. [Mobile job](https://github.com/datngo1203/KLCN-QuanLyTiemChung/actions/runs/39839857607/job/120037688833) đã qua analyze, test và **flutter build apk --debug**. Workflow không upload APK artifact; không tuyên bố đã cài APK.
- `flutter build apk --debug` local dừng vì không có Android SDK. `flutter doctor -v` chỉ thấy Windows/Edge, không Android/emulator. Không thay cấu hình repo để vượt giới hạn này.
- Runtime **Flutter web debug trên Edge, viewport 390×844** qua `flutter run -d web-server --web-port 8231 --dart-define=API_BASE_URL=http://localhost:5080 --no-pub`. Ban đầu API tắt: lỗi kết nối và retry đúng; bật backend Development với CORS chỉ qua biến môi trường local: retry tải 2 mục demo. Mở chi tiết không có hướng dẫn y tế/tồn kho giả; bấm đăng ký báo chưa tạo lịch; tìm chuỗi không khớp trả empty vẫn có nhãn demo; xóa tìm kiếm phục hồi danh sách và textfield; lịch/hồ sơ có banner mẫu. Đây là demo HTTP thật, không phải API nghiệp vụ/SQL.
- Ảnh đã xem: [danh mục](ui-review/mobile-review-runtime/mobile-catalog.png), [chi tiết](ui-review/mobile-review-runtime/mobile-detail.png), [empty](ui-review/mobile-review-runtime/mobile-empty.png), [lịch mẫu](ui-review/mobile-review-runtime/mobile-schedule.png), [hồ sơ mẫu](ui-review/mobile-review-runtime/mobile-profile.png). Không phát hiện overflow trong các màn đã chụp; không xem là kiểm thử mọi màn/DPI.
- Console web có một `ERR_CONNECTION_REFUSED` lúc cố ý thử API tắt; sau bật API/retry không phát sinh lỗi Flutter mới trong luồng đã kiểm.
- `git diff --check` PASS. Commit bàn giao tiếp theo chỉ cập nhật tài liệu/bằng chứng, không đổi code đã kiểm tại SHA trên.

## Chưa kiểm chứng

Android trên thiết bị/emulator, API dùng database, SQL/migration/transaction, auth/quyền thật, lưu hồ sơ/lịch/nhắc/báo cáo thật, mọi tab/thao tác sample, toàn bộ kích thước màn hình/DPI, iOS và ký phát hành. HTTP unit tests dùng adapter fixture; runtime catalog dùng Development demo. Không xem đây là nghiệp vụ hoàn thiện.
