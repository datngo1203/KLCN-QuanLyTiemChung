import test, { before, after, beforeEach, afterEach } from 'node:test'
import assert from 'node:assert/strict'
import { fileURLToPath } from 'node:url'
import { Window } from 'happy-dom'
import { createServer } from 'vite'
import { createElement, act } from 'react'
import { createRoot } from 'react-dom/client'

let server, History, identity, window, host, root
const customer = { accountType: 'customer', accountId: 1 }
const shot = id => ({ id, appointmentId: id + 10, registrationId: id + 20,
  recipient: { id, fullName: `Người tiêm API ${id}` }, vaccine: { id, name: `Vắc xin API ${id}` },
  lotNumber: `LOT-${id}`, injectionSite: `Vị trí ${id}`, administeredAt: '2026-11-01T03:00:00+00:00', status: 'DaTiem' })
const text = () => host.textContent
const buttons = () => [...host.querySelectorAll('.payment-select')]
const button = label => [...host.querySelectorAll('button')].find(b => b.textContent === label)
const render = async (account = customer, sessionBusy = false, go = () => {}) => {
  await act(async () => root.render(createElement(History, { account, sessionBusy, go })))
}
const click = async element => { assert.ok(element); await act(async () => element.click()) }

before(async () => {
  server = await createServer({ root: fileURLToPath(new URL('../..', import.meta.url)), server: { middlewareMode: true }, logLevel: 'error' })
  History = (await server.ssrLoadModule('/src/VaccinationHistory.jsx')).default
  identity = (await server.ssrLoadModule('/src/api/identity.js')).identity
})
after(async () => { await server?.close() })
beforeEach(() => {
  window = new Window()
  globalThis.window = window; globalThis.document = window.document
  globalThis.IS_REACT_ACT_ENVIRONMENT = true
  window.HTMLElement.prototype.scrollIntoView = () => {}
  host = document.createElement('div'); document.body.append(host); root = createRoot(host)
  identity.vaccinationHistory = async () => ({ items: [] })
})
afterEach(async () => {
  await act(async () => root.unmount())
  assert.equal(window.localStorage.length, 0); assert.equal(window.sessionStorage.length, 0)
  assert.equal(document.cookie, '')
  await window.happyDOM.close()
})

test('history guest and employee use the account gate without requesting private data', async () => {
  let calls = 0, destination
  identity.vaccinationHistory = async () => { calls++; return { items: [] } }
  await render(null, false, next => { destination = next })
  assert.match(text(), /Đăng nhập để xem lịch sử tiêm/)
  await click(button('Vào Tài khoản')); assert.equal(destination, 'account')
  await render({ accountType: 'employee', accountId: 2 })
  assert.equal(calls, 0); assert.equal(buttons().length, 0)
})

test('customer loading contains no previous or Sample rows and disables reload', async () => {
  const pending = Promise.withResolvers()
  identity.vaccinationHistory = () => pending.promise
  await render(); assert.match(text(), /Đang tải lịch sử tiêm/)
  assert.equal(button('Tải lại danh sách').disabled, true); assert.equal(buttons().length, 0)
  await act(async () => pending.resolve({ items: [shot(1)] }))
  assert.equal(buttons().length, 1); assert.doesNotMatch(text(), /Sample|Dữ liệu mẫu/)
})

test('list and local DTO detail select the correct shot, time, lot and keyboard focus without another request', async () => {
  let calls = 0
  identity.vaccinationHistory = async () => { calls++; return { items: [shot(2), shot(1)] } }
  await render(); assert.equal(buttons().length, 2)
  await click(buttons()[0]); assert.equal(document.activeElement, host.querySelector('#history-detail h2'))
  assert.match(host.querySelector('#history-detail').textContent, /Người tiêm API 2/)
  await click(buttons()[1]); await click(buttons()[1])
  const detail = host.querySelector('#history-detail')
  assert.match(detail.textContent, /LOT-1/); assert.match(detail.textContent, /Vị trí 1/)
  assert.doesNotMatch(detail.textContent, /Người tiêm API 2/)
  assert.equal(detail.querySelector('time').getAttribute('dateTime'), shot(1).administeredAt)
  assert.equal(detail.querySelector('time').textContent, new Date(shot(1).administeredAt).toLocaleString('vi-VN'))
  assert.equal(calls, 1); assert.equal(document.activeElement, detail.querySelector('h2'))
})

test('empty history has no invented doses and the next page is disabled', async () => {
  await render(); assert.match(text(), /Không có mũi tiêm được ghi nhận/)
  assert.equal(buttons().length, 0); assert.equal(button('Trang sau').disabled, true)
  assert.equal(button('Trang trước').disabled, true)
})

for (const status of [401, 403, 503]) {
  test(`HTTP ${status} clears rows, uses safe feedback and offers the appropriate action`, async () => {
    let failed = false, destination
    identity.vaccinationHistory = async () => {
      if (failed) throw Object.assign(new Error('SQL PRIVATE'), { status })
      return { items: [shot(1)] }
    }
    await render(customer, false, next => { destination = next })
    await click(buttons()[0]); failed = true; await click(button('Tải lại danh sách'))
    assert.equal(buttons().length, 0); assert.equal(host.querySelector('#history-detail time'), null)
    assert.ok(host.querySelector('[role=alert]')); assert.doesNotMatch(text(), /SQL PRIVATE/)
    if (status === 401) { await click(button('Vào Tài khoản')); assert.equal(destination, 'account') }
    else { failed = false; await click(button('Thử lại')); assert.equal(buttons().length, 1) }
  })
}

test('pagination and reload clear selection, fetch the requested SQL page and restore list focus', async () => {
  const queries = []
  identity.vaccinationHistory = async query => {
    queries.push(query)
    return { items: query.includes('page=2&') ? [shot(26), shot(27)] : Array.from({ length: 25 }, (_, i) => shot(i + 1)) }
  }
  await render(); await click(buttons()[0]); await click(button('Trang sau'))
  assert.equal(buttons().length, 2); assert.equal(host.querySelector('#history-detail time'), null)
  assert.equal(document.activeElement, host.querySelector('#history-list-title'))
  assert.equal(button('Trang sau').disabled, true)
  await click(button('Trang trước')); assert.equal(buttons().length, 25)
  await click(button('Tải lại danh sách'))
  assert.deepEqual(queries, ['?page=1&pageSize=25', '?page=2&pageSize=25', '?page=1&pageSize=25', '?page=1&pageSize=25'])
})

test('late A response cannot replace B after account change', async () => {
  const pending = Promise.withResolvers(); let calls = 0
  identity.vaccinationHistory = () => ++calls === 1 ? pending.promise : Promise.resolve({ items: [shot(2)] })
  await render(); await render({ ...customer, accountId: 2 })
  await act(async () => pending.resolve({ items: [shot(1)] }))
  assert.match(text(), /Người tiêm API 2/); assert.doesNotMatch(text(), /Người tiêm API 1/)
})

test('logout clears detail and ignores an in-flight response', async () => {
  const pending = Promise.withResolvers(); let calls = 0
  identity.vaccinationHistory = () => ++calls === 1 ? Promise.resolve({ items: [shot(1)] }) : pending.promise
  await render(); await click(buttons()[0]); await click(button('Tải lại danh sách'))
  await render(null); await act(async () => pending.resolve({ items: [shot(1)] }))
  assert.doesNotMatch(text(), /Người tiêm API 1|LOT-1/); assert.equal(buttons().length, 0)
})

test('sessionBusy during login/logout/password removes workspace immediately and ignores its old response', async () => {
  const pending = Promise.withResolvers(); let calls = 0
  identity.vaccinationHistory = () => ++calls === 1 ? pending.promise : Promise.resolve({ items: [shot(2)] })
  await render(); await render(customer, true)
  assert.match(text(), /Đang cập nhật phiên/); assert.equal(buttons().length, 0)
  await act(async () => pending.resolve({ items: [shot(1)] }))
  assert.doesNotMatch(text(), /Người tiêm API 1/)
  await render(); assert.match(text(), /Người tiêm API 2/); assert.equal(calls, 2)
})
