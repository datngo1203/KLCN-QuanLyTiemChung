import { useCallback, useEffect, useRef, useState } from 'react'
import { RefreshCw } from 'lucide-react'
import { identity } from './api/identity'
import { appointmentCode, appointmentTime, scheduleError } from './scheduleView'
import './Registrations.css'
import './Payments.css'

function ReadError({ error, retry, go }) {
  return <div className="registration-message error" role="alert">
    <p>{scheduleError(error)}</p>
    {error.status === 401 ? <button className="button outline" onClick={() => go('account')}>Vào Tài khoản</button>
      : <button className="button outline" onClick={retry}>Thử lại</button>}
  </div>
}

export default function Schedule({ account, sessionBusy, go }) {
  return <section className="registration-page payments-page wrap">
    <div className="page-intro"><div className="eyebrow">THÔNG TIN CÁ NHÂN</div><h1>Lịch hẹn của bạn</h1><p>Xem lịch do trung tâm xếp cho các hồ sơ bạn được phép truy cập. Thời gian hiển thị theo giờ địa phương của thiết bị.</p></div>
    {sessionBusy ? <p className="registration-message" role="status">Đang cập nhật phiên. Vui lòng chờ…</p>
      : account?.accountType === 'customer' ? <ScheduleWorkspace key={account.accountId} go={go} />
      : <div className="registration-panel"><h2>Đăng nhập để xem lịch hẹn</h2><p>Dùng tài khoản khách hàng để xem lịch thuộc hồ sơ của bạn.</p><button className="button primary" onClick={() => go('account')}>Vào Tài khoản</button></div>}
  </section>
}

function ScheduleWorkspace({ go }) {
  const [page, setPage] = useState(1)
  const [reload, setReload] = useState(0)
  const [list, setList] = useState({ loading: true, rows: [], error: null })
  const [selectedId, setSelectedId] = useState(null)
  const [detailReload, setDetailReload] = useState(0)
  const heading = useRef(null)

  useEffect(() => {
    let current = true
    identity.appointments(`?page=${page}&pageSize=25`)
      .then(response => { if (current) setList({ loading: false, rows: response.items, error: null }) })
      .catch(error => { if (current) setList({ loading: false, rows: [], error }) })
    return () => { current = false }
  }, [page, reload])

  function loadList(nextPage = page) {
    setSelectedId(null); setList({ loading: true, rows: [], error: null })
    setPage(nextPage); setReload(value => value + 1)
    heading.current?.focus()
  }
  const accessDenied = useCallback(error => {
    setSelectedId(null); setList({ loading: false, rows: [], error })
    heading.current?.focus()
  }, [])

  return <div className="payments-workspace">
    <section className="registration-panel" aria-labelledby="schedule-list-title" aria-busy={list.loading}>
      <h2 id="schedule-list-title" tabIndex={-1} ref={heading}>Danh sách lịch hẹn</h2>
      <button className="button outline" disabled={list.loading} onClick={() => loadList()}><RefreshCw aria-hidden="true" />Tải lại danh sách</button>
      {list.loading && <p role="status">Đang tải lịch hẹn…</p>}
      {list.error && <ReadError error={list.error} retry={() => loadList()} go={go} />}
      {!list.loading && !list.error && <>
        {list.rows.length === 0 && <p role="status">Không có lịch hẹn ở trang này.</p>}
        <ul className="payment-registration-list">{list.rows.map(row => <li key={row.id}>
          <button className="payment-select" aria-pressed={selectedId === row.id} aria-controls="schedule-detail" onClick={() => setSelectedId(row.id)}>
            <strong>{appointmentCode(row.id)}</strong><span>Người được tiêm: {row.recipient.fullName}</span>
            <span>{row.itemKind === 'package' ? 'Gói vắc xin' : 'Vắc xin lẻ'}: {row.serviceName}</span>
            <time dateTime={row.scheduledAt}>{appointmentTime(row.scheduledAt)}</time><span>Địa điểm: {row.location}</span>
            <span>Trạng thái: {row.status}</span><span className="payment-select-hint">Xem chi tiết</span>
          </button>
        </li>)}</ul>
        <nav className="registration-actions" aria-label="Trang lịch hẹn"><button className="button outline" disabled={page === 1} onClick={() => loadList(page - 1)}>Trang trước</button><span>Trang {page}</span><button className="button outline" disabled={list.rows.length < 25} onClick={() => loadList(page + 1)}>Trang sau</button></nav>
      </>}
    </section>
    <section id="schedule-detail" className="registration-panel" aria-label="Chi tiết lịch hẹn">
      {selectedId === null ? <><h2>Chi tiết lịch hẹn</h2><p>Chọn một lịch để xem thông tin buổi hẹn.</p></>
        : <ScheduleDetail key={`${selectedId}-${detailReload}`} id={selectedId} retry={() => setDetailReload(value => value + 1)} accessDenied={accessDenied} go={go} />}
    </section>
  </div>
}

function ScheduleDetail({ id, retry, accessDenied, go }) {
  const [state, setState] = useState({ loading: true, detail: null, error: null })
  const heading = useRef(null)
  useEffect(() => {
    heading.current?.focus({ preventScroll: true })
    heading.current?.scrollIntoView({ block: 'start', behavior: 'instant' })
    let current = true
    identity.getAppointment(id)
      .then(detail => { if (current) setState({ loading: false, detail, error: null }) })
      .catch(error => {
        if (!current) return
        if ([401, 403].includes(error.status)) accessDenied(error)
        else setState({ loading: false, detail: null, error })
      })
    return () => { current = false }
  }, [id, accessDenied])
  const { loading, detail, error } = state
  return <div aria-busy={loading}>
    <h2 ref={heading} tabIndex={-1}>Lịch hẹn {appointmentCode(id)}</h2>
    <button className="button outline" disabled={loading} onClick={retry}><RefreshCw aria-hidden="true" />Tải lại chi tiết</button>
    {loading && <p role="status">Đang tải chi tiết lịch hẹn…</p>}
    {error && <ReadError error={error} retry={retry} go={go} />}
    {detail && <>
      <dl className="payment-transaction">
        <div><dt>Khách hàng</dt><dd>{detail.customer.fullName}</dd></div>
        <div><dt>Người được tiêm</dt><dd>{detail.recipient.fullName}</dd></div>
        <div><dt>{detail.itemKind === 'package' ? 'Gói vắc xin' : 'Vắc xin lẻ'}</dt><dd>{detail.serviceName}</dd></div>
        <div><dt>Thời gian (giờ địa phương)</dt><dd><time dateTime={detail.scheduledAt}>{appointmentTime(detail.scheduledAt)}</time></dd></div>
        <div><dt>Địa điểm</dt><dd>{detail.location}</dd></div>
        <div><dt>Trạng thái</dt><dd>{detail.status}</dd></div>
        <div><dt>Mã đăng ký nguồn</dt><dd>DK-{String(detail.registrationId).padStart(4, '0')}</dd></div>
      </dl>
      <p className="muted">Lịch hẹn không phải kết quả tiêm. Khi tải lại trang, bạn cần đăng nhập lại để xem lịch.</p>
    </>}
  </div>
}
