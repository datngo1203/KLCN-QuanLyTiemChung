# Mobile review kế thừa PR #21 — 23/10/2026

- Nhánh `codex/mobile-review-ready`, kế thừa `3741e7a`, merge develop `f188084` bằng `70e4af8`. Không đưa `38c4f84`/`fa027ee` hoặc các PR Windows #27/#29/#30 vào gói mobile.
- Quý giữ owner mobile. Người dùng cho phép thực hiện gói sửa; cần Bảo review luồng khách hàng và Đạt review y tế/quyền. Không merge.
- Bỏ suy diễn bệnh/phác đồ/đối tượng/tồn kho và các khẳng định GSP/chính sách giá. API demo/database/nguồn chưa rõ được gắn nhãn. Null giá khác giá 0; malformed là lỗi, empty là thành công rỗng, lỗi mạng không thay bằng sample.
- Lịch/hồ sơ có banner dữ liệu mẫu; hành động mô phỏng không báo gửi thật; bỏ hướng dẫn y tế/hotline không có nguồn. Backend/contract/schema không sửa.
- Kiểm tra local: Flutter 3.47.7; 18 test PASS, gồm HTTP adapter và widget danh mục/chi tiết/navigation. Analyze ban đầu báo 2 lint dấu ngoặc, đã sửa và đang chạy lại. `flutter build apk --debug` local không chạy được vì máy không có Android SDK.
- Runtime web đang thử; chưa có thiết bị Android/emulator. Chưa kiểm API nghiệp vụ, SQL, auth/quyền, lưu hồ sơ/lịch/nhắc/gửi báo cáo thật, iOS hoặc ký phát hành.
- CI và SHA kiểm chứng sẽ cập nhật sau khi có kết quả. Test HTTP dùng fixture, không phải bằng chứng API/SQL thật.
