import { useEffect, useRef, useState } from 'react'
import { ArrowLeft, ArrowRight, Plus, RefreshCw, Search } from 'lucide-react'
import { identity } from './api/identity'
import { getRegistrationCatalog } from './api/client'
import { allProfiles, money, total, serviceKey, registrationPayload, refreshDraft, registrationError } from './registrationForm'
import './Registrations.css'

const emptyDraft = () => ({ customerId: '', recipientId: '', items: [] })
const dateTime = value => new Date(value).toLocaleString('vi-VN')
const kindName = kind => kind === 'package' ? 'Gói vắc xin' : 'Vắc xin lẻ'

function Components({ items }) {
  return items?.length ? <details className="registration-components"><summary>Thành phần trong một gói ({items.length})</summary><ul>{items.map(item => <li key={item.vaccineId}>{item.vaccineName} × {item.quantity}</li>)}</ul></details> : null
}

function Lines({ items, onChange, onRemove }) {
  return <ul className="registration-lines">{items.map(item => <li key={serviceKey(item)}>
    <div><small>{kindName(item.kind)}</small><h3>{item.name}</h3><p>Đơn giá: {money(item.price ?? item.unitPrice)}</p><Components items={item.components} />{item.unavailable && <p role="alert">Dịch vụ không còn khả dụng. Hãy bỏ mục này.</p>}</div>
    <div className="registration-line-amount">{onChange ? <label>Số lượng<input aria-label={`Số lượng ${item.name}`} type="number" min="1" max="100" step="1" required value={item.quantity} onChange={event => onChange(item, event.target.value)} /></label> : <span>Số lượng: {item.quantity}</span>}<strong>{money(BigInt(item.price ?? item.unitPrice) * BigInt(item.quantity || 0))}</strong>{onRemove && <button className="button text-button" type="button" onClick={() => onRemove(item)} aria-label={`Bỏ ${item.name}`}>Bỏ dịch vụ</button>}</div>
  </li>)}</ul>
}

export default function Registrations({ account, active, go, onSavingChange }) {
  const [view, setView] = useState('list')
  const [step, setStep] = useState(0)
  const [rows, setRows] = useState([])
  const [page, setPage] = useState(1)
  const [reload, setReload] = useState(0)
  const [listLoading, setListLoading] = useState(false)
  const [listReady, setListReady] = useState(false)
  const [listError, setListError] = useState('')
  const [customers, setCustomers] = useState([])
  const [recipients, setRecipients] = useState([])
  const [catalog, setCatalog] = useState([])
  const [draft, setDraft] = useState(emptyDraft)
  const [draftOpen, setDraftOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [kind, setKind] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [changes, setChanges] = useState([])
  const [needsCatalog, setNeedsCatalog] = useState(false)
  const [uncertain, setUncertain] = useState(false)
  const [checkedList, setCheckedList] = useState(false)
  const [detail, setDetail] = useState(null)
  const [detailId, setDetailId] = useState(null)
  const pending = useRef(false)
  const heading = useRef(null)

  useEffect(() => {
    if (active) { heading.current?.focus({ preventScroll: true }); window.scrollTo({ top: 0, behavior: 'instant' }) }
  }, [active, view, step])

  useEffect(() => {
    if (!account || !active || view !== 'list') return
    let current = true
    async function load() {
      await Promise.resolve()
      if (!current) return
      setListLoading(true); setListReady(false); setListError('')
      try {
        const response = await identity.registrations(`?page=${page}&pageSize=25`)
        if (current) { setRows(response.items); setListReady(true); setCheckedList(true) }
      } catch (failure) { if (current) setListError(registrationError(failure)) }
      finally { if (current) setListLoading(false) }
    }
    load()
    return () => { current = false }
  }, [account, active, view, page, reload])

  async function run(action) {
    if (pending.current) return
    pending.current = true; setBusy(true); setError('')
    try { await action() } catch (failure) { setError(registrationError(failure)) }
    finally { pending.current = false; setBusy(false) }
  }

  function start() {
    run(async () => {
      const [owners, people, services] = await Promise.all([allProfiles(identity, 'customers'), allProfiles(identity, 'recipients'), getRegistrationCatalog()])
      setCustomers(owners); setRecipients(people); setCatalog(services)
      setDraft(emptyDraft()); setDraftOpen(true); setDetail(null); setNotice(''); setChanges([]); setNeedsCatalog(false)
      setStep(0); setQuery(''); setKind(''); setView('form')
    })
  }

  function showList() { setView('list'); setPage(1); setReload(value => value + 1); setError('') }

  function reloadProfiles() {
    run(async () => {
      const [owners, people] = await Promise.all([allProfiles(identity, 'customers'), allProfiles(identity, 'recipients')])
      setCustomers(owners); setRecipients(people)
      setDraft(current => {
        const owner = owners.find(item => item.id === Number(current.customerId))
        const person = people.find(item => item.id === Number(current.recipientId) && item.customerId === owner?.id)
        return { ...current, customerId: owner?.id || '', recipientId: person?.id || '' }
      })
      setNotice('Đã tải lại hồ sơ. Chọn khách hàng và người được tiêm phù hợp để tiếp tục.')
    })
  }

  async function readDetail(id) {
    setDetailId(id); setDetail(null); setView('detail')
    setDetail(await identity.registrations(`/${id}`))
  }

  async function updateCatalog() {
    const services = await getRegistrationCatalog()
    const updated = refreshDraft(draft.items, services)
    setCatalog(services); setDraft(current => ({ ...current, items: updated.items })); setChanges(updated.changes)
    setNeedsCatalog(false)
    setNotice('Danh mục đã được tải lại. Kiểm tra giá, thành phần từng mục rồi chọn Xem lại để xác nhận lần nữa.')
  }

  async function submit() {
    if (pending.current || uncertain || needsCatalog) return
    let payload
    try { payload = registrationPayload(draft, customers, recipients) } catch (failure) { setError(failure.message); return }
    await run(async () => {
      onSavingChange(true)
      let saved
      try {
        saved = await identity.registrations('', 'POST', payload)
      } catch (failure) {
        if (failure.code === 'catalogChanged') {
          setNeedsCatalog(true); setStep(1)
          setNotice('Giá hoặc thành phần đã thay đổi. Cần tải lại danh mục, xem lại và xác nhận lần nữa.')
          await updateCatalog()
        } else if (failure.uncertain || !failure.status || failure.status >= 500) {
          setUncertain(true); setCheckedList(false)
          setNotice('Chưa xác định đăng ký đã được lưu hay chưa. Không gửi lại ngay; kiểm tra danh sách và chi tiết trước để tránh đăng ký trùng.')
        } else throw failure
        return
      } finally { onSavingChange(false) }
      setNotice(`Đã tạo đăng ký #${saved.id}. Giá và số lượng đã được lưu; chưa phát sinh lịch hoặc thanh toán.`)
      setDraftOpen(false); setDraft(emptyDraft()); setChanges([])
      await readDetail(saved.id)
    })
  }

  if (!account) return <section className="registration-page wrap"><div className="page-intro"><div className="eyebrow">ĐĂNG KÝ DỊCH VỤ</div><h1>Đăng ký tiêm chủng</h1><p>Đăng nhập bằng tài khoản khách hàng để tạo và xem lại đăng ký của bạn.</p></div><button className="button primary" onClick={() => go('account')}>Đăng nhập <ArrowRight aria-hidden="true" /></button></section>

  const customer = customers.find(item => item.id === Number(draft.customerId))
  const recipient = recipients.find(item => item.id === Number(draft.recipientId) && item.customerId === Number(draft.customerId))
  const people = recipients.filter(item => item.customerId === Number(draft.customerId))
  const filtered = catalog.filter(item => (!kind || item.kind === kind) && `${item.name} ${item.description || ''} ${(item.components || []).map(x => x.vaccineName).join(' ')}`.toLocaleLowerCase('vi').includes(query.trim().toLocaleLowerCase('vi')))
  const title = view === 'list' ? 'Đăng ký của tôi' : view === 'detail' ? `Chi tiết đăng ký #${detailId}` : ['Chọn hồ sơ', 'Chọn dịch vụ', 'Xem lại đăng ký'][step]

  return <section className="registration-page wrap" aria-busy={busy}>
    <div className="page-intro"><div className="eyebrow">ĐĂNG KÝ DỊCH VỤ</div><h1 ref={heading} tabIndex={-1}>{title}</h1><p>Chọn dịch vụ và lưu đăng ký cho người được tiêm trong hồ sơ của bạn. Các mục [DEMO] có dữ liệu và giá giả để trình diễn.</p></div>
    {notice && <div className={`registration-message ${uncertain ? 'warning' : ''}`} role="status">{notice}</div>}
    {uncertain && <div className="registration-actions"><button className="button outline" disabled={busy || listLoading} onClick={showList}>Kiểm tra danh sách đăng ký</button>{view === 'list' && checkedList && listReady && <button className="button outline" disabled={busy || listLoading} onClick={() => {
      if (window.confirm('Bạn đã kiểm tra các đăng ký mới nhất và chi tiết, nhưng chưa thấy đăng ký vừa gửi? Gửi lại có thể tạo đăng ký trùng nếu kết quả cũ đến chậm.')) { setUncertain(false); setView('form'); setStep(2); setNotice('Bản nháp được mở lại. Hãy xem kỹ trước khi xác nhận gửi lại.') }
    }}>Đã kiểm tra, mở lại bản nháp</button>}</div>}
    {error && view !== 'form' && <p className="registration-message error" role="alert">{error}</p>}

    {view === 'list' && <>
      {draftOpen && !uncertain && <button className="button outline" disabled={busy} onClick={() => {
        if (window.confirm('Bỏ bản nháp chưa gửi và chọn lại hồ sơ/dịch vụ?')) { setDraft(emptyDraft()); setDraftOpen(false); setChanges([]); setNeedsCatalog(false); setNotice('Đã bỏ bản nháp chưa gửi.') }
      }}>Bỏ bản nháp</button>}
      <div className="registration-actions"><button className="button primary" disabled={busy || uncertain || draftOpen} onClick={start}><Plus aria-hidden="true" />{busy ? 'Đang tải hồ sơ và dịch vụ…' : 'Tạo đăng ký'}</button>{draftOpen && !uncertain && <button className="button outline" disabled={busy} onClick={() => { setError(''); setView('form') }}>Tiếp tục bản nháp</button>}<button className="button outline" disabled={busy || listLoading} onClick={() => setReload(value => value + 1)}><RefreshCw aria-hidden="true" />Tải lại</button></div>
      {listLoading && <p role="status">Đang tải danh sách đăng ký…</p>}
      {listError && <p className="registration-message error" role="alert">{listError} <button className="button outline" onClick={() => setReload(value => value + 1)}>Thử lại</button></p>}
      {!listLoading && listReady && <><div className="registration-list">{rows.map(row => <article className="registration-record" key={row.id}><div><small>ĐĂNG KÝ #{row.id} · {dateTime(row.createdAt)}</small><h2>{row.recipient.fullName}</h2><p>Khách hàng: {row.customer.fullName}</p><span className="registration-status">{row.status === 'DaDangKy' ? 'Đã đăng ký' : row.status}</span></div><div><strong>{money(row.subtotal)}</strong><button className="button outline" disabled={busy} onClick={() => run(() => readDetail(row.id))}>Xem chi tiết #{row.id}</button></div></article>)}</div>{rows.length === 0 && <div className="empty-state">{page === 1 ? 'Bạn chưa có đăng ký nào. Chọn Tạo đăng ký để bắt đầu.' : 'Không còn đăng ký trên trang này.'}</div>}<nav className="registration-actions" aria-label="Trang danh sách đăng ký"><button className="button outline" disabled={busy || page === 1} onClick={() => setPage(value => value - 1)}>Trang trước</button><span>Trang {page}</span><button className="button outline" disabled={busy || rows.length < 25} onClick={() => setPage(value => value + 1)}>Trang sau</button></nav></>}
    </>}

    {view === 'detail' && <>
      <div className="registration-actions"><button className="button outline" disabled={busy} onClick={showList}><ArrowLeft aria-hidden="true" />Danh sách đăng ký</button>{!detail && !busy && <button className="button primary" onClick={() => run(() => readDetail(detailId))}>Thử tải lại chi tiết</button>}</div>
      {busy && <p role="status">Đang tải chi tiết từ hệ thống…</p>}
      {uncertain && detail && <button className="button primary" disabled={busy} onClick={() => {
        if (window.confirm('Bạn xác nhận đây là đăng ký vừa gửi? Bản nháp sẽ được bỏ, không gửi thêm đăng ký.')) {
          setUncertain(false); setDraftOpen(false); setDraft(emptyDraft()); setChanges([]); setNeedsCatalog(false)
          setNotice(`Bạn đã xác nhận tìm thấy đăng ký #${detail.id}. Bản nháp được kết thúc, không gửi thêm yêu cầu tạo.`)
        }
      }}>Đã tìm thấy đăng ký, kết thúc bản nháp</button>}
      {detail && <div className="registration-panel"><div className="registration-detail-info"><div><small>Người được tiêm</small><h2>{detail.recipient.fullName}</h2><p>Khách hàng: {detail.customer.fullName}</p></div><div><span className="registration-status">{detail.status === 'DaDangKy' ? 'Đã đăng ký' : detail.status}</span><p>{dateTime(detail.createdAt)}</p></div></div><Lines items={detail.items} /><div className="registration-total"><span>Tổng tiền đã chốt</span><strong>{money(detail.subtotal)}</strong></div><p className="muted">Đăng ký đã được lưu. Chưa phát sinh lịch tiêm hoặc thanh toán trong luồng này; đăng ký không xác nhận đủ điều kiện tiêm.</p></div>}
    </>}

    {view === 'form' && <>
      <ol className="registration-steps" aria-label="Các bước đăng ký">{['Hồ sơ', 'Dịch vụ', 'Xem lại'].map((label, index) => <li key={label} aria-current={step === index ? 'step' : undefined}><span>{index + 1}</span>{label}</li>)}</ol>
      <form onSubmit={event => { event.preventDefault(); if (step === 0) { if (!customer || !recipient) setError('Chọn đầy đủ khách hàng và người được tiêm.'); else { setError(''); setStep(1) } } else if (step === 1) { try { registrationPayload(draft, customers, recipients); setError(''); setStep(2) } catch (failure) { setError(failure.message) } } else submit() }}>
        <fieldset className="registration-fieldset" disabled={busy || uncertain}>
          {step === 0 && <div className="registration-panel registration-profile-select"><h2>Đăng ký cho ai?</h2><p>Chọn hồ sơ đã lưu, không cần nhập lại thông tin cá nhân.</p><label className="demo-field"><span>Khách hàng *</span><select required value={draft.customerId} onChange={event => setDraft(current => ({ ...current, customerId: event.target.value, recipientId: '' }))}><option value="">Chọn khách hàng</option>{customers.map(item => <option key={item.id} value={item.id}>{item.fullName} · #{item.id}</option>)}</select></label><label className="demo-field"><span>Người được tiêm *</span><select required disabled={!customer} value={draft.recipientId} onChange={event => setDraft(current => ({ ...current, recipientId: event.target.value }))}><option value="">Chọn người được tiêm</option>{people.map(item => <option key={item.id} value={item.id}>{item.fullName} · {item.dateOfBirth} · {item.relationshipToCustomer} · #{item.id}</option>)}</select></label>{(!customers.length || (customer && !people.length)) && <p role="status">Chưa có hồ sơ phù hợp trong phạm vi tài khoản.</p>}<div className="registration-actions"><button className="button outline" type="button" onClick={reloadProfiles}>Tải lại hồ sơ</button><button className="button text-button" type="button" onClick={() => go('account')}>Mở Hồ sơ để thêm hoặc kiểm tra liên kết</button></div></div>}

          {step === 1 && <div className="registration-workspace"><div className="registration-panel"><h2>Danh mục dịch vụ</h2><p className="muted">Danh mục từ cơ sở dữ liệu; không xác nhận tồn kho hoặc chỉ định y khoa.</p><label className="registration-search"><Search aria-hidden="true" /><input type="search" aria-label="Tìm dịch vụ" placeholder="Tìm tên vắc xin, gói hoặc thành phần" maxLength={150} value={query} onChange={event => setQuery(event.target.value)} /></label><div className="registration-actions">{[['', 'Tất cả'], ['vaccine', 'Vắc xin lẻ'], ['package', 'Gói vắc xin']].map(([value, label]) => <button key={value} className={`filter-tab ${kind === value ? 'selected' : ''}`} aria-pressed={kind === value} type="button" onClick={() => setKind(value)}>{label}</button>)}</div><p className="muted">{filtered.length} dịch vụ</p><ul className="registration-services">{filtered.map(item => { const selected = draft.items.some(x => serviceKey(x) === serviceKey(item)); return <li key={serviceKey(item)}><small>{kindName(item.kind)}</small><h3>{item.name}</h3>{item.description && <p>{item.description}</p>}<Components items={item.components} /><div className="registration-service-action"><strong>{money(item.price)}</strong><button type="button" className="button outline" disabled={selected || draft.items.length >= 100} aria-label={`${selected ? 'Đã chọn' : 'Chọn'} ${item.name}`} onClick={() => setDraft(current => ({ ...current, items: [...current.items, { ...item, quantity: 1 }] }))}>{selected ? 'Đã chọn' : 'Chọn'}{!selected && <Plus aria-hidden="true" />}</button></div></li> })}</ul>{!filtered.length && <div className="empty-state">Không có dịch vụ phù hợp. Thử đổi từ khóa hoặc loại dịch vụ.</div>}</div>
            <aside className="registration-panel registration-summary"><h2>Dịch vụ đã chọn</h2><p>{recipient?.fullName} · {customer?.fullName}</p>{!draft.items.length && <p>Chọn dịch vụ từ danh mục để bắt đầu.</p>}<Lines items={draft.items} onChange={(item, quantity) => { if (quantity === '' || /^\d{1,3}$/.test(quantity)) setDraft(current => ({ ...current, items: current.items.map(x => serviceKey(x) === serviceKey(item) ? { ...x, quantity } : x) })) }} onRemove={item => setDraft(current => ({ ...current, items: current.items.filter(x => serviceKey(x) !== serviceKey(item)) }))} /><div className="registration-total"><span>Tổng tạm tính</span><strong>{money(total(draft.items))}</strong></div></aside></div>}

          {changes.length > 0 && step > 0 && <div className="registration-message warning" role="alert"><strong>Các mục đã thay đổi</strong><ul>{changes.map(change => <li key={change}>{change}</li>)}</ul></div>}
          {needsCatalog && <button className="button outline" type="button" onClick={() => run(updateCatalog)}>Thử tải lại giá và thành phần</button>}
          {error && <p className="registration-message error" role="alert">{error}</p>}
          {step === 2 && <div className="registration-panel"><h2>Kiểm tra trước khi xác nhận</h2><dl className="registration-review-profiles"><div><dt>Khách hàng</dt><dd>{customer?.fullName} · #{customer?.id}</dd></div><div><dt>Người được tiêm</dt><dd>{recipient?.fullName} · #{recipient?.id}</dd></div></dl><Lines items={draft.items} /><div className="registration-total"><span>Tổng tiền đăng ký</span><strong>{money(total(draft.items))}</strong></div><p>Chỉ xác nhận đăng ký dịch vụ. Nhân viên sẽ xử lý các bước tiếp theo theo điều kiện nghiệp vụ; chưa tạo lịch hay thanh toán.</p></div>}
          <div className="registration-actions registration-form-actions"><button className="button outline" type="button" onClick={showList}>Lưu bản nháp trong phiên, về danh sách</button>{step > 0 && <button className="button outline" type="button" onClick={() => { setStep(value => value - 1); setError('') }}><ArrowLeft aria-hidden="true" />Quay lại</button>}<button className="button primary" type="submit" disabled={needsCatalog || (step === 0 && (!customer || !recipient)) || (step > 0 && !draft.items.length)}>{busy ? 'Đang gửi đăng ký…' : step === 2 ? 'Xác nhận đăng ký' : step === 1 ? 'Xem lại' : 'Chọn dịch vụ'}<ArrowRight aria-hidden="true" /></button></div>
        </fieldset>
      </form>
      {busy && <p role="status">Đang xử lý. Vui lòng chờ và không đóng trang.</p>}
    </>}
  </section>
}
