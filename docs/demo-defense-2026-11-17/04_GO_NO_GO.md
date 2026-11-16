# Checklist ra quyết định — trước khi ngủ / sáng ngày 17-11-2026

## Ưu tiên tuyệt đối

- [ ] Docker Desktop + `vac-final` giữ RUN; API `http://127.0.0.1:5080/health` OK; Web port 5173 OK.
- [ ] Shortcut **KLCN DEMO** mở Edge ở READY; F8 #1–#6 đến Xem lại; không POST.
- [ ] `demo.admin` vào WPF A10; 1 ưu đãi hợp lệ trong SQL → Web Offers có hiển thị → Xem lại tính giảm VND.
- [ ] `demo.reception` / `demo.warehouse` / các vai trò cần thiết có quyền tương ứng; chụp màn hoặc clip trước.
- [ ] Mở đúng các màn A03 A07 A08 A09 A02 A06 A06F A05 A04 A10 A11 A12 hoặc có ảnh minh chứng thật khi không thể live.
- [ ] Nhận biết chính xác dữ liệu nào synthetic personal, nào curated vaccine nguồn công khai, nào payment mô phỏng, nào lịch xác nhận và nào chưa có.
- [ ] Mở sẵn PowerPoint V13 và folder ảnh/clip, có kế hoạch chuyển máy chiếu.
- [ ] Một lượt tổng duyệt đo thời gian <= 8:30, giữ 1:30 dự phòng. Chỉ báo PASS nếu đã chạy.

## Nếu trục trặc

- API hoặc SQL không healthy: không chạy `VAC start` hoặc reset mù. Dùng ảnh/clip và slide.
- WPF role login DENIED: không đổi quyền hoặc reset mật khẩu để ép PASS; dùng ảnh chức năng đã kiểm chứng.
- Trang ưu đãi trống: kiểm WPF A10, `valid` và thời gian hiệu lực; không tạo dữ liệu qua SQL trực tiếp.
- Các bản ghi khám/tiêm/nhắc lịch trống: trình bày code + screen với ghi chú phạm vi, không khẳng định đã chứng minh end-to-end.
- Quá thời gian: cắt sâu nghiệp vụ WPF trước; giữ Web F8 và chứng minh kho + ưu đãi + báo cáo.

## Phần luyện tập

Web F8 #1–#6 **không gửi đăng ký**, nên có thể tập lại trên cùng dữ liệu nền, không cần nút xóa. Nút baseline restore là task **sau bảo vệ**, chỉ trên DB tập luyện tách biệt. Không đụng `vac-final` ngày bảo vệ.