import HarbourScene from './HarbourScene';
import './AuthShell.css';

export function AuthLogoRow() {
  return (
    <div className="auth-logo-row">
      <img src="/Customslogo.png" alt="Sri Lanka Customs" className="auth-logo-img" />
    </div>
  );
}

export default function AuthShell({ leftTop, compact = false, wideForm = false, children }) {
  return (
    <div className="auth-shell">
      <section className="auth-left">
        <div className={`auth-left-top${compact ? ' auth-left-top--compact' : ''}`}>
          {leftTop}
        </div>
        <HarbourScene />
      </section>

      <main className="auth-right">
        <div className={`auth-right-inner${wideForm ? ' auth-right-inner--wide' : ''}`}>
          {children}
        </div>
        <p className="auth-panel-footer">
          Customer Document Notification System · Sri Lanka Customs
        </p>
      </main>
    </div>
  );
}
