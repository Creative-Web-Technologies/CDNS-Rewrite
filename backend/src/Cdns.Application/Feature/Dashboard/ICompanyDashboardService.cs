namespace Cdns.Application.Feature.Dashboard;

/// <summary>
/// Builds the company-master's home-dashboard summary, scoped to every company
/// the authenticated master is mapped to.
/// </summary>
public interface ICompanyDashboardService
{
    Task<CompanyDashboardSummary> GetSummaryAsync(CancellationToken ct = default);

    /// <summary>Count of the master's unseen notifications (drives the header bell).</summary>
    Task<int> GetUnreadNotificationCountAsync(CancellationToken ct = default);
}
