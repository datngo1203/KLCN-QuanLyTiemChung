import test, { after, before } from 'node:test'
import assert from 'node:assert/strict'
import { fileURLToPath } from 'node:url'
import { Window } from 'happy-dom'
import { createServer } from 'vite'
import { act, createElement, useEffect, useState } from 'react'

let server
let Catalog

before(async () => {
  server = await createServer({ root: fileURLToPath(new URL('../..', import.meta.url)),
    server: { middlewareMode: true, ws: false }, logLevel: 'silent' })
  Catalog = (await server.ssrLoadModule('/src/Catalog.jsx')).default
})

after(async () => { await server?.close() })

const vaccines = [{
  id: 21, code: 'VX-21', name: 'Vắc xin A', diseases: [{ id: 7, code: 'DIS-7', name: 'Bệnh A' }],
  manufacturer: 'Hãng A', origin: 'Việt Nam', price: '100000',
}]
const packages = [{
  id: 31, code: 'PK-31', name: 'Gói B', diseases: [{ id: 7, code: 'DIS-7', name: 'Bệnh A' }],
  totalDoses: 3, price: '300000', items: [{ vaccineId: 21, vaccineCode: 'VX-21', vaccineName: 'Vắc xin A', quantity: 3 }],
}]

function catalogResponse(request, empty = false) {
  if (request.pathname === '/api/v1/catalog/diseases')
    return Response.json({ items: [{ id: 7, code: 'DIS-7', name: 'Bệnh A' }] })
  if (request.pathname === '/api/v1/catalog/vaccine-technologies')
    return Response.json({ items: [{ id: 12, name: 'Công nghệ A' }] })
  if (request.pathname === '/api/v1/catalog/vaccines')
    return Response.json({ source: 'database', items: empty ? [] : vaccines })
  if (request.pathname === '/api/v1/catalog/packages')
    return Response.json({ source: 'database', items: empty ? [] : packages })
  assert.fail(`Unexpected catalog request: ${request.pathname}`)
}

async function mount(t, fetcher, initialQuery = '') {
  const window = new Window({ url: 'http://localhost/#catalog' })
  const previousGlobals = {
    window: globalThis.window,
    document: globalThis.document,
    IS_REACT_ACT_ENVIRONMENT: globalThis.IS_REACT_ACT_ENVIRONMENT,
    requestAnimationFrame: globalThis.requestAnimationFrame,
  }
  Object.assign(globalThis, { window, document: window.document, IS_REACT_ACT_ENVIRONMENT: true,
    requestAnimationFrame: window.requestAnimationFrame.bind(window) })
  const originalFetch = globalThis.fetch
  globalThis.fetch = fetcher
  const host = document.createElement('div')
  document.body.append(host)
  const { createRoot } = await import('react-dom/client')
  const root = createRoot(host)
  const querySetter = { current: null }
  function Harness() {
    const [query, setQuery] = useState(initialQuery)
    useEffect(() => { querySetter.current = setQuery }, [setQuery])
    return createElement(Catalog, { query, setQuery })
  }
  t.after(async () => {
    await act(async () => root.unmount())
    globalThis.fetch = originalFetch
    await window.happyDOM.close()
    for (const [key, value] of Object.entries(previousGlobals)) {
      if (value === undefined) delete globalThis[key]
      else globalThis[key] = value
    }
  })
  return {
    host,
    render: () => act(async () => root.render(createElement(Harness))),
    settle: () => act(async () => new Promise(resolve => setTimeout(resolve, 0))),
    updateQuery: value => act(async () => querySetter.current(value)),
    window,
  }
}

function setValue(window, element, value) {
  const prototype = element.tagName === 'SELECT' ? window.HTMLSelectElement.prototype : window.HTMLInputElement.prototype
  Object.getOwnPropertyDescriptor(prototype, 'value').set.call(element, value)
  element.dispatchEvent(new window.Event(element.tagName === 'SELECT' ? 'change' : 'input', { bubbles: true }))
  if (element.tagName !== 'SELECT') element.dispatchEvent(new window.Event('change', { bubbles: true }))
}

test('catalog loads Active-only results and lookup-driven discovery filters', async t => {
  const calls = []
  const { host, render, settle, updateQuery, window } = await mount(t, async url => {
    const request = new URL(url)
    calls.push(request)
    return catalogResponse(request)
  })
  await render()
  await settle()

  assert.deepEqual(calls.map(request => request.pathname), [
    '/api/v1/catalog/diseases', '/api/v1/catalog/vaccine-technologies',
    '/api/v1/catalog/vaccines', '/api/v1/catalog/packages',
  ])
  for (const request of calls) {
    if (request.pathname !== '/api/v1/catalog/diseases' && request.pathname !== '/api/v1/catalog/vaccine-technologies') continue
    assert.equal(request.searchParams.get('status'), 'Active')
  }
  for (const request of calls.filter(item => /\/catalog\/(vaccines|packages)$/.test(item.pathname)))
    assert.equal(request.searchParams.get('status'), 'Active')

  const search = host.querySelector('input[aria-label="Tìm theo mã, tên, hãng hoặc phòng bệnh"]')
  assert.equal(search.getAttribute('aria-label'), 'Tìm theo mã, tên, hãng hoặc phòng bệnh')
  assert.match(search.placeholder, /mã, tên, hãng hoặc phòng bệnh/)
  assert.equal(host.querySelector('select[name="diseaseId"] option[value="7"]').textContent, 'Bệnh A')
  assert.equal(host.querySelector('select[name="technologyId"] option[value="12"]').textContent, 'Công nghệ A')
  assert.match(host.querySelector('tbody').textContent, /VX-21.*Vắc xin A.*Bệnh A/s)
  assert.match(host.querySelector('tbody').textContent, /Hãng A/)
  assert.match(host.querySelector('tbody').textContent, /Việt Nam/)
  assert.match(host.querySelector('tbody').textContent, /100\.000/)
  assert.equal(host.querySelector('a[aria-label="Xem chi tiết Vắc xin A"]').getAttribute('href'), '#catalog/vaccines/21')
  assert.match(host.querySelector('.catalog-card').textContent, /PK-31.*Gói B.*Bệnh A.*Tổng số mũi: 3.*VX-21 — Vắc xin A × 3/s)
  assert.match(host.querySelector('.catalog-card').textContent, /300\.000/)
  assert.doesNotMatch(host.textContent, /\bActive\b/)
  assert.equal([...host.querySelectorAll('a')].some(link => link.getAttribute('href')?.startsWith('#catalog/packages/')), false)

  const initialCallCount = calls.length
  for (const [label, expectedVaccine, expectedPackage] of [
    ['Vắc xin lẻ', true, false], ['Gói vắc xin', false, true], ['Tất cả', true, true],
  ]) {
    await act(async () => {
      [...host.querySelectorAll('.filter-tab')].find(button => button.textContent === label).click()
    })
    assert.equal(Boolean(host.querySelector('.catalog-table')), expectedVaccine)
    assert.equal(Boolean(host.querySelector('.catalog-card')), expectedPackage)
    assert.equal(calls.length, initialCallCount)
  }

  await act(async () => {
    setValue(window, host.querySelector('select[name="diseaseId"]'), '7')
    setValue(window, host.querySelector('select[name="technologyId"]'), '12')
    setValue(window, host.querySelector('input[name="minPrice"]'), '25000')
    setValue(window, host.querySelector('input[name="maxPrice"]'), '400000')
    await new Promise(resolve => setTimeout(resolve, 0))
  })
  await updateQuery('Vắc xin A & B')
  assert.equal(search.value, 'Vắc xin A & B')
  await act(async () => {
    host.querySelector('form').dispatchEvent(new window.Event('submit', { bubbles: true, cancelable: true }))
    await new Promise(resolve => setTimeout(resolve, 0))
  })

  const submittedRequests = calls.slice(-4)
  const listRequests = submittedRequests.filter(request => /\/catalog\/(vaccines|packages)$/.test(request.pathname))
  assert.equal(listRequests.length, 2)
  for (const request of listRequests) {
    assert.equal(request.searchParams.get('search'), 'Vắc xin A & B')
    assert.equal(request.searchParams.get('diseaseId'), '7')
    assert.equal(request.searchParams.get('technologyId'), '12')
    assert.equal(request.searchParams.get('minPrice'), '25000')
    assert.equal(request.searchParams.get('maxPrice'), '400000')
    assert.equal(request.searchParams.get('status'), 'Active')
  }
})

test('catalog shows empty results and keeps the submit control disabled while loading', async t => {
  const calls = []
  let release
  const gate = new Promise(resolve => { release = resolve })
  const { host, render, settle } = await mount(t, async url => {
    const request = new URL(url)
    calls.push(request)
    await gate
    return catalogResponse(request, true)
  })
  await render()
  assert.equal(host.querySelector('button[type="submit"]').disabled, true)
  assert.match(host.querySelector('[role="status"]').textContent, /Đang tải/)
  release()
  await settle()
  assert.match(host.querySelector('.empty-state').textContent, /Không có mục phù hợp/)
  assert.equal(calls.length, 4)
})

test('catalog error retry hides API details and preserves the submitted query and filters', async t => {
  const calls = []
  let failNextVaccineRequest = false
  const { host, render, settle, updateQuery, window } = await mount(t, async url => {
    const request = new URL(url)
    calls.push(request)
    if (request.pathname === '/api/v1/catalog/vaccines' && failNextVaccineRequest) {
      failNextVaccineRequest = false
      return new Response(null, { status: 503 })
    }
    return catalogResponse(request)
  })
  await render()
  await settle()

  await act(async () => {
    setValue(window, host.querySelector('select[name="diseaseId"]'), '7')
    setValue(window, host.querySelector('select[name="technologyId"]'), '12')
    setValue(window, host.querySelector('input[name="minPrice"]'), '25000')
    setValue(window, host.querySelector('input[name="maxPrice"]'), '400000')
    await new Promise(resolve => setTimeout(resolve, 0))
  })
  await updateQuery('retry query')
  failNextVaccineRequest = true
  await act(async () => {
    host.querySelector('form').dispatchEvent(new window.Event('submit', { bubbles: true, cancelable: true }))
    await new Promise(resolve => setTimeout(resolve, 0))
  })
  assert.match(host.querySelector('[role="alert"]').textContent, /Không thể tải danh mục.*Vui lòng thử lại/)
  assert.doesNotMatch(host.querySelector('[role="alert"]').textContent, /HTTP 503|Internal Server Error/)
  await act(async () => {
    host.querySelector('[role="alert"] button').click()
    await new Promise(resolve => setTimeout(resolve, 0))
  })
  await settle()

  assert.equal(host.querySelector('input[aria-label="Tìm theo mã, tên, hãng hoặc phòng bệnh"]').value, 'retry query')
  assert.equal(host.querySelector('select[name="diseaseId"]').value, '7')
  assert.equal(host.querySelector('select[name="technologyId"]').value, '12')
  assert.equal(host.querySelector('input[name="minPrice"]').value, '25000')
  assert.equal(host.querySelector('input[name="maxPrice"]').value, '400000')
  const retryRequests = calls.slice(-4).filter(request => /\/catalog\/(vaccines|packages)$/.test(request.pathname))
  assert.equal(retryRequests.length, 2)
  for (const request of retryRequests) {
    assert.equal(request.searchParams.get('search'), 'retry query')
    assert.equal(request.searchParams.get('diseaseId'), '7')
    assert.equal(request.searchParams.get('technologyId'), '12')
    assert.equal(request.searchParams.get('minPrice'), '25000')
    assert.equal(request.searchParams.get('maxPrice'), '400000')
    assert.equal(request.searchParams.get('status'), 'Active')
  }
})
