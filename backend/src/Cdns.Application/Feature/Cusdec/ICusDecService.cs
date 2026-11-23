namespace Cdns.Application.Feature.Cusdec;

/// <summary>
/// The shared CusDec page backend, used by both Member and Master. Both roles see
/// only their own subscriptions (MemberId == caller). CusDec ownership (a
/// declaration must belong to the subscription's company on the subscription's
/// side) is enforced here, not in the database.
/// </summary>
public interface ICusDecService
{
    /// <summary>Endpoint 1 — the subscriptions the caller may use, for the switcher.</summary>
    Task<IReadOnlyList<CusDecSubscriptionDto>> GetUsableSubscriptionsAsync(
        CancellationToken ct = default);

    /// <summary>Endpoint 2 — subscription strip data plus the four tab counts.</summary>
    Task<CusDecAccess<CusDecSubscriptionStripDto>> GetSubscriptionStripAsync(
        long memberSubscriptionId, CancellationToken ct = default);

    /// <summary>Endpoint 3 — a filtered, sorted, paged page of declarations for a tab.</summary>
    Task<CusDecAccess<CusDecListResponse>> GetCusDecsAsync(
        long memberSubscriptionId, CusDecListQuery query, CancellationToken ct = default);

    /// <summary>Endpoint 4 — acknowledge (legacy Claim) a declaration. Idempotent.</summary>
    Task<CusDecActionResponse> AcknowledgeAsync(
        long memberSubscriptionId, long cusDecId, CancellationToken ct = default);

    /// <summary>Endpoint 5 — disclaim a declaration. Permanent; 409 if already claimed.</summary>
    Task<CusDecActionResponse> DisclaimAsync(
        long memberSubscriptionId, long cusDecId, CancellationToken ct = default);

    /// <summary>
    /// Endpoint 6 — mark a declaration Completed. Requires the side to be claimed
    /// and not disclaimed. Idempotent: a no-op if already completed.
    /// </summary>
    Task<CusDecActionResponse> CompleteAsync(
        long memberSubscriptionId, long cusDecId, CancellationToken ct = default);

    /// <summary>
    /// Endpoint 7 — reopen a Completed declaration back to In-progress by clearing
    /// the completed fields. No-op if not currently completed.
    /// </summary>
    Task<CusDecActionResponse> ReopenAsync(
        long memberSubscriptionId, long cusDecId, CancellationToken ct = default);

    /// <summary>
    /// Endpoint 8 — distinct, sorted office codes visible to the subscription
    /// (company + side), across all tabs. Used by the UI office filter dropdown.
    /// </summary>
    Task<CusDecAccess<string[]>> GetOfficeCodesAsync(
        long memberSubscriptionId, CancellationToken ct = default);
}

/// <summary>
/// List query parameters. <paramref name="Tab"/> is "tracking" | "completed" |
/// "disclaimed"; <paramref name="State"/> is "all" | "new" | "inprogress" (only
/// meaningful for the tracking tab). <paramref name="From"/>/<paramref name="To"/>
/// are inclusive Asia/Colombo calendar days; the date they filter follows the tab.
/// <paramref name="DeclarantTin"/>/<paramref name="ConsigneeTin"/> are starts-with
/// matches on the respective TIN columns (internal whitespace stripped; ignored if
/// empty or longer than 20 chars after stripping). <paramref name="CusDecNumber"/>
/// is a case-insensitive CONTAINS on CusDecNumber only (independent of Search).
/// </summary>
public sealed record CusDecListQuery(
    string? Tab,
    string? State,
    string? Search,
    string? Office,
    DateOnly? From,
    DateOnly? To,
    string? Sort,
    int Page,
    int PageSize,
    string? DeclarantTin = null,
    string? ConsigneeTin = null,
    string? CusDecNumber = null);
