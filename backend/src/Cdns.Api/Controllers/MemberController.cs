using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cdns.Api.Controllers;

[ApiController]
[Route("api/member")]
[Authorize(Policy = "MemberOnly")]
public class MemberController : ControllerBase
{
    [HttpGet("dashboard-summary")]
    public IActionResult GetDashboardSummary()
    {
        return Ok(new
        {
            activeSubscriptions = 0,
            claimedCusDecs = 0,
            unreadNotifications = 0,
            pendingRequests = 0,
            recentActivity = Array.Empty<object>()
        });
    }
}
