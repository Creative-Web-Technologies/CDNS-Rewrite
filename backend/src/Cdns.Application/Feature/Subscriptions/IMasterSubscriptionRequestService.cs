namespace Cdns.Application.Feature.Subscriptions;

public interface IMasterSubscriptionRequestService
{
    /// <summary>L1 – All requests for companies the master administers, excluding master's own.</summary>
    Task<PagedResult<MasterSubscriptionRequestSummaryDto>> GetRequestsAsync(
        int? status,
        string? requestNumber,
        string? memberEmail,
        string? search,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>L2 – Pending-only view (same filters minus status).</summary>
    Task<PagedResult<MasterSubscriptionRequestSummaryDto>> GetPendingRequestsAsync(
        string? requestNumber,
        string? memberEmail,
        string? search,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>L3 – View a single request detail; null = not found / not owned.</summary>
    Task<MasterSubscriptionRequestDetailDto?> GetRequestAsync(long requestId, CancellationToken ct = default);

    /// <summary>L4 – Reject a pending request.</summary>
    Task<RejectRequestResult> RejectRequestAsync(long requestId, string comment, CancellationToken ct = default);

    /// <summary>L5 – Plans for plan picker: all active plans for own request; type-filtered for a member's request.</summary>
    Task<IReadOnlyList<SubscriptionPlanDto>?> GetPlansForRequestAsync(long requestId, CancellationToken ct = default);

    /// <summary>L6 – Approve (atomic): creates subscription, updates request, sends notification.</summary>
    Task<ApproveRequestResult> ApproveRequestAsync(long requestId, int planId, CancellationToken ct = default);

    /// <summary>L7 – Master self-creates a Consignee Pending request using their own (first) company; no body needed.</summary>
    Task<CreateMasterRequestResult> CreateSelfRequestAsync(CancellationToken ct = default);
}
