import test, { before, after } from 'node:test'
import assert from 'node:assert/strict'
import { fileURLToPath } from 'node:url'
import { Window } from 'happy-dom'
import { createServer } from 'vite'
import { createElement, act } from 'react'
import { createRoot } from 'react-dom/client'

let server, App, Journeys, Schedule, identity

before(async () => {
  server = await createServer({ root: fileURLToPath(new URL('../..', import.meta.url)),
    server: { middlewareMode: true, ws: false }, logLevel: 'silent' })
  App = (await server.ssrLoadModule('/src/App.jsx')).default
  Journeys = (await server.ssrLoadModule('/src/VaccinationJourneys.jsx')).default
  Schedule = (await server.ssrLoadModule('/src/Schedule.jsx')).default
  identity = (await server.ssrLoadModule('/src/api/identity.js')).identity
})
after(async () => { await server?.close() })

test('cancelled journey history is labelled and refresh shows rescheduled time without customer actions', async t => {
  const { host, render } = mount(t)
  const appointment = { id: 8, status: 'DaHuy', scheduledAt: '2026-12-01T01:30:00Z', location: 'Trung tâm tiêm chủng', serviceName: 'Dịch vụ đã đăng ký' }
  const facts = { registrationId: 42, createdAt: '2026-11-04T00:00:00Z', preferredAt: '2026-12-01T14:45:00', preferredTimeSpecified: true,
    payment: { registrationId: 42, recipient: { fullName: 'Nguyễn Test A' }, customer: { fullName: 'Nguyễn Test A' },
      items: [], totalDue: '0', subtotal: '0', payment: null }, appointments: [appointment], nextAppointment: null, shots: [], progress: [] }
  identity.getVaccinationJourney = async () => facts
  await render(Journeys, { account: { accountType: 'customer' }, page: 'vaccinations/42' })
  assert.match(host.textContent, /Đã hủy/)
  assert.match(host.textContent, /Thời gian mong muốn 01\/12\/2026 14:45 \(giờ Việt Nam\)/)
  assert.match(host.textContent, /Lịch hẹn đã xác nhận/)
  assert.match(host.textContent, /khám sàng lọc/)
  assert.match(host.textContent, /Chờ trung tâm xếp lịch mới/)
  assert.doesNotMatch(host.textContent, /Lịch hẹn đã qua|DaHuy|DaXepLich/)
  assert.ok(![...host.querySelectorAll('button')].some(b => /Đổi lịch|Hủy lịch/.test(b.textContent)))
  facts.appointments = [{ ...appointment, status: 'DaXepLich', scheduledAt: '2026-12-03T03:30:00Z' }]
  facts.nextAppointment = facts.appointments[0]
  await act(async () => host.querySelector('.payment-reload').click())
  assert.match(host.textContent, /3\/12\/2026/)
  assert.match(host.textContent, /Đã xếp lịch/)
})

test('schedule list and owned detail show cancelled status and reload the current instant', async t => {
  const { host, render } = mount(t)
  const person = { id: 1, fullName: 'Nguyễn Test A' }
  let row = { id: 7, registrationId: 42, customer: person, recipient: person, itemKind: 'vaccine', serviceName: 'Dịch vụ đã đăng ký',
    status: 'DaHuy', scheduledAt: '2026-12-01T01:30:00Z', location: 'Trung tâm' }
  identity.appointments = async () => ({ items: [row] })
  identity.getAppointment = async () => row
  await render(Schedule, { account: { accountType: 'customer', accountId: 1 }, go: () => {} })
  await act(async () => host.querySelector('.payment-select').click())
  assert.match(host.querySelector('#schedule-detail').textContent, /Đã hủy/)
  assert.doesNotMatch(host.textContent, /DaHuy|DaXepLich/)
  assert.ok(![...host.querySelectorAll('button')].some(b => /Đổi lịch|Hủy lịch/.test(b.textContent)))
  row = { ...row, status: 'DaXepLich', scheduledAt: '2026-12-03T03:30:00Z' }
  await act(async () => [...host.querySelectorAll('button')].find(b => b.textContent.includes('Tải lại chi tiết')).click())
  assert.match(host.querySelector('#schedule-detail').textContent, /3\/12\/2026/)
  assert.match(host.querySelector('#schedule-detail').textContent, /Đã xếp lịch/)
})

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

test('journey shows registered progress and confirmed dates without deriving regimen dates', async t => {
  const { host, render } = mount(t)
  const appointment = { id: 8, scheduledAt: '2026-12-01T01:30:00Z', location: 'Trung tâm tiêm chủng', serviceName: 'Gói đã đăng ký' }
  const facts = { registrationId: 42, createdAt: '2026-11-04T00:00:00Z',
    payment: { registrationId: 42, recipient: { fullName: 'Nguyễn Minh Anh' }, customer: { fullName: 'Nguyễn Minh Anh' },
      items: [], totalDue: '0', subtotal: '0', payment: null }, appointments: [appointment], nextAppointment: appointment, shots: [],
    progress: [
      { vaccineId: 1, vaccineName: 'Avaxim 80U', registeredDoses: 3, administeredDoses: 1, remainingDoses: 2, nextAppointment: appointment },
      { vaccineId: 2, vaccineName: 'Havax 0,5 ml', registeredDoses: 1, administeredDoses: 1, remainingDoses: 0, nextAppointment: null },
      { vaccineId: 3, vaccineName: 'Hồ sơ cũ', registeredDoses: null, administeredDoses: 0, remainingDoses: null, nextAppointment: null },
    ] }
  identity.getVaccinationJourney = async () => facts
  await render(Journeys, { account: { accountType: 'customer' }, page: 'vaccinations/42' })
  const rows = host.querySelectorAll('.journey-progress li')
  assert.match(rows[0].textContent, /Đã đăng ký 3 mũi · Đã tiêm 1 · Còn lại 2/)
  assert.match(rows[0].textContent, /Lịch hẹn đã xác nhận tiếp theo:.*1\/12\/2026/)
  assert.match(rows[1].textContent, /Còn lại 0/)
  assert.match(rows[1].textContent, /Chưa có lịch hẹn tiếp theo được xác nhận/)
  assert.match(rows[2].textContent, /Chưa xác định số mũi còn lại/)
  assert.doesNotMatch(host.textContent, /mũi nhắc lại|phác đồ|Tự động/i)
  facts.progress = []
  await act(async () => host.querySelector('.payment-reload').click())
  assert.match(host.textContent, /Chưa đủ thông tin để xác định số mũi đã đăng ký/)
})


for (const supplied of [true, false]) test(`journey midnight display respects explicit provenance ${supplied}`, async t => {
  const { host, render } = mount(t)
  identity.getVaccinationJourney = async () => ({ registrationId: 42, createdAt: '2026-11-04T00:00:00Z',
    preferredDate: '2026-12-01', preferredAt: '2026-12-01T00:00:00', preferredTimeSpecified: supplied,
    payment: { registrationId: 42, recipient: { fullName: 'Nguyễn Test A' }, customer: { fullName: 'Nguyễn Test A' },
      items: [], totalDue: '0', subtotal: '0', payment: null }, appointments: [], nextAppointment: null, shots: [], progress: [] })
  await render(Journeys, { account: { accountType: 'customer' }, page: 'vaccinations/42' })
  if (supplied) assert.match(host.textContent, /01\/12\/2026 00:00 \(giờ Việt Nam\)/)
  else { assert.match(host.textContent, /01\/12\/2026 · Chưa ghi nhận giờ mong muốn/); assert.doesNotMatch(host.textContent, /01\/12\/2026 00:00/) }
})
