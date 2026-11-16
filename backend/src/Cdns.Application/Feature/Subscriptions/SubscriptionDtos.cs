namespace Cdns.Application.Feature.Subscriptions;

public record ActiveSubscriptionDto(
    long MemberSubscriptionId,
    string CompanyName,
    string CompanyTin,
    string SubscriptionType,
    string PlanName,
    string PlanDuration,
    DateTimeOffset ExpiryDate,
    bool IsAcknowledgementOnly
);

public record SubscriptionRequestSummaryDto(
    long SubscriptionRequestId,
    string? RequestNumber,
    DateTimeOffset RequestedDateTime,
    string CompanyName,
    string? CompanyTin,
    string SubscriptionType,
    string Status
);

public record SubscriptionRequestDetailDto(
    long SubscriptionRequestId,
    string? RequestNumber,
    DateTimeOffset RequestedDateTime,
    string CompanyName,
    string? CompanyTin,
    string SubscriptionType,
    string Status,
    string? ResponderName,
    string? ResponderComment,
    DateTimeOffset? RespondDateTime,
    string? Note
);

public record CompanyLookupDto(int CompanyId, string Name, string Tin);

public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);

// Result type hierarchy for CreateRequest
public abstract record CreateRequestResult
{
    public sealed record Success(long RequestId, string RequestNumber) : CreateRequestResult;
    public sealed record DuplicatePending : CreateRequestResult;
    public sealed record CompanyNotFound : CreateRequestResult;
}

// Result type for Cancel
public abstract record CancelRequestResult
{
    public sealed record Success : CancelRequestResult;
    public sealed record NotFound : CancelRequestResult;
    public sealed record NotCancellable : CancelRequestResult;
}
