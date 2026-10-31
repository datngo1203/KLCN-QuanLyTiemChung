import { parseRegistrationJson, registrationBody } from '../registrationForm.js'

const messages = {
  invalidCredentials: 'Email hoặc mật khẩu không đúng.',
  emailVerificationRequired: 'Cần xác minh email trước khi đăng nhập.',
  invalidSession: 'Phiên đã hết hạn. Vui lòng đăng nhập lại.',
  invalidVerificationToken: 'Mã xác minh không hợp lệ hoặc đã hết hạn.',
  validation: 'Kiểm tra email và mật khẩu (12–128 ký tự). Demo dùng email @example.invalid.',
  rateLimited: 'Thao tác quá nhiều lần. Vui lòng thử lại sau.',
  dependencyUnavailable: 'Dịch vụ chưa sẵn sàng. Vui lòng thử lại sau.',
}

// S1 uses the existing token response transport; all tokens stay in this closure.
// No browser storage, refresh cookies or automatic session restore.
export function createIdentityClient(baseUrl, request = globalThis.fetch) {
  let tokens = null
  let refreshing = null
  let sessionVersion = 0
  async function call(path, body, accessToken) {
    const response = await request(`${baseUrl}/api/v1/auth/${path}`, {
      method: body === undefined ? 'GET' : 'POST',
      credentials: 'omit',
      headers: { ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
        ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}) },
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: AbortSignal.timeout(15000),
    })
    if (!response.ok) {
      const problem = await response.json().catch(() => ({}))
      const passwordMessage = path === 'change-password' && ({
        invalidCredentials: 'Mật khẩu hiện tại không đúng.',
        validation: 'Mật khẩu mới cần 12–128 ký tự và khác mật khẩu hiện tại.',
      })[problem.code]
      throw Object.assign(new Error(passwordMessage || messages[problem.code] || `Không thực hiện được yêu cầu (HTTP ${response.status}).`), { status: response.status, code: problem.code })
    }
    return response.status === 204 ? null : response.json()
  }
  function refresh(session) {
    if (tokens !== session) throw new Error(messages.invalidSession)
    refreshing ||= call('refresh', { clientType: 'native', refreshToken: session.refreshToken })
      .then(renewed => {
        if (tokens !== session) throw new Error(messages.invalidSession)
        tokens = renewed
      }).finally(() => { refreshing = null })
    return refreshing
  }
  async function profiles(path, method = 'GET', body, registration = false) {
    if (!tokens) throw Object.assign(new Error(messages.invalidSession), { status: 401 })
    const version = sessionVersion
    const send = () => request(`${baseUrl}/api/v1/${path}`, {
      method, credentials: 'omit', cache: 'no-store', headers: { Authorization: `Bearer ${tokens.accessToken}`, ...(body ? { 'Content-Type': 'application/json' } : {}) },
      body: body ? (registration ? registrationBody(body) : JSON.stringify(body)) : undefined, signal: AbortSignal.timeout(15000),
    })
    const session = tokens
    let response = await send()
    if (version !== sessionVersion) throw new Error(messages.invalidSession)
    if (response.status === 401) {
      if (tokens === session) await refresh(session)
      if (version !== sessionVersion) throw new Error(messages.invalidSession)
      if (!tokens) throw new Error(messages.invalidSession)
      response = await send()
    }
    if (version !== sessionVersion) throw new Error(messages.invalidSession)
    if (!response.ok) {
      const problem = await response.json().catch(() => ({}))
      if (version !== sessionVersion) throw new Error(messages.invalidSession)
      const details = Object.values(problem.errors || {}).flat().join(' ')
      throw Object.assign(new Error(details || (registration ? { validation: 'Kiểm tra hồ sơ, dịch vụ và số lượng đăng ký.', permissionDenied: 'Bạn không có quyền truy cập đăng ký này.', catalogChanged: 'Giá hoặc thành phần dịch vụ đã thay đổi.', resourceNotFound: 'Hồ sơ hoặc dịch vụ không còn tồn tại.' } : { validation: 'Kiểm tra thông tin hồ sơ.', permissionDenied: 'Bạn không có quyền thao tác hồ sơ này.', duplicateIdentity: 'CCCD đã tồn tại.', profileAlreadyExists: 'Bạn đã có hồ sơ. Hãy tải lại.', profileReferenced: 'Hồ sơ đang được sử dụng, không thể xóa.' })[problem.code] || messages[problem.code] || `API trả HTTP ${response.status}`), { status: response.status, code: problem.code })
    }
    if (registration && method === 'POST' && response.status !== 201)
      throw Object.assign(new Error('API chưa xác nhận tạo đăng ký (cần HTTP 201). Hãy kiểm tra danh sách trước khi tạo lại.'), { uncertain: true })
    const result = registration ? parseRegistrationJson(await response.text()) : response.status === 204 ? null : await response.json()
    if (version !== sessionVersion) throw new Error(messages.invalidSession)
    return result
  }
  return {
    profiles,
    appointments: (query = '') => profiles(`appointments${query}`),
    getAppointment: id => profiles(`appointments/${id}`),
    registrations: (path = '', method = 'GET', body) => profiles(`registrations${path}`, method, body, true),
    getRegistrationPayment: id => profiles(`registrations/${id}/payment`, 'GET', undefined, true),
    async changePassword(currentPassword, newPassword, confirmation) {
      if (!currentPassword) throw new Error('Nhập mật khẩu hiện tại.')
      if (newPassword.length < 12 || newPassword.length > 128 || newPassword === currentPassword)
        throw new Error('Mật khẩu mới cần 12–128 ký tự và khác mật khẩu hiện tại.')
      if (newPassword !== confirmation) throw new Error('Xác nhận mật khẩu không khớp.')
      if (!tokens) throw new Error(messages.invalidSession)
      const version = sessionVersion
      // Finish any Profiles refresh before revoking every session.
      if (refreshing) await refreshing
      if (version !== sessionVersion) throw new Error(messages.invalidSession)
      await refresh(tokens)
      if (version !== sessionVersion) throw new Error(messages.invalidSession)
      await call('change-password', { currentPassword, newPassword }, tokens.accessToken)
      if (version !== sessionVersion) throw new Error(messages.invalidSession)
      tokens = null
      sessionVersion++
    },
    register: (email, password) => call('register', { email, password }),
    confirm: token => call('email-verifications/confirm', { token }),
    resend: email => call('email-verifications/resend', { email }),
    async login(identifier, password) {
      tokens = null
      const version = ++sessionVersion
      const session = await call('login', { identifier, password, clientType: 'native' })
      try {
        const account = await call('me', undefined, session.accessToken)
        if (account.accountType !== 'customer') throw new Error('Cổng này dành cho tài khoản khách hàng.')
        if (version !== sessionVersion) throw new Error(messages.invalidSession)
        tokens = session
        return account
      } catch (error) {
        await call('logout', { clientType: 'native', refreshToken: session.refreshToken })
        throw error
      }
    },
    async logout() {
      const version = sessionVersion
      // Logout accepts the last credential even if refresh failed or already rotated it.
      if (refreshing) await refreshing.catch(() => {})
      if (version !== sessionVersion) throw new Error(messages.invalidSession)
      if (tokens) await call('logout', { clientType: 'native', refreshToken: tokens.refreshToken })
      if (version !== sessionVersion) throw new Error(messages.invalidSession)
      tokens = null
      sessionVersion++
    },
  }
}

export const identity = createIdentityClient((import.meta.env?.VITE_API_BASE_URL || 'http://localhost:5080').replace(/\/$/, ''))
