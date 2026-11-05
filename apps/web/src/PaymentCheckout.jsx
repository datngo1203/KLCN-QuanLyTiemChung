import { useEffect, useRef, useState } from 'react'
import { Building2, CreditCard, QrCode, WalletCards } from 'lucide-react'
import qrcode from 'qrcode-generator'
import { identity } from './api/identity'
import { cardNumberValid, paymentMethodLabel, registrationCode } from './paymentView'
import { contactFields, money } from './registrationForm'

const methods = [
  ['ChuyenKhoanOnline', 'Chuyển khoản / QR', QrCode],
  ['TheQuocTe', paymentMethodLabel('TheQuocTe'), CreditCard],
  ['TheNoiDia', paymentMethodLabel('TheNoiDia'), WalletCards],
  ['TaiTrungTam', 'Thanh toán tại trung tâm', Building2],
]
const numeric = { inputMode: 'numeric', autoComplete: 'off' }

function Field({ label, error, optional = false, ...props }) {
  const errorId = `payment-${props.name}-error`
  return <label className="payment-field">
    <span>{label}{optional && <small> (không bắt buộc)</small>}</span>
    <input required={!optional} aria-invalid={error ? true : undefined} aria-describedby={error ? errorId : undefined} {...props} />
    {error && <small id={errorId} className="payment-field-error" role="alert">{error}</small>}
  </label>
}

function CardGateway({ errors }) {
  const [number, setNumber] = useState('')
  function formatNumber(event) {
    const input = event.target
    const raw = input.value
    const beforeCaret = raw.slice(0, input.selectionStart).replaceAll(' ', '').length
    const digits = raw.replaceAll(' ', '')
    if (!/^[0-9]{0,19}$/.test(digits)) {
      setNumber(raw)
      return
    }
    const grouped = digits.match(/.{1,4}/g)?.join(' ') ?? ''
    const caret = beforeCaret + Math.floor(Math.max(0, beforeCaret - 1) / 4)
    input.value = grouped
    input.setSelectionRange(caret, caret)
    setNumber(grouped)
  }

  return <fieldset className="payment-gateway">
    <legend>Thanh toán bằng thẻ quốc tế</legend>
    <div className="payment-fields">
      <div className="payment-field">
        <label htmlFor="payment-card-brand">Loại thẻ</label>
        <select id="payment-card-brand" name="cardBrand" defaultValue="" required>
          <option value="" disabled>Chọn loại thẻ</option>
          <option>VISA</option><option>MASTER</option><option>JCB</option>
        </select>
      </div>
      <Field name="cardholderName" label="Tên chủ thẻ" maxLength={150} autoComplete="off" />
      <Field name="cardNumber" label="Số thẻ" {...numeric} value={number} onChange={formatNumber}
        className="payment-field-wide" maxLength={23} pattern="[0-9 ]{13,23}" placeholder="0000 0000 0000 0000" error={errors.cardNumber} />
      <fieldset className="payment-expiry">
        <legend>Ngày hết hạn (MM/YY)</legend>
        <div>
          <Field name="expiryMonth" label="Tháng" {...numeric} pattern="0[1-9]|1[0-2]" maxLength={2} placeholder="MM" error={errors.expiryMonth} />
          <Field name="expiryYear" label="Năm" {...numeric} pattern="[0-9]{2}" maxLength={2} placeholder="YY" />
        </div>
      </fieldset>
      <Field name="cvv" label="CVV/CVC" type="password" {...numeric} pattern="[0-9]{3}" maxLength={3} placeholder="3 chữ số" />
    </div>
  </fieldset>
}

function DomesticGateway({ otpStep, setOtpStep }) {
  const otp = useRef(null)
  useEffect(() => { if (otpStep) otp.current?.focus() }, [otpStep])
  return <fieldset className="payment-gateway">
    <legend>Thanh toán bằng thẻ / tài khoản nội địa</legend>
    <div className="payment-fields" hidden={otpStep}>
      <Field name="bankName" label="Ngân hàng" maxLength={100} />
      <Field name="accountHolderName" label="Tên chủ tài khoản" maxLength={150} />
      <Field name="cardOrAccountNumber" label="Số thẻ hoặc tài khoản" {...numeric} pattern="[0-9]{8,20}" maxLength={20} />
    </div>
    {otpStep && <>
      <p>Nhập mã OTP gồm 6 chữ số để xác thực.</p>
      <div className="payment-fields">
        <Field ref={otp} name="otp" label="Mã OTP" {...numeric} pattern="[0-9]{6}" maxLength={6} />
      </div>
      <button className="button outline" type="button" onClick={() => setOtpStep(false)}>Sửa thông tin tài khoản</button>
    </>}
  </fieldset>
}

function TransferGateway({ detail, onReady }) {
  const [state, setState] = useState({ loading: true })
  const [reload, setReload] = useState(0)
  useEffect(() => {
    let active = true
    identity.getPaymentSettings().then(settings => {
      if (!active) return
      const content = `VNVC ${registrationCode(detail.registrationId)}`
      const qr = qrcode(0, 'M')
      qr.addData(`VNVC|${settings.receiverAccountNumber}|${detail.totalDue}|${content}`)
      qr.make()
      setState({ settings, content, src: qr.createDataURL(4) })
      onReady(true)
    }).catch(() => { if (active) setState({ error: true }) })
    return () => { active = false }
  }, [detail.registrationId, detail.totalDue, reload, onReady])

  return <fieldset className="payment-gateway">
    <legend>Chuyển khoản / QR</legend>
    {state.loading && <p role="status">Đang tải hướng dẫn chuyển khoản…</p>}
    {state.error && <div className="registration-message error" role="alert">
      <p>Chưa tải được thông tin nhận chuyển khoản.</p>
      <button className="button outline" type="button" onClick={() => { setState({ loading: true }); setReload(value => value + 1) }}>Tải lại hướng dẫn</button>
    </div>}
    {state.settings && <>
      <div className="payment-transfer">
        <img src={state.src} alt="Mã QR chứa thông tin nhận chuyển khoản VNVC" width="172" height="172" />
        <dl className="payment-transaction">
          <div><dt>Ngân hàng nhận</dt><dd>{state.settings.receiverBankName}</dd></div>
          <div><dt>Chủ tài khoản nhận</dt><dd>{state.settings.receiverAccountName}</dd></div>
          <div><dt>Số tài khoản nhận</dt><dd>{state.settings.receiverAccountNumber}</dd></div>
          <div><dt>Số tiền</dt><dd>{money(detail.totalDue)}</dd></div>
          <div><dt>Nội dung chuyển khoản</dt><dd>{state.content}</dd></div>
        </dl>
      </div>
      <p>Đối chiếu số tiền và nội dung, sau đó nhập thông tin giao dịch để Thu ngân kiểm tra.</p>
      <div className="payment-fields">
        <Field name="sourceBankOrWallet" label="Ngân hàng hoặc ví gửi" maxLength={100} />
        <Field name="accountHolderName" label="Tên chủ tài khoản gửi" maxLength={150} />
        <Field name="sourceAccountOrWalletId" label="Tài khoản hoặc định danh ví gửi" maxLength={100}
          pattern={String.raw`[A-Za-z0-9+_.@\-]+`} />
        <Field name="transferReference" label="Mã tham chiếu chuyển khoản" maxLength={100} />
      </div>
    </>}
  </fieldset>
}

export default function PaymentCheckout({ detail, summary, disabled, sending, onPayment, onCenter, message }) {
  const [method, setMethod] = useState('')
  const [otpStep, setOtpStep] = useState(false)
  const [transferReady, setTransferReady] = useState(false)
  const [errors, setErrors] = useState({})
  const atCenter = method === 'TaiTrungTam'
  const isDomestic = method === 'TheNoiDia'
  const isTransfer = method === 'ChuyenKhoanOnline'
  const action = isDomestic && !otpStep ? 'Tiếp tục xác thực' : isTransfer ? 'Tôi đã chuyển khoản' : 'Thanh toán'

  function submit(event) {
    event.preventDefault()
    if (disabled || !method || atCenter || isTransfer && !transferReady) return
    const form = event.currentTarget
    const fields = new FormData(form)
    const value = name => fields.get(name)?.trim()
    const payer = Object.fromEntries(contactFields.map(([name]) => [name, value(name)]))
    const request = { payer, method, termsAccepted: fields.has('terms') }
    const problems = {}
    if (method === 'TheQuocTe') {
      const card = {
        cardBrand: value('cardBrand'), cardholderName: value('cardholderName'), cardNumber: value('cardNumber'),
        expiryMonth: Number(value('expiryMonth')), expiryYear: 2000 + Number(value('expiryYear')), cvv: value('cvv'),
      }
      const local = new Date(Date.now() + 7 * 3600 * 1000)
      if (!cardNumberValid(card.cardBrand, card.cardNumber)) problems.cardNumber = 'Kiểm tra số thẻ theo loại thẻ đã chọn.'
      if (card.expiryYear * 12 + card.expiryMonth < local.getUTCFullYear() * 12 + local.getUTCMonth() + 1)
        problems.expiryMonth = 'Thẻ đã hết hạn.'
      request.internationalCard = card
    } else if (isDomestic) {
      if (!otpStep) { setOtpStep(true); return }
      request.domestic = Object.fromEntries(['bankName', 'accountHolderName', 'cardOrAccountNumber', 'otp'].map(name => [name, value(name)]))
    } else {
      request.transfer = Object.fromEntries(['sourceBankOrWallet', 'accountHolderName', 'sourceAccountOrWalletId', 'transferReference'].map(name => [name, value(name)]))
      if (!/[A-Za-z0-9]/.test(request.transfer.sourceAccountOrWalletId))
        problems.sourceAccountOrWalletId = 'Nhập ít nhất một chữ cái hoặc chữ số.'
    }
    setErrors(problems)
    const invalidField = Object.keys(problems)[0]
    if (invalidField) { form.elements.namedItem(invalidField)?.focus(); return }
    onPayment(request)
  }

  return <form className="payment-checkout" onSubmit={submit} autoComplete="off">
    <fieldset className="payment-checkout-inputs" disabled={disabled}>
      <section aria-labelledby="payer-title">
        <h3 id="payer-title">Thông tin người thanh toán</h3>
        <p>Thông tin chỉnh sửa tại đây sẽ được lưu trên giao dịch này.</p>
        <div className="payment-fields">
          {contactFields.map(([name, label, maxLength]) => <Field key={name} name={name} label={name === 'fullName' ? 'Họ và tên' : label} maxLength={maxLength}
            defaultValue={detail.registrationContact?.[name] ?? ''} optional={name === 'email'} disabled={atCenter}
            {...(name === 'phoneNumber' ? { type: 'tel', inputMode: 'numeric', pattern: '[0-9]{10,11}' } : name === 'email' ? { type: 'email' } : {})} />)}
        </div>
      </section>
      <fieldset className="payment-methods">
        <legend>Phương thức thanh toán</legend>
        {methods.map(([code, label, Icon]) => <label className="payment-method" key={code}>
          <input type="radio" name="method" value={code} checked={method === code} onChange={() => {
            setMethod(code); setOtpStep(false); setTransferReady(false); setErrors({})
          }} />
          <Icon aria-hidden="true" size={20} /><span>{label}</span>
        </label>)}
      </fieldset>
      {method === 'TheQuocTe' && <CardGateway errors={errors} />}
      {isDomestic && <DomesticGateway otpStep={otpStep} setOtpStep={setOtpStep} />}
      {isTransfer && <TransferGateway detail={detail} onReady={setTransferReady} />}
      {errors.sourceAccountOrWalletId && isTransfer && <p role="alert" className="payment-field-error">{errors.sourceAccountOrWalletId}</p>}
      {atCenter && <p>Đến quầy Thu ngân và cung cấp mã đăng ký để thanh toán.</p>}
      {method && !atCenter && (!isDomestic || otpStep) && <label key={method} className="payment-terms">
        <input name="terms" type="checkbox" required /> Tôi xác nhận thông tin thanh toán và đồng ý thực hiện giao dịch.
      </label>}
    </fieldset>
    {summary}
    <div className="payment-checkout-actions">
      {message && <p className="registration-message error" role="alert">{message}</p>}
      {atCenter ? <button className="button primary" type="button" disabled={disabled} onClick={onCenter}>Thanh toán tại trung tâm</button>
        : <button className="button primary" disabled={disabled || !method || isTransfer && !transferReady}>{sending ? 'Đang xử lý…' : action}</button>}
    </div>
  </form>
}
