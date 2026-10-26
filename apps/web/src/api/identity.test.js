import test from 'node:test'
import assert from 'node:assert/strict'
import { createIdentityClient } from './identity.js'

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
