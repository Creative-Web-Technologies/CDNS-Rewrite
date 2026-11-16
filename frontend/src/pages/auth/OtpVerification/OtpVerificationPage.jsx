import AuthShell, { AuthLogoRow } from '../AuthShell';

// Stub — to be implemented. Layout uses AuthShell to match the rest of the auth UI.
export default function OtpVerificationPage() {
  const leftTop = <AuthLogoRow />;
  return (
    <AuthShell leftTop={leftTop}>
      <div className="auth-heading-block">
        <h2>Verify OTP</h2>
        <p>OTP verification form — coming soon.</p>
      </div>
    </AuthShell>
  );
}
