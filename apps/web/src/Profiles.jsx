import { useEffect, useState } from 'react'
import { identity } from './api/identity'

async function list(resource) {
  const items = []
  for (let page = 1; ; page++) {
    const response = await identity.profiles(`${resource}?page=${page}&pageSize=100`)
    items.push(...response.items)
    if (response.items.length < 100) return { items }
  }
}
const emptyCustomer = { fullName: '', identityNumber: '', phoneNumber: '', email: '' }
const emptyRecipient = { fullName: '', identityNumber: '', dateOfBirth: '', gender: 'Nam', relationshipToCustomer: 'Con' }
export default function Profiles() {
  const [customers, setCustomers] = useState([])
  const [recipients, setRecipients] = useState([])
  const [customer, setCustomer] = useState(emptyCustomer)
  const [recipient, setRecipient] = useState(emptyRecipient)
  const [busy, setBusy] = useState(true)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [ready, setReady] = useState(false)
  async function load() {
    const [c, r] = await Promise.all([list('customers'), list('recipients')])
    setCustomers(c.items); setRecipients(r.items); setCustomer(c.items[0] || emptyCustomer); setReady(true)
  }
  useEffect(() => {
    let active = true
    Promise.all([list('customers'), list('recipients')])
      .then(([c, r]) => { if (active) { setCustomers(c.items); setRecipients(r.items); setCustomer(c.items[0] || emptyCustomer); setReady(true) } })
      .catch(e => { if (active) setError(e.message) }).finally(() => { if (active) setBusy(false) })
    return () => { active = false }
  }, [])
  async function run(action) {
    if (busy) return
    setBusy(true); setError(''); setMessage('')
    try { await action() } catch (e) { setError(e.message || 'Không kết nối được API.') } finally { setBusy(false) }
  }
  function fields(value, setValue, child) {
    const input = (name, label, type = 'text', required = true, maxLength) => <label className="demo-field" key={name}><span>{label}</span><input type={type} required={required} maxLength={maxLength} value={value[name] || ''} onChange={e => setValue({ ...value, [name]: e.target.value })} pattern={name === 'identityNumber' ? '[0-9]{12}' : name === 'phoneNumber' ? '[0-9]{10,11}' : undefined} /></label>
    return <>{input('fullName', 'Họ tên *', 'text', true, 150)}{input('identityNumber', 'CCCD (có thể bỏ trống)', 'text', false, 12)}
      {child ? <>{input('dateOfBirth', 'Ngày sinh *', 'date')}<label className="demo-field"><span>Giới tính *</span><select value={value.gender} onChange={e => setValue({ ...value, gender: e.target.value })}>{['Nam', 'Nữ', 'Khác'].map(g => <option key={g}>{g}</option>)}</select></label>{input('relationshipToCustomer', 'Quan hệ với khách hàng *', 'text', true, 50)}</> : <>{input('phoneNumber', 'Số điện thoại *', 'tel', true, 11)}{input('email', 'Email *', 'email', true, 254)}</>}</>
  }
  async function save(resource, value) {
    const { id, customerId: _customerId, ...body } = value
    if (resource === 'recipients' && !id) body.customerId = customers[0].id
    body.identityNumber = body.identityNumber?.trim() || null
    await identity.profiles(`${resource}${id ? `/${id}` : ''}`, id ? 'PUT' : 'POST', body)
    await load(); setRecipient(emptyRecipient); setMessage('Đã lưu hồ sơ vào máy chủ.')
  }
  async function remove(resource, id) {
    if (!window.confirm('Xóa hồ sơ đã chọn?')) return
    await identity.profiles(`${resource}/${id}`, 'DELETE'); await load(); setRecipient(emptyRecipient); setMessage('Đã xóa hồ sơ.')
  }
  return <section className="form-card"><h2>Hồ sơ của tôi</h2><p>Thông tin được lưu qua API / SQL.</p>
    {error && <p role="alert">{error}</p>}{message && <p role="status">{message}</p>}
    <button className="button outline" disabled={busy} onClick={() => run(load)}>{busy ? 'Đang xử lý…' : 'Tải lại hồ sơ'}</button>
    {ready && <fieldset disabled={busy} style={{ border: 0, padding: 0 }}>
      <form onSubmit={e => { e.preventDefault(); run(() => save('customers', customer)) }}><h3>Khách hàng</h3>{fields(customer, setCustomer, false)}<button className="button primary">{customer.id ? 'Lưu khách hàng' : 'Đăng ký hồ sơ'}</button>{customer.id && <button type="button" className="button outline" onClick={() => run(() => remove('customers', customer.id))}>Xóa khách hàng</button>}</form>
      {customers.length > 0 && <><h3>Người được tiêm / bé</h3><ul>{recipients.map(r => <li key={r.id}><button type="button" className="button text-button" onClick={() => setRecipient(r)}>{r.fullName} · {r.dateOfBirth}</button></li>)}</ul>{recipients.length === 0 && <p>Chưa có người được tiêm.</p>}
        <button type="button" className="button outline" onClick={() => setRecipient(emptyRecipient)}>Thêm người được tiêm</button>
        <form onSubmit={e => { e.preventDefault(); run(() => save('recipients', recipient)) }}>{fields(recipient, setRecipient, true)}<button className="button primary">Lưu người được tiêm</button>{recipient.id && <button type="button" className="button outline" onClick={() => run(() => remove('recipients', recipient.id))}>Xóa người được tiêm</button>}</form></>}
    </fieldset>}
  </section>
}
