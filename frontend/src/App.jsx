import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import AdminDashboardPage from './pages/admin/AdminDashboardPage';
import ForgotPasswordPage from './pages/auth/ForgotPassword/ForgotPasswordPage';
import LoginPage from './pages/auth/Login/LoginPage';
import MasterActivationPage from './pages/auth/MasterActivation/MasterActivationPage';
import OtpVerificationPage from './pages/auth/OtpVerification/OtpVerificationPage';
import PasswordSetupPage from './pages/auth/PasswordSetup/PasswordSetupPage';
import SignupPage from './pages/auth/Signup/SignupPage';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/admin/dashboard" element={<AdminDashboardPage />} />
        <Route path="/signup" element={<SignupPage />} />
        <Route path="/otp-verification" element={<OtpVerificationPage />} />
        <Route path="/password-setup" element={<PasswordSetupPage />} />
        <Route path="/master-activation" element={<MasterActivationPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
