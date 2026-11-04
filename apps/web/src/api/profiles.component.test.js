import test, { before, after } from 'node:test'
import assert from 'node:assert/strict'
import { fileURLToPath } from 'node:url'
import { Window } from 'happy-dom'
import { createServer } from 'vite'
import { createElement, act } from 'react'

let server, Profiles, api, createRoot, window
const owner = { id: 10, fullName: 'Hồ sơ kiểm thử', phoneNumber: '0900000000', email: 'rf03@example.invalid', dateOfBirth: '1990-01-01', salutation: 'Chị', identityNumber: null }
const child = { id: 11, customerId: 10, fullName: 'Người tiêm kiểm thử', dateOfBirth: '2020-01-01', relationshipToCustomer: 'Con', gender: 'Nam', identityNumber: null }

before(async () => {
  window = new Window()
  globalThis.window = window; globalThis.document = window.document; globalThis.IS_REACT_ACT_ENVIRONMENT = true
  globalThis.requestAnimationFrame = window.requestAnimationFrame.bind(window)
  createRoot = (await import('react-dom/client')).createRoot
  server = await createServer({ root: fileURLToPath(new URL('../..', import.meta.url)), server: { middlewareMode: true, hmr: false }, logLevel: 'silent' })
  Profiles = (await server.ssrLoadModule('/src/Profiles.jsx')).default
  api = await server.ssrLoadModule('/src/api/identity.js')
})
after(async () => { await server?.close(); await window?.happyDOM.close() })

async function render(t, handler, capabilities) {
  const client = api.createIdentityClient('http://localhost', async (url, options) => {
    const path = new URL(url).pathname
    if (path.endsWith('/auth/login')) return Response.json({ accessToken: 'synthetic', refreshToken: 'synthetic' })
    if (path.endsWith('/auth/me')) return Response.json({ accountType: 'customer', accountId: 1 })
    assert.equal(options.headers.Authorization, 'Bearer synthetic')
    assert.equal(options.cache, 'no-store')
    return handler(path.split('/api/v1/')[1], options.body ? JSON.parse(options.body) : undefined)
  })
  await client.login(owner.email, 'synthetic-password')
  Object.assign(api.identity, client)
  const host = document.createElement('div'); document.body.append(host)
  const root = createRoot(host)
  t.after(async () => { await act(async () => root.unmount()); host.remove() })
  await act(async () => root.render(createElement(Profiles, { capabilities })))
  const button = text => [...host.querySelectorAll('button')].find(b => b.textContent === text)
  const field = (name, tag = 'input') => [...host.querySelectorAll('dialog label')].find(l => l.textContent.startsWith(name))?.querySelector(tag)
  return {
    host, field, button,
    click: async text => { assert.ok(button(text), text); await act(async () => button(text).click()) },
    fill: async (name, value) => {
      const element = field(name); assert.ok(element, name)
      await act(async () => {
        Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set.call(element, value)
        element.dispatchEvent(new window.Event('input', { bubbles: true }))
      })
    },
    submit: () => act(async () => host.querySelector('dialog form').dispatchEvent(new window.Event('submit', { bubbles: true, cancelable: true }))),
  }
}

test('existing customer and recipient claims preserve the draft on network failure and expose only the linked profile', async t => {
  const calls = []
  let attempt = 0
  const ui = await render(t, (path, body) => {
    if (!body) return Response.json({ items: [] })
    calls.push({ path, body })
    if (path === 'recipients/claim') return Response.json(child)
    attempt++
    if (attempt === 1) throw new TypeError('offline')
    if (attempt === 2) return Response.json({ code: 'profileClaimUnavailable', errors: { email: ['PRIVATE CANDIDATE'] } }, { status: 409 })
    return Response.json(owner)
  }, { profilesCreate: true, selfClaim: true })

  assert.equal(ui.button('Tìm hồ sơ có sẵn'), undefined)
  await ui.click('Đã có hồ sơ')
  await ui.fill('Họ và tên', owner.fullName); await ui.fill('Ngày sinh', owner.dateOfBirth); await ui.fill('Số điện thoại', owner.phoneNumber)
  await ui.submit()
  assert.equal(ui.field('Họ và tên').value, owner.fullName)
  assert.equal(ui.field('Ngày sinh').value, owner.dateOfBirth)
  assert.equal(ui.field('Số điện thoại').value, owner.phoneNumber)
  assert.match(ui.host.querySelector('[role=alert]').textContent, /Vui lòng thử lại/)
  await ui.submit()
  assert.match(ui.host.querySelector('[role=alert]').textContent, /liên hệ nhân viên/)
  assert.doesNotMatch(ui.host.textContent, /PRIVATE CANDIDATE/)
  await ui.submit()
  assert.equal(ui.host.querySelector('dialog'), null)
  assert.match(ui.host.textContent, /Hồ sơ kiểm thử/)
  assert.equal(ui.button('Đã có hồ sơ'), undefined)
  assert.ok(ui.button('Chỉnh sửa hồ sơ'))
  await ui.click('Tìm hồ sơ có sẵn')
  await ui.fill('Họ và tên', child.fullName); await ui.fill('Ngày sinh', child.dateOfBirth)
  await ui.submit()
  assert.match(ui.host.textContent, /Người tiêm kiểm thử/)
  assert.deepEqual(calls.at(-1), { path: 'recipients/claim', body: { fullName: child.fullName, dateOfBirth: child.dateOfBirth } })
  assert.deepEqual(calls[0], { path: 'customers/claim', body: { fullName: owner.fullName, dateOfBirth: owner.dateOfBirth, phoneNumber: owner.phoneNumber } })
})

test('legacy fields are readable and the editors require the controlled choices', async t => {
  let writes = 0
  const ui = await render(t, (path, body) => {
    if (body) writes++
    return Response.json({ items: path === 'customers' ? [{ ...owner, salutation: null, dateOfBirth: null }] : [] })
  }, { profilesCreate: true, profilesUpdate: true })
  const details = [...ui.host.querySelectorAll('.profile-details div')].filter(node => ['Danh xưng', 'Ngày sinh'].includes(node.querySelector('dt').textContent))
  assert.deepEqual(details.map(node => node.querySelector('dd').textContent), ['Chưa cập nhật', 'Chưa cập nhật'])
  await ui.click('Chỉnh sửa hồ sơ')
  assert.equal(ui.field('Danh xưng', 'select').required, true)
  assert.equal(ui.field('Danh xưng', 'select').options.length, 6)
  assert.equal(ui.field('Ngày sinh').value, '')
  await ui.submit()
  assert.equal(writes, 0)
  await ui.click('Hủy')
  await ui.click('Thêm mới')
  assert.deepEqual([...ui.field('Quan hệ', 'select').options].slice(1).map(o => o.textContent),
    ['Bản thân', 'Con', 'Anh', 'Chị', 'Em', 'Cha', 'Mẹ', 'Vợ', 'Chồng', 'Ông', 'Bà', 'Cùng hộ khẩu'])
})
