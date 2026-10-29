import { useState } from 'react';
import { Link } from 'react-router-dom';
import AuthLayout from '../AuthLayout';
import { apiPost } from '../../../lib/api';
import './LoginPage.css';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  async function handleSubmit(e) {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      await apiPost('/api/auth/login', { email, password });
      // TODO: navigate to dashboard on success
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <AuthLayout>
      <h1 className="login-title">Document Notification System</h1>
      <h2 className="login-subtitle">Log In</h2>

      {error && <div className="login-error" role="alert">{error}</div>}

      <form className="login-form" onSubmit={handleSubmit} noValidate>
        <div className="form-field">
          <label htmlFor="email">Email Address</label>
          <input
            id="email"
            type="email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            required
            autoComplete="email"
          />
        </div>

        <div className="form-field">
          <label htmlFor="password">Password</label>
          <input
            id="password"
            type="password"
            value={password}
            onChange={e => setPassword(e.target.value)}
            required
            autoComplete="current-password"
          />
        </div>

        {/* TODO: replace with real Cloudflare Turnstile widget once site key +
            dev-bypass strategy is decided */}
        <div className="turnstile-placeholder" aria-label="Verification placeholder">
          <span className="turnstile-check" aria-hidden="true">✓</span>
          <span>Verification placeholder</span>
        </div>

        <div className="login-row">
          <label className="remember-me">
            <input type="checkbox" />
            Remember Me
          </label>
          <Link to="/forgot-password" className="forgot-link">Forgot Password?</Link>
        </div>

        <button type="submit" className="login-btn" disabled={loading}>
          {loading ? 'Signing in…' : 'Sign in'}
        </button>
      </form>

      <div className="login-footer">
        <span>
          Don&apos;t have an account?{' '}
          <Link to="/signup">Create an account</Link>
        </span>
        <a href="mailto:support@customs.gov.lk" className="help-link">Help</a>
      </div>
    </AuthLayout>
  );
}
