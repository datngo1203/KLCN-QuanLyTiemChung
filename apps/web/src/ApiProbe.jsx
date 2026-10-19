import { useState } from 'react'
import { getHealth } from './api/client'

export default function ApiProbe() {
  const [state, setState] = useState('Kiểm tra API')
  const [busy, setBusy] = useState(false)
  async function check() {
    setBusy(true)
    try {
      const health = await getHealth()
      setState(health.status === 'ok' ? 'API đã kết nối' : 'API chưa sẵn sàng')
    } catch { setState('Chưa kết nối API · thử lại') }
    finally { setBusy(false) }
  }
  return <button className="api-probe" onClick={check} disabled={busy}>{busy ? 'Đang kiểm tra…' : state}</button>
}
