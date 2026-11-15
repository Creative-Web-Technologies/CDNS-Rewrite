import { useCallback, useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import {
  LayoutGrid, FileText, Container, BadgeCheck, ClipboardList, Bell,
  LifeBuoy, UserRound, CreditCard, Users, LogOut, Search, Settings,
} from 'lucide-react';
import { apiGet } from '../../lib/api';
import { useAuth } from '../../contexts/useAuth';
import { PortalChromeContext } from './portalChrome';
import './PortalLayout.css';

// Sri Lanka Customs crest shipped in /public (the official emblem PNG).
const crest = '/Customslogo.png';

// ── Nav configuration per portal ───────────────────────────────────────────
// `primary` fills the rail; `bottom` sits above the avatar. Each item carries
// the subtitle shown under the dashboard greeting / page title.
const MEMBER_NAV = {
  primary: [
    { to: '/member/dashboard',     label: 'Dashboard',     Icon: LayoutGrid,    subtitle: 'Your CDNS activity at a glance.' },
    { to: '/member/cusdecs',       label: 'CusDecs',       Icon: FileText,      subtitle: 'Declarations matching your TINs.' },
    { to: '/member/containers',    label: 'Containers',    Icon: Container,     subtitle: 'Containers across your declarations.' },
    { to: '/member/subscriptions', label: 'Subscriptions', Icon: BadgeCheck,    subtitle: 'Your active and past subscriptions.' },
    { to: '/member/requests',      label: 'Requests',      Icon: ClipboardList, subtitle: 'Your subscription requests.' },
    { to: '/member/notifications', label: 'Notifications', Icon: Bell,          subtitle: 'Updates about your account.' },
  ],
  bottom: [
    { to: '/member/support', label: 'Support',  Icon: LifeBuoy,  subtitle: 'Get help with CDNS.' },
    { to: '/member/profile', label: 'Settings', Icon: Settings,  subtitle: 'Your account details.' },
  ],
};

const COMPANY_NAV = {
  primary: [
    { to: '/company/dashboard',     label: 'Dashboard',       Icon: LayoutGrid, subtitle: 'Company administrator overview.' },
    { to: '/company/cusdecs',       label: 'Company CusDecs', Icon: FileText,   subtitle: 'Declarations for your company.' },
    { to: '/company/members',       label: 'Members',         Icon: Users,      subtitle: 'Members and subscriptions.' },
    { to: '/company/payments',      label: 'Payments',        Icon: CreditCard, subtitle: 'Subscription payments.' },
    { to: '/company/notifications', label: 'Notifications',   Icon: Bell,       subtitle: 'Updates about your company.' },
  ],
  bottom: [
    { to: '/company/support', label: 'Support',  Icon: LifeBuoy, subtitle: 'Get help with CDNS.' },
    { to: '/company/profile', label: 'Settings', Icon: Settings, subtitle: 'Your account details.' },
  ],
};

function initialsOf(user) {
  const a = user?.firstName?.[0] ?? '';
  const b = user?.lastName?.[0] ?? '';
  return (a + b).toUpperCase() || (user?.userName?.[0]?.toUpperCase() ?? '?');
}

function greetingFor(date) {
  const h = date.getHours();
  if (h < 12) return 'Good morning';
  if (h < 18) return 'Good afternoon';
  return 'Good evening';
}

function formatDate(date) {
  return date.toLocaleDateString('en-GB', {
    weekday: 'long', day: 'numeric', month: 'long', year: 'numeric',
  }).toUpperCase();
}

// ── Component ───────────────────────────────────────────────────────────────
export default function PortalLayout({ portal }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const isCompany = portal === 'Company';
  const nav = isCompany ? COMPANY_NAV : MEMBER_NAV;
  const allItems = [...nav.primary, ...nav.bottom];
  const active = allItems.find((item) => location.pathname.startsWith(item.to));
  const portalKey = portal.toLowerCase();
  const isDashboard = location.pathname === `/${portalKey}/dashboard`;

  // Page-injected header chrome (actions / optional title override).
  const [chrome, setChrome] = useState({});
  const setChromeStable = useCallback((next) => setChrome(next ?? {}), []);

  // Unread notifications drive the header bell dot. Refetched on navigation so
  // reading them on the notifications page clears the dot.
  const [unread, setUnread] = useState(0);
  useEffect(() => {
    let alive = true;
    apiGet(`/api/${portalKey}/notifications/unread-count`)
      .then((d) => { if (alive) setUnread(d?.unread ?? 0); })
      .catch(() => { if (alive) setUnread(0); });
    return () => { alive = false; };
  }, [portalKey, location.pathname]);

  // Avatar menu (Profile / Logout).
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef(null);
  useEffect(() => {
    if (!menuOpen) return undefined;
    function onDocClick(e) {
      if (menuRef.current && !menuRef.current.contains(e.target)) setMenuOpen(false);
    }
    function onEsc(e) { if (e.key === 'Escape') setMenuOpen(false); }
    document.addEventListener('mousedown', onDocClick);
    document.addEventListener('keydown', onEsc);
    return () => {
      document.removeEventListener('mousedown', onDocClick);
      document.removeEventListener('keydown', onEsc);
    };
  }, [menuOpen]);

  // Rail tooltip — a single, body-portaled label positioned to the right of the
  // hovered/focused icon, so no clipping parent can crop it and only one shows
  // at a time. Suppressed on the narrow (horizontal) rail.
  const [tip, setTip] = useState(null);
  const showTip = useCallback((e, label) => {
    if (window.matchMedia('(max-width: 860px)').matches) return;
    const r = e.currentTarget.getBoundingClientRect();
    setTip({ label, top: r.top + r.height / 2, left: r.right + 12 });
  }, []);
  const hideTip = useCallback(() => setTip(null), []);
  // Hide on route change.
  useEffect(() => { setTip(null); }, [location.pathname]);

  async function handleLogout() {
    setMenuOpen(false);
    await logout();
    navigate('/login');
  }

  // Captured once when the shell mounts — stable across re-renders.
  const [now] = useState(() => new Date());
  const title = isDashboard
    ? `${greetingFor(now)}, ${user?.firstName || 'there'}`
    : (chrome.title ?? active?.label ?? portal);
  const subtitle = chrome.subtitle ?? active?.subtitle;

  const renderItem = ({ to, label, Icon }) => (
    <NavLink
      key={to}
      to={to}
      aria-label={label}
      className={({ isActive }) => `pl-rail__item${isActive ? ' is-active' : ''}`}
      onMouseEnter={(e) => showTip(e, label)}
      onMouseLeave={hideTip}
      onFocus={(e) => showTip(e, label)}
      onBlur={hideTip}
      onClick={hideTip}
    >
      <Icon size={20} aria-hidden="true" />
    </NavLink>
  );

  return (
    <PortalChromeContext.Provider value={{ setChrome: setChromeStable }}>
      <a className="pl-skip" href="#main-content">Skip to content</a>
      <div className="pl-shell">
        {/* ── Icon rail ── */}
        <aside className="pl-rail" aria-label="Primary">
          <NavLink to={`/${portalKey}/dashboard`} className="pl-rail__crest" aria-label="Sri Lanka Customs — home">
            <img src={crest} alt="" width={32} height={32} />
          </NavLink>

          <nav className="pl-rail__nav" aria-label="Sections">
            {nav.primary.map(renderItem)}
          </nav>

          <div className="pl-rail__bottom">
            {nav.bottom.map(renderItem)}

            <div className="pl-rail__avatar-wrap" ref={menuRef}>
              <button
                type="button"
                className="pl-rail__avatar"
                aria-haspopup="menu"
                aria-expanded={menuOpen}
                aria-label="Account menu"
                onClick={() => setMenuOpen((o) => !o)}
              >
                {initialsOf(user)}
              </button>
              {menuOpen && (
                <div className="pl-rail__menu" role="menu">
                  <div className="pl-rail__menu-head">
                    <span className="pl-rail__menu-name">{user?.firstName} {user?.lastName}</span>
                    <span className="pl-rail__menu-sub">{user?.userName}</span>
                  </div>
                  <button
                    type="button" role="menuitem" className="pl-rail__menu-item"
                    onClick={() => { setMenuOpen(false); navigate(`/${portalKey}/profile`); }}
                  >
                    <UserRound size={16} aria-hidden="true" /> Profile
                  </button>
                  <button
                    type="button" role="menuitem" className="pl-rail__menu-item pl-rail__menu-item--danger"
                    onClick={handleLogout}
                  >
                    <LogOut size={16} aria-hidden="true" /> Log out
                  </button>
                </div>
              )}
            </div>
          </div>
        </aside>

        {/* ── Main column ── */}
        <div className="pl-main">
          <header className="pl-header">
            <div className="pl-header__titles">
              <span className="pl-header__date">{formatDate(now)}</span>
              <h1 className="pl-header__title">{title}</h1>
              {subtitle && <p className="pl-header__subtitle">{subtitle}</p>}
            </div>

            <div className="pl-header__right">
              {chrome.actions && <div className="pl-header__actions">{chrome.actions}</div>}
              <button type="button" className="pl-iconbtn" aria-label="Search">
                <Search size={18} aria-hidden="true" />
              </button>
              <button
                type="button"
                className="pl-iconbtn"
                aria-label={unread > 0 ? `Notifications, ${unread} unread` : 'Notifications'}
                onClick={() => navigate(`/${portalKey}/notifications`)}
              >
                <Bell size={18} aria-hidden="true" />
                {unread > 0 && <span className="pl-iconbtn__dot" aria-hidden="true" />}
              </button>
            </div>
          </header>

          <main className="pl-content" id="main-content">
            <Outlet />
          </main>
        </div>
      </div>

      {tip && createPortal(
        <div className="pl-tooltip" role="tooltip" style={{ top: tip.top, left: tip.left }}>
          {tip.label}
        </div>,
        document.body,
      )}
    </PortalChromeContext.Provider>
  );
}
