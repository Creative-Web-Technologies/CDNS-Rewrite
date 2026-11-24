using Cdns.Application.Feature.Dashboard;
using Cdns.Application.Feature.Subscriptions;
using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Services.Dashboard;

/// <summary>
/// Builds the company-master home dashboard, scoped to every company the
/// authenticated master is mapped to.
/// </summary>
public sealed class CompanyDashboardService : ICompanyDashboardService
{
    private readonly CdnsDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBusinessClock _clock;
    private readonly ISubscriptionAccessService _access;

    public CompanyDashboardService(
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

    public async Task<CompanyDashboardSummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var masterId = _currentUser.MemberId;
        var now = _clock.Now();
        var today = _clock.StartOfToday();

        // Companies this master administers.
        var companies = await _db.CompanyMemberMappings
            .Where(m => m.MemberId == masterId && !m.IsDeleted)
            .Join(_db.Companies, m => m.CompanyId, c => c.CompanyId,
                (m, c) => new { c.CompanyId, c.TinNumber })
            .ToListAsync(ct);

        var companyIds = companies.Select(c => c.CompanyId).Distinct().ToList();
        var companyKeys = companies
            .Select(c => DashboardSupport.Key(c.TinNumber))
            .Where(k => k is not null).Select(k => k!).Distinct().ToList();

        // All subscriptions under those companies, reduced to the fields the
        // display counts need (status + expiry).
        var subs = await _db.MemberSubscriptions
            .Where(s => companyIds.Contains(s.CompanyId))
            .Select(s => new SubscriptionView(s.MemberSubscriptionStatusEnum, s.ExpiryDate))
            .ToListAsync(ct);

        // Pending excludes the master's own requests.
        var requestStatuses = await _db.SubscriptionRequests
            .Where(r => r.CompanyId != null && companyIds.Contains(r.CompanyId.Value)
                        && r.MemberId != masterId && r.IsDeleted != true)
            .Select(r => r.SubscriptionRequestStatusEnum ?? -1)
            .ToListAsync(ct);

        // Usable subscriptions (access list: shared SQL rule + HMAC verification)
        // decide which declarations are visible under these companies.
        var usableSubs = await _access.GetUsableAsync(
            _db.MemberSubscriptions.Where(s => companyIds.Contains(s.CompanyId)), today, ct);

        var scopes = usableSubs
            .Where(s => DashboardSupport.Key(s.Company.TinNumber) is not null)
            .Select(s => new DashboardSupport.Scope(
                DashboardSupport.Key(s.Company.TinNumber)!, (SubscriptionType)s.SubscriptionTypeEnum))
            .ToList();

        var visible = await DashboardSupport.LoadVisibleCusDecsAsync(_db, scopes, ct);
        var nowColombo = SriLankaTime.ToColombo(now.UtcDateTime);
        var weekly = DashboardCalculator.Weekly(visible, DateOnly.FromDateTime(nowColombo));

        return new CompanyDashboardSummary(
            Subscriptions: DashboardCalculator.SubscriptionCounts(
                requestStatuses, subs, today),
            CusDecs: DashboardCalculator.CountCusDecs(visible),
            MonthlyCompleted: DashboardCalculator.MonthlyCompleted(visible, nowColombo.Year),
            Weekly: weekly,
            WeeklyAverage: DashboardCalculator.WeeklyAverage(weekly),
            Clearances: DashboardCalculator.Clearances(visible),
            RecentNotifications: await DashboardSupport.LoadRecentNotificationsAsync(_db, masterId, ct),
            PaymentSummary: await LoadPaymentSummaryAsync(companyKeys, ct));
    }

    public Task<int> GetUnreadNotificationCountAsync(CancellationToken ct = default) =>
        DashboardSupport.UnreadNotificationCountAsync(_db, _currentUser.MemberId, ct);

    private async Task<PaymentSummary> LoadPaymentSummaryAsync(
        List<string> companyKeys, CancellationToken ct)
    {
        if (companyKeys.Count == 0)
            return new PaymentSummary(0, 0, 0);

        var totals = await _db.CusDecPaymentSummaries
            .Where(p =>
                (p.DeclarantTinNumber != null && companyKeys.Contains(p.DeclarantTinNumber.Substring(0, 9))) ||
                (p.ConsigneeTinNumber != null && companyKeys.Contains(p.ConsigneeTinNumber.Substring(0, 9))))
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Cif = g.Sum(x => x.TotalCif ?? 0m),
                Tax = g.Sum(x => x.TotalTaxPaid ?? 0m),
                Inspection = g.Sum(x => x.InspectionRecoveries ?? 0m),
            })
            .FirstOrDefaultAsync(ct);

        return totals is null
            ? new PaymentSummary(0, 0, 0)
            : new PaymentSummary(totals.Cif, totals.Tax, totals.Inspection);
    }
}
