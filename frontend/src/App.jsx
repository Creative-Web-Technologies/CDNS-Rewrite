import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext.jsx';
import { useAuth } from './contexts/useAuth';

// Auth pages
import LoginPage from './pages/auth/Login/LoginPage';
import SignupPage from './pages/auth/Signup/SignupPage';
import MasterActivationPage from './pages/auth/MasterActivation/MasterActivationPage';
import OtpVerificationPage from './pages/auth/OtpVerification/OtpVerificationPage';
import ForgotPasswordPage from './pages/auth/ForgotPassword/ForgotPasswordPage';

// Portal layout
import PortalLayout from './components/portal/PortalLayout';

// Portal pages
import MemberDashboard from './pages/member/MemberDashboard';
import CompanyDashboard from './pages/company/CompanyDashboard';
import ComingSoon from './pages/portal/ComingSoon';
import SubscriptionsPage from './pages/member/subscriptions/SubscriptionsPage';
import RequestsPage from './pages/member/requests/RequestsPage';
import NewRequestPage from './pages/member/requests/NewRequestPage';
import RequestDetailPage from './pages/member/requests/RequestDetailPage';
import MasterRequestsPage from './pages/company/requests/MasterRequestsPage';
import MasterRequestDetailPage from './pages/company/requests/MasterRequestDetailPage';
import MasterNewRequestPage from './pages/company/requests/MasterNewRequestPage';
import MasterSubscriptionsPage from './pages/company/subscriptions/MasterSubscriptionsPage';
import MasterSubscriptionDetailPage from './pages/company/subscriptions/MasterSubscriptionDetailPage';

// ── Loading screen ──────────────────────────────────────────────────────────
function LoadingScreen() {
  return (
    <div style={{
      display: 'flex', flexDirection: 'column', alignItems: 'center',
      justifyContent: 'center', minHeight: '100vh', gap: 16,
      background: '#f8fafc', fontFamily: 'system-ui, sans-serif',
    }}>
      <div style={{
        width: 36, height: 36,
        border: '3px solid #e2e8f0',
        borderTopColor: '#1e40af',
        borderRadius: '50%',
        animation: 'spin 0.8s linear infinite',
      }} />
      <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
      <span style={{ fontSize: 14, color: '#64748b' }}>Loading…</span>
    </div>
  );
}

// ── Route guards ────────────────────────────────────────────────────────────

// Redirects unauthenticated users to /login; authenticated users see their portal
function RedirectIfLoggedIn({ children }) {
  const { user, loading } = useAuth();
  if (loading) return <LoadingScreen />;
  if (user) return <Navigate to={`/${user.portal.toLowerCase()}/dashboard`} replace />;
  return children;
}

// Guards a portal route: unauthenticated → /login, wrong portal → own portal
function PortalGuard({ portal }) {
  const { user, loading } = useAuth();
  if (loading) return <LoadingScreen />;
  if (!user) return <Navigate to="/login" replace />;
  if (user.portal !== portal) {
    return <Navigate to={`/${user.portal.toLowerCase()}/dashboard`} replace />;
  }
  return <PortalLayout portal={portal} />;
}

// Catch-all: logged in → own portal dashboard, otherwise → /login
function SmartRedirect() {
  const { user, loading } = useAuth();
  if (loading) return <LoadingScreen />;
  if (user) return <Navigate to={`/${user.portal.toLowerCase()}/dashboard`} replace />;
  return <Navigate to="/login" replace />;
}

// ── App ─────────────────────────────────────────────────────────────────────
function AppRoutes() {
  return (
    <Routes>
      {/* Auth routes — redirect to portal if already logged in */}
      <Route path="/login" element={
        <RedirectIfLoggedIn><LoginPage /></RedirectIfLoggedIn>
      } />
      <Route path="/signup" element={
        <RedirectIfLoggedIn><SignupPage /></RedirectIfLoggedIn>
      } />
      {/* Activation/OTP are transient; don't redirect mid-flow */}
      <Route path="/master-activation" element={<MasterActivationPage />} />
      <Route path="/otp-verification"  element={<OtpVerificationPage />} />
      <Route path="/forgot-password"   element={<ForgotPasswordPage />} />

      {/* Member Portal */}
      <Route path="/member" element={<PortalGuard portal="Member" />}>
        <Route index element={<Navigate to="dashboard" replace />} />
        <Route path="dashboard"     element={<MemberDashboard />} />
        <Route path="cusdecs"       element={<ComingSoon title="CusDecs" />} />
        <Route path="containers"    element={<ComingSoon title="Containers" />} />
        <Route path="subscriptions" element={<SubscriptionsPage />} />
        <Route path="requests"      element={<RequestsPage />} />
        <Route path="requests/new"  element={<NewRequestPage />} />
        <Route path="requests/:id"  element={<RequestDetailPage />} />
        <Route path="cusdec/:subscriptionId"     element={<ComingSoon title="CusDec workspace" />} />
        <Route path="containers/:subscriptionId" element={<ComingSoon title="Containers" />} />
        <Route path="notifications" element={<ComingSoon title="Notifications" />} />
        <Route path="support"       element={<ComingSoon title="Support" />} />
        <Route path="profile"       element={<ComingSoon title="Profile" />} />
      </Route>

      {/* Company Portal */}
      <Route path="/company" element={<PortalGuard portal="Company" />}>
        <Route index element={<Navigate to="dashboard" replace />} />
        <Route path="dashboard"           element={<CompanyDashboard />} />
        <Route path="cusdecs"             element={<ComingSoon title="Company CusDecs" />} />
        <Route path="members"             element={<ComingSoon title="Members & Subscriptions" />} />
        <Route path="requests"            element={<MasterRequestsPage />} />
        <Route path="requests/new"        element={<MasterNewRequestPage />} />
        <Route path="requests/:id"        element={<MasterRequestDetailPage />} />
        <Route path="subscriptions"       element={<MasterSubscriptionsPage />} />
        <Route path="subscriptions/:id"   element={<MasterSubscriptionDetailPage />} />
        <Route path="payments"            element={<ComingSoon title="Payments" />} />
        <Route path="notifications"       element={<ComingSoon title="Notifications" />} />
        <Route path="support"             element={<ComingSoon title="Support" />} />
        <Route path="profile"             element={<ComingSoon title="Profile" />} />
      </Route>

      {/* Catch-all */}
      <Route path="*" element={<SmartRedirect />} />
    </Routes>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </BrowserRouter>
  );
}
