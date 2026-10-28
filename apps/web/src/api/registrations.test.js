import test from 'node:test'
import assert from 'node:assert/strict'
import { createIdentityClient } from './identity.js'
import { allProfiles, catalogServices, money, total, parseRegistrationJson, registrationBody, registrationPayload, refreshDraft, maxMoney } from '../registrationForm.js'

const customers = [{ id: 12 }, { id: 13 }]
const recipients = [{ id: 34, customerId: 12 }, { id: 35, customerId: 13 }]
const vaccine = { kind: 'vaccine', serviceId: 5, name: 'Vaccine', quantity: 2, price: '150000' }
const pack = { kind: 'package', serviceId: 5, name: 'Package', quantity: 1, price: '350000', components: [{ vaccineId: 5, quantity: 2, vaccineName: 'Vaccine' }] }
const draft = items => ({ customerId: '12', recipientId: '34', items })

test('vaccine/package/mixed payloads carry only contract fields, package price and components', () => {
  for (const items of [[vaccine], [pack], [vaccine, pack]]) {
    const payload = registrationPayload({ ...draft(items), status: 'fake', employeeId: 100 }, customers, recipients)
    assert.deepEqual(Object.keys(payload), ['customerId', 'recipientId', 'items'])
    assert.deepEqual(Object.keys(payload.items[0]), items[0].kind === 'vaccine'
      ? ['kind', 'serviceId', 'quantity', 'expectedUnitPrice'] : ['kind', 'serviceId', 'quantity', 'expectedUnitPrice', 'expectedComponents'])
    assert.equal(payload.customerId, 12)
    assert.equal(payload.recipientId, 34)
    assert.equal(typeof JSON.parse(registrationBody(payload)).items[0].expectedUnitPrice, 'number')
  }
  const payload = registrationPayload(draft([vaccine, pack]), customers, recipients)
  assert.deepEqual(payload.items[1].expectedComponents, [{ vaccineId: 5, quantity: 2 }])
  assert.equal(total([vaccine, pack]), 650000n)
})

test('rejects mismatched recipients, duplicate services, invalid quantities/components and money overflow', () => {
  assert.throws(() => registrationPayload({ ...draft([vaccine]), recipientId: '35' }, customers, recipients), /thuộc/)
  assert.throws(() => registrationPayload({ ...draft([vaccine]), customerId: '99' }, customers, recipients), /thuộc/)
  for (const items of [[], Array(101).fill(vaccine), [vaccine, vaccine], [{ ...vaccine, kind: 'other' }], [{ ...vaccine, serviceId: 'not-sql' }],
    ...[0, 101, -1, 1.5, '', 'NaN'].map(quantity => [{ ...vaccine, quantity }]), [{ ...vaccine, price: '-1' }], [{ ...vaccine, unavailable: true }],
    [{ ...vaccine, price: maxMoney.toString(), quantity: 2 }], [{ ...pack, components: [] }], [{ ...pack, components: [pack.components[0], pack.components[0]] }], [{ ...pack, components: [{ vaccineId: 5, quantity: 0 }] }]])
    assert.throws(() => registrationPayload(draft(items), customers, recipients))
  assert.equal(registrationPayload(draft([{ ...vaccine, price: '0' }]), customers, recipients).items[0].expectedUnitPrice, '0')
})

test('VND parse/display/calculation/serialization stays exact at SQL decimal limits', () => {
  const parsed = parseRegistrationJson('{"price":9999999999999999.00,"unitPrice":0.00,"lineTotal":9007199254740993,"subtotal":9999999999999999,"name":"price: 9007199254740993"}')
  assert.equal(parsed.price, '9999999999999999')
  assert.equal(parsed.unitPrice, '0')
  assert.equal(parsed.lineTotal, '9007199254740993')
  assert.equal(money(parsed.price), '9.999.999.999.999.999 đ')
  assert.throws(() => parseRegistrationJson('{"price":1.5}'), /không hợp lệ/)
  const payload = registrationPayload(draft([{ ...vaccine, price: parsed.price, quantity: 1 }]), customers, recipients)
  assert.match(registrationBody(payload), /"expectedUnitPrice":9999999999999999/)
})

test('catalog changes retain quantities, report exact changes/removal, ignore component order', () => {
  const updated = refreshDraft([vaccine, pack], [{ ...vaccine, price: '160000' }, { ...pack, components: [{ vaccineId: 6, quantity: 1 }] }])
  assert.equal(updated.items[0].quantity, 2)
  assert.equal(updated.items[0].price, '160000')
  assert.equal(updated.changes.length, 2)
  assert.match(updated.changes[1], /thành phần/)
  assert.equal(refreshDraft([vaccine], []).items[0].unavailable, true)
  const parts = [{ vaccineId: 1, quantity: 1 }, { vaccineId: 2, quantity: 2 }]
  assert.equal(refreshDraft([{ ...pack, components: parts }], [{ ...pack, components: [...parts].reverse() }]).changes.length, 0)
  assert.throws(() => catalogServices({ source: 'demo', items: [] }, { source: 'database', items: [] }), /mẫu/)
})

test('profiles load all pages including full last page; recipients retain owner IDs', async () => {
  const calls = []
  const client = { profiles: async path => {
    calls.push(path)
    return { items: path.includes('page=1&') ? Array.from({ length: 100 }, (_, id) => ({ id, customerId: 12 })) : [] }
  } }
  assert.equal((await allProfiles(client, 'recipients')).length, 100)
  assert.deepEqual(calls, ['recipients?page=1&pageSize=100', 'recipients?page=2&pageSize=100'])
})

test('registrations use in-memory bearer refresh, strict 201, safe structured 401/403/409 errors', async () => {
  let mode = 201
  let refreshes = 0
  const calls = []
  const client = createIdentityClient('http://localhost', async (url, options) => {
    assert.equal(options.credentials, 'omit')
    const path = url.split('/').at(-1)
    if (path === 'login') return Response.json({ accessToken: 'expired', refreshToken: 'refresh' })
    if (path === 'me') return Response.json({ accountType: 'customer' })
    if (path === 'refresh') { refreshes++; return Response.json({ accessToken: 'fresh', refreshToken: 'rotated' }) }
    if (options.headers.Authorization === 'Bearer expired') return Response.json({ code: 'invalidSession' }, { status: 401 })
    assert.equal(options.headers.Authorization, 'Bearer fresh')
    calls.push({ url, method: options.method, body: options.body })
    if (mode === 'network') throw new TypeError('offline')
    if (mode === 'timeout') throw new DOMException('timeout', 'TimeoutError')
    if (mode >= 400) return Response.json({ code: mode === 409 ? 'catalogChanged' : mode === 401 ? 'invalidSession' : 'permissionDenied', detail: 'PRIVATE SQL' }, { status: mode })
    return new Response('{"id":7,"subtotal":650000.00}', { status: mode })
  })
  await assert.rejects(client.registrations(), error => error.status === 401)
  await client.login('synthetic@example.invalid', 'synthetic-password')
  const payload = registrationPayload(draft([vaccine, pack]), customers, recipients)
  assert.deepEqual(await client.registrations('', 'POST', payload), { id: 7, subtotal: '650000' })
  assert.equal(refreshes, 1)
  await client.registrations('?page=2&pageSize=25')
  assert.match(calls.at(-1).url, /registrations\?page=2&pageSize=25$/)
  for (const status of [401, 403, 409]) {
    mode = status
    await assert.rejects(client.registrations('', 'POST', payload), error => error.status === status && !error.message.includes('PRIVATE'))
  }
  mode = 200
  await assert.rejects(client.registrations('', 'POST', payload), error => error.uncertain)
  for (const failure of ['network', 'timeout']) {
    mode = failure
    const before = calls.length
    await assert.rejects(client.registrations('', 'POST', payload))
    assert.equal(calls.length - before, 1, 'transport failure must never retry POST')
  }
})
