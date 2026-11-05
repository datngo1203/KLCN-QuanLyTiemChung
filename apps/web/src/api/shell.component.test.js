import test from 'node:test'
import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { Window } from 'happy-dom'
import { createServer } from 'vite'
import { act, createElement } from 'react'

test('VNVC shell preserves the profile and auth routes without a second profile CTA', async t => {
  const window = new Window({ url: 'http://localhost/#schedule' })
  globalThis.window = window
  globalThis.document = window.document
  globalThis.IS_REACT_ACT_ENVIRONMENT = true
  window.scrollTo = () => {}
  const originalFetch = globalThis.fetch
  const catalogRequests = []
  globalThis.fetch = async url => {
    const path = new URL(url).pathname
    catalogRequests.push(path)
    const items = path.endsWith('/vaccines')
      ? [{ id: 42, name: 'Vắc xin kiểm thử API', price: 150000 }]
      : [{ id: 43, name: 'Gói tiêm kiểm thử API', price: 300000, items: [] }]
    return Response.json({ source: 'database', items })
  }
  const server = await createServer({ root: fileURLToPath(new URL('../..', import.meta.url)),
    server: { middlewareMode: true, hmr: false }, logLevel: 'silent' })
  const { createRoot } = await import('react-dom/client')
  const root = createRoot(document.body)
  t.after(async () => {
    await act(async () => root.unmount())
    globalThis.fetch = originalFetch
    await server.close()
    await window.happyDOM.close()
  })

  const { identity } = await server.ssrLoadModule('/src/api/identity.js')
  identity.login = async () => ({ accountId: 1, accountType: 'Customer' })
  identity.profiles = async () => ({ items: [] })
  identity.registrations = async () => ({ items: [] })
  const { default: App } = await server.ssrLoadModule('/src/App.jsx')
  await act(async () => root.render(createElement(App)))

  const header = document.querySelector('header')
  const profileEntries = () => [...header.querySelectorAll('button')].filter(button => button.textContent === 'Hồ sơ tiêm chủng')
  assert.equal(window.location.hash, '#vaccinations')
  assert.equal(header.querySelector('[aria-current="page"]').textContent, 'Tiêm chủng của tôi')
  assert.equal(header.querySelector('picture img').getAttribute('src'), '/assets/vnvc-logo-horizontal.png')
  assert.equal(header.querySelector('picture source').getAttribute('srcset'), '/assets/vnvc-logo-square.png')
  assert.match(header.textContent, /Trung tâm tiêm chủng VNVC/)
  assert.match(document.querySelector('footer address').textContent, /198 Hoàng Văn Thụ, P\. Đức Nhuận, TP\.HCM/)
  assert.doesNotMatch(header.textContent + document.querySelector('footer').textContent,
    /tiêm chủng VAC|140 Lê Trọng Tấn|phục vụ khóa luận|không phải website chính thức/i)
  assert.equal(profileEntries().length, 1)

  await act(async () => header.querySelector('.account-button').click())
  assert.equal(window.location.hash, '#account')
  assert.ok([...document.querySelectorAll('main button')].some(button => button.textContent === 'Tạo tài khoản khách hàng'))
  await act(async () => header.querySelector('.brand').click())
  const home = document.querySelector('main')
  const footer = document.querySelector('footer')
  const heroButton = label => [...home.querySelectorAll('.center-hero button')].find(button => button.textContent === label)
  assert.equal(home.querySelector('h1').textContent, 'VNVC Hoàng Văn Thụ')
  assert.match(home.textContent, /198 Hoàng Văn Thụ, P\. Đức Nhuận, TP\.HCM/)
  assert.match(home.textContent, /07:30 – 17:00/)
  assert.match(footer.textContent, /07:30 – 17:00.*Thứ Hai – Chủ nhật · Xuyên trưa/)
  for (const area of [home, footer]) {
    assert.equal(area.querySelector('a[href="tel:02871026595"]').textContent, '028 7102 6595')
    assert.equal(new URL(area.querySelector('a[href*="google.com/maps"]').href).searchParams.get('query'),
      '198 Hoàng Văn Thụ, P. Đức Nhuận, TP.HCM')
  }
  assert.doesNotMatch(home.textContent, /Tra cứu vắc xin|Tìm hiểu gói tiêm|Quản lý hồ sơ gia đình|Chủ động chăm sóc sức khỏe/)
  assert.doesNotMatch(footer.textContent, /example@email\.com|Giờ làm việc dự kiến/)
  assert.ok(catalogRequests.includes('/api/v1/catalog/vaccines'))
  assert.ok(catalogRequests.includes('/api/v1/catalog/packages'))
  assert.match(home.textContent, /Vắc xin kiểm thử API/)
  assert.match(home.textContent, /Gói tiêm kiểm thử API/)
  await act(async () => heroButton('Đăng ký tiêm').click())
  assert.equal(window.location.hash, '#register')
  await act(async () => header.querySelector('.brand').click())
  await act(async () => heroButton('Xem danh mục vắc xin').click())
  assert.equal(window.location.hash, '#catalog')
  // Start login from the profile route without a return-after-login destination.
  await act(async () => profileEntries()[0].click())
  await act(async () => {
    const fields = document.querySelectorAll('main form input')
    for (const [field, value] of [[fields[0], 'customer@example.invalid'], [fields[1], 'synthetic-password']]) {
      Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set.call(field, value)
      field.dispatchEvent(new window.Event('input', { bubbles: true }))
    }
    document.querySelector('main form').dispatchEvent(new window.Event('submit', { bubbles: true, cancelable: true }))
  })
  assert.equal(header.querySelector('.account-button'), null)
  assert.equal(profileEntries().length, 1)
  await act(async () => profileEntries()[0].click())
  assert.equal(document.querySelector('main h1').textContent, 'Hồ sơ tiêm chủng')
})

test('shared native date layout retains the calendar indicator', async () => {
  const css = await readFile(new URL('../theme.css', import.meta.url), 'utf8')
  assert.match(css, /input\[type="date"\]\s*\{[^}]*min-width:\s*0;[^}]*color-scheme:\s*light;/)
  assert.match(css, /input\[type="date"\]::-webkit-calendar-picker-indicator\s*\{[^}]*align-self:\s*center;/)
})
