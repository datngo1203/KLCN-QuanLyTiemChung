import test, { before, after } from 'node:test'
import assert from 'node:assert/strict'
import { fileURLToPath } from 'node:url'
import { Window } from 'happy-dom'
import { createServer } from 'vite'
import { createElement, act } from 'react'
import { registrationBody } from '../registrationForm.js'

let server, App, Registrations, PaymentDetail, identity, createRoot, window
const originalFetch = globalThis.fetch
before(async () => {
  window = new Window({ url: 'http://localhost/#promotions' })
  Object.assign(globalThis, { window, document: window.document, IS_REACT_ACT_ENVIRONMENT: true })
  window.scrollTo = () => {}
  window.HTMLElement.prototype.scrollIntoView = () => {}
  createRoot = (await import('react-dom/client')).createRoot
  server = await createServer({ root: fileURLToPath(new URL('../..', import.meta.url)),
    server: { middlewareMode: true, hmr: false }, logLevel: 'silent' })
  App = (await server.ssrLoadModule('/src/App.jsx')).default
  Registrations = (await server.ssrLoadModule('/src/Registrations.jsx')).default
  PaymentDetail = (await server.ssrLoadModule('/src/Payments.jsx')).PaymentDetail
  identity = (await server.ssrLoadModule('/src/api/identity.js')).identity
})
after(async () => { globalThis.fetch = originalFetch; await server?.close(); await window?.happyDOM.close() })

async function mount(t, component, props = {}) {
  const host = document.createElement('div'); document.body.append(host)
  const root = createRoot(host)
  t.after(async () => { await act(async () => root.unmount()); host.remove() })
  await act(async () => root.render(createElement(component, props)))
  const click = async text => {
    const button = [...host.querySelectorAll('button')].find(node => node.textContent === text)
    assert.ok(button, text)
    await act(async () => button.click())
  }
  const fill = async (label, value, tag = 'input') => {
    const input = [...host.querySelectorAll('label')].find(node => node.textContent.startsWith(label))?.querySelector(tag)
    assert.ok(input, label)
    const prototype = tag === 'select' ? window.HTMLSelectElement.prototype : window.HTMLInputElement.prototype
    await act(async () => {
      Object.getOwnPropertyDescriptor(prototype, 'value').set.call(input, value)
      input.dispatchEvent(new window.Event(tag === 'select' ? 'change' : 'input', { bubbles: true }))
    })
  }
  const submit = () => act(async () => host.querySelector('form').dispatchEvent(
    new window.Event('submit', { bubbles: true, cancelable: true })))
  return { host, click, fill, submit }
}
const promotion = { id: 3, name: 'Chương trình chăm sóc', description: 'Ưu đãi theo thời gian chương trình',
  startsAt: '2026-11-01T00:00:00Z', endsAt: '2026-11-30T23:59:59Z', discountAmount: 100000 }

test('Offers requests endpoint and shows loading, empty, error and retry states', async t => {
  window.location.hash = '#promotions'
  const pending = Promise.withResolvers()
  const requests = []
  let response = () => pending.promise
  globalThis.fetch = url => { requests.push(url); return response() }
  const ui = await mount(t, App)
  assert.match(ui.host.textContent, /Đang tải ưu đãi/)
  assert.equal(new URL(requests[0]).pathname, '/api/v1/promotions/valid')
  await act(async () => pending.resolve(Response.json({ items: [] })))
  assert.match(ui.host.textContent, /Không có ưu đãi phù hợp/)
  response = async () => new Response('', { status: 503 })
  await ui.click('Danh mục'); await ui.click('Ưu đãi')
  assert.match(ui.host.textContent, /Chưa tải được ưu đãi/)
  response = async () => Response.json({ items: [promotion] })
  await ui.click('Thử lại')
  assert.equal(requests.filter(url => url.endsWith('/promotions/valid')).length, 3)
  assert.match(ui.host.textContent, /Chương trình chăm sóc/)
  assert.match(ui.host.textContent, /100\.000 đ/)
  assert.match(ui.host.querySelector('.offer-period').textContent, /1\/11\/2026.*1\/12\/2026/)
  assert.doesNotMatch(ui.host.textContent, /THANG10|10%|Mã ưu đãi|KM-3|API \/ SQL|mô phỏng/)
})

test('Offers preserves exact VND JSON number text at the RF-04 money limit', async t => {
  window.location.hash = '#promotions'
  globalThis.fetch = async () => new Response(JSON.stringify({ items: [promotion] })
    .replace('"discountAmount":100000', '"discountAmount":9999999999999999'))
  const { host } = await mount(t, App)
  assert.match(host.textContent, /9\.999\.999\.999\.999\.999 đ/)
})

const owner = { id: 10, fullName: 'Khách thử', phoneNumber: '0900000000', email: 'test@example.invalid' }
const recipient = { id: 20, customerId: 10, fullName: 'Người tiêm thử' }
async function registration(t, price, offeredAmount, unavailable = false) {
  let offersRead = 0
  const posted = []
  globalThis.fetch = async url => {
    if (url.endsWith('/promotions/valid')) {
      offersRead++
      return Response.json({ items: [{ ...promotion, discountAmount: offeredAmount }] })
    }
    return Response.json({ source: 'database', items: url.endsWith('/vaccines')
      ? [{ id: 1, name: 'Dịch vụ thử', price }] : [] })
  }
  identity.profiles = async path => ({ items: path.startsWith('customers') ? [owner] : [recipient] })
  const saved = { id: 7, customer: owner, recipient, createdAt: '2026-11-05T00:00:00Z',
    items: [{ id: 1, kind: 'vaccine', serviceId: 1, name: 'Dịch vụ thử', quantity: 1, unitPrice: price }],
    subtotal: price, consultationFee: 50000, totalBeforeDiscount: price + 50000,
    promotionName: 'Tên ưu đãi đã chốt', discountAmount: 25000, totalDue: price + 25000 }
  const calls = []
  identity.registrations = async (path, method, body) => {
    calls.push([path, method])
    if (path === '/settings') return { consultationFee: 50000 }
    if (method === 'POST') {
      posted.push(body)
      if (unavailable && posted.length === 1) throw Object.assign(new Error('Unavailable'), { status: 409, code: 'promotionUnavailable' })
      return { id: 7 }
    }
    if (path === '/7') return { ...saved, contact: posted.at(-1).contact, preferredDate: posted.at(-1).preferredDate }
    return { items: [] }
  }
  const ui = await mount(t, Registrations, { account: { accountId: 1 }, active: true, go() {}, onSavingChange() {} })
  await ui.click('Tạo đăng ký'); await ui.fill('Người được tiêm', '20', 'select'); await ui.submit()
  await ui.click('Chọn'); await ui.submit()
  await ui.fill('Tỉnh', 'Tỉnh thử'); await ui.fill('Phường', 'Phường thử'); await ui.fill('Địa chỉ', 'Đường thử')
  const date = new Date(Date.now() + 86400000).toISOString().slice(0, 10)
  await ui.fill('Ngày mong muốn', date); await ui.submit()
  return { ...ui, posted, calls, offersRead: () => offersRead, date, saved }
}
function amount(host, label) {
  return [...host.querySelectorAll('dt')].find(node => node.textContent === label)?.nextElementSibling.textContent
}
for (const [price, offer, selected, discount, due] of [
  [150000, 100000, false, 0, 200000], [150000, 100000, true, 100000, 100000],
  [100000, 300000, true, 100000, 50000],
]) {
  test(`registration preserves fee and caps discount (service ${price}, promotion ${selected ? offer : 'none'})`, async t => {
    const ui = await registration(t, price, offer)
    assert.equal(ui.offersRead(), 1)
    if (selected) await ui.fill('Ưu đãi', '3', 'select')
    assert.equal(amount(ui.host, 'Phí khám/tư vấn'), '50.000 đ')
    assert.equal(amount(ui.host, 'Ưu đãi'), discount ? `−${discount.toLocaleString('vi-VN')} đ` : '0 đ')
    assert.equal(ui.host.querySelector('.registration-total').textContent, `Tổng thanh toán${due.toLocaleString('vi-VN')} đ`)
    await ui.submit()
    assert.equal(ui.posted[0].promotionId ?? null, selected ? 3 : null)
    assert.equal(ui.posted[0].items[0].expectedUnitPrice, String(price))
    assert.equal(JSON.parse(registrationBody(ui.posted[0])).items[0].expectedUnitPrice, price)
    assert.equal(ui.posted[0].discountAmount, undefined)
    assert.equal(ui.posted[0].consultationFee, undefined)
    assert.ok(ui.calls.some(([path, method]) => path === '/7' && method !== 'POST'))
    assert.match(ui.host.textContent, /Tên ưu đãi đã chốt/)
    assert.equal(ui.host.querySelector('.registration-total').textContent,
      `Tổng thanh toán${ui.saved.totalDue.toLocaleString('vi-VN')} đ`)
  })
}
test('unavailable promotion preserves services/contact/date and reloads options for reselection', async t => {
  const ui = await registration(t, 150000, 100000, true)
  await ui.fill('Ưu đãi', '3', 'select'); await ui.submit()
  assert.equal(ui.offersRead(), 2)
  assert.equal(ui.host.querySelector('select').value, '')
  assert.match(ui.host.textContent, /Ưu đãi vừa hết hiệu lực.*chọn lại/)
  assert.match(ui.host.textContent, /Dịch vụ thử/)
  await ui.click('Quay lại')
  const input = text => [...ui.host.querySelectorAll('label')].find(node => node.textContent.startsWith(text)).querySelector('input')
  assert.equal(input('Địa chỉ').value, 'Đường thử')
  assert.equal(input('Ngày mong muốn').value, ui.date)
  assert.equal(input('Họ tên').value, owner.fullName)
  await ui.submit(); await ui.submit()
  assert.equal(ui.posted.length, 2)
  assert.equal(ui.posted[1].promotionId ?? null, null)
  assert.deepEqual(ui.posted[1].items, ui.posted[0].items)
  assert.deepEqual(ui.posted[1].contact, ui.posted[0].contact)
  assert.equal(ui.posted[1].preferredDate, ui.posted[0].preferredDate)
})

test('payment renders service, fee, discount and authoritative total without technical wording', async t => {
  const detail = { registrationId: 7, customer: owner, recipient, items: [],
    subtotal: '150000', consultationFee: '50000', discountAmount: '100000', totalDue: '100000',
    paidAmount: '0', remainingAmount: '100000', payment: null, paymentState: 'ChuaThanhToan' }
  const { host } = await mount(t, PaymentDetail, { id: 7, initialDetail: detail, go() {}, accessDenied() {} })
  assert.equal(amount(host, 'Tạm tính dịch vụ'), '150.000 đ')
  assert.equal(amount(host, 'Phí khám/tư vấn'), '50.000 đ')
  assert.equal(amount(host, 'Ưu đãi'), '100.000 đ')
  assert.match(host.querySelector('.registration-total').textContent, /100\.000 đ/)
  assert.doesNotMatch(host.textContent, /API \/ SQL|Catalog SQL|mô phỏng|không xử lý tiền thật|Trung tâm tiêm chủng VAC/)
})
