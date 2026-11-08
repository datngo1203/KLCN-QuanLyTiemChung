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

async function mount(t) {
  const host = document.createElement('div')
  document.body.append(host)
  const root = createRoot(host)
  t.after(async () => { await act(async () => root.unmount()); host.remove() })
  await act(async () => root.render(createElement(VaccineDetail, { id: '12', go: () => {} })))
  return host
}

const vaccine = { id: 12, name: 'Vắc xin thử', price: '250000' }
const detail = item => Response.json(item)

test('vaccine detail renders ordered reference doses, provenance and legacy fallback', async t => {
  const pending = Promise.withResolvers()
  globalThis.fetch = () => pending.promise
  const host = await mount(t)
  assert.match(host.querySelector('[role="status"]').textContent, /Đang tải/)
  const regimen = { id: 1, name: 'Phác đồ thử', targetAudience: 'Đối tượng thử', summary: 'Tóm tắt thử',
    sourceName: 'Nguồn thử', sourceUrl: 'https://example.invalid/regimen', doses: [
      { id: 3, doseOrder: 3, timingLabel: 'Nhãn cũ', anchorDoseOrder: 1, intervalValue: 6, intervalUnit: 'Months' },
      { id: 1, doseOrder: 1, timingLabel: 'Mốc văn bản' },
      { id: 2, doseOrder: 2, minimumIntervalDays: 3, recommendedIntervalDays: 7 },
    ] }
  await act(async () => pending.resolve(detail({ ...vaccine, regimens: [regimen,
    { ...regimen, id: 2, sourceName: 'Tên nguồn cũ', sourceUrl: 'javascript:alert(1)', doses: [] }] })))
  const section = host.querySelector('[aria-labelledby="regimen-heading"]')
  assert.match(section.textContent, /Phác đồ tham khảo.*Lịch tiêm cá nhân và chỉ định tiêm cần được nhân viên y tế xác nhận/)
  assert.match(section.textContent, /Phác đồ thử.*Đối tượng thử.*Tóm tắt thử/)
  assert.doesNotMatch(section.textContent, /Nhãn cũ|chưa cung cấp.*lịch tiêm/)
  const rows = [...section.querySelectorAll('li')].map(row => row.textContent)
  assert.deepEqual(rows, ['Mũi 1Mốc văn bản', 'Mũi 2Tối thiểu 3 ngày · Khuyến nghị 7 ngày', 'Mũi 36 tháng sau mũi 1'])
  const links = section.querySelectorAll('a')
  assert.equal(links.length, 1)
  assert.equal(links[0].href, regimen.sourceUrl)
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
  globalThis.fetch = async () => detail(vaccine)
  const host = await mount(t)
  assert.match(host.textContent, /Chưa có phác đồ tham khảo cho vắc xin này/)
})
