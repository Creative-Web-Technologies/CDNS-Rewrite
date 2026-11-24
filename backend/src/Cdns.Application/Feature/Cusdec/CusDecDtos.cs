using Cdns.Application.Feature.Subscriptions;

namespace Cdns.Application.Feature.Cusdec;

// ── Switcher (endpoint 1) ─────────────────────────────────────────────────────

/// <summary>
/// One usable subscription for the CusDec page switcher. Same shape for Member
/// and Master; only the ownership scope that produced it differs.
/// </summary>
public sealed record CusDecSubscriptionDto(
    long MemberSubscriptionId,
    string SubscriptionType,        // "Declarant" | "Consignee"
    string CompanyName,
    string CompanyTin,
    string PlanName,
    DateTimeOffset ExpiryDate,
    string ActionType,              // "General" | "AcknowledgementOnly"
    bool OnlyForAcknowledgement);

// ── Subscription strip + tab counts (endpoint 2) ─────────────────────────────

/// <summary>Per-tab counts, computed server-side from claim/disclaim/complete columns.</summary>
public sealed record CusDecTabCounts(
    int TrackingNew,
    int TrackingInProgress,
    int Completed,
    int Disclaimed);

/// <summary>
/// The subscription strip above the tabs plus the four tab counts. When the
/// subscription is OnlyForAcknowledgement, <see cref="ContainersBlocked"/> is true
/// (container features are not built yet).
/// </summary>
public sealed record CusDecSubscriptionStripDto(
    long MemberSubscriptionId,
    string SubscriptionType,
    string CompanyName,
    string CompanyTin,
    string PlanName,
    DateTimeOffset ExpiryDate,
    string ActionType,
    bool OnlyForAcknowledgement,
    bool ContainersBlocked,
    CusDecTabCounts Counts);

// ── List item (endpoint 3) + action result item ──────────────────────────────

/// <summary>
/// A single declaration row, same shape for Member and Master. "Who" fields carry
/// the acting member's display name (null when the action has not happened yet).
/// <see cref="IsExited"/> is only meaningful for In-progress rows; it signals that
/// at least one of the declaration's visible status rows is a known exit status,
/// so the user may want to mark it Completed. It is false for all other tabs.
/// </summary>
public sealed record CusDecListItemDto(
    long CusDecId,
    string? CusDecNumber,
    DateTimeOffset? CusDecDate,
    string? Office,
    string? DeclarantName,
    string? ConsigneeName,
    string? CurrentStatus,
    DateTimeOffset? LastUpdate,
    string Tab,                     // "tracking" | "completed" | "disclaimed"
    string? State,                  // "new" | "inprogress" (tracking only; else null)
    DateTimeOffset? ClaimedDate,
    string? ClaimedByName,
    DateTimeOffset? DisclaimedDate,
    string? DisclaimedByName,
    DateTimeOffset? CompletedAt,
    string? CompletedByName,
    bool IsExited);

/// <summary>Paged list response plus the OnlyForAcknowledgement container flag.</summary>
public sealed record CusDecListResponse(
    IReadOnlyList<CusDecListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    bool ContainersBlocked);

// ── Detail (endpoint 9) ───────────────────────────────────────────────────────

public sealed record CusDecItemDto(
    long CusDecItemId,
    int ItemNumber,
    string? HsCode,
    string? Description,
    string? Quantity,
    decimal? UnitPrice,
    decimal? TotalValue,
    string? BlNumber);

public sealed record CusDecTaxDto(
    long CusDecTaxId,
    string? TaxCode,
    decimal TaxAmount,
    int TaxRank);

public sealed record CusDecTaxGroupDto(
    int ItemNumber,
    IReadOnlyList<CusDecTaxDto> Taxes);

public sealed record CusDecInspectionSectionDto(
    long CusDecInspectionSectionTabId,
    int KeySecNumber,
    string? SectionName,
    DateTimeOffset? SectionDate);

public sealed record CusDecInspectionDto(
    long CusDecInspectionId,
    long InspectionInstanceId,
    string? IssueCode,
    string? Description,
    decimal? Value,
    int KeyFrdNumber,
    string? FrdNfr,
    string? FrdUni,
    string? FrdUbr,
    IReadOnlyList<CusDecInspectionSectionDto> Sections);

public sealed record CusDecDetailDto(
    long CusDecId,
    string? CusDecNumber,
    DateTimeOffset? CusDecDate,
    DateTimeOffset? RegistrationDate,
    DateTimeOffset? ReleaseDate,
    string? OfficeCode,
    string? Origin,
    string? Destination,
    string? CusDecType,
    string? Channel,
    string? Incoterm,
    string? BlNumber,
    decimal? InvoiceValue,
    int? NoOfItems,
    int NoOfContainers,
    string? Exporter,
    string? SupplierCompanyName,
    string? DeclarantCompanyName,
    string? DeclarantTin,
    string? ConsigneeCompanyName,
    string? ConsigneeTin,
    string Tab,
    string? State,
    string? CurrentStatus,
    DateTimeOffset? LastUpdate,
    bool IsExited,
    IReadOnlyList<CusDecItemDto> Items,
    bool ItemsTruncated,
    IReadOnlyList<CusDecTaxGroupDto> Taxes,
    IReadOnlyList<CusDecInspectionDto> Inspections);

// ── Timeline (endpoint 10) ────────────────────────────────────────────────────

public sealed record CusDecTimelineRowDto(
    string? Label,
    DateTimeOffset? DateUtc,
    string Kind);  // "document" | "inspection"

public sealed record CusDecTimelineDto(
    IReadOnlyList<CusDecTimelineRowDto> Items,
    bool Truncated,
    string? CurrentStatus,
    DateTimeOffset? LastUpdate,
    bool IsExited,
    string State);  // "inprogress" | "completed"

// ── Access wrapper ───────────────────────────────────────────────────────────

/// <summary>
/// Wraps a CusDec service result with the subscription access outcome. On a
/// non-Ok <see cref="Code"/> the <see cref="Value"/> is null and the Api layer
/// maps the code to a ProblemDetails (NotFound → 404, everything else → 403) via
/// the shared SubscriptionAccessProblem mapper. A subscription the caller does not
/// own collapses to <see cref="SubscriptionAccessCode.NotFound"/> in the service,
/// so ownership is never revealed.
/// </summary>
public sealed record CusDecAccess<T>(SubscriptionAccessCode Code, T? Value) where T : class
{
    public bool IsOk => Code == SubscriptionAccessCode.Ok;

    public static CusDecAccess<T> Ok(T value) => new(SubscriptionAccessCode.Ok, value);
    public static CusDecAccess<T> Fail(SubscriptionAccessCode code) => new(code, null);
}

// ── Action results (acknowledge / disclaim / complete / reopen) ───────────────

/// <summary>CusDec-level outcome of a mutation action, once the subscription is usable.</summary>
public enum CusDecActionStatus
{
    Ok = 0,
    CusDecNotFound,     // declaration not in this subscription's company/side → 404
    AlreadyDisclaimed,  // acknowledge rejected: already disclaimed → 409
    AlreadyClaimed,     // disclaim rejected: already claimed → 409
    NotClaimed,         // complete rejected: not yet acknowledged → 409
}

/// <summary>
/// Result of an action endpoint. When <see cref="Access"/> is not Ok the
/// subscription itself was denied (mapped to a ProblemDetails). Otherwise
/// <see cref="Status"/> carries the CusDec-level outcome and, on success,
/// <see cref="Item"/> is the updated row (the UI patches in place).
/// </summary>
public sealed record CusDecActionResponse(
    SubscriptionAccessCode Access,
    CusDecActionStatus Status,
    CusDecListItemDto? Item)
{
    public static CusDecActionResponse Denied(SubscriptionAccessCode code) =>
        new(code, CusDecActionStatus.CusDecNotFound, null);

    public static CusDecActionResponse Of(CusDecActionStatus status, CusDecListItemDto? item = null) =>
        new(SubscriptionAccessCode.Ok, status, item);
}
