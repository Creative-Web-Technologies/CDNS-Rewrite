import { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import AuthLayout from '../AuthLayout';
import { apiPost } from '../../../lib/api';
import { useAuth } from '../../../contexts/useAuth';
import '../Login/LoginPage.css';

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

export default function MasterActivationPage() {
  const [params] = useSearchParams();
  const activationToken = params.get('token') ?? '';
  const navigate = useNavigate();
  const { setAuth } = useAuth();

  const [step, setStep] = useState('profile'); // 'profile' | 'password'
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  // Profile form
  const [profile, setProfile] = useState({
    firstName: '', lastName: '', mobile: '', nic: '', designation: '',
  });

  // Password form
  const [password, setPassword] = useState('');
  const [showPw, setShowPw] = useState(false);

  if (!activationToken) {
    return (
      <AuthLayout>
        <h1 style={{ fontSize: 20, fontWeight: 700, color: '#0f172a', margin: '0 0 8px' }}>
          Invalid Activation Link
        </h1>
        <p style={{ color: '#6b7280', fontSize: 14 }}>
          No activation token found. Please log in again to restart the activation flow.
        </p>
      </AuthLayout>
    );
  }

  async function handleProfile(e) {
    e.preventDefault();
    if (!profile.firstName || !profile.lastName) {
      setError('First name and last name are required.');
      return;
    }
    setLoading(true);
    setError('');
    try {
      await apiPost('/api/auth/master-activation/profile', {
        activationToken,
        ...profile,
      });
      setStep('password');
    } catch {
      setError('Could not save profile. Please try again.');
    } finally {
      setLoading(false);
    }
  }

  async function handlePassword(e) {
    e.preventDefault();
    if (password.length < 6) {
      setError('Password must be at least 6 characters.');
      return;
    }
    setLoading(true);
    setError('');
    try {
      const data = await apiPost('/api/auth/master-activation/password', {
        activationToken,
        password,
      });
      // data = { type: "success", portal: "Company" }
      setAuth(null); // force /me re-fetch on next protected page load
      navigate(`/${data.portal.toLowerCase()}/dashboard`);
    } catch {
      setError('Could not set password. The activation link may have expired.');
    } finally {
      setLoading(false);
    }
  }

  function handleProfileChange(e) {
    setProfile(p => ({ ...p, [e.target.name]: e.target.value }));
  }

  return (
    <AuthLayout>
      {/* Step indicator */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 24 }}>
        {['Complete Profile', 'Set Password'].map((label, i) => {
          const n = i + 1;
          const stepNum = step === 'profile' ? 1 : 2;
          const done = n < stepNum;
          const active = n === stepNum;
          return (
            <span
              key={label}
              style={{
                fontSize: 12,
                fontWeight: active || done ? 600 : 400,
                color: active ? '#1e40af' : done ? '#166534' : '#9ca3af',
                fontFamily: 'system-ui, sans-serif',
              }}
            >
              {n}. {label}{i < 1 ? ' →' : ''}
            </span>
          );
        })}
      </div>

      <h1 style={{ fontSize: 20, fontWeight: 700, color: '#0f172a', margin: '0 0 4px', fontFamily: 'system-ui, sans-serif' }}>
        {step === 'profile' ? 'Complete Your Profile' : 'Set Your Password'}
      </h1>
      <p style={{ color: '#6b7280', fontSize: 14, marginBottom: 24, fontFamily: 'system-ui, sans-serif' }}>
        {step === 'profile'
          ? 'Enter your personal details to activate your company administrator account.'
          : 'Create a secure password to finish activation.'}
      </p>

      {error && (
        <div role="alert" style={{
          background: '#fef2f2', border: '1px solid #fca5a5', color: '#b91c1c',
          borderRadius: 8, padding: '12px 16px', fontSize: 14, marginBottom: 16,
          fontFamily: 'system-ui, sans-serif',
        }}>
          {error}
        </div>
      )}

      {step === 'profile' && (
        <form onSubmit={handleProfile} noValidate style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <Field id="ma-firstName" label="First Name *">
              <input id="ma-firstName" name="firstName" type="text" value={profile.firstName}
                onChange={handleProfileChange} required autoComplete="given-name" />
            </Field>
            <Field id="ma-lastName" label="Last Name *">
              <input id="ma-lastName" name="lastName" type="text" value={profile.lastName}
                onChange={handleProfileChange} required autoComplete="family-name" />
            </Field>
            <Field id="ma-mobile" label="Mobile">
              <input id="ma-mobile" name="mobile" type="tel" value={profile.mobile}
                onChange={handleProfileChange} autoComplete="tel" />
            </Field>
            <Field id="ma-nic" label="NIC">
              <input id="ma-nic" name="nic" type="text" value={profile.nic}
                onChange={handleProfileChange} />
            </Field>
          </div>
          <Field id="ma-designation" label="Designation">
            <input id="ma-designation" name="designation" type="text" value={profile.designation}
              onChange={handleProfileChange} />
          </Field>
          <SubmitBtn loading={loading} label="Save & Continue" loadingLabel="Saving…" />
        </form>
      )}

      {step === 'password' && (
        <form onSubmit={handlePassword} noValidate style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <Field id="ma-password" label="New Password *">
            <div style={{ position: 'relative', display: 'flex', alignItems: 'center' }}>
              <input
                id="ma-password"
                type={showPw ? 'text' : 'password'}
                value={password}
                onChange={e => setPassword(e.target.value)}
                placeholder="At least 6 characters"
                autoComplete="new-password"
                style={{ paddingRight: 44 }}
                required
              />
              <button
                type="button"
                onClick={() => setShowPw(v => !v)}
                aria-label={showPw ? 'Hide password' : 'Show password'}
                style={{
                  position: 'absolute', right: 12, background: 'none', border: 'none',
                  cursor: 'pointer', color: '#9ca3af', display: 'flex', alignItems: 'center',
                }}
              >
                {showPw ? <EyeOffIcon /> : <EyeIcon />}
              </button>
            </div>
          </Field>
          <SubmitBtn loading={loading} label="Activate Account" loadingLabel="Activating…" />
        </form>
      )}
    </AuthLayout>
  );
}

function Field({ id, label, children }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
      <label htmlFor={id} style={{ fontSize: 13.5, fontWeight: 500, color: '#374151', fontFamily: 'system-ui, sans-serif' }}>
        {label}
      </label>
      {children}
    </div>
  );
}

function SubmitBtn({ loading, label, loadingLabel }) {
  return (
    <button
      type="submit"
      disabled={loading}
      style={{
        width: '100%', padding: '12px 14px', background: '#1e40af', color: '#ffffff',
        border: 'none', borderRadius: 50, fontSize: 15, fontWeight: 600,
        cursor: loading ? 'not-allowed' : 'pointer', opacity: loading ? 0.65 : 1,
        fontFamily: 'system-ui, sans-serif', marginTop: 4,
      }}
    >
      {loading ? loadingLabel : label}
    </button>
  );
}
