const baseUrl = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5080').replace(/\/$/, '')

export async function apiGet(path) {
  const response = await fetch(`${baseUrl}${path}`, { signal: AbortSignal.timeout(8000) })
  if (!response.ok) throw new Error(`API trả HTTP ${response.status}`)
  return response.json()
}

export const getHealth = () => apiGet('/health')
export const getVaccines = (search = '') => apiGet(`/api/v1/catalog/vaccines?search=${encodeURIComponent(search)}`)
