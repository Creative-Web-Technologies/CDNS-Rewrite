import { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import AuthLayout from '../AuthLayout';
import { apiPost } from '../../../lib/api';
import { useAuth } from '../../../contexts/useAuth';
import '../Login/LoginPage.css';
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

// Field validation mirroring server ProfileValidator rules
const PROFILE_FIELD_MESSAGES = {
  InvalidName:        'Valid name required (letters, spaces, hyphens, apostrophes, dots; max 50 chars).',
  InvalidMobile:      'Valid mobile required (8–15 digits, optional leading +).',
  InvalidNic:         'Valid Sri Lankan NIC required (9 digits + V/X, or 12 digits).',
  InvalidDesignation: 'Designation is required (max 100 characters).',
};

function validateProfileField(name, value) {
  switch (name) {
    case 'firstName':
    case 'lastName': {
      const v = (value || '').trim();
      if (!v || v.length > 50 || !/^[A-Za-z\s\-'.]+$/.test(v)) return PROFILE_FIELD_MESSAGES.InvalidName;
      return null;
    }
    case 'mobile': {
      const mob = (value || '').replace(/\s/g, '');
      if (!/^\+?\d{8,15}$/.test(mob)) return PROFILE_FIELD_MESSAGES.InvalidMobile;
      return null;
    }
    case 'nic': {
      const v = (value || '').trim();
      if (!/^\d{9}[VvXx]$/i.test(v) && !/^\d{12}$/.test(v)) return PROFILE_FIELD_MESSAGES.InvalidNic;
      return null;
    }
    case 'designation': {
      const v = (value || '').trim();
      if (!v || v.length > 100) return PROFILE_FIELD_MESSAGES.InvalidDesignation;
      return null;
    }
    default: return null;
  }
}

export default function MasterActivationPage() {
  const [params] = useSearchParams();
  const activationToken = params.get('token') ?? '';
  const navigate = useNavigate();
  const { refreshUser } = useAuth();

  const [step, setStep] = useState('profile'); // 'profile' | 'password'
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [fieldErrors, setFieldErrors] = useState({});

  // Profile form
  const [profile, setProfile] = useState({
    firstName: '', lastName: '', mobile: '', nic: '', designation: '',
  });

  // Password form
  const [password, setPassword] = useState('');
  const [showPw, setShowPw] = useState(false);
  const [confirmPw, setConfirmPw] = useState('');
  const [showConfirmPw, setShowConfirmPw] = useState(false);

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
    // Client-side validation first
    const profileFields = ['firstName', 'lastName', 'mobile', 'nic', 'designation'];
    const errors = {};
    profileFields.forEach(f => {
      const err = validateProfileField(f, profile[f]);
      if (err) errors[f] = err;
    });
    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors);
      return;
    }
    setFieldErrors({});
    setLoading(true);
    setError('');
    try {
      await apiPost('/api/auth/master-activation/profile', {
        activationToken,
        ...profile,
      });
      setStep('password');
    } catch (err) {
      if (err.code === 'InvalidActivationToken') {
        setError('This activation link is invalid or has already been used.');
      } else if (err.field) {
        const msg = PROFILE_FIELD_MESSAGES[err.code] || 'Invalid value.';
        setFieldErrors({ [err.field]: msg });
      } else {
        setError('Could not save profile. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  }

  async function handlePassword(e) {
    e.preventDefault();
    if (!checkPassword(password).valid) {
      setError('Password does not meet the requirements below.');
      return;
    }
    if (password !== confirmPw) {
      setError('Passwords do not match.');
      return;
    }
    setLoading(true);
    setError('');
    try {
      const data = await apiPost('/api/auth/master-activation/password', {
        activationToken,
        password,
      });
      await refreshUser();
      navigate(`/${data.portal.toLowerCase()}/dashboard`);
    } catch (err) {
      setError(
        err.code === 'InvalidActivationToken'
          ? 'This activation link is invalid or has already been used.'
          : err.code === 'WeakPassword'
          ? 'Password does not meet the requirements. Please choose a stronger password.'
          : 'Could not set password. The activation link may have expired.'
      );
    } finally {
      setLoading(false);
    }
  }

  function handleProfileChange(e) {
    const { name, value } = e.target;
    setProfile(p => ({ ...p, [name]: value }));
    if (fieldErrors[name]) setFieldErrors(prev => ({ ...prev, [name]: null }));
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
            <Field id="ma-firstName" label="First Name *" error={fieldErrors.firstName}>
              <input id="ma-firstName" name="firstName" type="text" value={profile.firstName}
                onChange={handleProfileChange} required autoComplete="given-name"
                aria-invalid={fieldErrors.firstName ? 'true' : 'false'} />
            </Field>
            <Field id="ma-lastName" label="Last Name *" error={fieldErrors.lastName}>
              <input id="ma-lastName" name="lastName" type="text" value={profile.lastName}
                onChange={handleProfileChange} required autoComplete="family-name"
                aria-invalid={fieldErrors.lastName ? 'true' : 'false'} />
            </Field>
            <Field id="ma-mobile" label="Mobile *" error={fieldErrors.mobile}>
              <input id="ma-mobile" name="mobile" type="tel" value={profile.mobile}
                onChange={handleProfileChange} autoComplete="tel"
                aria-invalid={fieldErrors.mobile ? 'true' : 'false'} />
            </Field>
            <Field id="ma-nic" label="NIC *" error={fieldErrors.nic}>
              <input id="ma-nic" name="nic" type="text" value={profile.nic}
                onChange={handleProfileChange}
                aria-invalid={fieldErrors.nic ? 'true' : 'false'} />
            </Field>
          </div>
          <Field id="ma-designation" label="Designation *" error={fieldErrors.designation}>
            <input id="ma-designation" name="designation" type="text" value={profile.designation}
              onChange={handleProfileChange}
              aria-invalid={fieldErrors.designation ? 'true' : 'false'} />
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
                placeholder="At least 8 characters"
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
            <PasswordRequirements value={password} />
          </Field>
          <Field id="ma-confirm-password" label="Confirm Password *">
            <div style={{ position: 'relative', display: 'flex', alignItems: 'center' }}>
              <input
                id="ma-confirm-password"
                type={showConfirmPw ? 'text' : 'password'}
                value={confirmPw}
                onChange={e => setConfirmPw(e.target.value)}
                placeholder="Re-enter your password"
                autoComplete="new-password"
                style={{ paddingRight: 44 }}
              />
              <button
                type="button"
                onClick={() => setShowConfirmPw(v => !v)}
                aria-label={showConfirmPw ? 'Hide password' : 'Show password'}
                style={{
                  position: 'absolute', right: 12, background: 'none', border: 'none',
                  cursor: 'pointer', color: '#9ca3af', display: 'flex', alignItems: 'center',
                }}
              >
                {showConfirmPw ? <EyeOffIcon /> : <EyeIcon />}
              </button>
            </div>
            {confirmPw.length > 0 && confirmPw !== password && (
              <p className="field-error">Passwords do not match.</p>
            )}
          </Field>
          <SubmitBtn loading={loading} label="Activate Account" loadingLabel="Activating…" />
        </form>
      )}
    </AuthLayout>
  );
}

function Field({ id, label, error, children }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
      <label htmlFor={id} style={{ fontSize: 13.5, fontWeight: 500, color: '#374151', fontFamily: 'system-ui, sans-serif' }}>
        {label}
      </label>
      {children}
      {error && (
        <p role="alert" style={{ color: '#b91c1c', fontSize: 12, margin: 0, fontFamily: 'system-ui, sans-serif' }}>
          {error}
        </p>
      )}
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
