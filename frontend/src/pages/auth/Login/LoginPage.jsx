import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import AuthLayout from '../AuthLayout';
import TurnstileWidget from '../TurnstileWidget';
import { apiPost } from '../../../lib/api';
import './LoginPage.css';

export default function LoginPage() {
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(false);
  const [turnstileToken, setTurnstileToken] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [errors, setErrors] = useState({});
  const [verificationError, setVerificationError] = useState('');

  async function handleSubmit(e) {
    e.preventDefault();
    const nextErrors = {};
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) nextErrors.email = 'Enter a valid email address.';
    if (!password) nextErrors.password = 'Enter your password.';
    if (!turnstileToken) nextErrors.turnstile = 'Complete the security verification.';
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length) return;

    setLoading(true);
    setError(null);
    try {
      const result = await apiPost('/api/auth/login', {
        email: email.trim(),
        password,
        rememberMe,
        turnstileToken,
      });

      if (result?.requiresOtp) {
        navigate('/otp-verification', {
          state: { challengeId: result.challengeId, email: email.trim() },
        });
        return;
      }

      if (result?.token) {
        // The backend does not set an httpOnly cookie, so store its JWT for bearer requests.
        localStorage.setItem('cdns_admin_token', result.token);
        navigate('/admin/dashboard', { replace: true });
        return;
      }

      setError('The sign-in response was incomplete. Please try again or contact support.');
    } catch (err) {
      if (err.status === 429) {
        setError(err.details?.lockedUntil
          ? `This account is temporarily locked. Try again after ${new Date(err.details.lockedUntil).toLocaleTimeString()}.`
          : 'Too many attempts. Please wait before trying again.');
      } else {
        setError(err.message);
      }
      setTurnstileToken('');
    } finally {
      setLoading(false);
    }
  }

  return (
    <AuthLayout>
      <h1 className="login-title">Document Notification System</h1>
      <h2 className="login-subtitle">Sign in to your account</h2>

      {error && <div className="login-error" role="alert" aria-live="polite">{error}</div>}

      <form className="login-form" onSubmit={handleSubmit} noValidate>
        <div className="form-field">
          <label htmlFor="email">Email Address</label>
          <input
            id="email"
            type="email"
            value={email}
            onChange={e => { setEmail(e.target.value); setErrors(current => ({ ...current, email: '' })); }}
            aria-invalid={Boolean(errors.email)}
            aria-describedby={errors.email ? 'email-error' : undefined}
            required
            autoComplete="email"
            disabled={loading}
          />
          {errors.email && <span id="email-error" className="field-error">{errors.email}</span>}
        </div>

        <div className="form-field">
          <label htmlFor="password">Password</label>
          <input
            id="password"
            type="password"
            value={password}
            onChange={e => { setPassword(e.target.value); setErrors(current => ({ ...current, password: '' })); }}
            aria-invalid={Boolean(errors.password)}
            aria-describedby={errors.password ? 'password-error' : undefined}
            required
            autoComplete="current-password"
            disabled={loading}
          />
          {errors.password && <span id="password-error" className="field-error">{errors.password}</span>}
        </div>

        <TurnstileWidget
          onToken={token => { setTurnstileToken(token); setErrors(current => ({ ...current, turnstile: '' })); }}
          onError={setVerificationError}
        />
        {(errors.turnstile || verificationError) && (
          <span className="field-error" role="alert">{errors.turnstile || verificationError}</span>
        )}

        <div className="login-row">
          <label className="remember-me">
            <input
              type="checkbox"
              checked={rememberMe}
              onChange={e => setRememberMe(e.target.checked)}
              disabled={loading}
            />
            Remember Me
          </label>
          <Link to="/forgot-password" className="forgot-link">Forgot Password?</Link>
        </div>

        <button type="submit" className="login-btn" disabled={loading}>
          {loading ? 'Signing in...' : 'Sign in'}
        </button>
      </form>

      <div className="login-footer">
        <a href="mailto:support@customs.gov.lk" className="help-link">Need help signing in?</a>
      </div>
    </AuthLayout>
  );
}
