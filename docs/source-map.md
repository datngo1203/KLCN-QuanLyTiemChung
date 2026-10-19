# Nguồn và nguồn gốc

Đối chiếu trực tiếp ngày 19/10/2026 với hai tài liệu Project Source: `01_KLCN_Project_Context_UPDATED.md`, `02_KLCN_Current_Checkpoint_UPDATED_2026-10-01.md`, cùng sơ đồ đã tạo trong cuộc hội thoại. Các sơ đồ kế thừa được xây dựa trên bài giảng Chương 5/6/7 ở các bước trước; không coi đây là lần audit lại toàn bộ PDF. Nguồn gốc giữ nguyên tại Project Source; không đưa PDF bài giảng lên repo hoặc sửa nguồn để khớp code.

Thứ tự: đề cương/nhận xét giảng viên → quyết định nhóm trong phiên làm việc → context/checkpoint → nguồn công khai → giả định kỹ thuật được ghi rõ. Yêu cầu trực tiếp 19/10/2026 bổ sung mobile cùng Windows và Website.

`apps/web` và `apps/windows` sao chép code UI đã có từ workspace Tiêm chủng; không chép node_modules/bin/obj, bản thảo DOCX hoặc gói Stitch. `docs/ui-checklist-inherited.md` là checklist lịch sử, các giới hạn của đợt UI cũ được ghi lại để hiểu nguồn; phạm vi phát triển mới nằm trong README/AGENTS và docs/status.md. Chỉ dùng mẫu hư cấu hiện có, không khẳng định là dữ liệu nội bộ VNVC. Giao diện chứa vài liên kết tham khảo y tế; cần review nội dung trước phát hành, không dùng chúng để tự động chỉ định tiêm.

`docs/diagrams` sao chép PNG/SVG/PlantUML đã tạo; mô hình quan hệ được dùng để sinh 27 thực thể EF ban đầu. Đây là bản nền để review, không khẳng định mọi quan hệ đã được nghiệp vụ phê duyệt. `database/README.md` ghi các khác biệt cần xử lý (hash mật khẩu, tài khoản khách hàng, tổng tiền snapshot, số lô, người bảo trợ).

Tham khảo kỹ thuật chính thức: [.NET 10](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/), [Flutter architecture](https://docs.flutter.dev/app-architecture/guide), [Flutter install](https://docs.flutter.dev/install), [GitHub Actions .NET](https://docs.github.com/en/actions/tutorials/build-and-test-code/net). Phiên bản phụ thuộc nằm trong project/lockfile, không tự nâng cấp khi đang tích hợp.
