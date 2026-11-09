import { useEffect, useState } from 'react'
import { ArrowLeft, ArrowRight, ImageOff } from 'lucide-react'
import { getVaccine } from './api/client'
import { money } from './registrationForm'
import { customerDisplayName } from './customerDisplayName'

export function ProductPlaceholder() {
  return <div className="product-placeholder"><ImageOff aria-hidden="true" /><span>Chưa có hình ảnh sản phẩm được xác minh</span></div>
}

const intervalUnits = new Map([['Days', 'ngày'], ['Weeks', 'tuần'], ['Months', 'tháng'], ['Years', 'năm']])
const routeNames = new Map([
  ['Intramuscular', 'Tiêm bắp'],
  ['Subcutaneous', 'Tiêm dưới da'],
  ['Intradermal', 'Tiêm trong da'],
  ['Oral', 'Đường uống'],
  ['Nasal', 'Đường mũi'],
  ['Other', 'Khác'],
])

function doseTiming(dose, doses) {
  const unit = intervalUnits.get(dose.intervalUnit)
  if (unit && Number.isInteger(dose.intervalValue) && dose.intervalValue > 0 &&
      dose.anchorDoseOrder < dose.doseOrder && doses.some(anchor => anchor.doseOrder === dose.anchorDoseOrder)) {
    return `${dose.intervalValue} ${unit} sau mũi ${dose.anchorDoseOrder}`
  }
  if (dose.timingLabel?.trim()) return dose.timingLabel
  const days = []
  if (Number.isInteger(dose.minimumIntervalDays)) days.push(`Tối thiểu ${dose.minimumIntervalDays} ngày`)
  if (Number.isInteger(dose.recommendedIntervalDays)) days.push(`Khuyến nghị ${dose.recommendedIntervalDays} ngày`)
  return days.join(' · ') || 'Chưa có thông tin về thời điểm'
}

function referenceUrl(value) {
  if (!/^https?:\/\//i.test(value)) return null
  try {
    const url = new URL(value)
    return ['http:', 'https:'].includes(url.protocol) && !url.username && !url.password ? url.href : null
  } catch { return null }
}

function informationDate(value) {
  if (!value || !Number.isFinite(Date.parse(value))) return null
  return new Intl.DateTimeFormat('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }).format(new Date(value))
}

function referenceList(items) {
  return items?.map(item => customerDisplayName(item.name)).filter(Boolean).join(', ')
}

function RegimenReference({ regimen }) {
  const url = referenceUrl(regimen.sourceUrl)
  const doses = [...(regimen.doses ?? [])].sort((a, b) => a.doseOrder - b.doseOrder)
  return <article className="regimen-reference">
    <h3>{regimen.name}</h3>
    <p><strong>Đối tượng tham khảo</strong> · {regimen.targetAudience}</p>
    {regimen.summary && <p>{regimen.summary}</p>}
    <ol className="regimen-doses" role="list">
      {doses.map(dose => <li key={dose.id}>
        <strong>Mũi {dose.doseOrder}</strong>
        <div>{doseTiming(dose, doses)}</div>
        {dose.note && <div className="vac-reference">{dose.note}</div>}
      </li>)}
    </ol>
    {url ? <p className="vac-reference"><a href={url} target="_blank" rel="noreferrer">{regimen.sourceName || 'Nguồn phác đồ'}</a></p>
      : regimen.sourceName && <p className="vac-reference">{regimen.sourceName}</p>}
  </article>
}

export default function VaccineDetail({ id, go }) {
  const [vaccine, setVaccine] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [reload, setReload] = useState(0)
  useEffect(() => {
    let active = true
    getVaccine(id)
      .then(item => { if (active) setVaccine(item) })
      .catch(() => { if (active) setError('Không tải được chi tiết vắc xin. Vui lòng kiểm tra kết nối và thử lại.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [id, reload])

  const active = vaccine?.status === 'Active'
  const availability = active ? 'Đang cung cấp'
    : vaccine?.status === 'Inactive' ? 'Tạm ngừng cung cấp' : 'Chưa có thông tin về tình trạng cung cấp'
  const sourceUrl = referenceUrl(vaccine?.sourceUrl)
  const updatedDate = informationDate(vaccine?.informationUpdatedAt)
  const routeName = routeNames.get(vaccine?.administrationRoute)
  const volume = Number(vaccine?.volume)
  const volumeLabel = Number.isFinite(volume) && volume > 0
    ? `${new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 3 }).format(volume)} mL`
    : 'Chưa có thông tin'
  const diseases = referenceList(vaccine?.diseases)
  const technologies = referenceList(vaccine?.technologies)

  return <section className="catalog-page vaccine-detail wrap">
    <a className="button text-button" href="#catalog"><ArrowLeft aria-hidden="true" />Về danh mục</a>
    <div className="page-intro"><div className="eyebrow">CHI TIẾT VẮC XIN</div><h1>{customerDisplayName(vaccine?.name) || 'Thông tin vắc xin'}</h1></div>
    {loading && <p role="status">Đang tải chi tiết vắc xin…</p>}
    {error && <div role="alert" className="notice">{error}<button className="button outline" onClick={() => { setLoading(true); setError(''); setReload(value => value + 1) }}>Thử lại</button></div>}
    {!loading && !error && !vaccine && <div className="empty-state">Không tìm thấy vắc xin trong danh mục hiện tại. Vắc xin có thể đã được thay đổi hoặc gỡ bỏ.</div>}
    {!loading && !error && vaccine && <>
      <div className="vaccine-detail-layout">
        <ProductPlaceholder />
        <div className="vaccine-detail-info">
          <div className={active ? 'vaccine-availability available' : 'vaccine-availability unavailable'} role="status">{availability}</div>
          <dl className="vaccine-facts">
            {vaccine.code && <div><dt>Mã vắc xin</dt><dd>{customerDisplayName(vaccine.code)}</dd></div>}
            <div><dt>Nhà sản xuất</dt><dd>{customerDisplayName(vaccine.manufacturer) || 'Chưa có thông tin'}</dd></div>
            <div><dt>Nước sản xuất / xuất xứ</dt><dd>{customerDisplayName(vaccine.origin) || 'Chưa có thông tin'}</dd></div>
            <div><dt>Giá niêm yết</dt><dd className="vaccine-price">{vaccine.price === null || vaccine.price === undefined ? 'Chưa có thông tin' : money(vaccine.price)}</dd></div>
          </dl>
          <button className="button primary" disabled={!active} onClick={() => go('register')}>Đăng ký dịch vụ<ArrowRight aria-hidden="true" /></button>
          <p className="vac-reference">Chọn vắc xin và người được tiêm trong trang đăng ký. Đăng ký dịch vụ không đồng nghĩa đủ điều kiện tiêm; lịch và chỉ định cá nhân do quy trình chuyên môn xác nhận.</p>
        </div>
      </div>
      <section className="vaccine-description" aria-labelledby="vaccine-description-heading">
        <h2 id="vaccine-description-heading">Mô tả</h2>
        {vaccine.description?.trim() ? <p>{vaccine.description}</p> : <p className="muted">Chưa có thông tin mô tả.</p>}
      </section>
      <section className="vaccine-details" aria-labelledby="vaccine-information-heading">
        <h2 id="vaccine-information-heading">Thông tin vắc xin</h2>
        <dl className="vaccine-facts">
          <div><dt>Phòng bệnh</dt><dd>{diseases || 'Chưa có thông tin'}</dd></div>
          <div><dt>Công nghệ vắc xin</dt><dd>{technologies || 'Chưa có thông tin'}</dd></div>
          <div><dt>Đối tượng tham khảo</dt><dd>{customerDisplayName(vaccine.targetAudience) || 'Chưa có thông tin'}</dd></div>
          <div><dt>Đường dùng</dt><dd>{routeName || 'Chưa có thông tin'}{vaccine.administrationRoute === 'Other' && vaccine.administrationRouteNote && ` · ${customerDisplayName(vaccine.administrationRouteNote)}`}</dd></div>
          <div><dt>Liều lượng</dt><dd>{customerDisplayName(vaccine.dosage) || 'Chưa có thông tin'}</dd></div>
          <div><dt>Thể tích</dt><dd>{volumeLabel}</dd></div>
        </dl>
        {vaccine.sourceName || sourceUrl || updatedDate ? <div className="vaccine-provenance">
          <h3>Nguồn thông tin</h3>
          {sourceUrl ? <p><a href={sourceUrl} target="_blank" rel="noreferrer">{customerDisplayName(vaccine.sourceName) || 'Nguồn thông tin sản phẩm'}</a></p>
            : vaccine.sourceName && <p>{customerDisplayName(vaccine.sourceName)}</p>}
          {updatedDate && <p>Ngày cập nhật thông tin: {updatedDate}</p>}
        </div> : null}
      </section>
      <section aria-labelledby="regimen-heading" className="vaccine-regimens">
        <h2 id="regimen-heading">Phác đồ tham khảo</h2>
        <p>Phác đồ chỉ mang tính tham khảo. Đăng ký dịch vụ không xác nhận đủ điều kiện tiêm; lịch tiêm và chỉ định cá nhân do quy trình chuyên môn xác nhận.</p>
        {vaccine.regimens?.length
          ? vaccine.regimens.map(regimen => <RegimenReference key={regimen.id} regimen={regimen} />)
          : <p className="empty-state">Chưa có phác đồ tham khảo cho vắc xin này.</p>}
      </section>
    </>}
  </section>
}
