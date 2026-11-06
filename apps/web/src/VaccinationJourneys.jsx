import { useCallback, useEffect, useState } from 'react'
import { identity } from './api/identity'
import { HistoryWorkspace } from './VaccinationHistory'
import { PaymentDetail } from './Payments'
import { paymentError, registrationCode } from './paymentView'
import { preferredTimeLabel } from './registrationForm'
import { appointmentCode, appointmentTime, appointmentStatus } from './scheduleView'
import { vaccinationCode, vaccinationTime } from './historyView'
import { journeyState } from './journeyView'
import { customerDisplayName } from './customerDisplayName'
import './VaccinationJourneys.css'

const hub = 'vaccinations'
const history = 'vaccinations/history'

export default function VaccinationJourneys({ account, sessionBusy, page, go }) {
  return <section className="registration-page payments-page checkout-page journey-page wrap">
    <div className="page-intro">
      <h1>Tiêm chủng của tôi</h1>
      <p>Theo dõi đăng ký, thanh toán, lịch hẹn và các mũi tiêm đã ghi nhận. Lịch hẹn không thay thế khám sàng lọc và chỉ định của bác sĩ.</p>
    </div>
    <nav className="journey-tabs" aria-label="Tiêm chủng của tôi">
      <button className="button outline" aria-current={page !== history ? 'page' : undefined} onClick={() => go(hub)}>Đang thực hiện</button>
      <button className="button outline" aria-current={page === history ? 'page' : undefined} onClick={() => go(history)}>Lịch sử tiêm</button>
    </nav>
    {sessionBusy ? <p role="status">Đang cập nhật phiên…</p>
      : account?.accountType !== 'customer' ? <div className="registration-panel">
        <h2>Đăng nhập tài khoản khách hàng</h2>
        <button className="button primary" onClick={() => go('account')}>Đăng nhập</button>
      </div>
      : page === history ? <><h2>Lịch sử tiêm</h2><HistoryWorkspace go={go} /></>
      : page === hub ? <JourneyList go={go} />
      : <JourneyDetail id={Number(page.slice(hub.length + 1))} go={go} />}
  </section>
}

function JourneyError({ error, retry, go }) {
  return <div className="registration-message error" role="alert">
    <p>{[401, 403, 409].includes(error.status) ? paymentError(error) : 'Không tải được hành trình. Vui lòng thử lại.'}</p>
    {error.status === 401 ? <button className="button outline" onClick={() => go('account')}>Đăng nhập</button>
      : <button className="button outline" onClick={retry}>Thử lại</button>}
  </div>
}

function JourneyList({ go }) {
  const [page, setPage] = useState(1)
  const [reload, setReload] = useState(0)
  const [state, setState] = useState({ loading: true })
  useEffect(() => {
    let active = true
    identity.vaccinationJourneys(`?page=${page}&pageSize=25`).then(
      ({ items }) => { if (active) setState({ items }) },
      error => { if (active) setState({ error }) },
    )
    return () => { active = false }
  }, [page, reload])

  function load(next = page) {
    setState({ loading: true })
    setPage(next)
    setReload(value => value + 1)
  }

  return <div aria-busy={!!state.loading}>
    <h2>Hành trình theo đăng ký</h2>
    {state.loading && <p role="status">Đang tải hành trình…</p>}
    {state.error && <JourneyError error={state.error} retry={() => load()} go={go} />}
    {state.items && <>
      {state.items.length === 0 && <div className="registration-panel">
        <p role="status">{page === 1 ? 'Bạn chưa có đăng ký tiêm.' : 'Không còn đăng ký trên trang này.'}</p>
        {page === 1 && <button className="button primary" onClick={() => go('register')}>Đăng ký tiêm</button>}
      </div>}
      <div className="journey-grid">{state.items.map(journey => {
        const { registrationId, createdAt, preferredDate, preferredAt, payment, nextAppointment } = journey
        const { action, paymentLabel, schedule } = journeyState(journey)
        return <article key={registrationId} className="registration-panel journey-card">
          <span className="registration-status">{registrationCode(registrationId)}</span>
          <h3>{payment.recipient.fullName}</h3>
          <p>{payment.items.slice(0, 2).map(item => `${item.name} × ${item.quantity}`).join(' · ')}
            {payment.items.length > 2 && ` · Và ${payment.items.length - 2} dịch vụ khác`}</p>
          <dl className="registration-review-profiles">
            <div><dt>Ngày đăng ký</dt><dd>{appointmentTime(createdAt)}</dd></div>
            <div><dt>Thời gian mong muốn</dt><dd>{preferredTimeLabel(preferredAt, preferredDate)}</dd></div>
            <div><dt>Thanh toán</dt><dd>{paymentLabel}</dd></div>
            <div><dt>Lịch hẹn đã xác nhận</dt><dd>{schedule}{nextAppointment && <><br />{appointmentTime(nextAppointment.scheduledAt)}<br />{nextAppointment.location} · {nextAppointment.serviceName}</>}</dd></div>
          </dl>
          <JourneyProgress progress={journey.progress} />
          <p className="journey-action"><small>Việc cần làm tiếp theo</small><strong>{action}</strong></p>
          <button className="button primary" onClick={() => go(`${hub}/${registrationId}`)}>Xem hành trình</button>
        </article>
      })}</div>
      {(page > 1 || state.items.length === 25) && <nav className="registration-actions" aria-label="Trang hành trình">
        <button className="button outline" disabled={page === 1} onClick={() => load(page - 1)}>Trang trước</button>
        <span>Trang {page}</span>
        <button className="button outline" disabled={state.items.length < 25} onClick={() => load(page + 1)}>Trang sau</button>
      </nav>}
    </>}
  </div>
}

function JourneyDetail({ id, go }) {
  const [reload, setReload] = useState(0)
  const [state, setState] = useState({ loading: true })
  useEffect(() => {
    let active = true
    identity.getVaccinationJourney(id).then(
      journey => { if (active) setState({ journey, initialPayment: journey.payment }) },
      error => { if (active) setState({ error }) },
    )
    return () => { active = false }
  }, [id, reload])

  const accessDenied = useCallback(error => setState({ error }), [])
  const loadPayment = useCallback(async () => {
    const journey = await identity.getVaccinationJourney(id)
    setState(previous => ({ ...previous, journey }))
    return journey.payment
  }, [id])

  const { journey, error, loading, initialPayment } = state
  const next = journey && journeyState(journey)
  return <>
    <button className="button text-button" onClick={() => go(hub)}>← Về danh sách hành trình</button>
    {loading && <p role="status">Đang tải hành trình…</p>}
    {error && <JourneyError error={error} go={go} retry={() => { setState({ loading: true }); setReload(value => value + 1) }} />}
    {journey && <>
      <h2>{registrationCode(id)} · {journey.payment.recipient.fullName}</h2>
      <p>Đăng ký {appointmentTime(journey.createdAt)} · Thời gian mong muốn {preferredTimeLabel(journey.preferredAt, journey.preferredDate)}</p>
      <section className="registration-panel journey-next">
        <h3>Việc cần làm tiếp theo</h3>
        <p><strong>{next.action}</strong></p>
        {next.step > 1 && journey.nextAppointment && <p>{appointmentTime(journey.nextAppointment.scheduledAt)} · {journey.nextAppointment.location} · {journey.nextAppointment.serviceName}</p>}
        {next.step > 1 && next.paymentLabel === 'Thanh toán tại trung tâm' && <p>{next.paymentLabel}</p>}
      </section>
      <ol className="registration-steps journey-steps" aria-label="Tiến trình tiêm chủng">
        {['Đăng ký', 'Thanh toán', 'Xác nhận lịch', 'Tiêm & theo dõi'].map((label, index) =>
          <li key={label} aria-current={next.step === index ? 'step' : undefined}><span>{index + 1}</span>{label}</li>)}
      </ol>
      <PaymentDetail id={id} accessDenied={accessDenied} go={go} initialDetail={initialPayment} loadDetail={loadPayment} />
      <section className="registration-panel">
        <h3>Tiến độ theo vắc xin</h3>
        <JourneyProgress progress={journey.progress} />
      </section>
      <div className="journey-grid">
        <section className="registration-panel">
          <h3>Lịch hẹn đã xác nhận</h3>
          {journey.appointments.length === 0 ? <p>Trung tâm chưa xác nhận lịch hẹn.</p> : <ul className="journey-events">
            {journey.appointments.map(item => <li key={item.id}><strong>{appointmentCode(item.id)} · {item.serviceName}</strong>
              <span>{appointmentTime(item.scheduledAt)} · {item.location}</span><span>{appointmentStatus(item.status)}</span></li>)}
          </ul>}
        </section>
        <section className="registration-panel">
          <h3>Mũi tiêm đã ghi nhận ({journey.shots.length})</h3>
          {journey.shots.length === 0 ? <p>Chưa có mũi tiêm được ghi nhận.</p> : <ul className="journey-events">
            {journey.shots.map(item => <li key={item.id}><strong>{vaccinationCode(item.id)} · {item.vaccine.name}</strong>
              <span>{vaccinationTime(item.administeredAt)} · Lô {item.lotNumber}</span></li>)}
          </ul>}
        </section>
      </div>
    </>}
  </>
}

function JourneyProgress({ progress }) {
  if (!progress?.length) return <p>Chưa đủ thông tin để xác định số mũi đã đăng ký.</p>
  return <ul className="journey-events journey-progress" aria-label="Tiến độ theo vắc xin">
    {progress.map(item => <li key={item.vaccineId}>
      <strong>{customerDisplayName(item.vaccineName)}</strong>
      {Number.isInteger(item.registeredDoses) && Number.isInteger(item.remainingDoses)
        ? <span>Đã đăng ký {item.registeredDoses} mũi · Đã tiêm {item.administeredDoses} · Còn lại {item.remainingDoses}</span>
        : <span>Đã tiêm {item.administeredDoses} · Chưa xác định số mũi còn lại</span>}
      <span>{item.nextAppointment
        ? <>Lịch hẹn đã xác nhận tiếp theo: {appointmentTime(item.nextAppointment.scheduledAt)} · {item.nextAppointment.location}</>
        : 'Chưa có lịch hẹn tiếp theo được xác nhận.'}</span>
    </li>)}
  </ul>
}
