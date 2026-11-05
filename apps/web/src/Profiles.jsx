import { useEffect, useRef, useState } from 'react'
import { Plus, Pencil, Trash2, RefreshCw, X, ArrowRight } from 'lucide-react'
import { identity } from './api/identity'
import { emptyCustomer, emptyRecipient, profileBody, profileError, relationships, salutations, vietnamToday } from './profileForm'
import ProfileClaimFields from './ProfileClaimFields'
import { profileSearchBody } from './profileForm'
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

function ProfileDetails({ value, recipient = false }) {
  const details = recipient
    ? [['Họ và tên', value.fullName], ['Ngày sinh', birthDate(value.dateOfBirth)], ['Giới tính', value.gender], ['Quan hệ với khách hàng', value.relationshipToCustomer], ['CCCD', value.identityNumber], ['Mã hồ sơ', value.profileCode]]
    : [['Họ và tên', value.fullName], ['Danh xưng', value.salutation], ['Ngày sinh', birthDate(value.dateOfBirth)], ['Số điện thoại', value.phoneNumber], ['Email', value.email], ['CCCD', value.identityNumber]]
  return <dl className="profile-details">{details.map(([label, text]) => <div key={label}><dt>{label}</dt><dd>{text || 'Chưa cập nhật'}</dd></div>)}</dl>
}

function ProfileDialog({ editor, setEditor, customers, busy, error, onClose, onSave, onDelete, canUpdate }) {
  const dialog = useRef(null)
  const isRecipient = editor.resource === 'recipients'
  const viewing = editor.mode === 'view'
  const claiming = editor.mode === 'claim'
  const findingRecipient = claiming && isRecipient
  const title = claiming ? 'Tìm hồ sơ tiêm chủng có sẵn' : viewing ? 'Thông tin người được tiêm' : `${editor.draft.id ? 'Chỉnh sửa' : 'Thêm'} ${isRecipient ? 'người được tiêm' : 'hồ sơ cá nhân'}`
  useEffect(() => {
    const element = dialog.current
    const previous = document.activeElement
    element.showModal()
    return () => { element.close(); requestAnimationFrame(() => { if (previous?.isConnected) previous.focus() }) }
  }, [])
  useEffect(() => { dialog.current.querySelector(viewing ? 'h2' : '.profile-fields input')?.focus() }, [viewing])
  const setDraft = draft => setEditor(current => ({ ...current, draft }))
  const input = (name, label, options = {}) => <label className="demo-field" key={name}>
    <span>{label}{options.required !== false && ' *'}</span>
    <input {...options} required={options.required !== false} value={editor.draft[name] || ''} onChange={e => setDraft({ ...editor.draft, [name]: e.target.value })} />
  </label>
  const select = (name, label, choices) => <label className="demo-field">
    <span>{label} *</span>
    <select required value={choices.includes(editor.draft[name]) ? editor.draft[name] : ''} onChange={e => setDraft({ ...editor.draft, [name]: e.target.value })}>
      <option value="">Chọn {label.toLowerCase()}…</option>
      {choices.map(choice => <option key={choice}>{choice}</option>)}
    </select>
  </label>
  return <dialog ref={dialog} className="profile-dialog" aria-labelledby="profile-dialog-title" onCancel={event => { event.preventDefault(); if (!busy) onClose() }}>
    <div className="profile-dialog-heading"><div><span className="profile-kicker">{claiming ? 'LIÊN KẾT HỒ SƠ' : viewing ? 'CHI TIẾT HỒ SƠ' : editor.draft.id ? 'CHỈNH SỬA HỒ SƠ' : 'HỒ SƠ MỚI'}</span><h2 id="profile-dialog-title" tabIndex={-1}>{title}</h2></div><button className="profile-close" aria-label="Đóng biểu mẫu" disabled={busy} onClick={onClose}><X aria-hidden="true" /></button></div>
    {viewing ? <>
      <ProfileDetails value={editor.draft} recipient />
      {error && <p className="profile-feedback error" role="alert">{error}</p>}
      <div className="profile-form-actions">
        <button className="button outline" disabled={busy} onClick={onClose}>Đóng</button>
        {canUpdate && <><button className="button profile-danger" disabled={busy} onClick={onDelete}><Trash2 aria-hidden="true" />Xóa</button><button className="button primary" disabled={busy} onClick={() => setEditor(current => ({ ...current, mode: 'edit' }))}><Pencil aria-hidden="true" />Chỉnh sửa</button></>}
      </div>
    </> : <form onSubmit={event => { event.preventDefault(); onSave() }} aria-busy={busy}>
      <p className="profile-help">{claiming
        ? isRecipient ? 'Nhập số điện thoại trong hồ sơ và thông tin người được tiêm.' : 'Nhập đúng thông tin đã cung cấp cho nhân viên. Email đã xác minh của tài khoản phải trùng email trong hồ sơ.'
        : 'Các trường có dấu * là bắt buộc. CCCD có thể để trống.'}</p>
      {isRecipient && !claiming && <p className="profile-owner">Hồ sơ liên kết: <strong>{customers.find(customer => customer.id === Number(editor.draft.customerId))?.fullName || 'Không có hồ sơ khách hàng trong phạm vi xem'}</strong>{editor.draft.id && <span>Không thể chuyển sang khách hàng khác.</span>}</p>}
      {findingRecipient ? <ProfileClaimFields draft={{ ...editor.draft, mode: editor.claimMode }} busy={busy}
        onChange={({ mode, ...draft }) => setEditor(current => ({ ...current, draft, claimMode: mode }))} /> : <>
      <fieldset disabled={busy} className="profile-fields">
          {input('fullName', 'Họ và tên', { maxLength: 150, autoComplete: 'off' })}
          {input('dateOfBirth', 'Ngày sinh', { type: 'date', max: vietnamToday() })}
        {!claiming && input('identityNumber', 'CCCD (12 chữ số)', { required: false, maxLength: 12, pattern: '[0-9]{12}', inputMode: 'numeric', title: 'Nhập đúng 12 chữ số hoặc để trống.' })}
        {(!isRecipient || findingRecipient) && input('phoneNumber', 'Số điện thoại', { type: 'tel', maxLength: 11, pattern: '[0-9]{10,11}', inputMode: 'numeric', title: 'Nhập số điện thoại gồm 10–11 chữ số.', autoComplete: 'tel' })}
        {!claiming && (isRecipient ? <>
          {select('gender', 'Giới tính', ['Nam', 'Nữ', 'Khác'])}
          {select('relationshipToCustomer', 'Quan hệ với khách hàng', relationships)}
        </> : <>
          {select('salutation', 'Danh xưng', salutations)}
          {input('email', 'Email', { type: 'email', maxLength: 254, autoComplete: 'email' })}
        </>)}
      </fieldset></>}
      {error && <p className="profile-feedback error" role="alert">{error}</p>}
      <div className="profile-form-actions"><button className="button outline" type="button" disabled={busy} onClick={onClose}>Hủy</button><button className="button primary" type="submit" disabled={busy}>{claiming ? busy ? 'Đang xác minh…' : 'Tìm và liên kết' : busy ? 'Đang lưu…' : 'Lưu hồ sơ'}</button></div>
    </form>}
  </dialog>
}

export default function Profiles({ initialClaim, onClaimDismiss = () => {}, capabilities, sessionBusy = false, onBusyChange, go = () => {} }) {
  const [customers, setCustomers] = useState([])
  const [recipients, setRecipients] = useState([])
  const [customerId, setCustomerId] = useState('')
  const [editor, setEditor] = useState(() => initialClaim ? { resource: 'recipients', mode: 'claim', draft: { ...initialClaim }, claimMode: initialClaim.mode } : null)
  const [busy, setBusy] = useState(true)
  const [ready, setReady] = useState(false)
  const [error, setError] = useState('')
  const [errorResource, setErrorResource] = useState(null)
  const [notice, setNotice] = useState(null)
  const [deletedCustomer, setDeletedCustomer] = useState(false)
  const pending = useRef(false)
  const customerAction = useRef(null)
  const recipientAction = useRef(null)
  const canCreate = capabilities?.profilesCreate === true
  // These lists reflect current ownership; the account capability is loaded at login.
  const canUpdate = capabilities?.profilesUpdate === true || (canCreate && customers.length > 0)
  const canClaimCustomer = capabilities?.selfClaim === true || deletedCustomer
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
    try { await action() } catch (error) { setError(editor?.mode === 'claim' && editor.resource === 'recipients' && error.code === 'profileClaimUnavailable' ? 'Không tìm thấy hồ sơ phù hợp. Vui lòng kiểm tra lại thông tin.' : profileError(error)) }
    finally { pending.current = false; setBusy(false) }
  }
  function open(resource, draft, mode = 'edit') {
    setError(''); setErrorResource(null); setNotice(null); setEditor({ resource, draft: { ...draft }, mode, claimMode: 'profileCode' })
  }
  function close() { setEditor(null); setError(''); onClaimDismiss() }
  async function save() {
    const { resource, draft, mode } = editor
    if (mode === 'claim') {
      const { fullName, dateOfBirth, phoneNumber } = draft
      const claimMode = editor.claimMode
      const body = resource === 'customers' ? { fullName, dateOfBirth, phoneNumber }
        : profileSearchBody({ ...draft, mode: claimMode })
      const linked = await identity.profiles(`${resource}/claim`, 'POST', body)
      const add = (items, item) => items.some(current => current.id === item.id)
        ? items.map(current => current.id === item.id ? item : current) : [...items, item]
      const customer = resource === 'customers' ? linked : linked.customer
      setCustomers(items => add(items, customer))
      setCustomerId(customer.id)
      if (resource === 'recipients') setRecipients(items => add(items, linked.recipient))
      setEditor(null); onClaimDismiss(); setNotice({ resource, text: 'Đã liên kết hồ sơ với tài khoản của bạn.' })
      return
    }
    const body = profileBody(resource, draft, customers)
    const saved = await identity.profiles(`${resource}${draft.id ? `/${draft.id}` : ''}`, draft.id ? 'PUT' : 'POST', body)
    // Apply the confirmed write without a second GET that could fail and invite a duplicate POST.
    const update = items => draft.id ? items.map(item => item.id === saved.id ? saved : item) : [...items, saved]
    if (resource === 'customers') { setCustomers(update); setCustomerId(saved.id) }
    else setRecipients(update)
    setEditor(null); setNotice({ resource, text: 'Đã lưu hồ sơ thành công.' })
  }
  function remove(resource, value) {
    const detail = resource === 'customers' ? 'Nếu còn người được tiêm hoặc dữ liệu liên quan, hồ sơ sẽ không thể xóa.' : 'Bạn sẽ không thể khôi phục hồ sơ này từ trang tài khoản.'
    if (!window.confirm(`Xóa hồ sơ “${value.fullName}”?\n${detail}`)) return
    run(async () => {
      await identity.profiles(`${resource}/${value.id}`, 'DELETE')
      if (resource === 'customers') { setCustomers(items => items.filter(item => item.id !== value.id)); setCustomerId(''); setDeletedCustomer(true) }
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
      <section className="profile-panel" aria-labelledby="recipients-title"><div className="profile-section-heading"><div><span className="profile-kicker">HỒ SƠ ĐÃ LIÊN KẾT</span><h2 id="recipients-title">Người được tiêm <span className="profile-count">{recipients.length}</span></h2></div>{canCreate && <button ref={recipientAction} className="button primary" disabled={locked || !selectedCustomer} onClick={() => open('recipients', { ...emptyRecipient, customerId: selectedCustomer.id })}><Plus aria-hidden="true" />Thêm mới</button>}</div>
        <p className="profile-help">Xem và quản lý thông tin từng người trước khi đăng ký tiêm.</p>
        {canCreate && <div className="profile-panel-actions"><button className="button outline" disabled={locked} onClick={() => open('recipients', {}, 'claim')}>Tìm hồ sơ tiêm chủng có sẵn</button></div>}
        {canCreate && !selectedCustomer && <p className="profile-help">{customers.length ? 'Chọn hồ sơ khách hàng ở phần Hồ sơ cá nhân để thêm người được tiêm.' : 'Cần liên kết hoặc tạo hồ sơ cá nhân trước khi thêm người được tiêm.'}</p>}
        {!canUpdate && recipients.length > 0 && <p className="profile-help">Bạn có quyền xem thông tin. Tài khoản hiện không có quyền chỉnh sửa hồ sơ.</p>}
        {recipients.length ? <ul className="profile-recipient-list">{recipients.map(recipient => <li key={recipient.id} className="profile-recipient-card"><div className="profile-initial" aria-hidden="true">{recipient.fullName.replace(/^\[DEMO\]\s*/, '').trim().slice(0, 1)}</div><div className="profile-recipient-info"><h3>{recipient.fullName}</h3><p>{recipient.relationshipToCustomer} · {birthDate(recipient.dateOfBirth)}</p>{recipient.profileCode && <p>Mã hồ sơ {recipient.profileCode}</p>}</div><button className="button text-button" disabled={locked} aria-label={`Xem ${recipient.fullName}`} onClick={() => open('recipients', recipient, 'view')}>Xem hồ sơ<ArrowRight aria-hidden="true" /></button><button className="button primary" disabled={locked} onClick={() => go(`register/recipient/${recipient.id}`)}>Đăng ký tiêm</button></li>)}</ul> : <div className="profile-empty"><h3>Chưa có người được tiêm</h3><p>Thêm người được tiêm hoặc tìm hồ sơ tiêm chủng có sẵn để bắt đầu.</p></div>}
        {feedback('recipients')}
      </section>
      <section className="profile-panel" aria-labelledby="personal-title"><div className="profile-section-heading"><div><span className="profile-kicker">THÔNG TIN CỦA BẠN</span><h2 id="personal-title">Hồ sơ khách hàng/người quản lý</h2></div></div>
        {customers.length > 1 && <label className="demo-field profile-customer-picker"><span>Chọn hồ sơ khách hàng</span><select ref={customerAction} disabled={locked} value={customerId} onChange={event => { setCustomerId(event.target.value); setError(''); setNotice(null) }}><option value="">Chọn hồ sơ…</option>{customers.map(customer => <option value={customer.id} key={customer.id}>{customer.fullName}</option>)}</select></label>}
        {selectedCustomer ? <>
          <ProfileDetails value={selectedCustomer} />
          {!canUpdate && <p className="profile-help">Bạn có quyền xem thông tin. Tài khoản hiện không có quyền chỉnh sửa hồ sơ.</p>}
          <div className="profile-panel-actions">{canUpdate && <><button ref={customers.length === 1 ? customerAction : undefined} className="button outline" disabled={locked} onClick={() => open('customers', selectedCustomer)}><Pencil aria-hidden="true" />Chỉnh sửa hồ sơ</button><button className="button profile-danger" disabled={locked} onClick={() => remove('customers', selectedCustomer)}><Trash2 aria-hidden="true" />Xóa hồ sơ</button></>}</div>
        </> : <div className="profile-empty"><h3>{customers.length ? 'Chọn hồ sơ để xem' : 'Hoàn thiện hồ sơ cá nhân'}</h3>
          <p>{customers.length ? 'Chọn đúng khách hàng trước khi thêm người được tiêm.' : 'Nếu đã cung cấp thông tin cho nhân viên, hãy tìm hồ sơ có sẵn trước.'}</p>
          {!customers.length && <div className="profile-panel-actions">
            {canClaimCustomer && <button className="button outline" disabled={locked} onClick={() => open('customers', {}, 'claim')}>Đã có hồ sơ</button>}
            {canCreate && <button ref={customerAction} className="button primary" disabled={locked} onClick={() => open('customers', emptyCustomer)}><Plus aria-hidden="true" />Thêm hồ sơ cá nhân</button>}
          </div>}
        </div>}
        {feedback('customers')}
      </section>
    </div>}
    {editor && <ProfileDialog editor={editor} setEditor={setEditor} customers={customers} busy={locked} error={error} onClose={close} onSave={() => run(save)} onDelete={() => remove(editor.resource, editor.draft)} canUpdate={canUpdate} />}
  </div>
}
