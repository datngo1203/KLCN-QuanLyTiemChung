import { useEffect, useRef, useState } from 'react'
import { ArrowRight, ArrowLeft, Search, Menu, X, Info, Check, UserRound, CalendarDays, History, Clock3, Mail, MapPin, Tag, CircleCheck, ArrowUpRight } from 'lucide-react'
import './App.css'
import Account from './Account'
import Catalog from './Catalog'
import VaccineDetail from './VaccineDetail'
import Registrations from './Registrations'
import Home from './Home'
import './theme.css'
import './Catalog.css'

const pages = [
  ['home', 'Trang chủ'], ['catalog', 'Danh mục'], ['account', 'Hồ sơ'], ['promotions', 'Ưu đãi'], ['register', 'Đăng ký tiêm'],
  ['payment', 'Thanh toán'], ['schedule', 'Lịch tiêm / Lịch sử'],
]

const pageFromHash = () => {
  const path = window.location.hash.slice(1)
  return pages.some(([id]) => id === path) || path.startsWith('catalog/vaccines/') ? path : 'home'
}

const services = [
  { id: 'vaccine-a', name: 'Vắc xin Sởi - Quai bị - Rubella (MMR)', category: 'Vắc xin lẻ', price: '320.000 đ', description: 'Phòng 3 bệnh sởi, quai bị, rubella.', inStock: true },
  { id: 'vaccine-b', name: 'Vắc xin Cúm mùa tứ giá (Vaxigrip Tetra)', category: 'Vắc xin lẻ', price: '380.000 đ', description: 'Phòng ngừa các chủng cúm mùa mới nhất.', inStock: false }, // Hết thuốc -> Test hàng đợi
  { id: 'vaccine-c', name: 'Vắc xin ngừa ung thư cổ tử cung (Gardasil 9)', category: 'Vắc xin lẻ', price: '2.950.000 đ', description: 'Phòng virus HPV và các bệnh đường sinh dục.', inStock: true },
  { id: 'pack-family', name: 'Gói tiêm chủng Trẻ em toàn diện (0-24 tháng)', category: 'Gói vắc xin', price: '12.500.000 đ', description: 'Trọn gói các mũi tiêm cơ bản trong 2 năm đầu đời.', inStock: true },
  { id: 'pack-adult', name: 'Gói tiêm chủng Người lớn & Tiền hôn nhân', category: 'Gói vắc xin', price: '4.800.000 đ', description: 'Các mũi phòng bệnh thiết yếu cho người lớn.', inStock: false }, // Hết gói -> Test hàng đợi
]



const offers = [
  { id: 'thang10', title: 'Ưu đãi tiêm chủng tháng 10', code: 'THANG10', discount: 'Giảm 10%', period: '01/10/2026 – 31/10/2026', status: 'Đang áp dụng' },
]

const initialDraft = {
  customerName: 'Nguyễn An Nhiên', phone: '0900000101', email: 'annhien@example.test',
  identity: '079098001001', address: '12 Trần Hưng Đạo, Hà Nội', samePerson: true,
  recipientName: 'Nguyễn An Nhiên', relation: 'Bản thân', birthDate: '1998-05-12',
  gender: 'Nữ', recipientIdentity: '079098001001', recipients: [], appointmentDate: '2026-10-25', slot: '09:00',
}

function App() {
  const [account, setAccount] = useState(null)
  const [page, setPage] = useState(pageFromHash)
  const mainRef = useRef(null)
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [scheduleTab, setScheduleTab] = useState('Lịch tiêm')
  const [toast, setToast] = useState('')
  const [selectedService] = useState(services[0])
  const draft = initialDraft
  const registration = null
  const [registrationBusy, setRegistrationBusy] = useState(false)
  const [paymentStatus, setPaymentStatus] = useState('Chờ thanh toán')

  const notify = (message) => {
    setToast(message)
    window.clearTimeout(window.__clinicToast)
    window.__clinicToast = window.setTimeout(() => setToast(''), 3200)
  }
  useEffect(() => {
    const navigate = () => {
      if (registrationBusy) { window.history.replaceState(null, '', `#${page}`); return }
      setMobileMenuOpen(false)
      setPage(pageFromHash())
    }
    window.addEventListener('hashchange', navigate)
    return () => window.removeEventListener('hashchange', navigate)
  }, [registrationBusy, page])
  useEffect(() => {
    mainRef.current?.focus({ preventScroll: true })
    window.scrollTo({ top: 0, behavior: 'instant' })
  }, [page])
  const go = (next) => { if (registrationBusy) return; setMobileMenuOpen(false); setPage(next); window.location.hash = next }

  return (
    <div className="app-shell">
      <div className="announcement"><span className="announcement-dot" /> VAC · Cổng thông tin tiêm chủng</div>
      <header className="site-header">
        <button disabled={registrationBusy} className="brand" onClick={() => go('home')} aria-label="Về trang chủ">
          <img className="brand-logo" src="/images/vac-logo.png" alt="VAC" width="64" height="64" /><span><b>TRUNG TÂM TIÊM CHỦNG VAC</b><small>VACCINE ADMINISTRATION CENTER</small></span>
        </button>
        <nav id="main-navigation" className={mobileMenuOpen ? 'main-nav mobile-open' : 'main-nav'} aria-label="Điều hướng chính">
          {pages.map(([id, label]) => <button disabled={registrationBusy} key={id} className={page === id || (id === 'catalog' && page.startsWith('catalog/')) ? 'nav-link active' : 'nav-link'} onClick={() => go(id)}>{label}</button>)}
        </nav>
        <button disabled={registrationBusy} className="button outline account-button" onClick={() => go('account')} aria-label="Tài khoản">{account ? 'Tài khoản' : 'Đăng nhập'}</button>
        <button disabled={registrationBusy} className="mobile-menu" aria-label={mobileMenuOpen ? 'Đóng điều hướng' : 'Mở điều hướng'} aria-expanded={mobileMenuOpen} aria-controls="main-navigation" onClick={() => setMobileMenuOpen(open => !open)}>{mobileMenuOpen ? <X aria-hidden="true" /> : <Menu aria-hidden="true" />}</button>
      </header>

      <main ref={mainRef} tabIndex={-1}>
        {['promotions', 'payment', 'schedule'].includes(page) && <div className="wrap registration-message warning" role="status">Mô phỏng · Chức năng này chưa nối nghiệp vụ API/SQL. Không thể hiện kết quả của đăng ký dịch vụ đã lưu.</div>}
        {page === 'account' && <Account account={account} setAccount={setAccount} />}
        {page === 'home' && <Home go={go} query={query} setQuery={setQuery} />}
        {page === 'catalog' && <Catalog query={query} setQuery={setQuery} />}
        {page.startsWith('catalog/vaccines/') && <VaccineDetail key={page} id={page.slice('catalog/vaccines/'.length)} go={go} />}
        {page === 'promotions' && <Offers go={go} notify={notify} />}
        {(account || page === 'register') && <div hidden={page !== 'register'}><Registrations key={account?.accountId || 'guest'} account={account} active={page === 'register'} go={go} onSavingChange={setRegistrationBusy} /></div>}
        {page === 'payment' && <Payment go={go} selectedService={selectedService} registration={registration} draft={draft} status={paymentStatus} skipIdentityCheck={false} onConfirm={() => { setPaymentStatus('Đã thanh toán'); notify('Đã cập nhật trạng thái thanh toán.') }} />}
        {page === 'schedule' && <Schedule tab={scheduleTab} setTab={setScheduleTab} registration={registration} draft={draft} selectedService={selectedService} paymentStatus={paymentStatus} notify={notify} />}
      </main>

      <footer id="lien-he" className="site-footer" aria-label="Thông tin trung tâm VAC">
        <div className="footer-content wrap">
          <div className="footer-identity">
            <div className="footer-brand"><img className="brand-logo" src="/images/vac-logo.png" alt="VAC" width="76" height="76" /><div><b>TRUNG TÂM<br />TIÊM CHỦNG VAC</b><small>Vaccine Administration Center</small></div></div>
            <p>Đồng hành cùng bạn trong việc tìm hiểu dịch vụ tiêm chủng và chăm sóc sức khỏe gia đình.</p>
          </div>
          <div className="footer-location">
            <h2><MapPin aria-hidden="true" />Đến trung tâm VAC</h2>
            <address>140 Lê Trọng Tấn, Phường Tây Thạnh,<br />Thành phố Hồ Chí Minh</address>
            <a className="footer-map" href="https://www.google.com/maps/search/?api=1&query=140%20L%C3%AA%20Tr%E1%BB%8Dng%20T%E1%BA%A5n%2C%20Ph%C6%B0%E1%BB%9Dng%20T%C3%A2y%20Th%E1%BA%A1nh%2C%20Th%C3%A0nh%20ph%E1%BB%91%20H%E1%BB%93%20Ch%C3%AD%20Minh" target="_blank" rel="noreferrer">Xem đường đi trên bản đồ <ArrowUpRight aria-hidden="true" /></a>
          </div>
          <div className="footer-contact">
            <h2><Clock3 aria-hidden="true" />Giờ làm việc dự kiến</h2>
            <strong className="footer-hours">07:30 – 17:00</strong>
            <p>Thứ Hai – Chủ nhật · Không nghỉ trưa</p>
            <p className="footer-email"><Mail aria-hidden="true" /><span>Email</span>example@email.com</p>
          </div>
        </div>
        <div className="footer-bottom wrap"><span>© VAC · Trung tâm tiêm chủng</span><span>Chủ động hôm nay, an tâm ngày mai.</span></div>
      </footer>
      {toast && <div className="toast" role="status"><span><Info aria-hidden="true" /></span>{toast}<button onClick={() => setToast('')} aria-label="Đóng thông báo"><X aria-hidden="true" /></button></div>}
    </div>
  )
}

function PendingBadge({ children = 'Đang xử lý' }) { if (!children || /minh họa|cần xác nhận|demo/i.test(String(children))) return null; return <span className="pending-badge"><span />{children}</span> }

function PageIntro({ eyebrow, title, text }) { return <div className="page-intro"><div className="eyebrow">{eyebrow}</div><h1>{title}</h1><p>{text}</p></div> }

function Offers({ go, notify }) {
  const [query, setQuery] = useState('')
  const [filter, setFilter] = useState('Tất cả')
  const [selectedOffer, setSelectedOffer] = useState(null)
  const filteredOffers = offers.filter(offer => {
    const matchesQuery = `${offer.title} ${offer.code} ${offer.discount}`.toLocaleLowerCase('vi').includes(query.toLocaleLowerCase('vi'))
    const matchesFilter = filter === 'Tất cả' || offer.status === filter
    return matchesQuery && matchesFilter
  })
  return <section className="offers-page wrap">
    <PageIntro eyebrow="CHƯƠNG TRÌNH ƯU ĐÃI" title="Ưu đãi dành cho bạn" text="Khám phá các chương trình ưu đãi đang áp dụng." />
    <div className="offers-controls"><label className="offer-search"><span><Search aria-hidden="true" /></span><input value={query} onChange={event => setQuery(event.target.value)} placeholder="Tìm ưu đãi hoặc mã" aria-label="Tìm ưu đãi hoặc mã" /></label><div className="filter-tabs" role="tablist" aria-label="Lọc ưu đãi">{['Tất cả', 'Đang áp dụng'].map(item => <button key={item} role="tab" aria-selected={filter === item} className={filter === item ? 'filter-tab selected' : 'filter-tab'} onClick={() => setFilter(item)}>{item}</button>)}</div></div>
    <div className="offers-grid">{filteredOffers.map(offer => <article className="offer-card" key={offer.id}><div className="offer-card-top"><span className="offer-spark"><Tag aria-hidden="true" /></span><span className="offer-status">{offer.status}</span></div><div className="offer-discount">{offer.discount}</div><h2>{offer.title}</h2><div className="offer-code"><span>Mã ưu đãi</span><b>{offer.code}</b></div><div className="offer-period"><span>Thời gian</span><b>{offer.period}</b></div><div className="offer-actions"><button className="button outline" onClick={() => setSelectedOffer(offer)}>Chi tiết</button><button className="button primary" onClick={() => { go('catalog'); notify('Chọn dịch vụ để tiếp tục đăng ký.') }}>Khám phá dịch vụ <ArrowRight aria-hidden="true" /></button></div></article>)}</div>
    {filteredOffers.length === 0 && <div className="empty-state">Không có ưu đãi phù hợp.</div>}
    {selectedOffer && <div className="offer-modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) setSelectedOffer(null) }}><section className="offer-modal" role="dialog" aria-modal="true" aria-labelledby="offer-modal-title"><button className="offer-modal-close" aria-label="Đóng" onClick={() => setSelectedOffer(null)}><X aria-hidden="true" /></button><div className="card-kicker">{selectedOffer.status.toLocaleUpperCase('vi')}</div><h2 id="offer-modal-title">{selectedOffer.title}</h2><div className="offer-modal-discount">{selectedOffer.discount}</div><p>Mã ưu đãi: <b>{selectedOffer.code}</b></p><p>Thời gian áp dụng: <b>{selectedOffer.period}</b></p><button className="button primary" onClick={() => { setSelectedOffer(null); go('catalog'); notify('Chọn dịch vụ để tiếp tục đăng ký.') }}>Khám phá dịch vụ <ArrowRight aria-hidden="true" /></button></section></div>}
  </section>
}

function Detail({ label, value }) { return <div className="detail-item"><span>{label}</span><b className="demo-value">{value}</b></div> }

function IdentityGate({ value, onChange, onSubmit, error, title, description }) {
  return <section className="identity-gate form-card"><div className="identity-gate-icon"><UserRound aria-hidden="true" /></div><div><div className="card-kicker">TRA CỨU HỒ SƠ</div><h2>{title}</h2><p>{description}</p><form onSubmit={onSubmit}><label className="demo-field"><span>CCCD / Mã định danh của người được tiêm hoặc người giám hộ</span><input value={value} onChange={event => onChange(event.target.value)} autoComplete="off" placeholder="Nhập số CCCD hoặc mã định danh" required /></label>{error && <div className="identity-error" role="alert">{error}</div>}<button className="button primary" type="submit">Tiếp tục xem thông tin <ArrowRight aria-hidden="true" /></button></form></div></section>
}

function getRecipientNames(record) {
  if (!record || record.samePerson) return [record?.customerName].filter(Boolean)
  const names = (record.recipients || []).map(person => person.name).filter(Boolean)
  return names.length ? names : [record.recipientName].filter(Boolean)
}

function matchesIdentity(input, registration, draft) {
  const key = String(input).trim().replace(/\s+/g, '').toLocaleLowerCase('vi')
  if (!key) return false
  const record = registration || draft
  const identifiers = [record?.identity, record?.recipientIdentity, ...(record?.samePerson ? [] : (record?.recipients || []).map(person => person.identity))]
  return identifiers.filter(Boolean).some(id => String(id).trim().replace(/\s+/g, '').toLocaleLowerCase('vi') === key)
}

function getRecipientItems(record, fallbackService) {
  if (record && !record.samePerson && record.recipients?.length) return record.recipients.map(person => ({ name: person.name, service: services.find(item => item.id === person.serviceId) || fallbackService }))
  return [{ name: getRecipientNames(record)[0] || '', service: fallbackService }]
}

function formatPrice(value) {
  return `${new Intl.NumberFormat('vi-VN').format(value)} đ`
}

function Payment({ go, selectedService, registration, draft, status, skipIdentityCheck, onConfirm }) {
  const [method, setMethod] = useState('Thanh toán tại quầy')
  const [identity, setIdentity] = useState('')
  const [unlocked, setUnlocked] = useState(false)
  const [error, setError] = useState('')
  const [processing, setProcessing] = useState(false)
  
  // --- B04: State Khuyến mãi ---
  const [promoInput, setPromoInput] = useState('')
  const [appliedPromo, setAppliedPromo] = useState(null)
  const [promoError, setPromoError] = useState('')

  const verify = event => { 
    event.preventDefault()
    if (matchesIdentity(identity, registration, draft)) { 
      setError(''); setUnlocked(true) 
    } else {
      setError('Không tìm thấy hồ sơ phù hợp với mã định danh này.') 
    }
  }

  if (!unlocked && !skipIdentityCheck) {
    return (
      <section className="payment-page wrap">
        <PageIntro eyebrow="HOÀN TẤT ĐĂNG KÝ" title="Thanh toán dịch vụ" text="Nhập mã định danh đã dùng khi đăng ký để tiếp tục." />
        <IdentityGate value={identity} onChange={setIdentity} onSubmit={verify} error={error} title="Xác nhận thông tin hồ sơ" description="Sử dụng mã định danh của người được tiêm hoặc người giám hộ đã đăng ký." />
        <button className="button text-button" onClick={() => go('register')}><ArrowLeft aria-hidden="true" />Quay lại đăng ký</button>
      </section>
    )
  }

  const items = getRecipientItems(registration || draft, selectedService)
  const isWaitlist = !selectedService.inStock
  const peopleAndServices = items.map(item => `${item.name}: ${item.service.name}`).join('; ')

  // --- B04: Logic Tính tiền & Giảm giá ---
  const subtotal = isWaitlist ? 0 : items.reduce((sum, item) => sum + Number(String(item.service.price).replace(/[^\d]/g, '')), 0)
  
  const handleApplyPromo = () => {
    // Tìm mã trong danh sách offers (khai báo ở đầu file App.jsx)
    const found = offers.find(o => o.code.toUpperCase() === promoInput.trim().toUpperCase() && o.status === 'Đang áp dụng')
    if (found) {
      setAppliedPromo(found)
      setPromoError('')
    } else {
      setAppliedPromo(null)
      setPromoError('Mã ưu đãi không hợp lệ, đã hết hạn hoặc không tồn tại.')
    }
  }

  let discountAmount = 0
  if (appliedPromo && !isWaitlist) {
    if (appliedPromo.discount.includes('%')) {
      const percent = parseInt(appliedPromo.discount.replace(/\D/g, '')) || 0
      discountAmount = (subtotal * percent) / 100
    } else {
      discountAmount = parseInt(appliedPromo.discount.replace(/\D/g, '')) || 0
    }
  }
  
  const total = Math.max(0, subtotal - discountAmount)

  const handlePay = () => {
    setProcessing(true)
    setTimeout(() => {
      setProcessing(false)
      onConfirm()
    }, 1500)
  }

  return (
    <section className="payment-page wrap">
      <PageIntro eyebrow="HOÀN TẤT ĐĂNG KÝ" title="Thanh toán & Tài chính" text="Kiểm tra thông tin dịch vụ, áp dụng khuyến mãi và thanh toán." />
      <div className="payment-steps">
        <span><Check aria-hidden="true" />Chọn dịch vụ</span><span><Check aria-hidden="true" />Đăng ký thông tin</span><b>03 Thanh toán</b><span>04 Theo dõi lịch</span>
      </div>
      
      <div className="payment-layout">
        <div className="payment-main">
          <section className="form-card">
            <h2><span>01</span> Dịch vụ và người thụ hưởng</h2>
            <div className="details-grid">
              <Detail label="Khách hàng (Người đăng ký)" value={registration?.customerName || draft.customerName} />
              <Detail label="Chi tiết vắc xin / Dịch vụ" value={peopleAndServices} />
              <Detail label="Thời gian hẹn dự kiến" value={`${draft.appointmentDate} · ${draft.slot}`} />
            </div>
          </section>

          {/* Module B04: Form nhập mã khuyến mãi */}
          <section className="form-card">
            <h2><span>02</span> Khuyến mãi & Ưu đãi</h2>
            {!isWaitlist ? (
              <div className="promo-box">
                <div style={{ display: 'flex', gap: '8px', maxWidth: '400px' }}>
                  <input type="text" value={promoInput} onChange={e => setPromoInput(e.target.value)} placeholder="Nhập mã ưu đãi (VD: THANG10)" style={{ flex: 1, padding: '8px 12px', border: '1px solid #d9d9d9', borderRadius: '4px' }} disabled={appliedPromo || status === 'Đã thanh toán'} />
                  {!appliedPromo ? (
                    <button type="button" className="button outline" onClick={handleApplyPromo} disabled={status === 'Đã thanh toán' || !promoInput.trim()}>Áp dụng</button>
                  ) : (
                    <button type="button" className="button outline" onClick={() => { setAppliedPromo(null); setPromoInput(''); setPromoError('') }} disabled={status === 'Đã thanh toán'}>Hủy mã</button>
                  )}
                </div>
                {promoError && <p style={{ color: '#ff4d4f', fontSize: '0.85rem', marginTop: '6px' }}>{promoError}</p>}
                {appliedPromo && <p style={{ color: '#52c41a', fontSize: '0.85rem', marginTop: '6px' }}><Check aria-hidden="true" />Đã áp dụng: <b>{appliedPromo.title}</b> ({appliedPromo.discount})</p>}
              </div>
            ) : (
              <p className="method-caption">Không áp dụng mã khuyến mãi cho dịch vụ đang trong Hàng đợi.</p>
            )}
          </section>

          <section className="form-card">
            <h2><span>03</span> Hình thức & Phương thức thanh toán</h2>
            {status === 'Đã thanh toán' ? (
              <div className="notice" style={{ background: '#f6ffed', borderColor: '#b7eb8f', marginTop: '12px' }}>
                <span><CircleCheck aria-hidden="true" /></span>
                <div>
                  <b>Giao dịch đã hoàn tất</b>
                  <p>Cảm ơn bạn. Khoản thanh toán đã được ghi nhận vào hệ thống kế toán.</p>
                </div>
              </div>
            ) : (
              <>
                <p className="method-caption">{isWaitlist ? 'Dịch vụ đang chờ hàng đợi (Chưa phát sinh thanh toán trước)' : 'Chọn phương thức thanh toán'}</p>
                <div className="radio-options payment-methods">
                  {['Thanh toán tại quầy', 'Thanh toán điện tử (VNPay/QR)'].filter(item => item === 'Thanh toán tại quầy').map(item => (
                    <label key={item}>
                      <input type="radio" checked={method === item} onChange={() => setMethod(item)} disabled={isWaitlist || processing} /> 
                      {item}
                    </label>
                  ))}
                </div>

                {method === 'Thanh toán điện tử (VNPay/QR)' && !isWaitlist && (
                  <div style={{ marginTop: '20px', padding: '20px', border: '1px dashed #ccc', borderRadius: '8px', textAlign: 'center', background: '#fafafa' }}>
                    <div style={{ width: '150px', height: '150px', background: '#fff', margin: '0 auto 12px', display: 'flex', alignItems: 'center', justifyContent: 'center', border: '4px solid #000', borderRadius: '8px' }}>
                      <span style={{ fontSize: '2rem' }}>📱 QR</span>
                    </div>
                    <b>Quét mã QR bằng ứng dụng ngân hàng</b>
                    <p style={{ margin: '4px 0 0', fontSize: '0.9rem', color: '#666' }}>Hệ thống sẽ tự động xác nhận khi nhận được tiền.</p>
                  </div>
                )}
              </>
            )}
          </section>
          
          {status !== 'Đã thanh toán' && (
            <button className="button text-button" onClick={() => go('register')} disabled={processing}><ArrowLeft aria-hidden="true" />Quay lại đăng ký</button>
          )}
        </div>

        <aside className="summary-card invoice-card" style={{ position: 'relative' }}>
          {status === 'Đã thanh toán' && (
            <div style={{ position: 'absolute', top: '20px', right: '20px', color: 'var(--primary)', border: '2px solid var(--primary)', padding: '4px 8px', borderRadius: '4px', fontWeight: 'bold', transform: 'rotate(15deg)', opacity: 0.8 }}>
              ĐÃ THANH TOÁN
            </div>
          )}
          
          <div className="card-kicker">BIÊN LAI ĐIỆN TỬ</div>
          <h3>Chi tiết thanh toán</h3>
          
          {items.map((item, index) => (
            <div className="invoice-row" key={`${item.name}-${item.service.id}-${index}`}>
              <span>{item.name || 'Người được tiêm'} · {item.service.name}</span>
              <b className="demo-value">{isWaitlist ? '0 đ' : item.service.price}</b>
            </div>
          ))}
          
          <div className="invoice-row" style={{ borderTop: '1px dashed #e2e8f0', paddingTop: '12px', marginTop: '12px' }}>
            <span>Tạm tính</span>
            <b className="demo-value">{formatPrice(subtotal)}</b>
          </div>
          
          {appliedPromo && !isWaitlist && (
            <div className="invoice-row" style={{ color: '#52c41a' }}>
              <span>Khuyến mãi ({appliedPromo.code})</span>
              <b className="demo-value">- {formatPrice(discountAmount)}</b>
            </div>
          )}

          <div className="invoice-row">
            <span>Phương thức</span>
            <b className="demo-value">{isWaitlist ? 'Không yêu cầu' : method}</b>
          </div>
          
          <div className="invoice-row">
            <span>Trạng thái</span>
            <b className="demo-value" style={{ color: status === 'Đã thanh toán' ? 'var(--primary)' : 'inherit' }}>{status}</b>
          </div>
          
          <div className="invoice-total">
            <span>Tổng thanh toán</span>
            <strong>{formatPrice(total)}</strong>
          </div>

          {status === 'Đã thanh toán' || status === 'Đang chờ hàng đợi' ? (
            <button className="button primary full" onClick={() => go('schedule')}>
              Theo dõi lịch <ArrowRight aria-hidden="true" />
            </button>
          ) : (
            <button className="button primary full" onClick={handlePay} disabled={processing}>
              {processing ? 'Đang xử lý giao dịch...' : 'Xác nhận thanh toán'}<ArrowRight aria-hidden="true" />
            </button>
          )}
        </aside>
      </div>
    </section>
  )
}

function Schedule({ tab, setTab, registration, draft, selectedService, paymentStatus, notify }) {
  const [identity, setIdentity] = useState('')
  const [unlocked, setUnlocked] = useState(false)
  const [error, setError] = useState('')
  const verify = event => { event.preventDefault(); if (matchesIdentity(identity, registration, draft)) { setError(''); setUnlocked(true) } else setError('Không tìm thấy hồ sơ phù hợp với mã định danh này.') }
  if (!unlocked) return <section className="schedule-page wrap"><PageIntro eyebrow="THÔNG TIN CÁ NHÂN" title="Theo dõi lịch tiêm" text="Nhập mã định danh đã dùng khi đăng ký để tiếp tục." /><IdentityGate value={identity} onChange={setIdentity} onSubmit={verify} error={error} title="Tra cứu lịch tiêm" description="Nhập mã của người được tiêm hoặc người giám hộ đã đăng ký." /></section>
  const name = registration ? getRecipientNames(registration).join(', ') : 'Trần Gia Bảo'
  const appointment = registration ? { id: 'LH-0004', name, service: getRecipientItems(registration, selectedService).map(item => `${item.name}: ${item.service.name}`).join(' · '), date: draft.appointmentDate, time: draft.slot, status: paymentStatus } : { id: 'LH-0002', name, service: 'Vắc xin B', date: '2026-10-17', time: '10:30', status: 'Đã xác nhận' }
  return <section className="schedule-page wrap"><PageIntro eyebrow="THÔNG TIN CÁ NHÂN" title="Theo dõi lịch tiêm" text="Xem lịch hẹn và lịch sử tiêm." /><div className="schedule-tabs" role="tablist"><button role="tab" aria-selected={tab === 'Lịch tiêm'} className={tab === 'Lịch tiêm' ? 'schedule-tab active' : 'schedule-tab'} onClick={() => setTab('Lịch tiêm')}><CalendarDays aria-hidden="true" />Lịch tiêm</button><button role="tab" aria-selected={tab === 'Lịch sử tiêm'} className={tab === 'Lịch sử tiêm' ? 'schedule-tab active' : 'schedule-tab'} onClick={() => setTab('Lịch sử tiêm')}><History aria-hidden="true" />Lịch sử tiêm</button></div>{tab === 'Lịch tiêm' ? <div className="demo-record-list"><article className="demo-record"><div className="record-date"><b>{appointment.date}</b><span>{appointment.time}</span></div><div className="record-main"><div className="card-kicker">MÃ LỊCH {appointment.id}</div><h3>{appointment.name}</h3><p>{appointment.service}</p><PendingBadge>{appointment.status}</PendingBadge></div><button className="button outline" onClick={() => notify(`Chi tiết ${appointment.id}.`)}>Xem chi tiết</button></article><article className="demo-record" style={{ display: 'none' }}><div className="record-date"><b>2026-10-19</b><span>14:00</span></div><div className="record-main"><div className="card-kicker">MÃ LỊCH LH-0003</div><h3>Phạm Minh Khoa</h3><p>Vắc xin Cúm mùa (Hàng đợi số #03)</p><PendingBadge>Đang chờ hàng đợi</PendingBadge></div><button className="button outline" onClick={() => notify('Đã mở trạng thái hàng đợi.')}>Xem hàng đợi</button></article></div> : <div className="demo-record-list"><article className="demo-record"><div className="record-date"><b>2026-09-10</b><span>Hoàn tất</span></div><div className="record-main"><div className="card-kicker">LS-0001</div><h3>Nguyễn An Nhiên</h3><p>Vắc xin A · Mũi 1</p><PendingBadge>Đã ghi nhận</PendingBadge></div><button className="button outline" onClick={() => notify('Đang mở lịch sử tiêm.')}>Xem lịch sử</button></article></div>}</section>
}
export default App
