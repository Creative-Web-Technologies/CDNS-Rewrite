// PageHeader — the header strip used by the portal shell (and available to any
// page): an eyebrow, title and subtitle on the left; an actions slot plus any
// `right` chrome (notifications, user chip) on the right.
export default function PageHeader({
  eyebrow,
  title,
  subtitle,
  actions,
  right,
  className = '',
}) {
  return (
    <header className={`ui-pageheader ${className}`}>
      <div className="ui-pageheader__titles">
        {eyebrow && <span className="ui-pageheader__eyebrow">{eyebrow}</span>}
        {title && <h1 className="ui-pageheader__title">{title}</h1>}
        {subtitle && <p className="ui-pageheader__subtitle">{subtitle}</p>}
      </div>
      <div className="ui-pageheader__right">
        {actions && <div className="ui-pageheader__actions">{actions}</div>}
        {right}
      </div>
    </header>
  );
}
