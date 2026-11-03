import test, { before, after, beforeEach, afterEach } from 'node:test'
import assert from 'node:assert/strict'
import { fileURLToPath } from 'node:url'
import { Window } from 'happy-dom'
import { createServer } from 'vite'
import { createElement, act } from 'react'

let server, Account, identity, createIdentityClient, createRoot, window, host, root, calls, handler
const email = 'rf01@example.invalid', password = 'synthetic-password'
const button = label => [...host.querySelectorAll('button')].find(b => b.textContent === label)
const input = label => [...host.querySelectorAll('label')].find(l => l.querySelector('span')?.textContent === label)?.querySelector('input')
const click = async element => { assert.ok(element); await act(async () => element.click()) }
const fill = async (label, value) => {
  const element = input(label); assert.ok(element)
  await act(async () => {
    Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set.call(element, value)
    element.dispatchEvent(new window.Event('input', { bubbles: true }))
  })
}
const submit = async () => { await act(async () => host.querySelector('form').dispatchEvent(new window.Event('submit', { bubbles: true, cancelable: true }))) }

before(async () => {
  window = new Window(); globalThis.window = window; globalThis.document = window.document
  globalThis.IS_REACT_ACT_ENVIRONMENT = true
  createRoot = (await import('react-dom/client')).createRoot
  server = await createServer({ root: fileURLToPath(new URL('../..', import.meta.url)), server: { middlewareMode: true, hmr: false }, logLevel: 'error' })
  Account = (await server.ssrLoadModule('/src/Account.jsx')).default
  const api = await server.ssrLoadModule('/src/api/identity.js')
  identity = api.identity; createIdentityClient = api.createIdentityClient
})
after(async () => { await server?.close(); await window?.happyDOM.close() })
beforeEach(async () => {
  calls = []; handler = async () => Response.json({ status: 'pending' }, { status: 202 })
  Object.assign(identity, createIdentityClient('http://localhost', async (url, options) => {
    calls.push({ path: url.split('/auth/')[1], body: options.body && JSON.parse(options.body) })
    return handler(url, options)
  }))
  host = document.createElement('div'); document.body.append(host); root = createRoot(host)
  await act(async () => root.render(createElement(Account, { account: null, setAccount: () => {} })))
})
afterEach(async () => {
  await act(async () => root.unmount()); host.remove()
  assert.equal(window.localStorage.length, 0); assert.equal(window.sessionStorage.length, 0)
})

test('register blocks mismatched confirmation, toggles visibility without losing values, and submits the existing contract', async () => {
  await click(button('Tạo tài khoản khách hàng'))
  await fill('Email', email); await fill('Mật khẩu', password); await fill('Xác nhận mật khẩu', 'different-password')
  await submit(); assert.equal(calls.length, 0); assert.match(host.textContent, /không khớp/)
  const toggle = host.querySelector('input[type=checkbox]')
  await click(toggle)
  assert.equal(input('Mật khẩu').type, 'text'); assert.equal(input('Mật khẩu').value, password)
  assert.equal(input('Xác nhận mật khẩu').type, 'text')
  await click(toggle); assert.equal(input('Mật khẩu').type, 'password')
  await fill('Xác nhận mật khẩu', password); await submit()
  assert.deepEqual(calls, [{ path: 'register', body: { email, password } }])
  assert.equal(document.activeElement.textContent, 'Xác minh email')
  assert.equal(host.querySelector('input[type=password]'), null)
})

test('forgot request uses generic feedback, blocks duplicate submits and supports reset confirmation', async () => {
  await click(button('Quên mật khẩu')); await fill('Email', email)
  const pending = Promise.withResolvers(); handler = () => pending.promise
  await act(async () => {
    const form = host.querySelector('form')
    for (let i = 0; i < 2; i++) form.dispatchEvent(new window.Event('submit', { bubbles: true, cancelable: true }))
  })
  assert.equal(calls.length, 1)
  assert.equal(host.querySelector('fieldset').disabled, true)
  await act(async () => pending.resolve(Response.json({ status: 'passwordResetPending' }, { status: 202 })))
  assert.match(host.textContent, /Nếu email thuộc tài khoản/)
  assert.equal(document.activeElement.textContent, 'Đặt lại mật khẩu')
  await fill('Token đặt lại mật khẩu', 'reset-token'); await fill('Mật khẩu mới', password)
  await fill('Xác nhận mật khẩu mới', 'mismatch-password'); await submit(); assert.equal(calls.length, 1)
  await fill('Xác nhận mật khẩu mới', password)
  handler = async () => new Response(null, { status: 204 })
  await submit()
  assert.deepEqual(calls[1], { path: 'password-resets/confirm', body: { token: 'reset-token', newPassword: password } })
  assert.equal(document.activeElement.textContent, 'Đăng nhập')
  assert.match(host.textContent, /Đã đặt lại mật khẩu và thu hồi các phiên/)
  assert.equal(input('Mật khẩu').value, '')
})

test('request network failure keeps email for retry and reset failure keeps the complete draft', async () => {
  await click(button('Quên mật khẩu')); await fill('Email', email)
  handler = async () => { throw new TypeError('network') }
  await submit(); assert.equal(input('Email').value, email); assert.match(host.textContent, /Không kết nối/)
  handler = async () => Response.json({ status: 'passwordResetPending' }, { status: 202 })
  await submit(); await fill('Token đặt lại mật khẩu', 'expired-token')
  await fill('Mật khẩu mới', password); await fill('Xác nhận mật khẩu mới', password)
  handler = async () => Response.json({ code: 'invalidPasswordResetToken', detail: 'PRIVATE' }, { status: 400 })
  await submit()
  assert.equal(input('Token đặt lại mật khẩu').value, 'expired-token')
  assert.equal(input('Mật khẩu mới').value, password); assert.equal(input('Xác nhận mật khẩu mới').value, password)
  assert.match(host.textContent, /đã hết hạn/); assert.doesNotMatch(host.textContent, /PRIVATE/)
  await click(button('Yêu cầu token mới')); assert.equal(input('Email').value, email)
  await click(button('Quay lại đăng nhập')); assert.equal(input('Mật khẩu').value, '')
})
