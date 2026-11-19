using Cdns.Application.Feature.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cdns.Api.Controllers;

[ApiController]
[Route("api/company")]
[Authorize(Policy = "MasterOnly")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyDashboardService _dashboard;

    public CompanyController(ICompanyDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet("dashboard-summary")]
    public async Task<ActionResult<CompanyDashboardSummary>> GetDashboardSummary(CancellationToken ct)
        => Ok(await _dashboard.GetSummaryAsync(ct));

    [HttpGet("notifications/unread-count")]
    public async Task<IActionResult> GetUnreadNotificationCount(CancellationToken ct)
        => Ok(new { unread = await _dashboard.GetUnreadNotificationCountAsync(ct) });
}
