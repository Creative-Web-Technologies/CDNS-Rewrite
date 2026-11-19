import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, Info, ExternalLink, Container, BadgeCheck, AlertCircle, RefreshCw } from 'lucide-react';
import { Button, Card, EmptyState, PageTransition, Pill } from '../../../components/ui';
import { usePortalChrome } from '../../../components/portal/portalChrome';
import { apiGet } from '../../../lib/api';
import './subscriptions.css';

function formatDate(iso) {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
}

function TypePill({ type }) {
  if (type === 'Declarant')
    return <span className="sub-pill sub-pill--declarant">Declarant</span>;
  return <Pill tone="blue">Consignee</Pill>;
}

function SkeletonCard() {
  return (
    <div className="sub-card sub-card--skeleton" aria-hidden="true">
      <div className="sub-skel sub-skel--row" />
      <div className="sub-skel sub-skel--title" />
      <div className="sub-skel sub-skel--line" />
      <div className="sub-skel sub-skel--line" />
      <div className="sub-skel sub-skel--actions" />
    </div>
  );
}

export default function SubscriptionsPage() {
  const navigate = useNavigate();
  const [subs, setSubs] = useState(null);
  const [error, setError] = useState(null);
  const [retryCount, setRetryCount] = useState(0);

  usePortalChrome({
    actions: (
      <Button icon={Plus} onClick={() => navigate('/member/requests/new')}>
        New subscription request
      </Button>
    ),
  });

  useEffect(() => {
    let alive = true;
    setSubs(null);
    setError(null);
    apiGet('/api/member/subscriptions')
      .then((data) => { if (alive) setSubs(data ?? []); })
      .catch(() => { if (alive) setError(true); });
    return () => { alive = false; };
  }, [retryCount]);

  const loading = subs === null && !error;

  return (
    <PageTransition>
      <div className="sub-stack">
        <div className="sub-page-header">
          <div>
            <h1 className="sub-page-title">My subscriptions</h1>
            <p className="sub-page-subtitle">Your active subscriptions. Each one opens its own CusDec workspace.</p>
          </div>
          <Button icon={Plus} onClick={() => navigate('/member/requests/new')}>
            New subscription request
          </Button>
        </div>

        <div className="sub-banner" role="note">
          <Info size={16} aria-hidden="true" className="sub-banner__icon" />
          <span>Subscriptions are issued by the company master. To get a new one, send a subscription request to the company.</span>
        </div>

        {error ? (
          <Card>
            <EmptyState
              icon={AlertCircle}
              title="Couldn't load subscriptions"
              message="Something went wrong while fetching your subscriptions."
              action={
                <Button variant="secondary" icon={RefreshCw} onClick={() => setRetryCount((c) => c + 1)}>
                  Retry
                </Button>
              }
            />
          </Card>
        ) : loading ? (
          <div className="sub-grid">
            {[1, 2, 3].map((k) => <SkeletonCard key={k} />)}
          </div>
        ) : subs.length === 0 ? (
          <Card>
            <EmptyState
              icon={BadgeCheck}
              title="No active subscriptions"
              message="You don't have any active subscriptions yet. Send a subscription request to a company to get started."
              action={
                <Button icon={Plus} onClick={() => navigate('/member/requests/new')}>
                  New subscription request
                </Button>
              }
            />
          </Card>
        ) : (
          <div className="sub-grid">
            {subs.map((sub) => (
              <div key={sub.memberSubscriptionId} className="sub-card">
                <div className="sub-card__pills">
                  <TypePill type={sub.subscriptionType} />
                  <Pill tone="green">Active</Pill>
                </div>
                <h3 className="sub-card__company">{sub.companyName}</h3>
                <p className="sub-card__tin">TIN: {sub.companyTin}</p>
                <p className="sub-card__plan">{sub.planName} · {sub.planDuration}</p>
                <p className="sub-card__expires">Expires: {formatDate(sub.expiryDate)}</p>
                <div className="sub-card__actions">
                  <Button
                    size="sm"
                    icon={ExternalLink}
                    onClick={() => navigate(`/member/cusdec/${sub.memberSubscriptionId}`)}
                  >
                    Open CusDec
                  </Button>
                  {!sub.isAcknowledgementOnly && (
                    <Button
                      size="sm"
                      variant="secondary"
                      icon={Container}
                      onClick={() => navigate(`/member/containers/${sub.memberSubscriptionId}`)}
                    >
                      Containers
                    </Button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </PageTransition>
  );
}
