import { useNavigate } from 'react-router-dom';
import { BadgeCheck } from 'lucide-react';
import { Card, Pill, EmptyState, Button } from '../ui';
import Skeleton from './Skeleton';
import { formatNumber } from './format';

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

// SubscriptionQuota (member only) — the nearest-expiring subscription with a
// usage bar and the pending/active/expired tiles. When nothing is active, an
// empty state invites the member to request a subscription.
export default function SubscriptionQuota({ nearest, counts, loading }) {
  const navigate = useNavigate();

  if (loading) {
    return (
      <Card eyebrow="Subscription" title="Subscription quota">
        <Skeleton h={14} w="60%" />
        <Skeleton h={8} w="100%" radius={999} style={{ marginTop: 18 }} />
        <Skeleton h={60} w="100%" radius={8} style={{ marginTop: 18 }} />
      </Card>
    );
  }

  if (!nearest) {
    return (
      <Card eyebrow="Subscription" title="Subscription quota">
        <EmptyState
          icon={BadgeCheck}
          title="No active subscription"
          message="Request a subscription to a company to start tracking declarations."
          action={(
            <Button onClick={() => navigate('/member/requests/new')}>
              Request a subscription
            </Button>
          )}
        />
        <MiniTiles counts={counts} />
      </Card>
    );
  }

  const total = Math.max(1, nearest.daysTotal ?? 1);
  const used = Math.min(total, Math.max(0, nearest.daysUsed ?? 0));
  const pct = Math.round((used / total) * 100);
  const remaining = nearest.daysRemaining ?? 0;
  const expiringSoon = remaining < 60;

  return (
    <Card eyebrow="Subscription" title="Subscription quota">
      <div className="dash-quota__company">{nearest.companyName}</div>
      <div className="dash-quota__plan">{nearest.subscriptionType} subscription</div>

      <div className="dash-quota__barhead">
        <span className="dash-quota__remaining">{formatNumber(remaining)} days remaining</span>
        {expiringSoon && <Pill tone="amber">Expiring soon</Pill>}
      </div>
      <div
        className="dash-quota__bar"
        role="progressbar"
        aria-valuemin={0}
        aria-valuemax={total}
        aria-valuenow={used}
        aria-label={`${used} of ${total} days used`}
      >
        <div className="dash-quota__fill" style={{ width: `${pct}%` }} />
      </div>
      <div className="dash-quota__dates">
        <span>{formatNumber(used)} days used</span>
        <span>{formatNumber(total)} days total</span>
      </div>

      <MiniTiles counts={counts} />
    </Card>
  );
}
