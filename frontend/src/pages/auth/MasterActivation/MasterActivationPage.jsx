import { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { motion } from 'motion/react';
import { Eye, EyeOff } from 'lucide-react';
import AuthShell, { AuthLogoRow } from '../AuthShell';
import { apiPost } from '../../../lib/api';
import { useAuth } from '../../../contexts/useAuth';
import { checkPassword } from '../../../utils/passwordPolicy';
import PasswordRequirements from '../../../components/auth/PasswordRequirements';

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

  const [profile, setProfile] = useState({
    firstName: '', lastName: '', mobile: '', nic: '', designation: '',
  });

  const [password, setPassword] = useState('');
  const [showPw, setShowPw] = useState(false);
  const [confirmPw, setConfirmPw] = useState('');
  const [showConfirmPw, setShowConfirmPw] = useState(false);

  const leftTop = (
    <>
      <Rise delay={0}><AuthLogoRow /></Rise>
      <div className="auth-left-copy">
        <Rise delay={0.08}>
          <h1>Company administrator activation.</h1>
        </Rise>
        <Rise delay={0.16}>
          <p>
            Complete your profile and set your password to activate your CDNS
            Master account.
          </p>
        </Rise>
      </div>
    </>
  );

  if (!activationToken) {
    return (
      <AuthShell leftTop={leftTop}>
        <Rise delay={0}>
          <div className="auth-heading-block">
            <h2>Invalid Activation Link</h2>
            <p>No activation token found. Please log in again to restart the activation flow.</p>
          </div>
        </Rise>
      </AuthShell>
    );
  }

  async function handleProfile(e) {
    e.preventDefault();
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
      } else if (!err.status || err.status >= 500) {
        setError('Something went wrong. Check your connection and try again.');
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
          : !err.status || err.status >= 500
          ? 'Something went wrong. Check your connection and try again.'
          : 'Could not set password. The activation link may have expired.',
      );
    } finally {
      setLoading(false);
    }
  }

  function handleProfileChange(e) {
    const { name, value } = e.target;
    setProfile(p => ({ ...p, [name]: value }));
    if (fieldErrors[name]) setFieldErrors(prev => ({ ...prev, [name]: null }));
    if (error) setError('');
  }

  // ── Step indicator ──
  const stepNum = step === 'profile' ? 1 : 2;
  const stepIndicator = (
    <div className="ma-steps">
      {['Complete Profile', 'Set Password'].map((label, i) => {
        const n = i + 1;
        const done = n < stepNum;
        const active = n === stepNum;
        return (
          <span
            key={label}
            className={`ma-step${active ? ' ma-step--active' : done ? ' ma-step--done' : ''}`}
          >
            {n}. {label}{i < 1 ? ' →' : ''}
          </span>
        );
      })}
    </div>
  );

  return (
    <AuthShell leftTop={leftTop}>
      <Rise delay={0}>
        {stepIndicator}
      </Rise>

      <Rise delay={0.06}>
        <div className="auth-heading-block">
          <h2>{step === 'profile' ? 'Complete Your Profile' : 'Set Your Password'}</h2>
          <p>
            {step === 'profile'
              ? 'Enter your personal details to activate your company administrator account.'
              : 'Create a secure password to finish activation.'}
          </p>
        </div>
      </Rise>

      {error && (
        <Rise delay={0}>
          <div className="auth-banner auth-banner--error cd-shake" role="alert">
            <span>{error}</span>
          </div>
        </Rise>
      )}

      {step === 'profile' && (
        <form onSubmit={handleProfile} noValidate style={{ display: 'contents' }}>
          <div className="ma-profile-grid">
            <MaField id="ma-firstName" label="First name" error={fieldErrors.firstName}>
              <input
                id="ma-firstName" name="firstName" type="text"
                className={`auth-input${fieldErrors.firstName ? ' auth-input--error' : ''}`}
                value={profile.firstName} onChange={handleProfileChange}
                autoComplete="given-name" aria-invalid={!!fieldErrors.firstName}
              />
            </MaField>
            <MaField id="ma-lastName" label="Last name" error={fieldErrors.lastName}>
              <input
                id="ma-lastName" name="lastName" type="text"
                className={`auth-input${fieldErrors.lastName ? ' auth-input--error' : ''}`}
                value={profile.lastName} onChange={handleProfileChange}
                autoComplete="family-name" aria-invalid={!!fieldErrors.lastName}
              />
            </MaField>
            <MaField id="ma-mobile" label="Mobile" error={fieldErrors.mobile}>
              <input
                id="ma-mobile" name="mobile" type="tel"
                className={`auth-input${fieldErrors.mobile ? ' auth-input--error' : ''}`}
                value={profile.mobile} onChange={handleProfileChange}
                autoComplete="tel" aria-invalid={!!fieldErrors.mobile}
              />
            </MaField>
            <MaField id="ma-nic" label="NIC" error={fieldErrors.nic}>
              <input
                id="ma-nic" name="nic" type="text"
                className={`auth-input${fieldErrors.nic ? ' auth-input--error' : ''}`}
                value={profile.nic} onChange={handleProfileChange}
                aria-invalid={!!fieldErrors.nic}
              />
            </MaField>
          </div>
          <MaField id="ma-designation" label="Designation" error={fieldErrors.designation}>
            <input
              id="ma-designation" name="designation" type="text"
              className={`auth-input${fieldErrors.designation ? ' auth-input--error' : ''}`}
              value={profile.designation} onChange={handleProfileChange}
              aria-invalid={!!fieldErrors.designation}
            />
          </MaField>
          <button type="submit" className="auth-btn" disabled={loading}>
            {loading && <span className="auth-btn-spinner" />}
            {loading ? 'Saving…' : 'Save & Continue'}
          </button>
        </form>
      )}

      {step === 'password' && (
        <form onSubmit={handlePassword} noValidate style={{ display: 'contents' }}>
          <MaField id="ma-password" label="New password">
            <div className="auth-pw-wrap">
              <input
                id="ma-password"
                className="auth-input"
                type={showPw ? 'text' : 'password'}
                value={password}
                onChange={e => { setPassword(e.target.value); if (error) setError(''); }}
                placeholder="At least 8 characters"
                autoComplete="new-password"
              />
              <button
                type="button" className="auth-pw-toggle"
                onClick={() => setShowPw(v => !v)}
                aria-label={showPw ? 'Hide password' : 'Show password'}
              >
                {showPw ? <EyeOff size={20} /> : <Eye size={20} />}
              </button>
            </div>
            <PasswordRequirements value={password} />
          </MaField>
          <MaField id="ma-confirm-password" label="Confirm password">
            <div className="auth-pw-wrap">
              <input
                id="ma-confirm-password"
                className="auth-input"
                type={showConfirmPw ? 'text' : 'password'}
                value={confirmPw}
                onChange={e => setConfirmPw(e.target.value)}
                placeholder="Re-enter your password"
                autoComplete="new-password"
              />
              <button
                type="button" className="auth-pw-toggle"
                onClick={() => setShowConfirmPw(v => !v)}
                aria-label={showConfirmPw ? 'Hide password' : 'Show password'}
              >
                {showConfirmPw ? <EyeOff size={20} /> : <Eye size={20} />}
              </button>
            </div>
            {confirmPw.length > 0 && confirmPw !== password && (
              <p className="auth-field-error" role="alert">Passwords do not match.</p>
            )}
          </MaField>
          <button type="submit" className="auth-btn" disabled={loading}>
            {loading && <span className="auth-btn-spinner" />}
            {loading ? 'Activating…' : 'Activate Account'}
          </button>
        </form>
      )}
    </AuthShell>
  );
}

function MaField({ id, label, error, children }) {
  return (
    <div>
      <label className="auth-label" htmlFor={id}>{label}</label>
      {children}
      {error && <p className="auth-field-error" role="alert">{error}</p>}
    </div>
  );
}
