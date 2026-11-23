using Cdns.Application.Feature.Dashboard;
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

    public MemberDashboardService(CdnsDbContext db, ICurrentUser currentUser, IBusinessClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<MemberDashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var memberId = _currentUser.MemberId;
        var now = _clock.Now();
        var today = _clock.StartOfToday();

        // The member's subscriptions, carrying the company TIN/name needed for
        // visibility scopes and the nearest-expiry tile.
        var subs = await (
            from s in _db.MemberSubscriptions
            where s.MemberId == memberId
            join c in _db.Companies on s.CompanyId equals c.CompanyId into gj
            from c in gj.DefaultIfEmpty()
            select new
            {
                s.MemberSubscriptionStatusEnum,
                s.SubscriptionTypeEnum,
                s.StartDate,
                s.ExpiryDate,
                CompanyTin = c.TinNumber,
                CompanyName = c.CompanyName,
                s.SubscriptionName,
            }).ToListAsync(ct);

        var requestStatuses = await _db.SubscriptionRequests
            .Where(r => r.MemberId == memberId && r.IsDeleted != true)
            .Select(r => r.SubscriptionRequestStatusEnum ?? -1)
            .ToListAsync(ct);

        // Active, unexpired subscriptions define which declarations are visible.
        var activeSubs = subs
            .Where(s => s.MemberSubscriptionStatusEnum == (int)MemberSubscriptionStatusEnum.Activate
                        && s.ExpiryDate is not null && s.ExpiryDate >= today)
            .ToList();

        var scopes = activeSubs
            .Where(s => DashboardSupport.Key(s.CompanyTin) is not null)
            .Select(s => new DashboardSupport.Scope(
                DashboardSupport.Key(s.CompanyTin)!, (SubscriptionType)s.SubscriptionTypeEnum))
            .ToList();

        var visible = await DashboardSupport.LoadVisibleCusDecsAsync(_db, scopes, ct);
        var nowColombo = SriLankaTime.ToColombo(now.UtcDateTime);
        var weekly = DashboardCalculator.Weekly(visible, DateOnly.FromDateTime(nowColombo));

        var nearest = activeSubs
            .OrderBy(s => s.ExpiryDate)
            .Select(s => ToNearestExpiry(s.CompanyName, s.SubscriptionTypeEnum, s.StartDate, s.ExpiryDate!.Value, now))
            .FirstOrDefault();

        return new MemberDashboardSummary(
            Subscriptions: DashboardCalculator.SubscriptionCounts(
                requestStatuses,
                subs.Select(s => new SubscriptionView(s.MemberSubscriptionStatusEnum, s.ExpiryDate)),
                today),
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
