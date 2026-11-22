using Cdns.Application.Feature.Subscriptions;
using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Services.Subscriptions;

public sealed class MasterSubscriptionService : IMasterSubscriptionService
{
    private readonly CdnsDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBusinessClock _clock;

    public MasterSubscriptionService(
        CdnsDbContext db,
        ICurrentUser currentUser,
        IBusinessClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    // ── L9: List subscriptions ────────────────────────────────────────────────

    public async Task<PagedResult<MasterSubscriptionSummaryDto>> GetSubscriptionsAsync(
        int? status,
        string? subscriptionNumber,
        string? memberEmail,
        string? search,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var masterId = _currentUser.MemberId;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var today = _clock.StartOfToday();

        var query = from s in _db.MemberSubscriptions
                    where s.CompanyMasterMemberId == masterId
                    join m in _db.Members on s.MemberId equals m.MemberId
                    select new { s, m };

        if (status.HasValue)
            query = query.Where(x => x.s.MemberSubscriptionStatusEnum == status.Value);

        if (!string.IsNullOrWhiteSpace(subscriptionNumber))
        {
            var lower = subscriptionNumber.ToLower();
            query = query.Where(x =>
                x.s.MemberSubscriptionNumber != null &&
                x.s.MemberSubscriptionNumber.ToLower().Contains(lower));
        }

        if (!string.IsNullOrWhiteSpace(memberEmail))
        {
            var lower = memberEmail.ToLower();
            query = query.Where(x =>
                x.m.Email != null && x.m.Email.ToLower().Contains(lower));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(x =>
                (x.m.FirstName != null && x.m.FirstName.ToLower().Contains(lower)) ||
                (x.m.LastName  != null && x.m.LastName.ToLower().Contains(lower))  ||
                (x.m.Email     != null && x.m.Email.ToLower().Contains(lower))     ||
                (x.s.MemberSubscriptionNumber != null &&
                 x.s.MemberSubscriptionNumber.ToLower().Contains(lower))            ||
                x.s.SubscriptionName.ToLower().Contains(lower));
        }

        // Both-dates-inside rule: only applied when BOTH fromDate AND toDate are provided;
        // requires BOTH StartDate AND ExpiryDate inside the range.
        if (fromDate.HasValue && toDate.HasValue)
            query = query.Where(x =>
                x.s.StartDate >= fromDate.Value && x.s.StartDate <= toDate.Value &&
                x.s.ExpiryDate != null &&
                x.s.ExpiryDate >= fromDate.Value && x.s.ExpiryDate <= toDate.Value);

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderBy(x => x.s.ExpiryDate)
            .ThenBy(x => x.s.MemberSubscriptionStatusEnum)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.s.MemberSubscriptionId,
                x.s.MemberSubscriptionNumber,
                MemberName = (x.m.FirstName ?? "") + " " + (x.m.LastName ?? ""),
                MemberEmail = x.m.Email ?? "—",
                x.s.SubscriptionTypeEnum,
                x.s.SubscriptionName,
                x.s.StartDate,
                x.s.ExpiryDate,
                x.s.MemberSubscriptionStatusEnum,
            })
            .ToListAsync(ct);

        var dtos = rows.Select(x => new MasterSubscriptionSummaryDto(
            x.MemberSubscriptionId,
            x.MemberSubscriptionNumber,
            x.MemberName.Trim(),
            x.MemberEmail,
            x.SubscriptionTypeEnum == (int)SubscriptionTypeEnum.Declarant ? "Declarant" : "Consignee",
            x.SubscriptionName,
            x.StartDate,
            x.ExpiryDate,
            SubscriptionStatusLabel(x.MemberSubscriptionStatusEnum),
            SubscriptionUsability.IsActiveForDisplay(x.MemberSubscriptionStatusEnum, x.ExpiryDate, today)
        )).ToList();

        return new PagedResult<MasterSubscriptionSummaryDto>(dtos, total, page, pageSize);
    }

    // ── L10: View subscription detail ─────────────────────────────────────────

    public async Task<MasterSubscriptionDetailDto?> GetSubscriptionAsync(
        long memberSubscriptionId, CancellationToken ct = default)
    {
        var masterId = _currentUser.MemberId;
        var today = _clock.StartOfToday();

        var row = await (
            from s in _db.MemberSubscriptions
            where s.MemberSubscriptionId == memberSubscriptionId
                  && s.CompanyMasterMemberId == masterId
            join m in _db.Members on s.MemberId equals m.MemberId
            join c in _db.Companies on s.CompanyId equals c.CompanyId into cj
            from c in cj.DefaultIfEmpty()
            join r in _db.SubscriptionRequests on s.SubscriptionRequestId equals r.SubscriptionRequestId into rj
            from r in rj.DefaultIfEmpty()
            select new { s, m, c, r }
        ).FirstOrDefaultAsync(ct);

        if (row is null) return null;

        var history = await _db.MemberSubscriptionStatusHistories
            .Where(h => h.MemberSubscriptionId == memberSubscriptionId)
            .OrderBy(h => h.CreatedDateTime)
            .Select(h => new SubscriptionStatusHistoryDto(
                h.MemberSubscriptionStatusHistoryId,
                MemberSubscriptionStatusLabel(h.MemberSubscriptionStatusEnum),
                h.Comment,
                h.CreatedDateTime
            ))
            .ToListAsync(ct);

        return new MasterSubscriptionDetailDto(
            row.s.MemberSubscriptionId,
            row.s.MemberSubscriptionNumber,
            ((row.m.FirstName ?? "") + " " + (row.m.LastName ?? "")).Trim(),
            row.m.Email ?? "—",
            row.c != null ? row.c.CompanyName : "—",
            row.c?.TinNumber,
            row.s.SubscriptionTypeEnum == (int)SubscriptionTypeEnum.Declarant ? "Declarant" : "Consignee",
            row.s.SubscriptionName,
            row.s.SubscriptionAmount,
            row.s.StartDate,
            row.s.ExpiryDate,
            SubscriptionStatusLabel(row.s.MemberSubscriptionStatusEnum),
            SubscriptionUsability.IsActiveForDisplay(row.s.MemberSubscriptionStatusEnum, row.s.ExpiryDate, today),
            row.r?.SubscriptionRequestNumber,
            history
        );
    }

    // ── L11: Deactivate ───────────────────────────────────────────────────────

    public async Task<DeactivateResult> DeactivateAsync(
        long memberSubscriptionId, string comment, CancellationToken ct = default)
    {
        var masterId = _currentUser.MemberId;

        var subscription = await _db.MemberSubscriptions
            .FirstOrDefaultAsync(s =>
                s.MemberSubscriptionId == memberSubscriptionId
                && s.CompanyMasterMemberId == masterId, ct);

        if (subscription is null) return new DeactivateResult.NotFound();
        if (subscription.MemberSubscriptionStatusEnum != (int)MemberSubscriptionStatusEnum.Activate)
            return new DeactivateResult.NotActive();

        subscription.MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Deactivate;

        _db.MemberSubscriptionStatusHistories.Add(new MemberSubscriptionStatusHistory
        {
            MemberSubscriptionId = memberSubscriptionId,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Deactivate,
            Comment = comment,
            MasterMemberId = masterId,
            CreatedDateTime = _clock.Now(),
        });

        await _db.SaveChangesAsync(ct);
        return new DeactivateResult.Success();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    // "Active" here is a display badge (status + expiry only, no company/HMAC gate)
    // and is defined once in SubscriptionUsability.IsActiveForDisplay.

    private static string SubscriptionStatusLabel(int status) => status switch
    {
        (int)MemberSubscriptionStatusEnum.PaymentPending => "PaymentPending",
        (int)MemberSubscriptionStatusEnum.Activate       => "Active",
        (int)MemberSubscriptionStatusEnum.Deactivate     => "Deactivated",
        (int)MemberSubscriptionStatusEnum.Suspended      => "Suspended",
        (int)MemberSubscriptionStatusEnum.Expired        => "Expired",
        _ => "Unknown",
    };

    private static string MemberSubscriptionStatusLabel(int status) => status switch
    {
        (int)MemberSubscriptionStatusEnum.PaymentPending => "PaymentPending",
        (int)MemberSubscriptionStatusEnum.Activate       => "Active",
        (int)MemberSubscriptionStatusEnum.Deactivate     => "Deactivated",
        (int)MemberSubscriptionStatusEnum.Suspended      => "Suspended",
        (int)MemberSubscriptionStatusEnum.Expired        => "Expired",
        _ => "Unknown",
    };
}
