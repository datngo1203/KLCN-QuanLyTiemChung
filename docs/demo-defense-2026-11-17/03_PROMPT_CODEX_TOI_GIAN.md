# Handoff một lần cho Codex — ĐẾN ĐÍCH TRƯỚC BUỔI BẢO VỆ

**Model:** GPT-6.1 Sol Medium, tối ưu thời gian/quota. Nếu cần thay đổi SQL hoặc clinical/money/inventory code (không được yêu cầu ở đây), STOP báo coordinator; không tự nâng scope.

**Task: DEMO-readiness only, ngoài business feature.** Đọc `AGENTS.md`, `SCOPE.md`, issue #194 và các màn liên quan khi cần. CODE repo branch `codex/data-r2-catalog-quality`, exact expected SHA `a92af63011eb2accddf402dfd7e6cdac402b49fe`; xác minh HEAD thực tế trước khi làm. Không switch main, không merge PR #204. Không sửa source app, backend, migration, auth, nghiệp vụ; không tạo PR/feature branch.

Bộ thiết kế ở thư mục đính kèm gồm `01_MA_TRAN_AUDIT.md`, `02_KICH_BAN_08PHUT30.md`, `04_GO_NO_GO.md`. Tận dụng `C:\Users\DAT\Documents\KLCN-Demo-Control\demo.cjs`, shortcut Desktop, F8 #1–#6 PASS. **Không viết lại phần Web.**

## A — MUST HAVE (ưu tiên, bỏ mọi việc khác nếu không đủ thời gian)

1. Chỉ làm việc trên Docker Compose **`vac-final`**, SQL **`TiemChung`**; runtime hiện hành đang RUN. Không `down -v`, reset, reseed, restore, migration, rebuild hay `VAC -Mode start` khi chưa kiểm chứng.
2. Xác minh `READY`, Web F8 #1–#6 một lượt. Giữ tiến trình/credentials hiện có. Đo thời gian chặng Web, chụp ảnh cuối.
3. **Ưu đãi đang trống:** qua Windows A10 bằng tài khoản `demo.admin` **chỉ khi đúng quyền**, tìm trước rồi tạo **đúng một** chương trình: `Ưu đãi chăm sóc sức khỏe gia đình – Tháng 11/2026`, mô tả/điều kiện/times/50.000 VND tại file kịch bản. Đây là dữ liệu chương trình nội bộ mô phỏng, không phải quảng cáo thật của VNVC. Tạo qua GUI/API hiện hành, không raw SQL. Tạo xong reload WPF, Web `#promotions`, kiểm nằm trong danh sách `valid`, kiểm hiện trong ô chọn Xem lại và khoản giảm/tổng khi chọn. KHÔNG gửi đăng ký. Nếu không có quyền hoặc lỗi thì dừng tạo, báo BLOCKED; tuyệt đối không tự cấp quyền.
4. Dùng WPF/SQL API read-only kiểm kê xem có bản ghi đủ thật cho A03, A07, A08 (mô phỏng), A09 (lịch và reminder), A02, A06, A06F, A05 kho và lịch sử tiếp nhận, A11 báo cáo, A04 catalog, A12 quyền. Chỉ báo `PASS` khi thấy GUI + dữ liệu phù hợp, `EMPTY`, `DENIED`, `UNVERIFIED` khi chưa đạt. KHÔNG tự sinh kết luận/chỉ định, thanh toán, ghi tiêm hoặc nhập kho để lấp chỗ trống.
5. Chụp màn hình evidence của từng nhóm sẵn có. Nếu đã có công cụ ghi hình hợp lý, làm **một clip proof ngắn** bằng cách thao tác thật trên màn hình; không bắt buộc nếu có nguy cơ tốn thời gian. Nếu WPF login thất bại, lưu nhật ký lỗi và dùng ảnh/screenshot có nguồn của các module có thể xác minh.
6. Tạo `DEMO-SANG-MAI.md`: thứ tự 8:30, cửa sổ cần mở, tài khoản cần dùng, chặng nào là LIVE/chặng nào CLIP/SCREENSHOT, phím F8/F9/Esc, dấu mốc bản ghi SQL, cách fallback, PASS/BLOCKED. Bảng này phải có đủ mọi tiêu chí trong `01_MA_TRAN_AUDIT.md`.

## B — ONLY IF A IS FINISHED

- Đo thời gian bằng một lượt tổng duyệt. Nếu quá 8:30, rút đoạn WPF hoặc thay bằng screenshot đã xác minh, không giảm tốc gõ đến mức vô hình.
- Sao chép toàn bộ bằng chứng lên Desktop/thư mục dễ tìm (không chứa mật khẩu hoặc log nhạy cảm).
- Giữ backup SQL hiện có. Không xây nút reset database đêm nay. Web F8 #1–#6 đã chạy mà không ghi dữ liệu, nên có thể tập lại.

## DO NOT

- Không xây framework automation mới/chuỗi hàng chục F8 WPF.
- Không claim `payOS` real QR, SMS thật, S8 full E2E, package curated thật, medical decision automation khi chưa được nghiệm thu.
- Không fake API/SQL, không đổi trạng thái nghiệp vụ qua DB, không dùng mock GUI để giả PASS.
- Không làm mất `demo.cjs`, shortcut, `vac-final`, hồ sơ Nguyễn Minh An/Gia Hân hay danh mục 15 vắc xin.

**Handoff phải cực ngắn:** (1) PASS/BLOCKED cho ma trận, (2) bằng chứng path, (3) thời gian tổng thực tế, (4) phím chạy sáng mai, (5) các hạn chế còn lại. Hoàn thành A rồi STOP, không hỏi thêm hoặc mở tính năng mới.