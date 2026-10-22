# Gói UI thay thế #27 — 23/10/2026

Owner Windows: Đạt; cần Bảo hoặc Quý review độc lập. Base develop `f188084389a0033b856594632ae2c6495b29e248`. Không kéo lịch sử 12 commit cũ: chỉ lấy cây của 19 file UI tại #27 `582ccc4`, thêm sửa nhãn gợi ý và focus panel. Không thay API/contract/schema/mobile/web/quyền cộng tác. #27 giữ Draft, không tích hợp đồng thời với gói thay thế.

## Sửa và giới hạn

- Giữ menu ngang/drawer, token theo màn, nhãn người dùng, layout quyền A12, panel phiên A13 và A14 qua tài khoản.
- A02 chỉ chạy từ khóa minh họa khi dùng dữ liệu mẫu; cảnh báo ghi rõ chưa duyệt chuyên môn và “Chờ bổ sung theo đề cương”. Không tự đổi cờ nguy cơ hoặc kết luận. Checkbox nguy cơ và hai bước xác nhận là thao tác thủ công của UI mẫu; không phải quy tắc y tế đã được nghiệm thu.
- Kết quả tìm chức năng dùng ListView và tên automation theo mã màn để UIA truy cập nút được; script đã Invoke trực tiếp qua A01–A14. Panel search/drawer/tài khoản nhận focus khi mở, giới hạn vòng Tab và trả focus khi đóng. Chưa chạy kiểm phím vật lý, nên chưa nghiệm thu hành vi bàn phím.
- A13 chỉ hiển thị chức danh/quyền mẫu hoặc chưa có nguồn. API vẫn phải xác thực/phân quyền. Login/đổi mật khẩu là mô phỏng.

## Kiểm tra theo mã nguồn

Code `c239041`: Release build 0 warning/0 error; harness WPF STA 54 PASS. Lệnh từ gốc repo:

```powershell
dotnet build apps/windows/TiemChung.Desktop.csproj -c Release --no-restore
dotnet run --project scripts/ui-review/PR27.Render/Review.csproj -c Release
```

[24 ảnh và manifest SHA/hash](ui-review/pr27-replacement/manifest.json), [log](ui-review/pr27-replacement/results.txt), [harness](../scripts/ui-review/PR27.Render/Program.cs). Ảnh commit để reviewer truy cập được, mỗi ảnh dưới 500 KB; giữ đủ 56 blob ảnh lịch sử của develop.

Harness nạp/measure/arrange/render Login, A01/A02/A04/A05/A07/A11/A12/A14, A13 trên shell; kiểm command/chuyển màn, A02 valid/invalid/empty/Sample off, không tự kết luận, hai xác nhận/reset sau sửa tiền sử; A12 tự sửa quyền/khóa/reset bị chặn, chọn tài khoản khác, tab quyền, CCCD sai, Sample off; A13 có/không nguồn, mở A14; A14 mismatch, lỗi dịch vụ mẫu, không nguồn, hủy; drawer đóng sau điều hướng, reset identity/password/navigation khi phiên mới.

Ảnh là render nội dung được arrange ở 1366×768 hoặc 980×620 DIP, bitmap 96 DPI. Đã xem ảnh A02 hint, A12 quyền/hẹp, A13 và drawer; đây không phải ảnh cửa sổ native hay bằng chứng hệ điều hành ở DPI 100/125/150%. Viewport hữu hạn còn cần cuộn ở A02/A12 và drawer hẹp. A14-error là mismatch làm nút lưu vô hiệu; A14-service-error mới là phản hồi lỗi dịch vụ mẫu.

Chưa kiểm: bàn phím vật lý/Enter/Esc/Tab/Shift+Tab, DPI thực/multi-monitor, chuột/cuộn E2E, login→logout→login qua hộp thoại, mọi trạng thái của các màn khác, Gallery, API/SQL/auth/quyền/lưu thật. Script PrintWindow thuộc [gói housekeeping #30](https://github.com/datngo1203/KLCN-QuanLyTiemChung/pull/30): script 9eefc68 + host UI c239041 đã điều hướng/capture A01–A14 và các viewport A02/A12/A13/A14 hẹp/rộng (22 ảnh native ở DPI120/125%). Đây chưa là checklist DPI hoặc bàn phím đầy đủ. A04 form gói và A11 xuất tệp vẫn ngoài lần sửa này.

Reviewer xem 19 file UI, nhãn A02, tính đầy đủ A12 khi cuộn/hẹp, focus/thoát panel và A13→A14. Cần chạy checklist bàn phím/DPI còn thiếu trước nghiệm thu tương tác; không coi CI build là render/E2E.
