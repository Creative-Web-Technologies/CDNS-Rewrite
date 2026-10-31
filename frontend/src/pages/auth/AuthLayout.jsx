/*
 * AuthLayout — the shared two-column shell for every auth page in CDNS.
 *
 * Every auth page (Login, Signup, OTP verification, PasswordSetup,
 * MasterActivation, ForgotPassword) must wrap its content with this component.
 * Chirath's pages should import and wrap with AuthLayout exactly as LoginPage
 * does — do not build a separate left/right split per page. The shell handles
 * the hero image panel, logo, brand name, and responsive single-column collapse.
 *
 * Usage:
 *   import AuthLayout from '../AuthLayout';
 *   export default function MyPage() {
 *     return <AuthLayout><MyFormContent /></AuthLayout>;
 *   }
 */
import heroImg from '../../assets/hero.png';
import customsLogo from '../../assets/Sri_Lanka_Customs_logo.svg';
import './AuthLayout.css';

function LogoCrest() {
  return (
    <img className="auth-logo-crest" src={customsLogo} alt="Sri Lanka Customs logo" />
  );
}

export default function AuthLayout({ children }) {
  return (
    <div className="auth-shell">
      <div className="auth-left" aria-hidden="true">
        <div className="auth-hero-card">
          {/* TODO: swap with the real Sri Lanka Customs port photo from legacy wwwroot */}
          <img src={heroImg} alt="" className="auth-hero-img" />
        </div>
      </div>

      <div className="auth-right">
        <div className="auth-right-content">
          <div className="auth-brand">
            <LogoCrest />
            <p className="auth-brand-name">Sri Lanka Customs</p>
          </div>
          {children}
        </div>
      </div>
    </div>
  );
}
