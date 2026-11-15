import { Card, Pill } from '../ui';
import Skeleton from './Skeleton';
import { formatNumber } from './format';

// StatusCard — Processing / Exited / Cancelled. Each row stacks an uppercase
// label above a large number, with a tone pill directly beside the number.
export default function StatusCard({ counts, loading }) {
  const c = counts ?? {};
  const rows = [
    { label: 'Processing', value: c.processing ?? 0, tone: 'blue', pill: 'In progress' },
    { label: 'Exited', value: c.exited ?? 0, tone: 'green', pill: 'Completed' },
    { label: 'Cancelled', value: c.cancelled ?? 0, tone: 'grey', pill: 'Cancelled' },
  ];

  return (
    <Card eyebrow="Status" title="Declaration status">
      {loading ? (
        <div className="dash-status">
          {rows.map((r) => (
            <div className="dash-status__row" key={r.label}>
              <Skeleton h={12} w={70} />
              <div className="dash-status__value">
                <Skeleton h={30} w={48} />
                <Skeleton h={20} w={84} radius={999} />
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="dash-status">
          {rows.map((r) => (
            <div className="dash-status__row" key={r.label}>
              <span className="dash-status__label">{r.label}</span>
              <div className="dash-status__value">
                <span className="dash-status__num">{formatNumber(r.value)}</span>
                <Pill tone={r.tone}>{r.pill}</Pill>
              </div>
            </div>
          ))}
        </div>
      )}
    </Card>
  );
}
