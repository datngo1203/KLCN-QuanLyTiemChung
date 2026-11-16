import { useState } from 'react'
import { ImageOff } from 'lucide-react'

const images = import.meta.glob('./assets/vaccines/{,local/}*.jpg', {
  eager: true, query: '?url&no-inline', import: 'default',
})

export default function VaccineImage({ code, name }) {
  const [failedSrc, setFailedSrc] = useState(null)
  const file = code?.startsWith('VX-') && code.slice(3).toLowerCase() + '.jpg'
  const src = images[`./assets/vaccines/${file}`] ?? images[`./assets/vaccines/local/${file}`]

  if (!src || src === failedSrc) {
    return <div className="product-placeholder"><ImageOff aria-hidden="true" /><span>Chưa có ảnh sản phẩm</span></div>
  }
  return <img className="vaccine-image" src={src} alt={`Hộp vắc xin ${name}`} onError={() => setFailedSrc(src)} />
}
