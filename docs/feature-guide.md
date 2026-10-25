# Làm một lát từ đầu đến cuối

1. Nhận lát S0–S8 có owner theo [SCOPE.md](SCOPE.md), đọc [giới hạn khóa luận](thesis-scope-rules.md). Không lấy backlog lịch sử làm phạm vi mới.
2. Chốt endpoint/DTO và lỗi trong contracts; khi chưa có endpoint, làm API trước UI. Contract/schema cần review chéo.
3. Viết quy tắc trong Domain, mapping/truy vấn trong Infrastructure, HTTP handler trong Api. Client không truy cập SQL hoặc quyết định tiền/quyền. Không tạo Sample mới cho chức năng mới.
4. Khi đổi schema, thêm migration theo quy trình hiện có; chỉ một người sửa schema/migration tại một thời điểm. Không tự xóa DB hoặc migration đã dùng chung.
5. Giữ hash mật khẩu, validation, quyền hai vai trò và phạm vi hồ sơ tại API; kiểm transaction nơi có ghi nhiều bảng hoặc tiền. S4 chỉ thanh toán một lần mô phỏng, chốt giá lúc đăng ký; không thêm đặt cọc/trả góp/idempotency, kho hoặc khám chi tiết.
6. Nối Web và Windows vào API thật, kiểm ghi/đọc lại SQL và lỗi phù hợp. Mobile đóng băng đến sau S8.
7. Build/test phần ảnh hưởng, ghi kết quả và phần chưa kiểm vào docs/status.md; tiến độ một dòng trong SCOPE. Không tạo status riêng, ảnh review hoặc tài liệu giai đoạn mới. PR nhỏ vào `main`.
