import { useState } from 'react';
import { Link } from 'react-router-dom';
import AuthShell from '../AuthShell';
import './Signup.css';

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

export default function Signup() {
  // --- States for the Form ---
  const [formData, setFormData] = useState({
    tin: '',
    accountType: 'CHA Agent (Declarant)',
    firstName: '',
    lastName: '',
    email: '',
    mobile: '',
    nic: '',
    designation: ''
  });

  // --- States for the App Flow ---
  const [step, setStep] = useState('SIGNUP'); // 'SIGNUP', 'OTP', 'PASSWORD', 'SUCCESS'
  const [tempToken, setTempToken] = useState('');
  const [otpInput, setOtpInput] = useState('');
  const [passwordInput, setPasswordInput] = useState('');
  const [showPassword, setShowPassword] = useState(false);

  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState(''); // Replaced raw JSON response with clean error string

  // --- Form Handlers ---
  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setErrorMsg('');

    try {
      const res = await fetch('http://localhost:5030/api/signup/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({
            tin: formData.tin,
            firstName: formData.firstName,
            lastName: formData.lastName,
            email: formData.email,
            mobile: formData.mobile,
            nic: formData.nic,
            designation: formData.designation
        })
      });

      const data = await res.json();

      // If successful, save the token and move to the OTP screen!
      if (res.ok && data.success) {
          setTempToken(data.tempSignupToken);
          setStep('OTP');
      } else {
          setErrorMsg(data.message || "Registration failed. Please check your details.");
      }

    } catch (error) {
      setErrorMsg("Network error. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  // --- OTP Submit Handler ---
  const handleVerifyOtp = async () => {
    if (!otpInput) {
        setErrorMsg("Please enter the OTP.");
        return;
    }

    setLoading(true);
    setErrorMsg('');
    try {
        const res = await fetch('http://localhost:5030/api/signup/verify-otp', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ tempSignupToken: tempToken, otp: parseInt(otpInput) })
        });

        const data = await res.json();

        if (res.ok) {
            setStep('PASSWORD');
        } else {
            setErrorMsg(data.message || "Invalid OTP. Please try again.");
        }
    } catch (error) {
        setErrorMsg("Network error. Please try again.");
    } finally {
        setLoading(false);
    }
  };

  // --- Set Password Handler ---
  const handleSetPassword = async () => {
     if (!tempToken) {
        setErrorMsg("Session expired. Please start over.");
        setStep('SIGNUP');
        return;
    }

    if (passwordInput.length < 6) {
        setErrorMsg("Password must be at least 6 characters.");
        return;
    }

    setLoading(true);
    setErrorMsg('');

    try {
        const res = await fetch('http://localhost:5030/api/signup/set-password', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({
                tempSignupToken: tempToken,
                password: passwordInput
            })
        });

        const data = await res.json();

        if (res.ok) {
            setStep('SUCCESS');
        } else {
            setErrorMsg(data.message || "Failed to set password.");
        }
    } catch (error) {
        setErrorMsg("Network error. Please try again.");
    } finally {
        setLoading(false);
    }
  };


  // ==========================================
  // UI RENDER LOGIC
  // ==========================================

  return (
    <AuthShell>
      <div className="signup-nav-row">
        <Link to="/login" className="portal-back-link">← Back to Login</Link>
      </div>

      {/* 1. SUCCESS SCREEN */}
      {step === 'SUCCESS' && (
        <div className="form-wrapper form-wrapper--centered">
          <div className="header">
            <div className="logo-placeholder">Logo</div>
            <h2>Sri Lanka Customs</h2>
            <h1 className="success-title">Account Created!</h1>
            <p>Your password has been set successfully. You are now ready to use the Document Notification System.</p>
          </div>

          <button
            className="submit-btn"
            onClick={() => window.location.href = '/dashboard'}
          >
            Go to Dashboard
          </button>
        </div>
      )}

      {/* 2. OTP SCREEN */}
      {step === 'OTP' && (
        <div className="form-wrapper form-wrapper--centered">
          <div className="header">
            <h2>Sri Lanka Customs</h2>
            <h1>Verify Your Email</h1>
            <p>We sent a 6-digit code to your email.</p>
          </div>

          <div className="input-group">
            <label>Enter OTP</label>
            <input
              type="text"
              className="otp-input"
              value={otpInput}
              onChange={(e) => setOtpInput(e.target.value)}
              placeholder="Enter 6-digit code"
            />
          </div>

          {errorMsg && <div className="error-banner" role="alert">{errorMsg}</div>}

          <button className="submit-btn" onClick={handleVerifyOtp} disabled={loading}>
            {loading ? 'Verifying...' : 'Verify OTP'}
          </button>
        </div>
      )}

      {/* 3. PASSWORD SCREEN */}
      {step === 'PASSWORD' && (
        <div className="form-wrapper form-wrapper--centered">
          <div className="header">
            <div className="logo-placeholder">Logo</div>
            <h2>Sri Lanka Customs</h2>
            <h1>Set Your Password</h1>
            <p>Please create a secure password for your account.</p>
          </div>

          <div className="input-group">
            <label>New Password *</label>
            <div className="pw-wrap">
              <input
                type={showPassword ? 'text' : 'password'}
                value={passwordInput}
                onChange={(e) => setPasswordInput(e.target.value)}
                placeholder="Enter at least 6 characters"
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

          {errorMsg && <div className="error-banner" role="alert">{errorMsg}</div>}

          <button className="submit-btn" onClick={handleSetPassword} disabled={loading}>
            {loading ? 'Saving...' : 'Complete Signup'}
          </button>
        </div>
      )}

      {/* 4. SIGNUP FORM SCREEN (Default) */}
      {step === 'SIGNUP' && (
        <div className="form-wrapper">
          <div className="header">
            <div className="logo-placeholder">Logo</div>
            <h2>Sri Lanka Customs</h2>
            <h1>Document Notification System</h1>
            <p>Sign Up</p>
          </div>

          <form onSubmit={handleSubmit} className="signup-form">
            <div className="input-grid">
              <div className="input-group">
                <label>TIN Number (9 Digit) *</label>
                <input required type="text" name="tin" value={formData.tin} onChange={handleChange} placeholder="TIN Number (9 Digit)" />
              </div>

              <div className="input-group">
                <label>Account Type *</label>
                <select name="accountType" value={formData.accountType} onChange={handleChange}>
                  <option>CHA Agent (Declarant)</option>
                  <option>Importer</option>
                  <option>Exporter</option>
                </select>
              </div>

              <div className="input-group">
                <label>First Name *</label>
                <input required type="text" name="firstName" value={formData.firstName} onChange={handleChange} placeholder="First Name" />
              </div>

              <div className="input-group">
                <label>Last Name *</label>
                <input required type="text" name="lastName" value={formData.lastName} onChange={handleChange} placeholder="Last Name" />
              </div>

              <div className="input-group">
                <label>Email *</label>
                <input required type="email" name="email" value={formData.email} onChange={handleChange} placeholder="Email" />
              </div>

              <div className="input-group">
                <label>Mobile *</label>
                <input required type="text" name="mobile" value={formData.mobile} onChange={handleChange} placeholder="Mobile" />
              </div>

              <div className="input-group">
                <label>NIC *</label>
                <input required type="text" name="nic" value={formData.nic} onChange={handleChange} placeholder="NIC" />
              </div>

              <div className="input-group">
                <label>Designation *</label>
                <input required type="text" name="designation" value={formData.designation} onChange={handleChange} placeholder="Designation" />
              </div>
            </div>

            {errorMsg && <div className="error-banner" role="alert">{errorMsg}</div>}

            <button type="submit" disabled={loading} className="submit-btn">
              {loading ? 'Checking...' : 'Next'}
            </button>
          </form>

          <p className="login-link">
            Already have an account? <Link to="/login">Back to Login</Link>
          </p>
        </div>
      )}
    </AuthShell>
  );
}
