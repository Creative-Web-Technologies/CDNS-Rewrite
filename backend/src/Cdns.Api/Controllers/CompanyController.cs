using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cdns.Api.Controllers;

[ApiController]
[Route("api/company")]
[Authorize(Policy = "MasterOnly")]
public class CompanyController : ControllerBase
{
    [HttpGet("dashboard-summary")]
    public IActionResult GetDashboardSummary()
    {
        return Ok(new
        {
            companyMembers = 0,
            pendingSubscriptionRequests = 0,
            activeSubscriptions = 0,
            paymentsDue = 0,
            recentActivity = Array.Empty<object>()
        });
    }
}
