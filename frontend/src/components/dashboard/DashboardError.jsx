import { TriangleAlert, RotateCw } from 'lucide-react';
import { Card, Button } from '../ui';

// DashboardError — inline error state with a retry action, shown when the
// summary fetch fails.
export default function DashboardError({ onRetry }) {
  return (
    <Card>
      <div className="dash-error">
        <span className="ui-empty__icon" aria-hidden="true"><TriangleAlert /></span>
        <p className="dash-error__title">We couldn’t load your dashboard</p>
        <p className="dash-error__text">Something went wrong fetching your workspace data.</p>
        <Button icon={RotateCw} onClick={onRetry}>Try again</Button>
      </div>
    </Card>
  );
}
