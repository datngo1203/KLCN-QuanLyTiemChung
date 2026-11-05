import { useEffect, useState } from 'react'
import { Armchair, ArrowUpRight, Baby, Clock3, MapPin, Phone, Stethoscope, Syringe } from 'lucide-react'
import { getVaccines, getPackages } from './api/client'
import { ProductPlaceholder } from './VaccineDetail'
import { money } from './registrationForm'

export const centerName = 'VNVC Hoàng Văn Thụ'
export const centerAddress = '198 Hoàng Văn Thụ, P. Đức Nhuận, TP.HCM'
export const centerHours = '07:30 – 17:00'
export const centerDays = 'Thứ Hai – Chủ nhật · Xuyên trưa'
export const centerHotline = '028 7102 6595'
export const centerTel = 'tel:02871026595'
export const centerMap = `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(centerAddress)}`

const facilities = [
  [Armchair, 'Sảnh chờ', 'Có chỗ ngồi, nước uống và Wi-Fi trong thời gian chờ.'],
  [Stethoscope, 'Phòng khám', 'Có khu vực riêng để khám sàng lọc và tư vấn trước tiêm.'],
  [Syringe, 'Phòng tiêm', 'Không gian riêng dành cho việc thực hiện mũi tiêm.'],
  [Baby, 'Tiện ích cho gia đình', 'Có khu vui chơi cho trẻ và phòng pha sữa, thay tã.'],
]

function HomeActions({ go, setQuery }) {
  return <div className="hero-actions">
    <button className="button primary" onClick={() => go('register')}>Đăng ký tiêm</button>
    <button className="button outline" onClick={() => { setQuery(''); go('catalog') }}>Xem danh mục vắc xin</button>
  </div>
}

export default function Home({ go, setQuery }) {
  return <>
    <section className="center-hero" aria-labelledby="home-title">
      <div className="wrap center-hero-layout">
        <div className="center-hero-copy">
          <h1 id="home-title">{centerName}</h1>
          <p>Trung tâm tiêm chủng cho trẻ em và người lớn</p>
          <HomeActions go={go} setQuery={setQuery} />
        </div>
        <img src="/assets/vnvc-home-vaccination.png" alt="Nhân viên VNVC tư vấn và tiêm chủng cho trẻ em và người lớn" width="1200" height="822" />
        <dl className="center-facts">
          <div><dt><MapPin aria-hidden="true" />Địa chỉ</dt><dd>{centerAddress}<a href={centerMap} target="_blank" rel="noreferrer">Xem bản đồ <ArrowUpRight aria-hidden="true" /></a></dd></div>
          <div><dt><Clock3 aria-hidden="true" />Giờ làm việc</dt><dd>{centerHours}<span>{centerDays}</span></dd></div>
          <div><dt><Phone aria-hidden="true" />Hotline</dt><dd><a href={centerTel}>{centerHotline}</a></dd></div>
        </dl>
      </div>
    </section>

    <section className="section wrap center-about" aria-labelledby="center-about-title">
      <div className="section-heading">
        <div>
          <h2 id="center-about-title">Giới thiệu VNVC Hoàng Văn Thụ</h2>
          <p>Khai trương ngày 06/12/2017, đây là cơ sở VNVC đầu tiên tại khu vực phía Nam. Trung tâm phục vụ trẻ em và người lớn, với các khu vực chờ, khám và tiêm được bố trí riêng.</p>
        </div>
      </div>
      <h3 id="center-facilities-title">Cơ sở vật chất và tiện ích</h3>
      <ul className="center-facilities" aria-labelledby="center-facilities-title">
        {facilities.map(([Icon, title, description]) => <li key={title}>
          <Icon aria-hidden="true" /><h4>{title}</h4><p>{description}</p>
        </li>)}
      </ul>
    </section>

    <HomeCatalog go={go} setQuery={setQuery} />

    <section className="wrap center-cta" aria-labelledby="center-cta-title">
      <h2 id="center-cta-title">Đăng ký tiêm tại VNVC Hoàng Văn Thụ</h2>
      <HomeActions go={go} setQuery={setQuery} />
    </section>
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
    ...data.vaccines.slice(0, 2).map(item => ({ ...item, key: `v-${item.id}`, kind: 'Vắc xin lẻ', href: `#catalog/vaccines/${item.id}`, description: [item.manufacturer, item.origin].filter(Boolean).join(' · ') })),
    ...data.packages.slice(0, 1).map(item => ({ ...item, key: `p-${item.id}`, kind: 'Gói vắc xin', description: item.items?.map(vaccine => `${vaccine.vaccineName} × ${vaccine.quantity}`).join(', ') })),
  ]
  return <section className="vac-discover section" aria-labelledby="home-catalog-title"><div className="wrap">
    <div className="section-heading"><div><div className="eyebrow">DỊCH VỤ TIÊM CHỦNG</div><h2 id="home-catalog-title">Danh mục vắc xin và gói tiêm</h2><p>Tham khảo thông tin và giá dịch vụ để chuẩn bị cho lần đến trung tâm.</p></div><button className="button outline" onClick={() => { setQuery(''); go('catalog') }}>Xem toàn bộ danh mục <ArrowUpRight aria-hidden="true" /></button></div>
    {loading && <div className="vac-catalog-state" role="status">Đang tải danh mục dịch vụ…</div>}
    {error && <div className="vac-catalog-state" role="alert"><p>{error}</p><button className="button outline" onClick={() => { setLoading(true); setError(''); setReload(value => value + 1) }}>Thử lại</button></div>}
    {!loading && !error && <>
      {(data.demo || items.some(item => item.name.includes('[DEMO]'))) && <p className="vac-reference">Các mục có nhãn [DEMO] hoặc nguồn dữ liệu mẫu sử dụng thông tin và giá giả để trình diễn.</p>}
      <div className="service-grid">{items.map(item => <article className="service-card vac-service-preview" key={item.key}>
        <ProductPlaceholder />
        <div className="service-card-body"><div className="card-kicker">{item.kind}</div><h3>{item.href ? <a className="catalog-name" href={item.href}>{item.name}</a> : item.name}</h3><p>{item.description || 'Xem thông tin trong danh mục dịch vụ.'}</p><strong>{money(item.price)}</strong>{item.href ? <a className="button outline" href={item.href} aria-label={`Xem chi tiết ${item.name}`}>Xem chi tiết <ArrowUpRight aria-hidden="true" /></a> : <button className="button outline" onClick={() => { setQuery(item.name); go('catalog') }}>Xem trong danh mục <ArrowUpRight aria-hidden="true" /></button>}</div>
      </article>)}</div>
      {items.length === 0 && <div className="vac-catalog-state">Danh mục dịch vụ đang được cập nhật. Bạn có thể quay lại sau.</div>}
    </>}
  </div></section>
}
