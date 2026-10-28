export const maxMoney = 9999999999999999n
const moneyFields = new Set(['price', 'unitPrice', 'lineTotal', 'subtotal'])

// VND exceeds Number.MAX_SAFE_INTEGER in the contract; preserve the JSON number text.
export function parseRegistrationJson(text) {
  return JSON.parse(text, (key, value, context) => {
    if (!moneyFields.has(key) || typeof value !== 'number') return value
    const source = context?.source
    if (!source && !Number.isSafeInteger(value)) throw new Error('Trình duyệt không hỗ trợ giá trị tiền này. Hãy cập nhật trình duyệt.')
    const integer = String(source || value)
    if (!/^\d+(\.0+)?$/.test(integer)) throw new Error('API trả giá trị tiền không hợp lệ.')
    return BigInt(integer.split('.')[0]).toString()
  })
}

export const money = value => `${BigInt(value).toLocaleString('vi-VN')} đ`
export const serviceKey = item => `${item.kind}-${item.serviceId}`
export const total = items => items.reduce((sum, item) => sum + BigInt(item.price) * BigInt(item.quantity || 0), 0n)
const positiveId = value => Number.isSafeInteger(value) && value > 0

export function registrationPayload(draft, customers, recipients) {
  const customer = customers.find(item => item.id === Number(draft.customerId))
  const recipient = recipients.find(item => item.id === Number(draft.recipientId) && item.customerId === customer?.id)
  if (!customer || !recipient) throw new Error('Chọn khách hàng và người được tiêm thuộc khách hàng đó.')
  if (draft.items.length < 1 || draft.items.length > 100) throw new Error('Chọn từ 1 đến 100 dịch vụ.')
  const keys = new Set()
  const items = draft.items.map(item => {
    if (!['vaccine', 'package'].includes(item.kind) || !positiveId(item.serviceId) || keys.has(serviceKey(item)))
      throw new Error('Dịch vụ không hợp lệ hoặc bị trùng.')
    keys.add(serviceKey(item))
    if (!Number.isInteger(Number(item.quantity)) || Number(item.quantity) < 1 || Number(item.quantity) > 100)
      throw new Error('Số lượng mỗi dịch vụ phải là số nguyên từ 1 đến 100.')
    if (!/^\d+$/.test(String(item.price)) || BigInt(item.price) > maxMoney || item.unavailable)
      throw new Error('Dịch vụ không còn khả dụng hoặc giá không hợp lệ. Hãy kiểm tra danh mục.')
    const line = { kind: item.kind, serviceId: item.serviceId, quantity: Number(item.quantity), expectedUnitPrice: String(item.price) }
    if (item.kind === 'package') {
      if (!item.components?.length || item.components.length > 100 || new Set(item.components.map(x => x.vaccineId)).size !== item.components.length ||
        item.components.some(x => !positiveId(x.vaccineId) || !Number.isInteger(x.quantity) || x.quantity < 1 || x.quantity > 100))
        throw new Error('Thành phần gói không hợp lệ. Hãy kiểm tra danh mục.')
      line.expectedComponents = item.components.map(({ vaccineId, quantity }) => ({ vaccineId, quantity }))
    }
    return line
  })
  if (total(draft.items) > maxMoney) throw new Error('Tổng tiền vượt giới hạn đăng ký. Hãy giảm số lượng dịch vụ.')
  return { customerId: customer.id, recipientId: recipient.id, items }
}

export function registrationBody(payload) {
  return JSON.stringify(payload, (key, value) => key === 'expectedUnitPrice' ? JSON.rawJSON(BigInt(value).toString()) : value)
}

export function catalogServices(vaccines, packages) {
  if (vaccines.source !== 'database' || packages.source !== 'database') throw new Error('Đăng ký cần danh mục từ cơ sở dữ liệu. Danh mục mẫu không thể dùng để đăng ký.')
  return [
    ...vaccines.items.map(item => ({ kind: 'vaccine', serviceId: item.id, name: item.name, price: item.price, description: `${item.manufacturer} · ${item.origin}` })),
    ...packages.items.map(item => ({ kind: 'package', serviceId: item.id, name: item.name, price: item.price, components: item.items })),
  ]
}

export function refreshDraft(items, catalog) {
  const changes = []
  const signature = components => JSON.stringify((components || []).map(x => [x.vaccineId, x.quantity]).sort((a, b) => a[0] - b[0]))
  const updated = items.map(item => {
    const current = catalog.find(x => serviceKey(x) === serviceKey(item))
    if (!current) { changes.push(`${item.name}: không còn trong danh mục; hãy bỏ mục này.`); return { ...item, unavailable: true } }
    if (String(current.price) !== String(item.price) || signature(current.components) !== signature(item.components))
      changes.push(`${item.name}: ${money(item.price)} → ${money(current.price)}${signature(current.components) !== signature(item.components) ? '; thành phần gói đã thay đổi' : ''}.`)
    return { ...current, quantity: item.quantity }
  })
  return { items: updated, changes }
}

export async function allProfiles(client, resource) {
  const items = []
  for (let page = 1; ; page++) {
    const response = await client.profiles(`${resource}?page=${page}&pageSize=100`)
    items.push(...response.items)
    if (response.items.length < 100) return items
  }
}

export const registrationError = error => error.status === 401 ? 'Phiên đã hết hạn. Vui lòng vào Tài khoản để đăng nhập lại.'
  : error.status === 403 ? 'Bạn không có quyền truy cập đăng ký hoặc hồ sơ này. Hãy kiểm tra tài khoản và liên kết hồ sơ.'
    : error instanceof TypeError || ['AbortError', 'TimeoutError', 'SyntaxError'].includes(error.name) ? 'Không kết nối được dịch vụ. Bản nháp vẫn được giữ để thử lại.'
      : error.message
