using Cdns.Application.Feature.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cdns.Api.Controllers;

[ApiController]
[Route("api/master")]
[Authorize(Policy = "MasterOnly")]
public class MasterController : ControllerBase
{
    private readonly IMasterSubscriptionRequestService _requests;
    private readonly IMasterSubscriptionService _subscriptions;

    public MasterController(
        IMasterSubscriptionRequestService requests,
        IMasterSubscriptionService subscriptions)
    {
        _requests = requests;
        _subscriptions = subscriptions;
    }

    // ── Subscription Requests ─────────────────────────────────────────────────

    // GET /api/master/subscription-requests
    [HttpGet("subscription-requests")]
    public async Task<IActionResult> GetRequests(
        [FromQuery] int? status,
        [FromQuery] string? requestNumber,
        [FromQuery] string? memberEmail,
        [FromQuery] string? search,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _requests.GetRequestsAsync(
            status, requestNumber, memberEmail, search, fromDate, toDate, page, pageSize, ct));

    // GET /api/master/subscription-requests/pending
    [HttpGet("subscription-requests/pending")]
    public async Task<IActionResult> GetPendingRequests(
        [FromQuery] string? requestNumber,
        [FromQuery] string? memberEmail,
        [FromQuery] string? search,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _requests.GetPendingRequestsAsync(
            requestNumber, memberEmail, search, fromDate, toDate, page, pageSize, ct));

    // POST /api/master/subscription-requests  (L7 self-create)
    [HttpPost("subscription-requests")]
    public async Task<IActionResult> CreateMasterRequest(
        [FromBody] CreateMasterRequestBody body, CancellationToken ct)
    {
        if (body.CompanyId <= 0)
            return BadRequest(new { code = "InvalidCompanyId", field = "companyId" });

        var result = await _requests.CreateMasterRequestAsync(body.CompanyId, body.Note, ct);
        return result switch
        {
            CreateMasterRequestResult.Success s =>
                Created(
                    $"/api/master/subscription-requests/{s.RequestId}",
                    new { requestId = s.RequestId, requestNumber = s.RequestNumber }),
            CreateMasterRequestResult.CompanyNotMapped =>
                NotFound(new { code = "CompanyNotMapped" }),
            _ => StatusCode(500),
        };
    }

    // GET /api/master/subscription-requests/{id}
    [HttpGet("subscription-requests/{id:long}")]
    public async Task<IActionResult> GetRequest(long id, CancellationToken ct)
    {
        var dto = await _requests.GetRequestAsync(id, ct);
        return dto is null ? NotFound(new { code = "NotFound" }) : Ok(dto);
    }

    // POST /api/master/subscription-requests/{id}/reject
    [HttpPost("subscription-requests/{id:long}/reject")]
    public async Task<IActionResult> RejectRequest(
        long id, [FromBody] RejectRequestBody body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Comment))
            return BadRequest(new { code = "CommentRequired", field = "comment" });

        var result = await _requests.RejectRequestAsync(id, body.Comment, ct);
        return result switch
        {
            RejectRequestResult.Success    => Ok(),
            RejectRequestResult.NotFound   => NotFound(new { code = "NotFound" }),
            RejectRequestResult.NotPending => Conflict(new { code = "NotPending" }),
            _ => StatusCode(500),
        };
    }

    // GET /api/master/subscription-requests/{id}/plans
    [HttpGet("subscription-requests/{id:long}/plans")]
    public async Task<IActionResult> GetPlans(long id, CancellationToken ct)
    {
        var plans = await _requests.GetPlansForRequestAsync(id, ct);
        if (plans is null) return NotFound(new { code = "NotFound" });
        return Ok(plans);
    }

    // POST /api/master/subscription-requests/{id}/approve
    [HttpPost("subscription-requests/{id:long}/approve")]
    public async Task<IActionResult> ApproveRequest(
        long id, [FromBody] ApproveRequestBody body, CancellationToken ct)
    {
        if (body.PlanId <= 0)
            return BadRequest(new { code = "InvalidPlanId", field = "planId" });

        var result = await _requests.ApproveRequestAsync(id, body.PlanId, ct);
        return result switch
        {
            ApproveRequestResult.Success s =>
                Ok(new { memberSubscriptionId = s.MemberSubscriptionId, number = s.Number }),
            ApproveRequestResult.NotFound   => NotFound(new { code = "NotFound" }),
            ApproveRequestResult.NotPending => Conflict(new { code = "NotPending" }),
            ApproveRequestResult.PlanNotFound => NotFound(new { code = "PlanNotFound" }),
            _ => StatusCode(500),
        };
    }

    // ── Subscriptions ─────────────────────────────────────────────────────────

    // GET /api/master/subscriptions
    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(
        [FromQuery] int? status,
        [FromQuery] string? subscriptionNumber,
        [FromQuery] string? memberEmail,
        [FromQuery] string? search,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _subscriptions.GetSubscriptionsAsync(
            status, subscriptionNumber, memberEmail, search, fromDate, toDate, page, pageSize, ct));

    // GET /api/master/subscriptions/{id}
    [HttpGet("subscriptions/{id:long}")]
    public async Task<IActionResult> GetSubscription(long id, CancellationToken ct)
    {
        var dto = await _subscriptions.GetSubscriptionAsync(id, ct);
        return dto is null ? NotFound(new { code = "NotFound" }) : Ok(dto);
    }

    // POST /api/master/subscriptions/{id}/deactivate
    [HttpPost("subscriptions/{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(
        long id, [FromBody] DeactivateBody body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Comment))
            return BadRequest(new { code = "CommentRequired", field = "comment" });

        var result = await _subscriptions.DeactivateAsync(id, body.Comment, ct);
        return result switch
        {
            DeactivateResult.Success   => Ok(),
            DeactivateResult.NotFound  => NotFound(new { code = "NotFound" }),
            DeactivateResult.NotActive => Conflict(new { code = "NotActive" }),
            _ => StatusCode(500),
        };
    }
}

public record CreateMasterRequestBody(int CompanyId, string? Note);
public record RejectRequestBody(string Comment);
public record ApproveRequestBody(int PlanId);
public record DeactivateBody(string Comment);
