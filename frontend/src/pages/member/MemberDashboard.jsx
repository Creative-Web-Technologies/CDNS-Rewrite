import { useNavigate } from 'react-router-dom';
import { Plus } from 'lucide-react';
import { Button, PageTransition } from '../../components/ui';
import {
  Banner, WorkspaceActivity, TotalClearances, PriorityStream,
  SubscriptionQuota, DashboardError, useDashboardSummary,
} from '../../components/dashboard';
import { usePortalChrome } from '../../components/portal/portalChrome';
import '../../components/dashboard/dashboard.css';

export default function MemberDashboard() {
  const navigate = useNavigate();
  const { summary, status, reload } = useDashboardSummary('/api/member/dashboard-summary');
  const loading = status === 'loading';

  usePortalChrome({
    showDate: true,
    compactHeader: false,
    actions: (
      <Button icon={Plus} onClick={() => navigate('/member/requests/new')}>
        New declaration subscription
      </Button>
    ),
  });

  return (
    <PageTransition>
      <div className="dash-stack">
        <Banner text="Track your declarations, subscriptions and clearances across every company you work with." />

        {status === 'error' ? (
          <DashboardError onRetry={reload} />
        ) : (
          <>
            <WorkspaceActivity summary={summary} loading={loading} />
            <div className="dash-row">
              <TotalClearances clearances={summary?.clearances} loading={loading} />
              <PriorityStream notifications={summary?.recentNotifications} portal="member" loading={loading} />
              <SubscriptionQuota nearest={summary?.nearestExpiry} counts={summary?.subscriptions} loading={loading} />
            </div>
          </>
        )}
      </div>
    </PageTransition>
  );
}
