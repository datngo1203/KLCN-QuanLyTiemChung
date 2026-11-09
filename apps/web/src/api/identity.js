import { parseRegistrationJson, registrationBody } from '../registrationForm.js'

const messages = {
  invalidCredentials: 'Email hoặc mật khẩu không đúng.',
  emailVerificationRequired: 'Cần xác minh email trước khi đăng nhập.',
  invalidSession: 'Phiên đã hết hạn. Vui lòng đăng nhập lại.',
  invalidVerificationToken: 'Mã xác minh không hợp lệ hoặc đã hết hạn.',
  invalidPasswordResetToken: 'Token đặt lại mật khẩu không hợp lệ, đã hết hạn hoặc đã được sử dụng. Vui lòng yêu cầu token mới.',
  validation: 'Kiểm tra email và mật khẩu (12–128 ký tự). Demo dùng email @example.invalid.',
  rateLimited: 'Thao tác quá nhiều lần. Vui lòng thử lại sau.',
  profileClaimUnavailable: 'Chưa thể liên kết hồ sơ. Hãy kiểm tra thông tin hoặc liên hệ nhân viên để được hỗ trợ.',
  dependencyUnavailable: 'Dịch vụ chưa sẵn sàng. Vui lòng thử lại sau.',
}

// S1 uses the existing token response transport; all tokens stay in this closure.
// No browser storage, refresh cookies or automatic session restore.
export function createIdentityClient(baseUrl, request = globalThis.fetch) {
  let tokens = null
  let refreshing = null
  let sessionVersion = 0
  const paymentWrites = new Map()
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
  async function profiles(path, method = 'GET', body, registration = false, paymentStatus) {
    if (!tokens) throw Object.assign(new Error(messages.invalidSession), { status: 401 })
    const version = sessionVersion
    const send = () => request(`${baseUrl}/api/v1/${path}`, {
      method, credentials: 'omit', cache: 'no-store', headers: { Authorization: `Bearer ${tokens.accessToken}`, ...(body ? { 'Content-Type': 'application/json' } : {}) },
      body: body ? (registration ? registrationBody(body) : JSON.stringify(body)) : undefined, signal: AbortSignal.timeout(15000),
    })
    const session = tokens
    let response = await send()
    if (version !== sessionVersion) throw new Error(messages.invalidSession)
    if (response.status === 401 && !paymentStatus) {
      if (tokens === session) await refresh(session)
      if (version !== sessionVersion) throw new Error(messages.invalidSession)
      if (!tokens) throw new Error(messages.invalidSession)
      response = await send()
    }
    if (version !== sessionVersion) throw new Error(messages.invalidSession)
    if (paymentStatus && response.ok) {
      if (response.status !== paymentStatus) throw new Error('Chưa xác định được kết quả.')
      return
    }
    if (!response.ok) {
      const problem = await response.json().catch(() => ({}))
      if (version !== sessionVersion) throw new Error(messages.invalidSession)
      const details = problem.code === 'profileClaimUnavailable' ? '' : Object.values(problem.errors || {}).flat().join(' ')
      throw Object.assign(new Error(details || (registration ? { validation: 'Kiểm tra hồ sơ, dịch vụ, thông tin liên hệ và ngày mong muốn tiêm.', permissionDenied: 'Bạn không có quyền truy cập đăng ký này.', catalogChanged: 'Giá hoặc thành phần dịch vụ đã thay đổi.', resourceNotFound: 'Hồ sơ hoặc dịch vụ không còn tồn tại.' } : { validation: 'Kiểm tra thông tin hồ sơ.', permissionDenied: 'Bạn không có quyền thao tác hồ sơ này.', duplicateIdentity: 'CCCD đã tồn tại.', profileAlreadyExists: 'Bạn đã có hồ sơ. Hãy tải lại.', profileReferenced: 'Hồ sơ đang được sử dụng, không thể xóa.' })[problem.code] || messages[problem.code] || `API trả HTTP ${response.status}`), { status: response.status, code: problem.code })
    }
    if (registration && method === 'POST' && response.status !== 201)
      throw Object.assign(new Error('API chưa xác nhận tạo đăng ký (cần HTTP 201). Hãy kiểm tra danh sách trước khi tạo lại.'), { uncertain: true })
    const result = registration ? parseRegistrationJson(await response.text()) : response.status === 204 ? null : await response.json()
    if (version !== sessionVersion) throw new Error(messages.invalidSession)
    return result
  }

  async function writePayment(id, action, body) {
    if (paymentWrites.has(id)) throw new Error('Cần đối chiếu kết quả trước khi tiếp tục.')
    const version = sessionVersion
    const online = action === 'online'
    const attempt = { status: 'sending', action }
    paymentWrites.set(id, attempt)
    attempt.done = profiles(`registrations/${id}/payment/${action}`, online ? 'POST' : 'PUT', body, false, online ? 201 : 200)
    try {
      await attempt.done
      if (version === sessionVersion) attempt.status = 'committed'
    } catch (error) {
      if (version === sessionVersion) {
        if (['paymentAlreadyRecorded', 'paymentNotRequired'].includes(error.code)) attempt.status = 'reconcile'
        else if (error.status === 409) attempt.status = 'conflict'
        else if (!error.status || error.status >= 500 || error.status === 408) attempt.status = 'unknown'
        else paymentWrites.delete(id)
      }
      throw error
    }
  }

  async function readPaymentFacts(id, load, selectPayment) {
    const version = sessionVersion
    const attempt = paymentWrites.get(id)
    if (attempt?.status === 'sending') await attempt.done.catch(() => {})
    if (version !== sessionVersion) throw new Error(messages.invalidSession)
    const response = await load()
    const detail = selectPayment(response)
    if (attempt && attempt === paymentWrites.get(id)) {
      if (detail.payment || detail.totalDue === '0' || attempt.action === 'channel' && detail.expectedChannel === 'TaiTrungTam')
        paymentWrites.delete(id)
      else if (attempt.status === 'unknown') attempt.status = 'unconfirmed'
    }
    return response
  }
  return {
    profiles,
    async findProfile(body) {
      const response = await request(`${baseUrl}/api/v1/recipients/find`, {
        method: 'POST', credentials: 'omit', cache: 'no-store', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body), signal: AbortSignal.timeout(15000),
      })
      if (!response.ok) {
        const problem = await response.json().catch(() => ({}))
        throw new Error(problem.code === 'rateLimited' ? messages.rateLimited : 'Chưa tìm được hồ sơ. Vui lòng kiểm tra thông tin và thử lại.')
      }
      return (await response.json()).matched === true
    },
    appointments: (query = '') => profiles(`appointments${query}`),
    getAppointment: id => profiles(`appointments/${id}`),
    vaccinationHistory: (query = '') => profiles(`vaccinations/history${query}`),
    registrations: (path = '', method = 'GET', body) => profiles(`registrations${path}`, method, body, true),
    vaccinationJourneys: (query = '') => profiles(`vaccination-journeys${query}`, 'GET', undefined, true),
    getVaccinationJourney: id => readPaymentFacts(id,
      () => profiles(`vaccination-journeys/${id}`, 'GET', undefined, true), journey => journey.payment),
    getRegistrationPayment: id => readPaymentFacts(id,
      () => profiles(`registrations/${id}/payment`, 'GET', undefined, true), detail => detail),
    getPaymentSettings: () => profiles('payment/settings'),
    recordOnlinePayment: (id, body) => writePayment(id, 'online', body),
    selectCenterPayment: id => writePayment(id, 'channel', { channel: 'TaiTrungTam' }),
    createPayOsPayment: (id, body) => profiles(`registrations/${id}/payment/payos`, 'POST', body),
    getPayOsPayment: id => profiles(`registrations/${id}/payment/payos`),
    reconcilePayOsPayment: id => profiles(`registrations/${id}/payment/payos/reconcile`, 'POST'),
    paymentWriteState: id => paymentWrites.get(id)?.status,
    acknowledgePaymentRetry(id) {
      if (paymentWrites.get(id)?.status === 'unconfirmed') paymentWrites.delete(id)
    },
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
      paymentWrites.clear()
      sessionVersion++
    },
    async register(email, password, confirmation) {
      if (password !== confirmation) throw new Error('Xác nhận mật khẩu không khớp.')
      if (password.length < 12 || password.length > 128) throw new Error('Mật khẩu cần 12–128 ký tự.')
      return call('register', { email, password })
    },
    requestPasswordReset: email => call('password-resets/request', { email }),
    async confirmPasswordReset(token, newPassword, confirmation) {
      if (newPassword !== confirmation) throw new Error('Xác nhận mật khẩu không khớp.')
      if (newPassword.length < 12 || newPassword.length > 128) throw new Error('Mật khẩu mới cần 12–128 ký tự.')
      await call('password-resets/confirm', { token, newPassword })
      tokens = null
      paymentWrites.clear()
      sessionVersion++
    },
    confirm: token => call('email-verifications/confirm', { token }),
    resend: email => call('email-verifications/resend', { email }),
    async login(identifier, password) {
      tokens = null
      paymentWrites.clear()
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
      paymentWrites.clear()
      sessionVersion++
    },
  }
}

export const identity = createIdentityClient((import.meta.env?.VITE_API_BASE_URL || 'http://localhost:5080').replace(/\/$/, ''))
