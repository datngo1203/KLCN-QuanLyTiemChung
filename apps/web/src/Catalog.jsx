import { useEffect, useState } from 'react'
import { Search } from 'lucide-react'
import { getVaccines, getPackages, getDiseases, getVaccineTechnologies } from './api/client'
import { money } from './registrationForm'
import { customerDisplayName } from './customerDisplayName'

export default function Catalog({ query, setQuery }) {
  const [diseases, setDiseases] = useState([])
  const [technologies, setTechnologies] = useState([])
  const [filters, setFilters] = useState({ diseaseId: '', technologyId: '', minPrice: '', maxPrice: '' })
  const [submitted, setSubmitted] = useState({ search: query, filters: { status: 'Active' } })
  const [data, setData] = useState({ vaccines: [], packages: [], source: '' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [kind, setKind] = useState('Tất cả')
  const [reload, setReload] = useState(0)
  useEffect(() => {
    let active = true
    Promise.all([getDiseases(), getVaccineTechnologies(), getVaccines(submitted.search, submitted.filters), getPackages(submitted.search, submitted.filters)])
      .then(([diseaseList, types, vaccines, packages]) => {
        if (!active) return
        setDiseases(diseaseList.items)
        setTechnologies(types.items)
        setData({ vaccines: vaccines.items, packages: packages.items, source: vaccines.source })
      }).catch(() => { if (active) setError('Không thể tải danh mục lúc này. Vui lòng thử lại.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [submitted, reload])
  const changeFilter = event => setFilters(previous => ({ ...previous, [event.target.name]: event.target.value }))
  const vaccines = kind === 'Gói vắc xin' ? [] : data.vaccines
  const packages = kind === 'Vắc xin lẻ' ? [] : data.packages
  const count = vaccines.length + packages.length
  const diseaseNames = item => item.diseases?.map(disease => customerDisplayName(disease.name)).filter(Boolean).join(', ')
  const componentSummary = item => item.items?.map(component => {
    const name = [component.vaccineCode, component.vaccineName].map(value => customerDisplayName(value)).filter(Boolean).join(' — ')
    return name ? `${name} × ${component.quantity}` : null
  }).filter(Boolean).join(', ')
  return <section className="catalog-page wrap">
    <div className="page-intro"><div className="eyebrow">DANH MỤC DỊCH VỤ</div><h1>Vắc xin và gói tiêm chủng</h1><p>Tìm theo mã, tên, hãng hoặc phòng bệnh; lọc theo công nghệ và khoảng giá.</p></div>
    <form className="catalog-controls" onSubmit={event => { event.preventDefault(); setLoading(true); setError(''); setSubmitted({ search: query, filters: { ...filters, status: 'Active' } }) }}>
      <div className="search-box"><span><Search aria-hidden="true" /></span><input value={customerDisplayName(query)} onChange={event => setQuery(event.target.value)} maxLength={150} aria-label="Tìm theo mã, tên, hãng hoặc phòng bệnh" placeholder="Tìm theo mã, tên, hãng hoặc phòng bệnh"/><button className="button primary" type="submit" disabled={loading}>Tìm kiếm</button></div>
      <div className="catalog-filters">
        <label>Phòng bệnh<select name="diseaseId" value={filters.diseaseId} onChange={changeFilter}><option value="">Tất cả</option>{diseases.map(disease => <option key={disease.id} value={disease.id}>{customerDisplayName(disease.name)}</option>)}</select></label>
        <label>Công nghệ vắc xin<select name="technologyId" value={filters.technologyId} onChange={changeFilter}><option value="">Tất cả</option>{technologies.map(t => <option key={t.id} value={t.id}>{customerDisplayName(t.name)}</option>)}</select></label>
        <label>Giá từ (VND)<input type="number" min="0" step="1" name="minPrice" value={filters.minPrice} onChange={changeFilter}/></label>
        <label>Giá đến (VND)<input type="number" min={filters.minPrice || '0'} step="1" name="maxPrice" value={filters.maxPrice} onChange={changeFilter}/></label>
      </div>
      <div className="filter-tabs">{['Tất cả', 'Vắc xin lẻ', 'Gói vắc xin'].map(value => <button type="button" className={kind === value ? 'filter-tab selected' : 'filter-tab'} aria-pressed={kind === value} key={value} onClick={() => setKind(value)}>{value}</button>)}</div>
    </form>
    {loading && <p role="status">Đang tải danh mục…</p>}
    {error && <div role="alert" className="notice">{error} <button className="button outline" onClick={() => { setLoading(true); setError(''); setReload(x => x + 1) }}>Thử lại</button></div>}
    {!loading && !error && <><p className="muted" role="status">Danh mục dịch vụ · {count} kết quả</p>
      {vaccines.length > 0 && <div className="catalog-table-scroll" role="region" aria-label="Bảng vắc xin, cuộn ngang để xem thêm" tabIndex={0}>
        <table className="catalog-table">
          <caption>Vắc xin lẻ · Giá niêm yết (VND)<span className="catalog-table-hint">Cuộn ngang để xem đầy đủ các cột.</span></caption>
          <thead><tr><th scope="col">Vắc xin / phòng bệnh</th><th scope="col">Nhà sản xuất</th><th scope="col">Nước sản xuất / xuất xứ</th><th scope="col" className="catalog-price">Giá</th><th scope="col">Chi tiết</th></tr></thead>
          <tbody>{vaccines.map(vaccine => <tr key={vaccine.id}>
            <th scope="row"><div className="catalog-item">
              {vaccine.code && <span className="catalog-code">{customerDisplayName(vaccine.code)}</span>}
              <a className="catalog-name" href={`#catalog/vaccines/${vaccine.id}`}>{customerDisplayName(vaccine.name)}</a>
              {diseaseNames(vaccine) && <span className="catalog-diseases">Phòng bệnh: {diseaseNames(vaccine)}</span>}
            </div></th>
            <td>{customerDisplayName(vaccine.manufacturer) || 'Chưa có thông tin'}</td><td>{customerDisplayName(vaccine.origin) || 'Chưa có thông tin'}</td>
            <td className="catalog-price">{money(vaccine.price)}</td>
            <td><a className="button outline" href={`#catalog/vaccines/${vaccine.id}`} aria-label={`Xem chi tiết ${customerDisplayName(vaccine.name)}`}>Xem chi tiết</a></td>
          </tr>)}</tbody>
        </table>
      </div>}
      {packages.length > 0 && <section aria-labelledby="catalog-packages-title"><h2 id="catalog-packages-title" className="catalog-packages-title">Gói vắc xin</h2><div className="catalog-grid">{packages.map(item => <article className="catalog-card" key={item.id}>
        <div className="catalog-info"><div className="card-kicker">Gói vắc xin{item.code && ` · ${customerDisplayName(item.code)}`}</div><h3>{customerDisplayName(item.name)}</h3>
        {diseaseNames(item) && <p className="catalog-diseases">Phòng bệnh: {diseaseNames(item)}</p>}
        {item.totalDoses != null && <p className="catalog-package-doses">Tổng số mũi: <strong>{item.totalDoses}</strong></p>}
        {componentSummary(item) && <p className="catalog-components">Thành phần: {componentSummary(item)}</p>}
        <div className="catalog-package-price">{money(item.price)}</div>
        <a className="button outline catalog-package-link" href={`#catalog/packages/${item.id}`} aria-label={`Xem chi tiết gói ${customerDisplayName(item.name)}`}>Xem chi tiết</a></div>
      </article>)}</div></section>}
      {count === 0 && <div className="empty-state" role="status">Không có mục phù hợp.</div>}</>}
  </section>
}
