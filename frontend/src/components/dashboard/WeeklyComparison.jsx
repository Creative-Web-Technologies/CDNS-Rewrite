import { AreaChart, Area, XAxis, ReferenceLine, ResponsiveContainer } from 'recharts';
import { Card, EmptyState } from '../ui';
import Skeleton from './Skeleton';
import { COLORS, formatWeekStart } from './format';

// WeeklyComparison — area chart of declarations registered over the last four
// weeks, with a dashed line at the weekly average and a small legend.
export default function WeeklyComparison({ data, average, loading }) {
  const weeks = data ?? [];
  const avg = average ?? 0;
  const chartData = weeks.map((w) => ({ label: formatWeekStart(w.weekStart), registered: w.registered ?? 0 }));
  const hasData = weeks.some((w) => (w.registered ?? 0) > 0);

  return (
    <Card eyebrow="Efficiency" title="Weekly comparison">
      {loading ? (
        <Skeleton h={180} radius={8} />
      ) : !hasData ? (
        <div className="dash-chart-empty">
          <EmptyState
            title="No declarations registered in the last 4 weeks"
            message="Weekly registrations will appear here as declarations come in."
          />
        </div>
      ) : (
        <figure
          className="dash-figure"
          role="img"
          aria-label={`Area chart of declarations registered over the last ${weeks.length} weeks. Weekly average ${avg}.`}
        >
          <div className="dash-figure__chart">
            <ResponsiveContainer width="100%" height={180}>
              <AreaChart data={chartData} margin={{ top: 6, right: 6, bottom: 0, left: -24 }}>
                <defs>
                  <linearGradient id="weeklyFill" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor={COLORS.chart} stopOpacity={0.28} />
                    <stop offset="100%" stopColor={COLORS.chart} stopOpacity={0.02} />
                  </linearGradient>
                </defs>
                <XAxis
                  dataKey="label"
                  tickLine={false}
                  axisLine={false}
                  tick={{ fontSize: 11, fill: COLORS.muted }}
                />
                <ReferenceLine y={avg} stroke={COLORS.muted} strokeDasharray="4 3" />
                <Area
                  type="monotone"
                  dataKey="registered"
                  stroke={COLORS.primary}
                  strokeWidth={2}
                  fill="url(#weeklyFill)"
                />
              </AreaChart>
            </ResponsiveContainer>
          </div>
          <div className="dash-legend">
            <span className="dash-legend__item">
              <span className="dash-legend__swatch" style={{ background: COLORS.primary }} />
              Registered
            </span>
            <span className="dash-legend__item">
              <span className="dash-legend__swatch dash-legend__swatch--dash" />
              Average ({avg})
            </span>
          </div>
        </figure>
      )}
    </Card>
  );
}
