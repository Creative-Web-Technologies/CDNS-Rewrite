using Cdns.Application.Feature.Cusdec;
using Cdns.Application.Feature.Subscriptions;

namespace Cdns.Application.Feature.Container;

/// <summary>
/// Container page backend (Member + Master). The gate order for each data endpoint:
///   1. Full subscription gate (NotFound → NotOwner → Inactive → Expired → CompanyInactive
///      → KeyMissing → KeyMismatch).
///   2. AcknowledgementOnly check: if the subscription is OnlyForAcknowledgement, return
///      <see cref="SubscriptionAccessCode.AcknowledgementOnly"/> (containers require General).
///   3. Data scope: containers linked to CusDeclarations on the subscription's company side.
/// </summary>
public interface IContainerService
{
    /// <summary>Endpoint 1 — paginated list of containers for a subscription.</summary>
    Task<CusDecAccess<ContainerListResponse>> GetContainersAsync(
        long memberSubscriptionId, ContainerListQuery query, CancellationToken ct = default);

    /// <summary>Endpoint 2 — full detail for one container.</summary>
    Task<CusDecAccess<ContainerDetailDto>> GetContainerDetailAsync(
        long memberSubscriptionId, long containerContainerId, CancellationToken ct = default);

    /// <summary>Endpoint 3 — full status history for one container.</summary>
    Task<CusDecAccess<ContainerHistoryDto>> GetContainerHistoryAsync(
        long memberSubscriptionId, long containerContainerId, CancellationToken ct = default);
}

// ── List (endpoint 1) ────────────────────────────────────────────────────────

/// <summary>
/// Query parameters for the container list.
/// <paramref name="CusDecId"/> narrows to one CusDec (must be visible to the subscription;
///   invisible → 404, visible with no containers → 200 empty list).
/// <paramref name="Search"/> is a case-insensitive CONTAINS over container number, location,
///   current status and CusDec number. % and _ in the search string are literal.
/// <paramref name="Location"/> and <paramref name="Status"/> are case-insensitive CONTAINS
///   filters on the respective columns.
/// Sort is always ArrivalDate DESC, nulls last, then CusDecContainerId DESC.
/// Default page size 10, max 50.
/// </summary>
public sealed record ContainerListQuery(
    long? CusDecId,
    string? Search,
    string? Location,
    string? Status,
    int Page,
    int PageSize);

public sealed record ContainerListItemDto(
    long CusDecContainerId,
    string? ContainerNumber,
    long? CusDecId,
    string? CusDecNumber,
    DateTimeOffset? CusDecDate,
    DateTimeOffset? ArrivalDate,
    string? Location,
    string? Vessel,
    string? Type,
    int? ContainerPassNumber,
    string? CurrentStatus);

/// <summary>Echoed cusDecId filter — null when no filter was applied.</summary>
public sealed record ContainerListFilter(long CusDecId, string? CusDecNumber);

/// <summary>
/// Paged list of containers.
/// When a <c>cusDecId</c> filter was applied, <see cref="Filter"/> carries back the
/// matched CusDec identity; null when no filter was sent.
/// </summary>
public sealed record ContainerListResponse(
    IReadOnlyList<ContainerListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    ContainerListFilter? Filter = null);

// ── Detail (endpoint 2) ──────────────────────────────────────────────────────

public sealed record ContainerDetailDto(
    long CusDecContainerId,
    string? ContainerNumber,
    long? CusDecId,
    string? CusDecNumber,
    DateTimeOffset? CusDecDate,
    DateTimeOffset? ArrivalDate,
    string? Location,
    string? Vessel,
    string? Voyage,
    string? Size,
    string? Type,
    string? CurrentStatus,
    int? ContainerPassNumber,
    string? CusDecType,
    string? Channel,
    string? Origin,
    string? BlNumber,
    decimal? InvoiceValue,
    string? DeclarantName,
    string? DeclarantTin,
    string? ConsigneeName,
    string? ConsigneeTin);

// ── History (endpoint 3) ─────────────────────────────────────────────────────

public sealed record ContainerHistoryRowDto(
    string? Status,
    string? Description,
    DateTimeOffset? StatusDateTime);

public sealed record ContainerHistoryDto(
    IReadOnlyList<ContainerHistoryRowDto> Rows,
    string? CurrentStatus,
    DateTimeOffset? LastStatusDate);
