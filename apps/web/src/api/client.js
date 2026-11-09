import { parseRegistrationJson, catalogServices } from '../registrationForm.js'

const baseUrl = (import.meta.env?.VITE_API_BASE_URL || 'http://localhost:5080').replace(/\/$/, '')

export async function apiGet(path, exactMoney = false) {
  const response = await fetch(`${baseUrl}${path}`, { signal: AbortSignal.timeout(8000) })
  if (!response.ok) {
    const error = new Error(`API trả HTTP ${response.status}`)
    error.status = response.status
    throw error
  }
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

export const getVaccines = (search = '', filters = {}) => apiGet(`/api/v1/catalog/vaccines?${catalogQuery(search, filters)}`, true)
async function getCatalogDetail(resource, id) {
  if (!/^[1-9]\d*$/.test(String(id)) || !Number.isSafeInteger(Number(id))) return null
  try {
    return await apiGet(`/api/v1/catalog/${resource}/${id}`, true)
  } catch (error) {
    if (error.status === 404) return null
    throw error
  }
}

export const getVaccine = id => getCatalogDetail('vaccines', id)
export const getPackages = (search = '', filters = {}) => apiGet(`/api/v1/catalog/packages?${catalogQuery(search, filters)}`, true)
export const getPackage = id => getCatalogDetail('packages', id)
export const getDiseases = () => apiGet('/api/v1/catalog/diseases?status=Active')
export const getVaccineTechnologies = () => apiGet('/api/v1/catalog/vaccine-technologies?status=Active')

export const getValidPromotions = () => apiGet('/api/v1/promotions/valid', true)
