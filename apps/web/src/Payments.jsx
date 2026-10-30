import { useCallback, useEffect, useRef, useState } from 'react'
import { RefreshCw } from 'lucide-react'
import { identity } from './api/identity'
import { money } from './registrationForm'
import { paymentError, paymentOutcome } from './paymentView'
import './Registrations.css'
import './Payments.css'

const dateTime = value => new Date(value).toLocaleString('vi-VN')
const registrationCode = id => `DK-${String(id).padStart(4, '0')}`

function ReadError({ error, retry, go }) {
  return <div className="registration-message error" role="alert">
    <p>{paymentError(error)}</p>
    {error.status === 401 ? <button className="button outline" onClick={() => go('account')}>Vào Tài khoản</button>
      : <button className="button outline" onClick={retry}>Thử lại</button>}
  </div>
}

export default function Payments({ account, go }) {
  return <section className="registration-page payments-page wrap">
    <div className="page-intro"><div className="eyebrow">KẾT QUẢ TỪ QUẦY THU NGÂN</div><h1>Thanh toán dịch vụ</h1><p>Xem kết quả Thu ngân đã ghi nhận cho các đăng ký thuộc hồ sơ của bạn.</p></div>
    <p className="registration-message warning">Thanh toán mô phỏng — Không xác nhận tiền thật</p>
    {account?.accountType === 'customer' ? <PaymentWorkspace key={account.accountId} go={go} />
      : <div className="registration-panel"><h2>Đăng nhập tài khoản khách hàng</h2><p>Đăng nhập để xem đăng ký và kết quả thanh toán thuộc hồ sơ của bạn.</p><button className="button primary" onClick={() => go('account')}>Vào Tài khoản</button></div>}
  </section>
}

function PaymentWorkspace({ go }) {
  const [page, setPage] = useState(1)
  const [reload, setReload] = useState(0)
  const [list, setList] = useState({ loading: true, rows: [], error: null })
  const [selectedId, setSelectedId] = useState(null)
  const [detailReload, setDetailReload] = useState(0)
  const listHeading = useRef(null)

  useEffect(() => {
    let current = true
    identity.registrations(`?page=${page}&pageSize=25`)
      .then(response => { if (current) setList({ loading: false, rows: response.items, error: null }) })
      .catch(error => { if (current) setList({ loading: false, rows: [], error }) })
    return () => { current = false }
  }, [page, reload])

  function loadList(nextPage = page) {
    setSelectedId(null); setList({ loading: true, rows: [], error: null })
    setPage(nextPage); setReload(value => value + 1)
    listHeading.current?.focus()
  }
  const accessDenied = useCallback(error => {
    setSelectedId(null); setList({ loading: false, rows: [], error })
    listHeading.current?.focus()
  }, [])

  return <div className="payments-workspace">
    <section className="registration-panel payment-list" aria-labelledby="payment-list-title" aria-busy={list.loading}>
      <h2 id="payment-list-title" ref={listHeading} tabIndex={-1}>Đăng ký của bạn</h2>
      <p>Chỉ hiển thị đăng ký thuộc hồ sơ bạn được phép xem.</p>
      <button className="button outline" disabled={list.loading} onClick={() => loadList()}><RefreshCw aria-hidden="true" />Tải lại danh sách</button>
      {list.loading && <p role="status">Đang tải đăng ký…</p>}
      {list.error && <ReadError error={list.error} retry={() => loadList()} go={go} />}
      {!list.loading && !list.error && <>
        {list.rows.length === 0 && <p role="status">Không có đăng ký ở trang này.</p>}
        <ul className="payment-registration-list">{list.rows.map(row => <li key={row.id}>
          <button className="payment-select" aria-pressed={selectedId === row.id} aria-controls="payment-detail" onClick={() => setSelectedId(row.id)}>
            <strong>{registrationCode(row.id)}</strong><span>Khách hàng: {row.customer.fullName}</span><span>Người được tiêm: {row.recipient.fullName}</span><small>{dateTime(row.createdAt)}</small><b>Tổng đăng ký: {money(row.subtotal)}</b><span className="payment-select-hint">Xem thanh toán</span>
          </button>
        </li>)}</ul>
        <nav className="registration-actions" aria-label="Trang đăng ký thanh toán"><button className="button outline" disabled={page === 1} onClick={() => loadList(page - 1)}>Trang trước</button><span>Trang {page}</span><button className="button outline" disabled={list.rows.length < 25} onClick={() => loadList(page + 1)}>Trang sau</button></nav>
      </>}
    </section>
    <section id="payment-detail" className="registration-panel" aria-label="Chi tiết thanh toán">
      {selectedId === null ? <><h2>Chi tiết thanh toán</h2><p>Chọn một đăng ký để xem dịch vụ, giá đã chốt và giao dịch đã được ghi nhận.</p></>
        : <PaymentDetail key={`${selectedId}-${detailReload}`} id={selectedId} retry={() => setDetailReload(value => value + 1)} accessDenied={accessDenied} go={go} />}
    </section>
  </div>
}

function PaymentDetail({ id, retry, accessDenied, go }) {
  const [state, setState] = useState({ loading: true, detail: null, error: null })
  const heading = useRef(null)
  useEffect(() => {
    heading.current?.focus({ preventScroll: true })
    heading.current?.scrollIntoView({ block: 'start', behavior: 'instant' })
    let current = true
    identity.getRegistrationPayment(id)
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
    <h2 tabIndex={-1} ref={heading}>Thanh toán {registrationCode(id)}</h2>
    <button className="button outline" disabled={loading} onClick={retry}><RefreshCw aria-hidden="true" />Tải lại kết quả</button>
    {loading && <p role="status">Đang đọc kết quả thanh toán…</p>}
    {error && <ReadError error={error} retry={retry} go={go} />}
    {detail && <>
      <dl className="registration-review-profiles"><div><dt>Khách hàng</dt><dd>{detail.customer.fullName}</dd></div><div><dt>Người được tiêm</dt><dd>{detail.recipient.fullName}</dd></div></dl>
      <h3>Dịch vụ đã đăng ký</h3>
      <ul className="registration-lines">{detail.items.map(item => <li key={`${item.kind}-${item.id}`}>
        <div><small>{item.kind === 'package' ? 'Gói vắc xin' : 'Vắc xin lẻ'}</small><h3>{item.name}</h3><p>Đơn giá lúc đăng ký: {money(item.unitPrice)}</p>
          {item.components?.length > 0 && <details className="registration-components"><summary>Thành phần trong một gói</summary><ul>{item.components.map(part => <li key={part.vaccineId}>{part.vaccineName} × {part.quantity}</li>)}</ul></details>}
        </div><div className="registration-line-amount"><span>Số lượng: {item.quantity}</span><strong>{money(item.lineTotal)}</strong></div>
      </li>)}</ul>
      <div className="registration-total"><span>Tổng phải thu (VND)</span><strong>{money(detail.totalDue)}</strong></div>
      <div className="registration-message" role="status"><strong>{paymentOutcome(detail)}</strong>
        {detail.payment ? <p>Giao dịch do Thu ngân ghi nhận để mô phỏng, không xác nhận tiền thật.</p>
          : BigInt(detail.totalDue) > 0n && <p>Vui lòng liên hệ quầy Thu ngân để ghi nhận thanh toán. Bạn có thể tải lại kết quả sau khi Thu ngân hoàn tất.</p>}
      </div>
      {detail.payment && <dl className="payment-transaction">
        <div><dt>Mã giao dịch</dt><dd>TT-{String(detail.payment.id).padStart(4, '0')}</dd></div>
        <div><dt>Số tiền đã ghi nhận</dt><dd>{money(detail.payment.amount)}</dd></div>
        <div><dt>Thời điểm ghi nhận (giờ địa phương)</dt><dd><time dateTime={detail.payment.occurredAt}>{dateTime(detail.payment.occurredAt)}</time></dd></div>
        <div><dt>Phương thức</dt><dd>Mô phỏng — Không xác nhận tiền thật</dd></div>
      </dl>}
      <p className="muted">Tải lại kết quả để xem cập nhật mới nhất. Khi tải lại trang, bạn cần đăng nhập lại để xem kết quả.</p>
    </>}
  </div>
}
