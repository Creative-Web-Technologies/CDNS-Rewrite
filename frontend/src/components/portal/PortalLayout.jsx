import { useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/useAuth';
import './PortalLayout.css';

// ── Inline SVG icons ────────────────────────────────────────────────────────
function IconGrid() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/>
      <rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/>
    </svg>
  );
}
function IconDoc() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/>
      <polyline points="14 2 14 8 20 8"/>
    </svg>
  );
}
function IconCheck() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M22 11.08V12a10 10 0 11-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/>
    </svg>
  );
}
function IconBell() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9"/>
      <path d="M13.73 21a2 2 0 01-3.46 0"/>
    </svg>
  );
}
function IconUser() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2"/>
      <circle cx="12" cy="7" r="4"/>
    </svg>
  );
}
function IconUsers() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/>
      <circle cx="9" cy="7" r="4"/>
      <path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/>
    </svg>
  );
}
function IconCard() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <rect x="1" y="4" width="22" height="16" rx="2" ry="2"/>
      <line x1="1" y1="10" x2="23" y2="10"/>
    </svg>
  );
}
function IconMenu() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <line x1="3" y1="12" x2="21" y2="12"/>
      <line x1="3" y1="6" x2="21" y2="6"/>
      <line x1="3" y1="18" x2="21" y2="18"/>
    </svg>
  );
}

// ── Nav configuration per portal ───────────────────────────────────────────
const MEMBER_NAV = [
  { path: '/member/dashboard',      label: 'Dashboard',      Icon: IconGrid  },
  { path: '/member/cusdecs',        label: 'My CusDecs',     Icon: IconDoc   },
  { path: '/member/subscriptions',  label: 'Subscriptions',  Icon: IconCheck },
  { path: '/member/notifications',  label: 'Notifications',  Icon: IconBell  },
  { path: '/member/profile',        label: 'Profile',        Icon: IconUser  },
];

const COMPANY_NAV = [
  { path: '/company/dashboard',     label: 'Dashboard',              Icon: IconGrid  },
  { path: '/company/cusdecs',       label: 'Company CusDecs',        Icon: IconDoc   },
  { path: '/company/members',       label: 'Members & Subscriptions',Icon: IconUsers },
  { path: '/company/payments',      label: 'Payments',               Icon: IconCard  },
  { path: '/company/notifications', label: 'Notifications',          Icon: IconBell  },
  { path: '/company/profile',       label: 'Profile',                Icon: IconUser  },
];

// ── Component ───────────────────────────────────────────────────────────────
export default function PortalLayout({ portal }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const isCompany = portal === 'Company';
  const portalLabel = isCompany ? 'Company Portal' : 'Member Portal';
  const navItems = isCompany ? COMPANY_NAV : MEMBER_NAV;

  async function handleLogout() {
    await logout();
    navigate('/login');
  }

  return (
    <div className="pl-layout">
      {/* ── Top bar ── */}
      <header className="pl-topbar">
        <div className="pl-topbar__left">
          <button
            className="pl-menu-btn"
            onClick={() => setSidebarOpen(v => !v)}
            aria-label="Toggle navigation"
            aria-expanded={sidebarOpen}
          >
            <IconMenu />
          </button>
          <div className="pl-topbar__brand">
            <div className="pl-topbar__crest" aria-hidden="true">SLC</div>
            <span className="pl-topbar__app-name">CDNS</span>
          </div>
          <span className="pl-topbar__badge">{portalLabel}</span>
        </div>
        <div className="pl-topbar__right">
          <span className="pl-topbar__user">
            {user?.firstName} {user?.lastName}
          </span>
          <button className="pl-logout-btn" onClick={handleLogout}>
            Log out
          </button>
        </div>
      </header>

      <div className="pl-body">
        {/* ── Sidebar ── */}
        <aside
          className={`pl-sidebar${sidebarOpen ? ' pl-sidebar--open' : ''}`}
          aria-label="Portal navigation"
        >
          <nav>
            {navItems.map(({ path, label, Icon }) => (
              <NavLink
                key={path}
                to={path}
                className={({ isActive }) =>
                  `pl-nav-item${isActive ? ' pl-nav-item--active' : ''}`
                }
                onClick={() => setSidebarOpen(false)}
              >
                <span className="pl-nav-icon"><Icon /></span>
                <span>{label}</span>
              </NavLink>
            ))}
          </nav>
        </aside>

        {/* Mobile overlay */}
        {sidebarOpen && (
          <div
            className="pl-sidebar-overlay"
            onClick={() => setSidebarOpen(false)}
            aria-hidden="true"
          />
        )}

        {/* ── Main content ── */}
        <main className="pl-content" id="main-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
