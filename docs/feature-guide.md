# Làm một module từ đầu đến cuối

1. Nhận issue có owner và điều kiện hoàn tất. Đối chiếu docs/project-context.md và sơ đồ; ghi quyết định mới vào PR.
2. Chốt endpoint/DTO và lỗi trong contracts. Tách dữ liệu nhạy cảm khỏi DTO danh mục công khai.
3. Viết quy tắc trong Domain, truy vấn/mapping trong Infrastructure, HTTP handler trong Api theo thư mục module. Không đặt quy tắc tiền/quyền ở client.
4. Khi đổi schema, thêm migration; cùng Đạt kiểm tra các FK/nullable/unique và dữ liệu hiện có. Không tự xóa DB để sửa lỗi migration.
5. Thêm test cho input, quyền, chủ hồ sơ và transaction liên quan. Không dùng test mock thay cho kiểm chứng SQL cạnh tranh.
6. Nối vào client mình phụ trách. Windows dùng service riêng; React tách App.jsx theo feature; Flutter tách features, dùng repository để có thể test.
7. Demo bằng dữ liệu hư cấu. Ghi kết quả build/test và điểm chưa xác nhận, tạo PR vào develop để review.

Với đăng ký/thanh toán: giữ cả vắc xin lẻ và gói, snapshot giá/giảm giá, idempotency khi gửi lặp, chỉ trừ khoản thành công. Với kho: cập nhật tồn có điều kiện trong transaction. Với khám/tiêm: người có quyền xác nhận, không tự suy luận tiêu chí y tế bằng code mẫu.
