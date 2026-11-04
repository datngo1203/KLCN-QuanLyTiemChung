import test, { before, after } from 'node:test'
import assert from 'node:assert/strict'
import { fileURLToPath } from 'node:url'
import { Window } from 'happy-dom'
import { createServer } from 'vite'
import { createElement, act } from 'react'

let server, Registrations, identity, createRoot, window
const customer = { id: 10, fullName: 'Hồ sơ gốc', phoneNumber: '0900000000', email: 'original@example.invalid' }
const recipient = { id: 20, customerId: 10, fullName: 'Người tiêm thử' }
const fetchOriginal = globalThis.fetch

before(async () => {
  window = new Window()
  globalThis.window = window
  globalThis.document = window.document
  globalThis.IS_REACT_ACT_ENVIRONMENT = true
  window.scrollTo = () => {}
  window.confirm = () => true
  createRoot = (await import('react-dom/client')).createRoot
  server = await createServer({ root: fileURLToPath(new URL('../..', import.meta.url)),
    server: { middlewareMode: true, hmr: false }, logLevel: 'silent' })
  Registrations = (await server.ssrLoadModule('/src/Registrations.jsx')).default
  identity = (await server.ssrLoadModule('/src/api/identity.js')).identity
})
after(async () => { globalThis.fetch = fetchOriginal; await server?.close(); await window?.happyDOM.close() })

async function render(t, fee, fail) {
  let price = 500000
  let posted = []
  const preferredDate = new Date(Date.now() + 24 * 3600000).toISOString().slice(0, 10)
  let changed = false
  globalThis.fetch = async url => Response.json({ source: 'database', items: url.endsWith('/vaccines')
    ? [{ id: 1, name: 'Vắc xin thử', price }] : [] })
  identity.profiles = async path => ({ items: path.startsWith('customers') ? [customer] : [recipient] })
  identity.registrations = async (path, method, body) => {
    if (path === '/settings') return { consultationFee: fee }
    if (method === 'POST') {
      posted.push(body)
      if (posted.length === 1 && fail === 'catalogChanged') {
        price = 510000; changed = true
        throw Object.assign(new Error('Catalog changed'), { code: fail, status: 409 })
      }
      if (posted.length === 1 && fail === 'network') throw new TypeError('offline')
      return { id: 7 }
    }
    if (path === '/7') return {
      customer: { ...customer, phoneNumber: '0909999999' }, recipient, createdAt: '2026-11-04T00:00:00Z',
      contact: { ...posted.at(-1).contact, fullName: 'Tên đọc từ snapshot' }, preferredDate,
      items: [{ kind: 'vaccine', serviceId: 1, name: 'Vắc xin thử', unitPrice: price, quantity: 1 }],
      subtotal: price, consultationFee: fee, totalBeforeDiscount: price + fee,
    }
    return { items: [] }
  }
  const host = document.createElement('div'); document.body.append(host)
  const root = createRoot(host)
  t.after(async () => { await act(async () => root.unmount()); host.remove() })
  await act(async () => root.render(createElement(Registrations, { account: customer, active: true, onSavingChange() {} })))
  const field = (name, tag = 'input') => [...host.querySelectorAll('label')].find(l => l.textContent.startsWith(name))?.querySelector(tag)
  const click = async text => {
    const button = [...host.querySelectorAll('button')].find(b => b.textContent === text)
    assert.ok(button, text); await act(async () => button.click())
  }
  const fill = async (name, value, tag = 'input') => {
    const element = field(name, tag); assert.ok(element, name)
    const prototype = tag === 'select' ? window.HTMLSelectElement.prototype : window.HTMLInputElement.prototype
    await act(async () => {
      Object.getOwnPropertyDescriptor(prototype, 'value').set.call(element, value)
      element.dispatchEvent(new window.Event(tag === 'select' ? 'change' : 'input', { bubbles: true }))
    })
  }
  const submit = () => act(async () => host.querySelector('form').dispatchEvent(new window.Event('submit', { bubbles: true, cancelable: true })))
  await click('Tạo đăng ký')
  await fill('Khách hàng', '10', 'select'); await fill('Người được tiêm', '20', 'select'); await submit()
  await click('Chọn'); await submit()
  assert.equal(field('Họ tên').value, customer.fullName)
  assert.equal(field('Số điện thoại').value, customer.phoneNumber)
  assert.equal(field('Email').value, customer.email)
  await submit()
  assert.match(host.textContent, /Tỉnh \/ Thành phố là bắt buộc/)
  await fill('Họ tên', 'Liên hệ riêng'); await fill('Số điện thoại', '09123456789')
  await fill('Email', 'contact@example.invalid'); await fill('Tỉnh', 'Tỉnh thử')
  await fill('Phường', 'Phường thử'); await fill('Địa chỉ', 'Đường thử'); await fill('Ngày mong muốn', preferredDate)
  await click('Quay lại'); await submit()
  assert.equal(field('Địa chỉ').value, 'Đường thử')
  assert.equal(field('Ngày mong muốn').value, preferredDate)
  await submit()
  assert.match(host.textContent, /Liên hệ riêng/)
  assert.match(host.textContent, fee ? /50\.000 đ/ : /Đã bao gồm \/ không thu riêng/)
  assert.match(host.textContent, fee ? /550\.000 đ/ : /500\.000 đ/)
  assert.equal(posted.length, 0)
  await submit()
  assert.equal(posted.length, 1)
  if (changed) {
    await submit()
    assert.equal(field('Họ tên').value, 'Liên hệ riêng')
    assert.equal(field('Ngày mong muốn').value, preferredDate)
    await submit()
    assert.match(host.textContent, /560\.000 đ/)
  } else if (fail === 'network') {
    assert.match(host.textContent, /Chưa xác định/)
    await submit(); assert.equal(posted.length, 1)
    await click('Kiểm tra danh sách đăng ký'); await click('Đã kiểm tra, mở lại bản nháp')
    assert.match(host.textContent, /Liên hệ riêng/)
    await click('Quay lại')
    assert.equal(field('Ngày mong muốn').value, preferredDate)
    await submit()
  }
  await submit()
  assert.equal(posted.length, 2)
  assert.equal(posted[1].contact.phoneNumber, '09123456789')
  assert.equal(posted[1].preferredDate, preferredDate)
  assert.equal(posted[1].consultationFee, undefined)
  assert.equal(customer.phoneNumber, '0900000000')
  assert.match(host.textContent, /Tên đọc từ snapshot/)
  assert.doesNotMatch(host.textContent, /0909999999|original@example.invalid/)
}

test('registration review retains contact and date after catalog change and renders the saved snapshot', t => render(t, 50000, 'catalogChanged'))
test('lost response blocks another POST and retains contact and date for manual reconciliation', t => render(t, 0, 'network'))
