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
  globalThis.fetch = async () => Response.json({ items: [] })
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
  // Start login from the profile route without a return-after-login destination.
  await act(async () => header.querySelector('.brand').click())
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
