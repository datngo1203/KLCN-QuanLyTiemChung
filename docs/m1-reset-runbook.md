# Reset nhân viên M1 — operator offline

**Chặn vận hành sau review P1 tại 9fa35ab:** môi trường caller tự đặt chưa phải trust boundary. Không dùng hướng dẫn env dưới đây với DB thật. [Đề xuất boundary OS/SQL và thay runbook](../contracts/m1-p1-operator-delivery-review.md) chờ review: Windows SID token, policy tại vị trí Known Folder cố định với owner/DACL được kiểm, Integrated Security/principal SQL riêng; không env/path override. Admin triển khai cấp ACL và SQL trước; test phải xác nhận caller không sửa policy/binary, không có quyền SQL trực tiếp. Không in secret hoặc coi OS/DB administrator là đối tượng có thể chống được. Chưa có code boundary mới, hướng dẫn dưới giữ truy vết implementation cũ.

Contract được người dùng chấp thuận: `f30641b6463858a2c10691054cf9090f3927521f` cùng báo cáo review kỹ thuật; không phải approval GitHub hoặc thành viên. Không HTTP reset, không self-service/force-change, không mở lại recovery ledger.

1. Đối chiếu danh tính người nhận theo quy trình được quản trị chấp nhận; tổ chức phiên hỗ trợ trực tiếp, người nhận tự nhập mật khẩu. Không đủ kênh bàn giao bảo mật đã phê duyệt thì dừng. Không gửi mail/SMS/chat hoặc lưu mật khẩu ra file.
2. Dùng đúng DB và operator binary riêng. Connection từ `M1_OPERATOR_CONNECTION` ngoài Git; không truyền password bằng argv/environment/file. Actor dùng credential hiện hành sau Admin recovery, không dùng recovery secret hoặc access token cũ.
3. Đọc targetAccountId/targetEmployeeId/username/RowVersion hiện hành qua vận hành read-only. Chạy `dotnet run --project backend/tools/TiemChung.IdentityOperator -- reset-employee-password`; nhập actor credential, cặp ID và expected RowVersion; xác nhận target hiển thị. Không tự reload version khi conflict.
4. Nhân viên nhập password hai lần trên terminal ẩn, 12–128 ký tự, không dùng password actor. Stdin redirect bị từ chối. Thành công chỉ sau commit, output chỉ ID và RowVersion mới. Không in/hash/log credential. Hạn chế transcript/recording của phiên hỗ trợ.
5. Account Locked/nhân viên inactive vẫn không login được; reset không đổi status/duty/quyền hoặc quota recovery. Chỉ dùng quy trình quản trị trạng thái riêng nếu được phép. Không gọi password là tạm hoặc hứa hết hạn/bắt đổi trong M1.
6. Timeout chưa rõ commit: dừng, đọc version/trạng thái/audit qua vận hành để xác minh, không tự chạy lại reset/recovery. Conflict cần xác thực actor và đọc target lại trong một yêu cầu mới có chủ đích.

Hash/password chỉ ở bộ nhớ khi cần; managed strings không bảo đảm xóa mọi bản sao. Audit chỉ sự kiện/actor/target/trace/tên trường; không nội dung handoff hay bằng chứng danh tính nhạy cảm.
