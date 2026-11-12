import { useState, useEffect, Fragment } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import AuthShell from '../AuthShell';
import { apiPost } from '../../../lib/api';
import { useAuth } from '../../../contexts/useAuth';
import './Signup.css';
import { checkPassword } from '../../../utils/passwordPolicy';
import PasswordRequirements from '../../../components/auth/PasswordRequirements';

function EyeIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
      <circle cx="12" cy="12" r="3"/>
    </svg>
  );
}

function EyeOffIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24"/>
      <line x1="1" y1="1" x2="23" y2="23"/>
    </svg>
  );
}

function SpinnerIcon() {
  return <span className="btn-spinner" aria-hidden="true" />;
}

const STEP_LABELS = ['Details', 'Verify Email', 'Set Password'];

function StepProgress({ current }) {
  return (
    <div className="signup-steps" aria-label="Registration progress">
      {STEP_LABELS.map((label, i) => {
        const n = i + 1;
        const done = n < current;
        const active = n === current;
        return (
          <Fragment key={label}>
            {i > 0 && (
              <div className={`signup-step-line${done ? ' signup-step-line--done' : ''}`} />
            )}
            <div
              className={[
                'signup-step',
                done ? 'signup-step--done' : '',
                active ? 'signup-step--active' : '',
              ].filter(Boolean).join(' ')}
              aria-current={active ? 'step' : undefined}
            >
              <div className="signup-step__dot" aria-hidden="true">
                {done ? '✓' : n}
              </div>
              <div className="signup-step__label">{label}</div>
            </div>
          </Fragment>
        );
      })}
    </div>
  );
}

// Friendly messages keyed by backend error code — ONE place for all codes
const CODE_MESSAGES = {
  InvalidTin:                     'Please enter a valid 9-digit company TIN.',
  OrganizationNotRegistered:      "We couldn't find a registered company with this TIN. Please check the number and try again.",
  CompanyNotActivated:            "This company's account hasn't been activated yet. Please ask your company administrator to complete activation.",
  CompanyPendingEregVerification: "This company's account hasn't been activated yet. Please ask your company administrator to complete activation.",
  CompanySetupIncomplete:         "This company's account setup is incomplete. Please contact your company administrator or CDNS support.",
  EmailBelongsToMaster:           "This email is already registered as the company's administrator account. Please log in instead.",
  AccountExists:                  'An account with this email already exists.',
  InvalidName:        'Please enter a valid name (letters, spaces, hyphens, apostrophes, and dots only; max 50 characters).',
  InvalidEmail:       'Please enter a valid email address (max 254 characters).',
  InvalidMobile:      'Please enter a valid mobile number (8–15 digits, optional leading +).',
  InvalidNic:         'Please enter a valid Sri Lankan NIC (9 digits + V/X, or 12 digits).',
  InvalidDesignation: 'Please enter a designation (max 100 characters).',
  TermsNotAccepted:   'You must agree to the Terms and Conditions to continue.',
};

// Fallback field mapping for codes that don't include a field from the server
const CODE_FIELD = {
  InvalidTin:                     'tin',
  OrganizationNotRegistered:      'tin',
  CompanyNotActivated:            'tin',
  CompanyPendingEregVerification: 'tin',
  CompanySetupIncomplete:         'tin',
  EmailBelongsToMaster:           'email',
  AccountExists:                  'email',
};

const REQUIRED_FIELDS = ['tin', 'firstName', 'lastName', 'email', 'mobile', 'nic', 'designation', 'agreeToTerms'];

// Mirrors server ProfileValidator rules exactly (source of truth remains the server)
function validateField(name, value) {
  switch (name) {
    case 'tin': {
      const v = (value || '').trim();
      if (!v) return 'Company TIN is required.';
      if (v.length !== 9) return 'TIN must be exactly 9 digits.';
      return null;
    }
    case 'firstName':
    case 'lastName': {
      const v = (value || '').trim();
      if (!v || v.length > 50 || !/^[A-Za-z\s\-'.]+$/.test(v))
        return CODE_MESSAGES.InvalidName;
      return null;
    }
    case 'email': {
      const v = (value || '').trim();
      if (!v || v.length > 254 || !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(v))
        return CODE_MESSAGES.InvalidEmail;
      return null;
    }
    case 'mobile': {
      const mob = (value || '').replace(/\s/g, '');
      if (!/^\+?\d{8,15}$/.test(mob)) return CODE_MESSAGES.InvalidMobile;
      return null;
    }
    case 'nic': {
      const v = (value || '').trim();
      if (!/^\d{9}[VvXx]$/i.test(v) && !/^\d{12}$/.test(v)) return CODE_MESSAGES.InvalidNic;
      return null;
    }
    case 'designation': {
      const v = (value || '').trim();
      if (!v || v.length > 100) return CODE_MESSAGES.InvalidDesignation;
      return null;
    }
    case 'agreeToTerms':
      return value ? null : CODE_MESSAGES.TermsNotAccepted;
    default:
      return null;
  }
}

export default function Signup() {
  const navigate = useNavigate();
  const { refreshUser } = useAuth();

  const [formData, setFormData] = useState({
    tin: '',
    accountType: 'CHA Agent (Declarant)',
    firstName: '',
    lastName: '',
    email: '',
    mobile: '',
    nic: '',
    designation: '',
    agreeToTerms: false,
  });

  const [step, setStep] = useState('SIGNUP');
  const [tempToken, setTempToken] = useState('');
  const [otpInput, setOtpInput] = useState('');
  const [passwordInput, setPasswordInput] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [signupPortal, setSignupPortal] = useState('member');

  const [loading, setLoading] = useState(false);
  const [touched, setTouched] = useState({});
  const [fieldErrors, setFieldErrors] = useState({});
  // serverErr = { code, message, field } | null
  const [serverErr, setServerErr] = useState(null);

  // Focus the errored field (or banner) after server error state settles
  useEffect(() => {
    if (!serverErr) return;
    setTimeout(() => {
      if (serverErr.field) {
        document.getElementById(`signup-${serverErr.field}`)?.focus();
      } else {
        document.getElementById('signup-error-banner')?.focus();
      }
    }, 50);
  }, [serverErr]);

  function getFieldError(name) {
    if (serverErr?.field === name) return serverErr.message;
    if (touched[name]) return fieldErrors[name] || null;
    return null;
  }

  function inputClass(name) {
    return getFieldError(name) ? 'input-error' : '';
  }

  function handleChange(e) {
    const { name, value, type, checked } = e.target;
    const newValue = type === 'checkbox' ? checked : value;
    setFormData(prev => ({ ...prev, [name]: newValue }));
    if (serverErr?.field === name) setServerErr(null);
    if (touched[name]) {
      setFieldErrors(prev => ({ ...prev, [name]: validateField(name, newValue) }));
    }
  }

  function handleTinChange(e) {
    const digits = e.target.value.replace(/\D/g, '').slice(0, 9);
    setFormData(prev => ({ ...prev, tin: digits }));
    if (serverErr?.field === 'tin') setServerErr(null);
    if (touched.tin) {
      setFieldErrors(prev => ({ ...prev, tin: validateField('tin', digits) }));
    }
  }

  function handleBlur(name) {
    setTouched(prev => ({ ...prev, [name]: true }));
    setFieldErrors(prev => ({ ...prev, [name]: validateField(name, formData[name]) }));
  }

  function validateAll() {
    const errors = {};
    REQUIRED_FIELDS.forEach(name => {
      const err = validateField(name, formData[name]);
      if (err) errors[name] = err;
    });
    setFieldErrors(errors);
    setTouched(Object.fromEntries(REQUIRED_FIELDS.map(f => [f, true])));
    return Object.keys(errors).length === 0;
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setServerErr(null);

    if (!validateAll()) {
      const first = REQUIRED_FIELDS.find(name => !!validateField(name, formData[name]));
      if (first) setTimeout(() => document.getElementById(`signup-${first}`)?.focus(), 0);
      return;
    }

    setLoading(true);
    try {
      const data = await apiPost('/api/signup/register', {
        tin: formData.tin,
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        mobile: formData.mobile,
        nic: formData.nic,
        designation: formData.designation,
        agreeToTerms: formData.agreeToTerms,
      });
      setTempToken(data.tempSignupToken);
      setStep('OTP');
    } catch (err) {
      const code = err.code;
      const field = err.field || CODE_FIELD[code] || null;
      const message = CODE_MESSAGES[code] || err.message || 'Something went wrong. Please try again.';
      setServerErr({ code, message, field });
    } finally {
      setLoading(false);
    }
  }

  async function handleVerifyOtp() {
    if (!otpInput.trim()) {
      setServerErr({ code: null, message: 'Please enter the verification code.', field: null });
      return;
    }
    setLoading(true);
    setServerErr(null);
    try {
      await apiPost('/api/signup/verify-otp', {
        tempSignupToken: tempToken,
        otp: otpInput,
      });
      setStep('PASSWORD');
    } catch {
      setServerErr({ code: null, message: 'Invalid or expired code. Please try again.', field: null });
    } finally {
      setLoading(false);
    }
  }

  async function handleSetPassword() {
    if (!tempToken) {
      setServerErr({ code: null, message: 'Session expired. Please start over.', field: null });
      setStep('SIGNUP');
      return;
    }
    if (!checkPassword(passwordInput).valid) {
      setServerErr({ code: null, message: 'Password does not meet the requirements below.', field: null });
      return;
    }
    if (passwordInput !== confirmPassword) {
      setServerErr({ code: null, message: 'Passwords do not match.', field: null });
      return;
    }
    setLoading(true);
    setServerErr(null);
    try {
      const data = await apiPost('/api/signup/set-password', {
        tempSignupToken: tempToken,
        password: passwordInput,
      });
      const portal = data?.portal?.toLowerCase() ?? 'member';
      setSignupPortal(portal);
      await refreshUser();
      setStep('SUCCESS');
    } catch (err) {
      const message = err.code === 'WeakPassword'
        ? 'Password does not meet the requirements. Please choose a stronger password.'
        : 'Failed to set password. Please try again.';
      setServerErr({ code: err.code, message, field: null });
    } finally {
      setLoading(false);
    }
  }

  // ── Render ──────────────────────────────────────────────────────────────

  return (
    <AuthShell>
      <div className="signup-nav-row">
        <Link to="/login" className="portal-back-link">← Back to Login</Link>
      </div>

      {/* SUCCESS */}
      {step === 'SUCCESS' && (
        <div className="form-wrapper form-wrapper--centered">
          <div className="header">
            <div className="logo-placeholder">SLC</div>
            <h2>Sri Lanka Customs</h2>
            <h1 className="success-title">Account Created!</h1>
            <p>Your account has been set up successfully. You are now ready to use the Document Notification System.</p>
          </div>
          <button className="submit-btn" onClick={() => navigate(`/${signupPortal}/dashboard`)}>
            Go to Dashboard
          </button>
        </div>
      )}

      {/* OTP */}
      {step === 'OTP' && (
        <div className="form-wrapper form-wrapper--centered">
          <StepProgress current={2} />
          <div className="header">
            <h2>Sri Lanka Customs</h2>
            <h1>Verify Your Email</h1>
            <p>We sent a 6-digit code to your email address.</p>
          </div>

          {serverErr && (
            <div id="signup-error-banner" className="error-banner" role="alert" tabIndex={-1}>
              {serverErr.message}
            </div>
          )}

          <div className="input-group" style={{ marginTop: 20, textAlign: 'left' }}>
            <label htmlFor="signup-otp">Verification Code</label>
            <input
              id="signup-otp"
              type="text"
              inputMode="numeric"
              className="otp-input"
              value={otpInput}
              onChange={e => setOtpInput(e.target.value.replace(/\D/g, ''))}
              placeholder="000000"
              maxLength={6}
              autoComplete="one-time-code"
            />
          </div>

          <button className="submit-btn" onClick={handleVerifyOtp} disabled={loading}>
            {loading ? <><SpinnerIcon /> Verifying…</> : 'Verify Code'}
          </button>
        </div>
      )}

      {/* PASSWORD */}
      {step === 'PASSWORD' && (
        <div className="form-wrapper form-wrapper--centered">
          <StepProgress current={3} />
          <div className="header">
            <div className="logo-placeholder">SLC</div>
            <h2>Sri Lanka Customs</h2>
            <h1>Set Your Password</h1>
            <p>Create a secure password for your new account.</p>
          </div>

          {serverErr && (
            <div id="signup-error-banner" className="error-banner" role="alert" tabIndex={-1}>
              {serverErr.message}
            </div>
          )}

          <div className="input-group" style={{ marginTop: 20, textAlign: 'left' }}>
            <label htmlFor="signup-password">New Password *</label>
            <div className="pw-wrap">
              <input
                id="signup-password"
                type={showPassword ? 'text' : 'password'}
                value={passwordInput}
                onChange={e => setPasswordInput(e.target.value)}
                placeholder="At least 8 characters"
                autoComplete="new-password"
              />
              <button
                type="button"
                className="pw-toggle"
                onClick={() => setShowPassword(v => !v)}
                aria-label={showPassword ? 'Hide password' : 'Show password'}
              >
                {showPassword ? <EyeOffIcon /> : <EyeIcon />}
              </button>
            </div>
            <PasswordRequirements value={passwordInput} />
          </div>

          <div className="input-group" style={{ marginTop: 16, textAlign: 'left' }}>
            <label htmlFor="signup-confirm-password">Confirm Password *</label>
            <div className="pw-wrap">
              <input
                id="signup-confirm-password"
                type={showConfirmPassword ? 'text' : 'password'}
                value={confirmPassword}
                onChange={e => setConfirmPassword(e.target.value)}
                placeholder="Re-enter your password"
                autoComplete="new-password"
              />
              <button
                type="button"
                className="pw-toggle"
                onClick={() => setShowConfirmPassword(v => !v)}
                aria-label={showConfirmPassword ? 'Hide password' : 'Show password'}
              >
                {showConfirmPassword ? <EyeOffIcon /> : <EyeIcon />}
              </button>
            </div>
            {confirmPassword.length > 0 && confirmPassword !== passwordInput && (
              <p className="field-error">Passwords do not match.</p>
            )}
          </div>

          <button className="submit-btn" onClick={handleSetPassword} disabled={loading}>
            {loading ? <><SpinnerIcon /> Creating account…</> : 'Complete Signup'}
          </button>
        </div>
      )}

      {/* SIGNUP FORM */}
      {step === 'SIGNUP' && (
        <div className="form-wrapper">
          <StepProgress current={1} />
          <div className="header">
            <div className="logo-placeholder">SLC</div>
            <h2>Sri Lanka Customs</h2>
            <h1>Document Notification System</h1>
            <p>Create your account</p>
          </div>

          {serverErr && (
            <div id="signup-error-banner" className="error-banner" role="alert" tabIndex={-1}>
              {serverErr.code === 'AccountExists' ? (
                <>
                  An account with this email already exists.{' '}
                  <Link to="/login">Already have an account? Log in</Link>
                </>
              ) : (
                serverErr.message
              )}
            </div>
          )}

          <form onSubmit={handleSubmit} className="signup-form" noValidate>

            {/* Company TIN — full width */}
            <div className="input-group signup-input-full">
              <label htmlFor="signup-tin">Company TIN (9 digits) *</label>
              <input
                id="signup-tin"
                name="tin"
                type="text"
                inputMode="numeric"
                maxLength={9}
                value={formData.tin}
                onChange={handleTinChange}
                onBlur={() => handleBlur('tin')}
                aria-invalid={getFieldError('tin') ? 'true' : 'false'}
                aria-describedby={getFieldError('tin') ? 'signup-tin-error' : 'signup-tin-hint'}
                className={inputClass('tin')}
              />
              {!getFieldError('tin') && (
                <p id="signup-tin-hint" className="field-hint">
                  Enter the TIN of the company you will work with.
                </p>
              )}
              {getFieldError('tin') && (
                <p id="signup-tin-error" className="field-error" role="alert">
                  {getFieldError('tin')}
                </p>
              )}
            </div>

            {/* Personal + contact details — 2-col grid */}
            <div className="input-grid" style={{ marginTop: 16 }}>

              <div className="input-group">
                <label htmlFor="signup-firstName">First Name *</label>
                <input
                  id="signup-firstName"
                  name="firstName"
                  type="text"
                  value={formData.firstName}
                  onChange={handleChange}
                  onBlur={() => handleBlur('firstName')}
                  aria-invalid={getFieldError('firstName') ? 'true' : 'false'}
                  aria-describedby={getFieldError('firstName') ? 'signup-firstName-error' : undefined}
                  className={inputClass('firstName')}
                  autoComplete="given-name"
                />
                {getFieldError('firstName') && (
                  <p id="signup-firstName-error" className="field-error" role="alert">
                    {getFieldError('firstName')}
                  </p>
                )}
              </div>

              <div className="input-group">
                <label htmlFor="signup-lastName">Last Name *</label>
                <input
                  id="signup-lastName"
                  name="lastName"
                  type="text"
                  value={formData.lastName}
                  onChange={handleChange}
                  onBlur={() => handleBlur('lastName')}
                  aria-invalid={getFieldError('lastName') ? 'true' : 'false'}
                  aria-describedby={getFieldError('lastName') ? 'signup-lastName-error' : undefined}
                  className={inputClass('lastName')}
                  autoComplete="family-name"
                />
                {getFieldError('lastName') && (
                  <p id="signup-lastName-error" className="field-error" role="alert">
                    {getFieldError('lastName')}
                  </p>
                )}
              </div>

              <div className="input-group">
                <label htmlFor="signup-email">Email Address *</label>
                <input
                  id="signup-email"
                  name="email"
                  type="email"
                  value={formData.email}
                  onChange={handleChange}
                  onBlur={() => handleBlur('email')}
                  aria-invalid={getFieldError('email') ? 'true' : 'false'}
                  aria-describedby={getFieldError('email') ? 'signup-email-error' : undefined}
                  className={inputClass('email')}
                  autoComplete="email"
                />
                {getFieldError('email') && (
                  <p id="signup-email-error" className="field-error" role="alert">
                    {getFieldError('email')}
                  </p>
                )}
              </div>

              <div className="input-group">
                <label htmlFor="signup-mobile">Mobile Number *</label>
                <input
                  id="signup-mobile"
                  name="mobile"
                  type="tel"
                  value={formData.mobile}
                  onChange={handleChange}
                  onBlur={() => handleBlur('mobile')}
                  aria-invalid={getFieldError('mobile') ? 'true' : 'false'}
                  aria-describedby={getFieldError('mobile') ? 'signup-mobile-error' : undefined}
                  className={inputClass('mobile')}
                  autoComplete="tel"
                />
                {getFieldError('mobile') && (
                  <p id="signup-mobile-error" className="field-error" role="alert">
                    {getFieldError('mobile')}
                  </p>
                )}
              </div>

              <div className="input-group">
                <label htmlFor="signup-nic">NIC *</label>
                <input
                  id="signup-nic"
                  name="nic"
                  type="text"
                  value={formData.nic}
                  onChange={handleChange}
                  onBlur={() => handleBlur('nic')}
                  aria-invalid={getFieldError('nic') ? 'true' : 'false'}
                  aria-describedby={getFieldError('nic') ? 'signup-nic-error' : undefined}
                  className={inputClass('nic')}
                />
                {getFieldError('nic') && (
                  <p id="signup-nic-error" className="field-error" role="alert">
                    {getFieldError('nic')}
                  </p>
                )}
              </div>

              <div className="input-group">
                <label htmlFor="signup-designation">Designation *</label>
                <input
                  id="signup-designation"
                  name="designation"
                  type="text"
                  value={formData.designation}
                  onChange={handleChange}
                  onBlur={() => handleBlur('designation')}
                  aria-invalid={getFieldError('designation') ? 'true' : 'false'}
                  aria-describedby={getFieldError('designation') ? 'signup-designation-error' : undefined}
                  className={inputClass('designation')}
                />
                {getFieldError('designation') && (
                  <p id="signup-designation-error" className="field-error" role="alert">
                    {getFieldError('designation')}
                  </p>
                )}
              </div>

            </div>

            {/* Account type — full width */}
            <div className="input-group signup-input-full" style={{ marginTop: 16 }}>
              <label htmlFor="signup-accountType">Account Type</label>
              <select
                id="signup-accountType"
                name="accountType"
                value={formData.accountType}
                onChange={handleChange}
              >
                <option>CHA Agent (Declarant)</option>
                <option>Importer</option>
                <option>Exporter</option>
              </select>
            </div>

            {/* Terms and Conditions — full width */}
            <div className="input-group signup-input-full" style={{ marginTop: 16 }}>
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  name="agreeToTerms"
                  checked={formData.agreeToTerms}
                  onChange={handleChange}
                  onBlur={() => handleBlur('agreeToTerms')}
                  aria-invalid={getFieldError('agreeToTerms') ? 'true' : 'false'}
                />
                {' '}I agree to the{' '}
                <a href="#" onClick={e => e.preventDefault()}>Terms and Conditions</a>
              </label>
              {getFieldError('agreeToTerms') && (
                <p className="field-error" role="alert">{getFieldError('agreeToTerms')}</p>
              )}
            </div>

            <button type="submit" disabled={loading} className="submit-btn">
              {loading ? <><SpinnerIcon /> Creating account…</> : 'Create Account'}
            </button>
          </form>

          <p className="login-link">
            Already have an account? <Link to="/login">Log in</Link>
          </p>
        </div>
      )}
    </AuthShell>
  );
}
