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
