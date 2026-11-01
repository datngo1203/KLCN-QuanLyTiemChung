export const vaccinationCode = id => `MT-${String(id).padStart(4, '0')}`
export { appointmentTime as vaccinationTime } from './scheduleView.js'

export function historyError(error) {
  if (error.status === 401) return 'Phiên đã hết hạn. Vui lòng đăng nhập lại.'
  if (error.status === 403) return 'Bạn không có quyền xem lịch sử tiêm này. Hãy kiểm tra tài khoản hoặc liên hệ trung tâm.'
  return 'Không tải được lịch sử tiêm. Vui lòng kiểm tra kết nối và thử lại.'
}
