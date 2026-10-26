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
      throw new Error(messages[problem.code] || `Không thực hiện được yêu cầu (HTTP ${response.status}).`)
    }
    return response.status === 204 ? null : response.json()
  }
  async function profiles(path, method = 'GET', body) {
    if (!tokens) throw new Error(messages.invalidSession)
    const send = () => request(`${baseUrl}/api/v1/${path}`, {
      method, credentials: 'omit', headers: { Authorization: `Bearer ${tokens.accessToken}`, ...(body ? { 'Content-Type': 'application/json' } : {}) },
      body: body ? JSON.stringify(body) : undefined, signal: AbortSignal.timeout(15000),
    })
    const accessToken = tokens.accessToken
    let response = await send()
    if (response.status === 401) {
      if (tokens?.accessToken === accessToken) {
        refreshing ||= call('refresh', { clientType: 'native', refreshToken: tokens.refreshToken })
          .then(session => { tokens = session }).finally(() => { refreshing = null })
        await refreshing
      }
      if (!tokens) throw new Error(messages.invalidSession)
      response = await send()
    }
    if (!response.ok) {
      const problem = await response.json().catch(() => ({}))
      const details = Object.values(problem.errors || {}).flat().join(' ')
      throw new Error(details || ({ validation: 'Kiểm tra thông tin hồ sơ.', permissionDenied: 'Bạn không có quyền thao tác hồ sơ này.', duplicateIdentity: 'CCCD đã tồn tại.', profileAlreadyExists: 'Bạn đã có hồ sơ. Hãy tải lại.', profileReferenced: 'Hồ sơ đang được sử dụng, không thể xóa.' })[problem.code] || messages[problem.code] || `API trả HTTP ${response.status}`)
    }
    return response.status === 204 ? null : response.json()
  }
  return {
    profiles,
    register: (email, password) => call('register', { email, password }),
    confirm: token => call('email-verifications/confirm', { token }),
    resend: email => call('email-verifications/resend', { email }),
    async login(identifier, password) {
      tokens = null
      const session = await call('login', { identifier, password, clientType: 'native' })
      try {
        const account = await call('me', undefined, session.accessToken)
        if (account.accountType !== 'customer') throw new Error('Cổng này dành cho tài khoản khách hàng.')
        tokens = session
        return account
      } catch (error) {
        await call('logout', { clientType: 'native', refreshToken: session.refreshToken })
        throw error
      }
    },
    async logout() {
      if (tokens) await call('logout', { clientType: 'native', refreshToken: tokens.refreshToken })
      tokens = null
    },
  }
}

export const identity = createIdentityClient((import.meta.env?.VITE_API_BASE_URL || 'http://localhost:5080').replace(/\/$/, ''))
