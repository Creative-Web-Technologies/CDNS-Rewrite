namespace Cdns.Application.Feature.Subscriptions;

// ── Request list / detail DTOs ────────────────────────────────────────────────

public record MasterSubscriptionRequestSummaryDto(
    long SubscriptionRequestId,
    string? RequestNumber,
    DateTimeOffset RequestedDateTime,
    string MemberName,
    string MemberEmail,
    string SubscriptionType,
    string Status
);

public record MasterSubscriptionRequestDetailDto(
    long SubscriptionRequestId,
    string? RequestNumber,
    DateTimeOffset RequestedDateTime,
    string MemberName,
    string MemberEmail,
    string CompanyName,
    string? CompanyTin,
    string SubscriptionType,
    string Status,
    string? Note,
    string? ResponderComment,
    DateTimeOffset? RespondDateTime,
    string? ResponderName
);

// ── Subscription plan DTO (for approve flow) ──────────────────────────────────

public record SubscriptionPlanDto(
    int SubscriptionId,
    string SubscriptionName,
    string Description,
    decimal Amount,
    int NoOfActiveDuration,
    string DurationType,
    string SubscriptionType,
    string ActionType
);

// ── Subscription list / detail DTOs ──────────────────────────────────────────

public record MasterSubscriptionSummaryDto(
    long MemberSubscriptionId,
    string? SubscriptionNumber,
    string MemberName,
    string MemberEmail,
    string SubscriptionType,
    string SubscriptionName,
    DateTimeOffset StartDate,
    DateTimeOffset? ExpiryDate,
    string Status,
    bool IsActive
);

public record MasterSubscriptionDetailDto(
    long MemberSubscriptionId,
    string? SubscriptionNumber,
    string MemberName,
    string MemberEmail,
    string CompanyName,
    string? CompanyTin,
    string SubscriptionType,
    string SubscriptionName,
    decimal SubscriptionAmount,
    DateTimeOffset StartDate,
    DateTimeOffset? ExpiryDate,
    string Status,
    bool IsActive,
    string? RequestNumber,
    IReadOnlyList<SubscriptionStatusHistoryDto> StatusHistory
);

public record SubscriptionStatusHistoryDto(
    long Id,
    string Status,
    string? Comment,
    DateTimeOffset CreatedDateTime
);

// ── Result types ──────────────────────────────────────────────────────────────

public abstract record RejectRequestResult
{
    public sealed record Success : RejectRequestResult;
    public sealed record NotFound : RejectRequestResult;
    public sealed record NotPending : RejectRequestResult;
}

public abstract record ApproveRequestResult
{
    public sealed record Success(long MemberSubscriptionId, string Number) : ApproveRequestResult;
    public sealed record NotFound : ApproveRequestResult;
    public sealed record NotPending : ApproveRequestResult;
    public sealed record PlanNotFound : ApproveRequestResult;
}

public abstract record DeactivateResult
{
    public sealed record Success : DeactivateResult;
    public sealed record NotFound : DeactivateResult;
    public sealed record NotActive : DeactivateResult;
}

public abstract record CreateMasterRequestResult
{
    public sealed record Success(long RequestId, string RequestNumber) : CreateMasterRequestResult;
    public sealed record CompanyNotMapped : CreateMasterRequestResult;
}
