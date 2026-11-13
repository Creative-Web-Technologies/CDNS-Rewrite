import './AuthShell.css';

export default function AuthShell({ children }) {
  return (
    <div className="portal-shell">
      <div className="portal-header-bar">
        <span className="portal-header-left">
          <span className="portal-status-dot" aria-hidden="true" />
          ASYCUDA World Gateway | v4.8.2-PROD [LK-CMB]
        </span>
        <span className="portal-header-right">
          Official Customs Operational Portal • Democratic Socialist Republic of Sri Lanka
        </span>
      </div>
      <div className="portal-body">
        <div className="portal-left" aria-hidden="true" />
        <div className="portal-right">
          <div className="portal-right-inner">
            {children}
          </div>
        </div>
      </div>
    </div>
  );
}
