namespace Cdns.Application.Feature.Dashboard;

/// <summary>
/// Builds the authenticated member's home-dashboard summary. Every query is
/// scoped to the current member via <c>ICurrentUser</c>.
/// </summary>
public interface IMemberDashboardService
{
    Task<MemberDashboardSummary> GetSummaryAsync(CancellationToken ct = default);

    /// <summary>Count of the member's unseen notifications (drives the header bell).</summary>
    Task<int> GetUnreadNotificationCountAsync(CancellationToken ct = default);
}
