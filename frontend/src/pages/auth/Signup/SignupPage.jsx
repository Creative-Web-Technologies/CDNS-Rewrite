import { useState, useEffect, useRef } from 'react';
import { Link } from 'react-router-dom';
import { motion } from 'motion/react';
import { AlertCircle, ArrowLeft, ArrowRight, Check, Eye, EyeOff, WifiOff } from 'lucide-react';
import AuthShell, { AuthLogoRow } from '../AuthShell';
import { apiPost } from '../../../lib/api';
import { useAuth } from '../../../contexts/useAuth';
import { checkPassword } from '../../../utils/passwordPolicy';
import PasswordRequirements from '../../../components/auth/PasswordRequirements';
import './Signup.css';

function Rise({ delay = 0, children }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 14 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.55, ease: [0.2, 0.7, 0.2, 1], delay }}
    >
      {children}
    </motion.div>
  );
}

const STEPS = [
  { n: 1, title: 'Company TIN',   sub: 'Confirm your organisation' },
  { n: 2, title: 'Your details',  sub: 'Name, contact and role' },
  { n: 3, title: 'Verify email',  sub: '6-digit code' },
  { n: 4, title: 'Set password',  sub: 'Secure your account' },
];

const STEP_INDEX = { TIN: 1, DETAILS: 2, OTP: 3, PASSWORD: 4 };

function SignupStepper({ step }) {
  // On SUCCESS, treat all 4 as done (#11)
  const active = step === 'SUCCESS' ? 5 : (STEP_INDEX[step] ?? 0);
  return (
    <ol className="signup-stepper">
      {STEPS.map(s => {
        const done = s.n < active;
        const isActive = s.n === active;
        return (
          <li key={s.n} className={`ss-item${done ? ' ss-item--done' : ''}${isActive ? ' ss-item--active' : ''}`}>
            <span className={`ss-circle${isActive ? ' cd-pulse' : ''}`} aria-hidden="true">
              {done ? <Check size={16} strokeWidth={2.5} color="#fff" /> : s.n}
            </span>
            <span className="ss-text">
              <span className="ss-title">{s.title}</span>
              <span className="ss-sub">{s.sub}</span>
            </span>
          </li>
        );
      })}
    </ol>
  );
}

function StepProgressBar({ step }) {
  const n = STEP_INDEX[step] ?? 0;
  return (
    <div
      className="signup-progress"
      role="progressbar"
      aria-valuemin={0}
      aria-valuemax={4}
      aria-valuenow={n}
    >
      {[1, 2, 3, 4].map(i => (
        <div key={i} className={`sp-seg${i <= n ? ' sp-seg--filled' : ''}`} />
      ))}
    </div>
  );
}

const CODE_MESSAGES = {
  InvalidTin:                     'Please enter a valid 9-digit company TIN.',
  OrganizationNotRegistered:      null,
  CompanyNotActivated:            null,
  CompanyPendingEregVerification: null,
  CompanySetupIncomplete:         null,
  EmailBelongsToMaster:           'This email is already registered as the company administrator. Please sign in instead.',
  AccountExists:                  'An account with this email already exists.',
  InvalidName:        'Please enter a valid name (letters, spaces, hyphens, apostrophes, and dots only; max 50 characters).',
  InvalidEmail:       'Please enter a valid email address.',
  InvalidMobile:      'Please enter a valid mobile number (8–15 digits).',
  InvalidNic:         'Please enter a valid NIC (9 digits + V/X, or 12 digits).',
  InvalidDesignation: 'Please enter a designation (max 100 characters).',
  TermsNotAccepted:   'Accept the terms to continue.',
};

function validateField(name, value) {
  switch (name) {
    case 'tin': {
      const v = (value || '').trim();
      if (!v || v.length !== 9) return 'Enter the 9-digit company TIN.';
      return null;
    }
    case 'firstName':
    case 'lastName': {
      const v = (value || '').trim();
      if (!v || v.length > 50 || !/^[A-Za-z\s\-'.]+$/.test(v)) return CODE_MESSAGES.InvalidName;
      return null;
    }
    case 'email': {
      const v = (value || '').trim();
      if (!v || v.length > 254 || !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(v)) return CODE_MESSAGES.InvalidEmail;
      return null;
    }
    case 'mobile': {
      const mob = (value || '').replace(/\s/g, '');
      if (!/^\+?\d{8,15}$/.test(mob)) return CODE_MESSAGES.InvalidMobile;
      return null;
    }
    case 'nic': {
      const v = (value || '').trim().toUpperCase();
      if (!/^\d{9}[VX]$/.test(v) && !/^\d{12}$/.test(v)) return CODE_MESSAGES.InvalidNic;
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

const DETAIL_FIELDS = ['firstName', 'lastName', 'email', 'mobile', 'nic', 'designation', 'agreeToTerms'];

const TIN_WARNING_CODES = new Set([
  'OrganizationNotRegistered',
  'CompanyNotActivated',
  'CompanyPendingEregVerification',
  'CompanySetupIncomplete',
]);

function tinWarning(code, backendMessage) {
  if (code === 'OrganizationNotRegistered') {
    return {
      title: 'Organisation not registered yet',
      body: 'We could not find a company with this TIN on CDNS. Check the number or contact Sri Lanka Customs.',
    };
  }
  if (code === 'CompanyNotActivated') {
    return {
      title: 'Master account not active yet',
      body: 'Your company Master user must sign in with eReg first. After that, members can sign up.',
    };
  }
  // CompanyPendingEregVerification and CompanySetupIncomplete use the backend message
  return {
    title: 'Company registration incomplete',
    body: backendMessage || 'Please contact Sri Lanka Customs to complete your company setup.',
  };
}

// ── Right panel step header (defined outside render to avoid recreating #12) ──
function StepHeader({ step, onBack }) {
  if (step === 'SUCCESS') return null;
  const n = STEP_INDEX[step] ?? 0;
  return (
    <div className="signup-right-header">
      <div className="signup-right-meta">
        <span className="signup-step-label">Step {n} of 4</span>
        {n > 1 && (
          <button type="button" className="auth-link-btn" onClick={onBack}>
            <ArrowLeft size={16} />
            Back
          </button>
        )}
      </div>
      <StepProgressBar step={step} />
    </div>
  );
}

// ── Main component ──────────────────────────────────────────────────────────
export default function Signup() {
  const { refreshUser } = useAuth();

  const [step, setStep] = useState('TIN');
  const [tempToken, setTempToken] = useState('');

  const [formData, setFormData] = useState({
    tin: '',
    firstName: '',
    lastName: '',
    email: '',
    mobile: '',
    nic: '',
    designation: '',
    agreeToTerms: false,
  });
  // TODO(accountType): Account type is determined by the backend from company
  // eReg data, not sent by the signup form. See docs/auth-ui-audit.md §3 G1.
  // Hide this dropdown until a backend AccountType field is added to RegisterRequest.

  const [otpInput, setOtpInput] = useState('');
  // 30-second resend cooldown with countdown (#20)
  const [resendCooldown, setResendCooldown] = useState(0);
  const resendTimerRef = useRef(null);

  const [passwordInput, setPasswordInput] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  const [loading, setLoading] = useState(false);
  const [touched, setTouched] = useState({});
  const [fieldErrors, setFieldErrors] = useState({});
  const [serverErr, setServerErr] = useState(null);
  const [tinWarningData, setTinWarningData] = useState(null);

  useEffect(() => {
    // Clear resend timer on unmount
    return () => { if (resendTimerRef.current) clearInterval(resendTimerRef.current); };
  }, []);

  useEffect(() => {
    if (!serverErr) return;
    const timer = setTimeout(() => {
      if (serverErr.field) {
        document.getElementById(`signup-${serverErr.field}`)?.focus();
      } else {
        document.getElementById('signup-err-banner')?.focus();
      }
    }, 50);
    return () => clearTimeout(timer);
  }, [serverErr]);

  function getFieldError(name) {
    if (serverErr?.field === name) return serverErr.message;
    if (touched[name]) return fieldErrors[name] || null;
    return null;
  }

  function handleChange(e) {
    const { name, value, type, checked } = e.target;
    const newValue = type === 'checkbox' ? checked : value;
    setFormData(prev => ({ ...prev, [name]: newValue }));
    if (serverErr?.field === name) setServerErr(null);
    if (touched[name]) setFieldErrors(prev => ({ ...prev, [name]: validateField(name, newValue) }));
  }

  function handleNicChange(e) {
    const v = e.target.value.replace(/[^0-9VvXx]/g, '').slice(0, 12).toUpperCase();
    setFormData(prev => ({ ...prev, nic: v }));
    if (touched.nic) setFieldErrors(prev => ({ ...prev, nic: validateField('nic', v) }));
  }

  function handleBlur(name) {
    setTouched(prev => ({ ...prev, [name]: true }));
    setFieldErrors(prev => ({ ...prev, [name]: validateField(name, formData[name]) }));
  }

  function handleBack() {
    const n = STEP_INDEX[step] ?? 0;
    const prev = ['TIN', 'TIN', 'DETAILS', 'OTP', 'PASSWORD'][n - 1];
    setServerErr(null);
    setStep(prev);
  }

  function netErrMessage(err) {
    if (!err.status || err.status >= 500) {
      return { code: null, message: 'Something went wrong. Check your connection and try again.', field: null };
    }
    return null;
  }

  // ── Step 1: Verify TIN ──
  async function handleVerifyTin(e) {
    e.preventDefault();
    const tinErr = validateField('tin', formData.tin);
    if (tinErr) {
      setFieldErrors(prev => ({ ...prev, tin: tinErr }));
      setTouched(prev => ({ ...prev, tin: true }));
      return;
    }
    setTinWarningData(null);
    setServerErr(null);
    setLoading(true);
    try {
      await apiPost('/api/signup/verify-tin', { tin: formData.tin });
      setStep('DETAILS');
    } catch (err) {
      const code = err.code || '';
      const net = netErrMessage(err);
      if (net) {
        setServerErr(net);
      } else if (TIN_WARNING_CODES.has(code)) {
        setTinWarningData(tinWarning(code, err.message));
      } else {
        // Fall back to backend message, never the generic "valid 9-digit" error
        setFieldErrors(prev => ({ ...prev, tin: err.message || CODE_MESSAGES[code] || 'Please enter a valid 9-digit company TIN.' }));
        setTouched(prev => ({ ...prev, tin: true }));
      }
    } finally {
      setLoading(false);
    }
  }

  // ── Step 2: Register ──
  async function handleRegister(e) {
    e.preventDefault();
    setServerErr(null);

    const errors = {};
    DETAIL_FIELDS.forEach(name => {
      const err = validateField(name, formData[name]);
      if (err) errors[name] = err;
    });
    setFieldErrors(errors);
    setTouched(Object.fromEntries(DETAIL_FIELDS.map(f => [f, true])));
    if (Object.keys(errors).length > 0) {
      const first = DETAIL_FIELDS.find(n => !!errors[n]);
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
      const net = netErrMessage(err);
      if (net) { setServerErr(net); return; }

      const code = err.code;
      const message = CODE_MESSAGES[code] || err.message || 'Something went wrong. Please try again.';
      // Map email-related codes to field highlight (#26)
      const emailField = code === 'EmailBelongsToMaster' || code === 'AccountExists' ? 'email' : (err.field || null);
      setServerErr({ code, message, field: emailField });
    } finally {
      setLoading(false);
    }
  }

  // ── Step 3: Verify OTP ──
  async function handleVerifyOtp(e) {
    e.preventDefault();
    if (!otpInput.trim() || otpInput.length < 6) {
      setServerErr({ code: null, message: 'Enter the 6-digit code from your email.', field: null });
      return;
    }
    setLoading(true);
    setServerErr(null);
    try {
      await apiPost('/api/signup/verify-otp', { tempSignupToken: tempToken, otp: otpInput });
      setStep('PASSWORD');
    } catch (err) {
      const net = netErrMessage(err);
      setServerErr(net || { code: null, message: 'Invalid or expired code. Please try again.', field: null });
    } finally {
      setLoading(false);
    }
  }

  /*
   * Resend re-calls /api/signup/register (idempotent — see G2 finding).
   * The backend soft-deletes the stale TemporaryMember and issues a new OTP
   * with a fresh tempSignupToken. We update tempToken so the next OTP submit
   * uses the correct new token.
   * 30-second cooldown prevents spam (#20).
   */
  async function handleResend() {
    if (resendCooldown > 0) return;
    try {
      const data = await apiPost('/api/signup/register', {
        tin: formData.tin, firstName: formData.firstName, lastName: formData.lastName,
        email: formData.email, mobile: formData.mobile, nic: formData.nic,
        designation: formData.designation, agreeToTerms: formData.agreeToTerms,
      });
      if (data?.tempSignupToken) setTempToken(data.tempSignupToken);
    } catch { /* ignore resend errors — user can retry */ }

    setResendCooldown(30);
    resendTimerRef.current = setInterval(() => {
      setResendCooldown(c => {
        if (c <= 1) { clearInterval(resendTimerRef.current); return 0; }
        return c - 1;
      });
    }, 1000);
  }

  // ── Step 4: Set password (M2 — no refreshUser here, deferred to button) ──
  async function handleSetPassword(e) {
    e.preventDefault();
    setServerErr(null);
    if (!checkPassword(passwordInput).valid) {
      setServerErr({ code: null, message: 'Your password must meet every rule above.', field: null });
      return;
    }
    if (passwordInput !== confirmPassword) {
      setServerErr({ code: null, message: 'The two passwords do not match.', field: null });
      return;
    }
    setLoading(true);
    try {
      await apiPost('/api/signup/set-password', {
        tempSignupToken: tempToken,
        password: passwordInput,
      });
      // Session is now issued. Show SUCCESS screen first (M2).
      // refreshUser() runs when "Continue to your dashboard" is clicked.
      setStep('SUCCESS');
    } catch (err) {
      const net = netErrMessage(err);
      if (net) { setServerErr(net); return; }
      const message = err.code === 'WeakPassword'
        ? 'Your password must meet every rule above.'
        : 'Failed to set password. Please try again.';
      setServerErr({ code: err.code, message, field: null });
    } finally {
      setLoading(false);
    }
  }

  // ── SUCCESS: navigate to dashboard (M2 — refreshUser here) ──
  const [dashLoading, setDashLoading] = useState(false);
  async function handleContinue() {
    setDashLoading(true);
    try {
      const u = await refreshUser();
      const portal = u?.portal?.toLowerCase() ?? 'member';
      window.location.assign(`/${portal}/dashboard`);
    } catch {
      // refreshUser failed — full reload forces re-auth
      window.location.assign('/login');
    }
  }

  // ── Left panel ──
  const showStepper = true; // show stepper on all steps including SUCCESS (#11)
  const leftTop = (
    <>
      <Rise delay={0}><AuthLogoRow /></Rise>
      {showStepper && (
        <>
          <Rise delay={0.08}><h1 className="signup-left-h1">Join your company on CDNS.</h1></Rise>
          <Rise delay={0.16}><SignupStepper step={step} /></Rise>
        </>
      )}
    </>
  );

  return (
    <AuthShell leftTop={leftTop} compact wideForm>
      {/* ── SUCCESS ── */}
      {step === 'SUCCESS' && (
        <div key="SUCCESS" className="signup-done">
          <div className="auth-success-circle">
            <svg width="36" height="36" viewBox="0 0 36 36" fill="none" aria-hidden="true">
              <polyline
                points="8,19 15,26 28,12"
                stroke="#17694A"
                strokeWidth="3"
                strokeLinecap="round"
                strokeLinejoin="round"
                className="auth-success-check"
              />
            </svg>
          </div>
          <Rise delay={0}>
            <div className="auth-heading-block" style={{ textAlign: 'center' }}>
              <h2>Your account is ready</h2>
              <p>
                Welcome to CDNS, {formData.firstName}. Your company's Master user approves
                subscription requests from here on.
              </p>
            </div>
          </Rise>
          <Rise delay={0.08}>
            <button
              className="auth-btn"
              onClick={handleContinue}
              disabled={dashLoading}
            >
              {dashLoading && <span className="auth-btn-spinner" />}
              {dashLoading ? 'Loading…' : 'Continue to your dashboard'}
              {!dashLoading && <ArrowRight size={18} />}
            </button>
          </Rise>
        </div>
      )}

      {/* ── STEPS 1-4 ── */}
      {step !== 'SUCCESS' && (
        <>
          <StepHeader step={step} onBack={handleBack} />

          {/* ── TIN ── */}
          {step === 'TIN' && (
            <div key="TIN" style={{ display: 'contents' }}>
              <Rise delay={0}>
                <div className="auth-heading-block">
                  <h2>Find your company</h2>
                  <p>
                    Enter your company's 9-digit TIN. We only use it to check that the
                    organisation is registered with CDNS.
                  </p>
                </div>
              </Rise>

              {/* Network/server error banner */}
              {serverErr && (
                <Rise delay={0}>
                  <div id="signup-err-banner" className="auth-banner auth-banner--error cd-shake" role="alert" tabIndex={-1}>
                    <span className="auth-banner-icon"><AlertCircle size={20} color="#7A1C14" /></span>
                    <span>{serverErr.message}</span>
                  </div>
                </Rise>
              )}

              {tinWarningData && (
                <Rise delay={0}>
                  <div className="auth-banner auth-banner--warning cd-shake" role="alert">
                    <span className="auth-banner-icon"><AlertCircle size={20} color="#6B3D06" /></span>
                    <span>
                      <strong className="auth-banner-title">{tinWarningData.title}</strong>
                      {' '}{tinWarningData.body}
                    </span>
                  </div>
                </Rise>
              )}

              <form onSubmit={handleVerifyTin} noValidate style={{ display: 'contents' }}>
                <Rise delay={0.06}>
                  <div>
                    <label className="auth-label" htmlFor="signup-tin">Company TIN</label>
                    <input
                      id="signup-tin"
                      className={`auth-input auth-input--tin${getFieldError('tin') ? ' auth-input--error' : ''}`}
                      type="text"
                      inputMode="numeric"
                      maxLength={9}
                      value={formData.tin}
                      onChange={e => {
                        const v = e.target.value.replace(/\D/g, '').slice(0, 9);
                        setFormData(prev => ({ ...prev, tin: v }));
                        setTinWarningData(null);
                        setServerErr(null);
                        if (touched.tin) setFieldErrors(prev => ({ ...prev, tin: validateField('tin', v) }));
                      }}
                      onBlur={() => handleBlur('tin')}
                      placeholder="9 digits"
                      aria-invalid={!!getFieldError('tin')}
                    />
                    {getFieldError('tin') && (
                      <p className="auth-field-error" role="alert">{getFieldError('tin')}</p>
                    )}
                  </div>
                </Rise>

                <Rise delay={0.12}>
                  <button type="submit" className="auth-btn" disabled={loading}>
                    {loading && <span className="auth-btn-spinner" />}
                    {loading ? 'Verifying…' : 'Verify TIN'}
                  </button>
                </Rise>
              </form>

              <Rise delay={0.18}>
                <p className="signup-signin-link">
                  Already have an account?{' '}
                  <Link to="/login" className="auth-link-btn" style={{ display: 'inline' }}>Sign in</Link>
                </p>
              </Rise>
            </div>
          )}

          {/* ── DETAILS ── */}
          {step === 'DETAILS' && (
            <div key="DETAILS" style={{ display: 'contents' }}>
              <Rise delay={0}>
                <div className="auth-heading-block">
                  <h2>Your details</h2>
                  <p>Tell us who you are. Your email becomes your CDNS username.</p>
                </div>
              </Rise>

              {serverErr && !serverErr.field && (
                <Rise delay={0}>
                  <div id="signup-err-banner" className="auth-banner auth-banner--error cd-shake" role="alert" tabIndex={-1}>
                    <span className="auth-banner-icon">
                      {!serverErr.status || serverErr.status >= 500
                        ? <WifiOff size={20} color="#7A1C14" />
                        : <AlertCircle size={20} color="#7A1C14" />}
                    </span>
                    <span>{serverErr.message}</span>
                  </div>
                </Rise>
              )}

              <form onSubmit={handleRegister} noValidate>
                <div className="signup-detail-grid">
                  <div>
                    <label className="auth-label" htmlFor="signup-firstName">First name</label>
                    <input
                      id="signup-firstName" name="firstName" type="text"
                      className={`auth-input${getFieldError('firstName') ? ' auth-input--error' : ''}`}
                      value={formData.firstName} onChange={handleChange} onBlur={() => handleBlur('firstName')}
                      autoComplete="given-name"
                    />
                    {getFieldError('firstName') && <p className="auth-field-error" role="alert">{getFieldError('firstName')}</p>}
                  </div>

                  <div>
                    <label className="auth-label" htmlFor="signup-lastName">Last name</label>
                    <input
                      id="signup-lastName" name="lastName" type="text"
                      className={`auth-input${getFieldError('lastName') ? ' auth-input--error' : ''}`}
                      value={formData.lastName} onChange={handleChange} onBlur={() => handleBlur('lastName')}
                      autoComplete="family-name"
                    />
                    {getFieldError('lastName') && <p className="auth-field-error" role="alert">{getFieldError('lastName')}</p>}
                  </div>

                  <div>
                    <label className="auth-label" htmlFor="signup-email">Email address</label>
                    <input
                      id="signup-email" name="email" type="email"
                      className={`auth-input${getFieldError('email') ? ' auth-input--error' : ''}`}
                      value={formData.email} onChange={handleChange} onBlur={() => handleBlur('email')}
                      placeholder="name@company.lk" autoComplete="email"
                      aria-invalid={!!getFieldError('email')}
                    />
                    {getFieldError('email') && <p className="auth-field-error" role="alert">{getFieldError('email')}</p>}
                  </div>

                  <div>
                    <label className="auth-label" htmlFor="signup-mobile">Mobile number</label>
                    <input
                      id="signup-mobile" name="mobile" type="tel"
                      className={`auth-input${getFieldError('mobile') ? ' auth-input--error' : ''}`}
                      value={formData.mobile} onChange={handleChange} onBlur={() => handleBlur('mobile')}
                      placeholder="07X XXX XXXX" autoComplete="tel"
                    />
                    {getFieldError('mobile') && <p className="auth-field-error" role="alert">{getFieldError('mobile')}</p>}
                  </div>

                  <div>
                    <label className="auth-label" htmlFor="signup-nic">NIC</label>
                    <input
                      id="signup-nic" name="nic" type="text"
                      className={`auth-input${getFieldError('nic') ? ' auth-input--error' : ''}`}
                      value={formData.nic} onChange={handleNicChange} onBlur={() => handleBlur('nic')}
                      placeholder="National ID number" maxLength={12}
                    />
                    {getFieldError('nic') && <p className="auth-field-error" role="alert">{getFieldError('nic')}</p>}
                  </div>

                  <div>
                    <label className="auth-label" htmlFor="signup-designation">Designation</label>
                    <input
                      id="signup-designation" name="designation" type="text"
                      className={`auth-input${getFieldError('designation') ? ' auth-input--error' : ''}`}
                      value={formData.designation} onChange={handleChange} onBlur={() => handleBlur('designation')}
                      placeholder="Your role"
                    />
                    {getFieldError('designation') && <p className="auth-field-error" role="alert">{getFieldError('designation')}</p>}
                  </div>
                </div>

                {/* TODO(accountType): Account type dropdown hidden — not sent to backend.
                    See docs/auth-ui-audit.md §3 G1 for the backend gap. */}

                <div className="signup-full-field signup-terms">
                  <label className="signup-checkbox-label">
                    <input
                      type="checkbox" name="agreeToTerms"
                      checked={formData.agreeToTerms} onChange={handleChange} onBlur={() => handleBlur('agreeToTerms')}
                    />
                    <span>I agree to the <a href="#" onClick={e => e.preventDefault()}>Terms and Conditions</a></span>
                  </label>
                  {getFieldError('agreeToTerms') && (
                    <p className="auth-field-error" role="alert">{getFieldError('agreeToTerms')}</p>
                  )}
                </div>

                <button type="submit" className="auth-btn signup-submit-btn" disabled={loading}>
                  {loading && <span className="auth-btn-spinner" />}
                  {loading ? 'Sending code…' : 'Send verification code'}
                </button>
              </form>

              <p className="signup-signin-link">
                Already have an account?{' '}
                <Link to="/login" className="auth-link-btn" style={{ display: 'inline' }}>Sign in</Link>
              </p>
            </div>
          )}

          {/* ── OTP ── */}
          {step === 'OTP' && (
            <div key="OTP" style={{ display: 'contents' }}>
              <Rise delay={0}>
                <div className="auth-heading-block">
                  <h2>Verify your email</h2>
                  <p>
                    Enter the 6-digit code we sent to <strong>{formData.email}</strong>.
                  </p>
                </div>
              </Rise>

              {serverErr && (
                <Rise delay={0}>
                  <div id="signup-err-banner" className="auth-banner auth-banner--error cd-shake" role="alert" tabIndex={-1}>
                    <span className="auth-banner-icon"><AlertCircle size={20} color="#7A1C14" /></span>
                    <span>{serverErr.message}</span>
                  </div>
                </Rise>
              )}

              <form onSubmit={handleVerifyOtp} noValidate style={{ display: 'contents' }}>
                <Rise delay={0.06}>
                  <div>
                    <label className="auth-label" htmlFor="signup-otp">6-digit code</label>
                    <input
                      id="signup-otp"
                      className="auth-input auth-input--otp"
                      type="text"
                      inputMode="numeric"
                      autoComplete="one-time-code"
                      value={otpInput}
                      onChange={e => { setOtpInput(e.target.value.replace(/\D/g, '').slice(0, 6)); setServerErr(null); }}
                      placeholder="000000"
                      maxLength={6}
                      aria-invalid={!!serverErr}
                    />
                  </div>
                </Rise>

                <Rise delay={0.12}>
                  <button type="submit" className="auth-btn" disabled={loading}>
                    {loading && <span className="auth-btn-spinner" />}
                    {loading ? 'Verifying…' : 'Verify email'}
                  </button>
                </Rise>
              </form>

              <Rise delay={0.18}>
                <p style={{ textAlign: 'center', margin: 0, fontSize: 14, color: 'var(--auth-muted)' }}>
                  No email yet?{' '}
                  <button
                    type="button"
                    className="auth-link-btn"
                    style={{ display: 'inline', minHeight: 'unset', fontSize: 14 }}
                    onClick={handleResend}
                    disabled={resendCooldown > 0}
                  >
                    {resendCooldown > 0
                      ? `Resend code in 0:${String(resendCooldown).padStart(2, '0')}`
                      : 'Resend code'}
                  </button>
                </p>
              </Rise>

              <p className="signup-signin-link">
                Already have an account?{' '}
                <Link to="/login" className="auth-link-btn" style={{ display: 'inline' }}>Sign in</Link>
              </p>
            </div>
          )}

          {/* ── PASSWORD ── */}
          {step === 'PASSWORD' && (
            <div key="PASSWORD" style={{ display: 'contents' }}>
              <Rise delay={0}>
                <div className="auth-heading-block">
                  <h2>Set your password</h2>
                  <p>Last step. Choose a password you do not use anywhere else.</p>
                </div>
              </Rise>

              {serverErr && (
                <Rise delay={0}>
                  <div id="signup-err-banner" className="auth-banner auth-banner--error cd-shake" role="alert" tabIndex={-1}>
                    <span className="auth-banner-icon"><AlertCircle size={20} color="#7A1C14" /></span>
                    <span>{serverErr.message}</span>
                  </div>
                </Rise>
              )}

              <form onSubmit={handleSetPassword} noValidate style={{ display: 'contents' }}>
                <Rise delay={0.06}>
                  <div>
                    <label className="auth-label" htmlFor="signup-password">Password</label>
                    <div className="auth-pw-wrap">
                      <input
                        id="signup-password"
                        className="auth-input"
                        type={showPassword ? 'text' : 'password'}
                        value={passwordInput}
                        onChange={e => { setPasswordInput(e.target.value); setServerErr(null); }}
                        autoComplete="new-password"
                      />
                      <button
                        type="button"
                        className="auth-pw-toggle"
                        onClick={() => setShowPassword(v => !v)}
                        aria-label={showPassword ? 'Hide password' : 'Show password'}
                      >
                        {showPassword ? <EyeOff size={20} /> : <Eye size={20} />}
                      </button>
                    </div>
                    <PasswordRequirements value={passwordInput} />
                  </div>
                </Rise>

                <Rise delay={0.12}>
                  <div>
                    <label className="auth-label" htmlFor="signup-confirm-password">Confirm password</label>
                    <div className="auth-pw-wrap">
                      <input
                        id="signup-confirm-password"
                        className="auth-input"
                        type={showConfirmPassword ? 'text' : 'password'}
                        value={confirmPassword}
                        onChange={e => setConfirmPassword(e.target.value)}
                        autoComplete="new-password"
                      />
                      {/* Separate eye toggle for confirm field (#16) */}
                      <button
                        type="button"
                        className="auth-pw-toggle"
                        onClick={() => setShowConfirmPassword(v => !v)}
                        aria-label={showConfirmPassword ? 'Hide password' : 'Show password'}
                      >
                        {showConfirmPassword ? <EyeOff size={20} /> : <Eye size={20} />}
                      </button>
                    </div>
                    {confirmPassword.length > 0 && confirmPassword !== passwordInput && (
                      <p className="auth-field-error" role="alert">The two passwords do not match.</p>
                    )}
                  </div>
                </Rise>

                <Rise delay={0.18}>
                  <button type="submit" className="auth-btn" disabled={loading}>
                    {loading && <span className="auth-btn-spinner" />}
                    {loading ? 'Creating account…' : 'Create account'}
                  </button>
                </Rise>
              </form>

              <p className="signup-signin-link">
                Already have an account?{' '}
                <Link to="/login" className="auth-link-btn" style={{ display: 'inline' }}>Sign in</Link>
              </p>
            </div>
          )}
        </>
      )}
    </AuthShell>
  );
}
