export const paymentOutcome = detail => BigInt(detail.totalDue) === 0n ? 'Không phát sinh khoản thu'
  : detail.payment ? 'Đã ghi nhận thanh toán mô phỏng' : 'Chưa ghi nhận thanh toán'

// Financial errors use fixed messages; never display arbitrary backend details or profile data.
export const paymentError = error => ({
  401: 'Phiên đã hết hạn. Vui lòng vào Tài khoản để đăng nhập lại.',
  403: 'Bạn không có quyền xem đăng ký này. Hãy kiểm tra tài khoản, xác minh email và liên kết hồ sơ.',
  404: 'Không tìm thấy đăng ký. Vui lòng tải lại danh sách.',
  409: 'Dữ liệu thanh toán không nhất quán. Vui lòng liên hệ quầy Thu ngân để đối chiếu; chưa thể xác nhận thanh toán.',
  503: 'Dịch vụ dữ liệu chưa sẵn sàng. Vui lòng thử tải lại sau.',
})[error.status] || 'Không tải được dữ liệu thanh toán. Vui lòng kiểm tra kết nối và thử lại.'
