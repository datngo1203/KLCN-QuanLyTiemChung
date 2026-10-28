import { useEffect, useRef, useState } from 'react'
import { Plus, Pencil, Trash2, RefreshCw, X, ArrowRight } from 'lucide-react'
import { identity } from './api/identity'
import { emptyCustomer, emptyRecipient, profileBody, profileError } from './profileForm'
import './Profiles.css'

async function list(resource) {
  const items = []
  for (let page = 1; ; page++) {
    const response = await identity.profiles(`${resource}?page=${page}&pageSize=100`)
    items.push(...response.items)
    if (response.items.length < 100) return items
  }
}

const birthDate = value => value ? value.split('-').reverse().join('/') : 'Chưa cập nhật'
const today = () => new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date())

function ProfileDetails({ value, recipient = false }) {
  const details = recipient
    ? [['Họ và tên', value.fullName], ['Ngày sinh', birthDate(value.dateOfBirth)], ['Giới tính', value.gender], ['Quan hệ với khách hàng', value.relationshipToCustomer], ['CCCD', value.identityNumber]]
    : [['Họ và tên', value.fullName], ['Số điện thoại', value.phoneNumber], ['Email', value.email], ['CCCD', value.identityNumber]]
  return <dl className="profile-details">{details.map(([label, text]) => <div key={label}><dt>{label}</dt><dd>{text || 'Chưa cập nhật'}</dd></div>)}</dl>
}

function ProfileDialog({ editor, setEditor, customers, busy, error, onClose, onSave, onDelete, canUpdate }) {
  const dialog = useRef(null)
  const isRecipient = editor.resource === 'recipients'
  const viewing = editor.mode === 'view'
  const title = viewing ? 'Thông tin người được tiêm' : `${editor.draft.id ? 'Chỉnh sửa' : 'Thêm'} ${isRecipient ? 'người được tiêm' : 'hồ sơ cá nhân'}`
  useEffect(() => {
    const element = dialog.current
    const previous = document.activeElement
    element.showModal()
    return () => { element.close(); requestAnimationFrame(() => { if (previous?.isConnected) previous.focus() }) }
  }, [])
  useEffect(() => { dialog.current.querySelector(viewing ? 'h2' : 'input')?.focus() }, [viewing])
  const setDraft = draft => setEditor(current => ({ ...current, draft }))
  const input = (name, label, options = {}) => <label className="demo-field" key={name}>
    <span>{label}{options.required !== false && ' *'}</span>
    <input {...options} required={options.required !== false} value={editor.draft[name] || ''} onChange={e => setDraft({ ...editor.draft, [name]: e.target.value })} />
  </label>
  return <dialog ref={dialog} className="profile-dialog" aria-labelledby="profile-dialog-title" onCancel={event => { event.preventDefault(); if (!busy) onClose() }}>
    <div className="profile-dialog-heading"><div><span className="profile-kicker">{viewing ? 'CHI TIẾT HỒ SƠ' : editor.draft.id ? 'CHỈNH SỬA HỒ SƠ' : 'HỒ SƠ MỚI'}</span><h2 id="profile-dialog-title" tabIndex={-1}>{title}</h2></div><button className="profile-close" aria-label="Đóng biểu mẫu" disabled={busy} onClick={onClose}><X aria-hidden="true" /></button></div>
    {viewing ? <>
      <ProfileDetails value={editor.draft} recipient />
      {error && <p className="profile-feedback error" role="alert">{error}</p>}
      <div className="profile-form-actions">
        <button className="button outline" disabled={busy} onClick={onClose}>Đóng</button>
        {canUpdate && <><button className="button profile-danger" disabled={busy} onClick={onDelete}><Trash2 aria-hidden="true" />Xóa</button><button className="button primary" disabled={busy} onClick={() => setEditor(current => ({ ...current, mode: 'edit' }))}><Pencil aria-hidden="true" />Chỉnh sửa</button></>}
      </div>
    </> : <form onSubmit={event => { event.preventDefault(); onSave() }} aria-busy={busy}>
      <p className="profile-help">Các trường có dấu * là bắt buộc. CCCD có thể để trống.</p>
      {isRecipient && <p className="profile-owner">Hồ sơ liên kết: <strong>{customers.find(customer => customer.id === Number(editor.draft.customerId))?.fullName || 'Không có hồ sơ khách hàng trong phạm vi xem'}</strong>{editor.draft.id && <span>Không thể chuyển sang khách hàng khác.</span>}</p>}
      <fieldset disabled={busy} className="profile-fields">
        {input('fullName', 'Họ và tên', { maxLength: 150, autoComplete: 'off' })}
        {input('identityNumber', 'CCCD (12 chữ số)', { required: false, maxLength: 12, pattern: '[0-9]{12}', inputMode: 'numeric', title: 'Nhập đúng 12 chữ số hoặc để trống.' })}
        {isRecipient ? <>
          {input('dateOfBirth', 'Ngày sinh', { type: 'date', max: today() })}
          <label className="demo-field"><span>Giới tính *</span><select value={editor.draft.gender} onChange={e => setDraft({ ...editor.draft, gender: e.target.value })}>{['Nam', 'Nữ', 'Khác'].map(gender => <option key={gender}>{gender}</option>)}</select></label>
          {input('relationshipToCustomer', 'Quan hệ với khách hàng', { maxLength: 50 })}
        </> : <>
          {input('phoneNumber', 'Số điện thoại', { type: 'tel', maxLength: 11, pattern: '[0-9]{10,11}', inputMode: 'numeric', title: 'Nhập số điện thoại gồm 10–11 chữ số.', autoComplete: 'tel' })}
          {input('email', 'Email', { type: 'email', maxLength: 254, autoComplete: 'email' })}
        </>}
      </fieldset>
      {error && <p className="profile-feedback error" role="alert">{error}</p>}
      <div className="profile-form-actions"><button className="button outline" type="button" disabled={busy} onClick={onClose}>Hủy</button><button className="button primary" type="submit" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu hồ sơ'}</button></div>
    </form>}
  </dialog>
}

export default function Profiles({ capabilities, sessionBusy = false, onBusyChange }) {
  const [customers, setCustomers] = useState([])
  const [recipients, setRecipients] = useState([])
  const [customerId, setCustomerId] = useState('')
  const [editor, setEditor] = useState(null)
  const [busy, setBusy] = useState(true)
  const [ready, setReady] = useState(false)
  const [error, setError] = useState('')
  const [errorResource, setErrorResource] = useState(null)
  const [notice, setNotice] = useState(null)
  const [createdCustomer, setCreatedCustomer] = useState(false)
  const pending = useRef(false)
  const customerAction = useRef(null)
  const recipientAction = useRef(null)
  const canCreate = capabilities?.profilesCreate === true
  // A new customer link is created by this page; /auth/me is a login-time snapshot.
  const canUpdate = capabilities?.profilesUpdate === true || createdCustomer
  const selectedCustomer = customers.find(customer => customer.id === Number(customerId))
  const locked = busy || sessionBusy

  function accept(customers, recipients) {
    setCustomers(customers); setRecipients(recipients); setReady(true)
    setCustomerId(current => customers.some(customer => customer.id === Number(current)) ? current : customers.length === 1 ? customers[0].id : '')
  }
  async function load() {
    const [c, r] = await Promise.all([list('customers'), list('recipients')])
    accept(c, r)
  }
  useEffect(() => {
    let active = true
    Promise.all([list('customers'), list('recipients')])
      .then(([c, r]) => { if (active) accept(c, r) })
      .catch(error => { if (active) setError(profileError(error)) })
      .finally(() => { if (active) setBusy(false) })
    return () => { active = false }
  }, [])
  useEffect(() => { onBusyChange?.(busy) }, [busy, onBusyChange])

  async function run(action, resource = null) {
    if (pending.current || locked) return
    pending.current = true; setBusy(true); setError(''); setErrorResource(resource); setNotice(null)
    try { await action() } catch (error) { setError(profileError(error)) }
    finally { pending.current = false; setBusy(false) }
  }
  function open(resource, draft, mode = 'edit') {
    setError(''); setErrorResource(null); setNotice(null); setEditor({ resource, draft: { ...draft }, mode })
  }
  function close() { setEditor(null); setError('') }
  async function save() {
    const { resource, draft } = editor
    const body = profileBody(resource, draft, customers)
    const saved = await identity.profiles(`${resource}${draft.id ? `/${draft.id}` : ''}`, draft.id ? 'PUT' : 'POST', body)
    // Apply the confirmed write without a second GET that could fail and invite a duplicate POST.
    const update = items => draft.id ? items.map(item => item.id === saved.id ? saved : item) : [...items, saved]
    if (resource === 'customers') { setCustomers(update); setCustomerId(saved.id); if (!draft.id) setCreatedCustomer(true) }
    else setRecipients(update)
    setEditor(null); setNotice({ resource, text: 'Đã lưu hồ sơ thành công.' })
  }
  function remove(resource, value) {
    const detail = resource === 'customers' ? 'Nếu còn người được tiêm hoặc dữ liệu liên quan, hồ sơ sẽ không thể xóa.' : 'Bạn sẽ không thể khôi phục hồ sơ này từ trang tài khoản.'
    if (!window.confirm(`Xóa hồ sơ “${value.fullName}”?\n${detail}`)) return
    run(async () => {
      await identity.profiles(`${resource}/${value.id}`, 'DELETE')
      if (resource === 'customers') { setCustomers(items => items.filter(item => item.id !== value.id)); setCustomerId('') }
      else setRecipients(items => items.filter(item => item.id !== value.id))
      setEditor(null); setNotice({ resource, text: 'Đã xóa hồ sơ.' })
      requestAnimationFrame(() => (resource === 'customers' ? customerAction : recipientAction).current?.focus())
    }, resource)
  }
  const feedback = resource => <>
    {!editor && error && errorResource === resource && <p className="profile-feedback error" role="alert">{error}</p>}
    {notice?.resource === resource && <p className="profile-feedback success" role="status">{notice.text}</p>}
  </>
  return <div className="profiles-area" aria-busy={busy}>
    <div className="profiles-toolbar"><p>Thông tin của bạn và những người được tiêm đã liên kết với tài khoản.</p><button className="button text-button" disabled={locked} onClick={() => run(load)}><RefreshCw aria-hidden="true" />{busy ? 'Đang tải…' : 'Tải lại'}</button></div>
    {!editor && error && !errorResource && <p className="profile-feedback error" role="alert">{error}</p>}
    {!ready ? <div className="profile-empty" role="status">{busy ? 'Đang tải hồ sơ của bạn…' : 'Chưa tải được hồ sơ. Chọn Tải lại để thử lại.'}</div> : <div className="profiles-layout">
      <section className="profile-panel" aria-labelledby="personal-title"><div className="profile-section-heading"><div><span className="profile-kicker">THÔNG TIN CỦA BẠN</span><h2 id="personal-title">Hồ sơ cá nhân</h2></div></div>
        {customers.length > 1 && <label className="demo-field profile-customer-picker"><span>Chọn hồ sơ khách hàng</span><select ref={customerAction} disabled={locked} value={customerId} onChange={event => { setCustomerId(event.target.value); setError(''); setNotice(null) }}><option value="">Chọn hồ sơ…</option>{customers.map(customer => <option value={customer.id} key={customer.id}>{customer.fullName} · #{customer.id}</option>)}</select></label>}
        {selectedCustomer ? <>
          <ProfileDetails value={selectedCustomer} />
          {!canUpdate && <p className="profile-help">Bạn có quyền xem thông tin. Tài khoản hiện không có quyền chỉnh sửa hồ sơ.</p>}
          <div className="profile-panel-actions">{canUpdate && <><button ref={customers.length === 1 ? customerAction : undefined} className="button outline" disabled={locked} onClick={() => open('customers', selectedCustomer)}><Pencil aria-hidden="true" />Chỉnh sửa hồ sơ</button><button className="button profile-danger" disabled={locked} onClick={() => remove('customers', selectedCustomer)}><Trash2 aria-hidden="true" />Xóa hồ sơ</button></>}</div>
        </> : <div className="profile-empty"><h3>{customers.length ? 'Chọn hồ sơ để xem' : 'Hoàn thiện hồ sơ cá nhân'}</h3><p>{customers.length ? 'Chọn đúng khách hàng trước khi thêm người được tiêm.' : 'Thêm thông tin liên hệ để bắt đầu quản lý hồ sơ người được tiêm.'}</p>{!customers.length && canCreate && <button ref={customerAction} className="button primary" disabled={locked} onClick={() => open('customers', emptyCustomer)}><Plus aria-hidden="true" />Thêm hồ sơ cá nhân</button>}</div>}
        {feedback('customers')}
      </section>
      <section className="profile-panel" aria-labelledby="recipients-title"><div className="profile-section-heading"><div><span className="profile-kicker">HỒ SƠ ĐÃ LIÊN KẾT</span><h2 id="recipients-title">Người được tiêm <span className="profile-count">{recipients.length}</span></h2></div>{canCreate && <button ref={recipientAction} className="button primary" disabled={locked || !selectedCustomer} onClick={() => open('recipients', { ...emptyRecipient, customerId: selectedCustomer.id })}><Plus aria-hidden="true" />Thêm mới</button>}</div>
        <p className="profile-help">Xem và quản lý thông tin từng người trước khi đăng ký tiêm.</p>
        {canCreate && !selectedCustomer && <p className="profile-help">{customers.length ? 'Chọn hồ sơ khách hàng ở phần Hồ sơ cá nhân để thêm người được tiêm.' : 'Cần tạo hồ sơ cá nhân trước khi thêm người được tiêm.'}</p>}
        {!canUpdate && recipients.length > 0 && <p className="profile-help">Bạn có quyền xem thông tin. Tài khoản hiện không có quyền chỉnh sửa hồ sơ.</p>}
        {recipients.length ? <ul className="profile-recipient-list">{recipients.map(recipient => <li key={recipient.id} className="profile-recipient-card"><div className="profile-initial" aria-hidden="true">{recipient.fullName.replace(/^\[DEMO\]\s*/, '').trim().slice(0, 1)}</div><div className="profile-recipient-info"><h3>{recipient.fullName}</h3><p>{recipient.relationshipToCustomer} · {birthDate(recipient.dateOfBirth)}</p></div><button className="button text-button" disabled={locked} aria-label={`Xem ${recipient.fullName}`} onClick={() => open('recipients', recipient, 'view')}>Xem<ArrowRight aria-hidden="true" /></button></li>)}</ul> : <div className="profile-empty"><h3>Chưa có người được tiêm</h3><p>Thêm hồ sơ để quản lý thông tin người được tiêm trong cùng tài khoản.</p></div>}
        {feedback('recipients')}
      </section>
    </div>}
    {editor && <ProfileDialog editor={editor} setEditor={setEditor} customers={customers} busy={locked} error={error} onClose={close} onSave={() => run(save)} onDelete={() => remove(editor.resource, editor.draft)} canUpdate={canUpdate} />}
  </div>
}
