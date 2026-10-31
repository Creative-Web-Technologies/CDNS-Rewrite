import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { apiGet } from '../../lib/api';
import './AdminDashboardPage.css';

export default function AdminDashboardPage() {
  const navigate = useNavigate();
  const [account, setAccount] = useState(null);
  const [error, setError] = useState(null);

  function handleLogout() {
    localStorage.removeItem('cdns_admin_token');
    navigate('/login', { replace: true });
  }

  useEffect(() => {
    apiGet('/api/auth/me')
      .then(setAccount)
      .catch(err => {
        if (err.status === 401) {
          navigate('/login', { replace: true });
          return;
        }
        setError(err.message || 'Unable to load your account.');
      });
  }, [navigate]);

  return (
    <main className="admin-dashboard">
      <header className="admin-dashboard-header">
        <h1>Admin Dashboard</h1>
        <button className="admin-logout-button" type="button" onClick={handleLogout}>
          Log out
        </button>
      </header>
      {error && <p role="alert">{error}</p>}
      {account && (
        <section>
          <p>Signed in as {account.email}</p>
          <p>Roles: {account.roles.join(', ')}</p>
        </section>
      )}
    </main>
  );
}
