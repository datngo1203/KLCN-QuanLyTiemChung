import { useCallback, useEffect, useRef, useState } from 'react'
import { ArrowLeft, ChevronRight, RefreshCw } from 'lucide-react'
import { identity } from './api/identity'
import { consultationFeeLabel, money } from './registrationForm'
import { paymentError, paymentMethodLabel, paymentOutcome, registrationCode } from './paymentView'
import PaymentCheckout from './PaymentCheckout'
import './Registrations.css'
import './Payments.css'

const dateTime = value => new Date(value).toLocaleString('vi-VN')

function ReadError({ error, retry, go }) {
  return <div className="registration-message error" role="alert">
    <p>{paymentError(error)}</p>
    {error.status === 401 ? <button className="button outline" onClick={() => go('account')}>Vào Tài khoản</button>
      : <button className="button outline" onClick={retry}>Thử lại</button>}
  </div>
}

export default function Payments({ account, sessionBusy, go }) {
  return <section className="registration-page payments-page checkout-page wrap">
    <div className="page-intro"><h1>Thanh toán dịch vụ</h1><p>Chọn đăng ký, thanh toán và xem lại biên nhận.</p></div>
    {sessionBusy ? <p className="registration-message" role="status">Đang cập nhật phiên. Vui lòng chờ…</p>
      : account?.accountType === 'customer' ? <PaymentWorkspace key={account.accountId} go={go} />
      : <div className="registration-panel"><h2>Đăng nhập tài khoản khách hàng</h2><p>Đăng nhập để xem đăng ký và kết quả thanh toán thuộc hồ sơ của bạn.</p><button className="button primary" onClick={() => go('account')}>Vào Tài khoản</button></div>}
  </section>
}

function PaymentWorkspace({ go }) {
  const [page, setPage] = useState(1)
  const [reload, setReload] = useState(0)
  const [list, setList] = useState({ loading: true, rows: [], error: null })
  const [selectedId, setSelectedId] = useState(null)
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
    <section className="registration-panel payment-list" hidden={selectedId !== null} aria-labelledby="payment-list-title" aria-busy={list.loading}>
      <h2 id="payment-list-title" ref={listHeading} tabIndex={-1}>Đăng ký của bạn</h2>
      <p>Chỉ hiển thị đăng ký thuộc hồ sơ bạn được phép xem.</p>
      <button className="button outline" disabled={list.loading} onClick={() => loadList()}><RefreshCw aria-hidden="true" />Tải lại danh sách</button>
      {list.loading && <p role="status">Đang tải đăng ký…</p>}
      {list.error && <ReadError error={list.error} retry={() => loadList()} go={go} />}
      {!list.loading && !list.error && <>
        {list.rows.length === 0 && <p role="status">Không có đăng ký ở trang này.</p>}
        <ul className="payment-registration-list">{list.rows.map(row => <li key={row.id}>
          <button className="payment-select" aria-pressed={selectedId === row.id} aria-controls="payment-detail" onClick={() => setSelectedId(row.id)}>
            <strong>{registrationCode(row.id)}</strong><span>Khách hàng: {row.customer.fullName}</span><span>Người được tiêm: {row.recipient.fullName}</span><small>{dateTime(row.createdAt)}</small><b>Tổng đăng ký: {money(row.totalBeforeDiscount)}</b><span className="payment-select-hint">Xem thanh toán</span>
          </button>
        </li>)}</ul>
        <nav className="registration-actions" aria-label="Trang đăng ký thanh toán"><button className="button outline" disabled={page === 1} onClick={() => loadList(page - 1)}>Trang trước</button><span>Trang {page}</span><button className="button outline" disabled={list.rows.length < 25} onClick={() => loadList(page + 1)}>Trang sau</button></nav>
      </>}
    </section>
    {selectedId !== null && <section id="payment-detail" aria-label="Chi tiết thanh toán">
      <button className="button text-button" onClick={() => { setSelectedId(null); requestAnimationFrame(() => listHeading.current?.focus()) }}>
        <ArrowLeft aria-hidden="true" size={16} /> Về danh sách đăng ký
      </button>
      <PaymentDetail key={selectedId} id={selectedId} accessDenied={accessDenied} go={go} />
    </section>}
  </div>
}

function PaymentDetail({ id, accessDenied, go }) {
  const [state, setState] = useState({ loading: true, detail: null, error: null })
  const [sending, setSending] = useState(false)
  const [message, setMessage] = useState('')
  const [chooseAgain, setChooseAgain] = useState(false)
  const [writeState, setWriteState] = useState(() => identity.paymentWriteState(id))
  const heading = useRef(null)
  const active = useRef(false)
  const inFlight = useRef(false)
  const resultHeading = useRef(null)

  const read = useCallback(async () => {
    try {
      const detail = await identity.getRegistrationPayment(id)
      if (!active.current) return
      setState({ loading: false, detail, error: null })
    } catch (error) {
      if (!active.current) return
      if ([401, 403].includes(error.status)) accessDenied(error)
      else setState(previous => ({ ...previous, loading: false, error }))
    }
    if (active.current) setWriteState(identity.paymentWriteState(id))
  }, [id, accessDenied])

  useEffect(() => {
    active.current = true
    heading.current?.focus({ preventScroll: true })
    heading.current?.scrollIntoView({ block: 'start', behavior: 'instant' })
    let current = true
    Promise.resolve().then(() => { if (current) return read() })
    return () => { current = false; active.current = false }
  }, [read])

  const { loading, detail, error } = state
  const center = detail?.expectedChannel === 'TaiTrungTam'
  const result = detail && (detail.payment || BigInt(detail.totalDue) === 0n || center && !chooseAgain)
  useEffect(() => {
    if (result) resultHeading.current?.focus()
  }, [result])

  function retry() {
    setState(previous => ({ ...previous, loading: true, error: null }))
    read()
  }

  async function submit(request) {
    if (inFlight.current || loading || error || writeState) return
    inFlight.current = true
    setSending(true)
    setMessage('')
    try {
      if (request) await identity.recordOnlinePayment(id, request)
      else await identity.selectCenterPayment(id)
      if (active.current && !request) setChooseAgain(false)
    } catch (failure) {
      if (!active.current) return
      if (failure.status === 403) accessDenied(failure)
      else if (failure.status === 401) {
        await read()
        if (active.current) setMessage('Phiên thanh toán vừa hết hạn. Kiểm tra thông tin rồi xác nhận lại giao dịch.')
      } else if (failure.status === 400) setMessage('Vui lòng kiểm tra thông tin và xác nhận giao dịch trước khi gửi lại.')
    } finally {
      if (active.current) {
        const next = identity.paymentWriteState(id)
        setWriteState(next)
        if (next && next !== 'conflict') await read()
        if (active.current) setSending(false)
      }
      inFlight.current = false
    }
  }

  const blocked = writeState === 'conflict' || error?.status === 409
  return <div className="payment-detail" aria-busy={loading || sending}>
    <h2 tabIndex={-1} ref={heading}>Thanh toán {registrationCode(id)}</h2>
    <ol className="registration-steps" aria-label="Các bước thanh toán">
      {['Đăng ký', 'Thanh toán', 'Xác nhận'].map((label, index) => <li key={label} aria-current={index === (result ? 2 : 1) ? 'step' : undefined}>
        {index > 0 && <ChevronRight aria-hidden="true" size={14} />}<span>{index + 1}</span>{label}
      </li>)}
    </ol>
    <button className="button outline payment-reload" disabled={loading || sending} onClick={retry}><RefreshCw aria-hidden="true" size={16} />Tải lại kết quả</button>
    {loading && <p role="status">Đang đọc kết quả thanh toán…</p>}
    {sending && <p role="status">Đang xử lý và đối chiếu kết quả thanh toán…</p>}
    {blocked ? <p className="registration-message error" role="alert">{paymentError({ status: 409 })}</p>
      : error && <ReadError error={error} retry={retry} go={go} />}
    {!sending && !blocked && writeState && <div className="registration-message warning" role="status">
      {['unknown', 'unconfirmed'].includes(writeState)
        ? <p>Chưa xác định được kết quả giao dịch. Hãy đối chiếu trước khi thử lại.</p>
        : <p>Đang chờ kết quả đối chiếu. Hãy tải lại kết quả để kiểm tra.</p>}
      {writeState === 'unconfirmed' && !error && !loading && <button className="button outline" type="button" onClick={() => {
        identity.acknowledgePaymentRetry(id); setWriteState(identity.paymentWriteState(id))
      }}>Tôi đã kiểm tra và muốn thử lại</button>}
    </div>}
    {result ? <div className="payment-result-layout">
      <PaymentResult detail={detail} heading={resultHeading} onChoose={() => setChooseAgain(true)} disabled={loading || sending || blocked || !!error || !!writeState} />
      <PaymentSummary detail={detail} />
    </div> : detail && <PaymentCheckout detail={detail} summary={<PaymentSummary detail={detail} />}
      disabled={loading || sending || !!error || !!writeState} sending={sending} message={message}
      onPayment={submit} onCenter={() => submit()} />}
  </div>
}

function PaymentSummary({ detail }) {
  return <aside className="registration-panel payment-summary" aria-labelledby="payment-summary-title">
      <h3 id="payment-summary-title">Tóm tắt đăng ký</h3>
      <span className="registration-status">{registrationCode(detail.registrationId)}</span>
      <dl className="registration-review-profiles"><div><dt>Khách hàng</dt><dd>{detail.customer.fullName}</dd></div><div><dt>Người được tiêm</dt><dd>{detail.recipient.fullName}</dd></div></dl>
      <h3>Dịch vụ đã đăng ký</h3>
      <ul className="registration-lines">{detail.items.map(item => <li key={`${item.kind}-${item.id}`}>
        <div><small>{item.kind === 'package' ? 'Gói vắc xin' : 'Vắc xin lẻ'}</small><h3>{item.name}</h3><p>Đơn giá lúc đăng ký: {money(item.unitPrice)}</p>
          {item.components?.length > 0 && <details className="registration-components"><summary>Thành phần trong một gói</summary><ul>{item.components.map(part => <li key={part.vaccineId}>{part.vaccineName} × {part.quantity}</li>)}</ul></details>}
        </div><div className="registration-line-amount"><span>Số lượng: {item.quantity}</span><strong>{money(item.lineTotal)}</strong></div>
      </li>)}</ul>
      <dl className="registration-review-profiles">
        <div><dt>Tạm tính dịch vụ</dt><dd>{money(detail.subtotal)}</dd></div>
        <div><dt>Phí khám/tư vấn</dt><dd>{consultationFeeLabel(detail.consultationFee)}</dd></div>
      </dl>
      <div className="registration-total"><span>Tổng phải thu</span><strong>{money(detail.totalDue)}</strong></div>
  </aside>
}

function PaymentResult({ detail, heading, onChoose, disabled }) {
  const { payment, paymentState } = detail
  const pending = paymentState === 'ChoXacNhan'
  const center = !payment && detail.expectedChannel === 'TaiTrungTam' && BigInt(detail.totalDue) > 0n
  return <section className="registration-panel payment-result">
    <h3 ref={heading} tabIndex={-1}>{payment ? (pending ? 'Thông tin chuyển khoản' : 'Biên nhận thanh toán')
      : center ? 'Đã chọn thanh toán tại trung tâm' : 'Kết quả thanh toán'}</h3>
    <div className={`registration-message${pending || center ? ' warning' : ''}`} role="status">
      <strong>{paymentOutcome(detail)}</strong>
      {pending && <p>Thu ngân cần đối chiếu khoản chuyển. Vui lòng tải lại kết quả để xem cập nhật.</p>}
      {center && <p>Kênh dự kiến: Tại trung tâm. Vui lòng cung cấp mã {registrationCode(detail.registrationId)} tại quầy Thu ngân.</p>}
    </div>
    {payment && <dl className="payment-transaction">
      <div><dt>Mã giao dịch</dt><dd>{payment.transactionCode ?? `TT-${String(payment.id).padStart(4, '0')}`}</dd></div>
      <div><dt>Mã đăng ký</dt><dd>{registrationCode(detail.registrationId)}</dd></div>
      <div><dt>Người được tiêm</dt><dd>{detail.recipient.fullName}</dd></div>
      {payment.payer && <div><dt>Người thanh toán</dt><dd>{payment.payer.fullName}<br />{payment.payer.phoneNumber}</dd></div>}
      <div><dt>{pending ? 'Số tiền chuyển khoản' : 'Số tiền đã thu'}</dt><dd>{money(payment.amount)}</dd></div>
      <div><dt>Thời điểm {pending ? 'gửi đối chiếu' : 'thanh toán'}</dt><dd><time dateTime={payment.occurredAt}>{dateTime(payment.occurredAt)}</time></dd></div>
      <div><dt>Phương thức</dt><dd>{paymentMethodLabel(payment.method)}</dd></div>
      {payment.provider && <div><dt>Ngân hàng / loại thẻ</dt><dd>{payment.provider}</dd></div>}
      {payment.holderName && <div><dt>Chủ tài khoản / thẻ</dt><dd>{payment.holderName}</dd></div>}
      {payment.maskedIdentifier && <div><dt>Định danh giao dịch</dt><dd>{payment.maskedIdentifier}</dd></div>}
      {payment.reference && <div><dt>Mã tham chiếu</dt><dd>{payment.reference}</dd></div>}
    </dl>}
    {center && <button className="button outline" disabled={disabled} onClick={onChoose}>Chọn phương thức khác</button>}
  </section>
}
