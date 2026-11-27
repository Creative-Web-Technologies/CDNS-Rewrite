using Cdns.Api.Endpoints.Subscriptions;
using Cdns.Application.Feature.Container;
using Cdns.Application.Feature.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cdns.Api.Controllers;

/// <summary>
/// Container page, shared by Member and Master. Access requires a General subscription
/// (AcknowledgementOnly subscriptions are blocked at the service layer and return 403
/// with code "SubscriptionAcknowledgementOnly").
/// Routes:
///   GET /api/cusdec/{subId}/containers
///   GET /api/cusdec/{subId}/containers/{containerId}
///   GET /api/cusdec/{subId}/containers/{containerId}/status
/// </summary>
[ApiController]
[Route("api/cusdec")]
[Authorize(Policy = "MemberOrMaster")]
public class ContainerController : ControllerBase
{
    private readonly IContainerService _containers;

    public ContainerController(IContainerService containers)
    {
        _containers = containers;
    }

    // GET /api/cusdec/{subId}/containers
    [HttpGet("{subId:long}/containers")]
    public async Task<IActionResult> GetContainers(
        long subId,
        [FromQuery] long? cusDecId,
        [FromQuery] string? search,
        [FromQuery] string? location,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = new ContainerListQuery(cusDecId, search, location, status, page, pageSize);
        var result = await _containers.GetContainersAsync(subId, query, ct);
        return result.IsOk ? Ok(result.Value) : AccessProblem(result.Code);
    }

    // GET /api/cusdec/{subId}/containers/{containerId}
    [HttpGet("{subId:long}/containers/{containerId:long}")]
    public async Task<IActionResult> GetDetail(long subId, long containerId, CancellationToken ct)
    {
        var result = await _containers.GetContainerDetailAsync(subId, containerId, ct);
        return result.IsOk ? Ok(result.Value) : AccessProblem(result.Code);
    }

    // GET /api/cusdec/{subId}/containers/{containerId}/status
    [HttpGet("{subId:long}/containers/{containerId:long}/status")]
    public async Task<IActionResult> GetStatus(long subId, long containerId, CancellationToken ct)
    {
        var result = await _containers.GetContainerHistoryAsync(subId, containerId, ct);
        return result.IsOk ? Ok(result.Value) : AccessProblem(result.Code);
    }

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
