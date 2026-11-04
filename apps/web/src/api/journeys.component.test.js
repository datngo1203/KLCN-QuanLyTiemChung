import test, { before, after } from 'node:test'
import assert from 'node:assert/strict'
import { fileURLToPath } from 'node:url'
import { Window } from 'happy-dom'
import { createServer } from 'vite'
import { createElement, act } from 'react'
import { createRoot } from 'react-dom/client'

let server, App, Journeys, identity

before(async () => {
  server = await createServer({ root: fileURLToPath(new URL('../..', import.meta.url)),
    server: { middlewareMode: true, ws: false }, logLevel: 'silent' })
  App = (await server.ssrLoadModule('/src/App.jsx')).default
  Journeys = (await server.ssrLoadModule('/src/VaccinationJourneys.jsx')).default
  identity = (await server.ssrLoadModule('/src/api/identity.js')).identity
})
after(async () => { await server?.close() })

function mount(t, path = '') {
  const window = new Window({ url: 'http://localhost/' + path })
  Object.assign(globalThis, { window, document: window.document, IS_REACT_ACT_ENVIRONMENT: true })
  const host = document.createElement('div')
  document.body.append(host)
  const root = createRoot(host)
  t.after(async () => {
    await act(async () => root.unmount())
    await window.happyDOM.close()
  })
  return { host, window, render: (component, props) => act(async () => root.render(createElement(component, props))) }
}

test('the shell normalizes old routes and focuses main on hash navigation', async t => {
  const { host, window, render } = mount(t, '#payment')
  await render(App)
  assert.equal(window.location.hash, '#vaccinations')
  assert.deepEqual([...host.querySelectorAll('#main-navigation button')].map(button => button.textContent),
    ['Trang chủ', 'Danh mục', 'Hồ sơ tiêm chủng', 'Ưu đãi', 'Đăng ký tiêm', 'Tiêm chủng của tôi'])
  assert.equal(document.activeElement, host.querySelector('main'))
  async function navigate(hash) {
    await act(async () => {
      window.location.hash = hash
      window.dispatchEvent(new window.Event('hashchange'))
    })
  }
  await navigate('#history')
  assert.equal(window.location.hash, '#vaccinations/history')
  assert.equal(host.querySelector('.journey-tabs [aria-current]').textContent, 'Lịch sử tiêm')
  await navigate('#vaccinations/42')
  assert.equal(host.querySelector('.journey-tabs [aria-current]').textContent, 'Đang thực hiện')
  await navigate('#schedule')
  assert.equal(window.location.hash, '#vaccinations')
})

test('embedded payment refreshes the same journey and switches to a receipt', async t => {
  const { host, render } = mount(t)
  const account = { accountType: 'customer', accountId: 1 }
  const person = { fullName: 'Nguyễn Minh Anh' }
  const payment = { registrationId: 42, customer: person, recipient: person, items: [],
    subtotal: '9007199254740993', totalDue: '9007199254740993', payment: null }
  const facts = { registrationId: 42, createdAt: '2026-11-04T00:00:00Z', payment, appointments: [], shots: [] }
  const calls = []
  identity.getVaccinationJourney = async id => { calls.push(id); return facts }
  await render(Journeys, { account, page: 'vaccinations/42' })
  assert.match(host.textContent, /Chờ thanh toán/)
  assert.equal(host.querySelectorAll('[name=method]').length, 4)
  facts.payment = { ...payment, paymentState: 'ThanhCong',
    payment: { id: 3, amount: '9007199254740993', method: 'TienMat', occurredAt: facts.createdAt } }
  await act(async () => host.querySelector('.payment-reload').click())
  assert.deepEqual(calls, [42, 42])
  assert.match(host.textContent, /Chờ trung tâm xác nhận lịch/)
  assert.match(host.textContent, /Biên nhận thanh toán/)
  assert.equal(host.querySelectorAll('[name=method]').length, 0)
})
