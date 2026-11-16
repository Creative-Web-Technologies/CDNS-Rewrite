import { Link } from 'react-router-dom';
import { Bell, ArrowRight } from 'lucide-react';
import { Card, EmptyState } from '../ui';
import Skeleton from './Skeleton';
import { formatDateTime } from './format';

// PriorityStream — the member/master's most recent notifications; unseen ones
// are highlighted. Links to the full notifications page.
export default function PriorityStream({ notifications, portal, loading }) {
  const items = notifications ?? [];
  const notificationsPath = `/${portal}/notifications`;

  return (
    <Card eyebrow="Alerts" title="Priority stream" padding={false}>
      {loading ? (
        <div style={{ padding: 20, display: 'flex', flexDirection: 'column', gap: 14 }}>
          {[0, 1, 2].map((i) => <Skeleton key={i} h={40} radius={8} />)}
        </div>
      ) : items.length === 0 ? (
        <div className="dash-card-empty">
          <EmptyState
            icon={Bell}
            title="No notifications yet"
            message="Updates about your declarations and subscriptions will appear here."
            className="dash-empty--compact"
          />
        </div>
      ) : (
        <div className="dash-stream" style={{ padding: '4px 16px 12px' }}>
          {items.map((n) => (
            <div
              key={n.id}
              className={`dash-stream__item${n.isSeen ? '' : ' dash-stream__item--unseen'}`}
            >
              <span className="dash-stream__dot" aria-hidden="true" />
              <div className="dash-stream__body">
                <div className="dash-stream__title">{n.title || 'Notification'}</div>
                {n.message && <div className="dash-stream__msg">{n.message}</div>}
                <div className="dash-stream__time">{formatDateTime(n.createdAt)}</div>
              </div>
            </div>
          ))}
          <div className="dash-stream__footer">
            <Link className="dash-stream__link" to={notificationsPath}>
              View all notifications <ArrowRight size={14} aria-hidden="true" />
            </Link>
          </div>
        </div>
      )}
    </Card>
  );
}
