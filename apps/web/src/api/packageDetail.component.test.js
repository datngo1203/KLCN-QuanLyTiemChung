import test, { after, before } from 'node:test'
import assert from 'node:assert/strict'
import { fileURLToPath } from 'node:url'
import { Window } from 'happy-dom'
import { createServer } from 'vite'
import { createElement, act } from 'react'

let server, PackageDetail, createRoot, window
const originalFetch = globalThis.fetch

before(async () => {
  window = new Window({ url: 'http://localhost/' })
  Object.assign(globalThis, { window, document: window.document, IS_REACT_ACT_ENVIRONMENT: true })
  createRoot = (await import('react-dom/client')).createRoot
  server = await createServer({ root: fileURLToPath(new URL('../..', import.meta.url)),
    server: { middlewareMode: true, hmr: false }, logLevel: 'silent' })
  PackageDetail = (await server.ssrLoadModule('/src/PackageDetail.jsx')).default
})

after(async () => { globalThis.fetch = originalFetch; await server?.close(); await window?.happyDOM.close() })

async function mount(t, go = () => {}, id = '31') {
  const host = document.createElement('div')
  document.body.append(host)
  const root = createRoot(host)
  t.after(async () => { await act(async () => root.unmount()); host.remove() })
  await act(async () => root.render(createElement(PackageDetail, { id, go })))
  return host
}

const activePackage = {
  id: 31,
  name: 'Gói vắc xin thử',
  code: 'PK-TEST-31',
  status: 'Active',
  price: '300000',
  description: 'Mô tả nguyên nghĩa từ API.',
  targetAudience: 'Đối tượng tham khảo thử',
  totalDoses: 3,
  diseases: [{ id: 7, name: 'Bệnh thử' }],
  items: [
    { vaccineId: 12, vaccineCode: 'VX-12', vaccineName: 'Vắc xin thử', quantity: 2 },
    { vaccineId: 13, vaccineCode: 'VX-13', vaccineName: null, quantity: 1 },
  ],
}

test('package detail exposes an accessible loading state until the request resolves', async t => {
  let resolveResponse
  globalThis.fetch = () => new Promise(resolve => { resolveResponse = resolve })
  const host = await mount(t)
  assert.match(host.querySelector('[role="status"]').textContent, /Đang tải chi tiết gói vắc xin/)
  await act(async () => resolveResponse(Response.json(activePackage)))
  assert.match(host.querySelector('h1').textContent, /Gói vắc xin thử/)
})

test('active package detail presents API fields and allows registration without preselection', async t => {
  const requests = []
  globalThis.fetch = async url => {
    requests.push(new URL(url))
    return Response.json(activePackage)
  }
  const destinations = []
  const host = await mount(t, destination => destinations.push(destination))

  assert.equal(requests.length, 1)
  assert.equal(requests[0].pathname, '/api/v1/catalog/packages/31')
  for (const value of ['Gói vắc xin thử', 'PK-TEST-31', 'Đang cung cấp', '300.000 đ',
    'Bệnh thử', 'Tổng số mũi', '3', 'Mô tả nguyên nghĩa từ API.', 'Đối tượng tham khảo thử',
    'VX-12 — Vắc xin thử', '× 2', 'VX-13 × 1']) {
    assert.ok(host.textContent.includes(value), `Missing package detail: ${value}`)
  }
  assert.doesNotMatch(host.textContent, /\bActive\b|\bInactive\b|undefined|null/)
  const register = [...host.querySelectorAll('button')].find(button => button.textContent.includes('Đăng ký dịch vụ'))
  assert.equal(register.disabled, false)
  await act(async () => register.click())
  assert.deepEqual(destinations, ['register'])
})

test('inactive and unknown package status are non-actionable without exposing raw values', async t => {
  for (const [status, message] of [
    ['Inactive', 'Tạm ngừng cung cấp'],
    ['Draft', 'Chưa có thông tin về tình trạng cung cấp'],
  ]) {
    globalThis.fetch = async () => Response.json({ ...activePackage, status })
    const destinations = []
    const host = await mount(t, destination => destinations.push(destination))
    assert.match(host.textContent, new RegExp(message))
    assert.doesNotMatch(host.textContent, /\bActive\b|\bInactive\b|\bDraft\b/)
    const register = [...host.querySelectorAll('button')].find(button => button.textContent.includes('Đăng ký dịch vụ'))
    assert.equal(register.disabled, true)
    await act(async () => register.click())
    assert.deepEqual(destinations, [])
  }
})

test('missing legacy fields render neutral values without leaking nulls or raw status', async t => {
  globalThis.fetch = async () => Response.json({
    id: 31, status: null, price: null, totalDoses: null,
    items: [{ vaccineId: 12, vaccineCode: null, vaccineName: null, quantity: null }],
  })
  const host = await mount(t)

  assert.match(host.textContent, /Thông tin gói vắc xin/)
  assert.match(host.textContent, /Chưa có thông tin về tình trạng cung cấp/)
  assert.match(host.textContent, /Giá niêm yết.*Chưa có thông tin/)
  assert.match(host.textContent, /Phòng bệnh.*Chưa có thông tin/)
  assert.match(host.textContent, /Tổng số mũi.*Chưa có thông tin/)
  assert.match(host.textContent, /Chưa có tên vắc xin/)
  assert.match(host.textContent, /Chưa có thông tin mô tả/)
  assert.match(host.textContent, /Đối tượng tham khảo.*Chưa có thông tin/)
  assert.doesNotMatch(host.textContent, /undefined|null|\bActive\b|\bInactive\b/)
  assert.equal(host.querySelector('.package-detail-summary > button').disabled, true)
})

test('package detail keeps retry and not-found states customer-friendly', async t => {
  let attempts = 0
  globalThis.fetch = async () => {
    attempts += 1
    if (attempts === 1) throw new Error('internal response details')
    return Response.json(activePackage)
  }
  const host = await mount(t)
  assert.match(host.querySelector('[role="alert"]').textContent, /Không tải được chi tiết gói vắc xin/)
  assert.doesNotMatch(host.textContent, /internal response details/)
  await act(async () => host.querySelector('[role="alert"] button').click())
  assert.equal(attempts, 2)
  assert.match(host.querySelector('h1').textContent, /Gói vắc xin thử/)

  globalThis.fetch = async () => Response.json({}, { status: 404 })
  const missing = await mount(t)
  assert.match(missing.textContent, /Không tìm thấy gói vắc xin/)
})

test('invalid package IDs render not-found without making a request', async t => {
  let requests = 0
  globalThis.fetch = async () => { requests += 1; return Response.json(activePackage) }
  const host = await mount(t, () => {}, '0')
  assert.equal(requests, 0)
  assert.match(host.textContent, /Không tìm thấy gói vắc xin/)
})
