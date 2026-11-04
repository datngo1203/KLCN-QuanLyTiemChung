import test from 'node:test'
import assert from 'node:assert/strict'
import { emptyRecipient, profileBody, profileError, relationships, salutations } from '../profileForm.js'

const recipientDraft = { ...emptyRecipient, relationshipToCustomer: 'Con', dateOfBirth: '2020-01-01' }

test('new recipient uses the explicitly selected linked customer, never the first item', () => {
  const customers = [{ id: 91 }, { id: 22 }]
  assert.equal(profileBody('recipients', { ...recipientDraft, customerId: '22' }, customers).customerId, 22)
  for (const customerId of ['', undefined, 999]) {
    assert.throws(() => profileBody('recipients', { ...recipientDraft, customerId }, customers), /Chọn hồ sơ/)
  }
})

test('editing cannot transfer the recipient; empty identity stays nullable', () => {
  const body = profileBody('recipients', { ...recipientDraft, id: 7, customerId: 91, identityNumber: '  ' }, [])
  assert.equal(body.identityNumber, null)
  assert.equal('id' in body, false)
  assert.equal('customerId' in body, false)
})

test('customer payload preserves contact fields and trims optional identity', () => {
  assert.deepEqual(profileBody('customers', { id: 1, fullName: 'Synthetic', phoneNumber: '0900000000', email: 'ux@example.invalid', identityNumber: ' 123456789012 ', salutation: 'Chị', dateOfBirth: '1990-01-01' }, []), {
    fullName: 'Synthetic', phoneNumber: '0900000000', email: 'ux@example.invalid', identityNumber: '123456789012',
    salutation: 'Chị', dateOfBirth: '1990-01-01',
  })
})

test('controlled relationship and salutation lists and linked customer date checks', () => {
  assert.deepEqual(salutations, ['Ông', 'Bà', 'Anh', 'Chị', 'Khác'])
  assert.deepEqual(relationships, ['Bản thân', 'Con', 'Anh', 'Chị', 'Em', 'Cha', 'Mẹ', 'Vợ', 'Chồng', 'Ông', 'Bà', 'Cùng hộ khẩu'])
  const draft = { ...recipientDraft, customerId: 22 }
  const owner = [{ id: 22, dateOfBirth: '2020-01-01' }]
  assert.throws(() => profileBody('recipients', draft, owner), /không phù hợp/)
  assert.throws(() => profileBody('recipients', { ...draft, relationshipToCustomer: 'Khác' }, owner), /danh sách/)
  assert.throws(() => profileBody('recipients', { ...draft, dateOfBirth: '2999-01-01' }, owner), /Việt Nam/)
  assert.equal(profileBody('recipients', draft, [{ id: 22, dateOfBirth: null }]).dateOfBirth, '2020-01-01')
  for (const relationshipToCustomer of ['Bản thân', 'Anh', 'Chị', 'Em'])
    assert.equal(profileBody('recipients', { ...draft, relationshipToCustomer }, owner).relationshipToCustomer, relationshipToCustomer)
})

test('network and transport errors show useful feedback without implementation terms', () => {
  for (const error of [new TypeError('Failed to fetch'), new Error('API trả HTTP 500'), new SyntaxError('JSON'), { name: 'TimeoutError' }]) {
    assert.match(profileError(error), /Vui lòng thử lại/)
    assert.doesNotMatch(profileError(error), /API|SQL|HTTP|JSON|fetch/)
  }
  assert.equal(profileError(new Error('Hồ sơ đang được sử dụng, không thể xóa.')), 'Hồ sơ đang được sử dụng, không thể xóa.')
})
