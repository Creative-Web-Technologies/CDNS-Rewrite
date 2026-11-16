import { Card } from '../ui';
import Skeleton from './Skeleton';
import { formatNumber, formatHours } from './format';

// TotalClearances — the total fully-exited count, with the average clearance
// time underneath (hidden when it cannot be derived).
export default function TotalClearances({ clearances, loading }) {
  const c = clearances ?? {};
  const avg = formatHours(c.avgClearanceHours ?? null);

  return (
    <Card eyebrow="Clearances" title="Total clearances">
      {loading ? (
        <div className="dash-clear">
          <Skeleton h={44} w={120} />
          <Skeleton h={14} w={160} style={{ marginTop: 8 }} />
        </div>
      ) : (
        <div className="dash-clear">
          <span className="dash-clear__num">{formatNumber(c.totalExited ?? 0)}</span>
          <span className="dash-clear__label">Declarations fully exited</span>
          {avg && (
            <p className="dash-clear__sub">
              Average clearance time <strong>{avg}</strong>
            </p>
          )}
        </div>
      )}
    </Card>
  );
}
