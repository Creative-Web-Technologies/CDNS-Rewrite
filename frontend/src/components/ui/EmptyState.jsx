import { Inbox } from 'lucide-react';

// EmptyState — centered placeholder for empty lists/tables/panels.
export default function EmptyState({
  icon: Icon = Inbox,
  title = 'Nothing here yet',
  message,
  action,
  className = '',
}) {
  return (
    <div className={`ui-empty ${className}`}>
      <span className="ui-empty__icon" aria-hidden="true">
        <Icon />
      </span>
      <p className="ui-empty__title">{title}</p>
      {message && <p className="ui-empty__text">{message}</p>}
      {action && <div className="ui-empty__action">{action}</div>}
    </div>
  );
}
