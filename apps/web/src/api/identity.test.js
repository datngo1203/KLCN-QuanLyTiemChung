import test from 'node:test'
import assert from 'node:assert/strict'
import { createIdentityClient } from './identity.js'

test('password validation, safe errors and successful change clear the client session', async () => {
  const calls = []
  let code = 'invalidCredentials'
  const client = createIdentityClient('http://localhost', async (url, options) => {
    const path = url.split('/').at(-1)
    calls.push(path)
    assert.equal(options.credentials, 'omit')
    if (path === 'me') return Response.json({ accountType: 'customer', accountId: 1 })
    if (path === 'change-password') {
      assert.equal(options.headers.Authorization, 'Bearer fresh')
      assert.deepEqual(JSON.parse(options.body), { currentPassword: 'current-password', newPassword: 'new-password-123' })
      if (code) return Response.json({ code, detail: 'sensitive backend text', errors: { secret: ['private'] } }, { status: 401 })
      return new Response(null, { status: 204 })
    }
    return Response.json({ accessToken: path === 'refresh' ? 'fresh' : 'access', refreshToken: 'refresh' })
  })
  await client.login('synthetic@example.invalid', 'current-password')
  await assert.rejects(client.changePassword('', 'new-password-123', 'new-password-123'), /hiện tại/)
  await assert.rejects(client.changePassword('current-password', 'short', 'short'), /12–128/)
  await assert.rejects(client.changePassword('current-password', 'current-password', 'current-password'), /khác/)
  await assert.rejects(client.changePassword('current-password', 'new-password-123', 'mismatch'), /không khớp/)
  assert.deepEqual(calls, ['login', 'me'])
  await assert.rejects(client.changePassword('current-password', 'new-password-123', 'new-password-123'), /Mật khẩu hiện tại không đúng/)
  code = 'validation'
  await assert.rejects(client.changePassword('current-password', 'new-password-123', 'new-password-123'), /12–128/)
  code = 'unknown'
  await assert.rejects(client.changePassword('current-password', 'new-password-123', 'new-password-123'), /HTTP 401/)
  code = null
  await client.changePassword('current-password', 'new-password-123', 'new-password-123')
  const count = calls.length
  await assert.rejects(client.profiles('customers'), /hết hạn/)
  await client.logout()
  assert.equal(calls.length, count)
})

test('late refresh cannot restore tokens after password change', async () => {
  let releaseRefresh
  let announceRefresh
  const started = new Promise(resolve => { announceRefresh = resolve })
  let refreshes = 0
  const client = createIdentityClient('http://localhost', async (url) => {
    const path = url.split('/').at(-1)
    if (path === 'me') return Response.json({ accountType: 'customer', accountId: 1 })
    if (path === 'login') return Response.json({ accessToken: 'access', refreshToken: 'refresh' })
    if (path === 'refresh') {
      refreshes++
      if (refreshes === 2) {
        announceRefresh()
        await new Promise(resolve => { releaseRefresh = resolve })
      }
      return Response.json({ accessToken: 'fresh', refreshToken: 'rotated' })
    }
    if (path === 'change-password') {
      await started
      return new Response(null, { status: 204 })
    }
    return Response.json({ code: 'invalidSession' }, { status: 401 })
  })
  await client.login('synthetic@example.invalid', 'current-password')
  const change = client.changePassword('current-password', 'new-password-123', 'new-password-123')
  // Let change-password enter the request before starting a separate, expired Profiles request.
  await new Promise(resolve => setTimeout(resolve, 0))
  const profile = assert.rejects(client.profiles('customers'), /hết hạn/)
  await change
  releaseRefresh()
  await profile
  await assert.rejects(client.profiles('customers'), /hết hạn/)
})

test('register/confirm/login/logout call Identity with tokens only in memory', async () => {
  const calls = []
  const request = async (url, options) => {
    const path = url.split('/').at(-1)
    calls.push(path)
    assert.equal(options.credentials, 'omit')
    const body = options.body && JSON.parse(options.body)
    if (['login', 'logout'].includes(path)) assert.equal(body.clientType, 'native')
    if (path === 'logout') assert.equal(body.refreshToken, 'refresh')
    if (path === 'me') assert.equal(options.headers.Authorization, 'Bearer access')
    if (['confirm', 'logout'].includes(path)) return new Response(null, { status: 204 })
    return Response.json(path === 'login' ? { accessToken: 'access', refreshToken: 'refresh' }
      : path === 'me' ? { accountType: 'customer', accountId: 1 } : { status: 'verificationPending' })
  }
  const client = createIdentityClient('http://localhost', request)
  await client.register('synthetic@example.invalid', 'input')
  await client.confirm('verification')
  assert.equal((await client.login('synthetic@example.invalid', 'input')).accountId, 1)
  await client.logout()
  await client.logout()
  await createIdentityClient('http://localhost', request).logout()
  assert.deepEqual(calls, ['register', 'confirm', 'login', 'me', 'logout'])
})

test('bad password rejected; failed logout retains refresh token for retry', async () => {
  let rejectLogin = true
  let failLogout = true
  let logoutCalls = 0
  const client = createIdentityClient('http://localhost', async (url, options) => {
    const path = url.split('/').at(-1)
    if (path === 'login' && rejectLogin) return Response.json({ code: 'invalidCredentials' }, { status: 401 })
    if (path === 'logout') {
      logoutCalls++
      assert.equal(JSON.parse(options.body).refreshToken, 'refresh')
      if (failLogout) throw new Error('offline')
      return new Response(null, { status: 204 })
    }
    return Response.json(path === 'me' ? { accountType: 'customer' } : { accessToken: 'access', refreshToken: 'refresh' })
  })
  await assert.rejects(client.login('synthetic@example.invalid', 'wrong'), /không đúng/)
  rejectLogin = false
  await client.login('synthetic@example.invalid', 'input')
  await assert.rejects(client.logout(), /offline/)
  failLogout = false
  await client.logout()
  assert.equal(logoutCalls, 2)
})

test('profiles share one refresh, retain bearer scope and surface API validation/denial', async () => {
  let refreshes = 0
  let forbidden = false
  let invalid = false
  const requests = []
  const client = createIdentityClient('http://localhost', async (url, options) => {
    const path = url.split('/').at(-1)
    if (path === 'login') return Response.json({ accessToken: 'expired', refreshToken: 'refresh' })
    if (path === 'me') return Response.json({ accountType: 'customer', accountId: 1 })
    if (path === 'refresh') {
      refreshes++
      assert.equal(JSON.parse(options.body).refreshToken, 'refresh')
      await new Promise(resolve => setTimeout(resolve, 10))
      return Response.json({ accessToken: 'fresh', refreshToken: 'rotated' })
    }
    assert.equal(options.credentials, 'omit')
    if (options.headers.Authorization === 'Bearer expired') return Response.json({ code: 'invalidSession' }, { status: 401 })
    assert.equal(options.headers.Authorization, 'Bearer fresh')
    if (forbidden) return Response.json({ code: 'permissionDenied' }, { status: 403 })
    if (invalid) return Response.json({ code: 'validation', errors: { fullName: ['Nhập họ tên.'] } }, { status: 400 })
    requests.push([path, options.method, options.body && JSON.parse(options.body)])
    return options.method === 'DELETE' ? new Response(null, { status: 204 }) : Response.json({ items: [] })
  })
  await assert.rejects(client.profiles('customers'), /hết hạn/)
  await client.login('synthetic@example.invalid', 'input')
  await Promise.all([client.profiles('customers'), client.profiles('recipients')])
  assert.equal(refreshes, 1)
  await client.profiles('customers', 'POST', { fullName: 'Synthetic', identityNumber: null })
  await client.profiles('recipients/7', 'PUT', { fullName: 'Child' })
  await client.profiles('recipients/7', 'DELETE')
  assert.deepEqual(requests.slice(2), [['customers', 'POST', { fullName: 'Synthetic', identityNumber: null }], ['7', 'PUT', { fullName: 'Child' }], ['7', 'DELETE', undefined]])
  forbidden = true
  await assert.rejects(client.profiles('customers/99', 'PUT', {}), /không có quyền/)
  assert.equal(refreshes, 1)
  forbidden = false; invalid = true
  await assert.rejects(client.profiles('customers', 'POST', {}), /Nhập họ tên/)
})

test('logout still revokes the session when an in-flight refresh fails', async () => {
  const started = Promise.withResolvers()
  const release = Promise.withResolvers()
  let logoutCalls = 0
  const client = createIdentityClient('http://localhost', async (url, options) => {
    const path = url.split('/').at(-1)
    if (path === 'login') return Response.json({ accessToken: 'access', refreshToken: 'refresh' })
    if (path === 'me') return Response.json({ accountType: 'customer', accountId: 1 })
    if (path === 'refresh') {
      started.resolve()
      await release.promise
      return Response.json({ code: 'invalidSession' }, { status: 401 })
    }
    if (path === 'logout') {
      logoutCalls++
      assert.equal(JSON.parse(options.body).refreshToken, 'refresh')
      return new Response(null, { status: 204 })
    }
    return Response.json({ code: 'invalidSession' }, { status: 401 })
  })
  await client.login('a@example.invalid', 'input')
  const profile = assert.rejects(client.profiles('customers'), /hết hạn/)
  await started.promise
  const logout = client.logout()
  release.resolve()
  await profile
  await logout
  assert.equal(logoutCalls, 1)
  await assert.rejects(client.profiles('customers'), /hết hạn/)
})

test('late password-change response must not clear a newer account session', async () => {
  const started = Promise.withResolvers()
  const release = Promise.withResolvers()
  const client = createIdentityClient('http://localhost', async (url, options) => {
    const path = url.split('/').at(-1)
    if (path === 'login') {
      const { identifier } = JSON.parse(options.body)
      return Response.json({ accessToken: identifier, refreshToken: identifier })
    }
    if (path === 'me') return Response.json({ accountType: 'customer', accountId: 1 })
    if (path === 'refresh') return Response.json({ accessToken: 'a-renewed', refreshToken: 'a-rotated' })
    if (path === 'change-password') {
      assert.equal(options.headers.Authorization, 'Bearer a-renewed')
      started.resolve()
      await release.promise
      return new Response(null, { status: 204 })
    }
    if (path === 'logout') return new Response(null, { status: 204 })
    assert.equal(options.headers.Authorization, 'Bearer b@example.invalid')
    return Response.json({ items: [] })
  })
  await client.login('a@example.invalid', 'current-password')
  const changed = assert.rejects(client.changePassword('current-password', 'new-password-123', 'new-password-123'), /hết hạn/)
  await started.promise
  await client.logout()
  await client.login('b@example.invalid', 'input')
  release.resolve()
  await changed
  assert.deepEqual(await client.profiles('customers'), { items: [] })
})

test('late logout response must not clear a newer account session', async () => {
  const started = Promise.withResolvers()
  const release = Promise.withResolvers()
  const client = createIdentityClient('http://localhost', async (url, options) => {
    const path = url.split('/').at(-1)
    if (path === 'login') {
      const { identifier } = JSON.parse(options.body)
      return Response.json({ accessToken: identifier, refreshToken: identifier })
    }
    if (path === 'me') return Response.json({ accountType: 'customer', accountId: 1 })
    if (path === 'logout') {
      assert.equal(JSON.parse(options.body).refreshToken, 'a@example.invalid')
      started.resolve()
      await release.promise
      return new Response(null, { status: 204 })
    }
    assert.equal(options.headers.Authorization, 'Bearer b@example.invalid')
    return Response.json({ items: [] })
  })
  await client.login('a@example.invalid', 'input')
  const logout = assert.rejects(client.logout(), /hết hạn/)
  await started.promise
  await client.login('b@example.invalid', 'input')
  release.resolve()
  await logout
  assert.deepEqual(await client.profiles('customers'), { items: [] })
})

test('late login must revoke its obsolete session instead of replacing a newer account', async () => {
  const started = Promise.withResolvers()
  const release = Promise.withResolvers()
  const revoked = []
  const client = createIdentityClient('http://localhost', async (url, options) => {
    const path = url.split('/').at(-1)
    if (path === 'login') {
      const { identifier } = JSON.parse(options.body)
      return Response.json({ accessToken: identifier, refreshToken: identifier })
    }
    if (path === 'me') {
      if (options.headers.Authorization === 'Bearer a@example.invalid') {
        started.resolve()
        await release.promise
      }
      return Response.json({ accountType: 'customer', accountId: options.headers.Authorization === 'Bearer b@example.invalid' ? 2 : 1 })
    }
    if (path === 'logout') {
      revoked.push(JSON.parse(options.body).refreshToken)
      return new Response(null, { status: 204 })
    }
    assert.equal(options.headers.Authorization, 'Bearer b@example.invalid')
    return Response.json({ items: [] })
  })
  const old = assert.rejects(client.login('a@example.invalid', 'input'), /hết hạn/)
  await started.promise
  assert.equal((await client.login('b@example.invalid', 'input')).accountId, 2)
  release.resolve()
  await old
  assert.deepEqual(revoked, ['a@example.invalid'])
  assert.deepEqual(await client.profiles('customers'), { items: [] })
})
