import { useEffect, useState } from 'react'
import { ArrowLeft, ArrowRight, ImageOff } from 'lucide-react'
import { getVaccine } from './api/client'

export function ProductPlaceholder() {
  return <div className="product-placeholder"><ImageOff aria-hidden="true" /><span>Chưa có hình ảnh sản phẩm được xác minh</span></div>
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
    <div className="page-intro"><div className="eyebrow">CHI TIẾT VẮC XIN</div><h1>{vaccine?.name || 'Thông tin vắc xin'}</h1></div>
    {loading && <p role="status">Đang tải chi tiết vắc xin…</p>}
    {error && <div role="alert" className="notice">{error}<button className="button outline" onClick={() => { setLoading(true); setError(''); setReload(value => value + 1) }}>Thử lại</button></div>}
    {!loading && !error && !vaccine && <div className="empty-state">Không tìm thấy vắc xin trong danh mục hiện tại. Vắc xin có thể đã được thay đổi hoặc gỡ bỏ.</div>}
    {!loading && !error && vaccine && <>
      <div className="vaccine-detail-layout">
        <ProductPlaceholder />
        <div className="vaccine-detail-info">
          <p className="vac-reference">Danh mục từ cơ sở dữ liệu. Các mục có nhãn [DEMO] sử dụng thông tin và giá giả để trình diễn.</p>
          <dl className="vaccine-facts">
            <div><dt>Nhà sản xuất</dt><dd>{vaccine.manufacturer || 'Chưa có thông tin'}</dd></div>
            <div><dt>Nước sản xuất / xuất xứ</dt><dd>{vaccine.origin || 'Chưa có thông tin'}</dd></div>
            <div><dt>Giá niêm yết</dt><dd className="vaccine-price">{Number(vaccine.price).toLocaleString('vi-VN')} đ</dd></div>
          </dl>
          <button className="button primary" onClick={() => go('register')}>Đăng ký dịch vụ<ArrowRight aria-hidden="true" /></button>
          <p className="vac-reference">Chọn vắc xin và người được tiêm trong trang đăng ký. Đăng ký dịch vụ không xác nhận đủ điều kiện tiêm.</p>
        </div>
      </div>
      <p className="catalog-clinical-note">Danh mục hiện chưa cung cấp độ tuổi, liều dùng, lịch tiêm, chống chỉ định hoặc tồn kho. Vui lòng trao đổi với nhân viên y tế để được tư vấn; chỉ định tiêm cần được đánh giá chuyên môn.</p>
    </>}
  </section>
}
