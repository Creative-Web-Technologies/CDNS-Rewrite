import { useNavigate } from 'react-router-dom';
import { ClipboardCheck } from 'lucide-react';
import { Button, PageTransition } from '../../components/ui';
import {
  Banner, WorkspaceActivity, TotalClearances, PriorityStream,
  PaymentSummaryCard, DashboardError, useDashboardSummary,
} from '../../components/dashboard';
import { usePortalChrome } from '../../components/portal/portalChrome';
import '../../components/dashboard/dashboard.css';

export default function CompanyDashboard() {
  const navigate = useNavigate();
  const { summary, status, reload } = useDashboardSummary('/api/company/dashboard-summary');
  const loading = status === 'loading';

  usePortalChrome({
    actions: (
      <Button icon={ClipboardCheck} onClick={() => navigate('/company/requests')}>
        Review requests
      </Button>
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
