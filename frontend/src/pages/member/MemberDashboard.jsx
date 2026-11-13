import { useEffect, useState } from 'react';
import { useAuth } from '../../contexts/useAuth';
import { apiGet } from '../../lib/api';
import '../portal/Dashboard.css';

function ActivityIcon() {
  return (
    <svg className="dash-empty__icon" viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
      <circle cx="24" cy="24" r="22" stroke="currentColor" strokeWidth="2"/>
      <path d="M16 24h16M24 16v16" stroke="currentColor" strokeWidth="2" strokeLinecap="round"/>
    </svg>
  );
}

function StatIcon({ children, variant }) {
  return (
    <div className={`stat-card__icon stat-card__icon--${variant}`}>
      {children}
    </div>
  );
}

function CheckIcon({ done }) {
  return (
    <span className={`dash-checklist__dot${done ? ' dash-checklist__dot--done' : ''}`}>
      {done && (
        <svg width="10" height="10" viewBox="0 0 12 12" fill="none" aria-hidden="true">
          <polyline points="2,6 5,9 10,3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"/>
        </svg>
      )}
    </span>
  );
}

export default function MemberDashboard() {
  const { user } = useAuth();
  const [summary, setSummary] = useState(null);
  const [loadErr, setLoadErr] = useState(false);

  useEffect(() => {
    apiGet('/api/member/dashboard-summary')
      .then(setSummary)
      .catch(() => setLoadErr(true));
  }, []);

  if (!summary && !loadErr) {
    return <div className="dash-loading">Loading dashboard…</div>;
  }

  const s = summary ?? {};

  return (
    <div>
      {/* Header */}
      <div className="dash-header">
        <h1 className="dash-welcome">Welcome back, {user?.firstName}!</h1>
        <p className="dash-subtitle">Here's what's happening with your CDNS account.</p>
      </div>

      {/* Stats */}
      <div className="dash-cards">
        <div className="stat-card">
          <StatIcon variant="blue">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
              <path d="M22 11.08V12a10 10 0 11-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/>
            </svg>
          </StatIcon>
          <div className="stat-card__label">Active Subscriptions</div>
          <div className="stat-card__value">{s.activeSubscriptions ?? 0}</div>
        </div>

        <div className="stat-card">
          <StatIcon variant="indigo">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
              <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/>
              <polyline points="14 2 14 8 20 8"/>
            </svg>
          </StatIcon>
          <div className="stat-card__label">Claimed CusDecs</div>
          <div className="stat-card__value">{s.claimedCusDecs ?? 0}</div>
        </div>

        <div className="stat-card">
          <StatIcon variant="amber">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
              <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9"/>
              <path d="M13.73 21a2 2 0 01-3.46 0"/>
            </svg>
          </StatIcon>
          <div className="stat-card__label">Unread Notifications</div>
          <div className="stat-card__value">{s.unreadNotifications ?? 0}</div>
        </div>

        <div className="stat-card">
          <StatIcon variant="purple">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
              <circle cx="12" cy="12" r="10"/>
              <polyline points="12 6 12 12 16 14"/>
            </svg>
          </StatIcon>
          <div className="stat-card__label">Pending Requests</div>
          <div className="stat-card__value">{s.pendingRequests ?? 0}</div>
        </div>
      </div>

      {/* Grid: recent activity + getting started */}
      <div className="dash-grid">
        <div className="dash-panel">
          <div className="dash-panel__header">
            <h2 className="dash-panel__title">Recent Activity</h2>
          </div>
          <div className="dash-panel__body">
            <div className="dash-empty">
              <ActivityIcon />
              <p className="dash-empty__title">No recent activity</p>
              <p className="dash-empty__text">
                Your document and subscription activity will appear here.
              </p>
            </div>
          </div>
        </div>

        <div className="dash-panel">
          <div className="dash-panel__header">
            <h2 className="dash-panel__title">Getting Started</h2>
          </div>
          <div className="dash-panel__body">
            <ul className="dash-checklist">
              <li className="dash-checklist__item">
                <CheckIcon done />
                <span>Create your CDNS account</span>
              </li>
              <li className="dash-checklist__item">
                <CheckIcon done={false} />
                <span>Complete your profile</span>
              </li>
              <li className="dash-checklist__item">
                <CheckIcon done={false} />
                <span>Subscribe to a company</span>
              </li>
              <li className="dash-checklist__item">
                <CheckIcon done={false} />
                <span>Track your first CusDec</span>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}
