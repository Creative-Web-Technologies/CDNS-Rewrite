using Cdns.Application.Feature.Dashboard;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Persistence;
using Cdns.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Services.Dashboard;

/// <summary>
/// Shared data-loading for both dashboard services. Resolves which declarations
/// are visible under a set of active subscriptions (TIN first-9 match, keyed to
/// the subscription side) and translates each declaration's ASYCUDA status
/// history into CDNS terms before handing plain views to
/// <see cref="DashboardCalculator"/>.
/// </summary>
internal static class DashboardSupport
{
    /// <summary>First nine characters of a TIN — the legacy match key.</summary>
    internal static string? Key(string? tin) =>
        string.IsNullOrEmpty(tin) ? null : tin.Length <= 9 ? tin : tin[..9];

    /// <summary>
    /// A subscription's visibility scope: the matched company TIN key and which
    /// side (declarant vs consignee) of a declaration it unlocks.
    /// </summary>
    internal readonly record struct Scope(string TinKey, SubscriptionType Side);

    /// <summary>
    /// Loads declarations visible under <paramref name="scopes"/>, with their
    /// status history already resolved to CDNS status names. A declaration is
    /// visible when its declarant TIN (for declarant subscriptions) or consignee
    /// TIN (for consignee subscriptions) matches a scope on the first 9 chars.
    /// </summary>
    internal static async Task<List<CusDecView>> LoadVisibleCusDecsAsync(
        CdnsDbContext db, IReadOnlyCollection<Scope> scopes, CancellationToken ct)
    {
        var declKeys = scopes.Where(s => s.Side == SubscriptionType.Declarant)
            .Select(s => s.TinKey).Distinct().ToList();
        var consKeys = scopes.Where(s => s.Side == SubscriptionType.Consignee)
            .Select(s => s.TinKey).Distinct().ToList();

        if (declKeys.Count == 0 && consKeys.Count == 0)
            return [];

        // Substring(0,9) maps to SQL substring(... from 1 for 9); null-safe in SQL.
        var rows = await db.CusDecs
            .Where(c =>
                (c.DeclarantTinNumber != null && declKeys.Contains(c.DeclarantTinNumber.Substring(0, 9))) ||
                (c.ConsigneeTinNumber != null && consKeys.Contains(c.ConsigneeTinNumber.Substring(0, 9))))
            .Select(c => new
            {
                c.CusDecId,
                c.OfficeCode,
                c.RegistrationDate,
                Statuses = db.CusDecStatuses
                    .Where(s => s.CusDecId == c.CusDecId)
                    .Select(s => new { s.Status, s.StatusDateTime, s.CusDecStatusNumber })
                    .ToList(),
            })
            .ToListAsync(ct);

        var resolver = await AsycudaStatusResolver.LoadAsync(db, ct);

        return rows.Select(c => new CusDecView(
            c.RegistrationDate,
            c.Statuses
                .Select(s => new StatusEvent(
                    resolver.ToCdns(s.Status, c.OfficeCode), s.StatusDateTime, s.CusDecStatusNumber))
                .ToList()))
            .ToList();
    }

    /// <summary>Top-5 most recent, non-deleted notifications for a member.</summary>
    internal static async Task<List<DashboardNotification>> LoadRecentNotificationsAsync(
        CdnsDbContext db, long memberId, CancellationToken ct) =>
        await db.MemberNotifications
            .Where(n => n.MemberId == memberId && !n.IsDeleted)
            .OrderByDescending(n => n.NotificationDateTime)
            .Take(5)
            .Select(n => new DashboardNotification(
                n.MemberNotificationId, n.Title, n.Message, n.NotificationDateTime, n.IsSeen))
            .ToListAsync(ct);

    internal static Task<int> UnreadNotificationCountAsync(
        CdnsDbContext db, long memberId, CancellationToken ct) =>
        db.MemberNotifications.CountAsync(
            n => n.MemberId == memberId && !n.IsDeleted && !n.IsSeen, ct);
}
