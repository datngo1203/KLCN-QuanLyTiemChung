import { useEffect, useRef, useState } from 'react'
import { ArrowRight, Search, Menu, X, Info, Clock3, Mail, MapPin, Tag, ArrowUpRight } from 'lucide-react'
import './App.css'
import Account from './Account'
import Catalog from './Catalog'
import VaccineDetail from './VaccineDetail'
import Registrations from './Registrations'
import Payments from './Payments'
import Schedule from './Schedule'
import Home from './Home'
import './theme.css'
import './Catalog.css'

const pages = [
  ['home', 'Trang chủ'], ['catalog', 'Danh mục'], ['account', 'Hồ sơ'], ['promotions', 'Ưu đãi'], ['register', 'Đăng ký tiêm'],
  ['payment', 'Thanh toán'], ['schedule', 'Lịch hẹn'],
]

const pageFromHash = () => {
  const path = window.location.hash.slice(1)
  return pages.some(([id]) => id === path) || path.startsWith('catalog/vaccines/') ? path : 'home'
}

const offers = [
  { id: 'thang10', title: 'Ưu đãi tiêm chủng tháng 10', code: 'THANG10', discount: 'Giảm 10%', period: '01/10/2026 – 31/10/2026', status: 'Đang áp dụng' },
]

function App() {
  const [account, setAccount] = useState(null)
  const [sessionBusy, setSessionBusy] = useState(false)
  const [page, setPage] = useState(pageFromHash)
  const mainRef = useRef(null)
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [toast, setToast] = useState('')
  const [registrationBusy, setRegistrationBusy] = useState(false)

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
        {page === 'promotions' && <div className="wrap registration-message warning" role="status">Mô phỏng · Chức năng này chưa nối nghiệp vụ API/SQL. Không thể hiện kết quả của đăng ký dịch vụ đã lưu.</div>}
        {page === 'account' && <Account account={account} setAccount={setAccount} sessionBusy={sessionBusy} onSessionBusyChange={setSessionBusy} />}
        {page === 'home' && <Home go={go} query={query} setQuery={setQuery} />}
        {page === 'catalog' && <Catalog query={query} setQuery={setQuery} />}
        {page.startsWith('catalog/vaccines/') && <VaccineDetail key={page} id={page.slice('catalog/vaccines/'.length)} go={go} />}
        {page === 'promotions' && <Offers go={go} notify={notify} />}
        {(account || page === 'register') && <div hidden={page !== 'register'}><Registrations key={account?.accountId || 'guest'} account={account} active={page === 'register'} go={go} onSavingChange={setRegistrationBusy} /></div>}
        {page === 'payment' && <Payments key={account?.accountId || 'guest'} account={account} go={go} />}
        {page === 'schedule' && <Schedule key={account?.accountId || 'guest'} account={account} sessionBusy={sessionBusy} go={go} />}
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

export default App
