import { useEffect, useState } from 'react'
import { ArrowUpRight, ArrowRight, ArrowDown, Search, ChevronDown } from 'lucide-react'
import { getVaccines, getPackages } from './api/client'

export default function Home({ go, query, setQuery }) {
  return <>
    <section className="vac-home" aria-labelledby="home-title">
      <div className="vac-cover">
        <img src="/images/vaccination.jpg" alt="Bác sĩ thực hiện tiêm chủng cho người trưởng thành" fetchPriority="high" width="1920" height="1280" />
        <div className="wrap cover-caption"><span>VAC · VACCINE ADMINISTRATION CENTER</span><p>Chăm sóc hôm nay.<br />An tâm ngày mai.</p></div>
      </div>
      <div className="vac-intro wrap">
        <div className="vac-intro-copy">
          <span className="vac-label">Trung tâm tiêm chủng VAC</span>
          <h1 id="home-title">Chủ động tiêm chủng.<br /><em>Vững tâm mỗi ngày.</em></h1>
          <p>Thông tin rõ ràng, lựa chọn dễ dàng. Cùng VAC tìm hiểu dịch vụ tiêm chủng và chăm sóc sức khỏe cho bạn, cho gia đình.</p>
          <div className="hero-actions">
            <button className="button primary" onClick={() => go('catalog')}>Khám phá vắc xin <ArrowUpRight aria-hidden="true" /></button>
            <a className="button outline" href="#lien-he">Thông tin trung tâm <ArrowDown aria-hidden="true" /></a>
          </div>
          <form className="search-box" onSubmit={event => { event.preventDefault(); go('catalog') }}>
            <span><Search aria-hidden="true" /></span><input value={query} onChange={event => setQuery(event.target.value)} maxLength={150} placeholder="Tìm loại tiêm chủng hoặc vắc xin" aria-label="Tìm loại tiêm chủng hoặc vắc xin" />
            <button type="submit" className="button primary">Tìm kiếm</button>
          </form>
        </div>
        <aside className="vac-stories" aria-label="Khám phá dịch vụ VAC">
          <button className="vac-story" onClick={() => go('catalog')}><img src="/images/vaccine-care.jpg" alt="Nhân viên y tế chuẩn bị vắc xin tại phòng khám" width="800" height="533" /><span><small>TÌM HIỂU DỊCH VỤ</small><b>Vắc xin & gói tiêm chủng</b><i><ArrowUpRight aria-hidden="true" /></i></span></button>
          <button className="vac-story" onClick={() => go('account')}><img src="/images/clinical-care.jpg" alt="Nhân viên y tế chăm sóc người được tiêm" width="800" height="533" /><span><small>ĐỒNG HÀNH CÙNG BẠN</small><b>Quản lý hồ sơ gia đình</b><i><ArrowUpRight aria-hidden="true" /></i></span></button>
        </aside>
      </div>
      <div className="vac-pillars wrap"><span>Tra cứu vắc xin</span><span>Tìm hiểu gói tiêm</span><span>Quản lý hồ sơ</span><span>Chủ động chăm sóc sức khỏe</span></div>
    </section>

    <section className="vac-about section wrap" aria-labelledby="about-title">
      <div><div className="eyebrow">VỀ VAC</div><h2 id="about-title">Thông tin trong tầm tay.<br />An tâm cho cả gia đình.</h2></div>
      <div className="vac-about-content"><p>Cổng thông tin VAC giúp bạn tìm hiểu vắc xin, tham khảo gói tiêm chủng và chuẩn bị hồ sơ trước khi đến trung tâm. Từ thông tin của bản thân đến hồ sơ của bé, bạn có thể quản lý tập trung trong tài khoản khách hàng.</p>
        <ul><li>Tra cứu tên, nguồn gốc và giá dịch vụ trong danh mục.</li><li>Tìm hiểu thành phần của từng gói tiêm chủng.</li><li>Lưu và cập nhật hồ sơ của bạn cùng người thân.</li></ul>
        <button className="button outline" onClick={() => go('account')}>Quản lý hồ sơ gia đình <ArrowUpRight aria-hidden="true" /></button>
      </div>
    </section>

    <HomeCatalog go={go} setQuery={setQuery} />

    <section className="vac-preparation section wrap" aria-labelledby="preparation-title">
      <div className="section-heading"><div><div className="eyebrow">TRƯỚC KHI ĐẾN TRUNG TÂM</div><h2 id="preparation-title">Chuẩn bị chu đáo, thăm khám thuận tiện.</h2><p>Một vài việc đơn giản giúp bạn sẵn sàng cho buổi tiêm chủng.</p></div></div>
      <div className="vac-preparation-grid">
        <article><span className="vac-step">01</span><h3>Kiểm tra thông tin cá nhân</h3><p>Chuẩn bị họ tên, ngày sinh, thông tin liên hệ và kiểm tra hồ sơ của người được tiêm trên tài khoản.</p><button className="button text-button" onClick={() => go('account')}>Đến hồ sơ của bạn <ArrowRight aria-hidden="true" /></button></article>
        <article><span className="vac-step">02</span><h3>Mang theo lịch sử tiêm</h3><p>Nếu có, mang theo sổ hoặc giấy ghi nhận các mũi đã tiêm để nhân viên y tế có thêm thông tin khi tư vấn.</p></article>
        <article><span className="vac-step">03</span><h3>Chuẩn bị câu hỏi cần tư vấn</h3><p>Ghi lại những điều bạn muốn hỏi và trao đổi với nhân viên y tế trước khi tiêm. Việc chỉ định tiêm được thực hiện sau khám sàng lọc.</p></article>
      </div>
      <p className="vac-reference">Tìm hiểu thêm về việc chuẩn bị sổ tiêm và câu hỏi cho nhân viên y tế trong <a href="https://www.cdc.gov/vaccines-adults/recommended-vaccines/keeping-vaccine-records-up-to-date.html" target="_blank" rel="noreferrer">hướng dẫn của CDC</a> và <a href="https://www.canada.ca/en/public-health/services/publications/vaccines-immunization/childhood-vaccinations-appointment-checklist.html" target="_blank" rel="noreferrer">Cơ quan Y tế Công cộng Canada</a>.</p>
    </section>

    <section className="vac-faq section" aria-labelledby="faq-title"><div className="wrap vac-faq-layout">
      <div><div className="eyebrow">GIẢI ĐÁP NHANH</div><h2 id="faq-title">Bạn đang cần<br />tìm hiểu điều gì?</h2><p>Những thông tin hữu ích khi sử dụng cổng tiêm chủng VAC.</p><a className="button outline" href="#lien-he">Thông tin liên hệ <ArrowDown aria-hidden="true" /></a></div>
      <div className="vac-faq-list">
        <details><summary>Tôi xem thông tin và giá vắc xin ở đâu?<ChevronDown className="faq-chevron" aria-hidden="true" /></summary><div><p>Mở Danh mục để tra cứu theo tên, loại vắc xin, loại tiêm chủng hoặc khoảng giá. Mỗi mục hiển thị thông tin và giá đang được niêm yết trong danh mục.</p><button className="button text-button" onClick={() => go('catalog')}>Tra cứu danh mục <ArrowRight aria-hidden="true" /></button></div></details>
        <details><summary>Tôi có thể quản lý hồ sơ cho bé và người thân không?<ChevronDown className="faq-chevron" aria-hidden="true" /></summary><div><p>Có. Sau khi đăng nhập và đăng ký hồ sơ khách hàng, bạn có thể thêm người được tiêm, nhập thông tin của bé hoặc người thân và cập nhật khi cần.</p><button className="button text-button" onClick={() => go('account')}>Mở tài khoản và hồ sơ <ArrowRight aria-hidden="true" /></button></div></details>
        <details><summary>Làm thế nào để tạo tài khoản VAC?<ChevronDown className="faq-chevron" aria-hidden="true" /></summary><div><p>Chọn Đăng nhập, sau đó chọn Tạo tài khoản khách hàng. Nhập email và mật khẩu, hoàn tất xác minh email rồi đăng nhập để quản lý hồ sơ.</p></div></details>
        <details><summary>Gói tiêm chủng gồm những vắc xin nào?<ChevronDown className="faq-chevron" aria-hidden="true" /></summary><div><p>Trong Danh mục, chọn Gói vắc xin để xem tên gói, giá và danh sách vắc xin cùng số lượng tương ứng. Trao đổi với nhân viên y tế để được tư vấn phù hợp.</p></div></details>
      </div>
    </div></section>


  </>
}

function HomeCatalog({ go, setQuery }) {
  const [data, setData] = useState({ vaccines: [], packages: [], demo: false })
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [reload, setReload] = useState(0)
  useEffect(() => {
    let active = true
    Promise.all([getVaccines(), getPackages()])
      .then(([vaccines, packages]) => {
        if (active) setData({ vaccines: vaccines.items, packages: packages.items, demo: vaccines.source === 'demo' || packages.source === 'demo' })
      })
      .catch(() => { if (active) setError('Chưa tải được danh mục dịch vụ. Vui lòng thử lại sau.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [reload])
  const items = [
    ...data.vaccines.slice(0, 2).map(item => ({ ...item, key: `v-${item.id}`, kind: 'Vắc xin lẻ', image: '/images/clinical-care.jpg', description: [item.manufacturer, item.origin].filter(Boolean).join(' · ') })),
    ...data.packages.slice(0, 1).map(item => ({ ...item, key: `p-${item.id}`, kind: 'Gói vắc xin', image: '/images/vaccine-care.jpg', description: item.description || item.items?.map(vaccine => `${vaccine.vaccineName} × ${vaccine.quantity}`).join(', ') })),
  ]
  return <section className="vac-discover section" aria-labelledby="home-catalog-title"><div className="wrap">
    <div className="section-heading"><div><div className="eyebrow">DỊCH VỤ TIÊM CHỦNG</div><h2 id="home-catalog-title">Khám phá vắc xin và gói tiêm chủng.</h2><p>Tham khảo thông tin và giá dịch vụ để chuẩn bị cho lần đến trung tâm.</p></div><button className="button outline" onClick={() => { setQuery(''); go('catalog') }}>Xem toàn bộ danh mục <ArrowUpRight aria-hidden="true" /></button></div>
    {loading && <div className="vac-catalog-state" role="status">Đang tải danh mục dịch vụ…</div>}
    {error && <div className="vac-catalog-state" role="alert"><p>{error}</p><button className="button outline" onClick={() => { setLoading(true); setError(''); setReload(value => value + 1) }}>Thử lại</button></div>}
    {!loading && !error && <>
      {(data.demo || items.some(item => item.name.includes('[DEMO]'))) && <p className="vac-reference">Các mục có nhãn [DEMO] hoặc nguồn dữ liệu mẫu sử dụng thông tin và giá giả để trình diễn.</p>}
      <div className="service-grid">{items.map(item => <article className="service-card vac-service-preview" key={item.key}>
        <img src={item.image} alt="Ảnh minh họa dịch vụ tiêm chủng" loading="lazy" width="800" height="533" />
        <div className="service-card-body"><div className="card-kicker">{item.kind}</div><h3>{item.name}</h3><p>{item.description || 'Xem thông tin trong danh mục dịch vụ.'}</p><strong>{Number(item.price).toLocaleString('vi-VN')} đ</strong><button className="button outline" onClick={() => { setQuery(item.name); go('catalog') }}>Xem trong danh mục <ArrowUpRight aria-hidden="true" /></button></div>
      </article>)}</div>
      {items.length === 0 && <div className="vac-catalog-state">Danh mục dịch vụ đang được cập nhật. Bạn có thể quay lại sau.</div>}
    </>}
  </div></section>
}
