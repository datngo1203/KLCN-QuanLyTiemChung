export const paymentOutcome = detail => BigInt(detail.totalDue) === 0n ? 'Không phát sinh khoản thu'
  : detail.paymentState === 'ChoXacNhan' ? 'Chờ xác nhận'
  : detail.payment ? 'Đã thanh toán' : 'Chưa thanh toán'

export const paymentMethodLabel = method => ({
  TienMat: 'Tiền mặt',
  TheTaiQuay: 'Thẻ tại quầy',
  ChuyenKhoanQR: 'Chuyển khoản / QR',
  ChuyenKhoanOnline: 'Chuyển khoản trực tuyến',
  TheQuocTe: 'Thẻ VISA / MASTER / JCB',
  TheNoiDia: 'Thẻ / tài khoản nội địa (ATM)',
})[method] ?? 'Giao dịch trước đây'

export const registrationCode = id => `DK-${String(id).padStart(4, '0')}`

export function cardNumberValid(brand, value) {
  const digits = value.replaceAll(' ', '')
  if (!/^[0-9]{13,19}$/.test(digits)) return false
  const prefix = Number(digits.slice(0, 4))
  const brandMatch = {
    VISA: digits.startsWith('4') && [13, 16, 19].includes(digits.length),
    MASTER: digits.length === 16 && (prefix >= 5100 && prefix <= 5599 || prefix >= 2221 && prefix <= 2720),
    JCB: digits.length >= 16 && prefix >= 3528 && prefix <= 3589,
  }
  return !!brandMatch[brand] && [...digits].reverse().reduce((sum, char, index) => {
    const digit = Number(char) * (index % 2 === 0 ? 1 : 2)
    return sum + (digit > 9 ? digit - 9 : digit)
  }, 0) % 10 === 0
}

// Financial errors use fixed messages; never display arbitrary backend details or profile data.
export const paymentError = error => ({
  401: 'Phiên đã hết hạn. Vui lòng vào Tài khoản để đăng nhập lại.',
  403: 'Bạn không có quyền xem đăng ký này. Hãy kiểm tra tài khoản, xác minh email và liên kết hồ sơ.',
  404: 'Không tìm thấy đăng ký. Vui lòng tải lại danh sách.',
  409: 'Dữ liệu thanh toán không nhất quán. Vui lòng liên hệ quầy Thu ngân để đối chiếu; chưa thể xác nhận thanh toán.',
  503: 'Dịch vụ dữ liệu chưa sẵn sàng. Vui lòng thử tải lại sau.',
})[error.status] || 'Không tải được dữ liệu thanh toán. Vui lòng kiểm tra kết nối và thử lại.'
