using Cdns.Domain.Entities.CompanySubscriptionManagement;

namespace Cdns.Application.Feature.Subscriptions;

/// <summary>
/// Reason a subscription access check passed or failed. Evaluated in a fixed
/// order so the first failing rule wins (see <see cref="ISubscriptionAccessService.CheckAsync"/>).
/// </summary>
public enum SubscriptionAccessCode
{
    Ok = 0,
    NotFound,
    NotOwner,
    Inactive,
    Expired,
    CompanyInactive,
    KeyMissing,
    KeyMismatch,
}

/// <summary>
/// Outcome of a single-subscription access check. On <see cref="SubscriptionAccessCode.Ok"/>
/// <see cref="Subscription"/> carries the loaded (Company-included) row; otherwise it is null.
/// </summary>
public sealed record SubscriptionAccessResult(SubscriptionAccessCode Code, MemberSubscription? Subscription)
{
    public bool IsOk => Code == SubscriptionAccessCode.Ok;

    public static SubscriptionAccessResult Fail(SubscriptionAccessCode code) => new(code, null);

    public static SubscriptionAccessResult Ok(MemberSubscription subscription) =>
        new(SubscriptionAccessCode.Ok, subscription);
}

/// <summary>
/// The single gate for "may this caller use this subscription" (B01 + B02):
/// the shared SQL-translatable usability rule plus in-memory HMAC verification.
/// Access decisions must go through here; display/summary sites use the plain
/// <see cref="SubscriptionUsability"/> predicates instead.
/// </summary>
public interface ISubscriptionAccessService
{
    /// <summary>
    /// Checks one subscription for one caller. Order:
    /// NotFound → NotOwner → Inactive → Expired → CompanyInactive → KeyMissing →
    /// KeyMismatch → Ok. Each failure is logged at Warning with the id, caller and
    /// code only (never the key).
    /// </summary>
    Task<SubscriptionAccessResult> CheckAsync(
        long memberSubscriptionId, long callerMemberId, CancellationToken ct = default);

    /// <summary>
    /// Materialises an access list: applies the shared usability rule in SQL to the
    /// already-scoped <paramref name="scopedSource"/>, then drops rows whose HMAC
    /// key is missing or fails verification (HMAC can't run in SQL). Returned rows
    /// have their Company navigation loaded. Dropped rows are logged at Warning.
    /// </summary>
    Task<List<MemberSubscription>> GetUsableAsync(
        IQueryable<MemberSubscription> scopedSource, DateTimeOffset today, CancellationToken ct = default);
}
