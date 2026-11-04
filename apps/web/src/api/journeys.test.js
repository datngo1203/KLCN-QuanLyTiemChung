import test from 'node:test'
import assert from 'node:assert/strict'
import { journeyState } from '../journeyView.js'
import { createIdentityClient } from './identity.js'

test('payment gates the next action before appointment or vaccination facts', () => {
  const appointment = { id: 4 }
  const journey = { appointments: [appointment], nextAppointment: appointment, shots: [{ id: 9 }] }
  const state = (payment, facts = journey) => journeyState({ ...facts, payment: { totalDue: '9007199254740993', ...payment } })
  assert.equal(state({ paymentState: 'ChoXacNhan', expectedChannel: 'TaiTrungTam' }).action, 'Chờ xác nhận thanh toán')
  assert.equal(state({}).action, 'Chờ thanh toán')
  assert.equal(state({ paymentState: 'ThanhCong' }).action, 'Đã xác nhận lịch')
  assert.equal(state({ paymentState: 'ThanhCong' }).step, 3)
  assert.equal(state({ totalDue: '0' }).action, 'Đã xác nhận lịch')
  const empty = { appointments: [], nextAppointment: null, shots: [] }
  const center = state({ expectedChannel: 'TaiTrungTam' }, empty)
  assert.equal(center.action, 'Chờ trung tâm xác nhận lịch')
  assert.equal(center.paymentLabel, 'Thanh toán tại trung tâm')
  assert.equal(state({ paymentState: 'ThanhCong' }, { ...empty, appointments: [appointment] }).action,
    'Lịch hẹn đã qua, chưa có mũi tiêm được ghi nhận')
  assert.equal(state({ paymentState: 'ThanhCong' }, { ...journey, nextAppointment: null }).action, 'Tiêm & theo dõi')
})

test('journey uses exact payment facts and reconciles an uncertain write through GET', async () => {
  let writes = 0
  let paid = false
  const client = createIdentityClient('http://localhost', async (url, options) => {
    const { pathname } = new URL(url)
    if (pathname.endsWith('/login')) return Response.json({ accessToken: 'access', refreshToken: 'refresh' })
    if (pathname.endsWith('/me')) return Response.json({ accountType: 'customer', accountId: 1 })
    if (pathname.endsWith('/online')) {
      writes++
      throw new Error('Connection dropped')
    }
    assert.equal(options.method, 'GET')
    assert.equal(options.cache, 'no-store')
    assert.equal(options.credentials, 'omit')
    assert.equal(options.headers.Authorization, 'Bearer access')
    const journey = '{"registrationId":12,"payment":{"totalDue":9007199254740993.00,"payment":'
      + (paid ? '{"amount":9007199254740993.00}' : 'null') + '}}'
    if (pathname.endsWith('/12')) return new Response(journey)
    assert.equal(pathname, '/api/v1/vaccination-journeys')
    return new Response('{"items":[' + journey + ']}')
  })
  await client.login('journey@example.invalid', 'synthetic-password')
  const list = await client.vaccinationJourneys()
  assert.equal(list.items[0].payment.totalDue, '9007199254740993')
  await assert.rejects(client.recordOnlinePayment(12, {}))
  await assert.rejects(client.recordOnlinePayment(12, {}))
  assert.equal(writes, 1)
  await client.getVaccinationJourney(12)
  assert.equal(client.paymentWriteState(12), 'unconfirmed')
  paid = true
  const detail = await client.getVaccinationJourney(12)
  assert.equal(detail.payment.payment.amount, '9007199254740993')
  assert.equal(client.paymentWriteState(12), undefined)
  assert.equal(writes, 1)
})

test('a journey response from before logout cannot restore customer data', async () => {
  const pending = Promise.withResolvers()
  const requested = Promise.withResolvers()
  const client = createIdentityClient('http://localhost', async url => {
    const path = new URL(url).pathname
    if (path.endsWith('/login')) return Response.json({ accessToken: 'access', refreshToken: 'refresh' })
    if (path.endsWith('/me')) return Response.json({ accountType: 'customer', accountId: 1 })
    if (path.endsWith('/logout')) return new Response(null, { status: 204 })
    requested.resolve()
    return pending.promise
  })
  await client.login('journey@example.invalid', 'synthetic-password')
  const response = client.getVaccinationJourney(12)
  await requested.promise
  await client.logout()
  pending.resolve(new Response('{"payment":{"totalDue":0}}'))
  await assert.rejects(response, /Phiên đã hết hạn/)
  await assert.rejects(client.vaccinationJourneys(), error => error.status === 401)
})
