namespace Cdns.Application.Feature.Subscriptions;

public interface IMasterSubscriptionService
{
    /// <summary>L9 – List subscriptions issued by this master.</summary>
    Task<PagedResult<MasterSubscriptionSummaryDto>> GetSubscriptionsAsync(
        int? status,
        string? subscriptionNumber,
        string? memberEmail,
        string? search,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>L10 – View a single subscription with status history; null = not found / not owned.</summary>
    Task<MasterSubscriptionDetailDto?> GetSubscriptionAsync(long memberSubscriptionId, CancellationToken ct = default);

    /// <summary>L11 – Deactivate (atomic): writes history entry, sets status to Deactivate.</summary>
    Task<DeactivateResult> DeactivateAsync(long memberSubscriptionId, string comment, CancellationToken ct = default);
}
