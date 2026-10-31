import { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import AuthLayout from '../AuthLayout';
import { apiPost } from '../../../lib/api';
import '../Login/LoginPage.css';

const CHALLENGE_LIFETIME_MS = 20 * 60 * 1000;

function formatTime(seconds) {
  const minutes = Math.floor(seconds / 60);
  return `${minutes}:${String(seconds % 60).padStart(2, '0')}`;
}

export default function OtpVerificationPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const challengeId = location.state?.challengeId;
  const email = location.state?.email || 'your email address';
  const [code, setCode] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [expiresIn, setExpiresIn] = useState(Math.floor(CHALLENGE_LIFETIME_MS / 1000));

  useEffect(() => {
    if (!challengeId) return undefined;
    const expiresAt = Date.now() + CHALLENGE_LIFETIME_MS;
    const timer = window.setInterval(() => {
      const remaining = Math.max(0, Math.ceil((expiresAt - Date.now()) / 1000));
      setExpiresIn(remaining);
      if (!remaining) setError('This verification code has expired. Sign in again to request a new one.');
    }, 1000);
    return () => window.clearInterval(timer);
  }, [challengeId]);

  async function handleSubmit(event) {
    event.preventDefault();
    if (!challengeId) {
      setError('Your sign-in verification has expired. Please sign in again.');
      return;
    }
    if (!/^\d{6}$/.test(code)) {
      setError('Enter the six-digit verification code.');
      return;
    }

    setLoading(true);
    setError('');
    try {
      await apiPost('/api/auth/verify-otp', { challengeId, code });
      navigate('/', { replace: true });
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <AuthLayout>
      <h1 className="login-title">Verify your sign-in</h1>
      <h2 className="login-subtitle">Enter the 6-digit code sent to {email}.</h2>
      {error && <div className="login-error" role="alert">{error}</div>}

      <form className="login-form" onSubmit={handleSubmit} noValidate>
        <div className="form-field">
          <label htmlFor="otp-code">Verification code</label>
          <input
            id="otp-code"
            type="text"
            inputMode="numeric"
            autoComplete="one-time-code"
            value={code}
            onChange={event => {
              setCode(event.target.value.replace(/\D/g, '').slice(0, 6));
              setError('');
            }}
            maxLength={6}
            pattern="[0-9]{6}"
            aria-label="6-digit verification code"
            disabled={!challengeId || expiresIn === 0 || loading}
            autoFocus
          />
        </div>
        <p className="otp-expiry" aria-live="polite">
          Code expires in <strong>{formatTime(expiresIn)}</strong>
        </p>
        <button className="login-btn" type="submit" disabled={loading || code.length !== 6 || !challengeId || expiresIn === 0}>
          {loading ? 'Verifying...' : 'Verify and continue'}
        </button>
      </form>

      <div className="login-footer">
        <Link to="/login">Back to sign in</Link>
      </div>
    </AuthLayout>
  );
}
