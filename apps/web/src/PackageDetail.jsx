import { useEffect, useState } from 'react'
import { ArrowLeft, ArrowRight } from 'lucide-react'
import { getPackage } from './api/client'
import { money } from './registrationForm'
import { customerDisplayName } from './customerDisplayName'

function packageDiseaseNames(diseases) {
  return diseases?.map(disease => customerDisplayName(disease.name)).filter(Boolean).join(', ')
}

function packageItemName(item) {
  const code = customerDisplayName(item.vaccineCode)
  const name = customerDisplayName(item.vaccineName)
  return [code, name].filter(Boolean).join(' — ') || 'Chưa có tên vắc xin'
}

export default function PackageDetail({ id, go }) {
  const [packageItem, setPackageItem] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [reload, setReload] = useState(0)

  useEffect(() => {
    let active = true
    getPackage(id)
      .then(item => { if (active) setPackageItem(item) })
      .catch(() => { if (active) setError('Không tải được chi tiết gói vắc xin. Vui lòng kiểm tra kết nối và thử lại.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [id, reload])

  const available = packageItem?.status === 'Active'
  const availability = available ? 'Đang cung cấp'
    : packageItem?.status === 'Inactive' ? 'Tạm ngừng cung cấp' : 'Chưa có thông tin về tình trạng cung cấp'
  const diseases = packageDiseaseNames(packageItem?.diseases)

  return <section className="catalog-page package-detail wrap">
    <a className="button text-button" href="#catalog"><ArrowLeft aria-hidden="true" />Về danh mục</a>
    <div className="page-intro"><div className="eyebrow">CHI TIẾT GÓI VẮC XIN</div><h1>{customerDisplayName(packageItem?.name) || 'Thông tin gói vắc xin'}</h1></div>
    {loading && <p role="status">Đang tải chi tiết gói vắc xin…</p>}
    {error && <div role="alert" className="notice">{error}<button className="button outline" onClick={() => { setLoading(true); setError(''); setReload(value => value + 1) }}>Thử lại</button></div>}
    {!loading && !error && !packageItem && <div className="empty-state">Không tìm thấy gói vắc xin trong danh mục hiện tại. Gói có thể đã được thay đổi hoặc gỡ bỏ.</div>}
    {!loading && !error && packageItem && <>
      <div className="package-detail-layout">
        <div className="package-detail-summary">
          <div className={available ? 'vaccine-availability available' : 'vaccine-availability unavailable'} role="status">{availability}</div>
          <dl className="vaccine-facts">
            {packageItem.code && <div><dt>Mã gói</dt><dd>{customerDisplayName(packageItem.code)}</dd></div>}
            <div><dt>Giá niêm yết</dt><dd className="vaccine-price">{packageItem.price === null || packageItem.price === undefined ? 'Chưa có thông tin' : money(packageItem.price)}</dd></div>
            <div><dt>Phòng bệnh</dt><dd>{diseases || 'Chưa có thông tin'}</dd></div>
            <div><dt>Tổng số mũi</dt><dd>{packageItem.totalDoses == null ? 'Chưa có thông tin' : packageItem.totalDoses}</dd></div>
          </dl>
          <button className="button primary" disabled={!available} onClick={() => go('register')}>Đăng ký dịch vụ<ArrowRight aria-hidden="true" /></button>
          <p className="vac-reference">Đăng ký dịch vụ không đồng nghĩa đủ điều kiện tiêm; đối tượng, lịch và chỉ định cá nhân do quy trình chuyên môn xác nhận.</p>
        </div>
        <section className="package-detail-items" aria-labelledby="package-items-heading">
          <h2 id="package-items-heading">Thành phần gói</h2>
          {packageItem.items?.length
            ? <ul>{packageItem.items.map((item, index) => <li key={`${item.vaccineId ?? 'vaccine'}-${index}`}>
              <span>{packageItemName(item)}</span>
              {item.quantity != null && <> <strong>× {item.quantity}</strong></>}
            </li>)}</ul>
            : <p className="empty-state">Chưa có thông tin thành phần.</p>}
        </section>
      </div>
      <section className="package-description" aria-labelledby="package-description-heading">
        <h2 id="package-description-heading">Mô tả</h2>
        {packageItem.description?.trim() ? <p>{packageItem.description}</p> : <p className="muted">Chưa có thông tin mô tả.</p>}
      </section>
      <section className="package-audience" aria-labelledby="package-audience-heading">
        <h2 id="package-audience-heading">Đối tượng tham khảo</h2>
        <p>{customerDisplayName(packageItem.targetAudience) || 'Chưa có thông tin'}</p>
      </section>
    </>}
  </section>
}
