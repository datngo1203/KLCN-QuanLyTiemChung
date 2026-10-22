# Quy trình làm giao diện Windows

Đọc tài liệu này ở đầu **mọi phiên làm UI Windows**, cùng [Screen Spec](desktop-screen-specs.md), [Design Spec](design/design-spec-clinical-emerald.md), [Plan](windows-ui-redesign-plan.md), [task board](task-board.md) và trạng thái nhóm. Đặc tả quyền đã duyệt là mục tiêu tích hợp; trạng thái Sample hiện tại chưa phải thực thi quyền.

## Quy tắc UI

- Làm trong `apps/windows/ViewsV2/`; không mở hoặc sao chép giao diện `_Legacy/`. Thư mục `_Legacy/` hiện chưa được tạo; giao diện cũ vẫn ở gốc `apps/windows/` và chỉ xem khi có việc di trú cụ thể.
- Không hardcode màu, cỡ chữ hoặc viết style inline mới; dùng resource `apps/windows/Themes/` và token `CE.*`. Nếu thiếu token, bổ sung qua phạm vi được giao và đối chiếu Design Spec.
- Mỗi màn phải bám Screen Spec về trường dữ liệu, command, trạng thái bình thường/lỗi/rỗng và quy tắc kiểm tra dữ liệu. Kiểm tra trường hợp biên, thao tác chuột/bàn phím và trạng thái vô hiệu.
- Không hiện tên class, DTO, API hoặc chữ `Sample` trên giao diện. Nếu dùng dữ liệu mẫu, mỗi màn có đúng một nhãn người dùng đọc được: **Dữ liệu mẫu**.
- Page XAML hướng tới dưới 200 dòng. Nếu vượt vì nhiều vùng chức năng, tách `UserControl` con theo trách nhiệm; không bỏ trường hoặc hành vi trong Screen Spec để giảm số dòng.

## Dữ liệu và quyền

- Khi API thật chưa có, định nghĩa `Services/I<Tên>Service` và bản `Services/Sample<Tên>Service`; ViewModel chỉ phụ thuộc interface. Ghi `TODO` tại điểm nối API thật. Dữ liệu mẫu phải có cờ bật/tắt; khi tắt mà API chưa sẵn sàng, hiển thị trạng thái chưa có nguồn.
- Ghi vào file status **riêng của nhóm/màn** các phương thức service và DTO màn cần, rồi đối chiếu với `contracts/`. Nêu rõ phần chưa có contract hoặc chưa được kiểm chứng; không tự đặt route, sửa contract/schema hay khẳng định có API thật.
- Chưa thực hiện kiểm tra chức danh/quyền UI trước khi MainViewModel (trách nhiệm shell hiện thuộc `ShellViewModel`) có dữ liệu quyền. Thuộc tính `CanXxx` cần cho thao tác mặc định `true` và có `TODO` nối quyền. Validation, trạng thái tải/lưu và ngăn gửi trùng vẫn phải hoạt động. Khi tích hợp, UI lọc menu/khóa thao tác theo ma trận; API phải kiểm tra quyền và phạm vi dữ liệu.

## Phân công

- Đạt làm toàn bộ giao diện Windows. Bảo và Quý làm API các module của mình theo [phân công nhóm](team.md); họ review/tích hợp contract liên quan, không cùng sửa UI Windows khi chưa thống nhất phạm vi.
- Thay đổi contract, schema và quyền cần owner module cùng người review chéo. Tài liệu này không thay đổi quyền sở hữu backend hiện tại.

## Hoàn tất từng màn

1. Đối chiếu từng trường, command, trạng thái và kiểm tra dữ liệu với Screen Spec; ghi trường hợp biên và chức năng còn thiếu vào file status riêng của nhóm/màn.
2. Build phần Windows theo [hướng dẫn kiểm thử](testing.md). Chạy màn, chụp PNG trạng thái bình thường, lỗi và rỗng; ảnh dưới 500 KB mỗi tệp, không có thông tin người thật.
3. Lưu ảnh tại `docs/ui-review/<tên-nhánh>/`; ghi lệnh, kết quả build, cách chụp và các giới hạn kiểm chứng vào status riêng. Không đánh dấu đã tích hợp API hoặc phân quyền khi chỉ dùng Sample.
4. Commit riêng cho từng màn, chỉ add các file thuộc màn và tài liệu kiểm chứng tương ứng.

## Git và PR

- Mỗi phạm vi dùng nhánh mới từ `main` mới nhất, đặt `ui/<tên>`; commit nhỏ, `git add` từng file. Không push lên `main`, force push, merge hoặc rebase nhánh người khác.
- Trước PR: `git fetch origin` rồi `git rebase origin/main` trên **nhánh của mình**. Xử lý xung đột trong phạm vi; nếu xung đột ở file ngoài phạm vi, dừng và báo để owner xử lý.
- PR vào `main` theo mẫu `.github`: nêu tiêu đề, danh sách màn, ảnh, kết quả build/CI, chức năng còn thiếu và `Refs #<issue>` theo [task board](task-board.md). Không dùng `Closes` trừ khi người dùng yêu cầu. Mỗi nhóm giữ file status riêng; người dùng gộp vào `docs/status.md`.
