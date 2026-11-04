import test from 'node:test'
import assert from 'node:assert/strict'
import { createIdentityClient } from './identity.js'
import { parseRegistrationJson, money } from '../registrationForm.js'
import { cardNumberValid, paymentError, paymentMethodLabel, paymentOutcome } from '../paymentView.js'

test('all G3 monetary decimal tokens remain exact, including transaction amount', () => {
  for (const raw of ['9999999999999999.00', '9007199254740993', '250000.00', '0.00']) {
    const exact = raw.split('.')[0]
    const detail = parseRegistrationJson(`{"registrationId":7,"totalDue":${raw},"paidAmount":${raw},"remainingAmount":0.00,"payment":{"id":3,"amount":${raw}}}`)
    assert.equal(detail.totalDue, exact)
    assert.equal(detail.paidAmount, exact)
    assert.equal(detail.payment.amount, exact)
    assert.equal(detail.remainingAmount, '0')
    assert.equal(detail.registrationId, 7)
    assert.equal(detail.payment.id, 3)
    assert.equal(money(detail.payment.amount), `${BigInt(exact).toLocaleString('vi-VN')} đ`)
  }
})

test('payment outcomes distinguish no charge, unpaid, pending and paid', () => {
  assert.equal(paymentOutcome({ totalDue: '0', payment: null }), 'Không phát sinh khoản thu')
  assert.equal(paymentOutcome({ totalDue: '250000', payment: null }), 'Chưa thanh toán')
  assert.equal(paymentOutcome({ totalDue: '9999999999999999', payment: { id: 1 } }), 'Đã thanh toán')
  assert.equal(paymentOutcome({ totalDue: '250000', paymentState: 'ChoXacNhan', payment: { id: 1 } }), 'Chờ xác nhận')
})

test('payment GET uses authenticated no-store transport, refresh and exact parsing; never POSTs payment', async () => {
  const calls = []
  let refreshes = 0
  const client = createIdentityClient('http://localhost', async (url, options) => {
    const path = new URL(url).pathname
    if (path.endsWith('/login')) return Response.json({ accessToken: 'old', refreshToken: 'refresh' })
    if (path.endsWith('/me')) return Response.json({ accountType: 'customer', accountId: 1 })
    if (path.endsWith('/refresh')) { refreshes++; return Response.json({ accessToken: 'new', refreshToken: 'rotated' }) }
    calls.push({ path, options })
    if (options.headers.Authorization === 'Bearer old') return Response.json({ code: 'invalidSession' }, { status: 401 })
    assert.equal(options.headers.Authorization, 'Bearer new')
    return new Response('{"registrationId":7,"totalDue":9999999999999999.00,"paymentState":"ThanhCong","payment":{"amount":9999999999999999.00,"method":"TheTaiQuay","transactionCode":"TT-DEMO","maskedIdentifier":"****4242"}}')
  })
  await assert.rejects(client.getRegistrationPayment(7), error => error.status === 401)
  await client.login('customer@example.invalid', 'synthetic-password')
  const detail = await client.getRegistrationPayment(7)
  assert.equal(detail.payment.amount, '9999999999999999')
  assert.equal(detail.payment.transactionCode, 'TT-DEMO')
  assert.equal(paymentOutcome(detail), 'Đã thanh toán')
  assert.equal(paymentMethodLabel(detail.payment.method), 'Thẻ tại quầy')
  assert.equal(refreshes, 1)
  assert.equal(calls.length, 2)
  for (const { path, options } of calls) {
    assert.equal(path, '/api/v1/registrations/7/payment')
    assert.equal(options.method, 'GET')
    assert.equal(options.body, undefined)
    assert.equal(options.credentials, 'omit')
    assert.equal(options.cache, 'no-store')
  }
})

test('HTTP failures and unavailable refresh show safe payment-specific messages', async () => {
  let status = 403
  const client = createIdentityClient('http://localhost', async url => {
    if (url.endsWith('/login')) return Response.json({ accessToken: 'token', refreshToken: 'refresh' })
    if (url.endsWith('/me')) return Response.json({ accountType: 'customer' })
    return Response.json({ code: status === 401 ? 'invalidSession' : 'paymentDataConflict', detail: 'PRIVATE SQL', errors: { profile: ['PRIVATE PII'] } }, { status })
  })
  await client.login('synthetic@example.invalid', 'synthetic-password')
  const expected = { 401: /hết hạn/, 403: /không có quyền/, 404: /Không tìm thấy/, 409: /không nhất quán/, 503: /chưa sẵn sàng/ }
  for (status of [401, 403, 404, 409, 503]) {
    await assert.rejects(client.getRegistrationPayment(7), error => {
      assert.equal(error.status, status)
      assert.match(paymentError(error), expected[status])
      assert.doesNotMatch(paymentError(error), /PRIVATE/)
      return true
    })
  }
  for (const error of [new TypeError('PRIVATE'), new DOMException('PRIVATE', 'TimeoutError'), new SyntaxError('PRIVATE')])
    assert.match(paymentError(error), /kiểm tra kết nối/)
})

for (const duringBody of [false, true]) {
  test(`late payment response is discarded after logout/account switch (${duringBody ? 'reading body' : 'waiting for headers'})`, async () => {
    const started = Promise.withResolvers()
    const release = Promise.withResolvers()
    const client = createIdentityClient('http://localhost', async (url, options) => {
      if (url.endsWith('/login')) { const { identifier } = JSON.parse(options.body); return Response.json({ accessToken: identifier, refreshToken: identifier }) }
      if (url.endsWith('/me')) return Response.json({ accountType: 'customer' })
      if (url.endsWith('/logout')) return new Response(null, { status: 204 })
      if (options.headers.Authorization === 'Bearer b@example.invalid') return Response.json({ payment: null, totalDue: 0 })
      const read = async () => { started.resolve(); await release.promise; return '{"payment":{"id":1,"amount":650000}}' }
      if (duringBody) return { ok: true, status: 200, text: read }
      await read()
      return Response.json({ payment: { id: 1, amount: 650000 } })
    })
    await client.login('a@example.invalid', 'synthetic-password')
    const old = assert.rejects(client.getRegistrationPayment(7), /hết hạn/)
    await started.promise
    await client.logout()
    await client.login('b@example.invalid', 'synthetic-password')
    release.resolve()
    await old
    assert.deepEqual(await client.getRegistrationPayment(8), { payment: null, totalDue: '0' })
  })
}

test('card validation checks brand, number length and Luhn before sending', () => {
  assert.equal(cardNumberValid('VISA', '4111 1111 1111 1111'), true)
  assert.equal(cardNumberValid('MASTER', '5555 5555 5555 4444'), true)
  assert.equal(cardNumberValid('JCB', '3530 1113 3330 0000'), true)
  assert.equal(cardNumberValid('MASTER', '4111 1111 1111 1111'), false)
  assert.equal(cardNumberValid('VISA', '4111 1111 1111 1112'), false)
  assert.equal(cardNumberValid('VISA', '4111'), false)
})

function checkoutClient(send) {
  return createIdentityClient('http://localhost', (url, options) => {
    const path = new URL(url).pathname
    if (path.endsWith('/login')) return Response.json({ accessToken: 'access', refreshToken: 'refresh' })
    if (path.endsWith('/me')) return Response.json({ accountType: 'customer', accountId: 1 })
    if (path.endsWith('/logout')) return new Response(null, { status: 204 })
    return send(path, options)
  })
}

for (const failure of ['network', 'timeout', '503']) {
  test(`financial POST stays locked after ${failure} until GET and explicit acknowledgement`, async () => {
    let posts = 0
    let reads = 0
    const client = checkoutClient((path, options) => {
      if (options.method === 'GET') {
        reads++
        return Response.json({ payment: null, totalDue: 250000 })
      }
      assert.equal(path, '/api/v1/registrations/7/payment/online')
      assert.equal(options.method, 'POST')
      posts++
      if (posts > 1) return new Response(null, { status: 201 })
      if (failure === 'network') throw new TypeError('network')
      if (failure === 'timeout') throw new DOMException('timeout', 'TimeoutError')
      return Response.json({ code: 'dependencyUnavailable' }, { status: 503 })
    })
    await client.login('customer@example.invalid', 'synthetic')
    await assert.rejects(client.recordOnlinePayment(7, { method: 'TheQuocTe' }))
    client.acknowledgePaymentRetry(7)
    await assert.rejects(client.recordOnlinePayment(7, {}), /đối chiếu/)
    assert.equal(posts, 1)
    assert.equal(client.paymentWriteState(7), 'unknown')
    await client.getRegistrationPayment(7)
    assert.equal(reads, 1)
    assert.equal(client.paymentWriteState(7), 'unconfirmed')
    await assert.rejects(client.recordOnlinePayment(7, {}), /đối chiếu/)
    client.acknowledgePaymentRetry(7)
    await client.recordOnlinePayment(7, {})
    assert.equal(posts, 2)
    await client.getRegistrationPayment(7)
    client.acknowledgePaymentRetry(7)
    assert.equal(client.paymentWriteState(7), 'committed')
  })
}

for (const outcome of ['committed', 'paymentAlreadyRecorded', 'paymentNotRequired', 'paymentDataConflict']) {
  test(`payment response ${outcome} blocks double submit and reconciles with GET only`, async () => {
    const release = Promise.withResolvers()
    const calls = []
    const client = checkoutClient(async (path, options) => {
      calls.push(options.method)
      if (options.method === 'GET') return Response.json(outcome === 'paymentNotRequired'
        ? { payment: null, totalDue: 0 } : { payment: { id: 1 }, totalDue: 650000 })
      await release.promise
      if (outcome === 'committed') {
        return { ok: true, status: 201, json: () => assert.fail('Create response body is not read') }
      }
      return Response.json({ code: outcome }, { status: 409 })
    })
    await client.login('customer@example.invalid', 'synthetic')
    const first = client.recordOnlinePayment(7, { method: 'TheQuocTe' })
    const result = outcome === 'committed' ? first : assert.rejects(first, { code: outcome })
    await assert.rejects(client.recordOnlinePayment(7, {}), /đối chiếu/)
    release.resolve()
    await result
    if (outcome === 'paymentDataConflict') {
      client.acknowledgePaymentRetry(7)
      assert.equal(client.paymentWriteState(7), 'conflict')
      assert.deepEqual(calls, ['POST'])
    } else {
      await client.getRegistrationPayment(7)
      assert.equal(client.paymentWriteState(7), undefined)
      assert.deepEqual(calls, ['POST', 'GET'])
    }
  })
}

test('financial writes do not refresh/replay on 401 and an old response cannot lock a new session', async () => {
  let mode = 'expired'
  const release = Promise.withResolvers()
  const calls = []
  const client = checkoutClient(async (path, options) => {
    calls.push([path, options.method])
    if (mode === 'expired') return Response.json({ code: 'invalidSession' }, { status: 401 })
    await release.promise
    return new Response(null, { status: 201 })
  })
  await client.login('customer@example.invalid', 'synthetic')
  await assert.rejects(client.recordOnlinePayment(7, {}), { status: 401 })
  assert.equal(client.paymentWriteState(7), undefined)
  mode = 'delayed'
  const old = assert.rejects(client.recordOnlinePayment(7, {}), /hết hạn/)
  await client.logout()
  await client.login('other@example.invalid', 'synthetic')
  release.resolve()
  await old
  assert.equal(client.paymentWriteState(7), undefined)
  assert.deepEqual(calls, Array(2).fill(['/api/v1/registrations/7/payment/online', 'POST']))
})
