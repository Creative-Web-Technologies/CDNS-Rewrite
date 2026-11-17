import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { ClipboardCheck, Clock, BadgeCheck, AlertCircle, PlusCircle } from 'lucide-react';
import { Button, Card, PageTransition, StatTile } from '../../components/ui';
import {
  Banner, WorkspaceActivity, TotalClearances, PriorityStream,
  PaymentSummaryCard, DashboardError, useDashboardSummary, Skeleton,
} from '../../components/dashboard';
import { usePortalChrome } from '../../components/portal/portalChrome';
import { apiPost } from '../../lib/api';
import '../../components/dashboard/dashboard.css';

function SubscriptionStatusWidget({ subscriptions, loading }) {
  const pending = subscriptions?.pending ?? 0;
  const active  = subscriptions?.active  ?? 0;
  const expired = subscriptions?.expired ?? 0;

  if (loading) {
    return (
      <Card title="Subscription overview">
        <div style={{ display: 'flex', gap: 16 }}>
          <Skeleton style={{ flex: 1, height: 72 }} />
          <Skeleton style={{ flex: 1, height: 72 }} />
          <Skeleton style={{ flex: 1, height: 72 }} />
        </div>
      </Card>
    );
  }

  return (
    <Card title="Subscription overview">
      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
        <Link to="/company/requests?status=0" style={{ flex: 1, minWidth: 120, textDecoration: 'none' }}>
          <StatTile icon={Clock}       value={pending} label="Pending requests" tone="amber" />
        </Link>
        <Link to="/company/subscriptions?status=1" style={{ flex: 1, minWidth: 120, textDecoration: 'none' }}>
          <StatTile icon={BadgeCheck}  value={active}  label="Active"           tone="success" />
        </Link>
        <Link to="/company/subscriptions?status=4" style={{ flex: 1, minWidth: 120, textDecoration: 'none' }}>
          <StatTile icon={AlertCircle} value={expired} label="Expired"          tone="danger" />
        </Link>
      </div>
    </Card>
  );
}

export default function CompanyDashboard() {
  const navigate = useNavigate();
  const { summary, status, reload } = useDashboardSummary('/api/company/dashboard-summary');
  const loading = status === 'loading';
  const [creating, setCreating] = useState(false);

  async function handleNewSubscription() {
    setCreating(true);
    try {
      const result = await apiPost('/api/master/subscription-requests', {});
      navigate(`/company/requests/${result.requestId}/plans`, { state: { type: 'Consignee' } });
    } catch {
      setCreating(false);
    }
  }

  usePortalChrome({
    actions: (
      <div style={{ display: 'flex', gap: 8 }}>
        <Button icon={PlusCircle} disabled={creating} onClick={handleNewSubscription}>
          {creating ? 'Creating…' : 'New subscription'}
        </Button>
        <Button icon={ClipboardCheck} onClick={() => navigate('/company/requests')}>
          Review requests
        </Button>
      </div>
    ),
  });

  return (
    <PageTransition>
      <div className="dash-stack">
        <Banner text="Monitor declarations, subscriptions and payments across every company you administer." />

        {status === 'error' ? (
          <DashboardError onRetry={reload} />
        ) : (
          <>
            <SubscriptionStatusWidget subscriptions={summary?.subscriptions} loading={loading} />
            <WorkspaceActivity summary={summary} loading={loading} />
            <div className="dash-row">
              <TotalClearances clearances={summary?.clearances} loading={loading} />
              <PriorityStream notifications={summary?.recentNotifications} portal="company" loading={loading} />
              <PaymentSummaryCard payment={summary?.paymentSummary} loading={loading} />
            </div>
          </>
        )}
      </div>
    </PageTransition>
  );
}
