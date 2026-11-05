import { useEffect, useRef, useState } from 'react'
import { ArrowRight, Search, Menu, X, Info, Clock3, MapPin, Tag, ArrowUpRight } from 'lucide-react'
import './App.css'
import Account from './Account'
import Catalog from './Catalog'
import VaccineDetail from './VaccineDetail'
import Registrations from './Registrations'
import VaccinationJourneys from './VaccinationJourneys'
import Home, { centerName, centerAddress, centerHours, centerDays, centerHotline, centerTel, centerMap } from './Home'
import './theme.css'
import './Catalog.css'

const brandName = 'Trung tâm tiêm chủng VNVC'

const pages = [
  ['home', 'Trang chủ'], ['catalog', 'Danh mục'], ['account', 'Hồ sơ tiêm chủng'], ['promotions', 'Ưu đãi'], ['register', 'Đăng ký tiêm'],
  ['vaccinations', 'Tiêm chủng của tôi'],
]

const pageFromHash = () => {
  const raw = window.location.hash.slice(1)
  const path = ({ payment: 'vaccinations', schedule: 'vaccinations', history: 'vaccinations/history' })[raw] ?? raw
  if (path === 'vaccinations/history' || /^vaccinations\/[1-9]\d{0,9}$/.test(path)) return path
  return pages.some(([id]) => id === path) || (path.startsWith('catalog/vaccines/') || path.startsWith('register/recipient/')) ? path : 'home'
}

const offers = [
  { id: 'thang10', title: 'Ưu đãi tiêm chủng tháng 10', code: 'THANG10', discount: 'Giảm 10%', period: '01/10/2026 – 31/10/2026', status: 'Đang áp dụng' },
]

function App() {
  const [account, setAccount] = useState(null)
  const [sessionBusy, setSessionBusy] = useState(false)
  const [page, setPage] = useState(pageFromHash)
  const mainRef = useRef(null)
  const registrationNavigation = useRef(null)
  const returnAfterLogin = useRef(null)
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
      const next = pageFromHash()
      if (registrationBusy || (next.startsWith('register/recipient/') && registrationNavigation.current && !registrationNavigation.current(next))) { window.history.replaceState(null, '', `#${page}`); return }
      if (['#payment', '#schedule', '#history'].includes(window.location.hash)) window.history.replaceState(null, '', `#${next}`)
      setMobileMenuOpen(false)
      setPage(next)
    }
    window.addEventListener('hashchange', navigate)
    return () => window.removeEventListener('hashchange', navigate)
  }, [registrationBusy, page])
  useEffect(() => {
    if (['#payment', '#schedule', '#history'].includes(window.location.hash))
      window.history.replaceState(null, '', `#${page}`)
    mainRef.current?.focus({ preventScroll: true })
    window.scrollTo({ top: 0, behavior: 'instant' })
  }, [page])
  useEffect(() => {
    if (account && returnAfterLogin.current) {
      const next = returnAfterLogin.current; returnAfterLogin.current = null
      setPage(next); window.history.replaceState(null, '', `#${next}`)
    }
  }, [account])
  const go = (next) => { if (registrationBusy || (next.startsWith('register/recipient/') && registrationNavigation.current && !registrationNavigation.current(next))) return; if (!account && next === 'account' && (page.startsWith('register/recipient/') || page === 'vaccinations' || page.startsWith('vaccinations/'))) returnAfterLogin.current = page; setMobileMenuOpen(false); if (next === page) return; setPage(next); window.history.pushState(null, '', `#${next}`) }

  return (
    <div className="app-shell">
      <header className="site-header">
        <button disabled={registrationBusy} className="brand" onClick={() => go('home')} aria-label={`Về trang chủ · ${brandName}`}>
          <picture>
            <source media="(max-width: 900px)" srcSet="/assets/vnvc-logo-square.png" width="447" height="447" />
            <img className="brand-logo" src="/assets/vnvc-logo-horizontal.png" alt="" width="383" height="99" />
          </picture>
          <b>{brandName}</b>
        </button>
        <nav id="main-navigation" className={mobileMenuOpen ? 'main-nav mobile-open' : 'main-nav'} aria-label="Điều hướng chính">
          {pages.map(([id, label]) => <button disabled={registrationBusy} key={id} aria-current={page === id || page.startsWith(`${id}/`) ? 'page' : undefined} className={page === id || page.startsWith(`${id}/`) ? 'nav-link active' : 'nav-link'} onClick={() => go(id)}>{label}</button>)}
        </nav>
        {!account && <button disabled={registrationBusy} className="button outline account-button" onClick={() => go('account')}>Đăng nhập</button>}
        <button disabled={registrationBusy} className="mobile-menu" aria-label={mobileMenuOpen ? 'Đóng điều hướng' : 'Mở điều hướng'} aria-expanded={mobileMenuOpen} aria-controls="main-navigation" onClick={() => setMobileMenuOpen(open => !open)}>{mobileMenuOpen ? <X aria-hidden="true" /> : <Menu aria-hidden="true" />}</button>
      </header>

      <main ref={mainRef} tabIndex={-1}>
        {page === 'promotions' && <div className="wrap registration-message warning" role="status">Mô phỏng · Chức năng này chưa nối nghiệp vụ API/SQL. Không thể hiện kết quả của đăng ký dịch vụ đã lưu.</div>}
        {page === 'account' && <Account account={account} setAccount={setAccount} sessionBusy={sessionBusy} onSessionBusyChange={setSessionBusy} go={go} />}
        {page === 'home' && <Home go={go} setQuery={setQuery} />}
        {page === 'catalog' && <Catalog query={query} setQuery={setQuery} />}
        {page.startsWith('catalog/vaccines/') && <VaccineDetail key={page} id={page.slice('catalog/vaccines/'.length)} go={go} />}
        {page === 'promotions' && <Offers go={go} notify={notify} />}
        {(account || page.startsWith('register')) && <div hidden={!page.startsWith('register')}><Registrations key={account?.accountId || 'guest'} account={account} active={page.startsWith('register')} navigationRef={registrationNavigation} recipientLink={page.startsWith('register/recipient/') ? page.slice('register/recipient/'.length) : null} go={go} onSavingChange={setRegistrationBusy} /></div>}
        {(page === 'vaccinations' || page.startsWith('vaccinations/')) && <VaccinationJourneys key={`${account?.accountId || 'guest'}:${page}`} account={account} sessionBusy={sessionBusy} page={page} go={go} />}
      </main>

      <footer id="lien-he" className="site-footer" aria-label="Thông tin trung tâm VNVC">
        <div className="footer-content wrap">
          <div className="footer-identity">
            <div className="footer-brand"><img className="brand-logo" src="/assets/vnvc-logo-horizontal.png" alt="" width="383" height="99" /><b>{brandName}</b></div>
            <p>Đồng hành cùng bạn trong việc tìm hiểu dịch vụ tiêm chủng và chăm sóc sức khỏe gia đình.</p>
          </div>
          <div className="footer-location">
            <h2><MapPin aria-hidden="true" />{centerName}</h2>
            <address>{centerAddress}</address>
            <a className="footer-map" href={centerMap} target="_blank" rel="noreferrer">Xem đường đi trên bản đồ <ArrowUpRight aria-hidden="true" /></a>
          </div>
          <div className="footer-contact">
            <h2><Clock3 aria-hidden="true" />Giờ làm việc</h2>
            <strong className="footer-hours">{centerHours}</strong>
            <p>{centerDays}</p>
            <p className="footer-hotline">Hotline <a href={centerTel}>{centerHotline}</a></p>
          </div>
        </div>
        <div className="footer-bottom wrap"><span>© {brandName}</span><span>Chủ động hôm nay, an tâm ngày mai.</span></div>
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
