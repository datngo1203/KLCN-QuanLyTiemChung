export const salutations = ['Ông', 'Bà', 'Anh', 'Chị', 'Khác']
export const relationships = ['Bản thân', 'Con', 'Anh', 'Chị', 'Em', 'Cha', 'Mẹ', 'Vợ', 'Chồng', 'Ông', 'Bà', 'Cùng hộ khẩu']
export const emptyCustomer = { fullName: '', identityNumber: '', phoneNumber: '', email: '', salutation: '', dateOfBirth: '' }
export const emptyRecipient = { fullName: '', identityNumber: '', dateOfBirth: '', gender: 'Nam', relationshipToCustomer: '' }

export const vietnamToday = () => new Intl.DateTimeFormat('en-CA', {
  timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit', day: '2-digit',
}).format(new Date())

export function profileBody(resource, draft, customers) {
  const { id, customerId, profileCode: _profileCode, ...body } = draft
  body.identityNumber = body.identityNumber?.trim() || null
  if (!body.dateOfBirth || body.dateOfBirth > vietnamToday())
    throw new Error('Ngày sinh không được lớn hơn ngày hiện tại tại Việt Nam.')
  if (resource === 'customers' && !salutations.includes(body.salutation))
    throw new Error('Chọn danh xưng hợp lệ.')
  if (resource === 'recipients') {
    if (!relationships.includes(body.relationshipToCustomer))
      throw new Error('Chọn quan hệ với khách hàng trong danh sách.')
    const owner = customers.find(customer => customer.id === Number(customerId))
    if (!id) {
      if (!owner) throw new Error('Chọn hồ sơ khách hàng liên kết trước khi lưu người được tiêm.')
      body.customerId = owner.id
    }
    if (owner?.dateOfBirth && !birthDatesAgree(body.relationshipToCustomer, body.dateOfBirth, owner.dateOfBirth))
      throw new Error('Ngày sinh không phù hợp với quan hệ của người được tiêm đối với khách hàng.')
  }
  return body
}

function birthDatesAgree(relationship, date, ownerDate) {
  switch (relationship) {
    case 'Bản thân': return date === ownerDate
    case 'Con': return date > ownerDate
    case 'Cha': case 'Mẹ': case 'Ông': case 'Bà': return date < ownerDate
    case 'Anh': case 'Chị': return date <= ownerDate
    case 'Em': return date >= ownerDate
    default: return true
  }
}

export function profileError(error) {
  if (error instanceof TypeError || error instanceof SyntaxError || /AbortError|TimeoutError/.test(error.name)
    || /API|HTTP|SQL|fetch|network|JSON/i.test(error.message || ''))
    return 'Không thể kết nối hoặc xử lý yêu cầu. Vui lòng thử lại.'
  return error.message || 'Chưa thực hiện được thao tác. Vui lòng thử lại.'
}
