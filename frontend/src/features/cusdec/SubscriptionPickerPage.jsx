import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AlertCircle, RefreshCw, BadgeCheck, Shield } from 'lucide-react';
import { Button, Card, EmptyState, PageTransition, Pill } from '../../components/ui';
import { usePortalChrome } from '../../components/portal/portalChrome';
import { getSubscriptions } from './cusdecApi';
import { formatDate, daysUntil } from './formatHelpers';
import './cusdec.css';

function TypePill({ type }) {
  if (type === 'Declarant')
    return (
      <span className="cd-type-pill cd-type-pill--declarant">
        <Shield size={11} aria-hidden="true" /> Declarant
      </span>
    );
  return (
    <span className="cd-type-pill cd-type-pill--consignee">
      <Shield size={11} aria-hidden="true" /> Consignee
    </span>
  );
}

function compareSubs(a, b) {
  const co = (a.companyName ?? '').localeCompare(b.companyName ?? '');
  if (co !== 0) return co;
  const to = (a.subscriptionType ?? '').localeCompare(b.subscriptionType ?? '');
  if (to !== 0) return to;
  return (a.planName ?? '').localeCompare(b.planName ?? '');
}

export default function SubscriptionPickerPage({ portal }) {
  const navigate = useNavigate();
  const [subs, setSubs] = useState(null);
  const [error, setError] = useState(null);
  const [retryKey, setRetryKey] = useState(0);

  const subsPath = `/${portal}/subscriptions`;
  const workspaceBase = `/${portal}/cusdec`;

  usePortalChrome({
    title: 'CusDecs',
    subtitle: 'Choose a subscription to open its workspace.',
  });

  useEffect(() => {
    let alive = true;
    setSubs(null);
    setError(null);
    getSubscriptions()
      .then((data) => {
        if (!alive) return;
        const list = data ?? [];
        if (list.length === 1) {
          navigate(`${workspaceBase}/${list[0].memberSubscriptionId}`, { replace: true });
          return;
        }
        setSubs([...list].sort(compareSubs));
      })
      .catch(() => { if (alive) setError(true); });
    return () => { alive = false; };
  }, [retryKey, navigate, workspaceBase]);

  const loading = subs === null && !error;

  return (
    <PageTransition>
      <div className="cd-stack">
        {error ? (
          <Card>
            <EmptyState
              icon={AlertCircle}
              title="Couldn't load subscriptions"
              message="Something went wrong while fetching your subscriptions. Please try again."
              action={
                <Button variant="secondary" icon={RefreshCw} onClick={() => setRetryKey((k) => k + 1)}>
                  Retry
                </Button>
              }
            />
          </Card>
        ) : loading ? (
          <div className="cd-picker-grid">
            {[1, 2, 3].map((k) => <PickerSkeleton key={k} />)}
          </div>
        ) : subs?.length === 0 ? (
          <Card>
            <EmptyState
              icon={BadgeCheck}
              title="No active subscriptions"
              message={
                portal === 'member'
                  ? "You don't have any active subscriptions yet. Send a subscription request to a company to get started."
                  : 'No active subscriptions found for this company.'
              }
              action={
                portal === 'member' ? (
                  <Button onClick={() => navigate('/member/requests/new')}>
                    New subscription request
                  </Button>
                ) : (
                  <Button variant="secondary" onClick={() => navigate(subsPath)}>
                    View subscriptions
                  </Button>
                )
              }
            />
          </Card>
        ) : (
          <>
            <p className="cd-picker-heading">Your subscriptions</p>
            <div className="cd-picker-grid">
              {subs.map((sub) => (
                <PickerCard
                  key={sub.memberSubscriptionId}
                  sub={sub}
                  workspaceBase={workspaceBase}
                  navigate={navigate}
                />
              ))}
            </div>
          </>
        )}
      </div>
    </PageTransition>
  );
}

function PickerCard({ sub, workspaceBase, navigate }) {
  const days = daysUntil(sub.expiryDate);

  return (
    <button
      type="button"
      className="cd-picker-card"
      onClick={() => navigate(`${workspaceBase}/${sub.memberSubscriptionId}`)}
    >
      <div className="cd-picker-card__header">
        <TypePill type={sub.subscriptionType} />
        <span className="cd-picker-card__ref">#{sub.memberSubscriptionId}</span>
      </div>

      <h3 className="cd-picker-card__company">{sub.companyName}</h3>
      <p className="cd-picker-card__tin">TIN {sub.companyTin}</p>
      <p className="cd-picker-card__plan">{sub.planName}</p>

      <div className="cd-picker-card__footer">
        <span className="cd-picker-card__expiry">Expires {formatDate(sub.expiryDate)}</span>
        {days !== null && days <= 90 && (
          <Pill tone={days <= 30 ? 'amber' : 'green'} className="cd-picker-card__expiry-pill">
            {days}d
          </Pill>
        )}
        {sub.onlyForAcknowledgement && (
          <span className="cd-picker-card__ack-badge">Acknowledgement only</span>
        )}
      </div>
    </button>
  );
}

function PickerSkeleton() {
  return (
    <div className="cd-picker-card cd-picker-card--skeleton" aria-hidden="true">
      <div className="cd-skel cd-skel--row" />
      <div className="cd-skel cd-skel--title" />
      <div className="cd-skel cd-skel--line" />
      <div className="cd-skel cd-skel--line" />
    </div>
  );
}
