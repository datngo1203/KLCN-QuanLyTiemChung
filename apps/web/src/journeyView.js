export function journeyState({ payment, appointments, nextAppointment, shots }) {
  const due = BigInt(payment.totalDue) > 0n
  const center = payment.expectedChannel === 'TaiTrungTam'
  const paid = payment.paymentState === 'ThanhCong'
  const pending = due && payment.paymentState === 'ChoXacNhan'
  const unpaid = due && !paid && !pending && !center
  const paymentLabel = pending ? 'Chờ xác nhận thanh toán' : paid ? 'Đã thanh toán'
    : !due ? 'Không phát sinh khoản thu' : center ? 'Thanh toán tại trung tâm' : 'Chờ thanh toán'
  const schedule = nextAppointment ? 'Đã xác nhận lịch' : appointments.length
    ? (shots.length ? 'Đã ghi nhận lịch hẹn' : 'Lịch hẹn đã qua, chưa có mũi tiêm được ghi nhận')
    : 'Chờ trung tâm xác nhận lịch'
  return {
    paymentLabel, schedule,
    action: pending || unpaid ? paymentLabel : nextAppointment ? schedule : shots.length ? 'Tiêm & theo dõi' : schedule,
    step: pending || unpaid ? 1 : shots.length ? 3 : 2,
  }
}
