import test from 'node:test'
import assert from 'node:assert/strict'
import { createIdentityClient } from './identity.js'
import { appointmentTime, scheduleError } from '../scheduleView.js'

test('appointments GET shares Identity bearer refresh, no-store and omit transport', async () => {
  const calls = []
  const client = createIdentityClient('http://localhost', async (url, options) => {
    if (url.endsWith('/login')) return Response.json({ accessToken: 'old', refreshToken: 'refresh' })
    if (url.endsWith('/me')) return Response.json({ accountType: 'customer', accountId: 1 })
    if (url.endsWith('/refresh')) return Response.json({ accessToken: 'new', refreshToken: 'rotated' })
    calls.push({ url, options })
    if (options.headers.Authorization === 'Bearer old') return Response.json({ code: 'invalidSession' }, { status: 401 })
    return Response.json(url.includes('?') ? { items: [] } : { id: 7, serviceName: 'Dịch vụ API', status: 'DaXepLich' })
  })
  await assert.rejects(client.appointments(), e => e.status === 401)
  await client.login('synthetic@example.invalid', 'synthetic-password')
  assert.deepEqual(await client.appointments('?page=2&pageSize=25'), { items: [] })
  assert.deepEqual(await client.getAppointment(7), { id: 7, serviceName: 'Dịch vụ API', status: 'DaXepLich' })
  assert.equal(calls.length, 3)
  assert.ok(calls[0].url.endsWith('/appointments?page=2&pageSize=25'))
  assert.ok(calls[2].url.endsWith('/appointments/7'))
  for (const { options } of calls) {
    assert.equal(options.method, 'GET'); assert.equal(options.body, undefined)
    assert.equal(options.credentials, 'omit'); assert.equal(options.cache, 'no-store')
  }
  assert.equal(calls[2].options.headers.Authorization, 'Bearer new')
})

test('times use the API instant; safe errors distinguish 401/403 and network failures', () => {
  assert.equal(appointmentTime('2026-11-01T02:00:00Z'), appointmentTime('2026-11-01T09:00:00+07:00'))
  assert.match(scheduleError({ status: 401 }), /đăng nhập lại/)
  assert.match(scheduleError({ status: 403 }), /không có quyền/)
  for (const error of [new TypeError('PRIVATE'), { status: 503, message: 'SQL PRIVATE' }, { status: 400, message: 'PRIVATE PII' }]) {
    assert.match(scheduleError(error), /kiểm tra kết nối/)
    assert.doesNotMatch(scheduleError(error), /PRIVATE|SQL/)
  }
})

for (const method of ['appointments', 'getAppointment']) {
  for (const duringBody of [false, true]) {
    for (const change of ['logout', 'login', 'password']) {
      test(`late ${method} ignored after ${change}, during ${duringBody ? 'body' : 'headers'}`, async () => {
        const started = Promise.withResolvers(), release = Promise.withResolvers()
        let hold = true
        const client = createIdentityClient('http://localhost', async (url, options) => {
          if (url.endsWith('/login')) {
            const { identifier } = JSON.parse(options.body)
            return Response.json({ accessToken: identifier, refreshToken: identifier })
          }
          if (url.endsWith('/me')) return Response.json({ accountType: 'customer' })
          if (url.endsWith('/refresh')) return Response.json({ accessToken: 'fresh', refreshToken: 'rotated' })
          if (url.endsWith('/logout') || url.endsWith('/change-password')) return new Response(null, { status: 204 })
          if (!hold) return Response.json({ items: [] })
          const read = async () => { started.resolve(); await release.promise; return '{"items":[{"id":1}]}' }
          if (duringBody) return { ok: true, status: 200, json: async () => JSON.parse(await read()) }
          await read(); return Response.json({ items: [{ id: 1 }] })
        })
        await client.login('a@example.invalid', 'synthetic-password')
        const old = assert.rejects(client[method](method === 'getAppointment' ? 1 : ''), /hết hạn/)
        await started.promise
        if (change === 'logout') await client.logout()
        if (change === 'password') await client.changePassword('synthetic-password', 'new-password-123', 'new-password-123')
        await client.login('b@example.invalid', 'synthetic-password')
        hold = false; release.resolve(); await old
        assert.deepEqual(await client.appointments(), { items: [] })
      })
    }
  }
}
