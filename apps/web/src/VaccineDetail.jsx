import { useEffect, useState } from 'react'
import { ArrowLeft, ArrowRight, ImageOff } from 'lucide-react'
import { getVaccine } from './api/client'
import { money } from './registrationForm'
import { customerDisplayName } from './customerDisplayName'

export function ProductPlaceholder() {
  return <div className="product-placeholder"><ImageOff aria-hidden="true" /><span>Chưa có hình ảnh sản phẩm được xác minh</span></div>
}

const intervalUnits = new Map([['Days', 'ngày'], ['Weeks', 'tuần'], ['Months', 'tháng'], ['Years', 'năm']])

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
    return url.username || url.password ? null : url.href
  } catch { return null }
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
          {vaccine.sourceUrl && /^https?:\/\//i.test(vaccine.sourceUrl) && <p className="vac-reference">
            <a href={vaccine.sourceUrl} target="_blank" rel="noreferrer">Nguồn thông tin sản phẩm</a>
            {vaccine.informationUpdatedAt && Number.isFinite(Date.parse(vaccine.informationUpdatedAt)) && <> · Kiểm tra {new Date(vaccine.informationUpdatedAt).toLocaleDateString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })}</>}
          </p>}
          <dl className="vaccine-facts">
            <div><dt>Nhà sản xuất</dt><dd>{customerDisplayName(vaccine.manufacturer) || 'Chưa có thông tin'}</dd></div>
            <div><dt>Nước sản xuất / xuất xứ</dt><dd>{customerDisplayName(vaccine.origin) || 'Chưa có thông tin'}</dd></div>
            <div><dt>Giá niêm yết</dt><dd className="vaccine-price">{money(vaccine.price)}</dd></div>
          </dl>
          <button className="button primary" onClick={() => go('register')}>Đăng ký dịch vụ<ArrowRight aria-hidden="true" /></button>
          <p className="vac-reference">Chọn vắc xin và người được tiêm trong trang đăng ký. Đăng ký dịch vụ không xác nhận đủ điều kiện tiêm.</p>
        </div>
      </div>
      {vaccine.sourceName && <p className="catalog-clinical-note">{vaccine.sourceName}</p>}
      <section aria-labelledby="regimen-heading" className="vaccine-regimens">
        <h2 id="regimen-heading">Phác đồ tham khảo</h2>
        <p>Lịch tiêm cá nhân và chỉ định tiêm cần được nhân viên y tế xác nhận.</p>
        {vaccine.regimens?.length
          ? vaccine.regimens.map(regimen => <RegimenReference key={regimen.id} regimen={regimen} />)
          : <p className="empty-state">Chưa có phác đồ tham khảo cho vắc xin này.</p>}
      </section>
    </>}
  </section>
}
