import SectionLabel from './SectionLabel';
import PerformanceAnnually from './PerformanceAnnually';
import WeeklyComparison from './WeeklyComparison';
import StatusCard from './StatusCard';

// WorkspaceActivity — the shared "LIVE PERFORMANCE" section and its 3-column
// row (annual performance, weekly comparison, status), used by both portals.
export default function WorkspaceActivity({ summary, loading }) {
  return (
    <>
      <SectionLabel eyebrow="Live performance" title="Workspace activity" />
      <div className="dash-row">
        <PerformanceAnnually data={summary?.monthlyCompleted} loading={loading} />
        <WeeklyComparison data={summary?.weekly} average={summary?.weeklyAverage} loading={loading} />
        <StatusCard counts={summary?.cusDecs} loading={loading} />
      </div>
    </>
  );
}
