import { vietnamToday } from './profileForm'

export default function ProfileClaimFields({ draft, onChange, busy }) {
  const input = (name, label, options = {}) => <label className="demo-field">
    <span>{label} *</span><input {...options} required value={draft[name] || ''}
      onChange={event => onChange({ ...draft, [name]: event.target.value })} />
  </label>
  return <>
    <fieldset disabled={busy} className="profile-claim-method">
      <legend>Cách tìm hồ sơ</legend>
      {[['profileCode', 'Mã hồ sơ'], ['personalInfo', 'Thông tin cá nhân']].map(([value, label]) =>
        <label key={value}><input type="radio" name="claimMode" value={value} checked={draft.mode === value}
          onChange={() => onChange({ ...draft, mode: value })} /> {label}</label>)}
    </fieldset>
    <fieldset disabled={busy} className="profile-fields">
      {draft.mode === 'profileCode'
        ? input('profileCode', 'Mã hồ sơ', { maxLength: 16, pattern: '[Vv][Aa][Cc]-[0-9a-fA-F]{12}', placeholder: 'VAC-XXXXXXXXXXXX', title: 'Nhập VAC- và 12 ký tự từ 0–9 hoặc A–F.' })
        : <>{input('fullName', 'Họ và tên', { maxLength: 150, autoComplete: 'off' })}
          {input('dateOfBirth', 'Ngày sinh', { type: 'date', max: vietnamToday() })}</>}
      {input('phoneNumber', 'Số điện thoại', { type: 'tel', maxLength: 11, pattern: '[0-9]{10,11}', inputMode: 'numeric', title: 'Nhập số điện thoại gồm 10–11 chữ số.', autoComplete: 'tel' })}
    </fieldset>
  </>
}
