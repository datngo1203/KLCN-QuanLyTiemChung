import test, { before, after } from 'node:test'
import assert from 'node:assert/strict'
import { fileURLToPath } from 'node:url'
import { Window } from 'happy-dom'
import { createServer } from 'vite'
import { createElement, act } from 'react'
import { createRoot } from 'react-dom/client'

let server, Payments, identity

before(async () => {
  server = await createServer({ root: fileURLToPath(new URL('../..', import.meta.url)),
    server: { middlewareMode: true, ws: false }, logLevel: 'silent' })
  Payments = (await server.ssrLoadModule('/src/Payments.jsx')).default
  identity = (await server.ssrLoadModule('/src/api/identity.js')).identity
})
after(async () => { await server?.close() })

test('checkout keeps the registration snapshot, opens a local QR without writing, and disables a second submit', async t => {
  const window = new Window()
  const originalFormData = globalThis.FormData
  Object.assign(globalThis, { window, document: window.document, FormData: window.FormData, IS_REACT_ACT_ENVIRONMENT: true })
  window.HTMLElement.prototype.scrollIntoView = () => {}
  const host = document.createElement('div')
  document.body.append(host)
  const root = createRoot(host)
  t.after(async () => {
    await act(async () => root.unmount())
    globalThis.FormData = originalFormData
    await window.happyDOM.close()
  })
  const customer = { fullName: 'Hồ sơ hiện tại' }
  const recipient = { fullName: 'Người được tiêm' }
  const detail = {
    registrationId: 7, customer, recipient, subtotal: '500000', consultationFee: '50000', totalDue: '550000',
    items: [], payment: null, paymentState: 'ChuaThanhToan',
    registrationContact: { fullName: 'Liên hệ đã chốt', phoneNumber: '0900000123', provinceCity: 'Hà Nội',
      wardCommune: 'Hoàn Kiếm', addressLine: 'Địa chỉ thử' },
    message: 'PRIVATE HTTP SQL DTO [DEMO]',
  }
  identity.registrations = async () => ({ items: [{ id: 7, customer, recipient, totalBeforeDiscount: '550000' }] })
  identity.getPaymentSettings = async () => ({
    receiverBankName: 'Đơn vị nhận chuyển khoản', receiverAccountName: 'Trung tâm tiêm chủng VNVC', receiverAccountNumber: '0000000402',
  })
  const pending = Promise.withResolvers()
  const posted = []
  let result
  identity.paymentWriteState = () => result
  identity.recordOnlinePayment = async (id, body) => {
    posted.push({ id, body })
    await pending.promise
    detail.payment = { id: 1, method: 'ChuyenKhoanOnline', amount: '550000', occurredAt: '2026-11-04T00:00:00Z',
      provider: body.transfer.sourceBankOrWallet, reference: body.transfer.transferReference }
    detail.paymentState = 'ChoXacNhan'
    result = 'committed'
  }
  identity.getRegistrationPayment = async () => { result = undefined; return detail }
  const field = name => host.querySelector(`input[name="${name}"]`)
  const submit = () => host.querySelector('form').dispatchEvent(new window.Event('submit', { bubbles: true, cancelable: true }))
  await act(async () => root.render(createElement(Payments, { account: { accountType: 'customer', accountId: 1 } })))
  await act(async () => host.querySelector('.payment-select').click())
  assert.equal(field('fullName').value, 'Liên hệ đã chốt')
  assert.equal(host.querySelectorAll('[name=method]').length, 5)
  await act(async () => host.querySelector('[value=TheNoiDia]').click())
  field('bankName').value = 'Ngân hàng gửi'
  field('accountHolderName').value = 'Chủ tài khoản'
  field('cardOrAccountNumber').value = '1122334455'
  await act(async () => submit())
  assert.ok(field('otp'))
  assert.match(host.textContent, /Nhập mã OTP gồm 6 chữ số để xác thực\./)
  assert.doesNotMatch(host.textContent, /Mã thử/)
  await act(async () => host.querySelector('[value=TheQuocTe]').click())
  assert.equal(host.querySelector('select[name=cardBrand]').labels[0].textContent, 'Loại thẻ')
  field('cardNumber').value = '4111 1111 1111 1111'
  field('cvv').value = '683'
  field('terms').checked = true
  await act(async () => host.querySelector('[value=ChuyenKhoanOnline]').click())
  assert.equal(field('cardNumber'), null)
  assert.equal(field('cvv'), null)
  assert.equal(field('terms').checked, false)
  assert.match(host.querySelector('img').getAttribute('src'), /^data:image\/gif/)
  assert.match(host.textContent, /Đơn vị nhận chuyển khoản/)
  assert.doesNotMatch(host.textContent, /Trung tâm tiêm chủng VAC|mô phỏng|không xử lý tiền thật/)
  assert.match(host.textContent, /Trung tâm tiêm chủng VNVC/)
  assert.match(host.textContent, /VNVC DK-0007/)
  assert.equal(posted.length, 0)
  field('fullName').value = 'Người trả tiền'
  field('sourceBankOrWallet').value = 'Ngân hàng thử'
  field('accountHolderName').value = 'Chủ tài khoản'
  field('sourceAccountOrWalletId').value = '1122334455'
  field('transferReference').value = 'CK-111'
  field('terms').checked = true
  await act(async () => { submit(); submit() })
  assert.equal(posted.length, 1)
  assert.equal(host.querySelector('.payment-checkout-actions button').disabled, true)
  assert.equal(posted[0].body.payer.fullName, 'Người trả tiền')
  assert.equal(detail.registrationContact.fullName, 'Liên hệ đã chốt')
  assert.equal(posted[0].body.termsAccepted, true)
  await act(async () => pending.resolve())
  assert.match(host.textContent, /Chờ xác nhận/)
  assert.doesNotMatch(host.textContent, /PRIVATE|HTTP|SQL|DTO|\[DEMO\]|KẾT QUẢ TỪ QUẦY|Biên nhận thanh toán|Thanh toán trong môi trường thử nghiệm/)
  assert.equal(window.localStorage.length, 0)
})

test('PayOS QR is a distinct method, creates payment with payer/terms only, renders QR and checkout link in Pending', async t => {
  const window = new Window()
  const originalFormData = globalThis.FormData
  Object.assign(globalThis, { window, document: window.document, FormData: window.FormData, IS_REACT_ACT_ENVIRONMENT: true })
  window.HTMLElement.prototype.scrollIntoView = () => {}
  const host = document.createElement('div')
  document.body.append(host)
  const root = createRoot(host)
  t.after(async () => {
    await act(async () => root.unmount())
    globalThis.FormData = originalFormData
    await window.happyDOM.close()
  })

  const customer = { fullName: 'Hồ sơ PayOS' }
  const recipient = { fullName: 'Người được tiêm PayOS' }
  const detail = {
    registrationId: 8, customer, recipient, subtotal: '600000', consultationFee: '0', totalDue: '600000',
    items: [], payment: null, paymentState: 'ChuaThanhToan',
    registrationContact: { fullName: 'Liên hệ PayOS', phoneNumber: '0900000888', provinceCity: 'TP. Hồ Chí Minh',
      wardCommune: 'Bến Nghé', addressLine: '123 Lê Lợi' },
  }
  identity.registrations = async () => ({ items: [{ id: 8, customer, recipient, totalBeforeDiscount: '600000' }] })
  identity.getRegistrationPayment = async () => detail

  const payOsCreates = []
  identity.createPayOsPayment = async (id, body) => {
    payOsCreates.push({ id, body })
    return {
      registrationId: 8,
      amount: 600000,
      currency: 'VND',
      state: 'Pending',
      checkoutUrl: 'https://pay.payos.vn/web/chk-888',
      qrCode: '00020101021238540010A0000007270124000697042201100001112222530370454061000005802VN62160812PAYOS88888886304ABCD',
      canReconcile: true,
    }
  }

  await act(async () => root.render(createElement(Payments, { account: { accountType: 'customer', accountId: 1 } })))
  await act(async () => host.querySelector('.payment-select').click())

  const methods = host.querySelectorAll('[name=method]')
  assert.equal(methods.length, 5)

  // Verify selecting PayOsQr does not show manual transfer or card fields
  await act(async () => host.querySelector('[value=PayOsQr]').click())
  assert.equal(host.querySelector('input[name=sourceBankOrWallet]'), null)
  assert.equal(host.querySelector('input[name=transferReference]'), null)
  assert.equal(host.querySelector('input[name=cardNumber]'), null)
  assert.match(host.textContent, /QR ngân hàng/)
  assert.match(host.textContent, /payOS/)

  // Verify switching to ChuyenKhoanOnline shows manual fields, and does not call PayOS API
  await act(async () => host.querySelector('[value=ChuyenKhoanOnline]').click())
  assert.match(host.textContent, /Chuyển khoản thủ công/)
  assert.equal(payOsCreates.length, 0)

  // Switch back to PayOsQr and submit
  await act(async () => host.querySelector('[value=PayOsQr]').click())
  host.querySelector('input[name="fullName"]').value = 'Người trả tiền PayOS'
  host.querySelector('input[name="terms"]').checked = true

  const submit = () => host.querySelector('form').dispatchEvent(new window.Event('submit', { bubbles: true, cancelable: true }))
  await act(async () => submit())

  // Assert create request: contains payer and terms only, no amount, orderCode, paymentLinkId, or card/transfer metadata
  assert.equal(payOsCreates.length, 1)
  assert.equal(payOsCreates[0].id, 8)
  assert.deepEqual(payOsCreates[0].body, {
    payer: {
      fullName: 'Người trả tiền PayOS',
      phoneNumber: '0900000888',
      email: '',
      provinceCity: 'TP. Hồ Chí Minh',
      wardCommune: 'Bến Nghé',
      addressLine: '123 Lê Lợi',
    },
    termsAccepted: true,
  })
  assert.equal('amount' in payOsCreates[0].body, false)
  assert.equal('orderCode' in payOsCreates[0].body, false)
  assert.equal('paymentLinkId' in payOsCreates[0].body, false)
  assert.equal('cardNumber' in payOsCreates[0].body, false)
  assert.equal('transfer' in payOsCreates[0].body, false)

  // Prove Pending state: QR rendered, checkout action shown, exact amount shown, no raw enums
  const qrImg = host.querySelector('img[alt*="Mã QR"]')
  assert.ok(qrImg)
  assert.match(qrImg.getAttribute('src'), /^data:image\/gif/)
  const checkoutLink = host.querySelector('a.payos-checkout-link')
  assert.ok(checkoutLink)
  assert.equal(checkoutLink.getAttribute('href'), 'https://pay.payos.vn/web/chk-888')
  assert.equal(checkoutLink.getAttribute('target'), '_blank')
  assert.match(host.textContent, /Đang chờ thanh toán \/ xác nhận từ ngân hàng\./)
  assert.match(host.textContent, /600\.000 đ/)
  assert.doesNotMatch(host.textContent, /\bPending\b|\bPayOsQr\b|\borderCode\b/)
})

test('PayOS create unknown outcome inspects local state via GET without a second POST, and reconcile converges to paid receipt', async t => {
  const window = new Window()
  const originalFormData = globalThis.FormData
  Object.assign(globalThis, { window, document: window.document, FormData: window.FormData, IS_REACT_ACT_ENVIRONMENT: true })
  window.HTMLElement.prototype.scrollIntoView = () => {}
  const host = document.createElement('div')
  document.body.append(host)
  const root = createRoot(host)
  t.after(async () => {
    await act(async () => root.unmount())
    globalThis.FormData = originalFormData
    await window.happyDOM.close()
  })

  const customer = { fullName: 'Hồ sơ Reconcile' }
  const recipient = { fullName: 'Người được tiêm' }
  let detail = {
    registrationId: 9, customer, recipient, subtotal: '450000', consultationFee: '0', totalDue: '450000',
    items: [], payment: null, paymentState: 'ChuaThanhToan',
    registrationContact: { fullName: 'Người liên hệ', phoneNumber: '0900000999', provinceCity: 'Đà Nẵng',
      wardCommune: 'Hải Châu', addressLine: '12 Bạch Đằng' },
  }
  identity.registrations = async () => ({ items: [{ id: 9, customer, recipient, totalBeforeDiscount: '450000' }] })
  identity.getRegistrationPayment = async () => detail

  let createCalls = 0
  let getCalls = 0
  let reconcileCalls = 0

  identity.createPayOsPayment = async () => {
    createCalls++
    throw Object.assign(new Error('Server error'), { status: 500 })
  }
  identity.getPayOsPayment = async () => {
    getCalls++
    return {
      registrationId: 9,
      amount: 450000,
      currency: 'VND',
      state: 'OutcomeUnknown',
      canReconcile: true,
    }
  }
  identity.reconcilePayOsPayment = async () => {
    reconcileCalls++
    if (reconcileCalls === 1) {
      return {
        registrationId: 9,
        amount: 450000,
        currency: 'VND',
        state: 'Pending',
        checkoutUrl: 'https://pay.payos.vn/web/chk-999',
        qrCode: '00020101021238540010A0000007270124000697042201100001112222530370454061000005802VN62160812PAYOS99999996304ABCD',
        canReconcile: true,
      }
    }
    detail = {
      ...detail,
      paymentState: 'ThanhCong',
      payment: {
        id: 99,
        amount: '450000',
        method: 'PayOsQr',
        status: 'ThanhCong',
        transactionCode: 'PAYOS-TX-999',
        occurredAt: '2026-11-09T08:00:00Z',
        reference: 'PAYOS-REF-999',
        payer: { fullName: 'Người liên hệ', phoneNumber: '0900000999' },
      },
    }
    return {
      registrationId: 9,
      amount: 450000,
      currency: 'VND',
      state: 'Paid',
      canReconcile: false,
    }
  }

  await act(async () => root.render(createElement(Payments, { account: { accountType: 'customer', accountId: 1 } })))
  await act(async () => host.querySelector('.payment-select').click())

  await act(async () => host.querySelector('[value=PayOsQr]').click())
  host.querySelector('input[name="terms"]').checked = true
  const submit = () => host.querySelector('form').dispatchEvent(new window.Event('submit', { bubbles: true, cancelable: true }))
  await act(async () => submit())

  assert.equal(createCalls, 1)
  assert.equal(getCalls, 1)
  assert.match(host.textContent, /Chưa xác định được trạng thái tạo giao dịch\./)
  assert.match(host.textContent, /Không thực hiện thanh toán lại lúc này\./)

  const reconcileBtn = Array.from(host.querySelectorAll('button')).find(b => b.textContent.includes('Đối chiếu trạng thái'))
  assert.ok(reconcileBtn)

  await act(async () => reconcileBtn.click())
  assert.equal(reconcileCalls, 1)
  assert.match(host.textContent, /Đang chờ thanh toán \/ xác nhận từ ngân hàng\./)
  assert.ok(host.querySelector('img[alt*="Mã QR"]'))

  const secondReconcileBtn = Array.from(host.querySelectorAll('button')).find(b => b.textContent.includes('Đối chiếu trạng thái'))
  await act(async () => secondReconcileBtn.click())
  assert.equal(reconcileCalls, 2)

  assert.match(host.textContent, /Biên nhận thanh toán/)
  assert.match(host.textContent, /Đã thanh toán/)
  assert.match(host.textContent, /QR ngân hàng/)
  assert.match(host.textContent, /PAYOS-TX-999/)
  assert.match(host.textContent, /PAYOS-REF-999/)
  assert.match(host.textContent, /450\.000 đ/)
})

test('PayOS webhook convergence: Pending page reloads normal detail and shows paid receipt without client-side paid mutation', async t => {
  const window = new Window()
  const originalFormData = globalThis.FormData
  Object.assign(globalThis, { window, document: window.document, FormData: window.FormData, IS_REACT_ACT_ENVIRONMENT: true })
  window.HTMLElement.prototype.scrollIntoView = () => {}
  const host = document.createElement('div')
  document.body.append(host)
  const root = createRoot(host)
  t.after(async () => {
    await act(async () => root.unmount())
    globalThis.FormData = originalFormData
    await window.happyDOM.close()
  })

  const customer = { fullName: 'Hồ sơ Webhook' }
  const recipient = { fullName: 'Người được tiêm' }
  let detail = {
    registrationId: 10, customer, recipient, subtotal: '700000', consultationFee: '0', totalDue: '700000',
    items: [], payment: null, paymentState: 'ChuaThanhToan',
    registrationContact: { fullName: 'Khách hàng', phoneNumber: '0900000777', provinceCity: 'Hà Nội',
      wardCommune: 'Ba Đình', addressLine: '1 Phố Huế' },
  }
  identity.registrations = async () => ({ items: [{ id: 10, customer, recipient, totalBeforeDiscount: '700000' }] })
  identity.getRegistrationPayment = async () => detail

  identity.createPayOsPayment = async () => ({
    registrationId: 10,
    amount: 700000,
    currency: 'VND',
    state: 'Pending',
    checkoutUrl: 'https://pay.payos.vn/web/chk-777',
    qrCode: '00020101021238540010A0000007270124000697042201100001112222530370454061000005802VN62160812PAYOS77777776304ABCD',
    canReconcile: true,
  })

  await act(async () => root.render(createElement(Payments, { account: { accountType: 'customer', accountId: 1 } })))
  await act(async () => host.querySelector('.payment-select').click())

  await act(async () => host.querySelector('[value=PayOsQr]').click())
  host.querySelector('input[name="terms"]').checked = true
  const submit = () => host.querySelector('form').dispatchEvent(new window.Event('submit', { bubbles: true, cancelable: true }))
  await act(async () => submit())

  assert.match(host.textContent, /Đang chờ thanh toán \/ xác nhận từ ngân hàng\./)

  // Simulate webhook arrival on server: detail in DB becomes ThanhCong
  detail = {
    ...detail,
    paymentState: 'ThanhCong',
    payment: {
      id: 77,
      amount: '700000',
      method: 'PayOsQr',
      status: 'ThanhCong',
      transactionCode: 'PAYOS-TX-777',
      occurredAt: '2026-11-09T08:30:00Z',
      reference: 'PAYOS-REF-777',
      payer: { fullName: 'Khách hàng', phoneNumber: '0900000777' },
    },
  }

  // User clicks "Tải lại kết quả"
  const reloadBtn = Array.from(host.querySelectorAll('button')).find(b => b.textContent.includes('Tải lại kết quả'))
  assert.ok(reloadBtn)
  await act(async () => reloadBtn.click())

  // Authoritative receipt is shown without any client-side paid mutation
  assert.match(host.textContent, /Biên nhận thanh toán/)
  assert.match(host.textContent, /Đã thanh toán/)
  assert.match(host.textContent, /QR ngân hàng/)
  assert.match(host.textContent, /PAYOS-TX-777/)
  assert.match(host.textContent, /PAYOS-REF-777/)
  assert.match(host.textContent, /700\.000 đ/)
})
