import test, { before, after } from 'node:test'
import assert from 'node:assert/strict'
import { fileURLToPath } from 'node:url'
import { Window } from 'happy-dom'
import { createServer } from 'vite'
import { createElement, act } from 'react'

let server, VaccineDetail, createRoot, window
const originalFetch = globalThis.fetch

before(async () => {
  window = new Window({ url: 'http://localhost/' })
  Object.assign(globalThis, { window, document: window.document, IS_REACT_ACT_ENVIRONMENT: true })
  createRoot = (await import('react-dom/client')).createRoot
  server = await createServer({ root: fileURLToPath(new URL('../..', import.meta.url)),
    server: { middlewareMode: true, hmr: false }, logLevel: 'silent' })
  VaccineDetail = (await server.ssrLoadModule('/src/VaccineDetail.jsx')).default
})

after(async () => { globalThis.fetch = originalFetch; await server?.close(); await window?.happyDOM.close() })

async function mount(t, go = () => {}, id = '12') {
  const host = document.createElement('div')
  document.body.append(host)
  const root = createRoot(host)
  t.after(async () => { await act(async () => root.unmount()); host.remove() })
  await act(async () => root.render(createElement(VaccineDetail, { id, go })))
  return host
}

const detail = item => Response.json(item)
const activeVaccine = {
  id: 12,
  name: 'Vắc xin thử',
  code: 'VX-TEST-12',
  status: 'Active',
  manufacturer: 'Nhà sản xuất thử',
  origin: 'Xuất xứ thử',
  price: '250000',
  description: 'Mô tả nguyên nghĩa từ API.\nDòng thứ hai.',
  diseases: [{ id: 1, code: 'DIS-1', name: 'Bệnh thử' }],
  technologies: [{ id: 2, code: 'TEC-2', name: 'Công nghệ thử' }],
  targetAudience: 'Đối tượng tham khảo thử',
  administrationRoute: 'Intramuscular',
  administrationRouteNote: null,
  dosage: 'Nội dung liều lượng tham khảo',
  volume: 0.5,
  sourceName: 'Nguồn tham khảo sản phẩm',
  sourceUrl: 'https://example.invalid/vaccine',
  informationUpdatedAt: '2026-11-05T00:30:00Z',
  regimens: [],
}

test('active vaccine detail presents API fields, mapped route, provenance and actionable registration', async t => {
  globalThis.fetch = async () => detail(activeVaccine)
  const destinations = []
  const host = await mount(t, destination => destinations.push(destination))

  assert.match(host.querySelector('h1').textContent, /Vắc xin thử/)
  assert.match(host.textContent, /VX-TEST-12/)
  assert.match(host.textContent, /Đang cung cấp/)
  assert.doesNotMatch(host.textContent, /\bActive\b|\bInactive\b/)
  for (const value of ['Nhà sản xuất thử', 'Xuất xứ thử', '250.000 đ', 'Mô tả nguyên nghĩa từ API.',
    'Bệnh thử', 'Công nghệ thử', 'Đối tượng tham khảo thử', 'Tiêm bắp',
    'Nội dung liều lượng tham khảo', '0,5 mL', 'Nguồn tham khảo sản phẩm', '5/11/2026']) {
    assert.ok(host.textContent.includes(value), `Missing vaccine detail: ${value}`)
  }
  assert.match(host.querySelector('#vaccine-description-heading').parentElement.textContent, /Dòng thứ hai\./)
  assert.equal(host.querySelector('.vaccine-provenance a').href, activeVaccine.sourceUrl)
  assert.equal(host.querySelector('.vaccine-provenance a').rel, 'noreferrer')
  assert.match(host.querySelector('[aria-labelledby="regimen-heading"]').textContent,
    /Phác đồ chỉ mang tính tham khảo.*không xác nhận đủ điều kiện tiêm.*quy trình chuyên môn/)
  const register = [...host.querySelectorAll('button')].find(button => button.textContent.includes('Đăng ký dịch vụ'))
  assert.equal(register.disabled, false)
  await act(async () => register.click())
  assert.deepEqual(destinations, ['register'])
})

test('inactive direct-link remains readable and cannot continue to registration', async t => {
  globalThis.fetch = async () => detail({ ...activeVaccine, status: 'Inactive' })
  const destinations = []
  const host = await mount(t, destination => destinations.push(destination))

  assert.match(host.querySelector('h1').textContent, /Vắc xin thử/)
  assert.match(host.textContent, /Tạm ngừng cung cấp/)
  assert.doesNotMatch(host.textContent, /\bInactive\b/)
  const register = [...host.querySelectorAll('button')].find(button => button.textContent.includes('Đăng ký dịch vụ'))
  assert.equal(register.disabled, true)
  await act(async () => register.click())
  assert.deepEqual(destinations, [])
})

test('route Other shows its note and maps the remaining canonical routes', async t => {
  const routes = [
    ['Subcutaneous', 'Tiêm dưới da'],
    ['Intradermal', 'Tiêm trong da'],
    ['Oral', 'Đường uống'],
    ['Nasal', 'Đường mũi'],
  ]
  for (const [route, label] of routes) {
    globalThis.fetch = async () => detail({ ...activeVaccine, administrationRoute: route })
    const host = await mount(t)
    const value = [...host.querySelectorAll('.vaccine-details dt')].find(dt => dt.textContent === 'Đường dùng').nextElementSibling
    assert.equal(value.textContent, label)
    await act(async () => host.querySelector('.vaccine-detail-info > .button').click())
  }

  globalThis.fetch = async () => detail({ ...activeVaccine, administrationRoute: 'Other', administrationRouteNote: 'Ghi chú đường dùng' })
  const host = await mount(t)
  const route = [...host.querySelectorAll('.vaccine-details dt')].find(dt => dt.textContent === 'Đường dùng').nextElementSibling
  assert.equal(route.textContent, 'Khác · Ghi chú đường dùng')
})

test('legacy missing fields render neutral values without raw enums or invented volume', async t => {
  globalThis.fetch = async () => detail({ id: 12, name: 'Vắc xin cũ', volume: 0, regimens: [] })
  const host = await mount(t)

  assert.match(host.textContent, /Chưa có thông tin về tình trạng cung cấp/)
  assert.match(host.textContent, /Nhà sản xuất.*Chưa có thông tin/)
  assert.match(host.textContent, /Nước sản xuất \/ xuất xứ.*Chưa có thông tin/)
  assert.match(host.textContent, /Đường dùng.*Chưa có thông tin/)
  assert.match(host.textContent, /Thể tích.*Chưa có thông tin/)
  assert.match(host.textContent, /Giá niêm yết.*Chưa có thông tin/)
  assert.match(host.textContent, /Chưa có thông tin mô tả/)
  assert.doesNotMatch(host.textContent, /undefined|null|\bActive\b|\bInactive\b|0 mL/)
  assert.equal(host.querySelector('.vaccine-provenance'), null)
  assert.equal(host.querySelector('.vaccine-detail-info > .button').disabled, true)
})

test('unsafe product source is not linked but its source name and valid update date remain visible', async t => {
  for (const sourceUrl of ['javascript:alert(1)', 'https://user:password@example.invalid/source']) {
    globalThis.fetch = async () => detail({
      ...activeVaccine, sourceName: 'Nguồn tên thử', sourceUrl,
      informationUpdatedAt: '2026-11-05T00:30:00Z',
    })
    const host = await mount(t)
    assert.match(host.querySelector('.vaccine-provenance').textContent, /Nguồn tên thử.*5\/11\/2026/)
    assert.equal(host.querySelector('.vaccine-provenance a'), null)
    assert.doesNotMatch(host.querySelector('.vaccine-provenance').innerHTML, /href=/i)
  }
})

test('vaccine detail renders ordered regimen, calendar intervals, provenance and legacy timing fallback', async t => {
  const pending = Promise.withResolvers()
  globalThis.fetch = () => pending.promise
  const host = await mount(t)
  assert.match(host.querySelector('[role="status"]').textContent, /Đang tải/)
  const regimen = { id: 1, name: 'Phác đồ thử', targetAudience: 'Đối tượng tham khảo thử', summary: 'Tóm tắt thử',
    sourceName: 'Nguồn thử', sourceUrl: 'https://example.invalid/regimen', doses: [
      { id: 3, doseOrder: 3, timingLabel: 'Nhãn cũ', anchorDoseOrder: 1, intervalValue: 6, intervalUnit: 'Months' },
      { id: 1, doseOrder: 1, timingLabel: 'Mốc văn bản' },
      { id: 2, doseOrder: 2, minimumIntervalDays: 3, recommendedIntervalDays: 7 },
    ] }
  await act(async () => pending.resolve(detail({ ...activeVaccine, regimens: [regimen,
    { ...regimen, id: 2, sourceName: 'Tên nguồn cũ', sourceUrl: 'javascript:alert(1)', doses: [] }] })))
  const section = host.querySelector('[aria-labelledby="regimen-heading"]')
  assert.match(section.textContent, /Phác đồ tham khảo.*Phác đồ chỉ mang tính tham khảo/)
  assert.match(section.textContent, /Phác đồ thử.*Đối tượng tham khảo thử.*Tóm tắt thử/)
  assert.doesNotMatch(section.textContent, /Nhãn cũ|chưa cung cấp.*lịch tiêm/)
  const rows = [...section.querySelectorAll('li')].map(row => row.textContent)
  assert.deepEqual(rows, ['Mũi 1Mốc văn bản', 'Mũi 2Tối thiểu 3 ngày · Khuyến nghị 7 ngày', 'Mũi 36 tháng sau mũi 1'])
  const links = section.querySelectorAll('a')
  assert.equal(links.length, 1)
  assert.equal(links[0].href, regimen.sourceUrl)
  assert.equal(links[0].rel, 'noreferrer')
  assert.equal(links[0].textContent, regimen.sourceName)
  assert.match(section.textContent, /Tên nguồn cũ/)
})

test('vaccine detail keeps its error, retry and missing-item states', async t => {
  globalThis.fetch = async () => new Response(null, { status: 503 })
  const host = await mount(t)
  assert.match(host.querySelector('[role="alert"]').textContent, /Không tải được chi tiết vắc xin/)
  globalThis.fetch = async () => new Response(null, { status: 404 })
  await act(async () => host.querySelector('[role="alert"] button').click())
  assert.match(host.textContent, /Không tìm thấy vắc xin/)
  assert.equal(host.querySelector('[role="alert"]'), null)
})

test('legacy vaccine detail with no regimen has a neutral empty state', async t => {
  globalThis.fetch = async () => detail(activeVaccine)
  const host = await mount(t)
  assert.match(host.textContent, /Chưa có phác đồ tham khảo cho vắc xin này/)
})
