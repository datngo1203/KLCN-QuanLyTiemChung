import { useMemo, useState } from 'react'
import './App.css'
import ApiProbe from './ApiProbe'
import './theme.css'

const pages = [
  ['home', 'Trang chủ'], ['catalog', 'Danh mục'], ['promotions', 'Ưu đãi'], ['register', 'Đăng ký tiêm'],
  ['payment', 'Thanh toán'], ['schedule', 'Lịch tiêm / Lịch sử'],
]

const services = [
  { id: 'type-a', name: 'Loại tiêm chủng A', category: 'Loại tiêm chủng', icon: '✚', price: '450.000 đ', description: 'Nhóm dịch vụ tiêm chủng.' },
  { id: 'vaccine-a', name: 'Vắc xin A', category: 'Vắc xin', icon: '♧', price: '450.000 đ', description: 'Vắc xin dùng trong danh mục.' },
  { id: 'pack-a', name: 'Gói tiêm Gia đình', category: 'Loại tiêm chủng', icon: '▣', price: '1.200.000 đ', description: 'Gói dịch vụ dành cho gia đình.' },
  { id: 'vaccine-b', name: 'Vắc xin B', category: 'Vắc xin', icon: '♧', price: '620.000 đ', description: 'Vắc xin dùng trong danh mục.' },
  { id: 'type-b', name: 'Loại tiêm chủng B', category: 'Loại tiêm chủng', icon: '✚', price: '300.000 đ', description: 'Nhóm dịch vụ tiêm chủng.' },
  { id: 'vaccine-c', name: 'Vắc xin C', category: 'Vắc xin', icon: '♧', price: '510.000 đ', description: 'Vắc xin dùng trong danh mục.' },
]

const diseaseVaccineInfo = [
  { id: 'measles', disease: 'Sởi', vaccine: 'Vắc xin sởi; vắc xin phối hợp MR hoặc MMR', source: 'https://www.who.int/teams/immunization-vaccines-and-biologicals/diseases/measles' },
  { id: 'influenza', disease: 'Cúm mùa', vaccine: 'Vắc xin cúm mùa', source: 'https://www.who.int/news-room/fact-sheets/detail/influenza-(seasonal)' },
  { id: 'hpv', disease: 'Nhiễm HPV', vaccine: 'Vắc xin HPV', source: 'https://www.who.int/teams/immunization-vaccines-and-biologicals/diseases/human-papillomavirus-vaccines-%28HPV%29/hpv-clearing-house/vaccines-safety' },
  { id: 'diphtheria', disease: 'Bạch hầu, ho gà và uốn ván', vaccine: 'Vắc xin phối hợp có thành phần phòng các bệnh tương ứng', source: 'https://www.who.int/teams/immunization-vaccines-and-biologicals/essential-programme-on-immunization' },
]

const offers = [
  { id: 'thang10', title: 'Ưu đãi tiêm chủng tháng 10', code: 'THANG10', discount: 'Giảm 10%', period: '01/10/2026 – 31/10/2026', status: 'Đang áp dụng' },
]

const initialDraft = {
  customerName: 'Nguyễn An Nhiên', phone: '0900 000 101', email: 'annhien@example.test',
  identity: 'ID-1001', address: '12 Trần Hưng Đạo, Hà Nội', samePerson: true,
  recipientName: 'Nguyễn An Nhiên', relation: 'Chính khách hàng', birthDate: '1998-05-12',
  gender: 'Nữ', recipientIdentity: 'ID-1001', recipients: [], appointmentDate: '2026-10-16', slot: '09:00',
}

function App() {
  const [page, setPage] = useState('home')
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [filter, setFilter] = useState('Tất cả')
  const [scheduleTab, setScheduleTab] = useState('Lịch tiêm')
  const [toast, setToast] = useState('')
  const [selectedService, setSelectedService] = useState(services[1])
  const [draft, setDraft] = useState(initialDraft)
  const [registration, setRegistration] = useState(null)
  const [paymentStatus, setPaymentStatus] = useState('Chờ thanh toán')
  const [paymentFromRegistration, setPaymentFromRegistration] = useState(false)

  const notify = (message) => {
    setToast(message)
    window.clearTimeout(window.__clinicToast)
    window.__clinicToast = window.setTimeout(() => setToast(''), 3200)
  }
  const go = (next) => { if (next === 'payment') setPaymentFromRegistration(false); setMobileMenuOpen(false); setPage(next); window.scrollTo({ top: 0, behavior: 'smooth' }) }
  const chooseService = (service) => { setSelectedService(service); go('register') }

  return (
    <div className="app-shell">
      <div className="announcement"><span className="announcement-dot" /> Cổng thông tin tiêm chủng <ApiProbe /></div>
      <header className="site-header">
        <button className="brand" onClick={() => go('home')} aria-label="Về trang chủ">
          <span className="brand-mark">✚</span><span><b>TRUNG TÂM TIÊM CHỦNG</b><small>CỔNG THÔNG TIN KHÁCH HÀNG</small></span>
        </button>
        <nav id="main-navigation" className={mobileMenuOpen ? 'main-nav mobile-open' : 'main-nav'} aria-label="Điều hướng chính">
          {pages.map(([id, label]) => <button key={id} className={page === id ? 'nav-link active' : 'nav-link'} onClick={() => go(id)}>{label}</button>)}
        </nav>
        <button className="icon-button account-button" onClick={() => { go('schedule'); notify('Đang mở hồ sơ của bạn.') }} aria-label="Tài khoản">♙</button>
        <button className="mobile-menu" aria-label={mobileMenuOpen ? 'Đóng điều hướng' : 'Mở điều hướng'} aria-expanded={mobileMenuOpen} aria-controls="main-navigation" onClick={() => setMobileMenuOpen(open => !open)}>{mobileMenuOpen ? '×' : '☰'}</button>
      </header>

      <main>
        {page === 'home' && <Home go={go} query={query} setQuery={setQuery} notify={notify} />}
        {page === 'catalog' && <Catalog query={query} setQuery={setQuery} filter={filter} setFilter={setFilter} chooseService={chooseService} notify={notify} />}
        {page === 'promotions' && <Offers go={go} notify={notify} />}
        {page === 'register' && <Register go={go} notify={notify} selectedService={selectedService} draft={draft} setDraft={setDraft} onSubmit={() => { setRegistration(draft); setPaymentStatus('Chờ thanh toán'); setPaymentFromRegistration(true); setPage('payment'); window.scrollTo({ top: 0, behavior: 'smooth' }) }} />}
        {page === 'payment' && <Payment go={go} selectedService={selectedService} registration={registration} draft={draft} status={paymentStatus} skipIdentityCheck={paymentFromRegistration} onConfirm={() => { setPaymentStatus('Đã thanh toán'); notify('Đã cập nhật trạng thái thanh toán.') }} />}
        {page === 'schedule' && <Schedule tab={scheduleTab} setTab={setScheduleTab} registration={registration} draft={draft} selectedService={selectedService} paymentStatus={paymentStatus} notify={notify} />}
      </main>

      <footer className="site-footer">
        <div className="footer-brand"><span className="brand-mark">✚</span><div><b>TRUNG TÂM TIÊM CHỦNG</b><p>Cổng thông tin dịch vụ tiêm chủng</p></div></div>

        <div className="footer-links">{pages.map(([id, label]) => <button key={id} onClick={() => go(id)}>{label}</button>)}</div>
      </footer>
      {toast && <div className="toast" role="status"><span>ⓘ</span>{toast}<button onClick={() => setToast('')} aria-label="Đóng thông báo">×</button></div>}
    </div>
  )
}

function SearchBox({ query, setQuery, onSearch }) {
  return <form className="search-box" onSubmit={(event) => { event.preventDefault(); onSearch?.() }}><span>⌕</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Tìm loại tiêm chủng hoặc vắc xin" aria-label="Tìm loại tiêm chủng hoặc vắc xin" /><button type="submit" className="button primary">Tìm kiếm</button></form>
}
function PendingBadge({ children = 'Đang xử lý' }) { if (!children || /minh họa|cần xác nhận|demo/i.test(String(children))) return null; return <span className="pending-badge"><span />{children}</span> }

function Home({ go, query, setQuery, notify }) {
  return <>
    <section className="hero wrap">
      <div className="hero-copy">
        <div className="eyebrow"><span>✦</span> AN TÂM CHỦ ĐỘNG BẢO VỆ SỨC KHỎE</div>
        <h1>Chủ động tiêm chủng,<br /><em>an tâm mỗi ngày.</em></h1>
        <p className="hero-lead">Tra cứu thông tin, tìm hiểu dịch vụ và đăng ký tiêm chủng trên cổng thông tin của Trung tâm.</p>
        <SearchBox query={query} setQuery={setQuery} onSearch={() => go('catalog')} />
        <div className="hero-actions"><button className="button primary" onClick={() => go('catalog')}>Khám phá danh mục <span>→</span></button><button className="button text-button" onClick={() => go('register')}>Đăng ký tiêm <span>↗</span></button></div>
        <div className="hero-assurance"><span>✓</span> Quyết định chỉ định tiêm thuộc quy trình khám sàng lọc tại trung tâm.</div>
      </div>
      <div className="hero-art" aria-label="Hình ảnh chăm sóc sức khỏe"><div className="art-orbit orbit-one" /><div className="art-orbit orbit-two" /><div className="art-sun" /><div className="art-cross">✚</div><div className="art-card card-top"><span className="tiny-icon blue">✚</span><span>Tiêm chủng<br /><b>chủ động</b></span><span className="green-check">✓</span></div><div className="art-card card-bottom"><span className="heart">♡</span><span>Sức khỏe<br /><b>được quan tâm</b></span></div><div className="art-person"><div className="person-head" /><div className="person-body" /><div className="person-arm" /></div><div className="art-leaf leaf-a">❋</div><div className="art-leaf leaf-b">✦</div></div>
    </section>
    <section className="trust-strip"><div className="wrap trust-items"><span><i>⌕</i> Tra cứu danh mục</span><span><i>♡</i> Chủ động lựa chọn</span><span><i>▦</i> Theo dõi lịch hẹn</span><span><i>✚</i> Chăm sóc sức khỏe</span></div></section>
    <section className="section wrap">
      <div className="section-heading"><div><div className="eyebrow">DỊCH VỤ TIÊM CHỦNG</div><h2>Khám phá danh mục dịch vụ</h2><p>Chọn một dịch vụ để xem thông tin và đăng ký.</p></div><button className="button outline" onClick={() => go('catalog')}>Xem danh mục <span>→</span></button></div>
      <div className="service-grid">
        <ServiceCard icon="✚" title="Loại tiêm chủng A" note="Nhóm dịch vụ tiêm chủng" onClick={() => go('catalog')} />
        <ServiceCard icon="♧" title="Vắc xin A" note="Thông tin vắc xin" onClick={() => go('catalog')} />
        <ServiceCard icon="▣" title="Gói tiêm Gia đình" note="Gói dịch vụ tiêm chủng" onClick={() => go('catalog')} />
      </div>

    </section>
    <section className="section process-section"><div className="wrap"><div className="section-heading centered"><div><div className="eyebrow">HÀNH TRÌNH TRÊN CỔNG THÔNG TIN</div><h2>Các bước trải nghiệm dịch vụ</h2><p>Thực hiện lần lượt các bước để sử dụng dịch vụ.</p></div></div><div className="process-grid"><ProcessStep n="01" icon="⌕" title="Khám phá danh mục" onClick={() => go('catalog')} /><ProcessStep n="02" icon="▤" title="Đăng ký tiêm" onClick={() => go('register')} /><ProcessStep n="03" icon="▣" title="Thanh toán" onClick={() => go('payment')} /><ProcessStep n="04" icon="◷" title="Theo dõi lịch" onClick={() => go('schedule')} /></div></div></section>
    <section className="support-banner wrap"><div className="support-icon">♡</div><div><b>Bạn cần tìm hiểu thêm?</b><p>Tra cứu các kênh hỗ trợ của Trung tâm.</p></div><button className="button outline" onClick={() => notify('Thông tin hỗ trợ sẽ sớm được cập nhật.')}>Thông tin hỗ trợ <span>→</span></button></section>
  </>
}

function ServiceCard({ icon, title, note, onClick }) { return <button className="service-card" onClick={onClick}><span className="service-icon">{icon}</span><span className="service-title">{title}</span><span className="service-note">{note}</span><PendingBadge>Minh họa</PendingBadge><span className="card-arrow">↗</span></button> }
function ProcessStep({ n, icon, title, onClick }) { return <button className="process-step" onClick={onClick}><span className="step-number">{n}</span><span className="step-icon">{icon}</span><b>{title}</b><small>Thao tác nhanh</small></button> }
function PageIntro({ eyebrow, title, text }) { return <div className="page-intro"><div className="eyebrow">{eyebrow}</div><h1>{title}</h1><p>{text}</p></div> }

function Catalog({ query, setQuery, filter, setFilter, chooseService, notify }) {
  const items = useMemo(() => services.filter((item) => (filter === 'Tất cả' || item.category === filter) && `${item.name} ${item.category}`.toLocaleLowerCase('vi').includes(query.toLocaleLowerCase('vi'))), [filter, query])
  const diseaseItems = useMemo(() => diseaseVaccineInfo.filter(item => `${item.disease} ${item.vaccine}`.toLocaleLowerCase('vi').includes(query.toLocaleLowerCase('vi'))), [query])
  const tabs = ['Tất cả', 'Loại tiêm chủng', 'Vắc xin', 'Bệnh & vắc xin']
  return <section className="catalog-page wrap"><PageIntro eyebrow="DANH MỤC DỊCH VỤ" title="Danh mục loại tiêm chủng và vắc xin" text="Tra cứu các nhóm dịch vụ và thông tin liên quan." /><div className="notice"><span>ⓘ</span><div><b>Thông tin tham khảo</b><p>Danh mục hỗ trợ tra cứu. Quyết định chỉ định tiêm thuộc quy trình khám sàng lọc tại trung tâm.</p></div></div><div className="catalog-controls"><SearchBox query={query} setQuery={setQuery} onSearch={() => {}} /><div className="filter-tabs" role="tablist" aria-label="Lọc danh mục">{tabs.map((name) => <button role="tab" aria-selected={filter === name} className={filter === name ? 'filter-tab selected' : 'filter-tab'} key={name} onClick={() => setFilter(name)}>{name}</button>)}</div></div>{filter === 'Bệnh & vắc xin' ? <div className="disease-grid">{diseaseItems.map(item => <article className="disease-card" key={item.id}><span className="disease-icon">✚</span><div className="card-kicker">BỆNH CÓ THỂ PHÒNG NGỪA</div><h3>{item.disease}</h3><div className="disease-arrow">↓</div><div className="disease-vaccine"><span>Vắc xin liên quan</span><b>{item.vaccine}</b></div><a href={item.source} target="_blank" rel="noreferrer">Nguồn WHO ↗</a></article>)}</div> : <div className="catalog-grid">{items.map((item, index) => <article className="catalog-card" key={item.id}><div className={`catalog-art art-${index % 3}`}><span>{item.icon}</span></div><div className="catalog-info"><div className="card-kicker">{item.category.toLocaleUpperCase('vi')}</div><h3>{item.name}</h3><p>{item.description}</p><div className="demo-price">{item.price}</div><div className="catalog-actions"><button className="button outline" onClick={() => { chooseService(item); notify(`Thông tin ${item.name}.`) }}>Xem chi tiết</button><button className="button primary" onClick={() => chooseService(item)}>Đăng ký tiêm</button></div></div></article>)}</div>}{filter === 'Bệnh & vắc xin' && diseaseItems.length === 0 && <div className="empty-state">Không có mục phù hợp với nội dung tìm kiếm.</div>}{filter !== 'Bệnh & vắc xin' && items.length === 0 && <div className="empty-state">Không có mục phù hợp với nội dung tìm kiếm.</div>}</section>
}

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
    <div className="offers-controls"><label className="offer-search"><span>⌕</span><input value={query} onChange={event => setQuery(event.target.value)} placeholder="Tìm ưu đãi hoặc mã" aria-label="Tìm ưu đãi hoặc mã" /></label><div className="filter-tabs" role="tablist" aria-label="Lọc ưu đãi">{['Tất cả', 'Đang áp dụng'].map(item => <button key={item} role="tab" aria-selected={filter === item} className={filter === item ? 'filter-tab selected' : 'filter-tab'} onClick={() => setFilter(item)}>{item}</button>)}</div></div>
    <div className="offers-grid">{filteredOffers.map(offer => <article className="offer-card" key={offer.id}><div className="offer-card-top"><span className="offer-spark">✦</span><span className="offer-status">{offer.status}</span></div><div className="offer-discount">{offer.discount}</div><h2>{offer.title}</h2><div className="offer-code"><span>Mã ưu đãi</span><b>{offer.code}</b></div><div className="offer-period"><span>Thời gian</span><b>{offer.period}</b></div><div className="offer-actions"><button className="button outline" onClick={() => setSelectedOffer(offer)}>Chi tiết</button><button className="button primary" onClick={() => { go('catalog'); notify('Chọn dịch vụ để tiếp tục đăng ký.') }}>Khám phá dịch vụ <span>→</span></button></div></article>)}</div>
    {filteredOffers.length === 0 && <div className="empty-state">Không có ưu đãi phù hợp.</div>}
    {selectedOffer && <div className="offer-modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) setSelectedOffer(null) }}><section className="offer-modal" role="dialog" aria-modal="true" aria-labelledby="offer-modal-title"><button className="offer-modal-close" aria-label="Đóng" onClick={() => setSelectedOffer(null)}>×</button><div className="card-kicker">{selectedOffer.status.toLocaleUpperCase('vi')}</div><h2 id="offer-modal-title">{selectedOffer.title}</h2><div className="offer-modal-discount">{selectedOffer.discount}</div><p>Mã ưu đãi: <b>{selectedOffer.code}</b></p><p>Thời gian áp dụng: <b>{selectedOffer.period}</b></p><button className="button primary" onClick={() => { setSelectedOffer(null); go('catalog'); notify('Chọn dịch vụ để tiếp tục đăng ký.') }}>Khám phá dịch vụ <span>→</span></button></section></div>}
  </section>
}

function Field({ label, value, onChange, placeholder, type = 'text', required = false }) {
  return <label className="demo-field"><span>{label}{required && <b> *</b>}</span><input type={type} value={value} onChange={onChange} placeholder={placeholder} required={required} /></label>
}

function Register({ go, notify, selectedService, draft, setDraft, onSubmit }) {
  const update = (key, value) => setDraft(current => ({ ...current, [key]: value }))
  const recipients = draft.recipients || []
  const createRecipient = () => ({ id: `${Date.now()}-${Math.random()}`, name: '', relation: '', birthDate: '', gender: 'Nữ', identity: '', serviceId: selectedService.id })
  const updateRecipient = (id, key, value) => update('recipients', recipients.map(person => person.id === id ? { ...person, [key]: value } : person))
  const addRecipient = () => update('recipients', [...recipients, createRecipient()])
  const removeRecipient = id => update('recipients', recipients.filter(person => person.id !== id))
  const chooseRelatives = () => {
    if (!recipients.length) update('recipients', [createRecipient()])
    update('samePerson', false)
  }
  const recipientSummary = draft.samePerson ? draft.customerName : recipients.map(person => {
    const service = services.find(item => item.id === person.serviceId) || selectedService
    return person.name ? `${person.name} · ${service.name}` : ''
  }).filter(Boolean).join(', ')
  const handleSubmit = event => { event.preventDefault(); onSubmit(); notify('Đã tiếp nhận đăng ký. Tiếp tục đến bước thanh toán.') }

  return <section className="form-page wrap">
    <PageIntro eyebrow="CỔNG TIẾP NHẬN TRỰC TUYẾN" title="Đăng ký tiêm chủng" text="Điền thông tin đăng ký." />
    <div className="stepper"><span className="step"><i>✓</i> Chọn dịch vụ</span><span className="step current"><i>2</i> Đăng ký thông tin</span><span className="step"><i>3</i> Thanh toán</span><span className="step"><i>4</i> Theo dõi lịch</span></div>
    <form className="form-layout" onSubmit={handleSubmit}>
      <div className="form-content">
        <section className="form-card"><h2><span>01</span> Dịch vụ / vắc xin đã chọn</h2><div className="details-grid"><Detail label="Dịch vụ / vắc xin" value={selectedService.name} /><Detail label="Loại danh mục" value={selectedService.category} /></div><button type="button" className="button outline" onClick={() => go('catalog')}>← Thay đổi dịch vụ</button></section>
        <section className="form-card"><h2><span>02</span> Thông tin Khách hàng (Người đăng ký)</h2><div className="demo-form-grid"><Field label="Họ và tên khách hàng" value={draft.customerName} onChange={event => update('customerName', event.target.value)} required /><Field label="Số điện thoại liên hệ" value={draft.phone} onChange={event => update('phone', event.target.value)} required /><Field label="Địa chỉ Email" value={draft.email} onChange={event => update('email', event.target.value)} type="email" /><Field label="Số CCCD / Định danh cá nhân" value={draft.identity} onChange={event => update('identity', event.target.value)} /><Field label="Địa chỉ thường trú / Liên hệ" value={draft.address} onChange={event => update('address', event.target.value)} /></div></section>
        <section className="form-card">
          <h2><span>03</span> Thông tin Người được tiêm</h2>
          <div className="radio-options"><label><input type="radio" checked={draft.samePerson} onChange={() => update('samePerson', true)} /> Người được tiêm là chính khách hàng</label><label><input type="radio" checked={!draft.samePerson} onChange={chooseRelatives} /> Đăng ký cho người thân</label></div>
          {!draft.samePerson && <div className="recipient-list">{recipients.map((person, index) => <div className="recipient-card" key={person.id}>
            <div className="recipient-card-heading"><h3>Người được tiêm {index + 1}</h3>{recipients.length > 1 && <button type="button" className="recipient-remove" onClick={() => removeRecipient(person.id)}>Xóa người này</button>}</div>
            <div className="demo-form-grid"><Field label="Họ tên người được tiêm" value={person.name} onChange={event => updateRecipient(person.id, 'name', event.target.value)} required /><label className="demo-field"><span>Vắc xin / dịch vụ đăng ký</span><select value={person.serviceId || selectedService.id} onChange={event => updateRecipient(person.id, 'serviceId', event.target.value)}>{services.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label><Field label="Quan hệ với khách hàng" value={person.relation} onChange={event => updateRecipient(person.id, 'relation', event.target.value)} /><Field label="Ngày sinh" value={person.birthDate} onChange={event => updateRecipient(person.id, 'birthDate', event.target.value)} type="date" /><label className="demo-field"><span>Giới tính</span><select value={person.gender} onChange={event => updateRecipient(person.id, 'gender', event.target.value)}><option>Nữ</option><option>Nam</option><option>Khác</option></select></label><Field label="Số CCCD / Mã định danh / Giấy khai sinh" value={person.identity} onChange={event => updateRecipient(person.id, 'identity', event.target.value)} /></div>
          </div>)}<button type="button" className="button outline add-recipient" onClick={addRecipient}><span aria-hidden="true">＋</span> Thêm người được tiêm</button></div>}
        </section>
        <section className="form-card"><h2><span>04</span> Chọn lịch hẹn (Ngày & Khung giờ)</h2><div className="demo-form-grid"><Field label="Chọn ngày hẹn" value={draft.appointmentDate} onChange={event => update('appointmentDate', event.target.value)} type="date" required /><label className="demo-field"><span>Khung giờ hẹn <b>*</b></span><select value={draft.slot} onChange={event => update('slot', event.target.value)}><option>09:00</option><option>10:30</option><option>14:00</option></select></label></div></section>
      </div>
      <aside className="summary-card"><div className="card-kicker">TÓM TẮT ĐĂNG KÝ</div><h3>Kiểm tra thông tin</h3><SummaryRow label="Dịch vụ / vắc xin" value={selectedService.name} /><SummaryRow label="Người đăng ký" value={draft.customerName || 'Chưa nhập'} /><SummaryRow label="Người được tiêm" value={recipientSummary || 'Chưa nhập'} /><SummaryRow label="Thời gian dự kiến" value={`${draft.appointmentDate} · ${draft.slot}`} /><div className="summary-total"><span>Trạng thái</span><PendingBadge>Đã tiếp nhận</PendingBadge></div><button className="button primary full" type="submit">Tiếp tục đăng ký <span>→</span></button></aside>
    </form>
  </section>
}function SummaryRow({ label, value }) { return <div className="summary-row"><span>{label}</span><b className="demo-value">{value}</b></div> }
function Detail({ label, value }) { return <div className="detail-item"><span>{label}</span><b className="demo-value">{value}</b></div> }

function IdentityGate({ value, onChange, onSubmit, error, title, description }) {
  return <section className="identity-gate form-card"><div className="identity-gate-icon">♙</div><div><div className="card-kicker">TRA CỨU HỒ SƠ</div><h2>{title}</h2><p>{description}</p><form onSubmit={onSubmit}><label className="demo-field"><span>CCCD / Mã định danh của người được tiêm hoặc người giám hộ</span><input value={value} onChange={event => onChange(event.target.value)} autoComplete="off" placeholder="Nhập số CCCD hoặc mã định danh" required /></label>{error && <div className="identity-error" role="alert">{error}</div>}<button className="button primary" type="submit">Tiếp tục xem thông tin <span>→</span></button></form></div></section>
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
  const verify = event => { event.preventDefault(); if (matchesIdentity(identity, registration, draft)) { setError(''); setUnlocked(true) } else setError('Không tìm thấy hồ sơ phù hợp với mã định danh này.') }
  if (!unlocked && !skipIdentityCheck) return <section className="payment-page wrap"><PageIntro eyebrow="HOÀN TẤT ĐĂNG KÝ" title="Thanh toán đăng ký tiêm" text="Nhập mã định danh đã dùng khi đăng ký để tiếp tục." /><IdentityGate value={identity} onChange={setIdentity} onSubmit={verify} error={error} title="Xác nhận thông tin hồ sơ" description="Sử dụng mã định danh của người được tiêm hoặc người giám hộ đã đăng ký." /><button className="button text-button" onClick={() => go('register')}>← Quay lại đăng ký</button></section>
  const items = getRecipientItems(registration || draft, selectedService)
  const total = items.reduce((sum, item) => sum + Number(String(item.service.price).replace(/[^\d]/g, '')), 0)
  const peopleAndServices = items.map(item => `${item.name}: ${item.service.name}`).join('; ')
  return <section className="payment-page wrap"><PageIntro eyebrow="HOÀN TẤT ĐĂNG KÝ" title="Thanh toán đăng ký tiêm" text="Kiểm tra thông tin đăng ký và chọn phương thức thanh toán." /><div className="payment-steps"><span>✓ Chọn dịch vụ</span><span>✓ Đăng ký thông tin</span><b>03 Thanh toán</b><span>04 Theo dõi lịch</span></div><div className="payment-layout"><div className="payment-main"><section className="form-card"><h2><span>01</span> Dịch vụ và người thụ hưởng</h2><div className="details-grid"><Detail label="Khách hàng (người đăng ký)" value={registration?.customerName || draft.customerName} /><Detail label="Người được tiêm · vắc xin / dịch vụ" value={peopleAndServices} /><Detail label="Thời gian hẹn dự kiến" value={`${draft.appointmentDate} · ${draft.slot}`} /></div></section><section className="form-card"><h2><span>02</span> Hướng dẫn và phương thức thanh toán</h2><p className="method-caption">Chọn phương thức thanh toán</p><div className="radio-options payment-methods">{['Thanh toán tại quầy', 'Thanh toán điện tử'].map(item => <label key={item}><input type="radio" checked={method === item} onChange={() => setMethod(item)} /> {item}</label>)}</div></section><button className="button text-button" onClick={() => go('register')}>← Quay lại đăng ký</button></div><aside className="summary-card invoice-card"><div className="card-kicker">PHIẾU TỔNG KẾT</div><h3>Chi tiết thanh toán</h3>{items.map((item, index) => <div className="invoice-row" key={`${item.name}-${item.service.id}-${index}`}><span>{item.name || 'Người được tiêm'} · {item.service.name}</span><b className="demo-value">{item.service.price}</b></div>)}<div className="invoice-row"><span>Phương thức</span><b className="demo-value">{method}</b></div><div className="invoice-row"><span>Trạng thái</span><b className="demo-value">{status}</b></div><div className="invoice-total"><span>Tổng thanh toán</span><strong>{formatPrice(total)}</strong></div>{status === 'Đã thanh toán' ? <button className="button primary full" onClick={() => go('schedule')}>Theo dõi lịch <span>→</span></button> : <button className="button primary full" onClick={onConfirm}>Xác nhận thanh toán <span>→</span></button>}</aside></div></section>
}
function Schedule({ tab, setTab, registration, draft, selectedService, paymentStatus, notify }) {
  const [identity, setIdentity] = useState('')
  const [unlocked, setUnlocked] = useState(false)
  const [error, setError] = useState('')
  const verify = event => { event.preventDefault(); if (matchesIdentity(identity, registration, draft)) { setError(''); setUnlocked(true) } else setError('Không tìm thấy hồ sơ phù hợp với mã định danh này.') }
  if (!unlocked) return <section className="schedule-page wrap"><PageIntro eyebrow="THÔNG TIN CÁ NHÂN" title="Theo dõi lịch tiêm và lịch sử" text="Nhập mã định danh đã dùng khi đăng ký để tiếp tục." /><IdentityGate value={identity} onChange={setIdentity} onSubmit={verify} error={error} title="Tra cứu lịch tiêm và lịch sử" description="Nhập mã của người được tiêm hoặc người giám hộ đã đăng ký." /></section>
  const name = registration ? getRecipientNames(registration).join(', ') : 'Trần Gia Bảo'
  const appointment = registration ? { id: 'LH-0004', name, service: getRecipientItems(registration, selectedService).map(item => `${item.name}: ${item.service.name}`).join(' · '), date: draft.appointmentDate, time: draft.slot, status: paymentStatus } : { id: 'LH-0002', name, service: 'Vắc xin B', date: '2026-10-17', time: '10:30', status: 'Đã xác nhận' }
  return <section className="schedule-page wrap"><PageIntro eyebrow="THÔNG TIN CÁ NHÂN" title="Theo dõi lịch tiêm và lịch sử" text="Xem lịch hẹn và lịch sử tiêm." /><div className="schedule-tabs" role="tablist"><button role="tab" aria-selected={tab === 'Lịch tiêm'} className={tab === 'Lịch tiêm' ? 'schedule-tab active' : 'schedule-tab'} onClick={() => setTab('Lịch tiêm')}>▦ Lịch tiêm</button><button role="tab" aria-selected={tab === 'Lịch sử tiêm'} className={tab === 'Lịch sử tiêm' ? 'schedule-tab active' : 'schedule-tab'} onClick={() => setTab('Lịch sử tiêm')}>◷ Lịch sử tiêm</button></div>{tab === 'Lịch tiêm' ? <div className="demo-record-list"><article className="demo-record"><div className="record-date"><b>{appointment.date}</b><span>{appointment.time}</span></div><div className="record-main"><div className="card-kicker">MÃ LỊCH {appointment.id}</div><h3>{appointment.name}</h3><p>{appointment.service}</p><PendingBadge>{appointment.status}</PendingBadge></div><button className="button outline" onClick={() => notify(`Chi tiết ${appointment.id}.`)}>Xem chi tiết</button></article><article className="demo-record"><div className="record-date"><b>2026-10-19</b><span>14:00</span></div><div className="record-main"><div className="card-kicker">MÃ LỊCH LH-0003</div><h3>Phạm Minh Khoa</h3><p>Loại tiêm chủng B</p><PendingBadge>Đã tiếp nhận</PendingBadge></div><button className="button outline" onClick={() => notify('Đã mở chức năng đổi lịch.')}>Đổi lịch</button></article></div> : <div className="demo-record-list"><article className="demo-record"><div className="record-date"><b>2026-09-10</b><span>Hoàn tất</span></div><div className="record-main"><div className="card-kicker">LS-0001</div><h3>Nguyễn An Nhiên</h3><p>Vắc xin A · Mũi 1</p><PendingBadge>Đã ghi nhận</PendingBadge></div><button className="button outline" onClick={() => notify('Đang mở lịch sử tiêm.')}>Xem lịch sử</button></article></div>}</section>
}
export default App
