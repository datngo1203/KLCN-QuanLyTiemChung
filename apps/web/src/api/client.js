import { parseRegistrationJson, catalogServices } from '../registrationForm.js'

const baseUrl = (import.meta.env?.VITE_API_BASE_URL || 'http://localhost:5080').replace(/\/$/, '')

export async function apiGet(path, exactMoney = false) {
  const response = await fetch(`${baseUrl}${path}`, { signal: AbortSignal.timeout(8000) })
  if (!response.ok) throw new Error(`API trả HTTP ${response.status}`)
  return exactMoney ? parseRegistrationJson(await response.text()) : response.json()
}

export async function getRegistrationCatalog() {
  const [vaccines, packages] = await Promise.all([
    apiGet('/api/v1/catalog/vaccines', true), apiGet('/api/v1/catalog/packages', true),
  ])
  return catalogServices(vaccines, packages)
}

export const getHealth = () => apiGet('/health')
function catalogQuery(search = '', filters = {}) {
  const params = new URLSearchParams({ search })
  for (const [key, value] of Object.entries(filters)) {
    if (value !== '' && value !== null && value !== undefined) params.set(key, String(value))
  }
  return params.toString()
}
export const getVaccines = (search = '', filters = {}) => apiGet(`/api/v1/catalog/vaccines?${catalogQuery(search, filters)}`)
export const getPackages = (search = '', filters = {}) => apiGet(`/api/v1/catalog/packages?${catalogQuery(search, filters)}`)
export const getVaccineTypes = () => apiGet('/api/v1/catalog/vaccine-types')
export const getInjectionTypes = () => apiGet('/api/v1/catalog/injection-types')
