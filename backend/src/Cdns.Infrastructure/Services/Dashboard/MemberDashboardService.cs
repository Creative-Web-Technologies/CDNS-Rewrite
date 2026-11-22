using Cdns.Application.Feature.Dashboard;
using Cdns.Application.Feature.Subscriptions;
using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Services.Dashboard;

/// <summary>
/// Builds the member home dashboard. Every query is scoped to
/// <see cref="ICurrentUser.MemberId"/>.
/// </summary>
public sealed class MemberDashboardService : IMemberDashboardService
{
    private readonly CdnsDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBusinessClock _clock;
    private readonly ISubscriptionAccessService _access;

    public MemberDashboardService(
        CdnsDbContext db,
        ICurrentUser currentUser,
        IBusinessClock clock,
        ISubscriptionAccessService access)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _access = access;
    }

    public async Task<MemberDashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var memberId = _currentUser.MemberId;
        var now = _clock.Now();
        var today = _clock.StartOfToday();

        // All of the member's subscriptions, reduced to the fields the display
        // counts need (status + expiry). Access scoping is handled separately below.
        var subs = await _db.MemberSubscriptions
            .Where(s => s.MemberId == memberId)
            .Select(s => new SubscriptionView(s.MemberSubscriptionStatusEnum, s.ExpiryDate))
            .ToListAsync(ct);

        var requestStatuses = await _db.SubscriptionRequests
            .Where(r => r.MemberId == memberId && r.IsDeleted != true)
            .Select(r => r.SubscriptionRequestStatusEnum ?? -1)
            .ToListAsync(ct);

        // Usable subscriptions (access list: shared SQL rule + HMAC verification)
        // decide which declarations are visible and drive the nearest-expiry tile.
        var usableSubs = await _access.GetUsableAsync(
            _db.MemberSubscriptions.Where(s => s.MemberId == memberId), today, ct);

        var scopes = usableSubs
            .Where(s => DashboardSupport.Key(s.Company.TinNumber) is not null)
            .Select(s => new DashboardSupport.Scope(
                DashboardSupport.Key(s.Company.TinNumber)!, (SubscriptionType)s.SubscriptionTypeEnum))
            .ToList();

        var visible = await DashboardSupport.LoadVisibleCusDecsAsync(_db, scopes, ct);
        var nowColombo = SriLankaTime.ToColombo(now.UtcDateTime);
        var weekly = DashboardCalculator.Weekly(visible, DateOnly.FromDateTime(nowColombo));

        var nearest = usableSubs
            .OrderBy(s => s.ExpiryDate)
            .Select(s => ToNearestExpiry(s.Company.CompanyName, s.SubscriptionTypeEnum, s.StartDate, s.ExpiryDate!.Value, now))
            .FirstOrDefault();

        return new MemberDashboardSummary(
            Subscriptions: DashboardCalculator.SubscriptionCounts(
                requestStatuses, subs, today),
            CusDecs: DashboardCalculator.CountCusDecs(visible),
            MonthlyCompleted: DashboardCalculator.MonthlyCompleted(visible, nowColombo.Year),
            Weekly: weekly,
            WeeklyAverage: DashboardCalculator.WeeklyAverage(weekly),
            Clearances: DashboardCalculator.Clearances(visible),
            NearestExpiry: nearest,
            RecentNotifications: await DashboardSupport.LoadRecentNotificationsAsync(_db, memberId, ct));
    }

    public Task<int> GetUnreadNotificationCountAsync(CancellationToken ct = default) =>
        DashboardSupport.UnreadNotificationCountAsync(_db, _currentUser.MemberId, ct);

    private static NearestExpiry ToNearestExpiry(
        string? companyName, int subscriptionTypeEnum,
        DateTimeOffset start, DateTimeOffset expiry, DateTimeOffset now)
    {
        var daysTotal = Math.Max(0, (int)Math.Round((expiry - start).TotalDays));
        var daysUsed = Math.Clamp((int)Math.Round((now - start).TotalDays), 0, daysTotal);
        var daysRemaining = Math.Max(0, (int)Math.Ceiling((expiry - now).TotalDays));
        return new NearestExpiry(
            companyName ?? "—",
            ((SubscriptionType)subscriptionTypeEnum).ToString(),
            start, expiry, daysTotal, daysUsed, daysRemaining);
    }
}
