import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import AuthShell from '../AuthShell';
import { apiPost } from '../../../lib/api';
import './LoginPage.css';

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

const TABS = [
  { id: 'login',  label: 'Log In',   disabled: false },
  { id: '2fa',   label: '2FA Code', disabled: true  },
  { id: 'forgot', label: 'Forgot',   disabled: false },
  { id: 'reset',  label: 'Reset',    disabled: false },
];

export default function LoginPage() {
  const navigate = useNavigate();
  const [tab, setTab] = useState('login');

  // Login state
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loginLoading, setLoginLoading] = useState(false);
  const [loginError, setLoginError] = useState(null);

  // Forgot password state
  const [forgotEmail, setForgotEmail] = useState('');
  const [forgotStep, setForgotStep] = useState('email'); // 'email' | 'otp'
  const [forgotOtp, setForgotOtp] = useState('');
  const [forgotLoading, setForgotLoading] = useState(false);
  const [forgotError, setForgotError] = useState(null);
  const [forgotInfo, setForgotInfo] = useState(null);

  // Reset password state
  const [resetPassword, setResetPassword] = useState('');
  const [showResetPassword, setShowResetPassword] = useState(false);
  const [resetLoading, setResetLoading] = useState(false);
  const [resetError, setResetError] = useState(null);
  const [resetDone, setResetDone] = useState(false);

  function switchTab(t) {
    setTab(t);
    setLoginError(null);
    setForgotError(null);
    setForgotInfo(null);
    setResetError(null);
  }

  async function handleLogin(e) {
    e.preventDefault();
    setLoginLoading(true);
    setLoginError(null);
    try {
      const data = await apiPost('/api/auth/login', { userName: email, password });
      if (data?.type === 'masterActivationRequired') {
        navigate(`/master-activation?token=${data.activationToken}`);
      } else {
        navigate(`/${data.portal.toLowerCase()}/dashboard`);
      }
    } catch (err) {
      setLoginError(
        err.code === 'AccountLocked'
          ? 'Your account is locked due to too many failed attempts. Please try again in 20 minutes.'
          : 'Invalid email address or password.'
      );
    } finally {
      setLoginLoading(false);
    }
  }

  async function handleForgotEmail(e) {
    e.preventDefault();
    setForgotLoading(true);
    setForgotError(null);
    setForgotInfo(null);
    try {
      await apiPost('/api/auth/password-reset/forgot-password', { userName: forgotEmail });
      setForgotStep('otp');
      setForgotInfo('A verification code has been sent to your email address.');
    } catch {
      setForgotError('Unable to send a verification code. Please try again.');
    } finally {
      setForgotLoading(false);
    }
  }

  async function handleVerifyOtp(e) {
    e.preventDefault();
    if (!forgotOtp.trim()) {
      setForgotError('Please enter the verification code.');
      return;
    }
    setForgotLoading(true);
    setForgotError(null);
    try {
      await apiPost('/api/auth/password-reset/verify-otp', { userName: forgotEmail, otp: parseInt(forgotOtp, 10) });
      setForgotInfo(null);
      setTab('reset');
    } catch {
      setForgotError('Invalid or expired code. Please check the code and try again.');
    } finally {
      setForgotLoading(false);
    }
  }

  async function handleResetPassword(e) {
    e.preventDefault();
    if (!forgotEmail) {
      setResetError('Please complete the email verification step first (use the Forgot tab).');
      return;
    }
    if (resetPassword.length < 6) {
      setResetError('Password must be at least 6 characters.');
      return;
    }
    setResetLoading(true);
    setResetError(null);
    try {
      await apiPost('/api/auth/password-reset/reset-password', {
        userName: forgotEmail,
        otp: parseInt(forgotOtp, 10),
        newPassword: resetPassword,
      });
      setResetDone(true);
    } catch {
      setResetError('Could not reset your password. The code may have expired — please start over using the Forgot tab.');
    } finally {
      setResetLoading(false);
    }
  }

  return (
    <AuthShell>
      {/* Brand */}
      <div className="auth-brand">
        <div className="auth-logo-crest" aria-hidden="true"><span>SLC</span></div>
        <p className="auth-brand-name">Sri Lanka Customs</p>
      </div>

      {/* Nav row: back link + segmented tabs */}
      <div className="login-nav-row">
        <Link to="/" className="portal-back-link">← Back to National Portal</Link>
        <div className="login-tabs" role="tablist">
          {TABS.map(t => (
            <button
              key={t.id}
              role="tab"
              aria-selected={tab === t.id}
              className={[
                'login-tab',
                tab === t.id ? 'login-tab--active' : '',
                t.disabled ? 'login-tab--disabled' : '',
              ].filter(Boolean).join(' ')}
              onClick={() => !t.disabled && switchTab(t.id)}
              disabled={t.disabled}
              title={t.disabled ? 'Not yet available' : undefined}
            >
              {t.label}
            </button>
          ))}
        </div>
      </div>

      {/* ── Log In ── */}
      {tab === 'login' && (
        <div className="login-panel">
          <h1 className="login-title">Log In</h1>
          {loginError && <div className="login-error" role="alert">{loginError}</div>}
          <form className="login-form" onSubmit={handleLogin} noValidate>
            <div className="form-field">
              <label htmlFor="login-email">Email Address</label>
              <input
                id="login-email"
                type="email"
                value={email}
                onChange={e => setEmail(e.target.value)}
                required
                autoComplete="email"
              />
            </div>
            <div className="form-field">
              <label htmlFor="login-password">Password</label>
              <div className="pw-wrap">
                <input
                  id="login-password"
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  required
                  autoComplete="current-password"
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
            </div>
            <div className="login-row">
              <label className="remember-me">
                <input type="checkbox" /> Remember Me
              </label>
              <button type="button" className="forgot-link" onClick={() => switchTab('forgot')}>
                Forgot Password?
              </button>
            </div>
            <button type="submit" className="login-btn" disabled={loginLoading}>
              {loginLoading ? 'Signing in…' : 'Sign In'}
            </button>
          </form>
        </div>
      )}

      {/* ── Forgot ── */}
      {tab === 'forgot' && (
        <div className="login-panel">
          <h1 className="login-title">Reset Password</h1>
          <p className="login-subtitle">
            {forgotStep === 'email'
              ? 'Enter your registered email address to receive a verification code.'
              : 'Enter the 6-digit code sent to your email address.'}
          </p>
          {forgotError && <div className="login-error" role="alert">{forgotError}</div>}
          {forgotInfo && <div className="login-success" role="status">{forgotInfo}</div>}

          {forgotStep === 'email' && (
            <form className="login-form" onSubmit={handleForgotEmail} noValidate>
              <div className="form-field">
                <label htmlFor="forgot-email">Email Address</label>
                <input
                  id="forgot-email"
                  type="email"
                  value={forgotEmail}
                  onChange={e => setForgotEmail(e.target.value)}
                  required
                  autoComplete="email"
                />
              </div>
              <button type="submit" className="login-btn" disabled={forgotLoading}>
                {forgotLoading ? 'Sending…' : 'Send Verification Code'}
              </button>
            </form>
          )}

          {forgotStep === 'otp' && (
            <form className="login-form" onSubmit={handleVerifyOtp} noValidate>
              <div className="form-field">
                <label htmlFor="forgot-otp">Verification Code</label>
                <input
                  id="forgot-otp"
                  type="text"
                  inputMode="numeric"
                  className="otp-centered"
                  value={forgotOtp}
                  onChange={e => setForgotOtp(e.target.value.replace(/\D/g, ''))}
                  placeholder="000000"
                  maxLength={6}
                  required
                />
              </div>
              <button type="submit" className="login-btn" disabled={forgotLoading}>
                {forgotLoading ? 'Verifying…' : 'Verify Code'}
              </button>
              <button
                type="button"
                className="login-link-btn"
                onClick={() => { setForgotStep('email'); setForgotInfo(null); setForgotOtp(''); }}
              >
                ← Back / Resend code
              </button>
            </form>
          )}
        </div>
      )}

      {/* ── Reset ── */}
      {tab === 'reset' && (
        <div className="login-panel">
          <h1 className="login-title">Set New Password</h1>
          {!resetDone ? (
            <>
              <p className="login-subtitle">Create a new password for your account.</p>
              {resetError && <div className="login-error" role="alert">{resetError}</div>}
              <form className="login-form" onSubmit={handleResetPassword} noValidate>
                <div className="form-field">
                  <label htmlFor="reset-password">New Password</label>
                  <div className="pw-wrap">
                    <input
                      id="reset-password"
                      type={showResetPassword ? 'text' : 'password'}
                      value={resetPassword}
                      onChange={e => setResetPassword(e.target.value)}
                      required
                      autoComplete="new-password"
                    />
                    <button
                      type="button"
                      className="pw-toggle"
                      onClick={() => setShowResetPassword(v => !v)}
                      aria-label={showResetPassword ? 'Hide password' : 'Show password'}
                    >
                      {showResetPassword ? <EyeOffIcon /> : <EyeIcon />}
                    </button>
                  </div>
                </div>
                <button type="submit" className="login-btn" disabled={resetLoading}>
                  {resetLoading ? 'Resetting…' : 'Reset Password'}
                </button>
              </form>
            </>
          ) : (
            <>
              <p className="login-subtitle">Your password has been reset successfully.</p>
              <button className="login-btn" onClick={() => switchTab('login')}>
                Back to Log In
              </button>
            </>
          )}
        </div>
      )}

      {/* Footer */}
      <div className="login-footer">
        <span>
          Don't have an account?{' '}
          <Link to="/signup">Create an account</Link>
          {' or '}
          <a href="mailto:support@customs.gov.lk">Help</a>
        </span>
      </div>

      {/* Copyright */}
      <div className="login-copyright">
        <span>© 2026 Sri Lanka Customs</span>
        <span className="login-copyright-links">
          <a href="#">Privacy Policy</a>
          <a href="#">Security Disclosure</a>
          <a href="#">Helpdesk</a>
        </span>
      </div>
    </AuthShell>
  );
}
