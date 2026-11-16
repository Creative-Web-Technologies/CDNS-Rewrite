import { BarChart, Bar, XAxis, ResponsiveContainer } from 'recharts';
import { Card, EmptyState } from '../ui';
import Skeleton from './Skeleton';
import { COLORS, monthInitial, formatNumber } from './format';

// PerformanceAnnually — bar chart of declarations completed (exited) per month,
// with the year total underneath.
export default function PerformanceAnnually({ data, loading }) {
  const months = data ?? [];
  const total = months.reduce((sum, m) => sum + (m.count ?? 0), 0);
  const chartData = months.map((m) => ({ label: monthInitial(m.month), count: m.count ?? 0 }));

  return (
    <Card eyebrow="Activity" title="Performance annually">
      {loading ? (
        <Skeleton h={180} radius={8} />
      ) : total === 0 ? (
        <div className="dash-chart-empty">
          <EmptyState
            title="No completed declarations yet"
            message="Completed declarations will appear here once they exit."
          />
        </div>
      ) : (
        <figure
          className="dash-figure"
          role="img"
          aria-label={`Bar chart of declarations completed in each month this year. Total of ${total} completed.`}
        >
          <div className="dash-figure__chart">
            <ResponsiveContainer width="100%" height={180}>
              <BarChart data={chartData} margin={{ top: 4, right: 4, bottom: 0, left: -24 }}>
                <XAxis
                  dataKey="label"
                  interval={0}
                  tickLine={false}
                  axisLine={false}
                  tick={{ fontSize: 11, fill: COLORS.muted }}
                />
                <Bar dataKey="count" radius={[4, 4, 0, 0]} fill={COLORS.chart} maxBarSize={22} />
              </BarChart>
            </ResponsiveContainer>
          </div>
          <figcaption className="dash-figure__caption">
            Total this year: <strong>{formatNumber(total)}</strong>
          </figcaption>
        </figure>
      )}
    </Card>
  );
}
