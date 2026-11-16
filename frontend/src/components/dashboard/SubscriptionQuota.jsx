import { Link, useNavigate } from 'react-router-dom';
import { BadgeCheck } from 'lucide-react';
import { Card, EmptyState, Button } from '../ui';
import Skeleton from './Skeleton';
import { formatNumber, formatDate } from './format';

function MiniTiles({ counts }) {
  const tiles = [
    { label: 'Pending', value: counts?.pending ?? 0 },
    { label: 'Active', value: counts?.active ?? 0 },
    { label: 'Expired', value: counts?.expired ?? 0 },
  ];
  return (
    <div className="dash-quota__tiles">
      {tiles.map((t) => (
        <div className="dash-quota__tile" key={t.label}>
          <div className="dash-quota__tile-num">{formatNumber(t.value)}</div>
          <div className="dash-quota__tile-label">{t.label}</div>
        </div>
      ))}
    </div>
  );
}

// SubscriptionQuota (member only) — Pending/Active/Expired tiles at the top,
// then one of three body states: active subscription details, pending-only
// message, or an empty state inviting the member to request a subscription.
export default function SubscriptionQuota({ nearest, counts, loading }) {
  const navigate = useNavigate();

  if (loading) {
    return (
      <Card eyebrow="Subscription" title="Subscription quota">
        <div className="dash-quota__tiles">
          {[0, 1, 2].map((i) => <Skeleton key={i} h={52} radius={8} />)}
        </div>
        <Skeleton h={52} w="100%" radius={8} />
      </Card>
    );
  }

  const active = counts?.active ?? 0;
  const pending = counts?.pending ?? 0;

  return (
    <Card eyebrow="Subscription" title="Subscription quota">
      <MiniTiles counts={counts} />

      {active > 0 && (
        <>
          {nearest && (
            <div className="dash-quota__sub-list">
              <div className="dash-quota__sub-item">
                <span className="dash-quota__sub-company">{nearest.companyName}</span>
                <span className="dash-quota__sub-expiry">Expires {formatDate(nearest.expiryDate)}</span>
              </div>
            </div>
          )}
          <div className="dash-quota__viewall">
            <Link className="dash-quota__link" to="/member/subscriptions">
              View all subscriptions
            </Link>
          </div>
        </>
      )}

      {active === 0 && pending > 0 && (
        <div className="dash-quota__pending-msg">
          {pending === 1 ? '1 request awaiting approval' : `${formatNumber(pending)} requests awaiting approval`}
          {' — '}
          <Link className="dash-quota__link" to="/member/requests">view requests</Link>
        </div>
      )}

      {active === 0 && pending === 0 && (
        <div className="dash-card-empty">
          <EmptyState
            icon={BadgeCheck}
            message="Request a subscription to a company to start tracking declarations."
            className="dash-empty--compact"
            action={(
              <Button onClick={() => navigate('/member/requests/new')}>
                Request a subscription
              </Button>
            )}
          />
        </div>
      )}
    </Card>
  );
}
