import { useEffect, useRef, useState } from 'react'
import { identity } from './api/identity'
import Profiles from './Profiles'
import { LogOut, KeyRound } from 'lucide-react'
import './Profiles.css'

export default function Account({ account, setAccount, sessionBusy = false, onSessionBusyChange = () => {}, go = () => {} }) {
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
  const running = useRef(false)
  const [profileBusy, setProfileBusy] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [feedbackArea, setFeedbackArea] = useState('security')
  const authHeading = useRef(null)
  useEffect(() => { if (!account) authHeading.current?.focus() }, [mode, account])
  function switchMode(next) {
    setMode(next); setPassword(''); setNewPassword(''); setConfirmation(''); setShowPasswords(false)
    setToken(''); setError(''); setMessage('')
  }
  function closePasswordForm() {
    setPasswordFormOpen(false); setShowPasswords(false)
    setPassword(''); setNewPassword(''); setConfirmation(''); setError(''); setMessage('')
    passwordAction.current?.focus()
  }
  function openPasswordForm() {
    if (passwordFormOpen) { closePasswordForm(); return }
    setPassword(''); setError(''); setMessage(''); setPasswordFormOpen(true)
  }
  async function run(action, area = 'security') {
    if (running.current || sessionBusy) return
    running.current = true
    setBusy(true); onSessionBusyChange(true); setError(''); setMessage(''); setFeedbackArea(area)
    try { await action() } catch (failure) {
      const unavailable = failure instanceof TypeError || failure instanceof SyntaxError || ['AbortError', 'TimeoutError'].includes(failure.name)
      setError(unavailable ? 'Không kết nối được dịch vụ. Vui lòng thử lại.' : failure.message || 'Không kết nối được dịch vụ.')
    }
    finally { running.current = false; setBusy(false); onSessionBusyChange(false) }
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
        await identity.register(email, password, confirmation)
        switchMode('confirm')
        setMessage('Đăng ký đã được tiếp nhận. Nhập mã xác minh email để hoàn tất. Khi chạy thử, xem hướng dẫn nhận mã xác minh.')
      } else if (mode === 'forgot') {
        await identity.requestPasswordReset(email)
        switchMode('reset')
        setMessage('Nếu email thuộc tài khoản khách hàng đang hoạt động, hướng dẫn đặt lại mật khẩu sẽ được gửi. Token có hiệu lực 30 phút; yêu cầu mới sẽ vô hiệu token cũ.')
      } else if (mode === 'reset') {
        await identity.confirmPasswordReset(token, newPassword, confirmation)
        switchMode('login'); setAccount(null)
        setMessage('Đã đặt lại mật khẩu và thu hồi các phiên. Vui lòng đăng nhập bằng mật khẩu mới.')
      } else {
        await identity.confirm(token)
        setToken(''); setMode('login'); setMessage('Đã xác minh email. Bạn có thể đăng nhập.')
      }
      setPassword(''); setConfirmation(''); setShowPasswords(false)
    })
  }
  const labels = { login: 'Đăng nhập', register: 'Đăng ký tài khoản', confirm: 'Xác minh email', forgot: 'Quên mật khẩu', reset: 'Đặt lại mật khẩu' }
  return <section className={`account-page wrap ${account ? '' : 'account-auth'}`}>
    <div className="account-page-heading"><div><span className="profile-kicker">TÀI KHOẢN VAC</span><h1 ref={authHeading} tabIndex={-1}>{account ? 'Hồ sơ tiêm chủng' : labels[mode]}</h1><p>{account ? 'Quản lý hồ sơ, người được tiêm và bảo mật tài khoản.' : mode === 'forgot' ? 'Nhập email tài khoản khách hàng để yêu cầu token đặt lại mật khẩu.' : mode === 'reset' ? 'Nhập token nhận qua email và chọn mật khẩu mới.' : 'Đăng nhập để quản lý thông tin tiêm chủng của bạn.'}</p></div>
      {account && <button className="button outline" disabled={busy || sessionBusy || profileBusy} onClick={() => run(async () => { await identity.logout(); closePasswordForm(); setAccount(null); setMessage('Đã đăng xuất.'); setMode('login') }, 'logout')}><LogOut aria-hidden="true" />Đăng xuất</button>}
    </div>
    {account && feedbackArea === 'logout' && error && <p className="profile-feedback error" role="alert">{error}</p>}
    {account ? <>
      <Profiles key={account.accountId} capabilities={account.capabilities} sessionBusy={busy || sessionBusy} onBusyChange={setProfileBusy} go={go} />
      <section className="profile-panel account-security" aria-labelledby="security-title">
        <div className="profile-section-heading"><div><span className="profile-kicker">ĐĂNG NHẬP AN TOÀN</span><h2 id="security-title">Cài đặt &amp; bảo mật</h2><p className="profile-help">Quản lý phương thức đăng nhập và bảo mật của bạn.</p></div>
        <button ref={passwordAction} className="button outline" type="button" aria-expanded={passwordFormOpen} aria-controls="change-password-form" disabled={busy || sessionBusy || profileBusy} onClick={openPasswordForm}><KeyRound aria-hidden="true" />{passwordFormOpen ? 'Đóng biểu mẫu' : 'Đổi mật khẩu'}</button></div>
      {passwordFormOpen && <form id="change-password-form" className="account-password-form" onSubmit={changePassword} aria-busy={busy}>
        <h2>Đổi mật khẩu</h2>
        <p id="password-requirements">Mật khẩu mới 12–128 ký tự, khác mật khẩu hiện tại. Đổi thành công sẽ yêu cầu đăng nhập lại trên mọi thiết bị.</p>
        <fieldset disabled={busy || sessionBusy || profileBusy} style={{ border: 0, padding: 0 }}>
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
        {feedbackArea !== 'logout' && error && <p className="profile-feedback error" role="alert">{error}</p>}
        {message && <p className="profile-feedback success" role="status">{message}</p>}
      </section>
    </> : <form className="form-card" onSubmit={submit} aria-busy={busy}>
      <fieldset disabled={busy || sessionBusy} style={{ border: 0, padding: 0 }}>
        {['login', 'register', 'forgot'].includes(mode) &&
          <label className="demo-field"><span>Email</span><input type="email" autoComplete="username" required maxLength={254} value={email} onChange={e => setEmail(e.target.value)} /></label>
        }
        {['login', 'register'].includes(mode) && <>
          <label className="demo-field"><span>Mật khẩu</span><input type={showPasswords ? 'text' : 'password'} autoComplete={mode === 'register' ? 'new-password' : 'current-password'} required minLength={mode === 'register' ? 12 : undefined} maxLength={128} value={password} onChange={e => setPassword(e.target.value)} /></label>
          {mode === 'register' && <p>Demo: email @example.invalid, mật khẩu 12–128 ký tự.</p>}
        </>}
        {mode === 'confirm' && <label className="demo-field"><span>Mã xác minh email</span><input required maxLength={256} value={token} onChange={e => setToken(e.target.value)} autoComplete="off" /></label>}
        {mode === 'reset' && <>
          <label className="demo-field"><span>Token đặt lại mật khẩu</span><input required maxLength={256} value={token} onChange={e => setToken(e.target.value)} autoComplete="off" spellCheck={false} /></label>
          <label className="demo-field"><span>Mật khẩu mới</span><input type={showPasswords ? 'text' : 'password'} autoComplete="new-password" required minLength={12} maxLength={128} value={newPassword} onChange={e => setNewPassword(e.target.value)} /></label>
          <p>Mật khẩu mới 12–128 ký tự. Đặt lại thành công sẽ đăng xuất các phiên hiện tại.</p>
        </>}
        {['register', 'reset'].includes(mode) && <label className="demo-field"><span>{mode === 'reset' ? 'Xác nhận mật khẩu mới' : 'Xác nhận mật khẩu'}</span><input type={showPasswords ? 'text' : 'password'} autoComplete="new-password" required minLength={12} maxLength={128} value={confirmation} onChange={e => setConfirmation(e.target.value)} /></label>}
        {['login', 'register', 'reset'].includes(mode) && <label className="password-visibility"><input type="checkbox" checked={showPasswords} onChange={e => setShowPasswords(e.target.checked)} />Hiển thị mật khẩu</label>}
        {['forgot', 'reset'].includes(mode) && <p className="profile-help">Demo chỉ dùng email @example.invalid. Token được lưu trong thư mô phỏng local, không gửi email ra ngoài.</p>}
        <div className="account-actions">
          <button className="button primary" type="submit">{busy ? 'Đang xử lý…' : mode === 'forgot' ? 'Gửi yêu cầu' : labels[mode]}</button>
          {mode === 'confirm' && <button className="button outline" type="button" onClick={() => run(async () => { await identity.resend(email); setMessage('Đã yêu cầu gửi lại mã xác minh.') })}>Gửi lại mã</button>}
          {mode === 'login' && <button className="button text-button" type="button" onClick={() => switchMode('forgot')}>Quên mật khẩu</button>}
          {mode === 'reset' && <button className="button outline" type="button" onClick={() => switchMode('forgot')}>Yêu cầu token mới</button>}
          <button className="button text-button" type="button" onClick={() => switchMode(mode === 'login' ? 'register' : 'login')}>{mode === 'login' ? 'Tạo tài khoản khách hàng' : 'Quay lại đăng nhập'}</button>
        </div>
      </fieldset>
    </form>}
    {!account && error && <p className="profile-feedback error" role="alert">{error}</p>}
    {!account && message && <p className="profile-feedback success" role="status">{message}</p>}
    {!account && <p className="profile-help">Hệ thống mô phỏng phục vụ khóa luận – không phải website chính thức VNVC.</p>}
  </section>
}
