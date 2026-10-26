import { useEffect, useState } from 'react'
import { Search } from 'lucide-react'
import { getVaccines, getPackages, getVaccineTypes, getInjectionTypes } from './api/client'

const money = value => `${Number(value).toLocaleString('vi-VN')} đ`
export default function Catalog({ query, setQuery }) {
  const [vaccineTypes, setVaccineTypes] = useState([])
  const [injectionTypes, setInjectionTypes] = useState([])
  const [filters, setFilters] = useState({ vaccineTypeId: '', injectionTypeId: '', minPrice: '', maxPrice: '' })
  const [submitted, setSubmitted] = useState({ search: query, filters: {} })
  const [data, setData] = useState({ vaccines: [], packages: [], source: '' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [kind, setKind] = useState('Tất cả')
  const [reload, setReload] = useState(0)
  useEffect(() => {
    let active = true
    Promise.all([getVaccineTypes(), getInjectionTypes(), getVaccines(submitted.search, submitted.filters), getPackages(submitted.search, submitted.filters)])
      .then(([types, injections, vaccines, packages]) => {
        if (!active) return
        setVaccineTypes(types.items)
        setInjectionTypes(injections.items)
        setData({ vaccines: vaccines.items, packages: packages.items, source: vaccines.source })
      }).catch(err => { if (active) setError(`Không tải được danh mục. ${err.message}`) })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [submitted, reload])
  const changeFilter = event => setFilters(previous => ({ ...previous, [event.target.name]: event.target.value }))
  const items = [
    ...(kind === 'Gói vắc xin' ? [] : data.vaccines.map(v => ({ ...v, key: `v-${v.id}`, kind: 'Vắc xin lẻ', description: `${v.manufacturer} · ${v.origin}`, type: vaccineTypes.find(t => t.id === v.vaccineTypeId)?.name, injection: injectionTypes.find(t => t.id === v.injectionTypeId)?.name }))),
    ...(kind === 'Vắc xin lẻ' ? [] : data.packages.map(p => ({ ...p, key: `p-${p.id}`, kind: 'Gói vắc xin' }))),
  ]
  return <section className="catalog-page wrap">
    <div className="page-intro"><div className="eyebrow">DANH MỤC DỊCH VỤ</div><h1>Vắc xin và gói tiêm chủng</h1><p>Tra cứu tên, loại và giá dịch vụ. Các mục có nhãn [DEMO] dùng dữ liệu và giá giả để trình diễn.</p></div>
    <form className="catalog-controls" onSubmit={event => { event.preventDefault(); setLoading(true); setError(''); setSubmitted({ search: query, filters: { ...filters } }) }}>
      <div className="search-box"><span><Search aria-hidden="true" /></span><input value={query} onChange={event => setQuery(event.target.value)} maxLength={150} aria-label="Tìm theo tên" placeholder="Tìm vắc xin hoặc gói theo tên"/><button className="button primary" type="submit" disabled={loading}>Tìm kiếm</button></div>
      <div className="catalog-filters">
        <label>Loại vắc xin<select name="vaccineTypeId" value={filters.vaccineTypeId} onChange={changeFilter}><option value="">Tất cả</option>{vaccineTypes.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></label>
        <label>Loại tiêm chủng<select name="injectionTypeId" value={filters.injectionTypeId} onChange={changeFilter}><option value="">Tất cả</option>{injectionTypes.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></label>
        <label>Giá từ (VND)<input type="number" min="0" step="1" name="minPrice" value={filters.minPrice} onChange={changeFilter}/></label>
        <label>Giá đến (VND)<input type="number" min={filters.minPrice || '0'} step="1" name="maxPrice" value={filters.maxPrice} onChange={changeFilter}/></label>
      </div>
      <div className="filter-tabs">{['Tất cả', 'Vắc xin lẻ', 'Gói vắc xin'].map(value => <button type="button" className={kind === value ? 'filter-tab selected' : 'filter-tab'} aria-pressed={kind === value} key={value} onClick={() => setKind(value)}>{value}</button>)}</div>
    </form>
    {loading && <p role="status">Đang tải danh mục…</p>}
    {error && <div role="alert" className="notice">{error} <button className="button outline" onClick={() => { setLoading(true); setError(''); setReload(x => x + 1) }}>Thử lại</button></div>}
    {!loading && !error && <><p className="muted">{data.source === 'database' ? 'Danh mục từ cơ sở dữ liệu' : 'Danh mục mẫu'} · {items.length} kết quả</p><div className="catalog-grid">{items.map((item) => <article className="catalog-card" key={item.key}>
      <div className="catalog-art"><img src={item.kind === 'Gói vắc xin' ? '/images/vaccine-care.jpg' : '/images/clinical-care.jpg'} alt="Ảnh minh họa dịch vụ tiêm chủng" loading="lazy" width="800" height="533" /></div><div className="catalog-info"><div className="card-kicker">{item.kind}</div><h3>{item.name}</h3><p>{item.description}</p>{item.type && <p>{item.type} · {item.injection}</p>}
      {item.items && <p>Thành phần: {item.items.map(v => `${v.vaccineName} × ${v.quantity}`).join(', ')}</p>}
      <div className="demo-price">{money(item.price)}</div></div>
    </article>)}</div>{items.length === 0 && <div className="empty-state">Không có mục phù hợp.</div>}</>}
  </section>
}
