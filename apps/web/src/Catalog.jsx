import { useEffect, useState } from 'react'
import { Search } from 'lucide-react'
import { getVaccines, getPackages, getVaccineTypes, getInjectionTypes } from './api/client'
import { ProductPlaceholder } from './VaccineDetail'
import { money } from './registrationForm'

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
  const vaccines = kind === 'Gói vắc xin' ? [] : data.vaccines
  const packages = kind === 'Vắc xin lẻ' ? [] : data.packages
  const count = vaccines.length + packages.length
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
    {!loading && !error && <><p className="muted" role="status">{data.source === 'database' ? 'Danh mục từ cơ sở dữ liệu' : 'Danh mục mẫu'} · {count} kết quả</p>
      {vaccines.length > 0 && <div className="catalog-table-scroll" role="region" aria-label="Bảng vắc xin, cuộn ngang để xem thêm" tabIndex={0}>
        <table className="catalog-table">
          <caption>Vắc xin lẻ · Giá niêm yết (VND)<span className="catalog-table-hint">Cuộn ngang để xem đầy đủ các cột.</span></caption>
          <thead><tr><th scope="col">Tên vắc xin</th><th scope="col">Nhà sản xuất</th><th scope="col">Nước sản xuất / xuất xứ</th><th scope="col" className="catalog-price">Giá</th><th scope="col">Chi tiết</th></tr></thead>
          <tbody>{vaccines.map(vaccine => <tr key={vaccine.id}>
            <th scope="row"><a className="catalog-name" href={`#catalog/vaccines/${vaccine.id}`}>{vaccine.name}</a></th>
            <td>{vaccine.manufacturer || 'Chưa có thông tin'}</td><td>{vaccine.origin || 'Chưa có thông tin'}</td>
            <td className="catalog-price">{money(vaccine.price)}</td>
            <td><a className="button outline" href={`#catalog/vaccines/${vaccine.id}`} aria-label={`Xem chi tiết ${vaccine.name}`}>Xem chi tiết</a></td>
          </tr>)}</tbody>
        </table>
      </div>}
      {packages.length > 0 && <><h2 className="catalog-packages-title">Gói vắc xin</h2><div className="catalog-grid">{packages.map(item => <article className="catalog-card" key={item.id}>
        <ProductPlaceholder /><div className="catalog-info"><div className="card-kicker">Gói vắc xin</div><h3>{item.name}</h3>
        <p>Thành phần: {item.items.map(v => `${v.vaccineName} × ${v.quantity}`).join(', ')}</p>
        <div className="demo-price">{money(item.price)}</div></div>
      </article>)}</div></>}
      {count === 0 && <div className="empty-state">Không có mục phù hợp.</div>}</>}
  </section>
}
