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
    receiverBankName: 'VAC', receiverAccountName: 'VAC', receiverAccountNumber: '0000000402',
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
  assert.equal(host.querySelectorAll('[name=method]').length, 4)
  await act(async () => host.querySelector('[value=TheQuocTe]').click())
  field('cardNumber').value = '4111 1111 1111 1111'
  field('cvv').value = '683'
  field('terms').checked = true
  await act(async () => host.querySelector('[value=ChuyenKhoanOnline]').click())
  assert.equal(field('cardNumber'), null)
  assert.equal(field('cvv'), null)
  assert.equal(field('terms').checked, false)
  assert.match(host.querySelector('img').getAttribute('src'), /^data:image\/gif/)
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
  assert.doesNotMatch(host.textContent, /PRIVATE|HTTP|SQL|DTO|\[DEMO\]|KẾT QUẢ TỪ QUẦY|Biên nhận thanh toán/)
  assert.equal((host.textContent.match(/Thanh toán trong môi trường thử nghiệm/g) ?? []).length, 1)
  assert.equal(window.localStorage.length, 0)
})
