using System.Linq.Expressions;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Entities.CompanySubscriptionManagement;

namespace Cdns.Application.Feature.Subscriptions;

/// <summary>
/// Single definition of what makes a member subscription "usable" (B01), so the
/// access sites can't drift apart. "Usable" = status Activate, not past expiry
/// (Colombo business day), and the owning company Active and not soft-deleted.
///
/// HMAC verification (B02) is intentionally NOT part of these predicates: it
/// cannot be expressed in SQL and must run in memory after materialisation (see
/// ISubscriptionAccessService). These predicates are the SQL-translatable gate;
/// the access service applies them and then verifies the key.
///
/// The display helpers (<see cref="IsActiveForDisplay"/> /
/// <see cref="IsExpiredForDisplay"/>) are the status+expiry-only rules used by
/// summary counts and badges; they deliberately omit the company/HMAC gate so a
/// history count never drops rows, and both reference the same expiry boundary
/// (<see cref="ExpiryIsCurrent"/>) so active and expired can't disagree.
/// </summary>
public static class SubscriptionUsability
{
    // ── Full "usable" rule (status + expiry + company) ────────────────────────

    /// <summary>
    /// EF-translatable usable predicate WITHOUT the owner check. Callers that
    /// scope by company or master (Master list, company dashboard) pre-filter the
    /// query and apply this.
    /// </summary>
    public static Expression<Func<MemberSubscription, bool>> UsableExpression(DateTimeOffset today) =>
        s => s.MemberSubscriptionStatusEnum == (int)MemberSubscriptionStatusEnum.Activate
             && s.ExpiryDate != null && s.ExpiryDate >= today
             && s.Company != null
             && s.Company.CompanyStatusEnum == (int)CompanyStatusEnum.Active
             && !s.Company.IsDeleted;

    /// <summary>
    /// EF-translatable usable predicate WITH the owner check, for the member's own
    /// subscriptions (switcher list, member dashboard).
    /// </summary>
    public static Expression<Func<MemberSubscription, bool>> UsableExpression(long ownerMemberId, DateTimeOffset today) =>
        s => s.MemberId == ownerMemberId
             && s.MemberSubscriptionStatusEnum == (int)MemberSubscriptionStatusEnum.Activate
             && s.ExpiryDate != null && s.ExpiryDate >= today
             && s.Company != null
             && s.Company.CompanyStatusEnum == (int)CompanyStatusEnum.Active
             && !s.Company.IsDeleted;

    /// <summary>
    /// In-memory counterpart of <see cref="UsableExpression(DateTimeOffset)"/>,
    /// kept byte-for-byte equivalent so the two can't diverge. Exists so the
    /// in-memory sites can share the rule, and so a test can assert agreement.
    /// </summary>
    public static bool IsUsable(MemberSubscription s, DateTimeOffset today) =>
        s.MemberSubscriptionStatusEnum == (int)MemberSubscriptionStatusEnum.Activate
        && s.ExpiryDate != null && s.ExpiryDate >= today
        && s.Company != null
        && s.Company.CompanyStatusEnum == (int)CompanyStatusEnum.Active
        && !s.Company.IsDeleted;

    // ── Display-only rules (status + expiry, no company/HMAC) ─────────────────

    /// <summary>The shared expiry boundary: has an expiry and it is today or later.</summary>
    public static bool ExpiryIsCurrent(DateTimeOffset? expiryDate, DateTimeOffset today) =>
        expiryDate is not null && expiryDate >= today;

    /// <summary>Active for a summary/badge: Activate status and not past expiry.</summary>
    public static bool IsActiveForDisplay(int statusEnum, DateTimeOffset? expiryDate, DateTimeOffset today) =>
        statusEnum == (int)MemberSubscriptionStatusEnum.Activate
        && ExpiryIsCurrent(expiryDate, today);

    /// <summary>
    /// Expired for a summary: Expired status, or it has an expiry that is in the
    /// past. Defined as the inverse of the shared expiry boundary so it can never
    /// drift from <see cref="IsActiveForDisplay"/>.
    /// </summary>
    public static bool IsExpiredForDisplay(int statusEnum, DateTimeOffset? expiryDate, DateTimeOffset today) =>
        statusEnum == (int)MemberSubscriptionStatusEnum.Expired
        || (expiryDate is not null && !ExpiryIsCurrent(expiryDate, today));
}
