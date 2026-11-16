namespace Cdns.Application.Feature.Subscriptions;

public interface IMemberSubscriptionRequestService
{
    Task<PagedResult<SubscriptionRequestSummaryDto>> GetRequestsAsync(
        int? status, string? search, int page, int pageSize, CancellationToken ct = default);

    Task<SubscriptionRequestDetailDto?> GetRequestAsync(long requestId, CancellationToken ct = default);

    Task<CompanyLookupDto?> LookupCompanyByTinAsync(string tin, CancellationToken ct = default);

    Task<CreateRequestResult> CreateRequestAsync(
        int companyId, int subscriptionType, string? note, CancellationToken ct = default);

    Task<CancelRequestResult> CancelRequestAsync(long requestId, CancellationToken ct = default);
}
