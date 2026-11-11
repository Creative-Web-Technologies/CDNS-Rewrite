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

export default function CompanyDashboard() {
  const { user } = useAuth();
  const [summary, setSummary] = useState(null);
  const [loadErr, setLoadErr] = useState(false);

  useEffect(() => {
    apiGet('/api/company/dashboard-summary')
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
        <p className="dash-subtitle">Company administrator view — CDNS Document Notification System.</p>
      </div>

      {/* Stats */}
      <div className="dash-cards">
        <div className="stat-card">
          <StatIcon variant="indigo">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
              <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/>
              <circle cx="9" cy="7" r="4"/>
              <path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/>
            </svg>
          </StatIcon>
          <div className="stat-card__label">Company Members</div>
          <div className="stat-card__value">{s.companyMembers ?? 0}</div>
        </div>

        <div className="stat-card">
          <StatIcon variant="amber">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
              <circle cx="12" cy="12" r="10"/>
              <polyline points="12 6 12 12 16 14"/>
            </svg>
          </StatIcon>
          <div className="stat-card__label">Pending Requests</div>
          <div className="stat-card__value">{s.pendingSubscriptionRequests ?? 0}</div>
        </div>

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
          <StatIcon variant="purple">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
              <rect x="1" y="4" width="22" height="16" rx="2" ry="2"/>
              <line x1="1" y1="10" x2="23" y2="10"/>
            </svg>
          </StatIcon>
          <div className="stat-card__label">Payments Due</div>
          <div className="stat-card__value">{s.paymentsDue ?? 0}</div>
        </div>
      </div>

      {/* Grid: recent activity + eReg status */}
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
                Member subscriptions, CusDec events, and payment activity will appear here.
              </p>
            </div>
          </div>
        </div>

        <div className="dash-panel">
          <div className="dash-panel__header">
            <h2 className="dash-panel__title">Account Status</h2>
          </div>
          <div className="dash-panel__body">
            <ul className="dash-checklist">
              <li className="dash-checklist__item">
                <span className="dash-checklist__dot dash-checklist__dot--done">
                  <svg width="10" height="10" viewBox="0 0 12 12" fill="none" aria-hidden="true">
                    <polyline points="2,6 5,9 10,3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"/>
                  </svg>
                </span>
                <span>eReg verification complete</span>
              </li>
              <li className="dash-checklist__item">
                <span className="dash-checklist__dot dash-checklist__dot--done">
                  <svg width="10" height="10" viewBox="0 0 12 12" fill="none" aria-hidden="true">
                    <polyline points="2,6 5,9 10,3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"/>
                  </svg>
                </span>
                <span>Company profile activated</span>
              </li>
              <li className="dash-checklist__item">
                <span className="dash-checklist__dot"/>
                <span>Add company members</span>
              </li>
              <li className="dash-checklist__item">
                <span className="dash-checklist__dot"/>
                <span>Set up subscription plan</span>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}
