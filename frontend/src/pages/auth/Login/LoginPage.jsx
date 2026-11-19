import { useState, useEffect, useRef } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { motion } from 'motion/react';
import { AlertCircle, ArrowRight, Eye, EyeOff, Info, Lock, ArrowLeft, WifiOff } from 'lucide-react';
import AuthShell, { AuthLogoRow } from '../AuthShell';
import { apiPost } from '../../../lib/api';
import { useAuth } from '../../../contexts/useAuth';
import { checkPassword } from '../../../utils/passwordPolicy';
import shipSvg from '../../../assets/svg/ship.svg';
import './LoginPage.css';

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

function Banner({ variant, icon, title, body }) {
  const [shakeKey, setShakeKey] = useState(0);
  useEffect(() => { setShakeKey(k => k + 1); }, [title, body]);
  return (
    <div
      key={shakeKey}
      className={`auth-banner auth-banner--${variant} cd-shake`}
      role="alert"
    >
      <span className="auth-banner-icon">{icon}</span>
      <span>
        <strong className="auth-banner-title">{title}</strong>
        {' '}{body}
      </span>
    </div>
  );
}

const STAGE_TEXT = [
  'Clearing Customs…',
  'Preparing your workspace…',
  'Opening your portal…',
];

function LoadingOverlay({ stage }) {
  return (
    <div className="login-overlay" role="status" aria-live="polite">
      {stage !== 'done' ? (
        <>
          <div className="login-overlay-lane">
            <div className="login-overlay-ship cd-load-ship">
              <img
                src={shipSvg}
                alt=""
                aria-hidden="true"
                className="cd-bob-load"
                style={{ width: 64, height: 25, objectFit: 'fill' }}
              />
            </div>
            <div className="login-overlay-dock-post" aria-hidden="true" />
            <svg
              aria-hidden="true"
              width="100%"
              height="16"
              viewBox="0 0 320 16"
              preserveAspectRatio="none"
              style={{ position: 'absolute', left: 0, bottom: 0 }}
            >
              <path
                className="overlay-wave"
                fill="none"
                stroke="#5E97A8"
                strokeWidth="2"
                d="M-120 8q15 -6 30 0t30 0t30 0t30 0t30 0t30 0t30 0t30 0t30 0t30 0t30 0t30 0t30 0t30 0t30 0t30 0t30 0t30 0t30 0"
              />
            </svg>
          </div>

          <div className="login-overlay-track">
            <div className="login-overlay-bar" />
          </div>

          <p className="login-overlay-stage">{STAGE_TEXT[stage] ?? STAGE_TEXT[0]}</p>
          <p className="login-overlay-hint">This only takes a moment.</p>
        </>
      ) : (
        <>
          <div className="login-overlay-success-circle">
            <svg width="36" height="36" viewBox="0 0 36 36" fill="none" aria-hidden="true">
              <polyline
                points="8,19 15,26 28,12"
                stroke="var(--auth-primary)"
                strokeWidth="3"
                strokeLinecap="round"
                strokeLinejoin="round"
                className="auth-success-check"
              />
            </svg>
          </div>
          <p className="login-overlay-welcome">Welcome back</p>
          <p className="login-overlay-opening">Your dashboard opens here.</p>
        </>
      )}
    </div>
  );
}

export default function LoginPage() {
  const navigate = useNavigate();
  const { refreshUser } = useAuth();

  const [view, setView] = useState('login');
  const [overlay, setOverlay] = useState(null);

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loginLoading, setLoginLoading] = useState(false);
  // 'invalid' | 'locked' | 'network' | 'empty-username' | 'empty-password' | null
  const [loginError, setLoginError] = useState(null);

  const [forgotUsername, setForgotUsername] = useState('');
  const [forgotLoading, setForgotLoading] = useState(false);
  const [forgotError, setForgotError] = useState(null);

  const [resetOtp, setResetOtp] = useState('');
  const [resetPassword, setResetPassword] = useState('');
  const [showResetPassword, setShowResetPassword] = useState(false);
  const [resetLoading, setResetLoading] = useState(false);
  const [resetOtpError, setResetOtpError] = useState(null);
  const [resetPwError, setResetPwError] = useState(null);

  // Store timer IDs so they can be cleared on unmount (#27)
  const timerRefs = useRef([]);
  useEffect(() => {
    const timers = timerRefs.current;
    return () => { timers.forEach(clearTimeout); };
  }, []);

  function scheduleTimeout(fn, ms) {
    const id = setTimeout(fn, ms);
    timerRefs.current.push(id);
    return id;
  }

  function goToForgot() {
    setForgotUsername(username);
    setForgotError(null);
    setView('forgot');
  }

  function goToLogin() {
    setLoginError(null);
    setResetOtp('');
    setResetPassword('');
    setResetOtpError(null);
    setResetPwError(null);
    setView('login');
  }

  // ── Login ──
  async function handleLogin(e) {
    e.preventDefault();

    // B1 — empty-field validation before hitting the API
    if (!username.trim()) { setLoginError('empty-username'); return; }
    if (!password.trim()) { setLoginError('empty-password'); return; }

    setLoginLoading(true);
    setLoginError(null);

    // Show overlay immediately after login API succeeds (#23)
    // refreshUser runs asynchronously during the stage cycle
    try {
      const data = await apiPost('/api/auth/login', { userName: username, password });

      if (data?.type === 'masterActivationRequired') {
        navigate(`/master-activation?token=${data.activationToken}`);
        return;
      }

      // Kick off refreshUser in parallel with the overlay animation (#23)
      const userPromise = refreshUser();

      setOverlay({ stage: 0, path: null });
      scheduleTimeout(() => setOverlay(o => o ? { ...o, stage: 1 } : o), 900);
      scheduleTimeout(() => setOverlay(o => o ? { ...o, stage: 2 } : o), 1800);

      // Resolve user and navigate once both the minimum animation time and
      // refreshUser have finished (#23)
      const minWait = new Promise(res => scheduleTimeout(res, 2600));
      const [u] = await Promise.all([userPromise, minWait]);

      if (!u) {
        // refreshUser failed — treat as network error
        setOverlay(null);
        setLoginError('network');
        return;
      }

      const path = `/${u.portal.toLowerCase()}/dashboard`;
      setOverlay(o => o ? { ...o, stage: 'done', path } : o);
      scheduleTimeout(() => navigate(path), 600);

    } catch (err) {
      if (!err.status || err.status >= 500) {
        setLoginError('network');
      } else if (err.code === 'AccountLocked') {
        setPassword('');
        setLoginError('locked');
      } else {
        setPassword('');
        setLoginError('invalid');
      }
    } finally {
      setLoginLoading(false);
    }
  }

  // ── Forgot: send reset code ──
  async function handleForgot(e) {
    e.preventDefault();
    if (!forgotUsername.trim()) {
      setForgotError('Enter your username.');
      return;
    }
    setForgotLoading(true);
    setForgotError(null);
    try {
      await apiPost('/api/auth/password-reset/forgot-password', { userName: forgotUsername });
      setResetOtp('');
      setResetPassword('');
      setResetOtpError(null);
      setResetPwError(null);
      setView('reset');
    } catch (err) {
      setForgotError(
        !err.status || err.status >= 500
          ? 'Something went wrong. Check your connection and try again.'
          : 'Unable to send a reset code. Please try again.',
      );
    } finally {
      setForgotLoading(false);
    }
  }

  // ── Reset: OTP + new password together ──
  async function handleReset(e) {
    e.preventDefault();
    let valid = true;

    if (resetOtp.length < 6) {
      setResetOtpError('Enter the 6-digit code from your email.');
      valid = false;
    } else {
      setResetOtpError(null);
    }

    if (!checkPassword(resetPassword).valid) {
      setResetPwError('That password does not meet the rules above.');
      valid = false;
    } else {
      setResetPwError(null);
    }

    if (!valid) return;

    setResetLoading(true);
    try {
      await apiPost('/api/auth/password-reset/reset-password', {
        userName: forgotUsername,
        otp: resetOtp,
        newPassword: resetPassword,
      });
      setView('resetDone');
    } catch (err) {
      if (err.code === 'WeakPassword') {
        setResetPwError('That password does not meet the rules above.');
      } else if (!err.status || err.status >= 500) {
        setResetOtpError('Something went wrong. Check your connection and try again.');
      } else {
        setResetOtpError('The code may have expired — go back and request a new one.');
      }
    } finally {
      setResetLoading(false);
    }
  }

  const leftTop = (
    <>
      <Rise delay={0}><AuthLogoRow /></Rise>
      <div className="auth-left-copy">
        <Rise delay={0.08}>
          <h1>Every declaration, from ship to gate.</h1>
        </Rise>
        <Rise delay={0.16}>
          <p>
            The Customer Document Notification System keeps your company up to
            date on every CusDec and container as it moves through Customs.
          </p>
        </Rise>
      </div>
    </>
  );

  return (
    <AuthShell leftTop={leftTop}>
      {overlay && <LoadingOverlay stage={overlay.stage} />}

      {/* ── login view ── */}
      {view === 'login' && (
        <div key="login" style={{ display: 'contents' }}>
          <Rise delay={0}>
            <div className="auth-heading-block">
              <h2>Sign in to CDNS</h2>
              <p>Use the username and password for your Member or Master account.</p>
            </div>
          </Rise>

          {loginError === 'invalid' && (
            <Rise delay={0}>
              <Banner
                variant="error"
                icon={<AlertCircle size={20} color="#7A1C14" />}
                title="Incorrect username or password"
                body="Check your details and try again."
              />
            </Rise>
          )}
          {loginError === 'locked' && (
            <Rise delay={0}>
              <Banner
                variant="warning"
                icon={<Lock size={20} color="#6B3D06" />}
                title="Account locked"
                body="Too many failed attempts. Try again in 20 minutes, or reset your password."
              />
            </Rise>
          )}
          {loginError === 'network' && (
            <Rise delay={0}>
              <Banner
                variant="error"
                icon={<WifiOff size={20} color="#7A1C14" />}
                title="Something went wrong"
                body="We couldn't reach CDNS. Check your connection and try again."
              />
            </Rise>
          )}

          <form onSubmit={handleLogin} noValidate style={{ display: 'contents' }}>
            <Rise delay={0.06}>
              <div>
                <label className="auth-label" htmlFor="login-username">Username</label>
                <input
                  id="login-username"
                  className={`auth-input${loginError === 'empty-username' ? ' auth-input--error' : ''}`}
                  type="text"
                  value={username}
                  onChange={e => { setUsername(e.target.value); if (loginError) setLoginError(null); }}
                  placeholder="name@company.lk"
                  autoComplete="username"
                  aria-invalid={loginError === 'empty-username' || loginError === 'invalid'}
                />
                {loginError === 'empty-username' && (
                  <p className="auth-field-error" role="alert">Enter your username.</p>
                )}
              </div>
            </Rise>

            <Rise delay={0.12}>
              <div>
                <div className="auth-label-row">
                  <label className="auth-label" htmlFor="login-password">Password</label>
                  <button type="button" className="auth-link-btn" onClick={goToForgot}>
                    Forgot password?
                  </button>
                </div>
                <div className="auth-pw-wrap">
                  <input
                    id="login-password"
                    className={`auth-input${loginError === 'empty-password' ? ' auth-input--error' : ''}`}
                    type={showPassword ? 'text' : 'password'}
                    value={password}
                    onChange={e => { setPassword(e.target.value); if (loginError) setLoginError(null); }}
                    placeholder="Enter your password"
                    autoComplete="current-password"
                    aria-invalid={loginError === 'empty-password' || loginError === 'invalid'}
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
                {loginError === 'empty-password' && (
                  <p className="auth-field-error" role="alert">Enter your password.</p>
                )}
              </div>
            </Rise>

            <Rise delay={0.18}>
              {/* Sign in → (arrow to the RIGHT of label) */}
              <button type="submit" className="auth-btn" disabled={loginLoading}>
                {loginLoading ? <span className="auth-btn-spinner" /> : null}
                {loginLoading ? 'Signing in…' : 'Sign in'}
                {!loginLoading && <ArrowRight size={18} />}
              </button>
            </Rise>
          </form>

          <Rise delay={0.24}>
            <div className="auth-info-card">
              <Info size={20} color="var(--auth-primary)" />
              <span>
                Signing in as a company Master for the first time? Use your eReg password
                to activate the account.
              </span>
            </div>
          </Rise>

          <Rise delay={0.30}>
            <p className="login-signup-link">
              New to CDNS?{' '}
              <Link to="/signup" className="auth-link-btn" style={{ display: 'inline' }}>
                Create an account
              </Link>
            </p>
          </Rise>
        </div>
      )}

      {/* ── forgot view ── */}
      {view === 'forgot' && (
        <div key="forgot" style={{ display: 'contents' }}>
          <Rise delay={0}>
            <div className="auth-heading-block">
              <h2>Reset your password</h2>
              <p>Enter your username and we will email you a 6-digit code.</p>
            </div>
          </Rise>

          {/* Only show banner for API-level errors, not for empty-field (#10) */}
          {forgotError && forgotError !== 'Enter your username.' && (
            <Rise delay={0}>
              <Banner
                variant="error"
                icon={<AlertCircle size={20} color="#7A1C14" />}
                title="Could not send code"
                body={forgotError}
              />
            </Rise>
          )}

          <form onSubmit={handleForgot} noValidate style={{ display: 'contents' }}>
            <Rise delay={0.06}>
              <div>
                <label className="auth-label" htmlFor="forgot-username">Username</label>
                <input
                  id="forgot-username"
                  className={`auth-input${forgotError ? ' auth-input--error' : ''}`}
                  type="text"
                  value={forgotUsername}
                  onChange={e => { setForgotUsername(e.target.value); setForgotError(null); }}
                  autoComplete="username"
                  aria-invalid={!!forgotError}
                />
                {/* Inline error only for empty-field case (#10 — no banner duplication) */}
                {forgotError === 'Enter your username.' && (
                  <p className="auth-field-error" role="alert">{forgotError}</p>
                )}
              </div>
            </Rise>

            <Rise delay={0.12}>
              <button type="submit" className="auth-btn" disabled={forgotLoading}>
                {forgotLoading && <span className="auth-btn-spinner" />}
                {forgotLoading ? 'Sending…' : 'Send reset code'}
              </button>
            </Rise>
          </form>

          <Rise delay={0.18}>
            <button type="button" className="auth-link-btn auth-link-btn--block" onClick={goToLogin}>
              <ArrowLeft size={16} />
              Back to sign in
            </button>
          </Rise>
        </div>
      )}

      {/* ── reset view ── */}
      {view === 'reset' && (
        <div key="reset" style={{ display: 'contents' }}>
          <Rise delay={0}>
            <div className="auth-heading-block">
              <h2>Check your email</h2>
              <p>
                If an account exists for that username, a 6-digit code is on its way.
                Enter it with your new password.
              </p>
            </div>
          </Rise>

          <form onSubmit={handleReset} noValidate style={{ display: 'contents' }}>
            <Rise delay={0.06}>
              <div>
                <label className="auth-label" htmlFor="reset-otp">6-digit code</label>
                <input
                  id="reset-otp"
                  className={`auth-input auth-input--otp${resetOtpError ? ' auth-input--error' : ''}`}
                  type="text"
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  value={resetOtp}
                  onChange={e => { setResetOtp(e.target.value.replace(/\D/g, '').slice(0, 6)); setResetOtpError(null); }}
                  placeholder="000000"
                  maxLength={6}
                />
                {resetOtpError && <p className="auth-field-error" role="alert">{resetOtpError}</p>}
              </div>
            </Rise>

            <Rise delay={0.12}>
              <div>
                <label className="auth-label" htmlFor="reset-password">New password</label>
                <div className="auth-pw-wrap">
                  <input
                    id="reset-password"
                    className={`auth-input${resetPwError ? ' auth-input--error' : ''}`}
                    type={showResetPassword ? 'text' : 'password'}
                    value={resetPassword}
                    onChange={e => { setResetPassword(e.target.value); setResetPwError(null); }}
                    autoComplete="new-password"
                  />
                  <button
                    type="button"
                    className="auth-pw-toggle"
                    onClick={() => setShowResetPassword(v => !v)}
                    aria-label={showResetPassword ? 'Hide password' : 'Show password'}
                  >
                    {showResetPassword ? <EyeOff size={20} /> : <Eye size={20} />}
                  </button>
                </div>
                <p className="auth-field-hint">
                  At least 8 characters with an uppercase letter, a lowercase letter, a number and a symbol.
                </p>
                {resetPwError && <p className="auth-field-error" role="alert">{resetPwError}</p>}
              </div>
            </Rise>

            <Rise delay={0.18}>
              <button type="submit" className="auth-btn" disabled={resetLoading}>
                {resetLoading && <span className="auth-btn-spinner" />}
                {resetLoading ? 'Resetting…' : 'Reset password'}
              </button>
            </Rise>
          </form>

          <Rise delay={0.24}>
            <button type="button" className="auth-link-btn auth-link-btn--block" onClick={goToLogin}>
              <ArrowLeft size={16} />
              Back to sign in
            </button>
          </Rise>
        </div>
      )}

      {/* ── resetDone view ── */}
      {view === 'resetDone' && (
        <div key="resetDone" className="login-reset-done">
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
              <h2>Password updated</h2>
              <p>
                You have been signed out on every device. Sign in with your new password.
              </p>
            </div>
          </Rise>
          <Rise delay={0.08}>
            <button className="auth-btn" onClick={goToLogin}>
              Back to sign in
            </button>
          </Rise>
        </div>
      )}
    </AuthShell>
  );
}
