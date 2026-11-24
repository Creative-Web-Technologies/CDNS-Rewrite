using Cdns.Api.Endpoints.Subscriptions;
using Cdns.Application.Feature.Cusdec;
using Cdns.Application.Feature.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cdns.Api.Controllers;

/// <summary>
/// The CusDec page, shared by Member and Master. The controller only authorises
/// (either role) and shapes responses; all ownership and access rules live in
/// <see cref="ICusDecService"/>. A denied subscription is mapped through the shared
/// <see cref="SubscriptionAccessProblem"/> code strings (a not-owned subscription
/// surfaces as a 404, never revealing that it exists).
/// </summary>
[ApiController]
[Route("api/cusdec")]
[Authorize(Policy = "MemberOrMaster")]
public class CusDecController : ControllerBase
{
    private readonly ICusDecService _cusDecs;

    public CusDecController(ICusDecService cusDecs)
    {
        _cusDecs = cusDecs;
    }

    // GET /api/cusdec/subscriptions
    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(CancellationToken ct)
        => Ok(await _cusDecs.GetUsableSubscriptionsAsync(ct));

    // GET /api/cusdec/{subId}
    [HttpGet("{subId:long}")]
    public async Task<IActionResult> GetStrip(long subId, CancellationToken ct)
    {
        var result = await _cusDecs.GetSubscriptionStripAsync(subId, ct);
        return result.IsOk ? Ok(result.Value) : AccessProblem(result.Code);
    }

    // GET /api/cusdec/{subId}/cusdecs
    [HttpGet("{subId:long}/cusdecs")]
    public async Task<IActionResult> GetCusDecs(
        long subId,
        [FromQuery] string? tab,
        [FromQuery] string? state,
        [FromQuery] string? search,
        [FromQuery] string? office,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 4,
        [FromQuery] string? declarantTin = null,
        [FromQuery] string? consigneeTin = null,
        [FromQuery] string? cusDecNumber = null,
        CancellationToken ct = default)
    {
        var query = new CusDecListQuery(tab, state, search, office, from, to, sort, page, pageSize,
            declarantTin, consigneeTin, cusDecNumber);
        var result = await _cusDecs.GetCusDecsAsync(subId, query, ct);
        return result.IsOk ? Ok(result.Value) : AccessProblem(result.Code);
    }

    // GET /api/cusdec/{subId}/offices
    [HttpGet("{subId:long}/offices")]
    public async Task<IActionResult> GetOfficeCodes(long subId, CancellationToken ct)
    {
        var result = await _cusDecs.GetOfficeCodesAsync(subId, ct);
        return result.IsOk ? Ok(result.Value) : AccessProblem(result.Code);
    }

    // POST /api/cusdec/{subId}/cusdecs/{id}/acknowledge
    [HttpPost("{subId:long}/cusdecs/{id:long}/acknowledge")]
    public async Task<IActionResult> Acknowledge(long subId, long id, CancellationToken ct)
        => ActionResponse(await _cusDecs.AcknowledgeAsync(subId, id, ct));

    // POST /api/cusdec/{subId}/cusdecs/{id}/disclaim
    [HttpPost("{subId:long}/cusdecs/{id:long}/disclaim")]
    public async Task<IActionResult> Disclaim(long subId, long id, CancellationToken ct)
        => ActionResponse(await _cusDecs.DisclaimAsync(subId, id, ct));

    // POST /api/cusdec/{subId}/cusdecs/{id}/complete
    [HttpPost("{subId:long}/cusdecs/{id:long}/complete")]
    public async Task<IActionResult> Complete(long subId, long id, CancellationToken ct)
        => ActionResponse(await _cusDecs.CompleteAsync(subId, id, ct));

    // POST /api/cusdec/{subId}/cusdecs/{id}/reopen
    [HttpPost("{subId:long}/cusdecs/{id:long}/reopen")]
    public async Task<IActionResult> Reopen(long subId, long id, CancellationToken ct)
        => ActionResponse(await _cusDecs.ReopenAsync(subId, id, ct));

    // GET /api/cusdec/{subId}/cusdecs/{id}
    [HttpGet("{subId:long}/cusdecs/{id:long}")]
    public async Task<IActionResult> GetDetail(long subId, long id, CancellationToken ct)
    {
        var result = await _cusDecs.GetCusDecDetailAsync(subId, id, ct);
        return result.IsOk ? Ok(result.Value) : AccessProblem(result.Code);
    }

    // GET /api/cusdec/{subId}/cusdecs/{id}/timeline
    [HttpGet("{subId:long}/cusdecs/{id:long}/timeline")]
    public async Task<IActionResult> GetTimeline(long subId, long id, CancellationToken ct)
    {
        var result = await _cusDecs.GetCusDecTimelineAsync(subId, id, ct);
        return result.IsOk ? Ok(result.Value) : AccessProblem(result.Code);
    }

    private IActionResult ActionResponse(CusDecActionResponse response)
    {
        if (response.Access != SubscriptionAccessCode.Ok)
            return AccessProblem(response.Access);

        return response.Status switch
        {
            CusDecActionStatus.Ok               => Ok(response.Item),
            CusDecActionStatus.CusDecNotFound   => NotFound(new { code = "CusDecNotFound" }),
            CusDecActionStatus.AlreadyDisclaimed => Conflict(new { code = "AlreadyDisclaimed" }),
            CusDecActionStatus.AlreadyClaimed   => Conflict(new { code = "AlreadyClaimed" }),
            CusDecActionStatus.NotClaimed       => Conflict(new { code = "NotClaimed" }),
            _ => StatusCode(500),
        };
    }

    // Maps a subscription access failure to an MVC ProblemDetails, reusing the same
    // stable code strings the minimal-API SubscriptionAccessProblem emits: NotFound
    // → 404, every other failure → 403.
    private IActionResult AccessProblem(SubscriptionAccessCode code)
    {
        var status = code == SubscriptionAccessCode.NotFound
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status403Forbidden;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = status == StatusCodes.Status404NotFound
                ? "Subscription not found."
                : "Subscription access denied.",
        };
        problem.Extensions["code"] = SubscriptionAccessProblem.CodeString(code);
        return new ObjectResult(problem) { StatusCode = status };
    }
}
