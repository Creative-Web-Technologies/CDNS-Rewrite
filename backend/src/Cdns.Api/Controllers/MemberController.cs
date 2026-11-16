using Cdns.Application.Feature.Dashboard;
using Cdns.Application.Feature.Subscriptions;
using Cdns.Domain.Entities.Company;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cdns.Api.Controllers;

[ApiController]
[Route("api/member")]
[Authorize(Policy = "MemberOnly")]
public class MemberController : ControllerBase
{
    private readonly IMemberDashboardService _dashboard;
    private readonly IMemberSubscriptionService _subscriptions;
    private readonly IMemberSubscriptionRequestService _requests;

    public MemberController(
        IMemberDashboardService dashboard,
        IMemberSubscriptionService subscriptions,
        IMemberSubscriptionRequestService requests)
    {
        _dashboard = dashboard;
        _subscriptions = subscriptions;
        _requests = requests;
    }

    [HttpGet("dashboard-summary")]
    public async Task<ActionResult<MemberDashboardSummary>> GetDashboardSummary(CancellationToken ct)
        => Ok(await _dashboard.GetSummaryAsync(ct));

    [HttpGet("notifications/unread-count")]
    public async Task<IActionResult> GetUnreadNotificationCount(CancellationToken ct)
        => Ok(new { unread = await _dashboard.GetUnreadNotificationCountAsync(ct) });

    // GET /api/member/subscriptions
    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(CancellationToken ct)
        => Ok(await _subscriptions.GetActiveSubscriptionsAsync(ct));

    // GET /api/member/subscription-requests
    [HttpGet("subscription-requests")]
    public async Task<IActionResult> GetRequests(
        [FromQuery] int? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _requests.GetRequestsAsync(status, search, page, pageSize, ct));

    // GET /api/member/subscription-requests/{id}
    [HttpGet("subscription-requests/{id:long}")]
    public async Task<IActionResult> GetRequest(long id, CancellationToken ct)
    {
        var dto = await _requests.GetRequestAsync(id, ct);
        return dto is null ? NotFound(new { code = "NotFound" }) : Ok(dto);
    }

    // GET /api/member/companies/lookup
    [HttpGet("companies/lookup")]
    public async Task<IActionResult> LookupCompany([FromQuery] string tin, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tin))
            return BadRequest(new { code = "InvalidTin", field = "tin" });
        var dto = await _requests.LookupCompanyByTinAsync(tin, ct);
        return dto is null ? NotFound(new { code = "CompanyNotFound" }) : Ok(dto);
    }

    // POST /api/member/subscription-requests
    [HttpPost("subscription-requests")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateSubscriptionRequestBody body, CancellationToken ct)
    {
        if (body.CompanyId <= 0)
            return BadRequest(new { code = "InvalidCompanyId", field = "companyId" });
        if (body.SubscriptionType != (int)SubscriptionTypeEnum.Declarant && body.SubscriptionType != (int)SubscriptionTypeEnum.Consignee)
            return BadRequest(new { code = "InvalidSubscriptionType", field = "subscriptionType" });

        var result = await _requests.CreateRequestAsync(body.CompanyId, body.SubscriptionType, body.Note, ct);
        return result switch
        {
            CreateRequestResult.Success s => Created($"/api/member/subscription-requests/{s.RequestId}", new { requestId = s.RequestId, requestNumber = s.RequestNumber }),
            CreateRequestResult.CompanyNotFound => NotFound(new { code = "CompanyNotFound" }),
            CreateRequestResult.DuplicatePending => Conflict(new { code = "DuplicatePendingRequest" }),
            _ => StatusCode(500)
        };
    }

    // POST /api/member/subscription-requests/{id}/cancel
    [HttpPost("subscription-requests/{id:long}/cancel")]
    public async Task<IActionResult> CancelRequest(long id, CancellationToken ct)
    {
        var result = await _requests.CancelRequestAsync(id, ct);
        return result switch
        {
            CancelRequestResult.Success => Ok(),
            CancelRequestResult.NotFound => NotFound(new { code = "NotFound" }),
            CancelRequestResult.NotCancellable => Conflict(new { code = "NotCancellable" }),
            _ => StatusCode(500)
        };
    }
}

public record CreateSubscriptionRequestBody(int CompanyId, int SubscriptionType, string? Note);
