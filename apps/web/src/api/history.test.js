import test from 'node:test'
import assert from 'node:assert/strict'
import { createIdentityClient } from './identity.js'
import { vaccinationCode, vaccinationTime, historyError } from '../historyView.js'

test('history is an authenticated read with refresh, no-store, omit and optional narrowing query', async () => {
  const calls = []
  const client = createIdentityClient('http://localhost', async (url, options) => {
    if (url.endsWith('/login')) return Response.json({ accessToken: 'old', refreshToken: 'refresh' })
    if (url.endsWith('/me')) return Response.json({ accountType: 'customer', accountId: 1 })
    if (url.endsWith('/refresh')) return Response.json({ accessToken: 'new', refreshToken: 'rotated' })
    calls.push({ url, options })
    if (options.headers.Authorization === 'Bearer old') return Response.json({ code: 'invalidSession' }, { status: 401 })
    return Response.json({ items: [{ id: 3, recipient: { id: 2, fullName: 'API recipient' } }] })
  })
  await assert.rejects(client.vaccinationHistory(), e => e.status === 401)
  await client.login('synthetic@example.invalid', 'synthetic-password')
  assert.equal((await client.vaccinationHistory('?recipientId=2&page=2&pageSize=25')).items[0].id, 3)
  assert.equal(calls.length, 2)
  for (const { url, options } of calls) {
    assert.equal(url, 'http://localhost/api/v1/vaccinations/history?recipientId=2&page=2&pageSize=25')
    assert.equal(options.method, 'GET'); assert.equal(options.body, undefined)
    assert.equal(options.credentials, 'omit'); assert.equal(options.cache, 'no-store')
  }
  assert.equal(calls[1].options.headers.Authorization, 'Bearer new')
})

test('history time preserves the instant and errors do not render backend private contents', () => {
  assert.equal(vaccinationCode(12), 'MT-0012')
  assert.equal(vaccinationTime('2026-11-01T02:00:00Z'), vaccinationTime('2026-11-01T09:00:00+07:00'))
  assert.equal(vaccinationTime('2026-11-01T02:00:00Z'), new Date('2026-11-01T02:00:00Z').toLocaleString('vi-VN'))
  assert.match(historyError({ status: 401 }), /đăng nhập lại/)
  assert.match(historyError({ status: 403 }), /không có quyền/)
  for (const status of [400, 503, undefined]) {
    assert.match(historyError({ status, message: 'SQL PRIVATE' }), /kiểm tra kết nối/)
    assert.doesNotMatch(historyError({ status, message: 'SQL PRIVATE' }), /SQL|PRIVATE/)
  }
})

for (const status of [401, 403, 503]) {
  test(`history HTTP ${status} rejects without inventing sample results`, async () => {
    const client = createIdentityClient('http://localhost', async url => {
      if (url.endsWith('/login')) return Response.json({ accessToken: 'access', refreshToken: 'refresh' })
      if (url.endsWith('/me')) return Response.json({ accountType: 'customer' })
      return Response.json({ code: status === 401 ? 'invalidSession' : status === 403 ? 'permissionDenied' : 'dependencyUnavailable' }, { status })
    })
    await client.login('a@example.invalid', 'synthetic-password')
    await assert.rejects(client.vaccinationHistory(), e => e.status === status)
  })
}

for (const duringBody of [false, true]) {
  for (const change of ['logout', 'login', 'password']) {
    test(`late history cannot restore previous account after ${change}, during ${duringBody ? 'body' : 'headers'}`, async () => {
      const started = Promise.withResolvers(), release = Promise.withResolvers()
      let hold = true
      const client = createIdentityClient('http://localhost', async (url, options) => {
        if (url.endsWith('/login')) return Response.json({ accessToken: JSON.parse(options.body).identifier, refreshToken: 'refresh' })
        if (url.endsWith('/me')) return Response.json({ accountType: 'customer' })
        if (url.endsWith('/refresh')) return Response.json({ accessToken: 'fresh', refreshToken: 'rotated' })
        if (url.endsWith('/logout') || url.endsWith('/change-password')) return new Response(null, { status: 204 })
        if (!hold) return Response.json({ items: [] })
        const read = async () => { started.resolve(); await release.promise; return { items: [{ id: 1 }] } }
        if (duringBody) return { ok: true, status: 200, json: read }
        return Response.json(await read())
      })
      await client.login('a@example.invalid', 'synthetic-password')
      const old = assert.rejects(client.vaccinationHistory(), /hết hạn/)
      await started.promise
      if (change === 'logout') await client.logout()
      if (change === 'password') await client.changePassword('synthetic-password', 'new-password-123', 'new-password-123')
      await client.login('b@example.invalid', 'synthetic-password')
      hold = false; release.resolve(); await old
      assert.deepEqual(await client.vaccinationHistory(), { items: [] })
    })
  }
}
