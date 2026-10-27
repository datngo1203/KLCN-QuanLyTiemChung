import { useRef, useState } from 'react'
import { identity } from './api/identity'
import Profiles from './Profiles'

export default function Account({ account, setAccount }) {
  const [mode, setMode] = useState('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmation, setConfirmation] = useState('')
  const [passwordFormOpen, setPasswordFormOpen] = useState(false)
  const [showPasswords, setShowPasswords] = useState(false)
  const passwordAction = useRef(null)
  const [token, setToken] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  function closePasswordForm() {
    setPasswordFormOpen(false); setShowPasswords(false)
    setPassword(''); setNewPassword(''); setConfirmation(''); setError(''); setMessage('')
    passwordAction.current?.focus()
  }
  function openPasswordForm() {
    if (passwordFormOpen) { closePasswordForm(); return }
    setPassword(''); setError(''); setMessage(''); setPasswordFormOpen(true)
  }
  async function run(action) {
    if (busy) return
    setBusy(true); setError(''); setMessage('')
    try { await action() } catch (failure) {
      const unavailable = failure instanceof TypeError || failure instanceof SyntaxError || ['AbortError', 'TimeoutError'].includes(failure.name)
      setError(unavailable ? 'Không kết nối được dịch vụ. Vui lòng thử lại.' : failure.message || 'Không kết nối được dịch vụ.')
    }
    finally { setBusy(false) }
  }
  function changePassword(event) {
    event.preventDefault()
    run(async () => {
      await identity.changePassword(password, newPassword, confirmation)
      setPasswordFormOpen(false); setShowPasswords(false)
      setPassword(''); setNewPassword(''); setConfirmation(''); setAccount(null); setMode('login')
      setMessage('Đã đổi mật khẩu và thu hồi các phiên. Vui lòng đăng nhập lại.')
    })
  }
  function submit(event) {
    event.preventDefault()
    run(async () => {
      if (mode === 'login') setAccount(await identity.login(email, password))
      else if (mode === 'register') {
        await identity.register(email, password)
        setMode('confirm')
        setMessage('Đăng ký đã được tiếp nhận. Nhập mã xác minh email để hoàn tất. Demo nhận mã qua thư mục mail local trong hướng dẫn setup.')
      } else {
        await identity.confirm(token)
        setToken(''); setMode('login'); setMessage('Đã xác minh email. Bạn có thể đăng nhập.')
      }
      setPassword('')
    })
  }
  const labels = { login: 'Đăng nhập', register: 'Đăng ký tài khoản', confirm: 'Xác minh email' }
  return <section className="form-page wrap">
    <h1>{account ? 'Tài khoản khách hàng' : labels[mode]}</h1>
    {account ? <div className="form-card">
      <p>Đã đăng nhập · Mã tài khoản {account.accountId}</p>
      <div className="account-actions">
        <button ref={passwordAction} className="button outline" type="button" aria-expanded={passwordFormOpen} aria-controls="change-password-form" disabled={busy} onClick={openPasswordForm}>Đổi mật khẩu</button>
        <button className="button primary" disabled={busy} onClick={() => run(async () => { await identity.logout(); closePasswordForm(); setAccount(null); setMessage('Đã đăng xuất.'); setMode('login') })}>Đăng xuất</button>
      </div>
      {passwordFormOpen && <form id="change-password-form" className="account-password-form" onSubmit={changePassword} aria-busy={busy}>
        <h2>Đổi mật khẩu</h2>
        <p id="password-requirements">Mật khẩu mới 12–128 ký tự, khác mật khẩu hiện tại. Đổi thành công sẽ yêu cầu đăng nhập lại trên mọi thiết bị.</p>
        <fieldset disabled={busy} style={{ border: 0, padding: 0 }}>
          <label className="demo-field"><span>Mật khẩu hiện tại</span><input autoFocus type={showPasswords ? 'text' : 'password'} autoComplete="current-password" required maxLength={128} value={password} onChange={e => setPassword(e.target.value)} /></label>
          <label className="demo-field"><span>Mật khẩu mới</span><input type={showPasswords ? 'text' : 'password'} autoComplete="new-password" aria-describedby="password-requirements" required minLength={12} maxLength={128} value={newPassword} onChange={e => setNewPassword(e.target.value)} /></label>
          <label className="demo-field"><span>Xác nhận mật khẩu mới</span><input type={showPasswords ? 'text' : 'password'} autoComplete="new-password" required minLength={12} maxLength={128} value={confirmation} onChange={e => setConfirmation(e.target.value)} /></label>
          <label className="password-visibility"><input type="checkbox" checked={showPasswords} onChange={e => setShowPasswords(e.target.checked)} />Hiển thị mật khẩu</label>
          <div className="account-actions">
            <button className="button primary" type="submit">{busy ? 'Đang đổi mật khẩu…' : 'Lưu mật khẩu mới'}</button>
            <button className="button outline" type="button" onClick={closePasswordForm}>Hủy</button>
          </div>
        </fieldset>
      </form>}
    </div> : <form className="form-card" onSubmit={submit}>
      <fieldset disabled={busy} style={{ border: 0, padding: 0 }}>
        {mode !== 'confirm' ? <>
          <label className="demo-field"><span>Email</span><input type="email" autoComplete="username" required maxLength={254} value={email} onChange={e => setEmail(e.target.value)} /></label>
          <label className="demo-field"><span>Mật khẩu</span><input type="password" autoComplete={mode === 'register' ? 'new-password' : 'current-password'} required minLength={mode === 'register' ? 12 : undefined} maxLength={128} value={password} onChange={e => setPassword(e.target.value)} /></label>
          {mode === 'register' && <p>Demo: email @example.invalid, mật khẩu 12–128 ký tự.</p>}
        </> : <label className="demo-field"><span>Mã xác minh email</span><input required value={token} onChange={e => setToken(e.target.value)} autoComplete="off" /></label>}
        <button className="button primary" type="submit">{busy ? 'Đang xử lý…' : labels[mode]}</button>
        {mode === 'confirm' && <button className="button outline" type="button" onClick={() => run(async () => { await identity.resend(email); setMessage('Đã yêu cầu gửi lại mã xác minh.') })}>Gửi lại mã</button>}
        <button className="button text-button" type="button" onClick={() => { setMode(mode === 'login' ? 'register' : 'login'); setPassword(''); setToken(''); setError(''); setMessage('') }}>{mode === 'login' ? 'Tạo tài khoản khách hàng' : 'Quay lại đăng nhập'}</button>
      </fieldset>
    </form>}
    {error && <p role="alert">{error}</p>}
    {message && <p role="status">{message}</p>}
    {account && <Profiles key={account.accountId} />}
  </section>
}
