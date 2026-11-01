import { useEffect, useRef, useState } from 'react'
import { RefreshCw } from 'lucide-react'
import { identity } from './api/identity'
import { vaccinationCode, vaccinationTime, historyError } from './historyView'
import './Registrations.css'
import './Payments.css'

export default function VaccinationHistory({ account, sessionBusy, go }) {
  return <section className="registration-page payments-page wrap">
    <div className="page-intro"><div className="eyebrow">THÔNG TIN CÁ NHÂN</div><h1>Lịch sử tiêm</h1>
      <p>Đây là lịch sử các mũi đã được ghi nhận trong hệ thống VAC.</p>
      <p>Thời gian hiển thị theo giờ địa phương của thiết bị.</p></div>
    {sessionBusy ? <p className="registration-message" role="status">Đang cập nhật phiên. Vui lòng chờ…</p>
      : account?.accountType === 'customer' ? <HistoryWorkspace key={account.accountId} go={go} />
      : <div className="registration-panel"><h2>Đăng nhập để xem lịch sử tiêm</h2><p>Dùng tài khoản khách hàng để xem các mũi thuộc hồ sơ bạn được phép truy cập.</p>
        <button className="button primary" onClick={() => go('account')}>Vào Tài khoản</button></div>}
  </section>
}

function HistoryWorkspace({ go }) {
  const [page, setPage] = useState(1)
  const [reload, setReload] = useState(0)
  const [list, setList] = useState({ loading: true, rows: [], error: null })
  const [selectedId, setSelectedId] = useState(null)
  const heading = useRef(null)
  const detailHeading = useRef(null)
  useEffect(() => {
    let current = true
    identity.vaccinationHistory(`?page=${page}&pageSize=25`)
      .then(response => { if (current) setList({ loading: false, rows: response.items, error: null }) })
      .catch(error => { if (current) setList({ loading: false, rows: [], error }) })
    return () => { current = false }
  }, [page, reload])
  function select(id) {
    setSelectedId(id)
    detailHeading.current?.focus({ preventScroll: true })
    detailHeading.current?.scrollIntoView({ block: 'start', behavior: 'instant' })
  }
  function load(nextPage = page) {
    setSelectedId(null); setList({ loading: true, rows: [], error: null })
    setPage(nextPage); setReload(value => value + 1)
    heading.current?.focus()
  }
  const selected = list.rows.find(row => row.id === selectedId)
  return <div className="payments-workspace">
    <section className="registration-panel" aria-labelledby="history-list-title" aria-busy={list.loading}>
      <h2 id="history-list-title" tabIndex={-1} ref={heading}>Các mũi đã tiêm</h2>
      <button className="button outline" disabled={list.loading} onClick={() => load()}><RefreshCw aria-hidden="true" />Tải lại danh sách</button>
      {list.loading && <p role="status">Đang tải lịch sử tiêm…</p>}
      {list.error && <div className="registration-message error" role="alert"><p>{historyError(list.error)}</p>
        {list.error.status === 401 ? <button className="button outline" onClick={() => go('account')}>Vào Tài khoản</button>
          : <button className="button outline" onClick={() => load()}>Thử lại</button>}</div>}
      {!list.loading && !list.error && <>
        {list.rows.length === 0 && <p role="status">Không có mũi tiêm được ghi nhận ở trang này.</p>}
        <ul className="payment-registration-list">{list.rows.map(row => <li key={row.id}>
          <button className="payment-select" aria-pressed={selectedId === row.id} aria-controls="history-detail" onClick={() => select(row.id)}>
            <strong>{vaccinationCode(row.id)} · Đã tiêm</strong><span>Người được tiêm: {row.recipient.fullName}</span>
            <span>Vắc xin: {row.vaccine.name}</span><time dateTime={row.administeredAt}>{vaccinationTime(row.administeredAt)}</time>
            <span className="payment-select-hint">Xem chi tiết</span>
          </button>
        </li>)}</ul>
        <nav className="registration-actions" aria-label="Trang lịch sử tiêm">
          <button className="button outline" disabled={page === 1} onClick={() => load(page - 1)}>Trang trước</button><span>Trang {page}</span>
          <button className="button outline" disabled={list.rows.length < 25} onClick={() => load(page + 1)}>Trang sau</button>
        </nav>
      </>}
    </section>
    <section id="history-detail" className="registration-panel" aria-label="Chi tiết mũi tiêm">
      <h2 ref={detailHeading} tabIndex={-1}>{selected ? `Mũi tiêm ${vaccinationCode(selected.id)}` : 'Chi tiết mũi tiêm'}</h2>
      {selected ? <dl className="payment-transaction">
        <div><dt>Người được tiêm</dt><dd>{selected.recipient.fullName}</dd></div>
        <div><dt>Vắc xin</dt><dd>{selected.vaccine.name}</dd></div>
        <div><dt>Thời gian (giờ địa phương)</dt><dd><time dateTime={selected.administeredAt}>{vaccinationTime(selected.administeredAt)}</time></dd></div>
        <div><dt>Mã lịch hẹn</dt><dd>LH-{String(selected.appointmentId).padStart(4, '0')}</dd></div>
        <div><dt>Số lô</dt><dd>{selected.lotNumber}</dd></div>
        <div><dt>Vị trí tiêm</dt><dd>{selected.injectionSite}</dd></div>
        <div><dt>Trạng thái</dt><dd>Đã tiêm</dd></div>
      </dl> : <p>Chọn một mũi để xem thông tin đã ghi nhận.</p>}
    </section>
  </div>
}
