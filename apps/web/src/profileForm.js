export const emptyCustomer = { fullName: '', identityNumber: '', phoneNumber: '', email: '' }
export const emptyRecipient = { fullName: '', identityNumber: '', dateOfBirth: '', gender: 'Nam', relationshipToCustomer: 'Con' }

export function profileBody(resource, draft, customers) {
  const { id, customerId, ...body } = draft
  body.identityNumber = body.identityNumber?.trim() || null
  if (resource === 'recipients' && !id) {
    const owner = customers.find(customer => customer.id === Number(customerId))
    if (!owner) throw new Error('Chọn hồ sơ khách hàng liên kết trước khi lưu người được tiêm.')
    body.customerId = owner.id
  }
  return body
}

export function profileError(error) {
  if (error instanceof TypeError || error instanceof SyntaxError || /AbortError|TimeoutError/.test(error.name)
    || /API|HTTP|SQL|fetch|network|JSON/i.test(error.message || ''))
    return 'Không thể kết nối hoặc xử lý yêu cầu. Vui lòng thử lại.'
  return error.message || 'Chưa thực hiện được thao tác. Vui lòng thử lại.'
}
