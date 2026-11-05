import test, { before, after, beforeEach, afterEach } from 'node:test'
import assert from 'node:assert/strict'
import { fileURLToPath } from 'node:url'
import { Window } from 'happy-dom'
import { createServer } from 'vite'
import { createElement, act, useState } from 'react'

let server, Account, identity, createIdentityClient, createRoot, window, host, root, calls, handler
const email = 'rf01@example.invalid', password = 'synthetic-password'
const button = label => [...host.querySelectorAll('button')].find(b => b.textContent === label)
const input = label => [...host.querySelectorAll('label')].find(l => l.querySelector('span')?.textContent ?.replace(/ \*$/, '') === label)?.querySelector('input')
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
  globalThis.requestAnimationFrame = window.requestAnimationFrame.bind(window)
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
    calls.push({ path: url.split('/auth/')[1] || url.split('/api/v1/')[1], body: options.body && JSON.parse(options.body) })
    return handler(url, options)
  }))
  host = document.createElement('div'); document.body.append(host); root = createRoot(host)
  function TestAccount() {
    const [account, setAccount] = useState(null)
    return createElement(Account, { account, setAccount })
  }
  await act(async () => root.render(createElement(TestAccount)))
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


test('guest has equal entry choices and search preserves both drafts through tabs, network failure, loading and generic retry', async () => {
  const options = host.querySelectorAll('.account-entry-option')
  assert.equal(options.length, 2)
  assert.equal(options[0].getAttribute('aria-pressed'), 'true')
  await click(button('Tìm hồ sơ tiêm chủngTìm hồ sơ đã có tại trung tâm.'))
  assert.equal(options[1].getAttribute('aria-pressed'), 'true')
  await fill('Mã hồ sơ', 'VAC-ABCDEF123456'); await fill('Số điện thoại', '0900000000')
  await click(host.querySelector('input[value=personalInfo]'))
  await fill('Họ và tên', 'Nguyễn Test An'); await fill('Ngày sinh', '2000-01-01')
  await click(host.querySelector('input[value=profileCode]'))
  assert.equal(input('Mã hồ sơ').value, 'VAC-ABCDEF123456')
  handler = async () => { throw new TypeError('network') }
  await submit(); assert.match(host.textContent, /Không kết nối/)
  assert.equal(input('Số điện thoại').value, '0900000000')
  const pending = Promise.withResolvers(); handler = () => pending.promise
  await act(async () => {
    for (let i = 0; i < 2; i++) host.querySelector('form').dispatchEvent(new window.Event('submit', { bubbles: true, cancelable: true }))
  })
  assert.equal(calls.length, 2)
  assert.equal(button('Đang tìm hồ sơ…').disabled, true)
  assert.equal(options[0].disabled, true)
  await act(async () => pending.resolve(Response.json({ matched: false })))
  assert.match(host.textContent, /Không tìm thấy hồ sơ phù hợp/)
  assert.equal(button('Tiếp tục đăng nhập'), undefined)
  await click(host.querySelector('input[value=personalInfo]'))
  assert.equal(input('Họ và tên').value, 'Nguyễn Test An')
  assert.equal(input('Ngày sinh').value, '2000-01-01')
  handler = async () => Response.json({ matched: true })
  await submit()
  assert.deepEqual(calls.at(-1), { path: 'recipients/find', body: { mode: 'personalInfo', fullName: 'Nguyễn Test An', dateOfBirth: '2000-01-01', phoneNumber: '0900000000' } })
  await click(button('Tiếp tục đăng nhập'))
  assert.ok(input('Email'))
  await click(host.querySelectorAll('.account-entry-option')[1])
  assert.equal(input('Họ và tên').value, 'Nguyễn Test An')
  assert.ok(button('Tiếp tục đăng ký tài khoản'))
  await fill('Số điện thoại', '0900000001')
  assert.equal(button('Tiếp tục đăng ký tài khoản'), undefined)
  assert.doesNotMatch(host.textContent, /Có hồ sơ phù hợp/)
})

for (const register of [false, true]) test(`search continues through ${register ? 'registration and email verification' : 'login'} then revalidates authenticated claim`, async () => {
  const profile = { mode: 'profileCode', profileCode: 'VAC-ABCDEF123456', phoneNumber: '0900000000' }
  let claimAttempts = 0
  handler = async (url, options) => {
    const path = new URL(url).pathname.split('/api/v1/')[1]
    if (path === 'recipients/find') {
      assert.equal(options.headers.Authorization, undefined)
      assert.equal(options.credentials, 'omit'); assert.equal(options.cache, 'no-store')
      return Response.json({ matched: true })
    }
    if (path === 'auth/register') return Response.json({}, { status: 202 })
    if (path === 'auth/email-verifications/confirm') return new Response(null, { status: 204 })
    if (path === 'auth/login') return Response.json({ accessToken: 'synthetic', refreshToken: 'synthetic' })
    if (path === 'auth/me') return Response.json({ accountId: 1, accountType: 'customer', capabilities: { profilesCreate: true, selfClaim: true } })
    assert.equal(options.headers.Authorization, 'Bearer synthetic')
    if (path === 'recipients/claim') {
      assert.deepEqual(JSON.parse(options.body), profile)
      claimAttempts++
      if (claimAttempts === 1) return Response.json({ code: 'profileClaimUnavailable', errors: { phone: ['PRIVATE'] } }, { status: 409 })
      return Response.json({ customer: { id: 1, fullName: 'Nguyễn Test An' }, recipient: { id: 2, customerId: 1, fullName: 'Nguyễn Test Bình', profileCode: profile.profileCode } })
    }
    return Response.json({ items: [] })
  }
  await click(host.querySelectorAll('.account-entry-option')[1])
  await fill('Mã hồ sơ', profile.profileCode); await fill('Số điện thoại', profile.phoneNumber); await submit()
  assert.equal(host.querySelector('dialog'), null)
  assert.equal(calls.filter(x => x.path === 'login' || x.path === 'recipients/claim').length, 0)
  await click(button(register ? 'Tiếp tục đăng ký tài khoản' : 'Tiếp tục đăng nhập'))
  assert.match(host.textContent, /Nếu tải lại trang.*nhập lại thông tin/)
  await fill('Email', email); await fill('Mật khẩu', password)
  if (register) {
    await fill('Xác nhận mật khẩu', password); await submit()
    assert.equal(document.activeElement.textContent, 'Xác minh email')
    await fill('Mã xác minh email', 'synthetic-verification-token'); await submit()
    await fill('Mật khẩu', password)
  }
  await submit()
  assert.ok(host.querySelector('dialog[open]'))
  assert.equal(input('Mã hồ sơ').value, profile.profileCode)
  assert.equal(input('Số điện thoại').value, profile.phoneNumber)
  await submit()
  assert.match(host.textContent, /Không tìm thấy hồ sơ phù hợp/)
  assert.doesNotMatch(host.textContent, /PRIVATE/)
  assert.equal(input('Mã hồ sơ').value, profile.profileCode)
  await submit()
  assert.equal(host.querySelector('dialog'), null)
  assert.match(host.textContent, /Đã liên kết hồ sơ/)
  assert.match(host.textContent, /Nguyễn Test Bình/)
})

test('public search rate-limit response hides server detail and allows retry with the same input', async () => {
  await click(host.querySelectorAll('.account-entry-option')[1])
  await fill('Mã hồ sơ', 'VAC-ABCDEF123456'); await fill('Số điện thoại', '0900000000')
  handler = async () => Response.json({ code: 'rateLimited', errors: { phone: ['PRIVATE'] } }, { status: 429 })
  await submit(); assert.match(host.textContent, /quá nhiều lần/); assert.doesNotMatch(host.textContent, /PRIVATE/)
  assert.equal(input('Mã hồ sơ').value, 'VAC-ABCDEF123456')
  handler = async () => Response.json({ matched: false }); await submit()
  assert.match(host.textContent, /Không tìm thấy hồ sơ phù hợp/)
})


test('a fresh page discards the pending search and requires finding the profile again', async () => {
  handler = async () => Response.json({ matched: true })
  await click(host.querySelectorAll('.account-entry-option')[1])
  await fill('Mã hồ sơ', 'VAC-ABCDEF123456'); await fill('Số điện thoại', '0900000000'); await submit()
  await click(button('Tiếp tục đăng ký tài khoản'))
  assert.match(host.textContent, /Nếu tải lại trang.*nhập lại thông tin/)
  await act(async () => root.render(createElement(Account, { key: 'new-page', account: null, setAccount: () => {} })))
  await click(host.querySelectorAll('.account-entry-option')[1])
  assert.equal(input('Mã hồ sơ').value, '')
  assert.equal(input('Số điện thoại').value, '')
  assert.equal(button('Tiếp tục đăng nhập'), undefined)
})
