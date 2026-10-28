import test from 'node:test'
import assert from 'node:assert/strict'
import { emptyRecipient, profileBody, profileError } from '../profileForm.js'

test('new recipient uses the explicitly selected linked customer, never the first item', () => {
  const customers = [{ id: 91 }, { id: 22 }]
  assert.equal(profileBody('recipients', { ...emptyRecipient, customerId: '22' }, customers).customerId, 22)
  for (const customerId of ['', undefined, 999]) {
    assert.throws(() => profileBody('recipients', { ...emptyRecipient, customerId }, customers), /Chọn hồ sơ/)
  }
})

test('editing cannot transfer the recipient; empty identity stays nullable', () => {
  const body = profileBody('recipients', { ...emptyRecipient, id: 7, customerId: 91, identityNumber: '  ' }, [])
  assert.equal(body.identityNumber, null)
  assert.equal('id' in body, false)
  assert.equal('customerId' in body, false)
})

test('customer payload preserves contact fields and trims optional identity', () => {
  assert.deepEqual(profileBody('customers', { id: 1, fullName: 'Synthetic', phoneNumber: '0900000000', email: 'ux@example.invalid', identityNumber: ' 123456789012 ' }, []), {
    fullName: 'Synthetic', phoneNumber: '0900000000', email: 'ux@example.invalid', identityNumber: '123456789012',
  })
})

test('network and transport errors show useful feedback without implementation terms', () => {
  for (const error of [new TypeError('Failed to fetch'), new Error('API trả HTTP 500'), new SyntaxError('JSON'), { name: 'TimeoutError' }]) {
    assert.match(profileError(error), /Vui lòng thử lại/)
    assert.doesNotMatch(profileError(error), /API|SQL|HTTP|JSON|fetch/)
  }
  assert.equal(profileError(new Error('Hồ sơ đang được sử dụng, không thể xóa.')), 'Hồ sơ đang được sử dụng, không thể xóa.')
})
