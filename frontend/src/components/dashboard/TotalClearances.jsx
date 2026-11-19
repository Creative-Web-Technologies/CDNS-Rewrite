import { Timer } from 'lucide-react';
import { Card, EmptyState } from '../ui';
import Skeleton from './Skeleton';
import { formatNumber, formatHours } from './format';

// TotalClearances — average clearance time as the headline figure, with the
// fully-exited count as a secondary line. Compact empty state when no
// declarations have exited yet.
export default function TotalClearances({ clearances, loading }) {
  const c = clearances ?? {};
  const total = c.totalExited ?? 0;
  const avg = formatHours(c.avgClearanceHours ?? null);

  return (
    <Card eyebrow="Clearances" title="Total clearances">
      {loading ? (
        <div className="dash-clear">
          <Skeleton h={44} w={120} />
          <Skeleton h={14} w={160} style={{ marginTop: 8 }} />
        </div>
      ) : total === 0 ? (
        <div className="dash-card-empty">
          <EmptyState
            icon={Timer}
            title="No clearances yet"
            message="Average clearance time will appear once declarations have fully exited."
            className="dash-empty--compact"
          />
        </div>
      ) : avg ? (
        <div className="dash-clear">
          <span className="dash-clear__num">{avg}</span>
          <span className="dash-clear__label">Average clearance time</span>
          <p className="dash-clear__sub">{formatNumber(total)} declarations fully exited</p>
        </div>
      ) : (
        <div className="dash-clear">
          <span className="dash-clear__num">{formatNumber(total)}</span>
          <span className="dash-clear__label">Declarations fully exited</span>
        </div>
      )}
    </Card>
  );
}
