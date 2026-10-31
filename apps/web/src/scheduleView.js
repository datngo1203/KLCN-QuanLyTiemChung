export const appointmentCode = id => `LH-${String(id).padStart(4, '0')}`
export const appointmentTime = value => new Date(value).toLocaleString('vi-VN')

export function scheduleError(error) {
  if (error.status === 401) return 'Phiên đã hết hạn. Vui lòng đăng nhập lại.'
  if (error.status === 403) return 'Bạn không có quyền xem lịch hẹn này. Hãy kiểm tra tài khoản hoặc liên hệ trung tâm.'
  return 'Không tải được lịch hẹn. Vui lòng kiểm tra kết nối và thử lại.'
}
