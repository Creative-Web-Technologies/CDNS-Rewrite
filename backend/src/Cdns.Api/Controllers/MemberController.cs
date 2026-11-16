using Cdns.Application.Feature.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cdns.Api.Controllers;

[ApiController]
[Route("api/member")]
[Authorize(Policy = "MemberOnly")]
public class MemberController : ControllerBase
{
    private readonly IMemberDashboardService _dashboard;

    public MemberController(IMemberDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet("dashboard-summary")]
    public async Task<ActionResult<MemberDashboardSummary>> GetDashboardSummary(CancellationToken ct)
        => Ok(await _dashboard.GetSummaryAsync(ct));

    [HttpGet("notifications/unread-count")]
    public async Task<IActionResult> GetUnreadNotificationCount(CancellationToken ct)
        => Ok(new { unread = await _dashboard.GetUnreadNotificationCountAsync(ct) });
}
