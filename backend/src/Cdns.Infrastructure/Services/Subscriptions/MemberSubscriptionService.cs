using Cdns.Application.Feature.Subscriptions;
using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.Company;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Services.Subscriptions;

public sealed class MemberSubscriptionService : IMemberSubscriptionService
{
    private const int AcknowledgementOnlyAction = 2;

    private readonly CdnsDbContext _db;
    private readonly ICurrentUser _currentUser;

    public MemberSubscriptionService(CdnsDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<ActiveSubscriptionDto>> GetActiveSubscriptionsAsync(CancellationToken ct = default)
    {
        var memberId = _currentUser.MemberId;
        var now = DateTimeOffset.UtcNow;

        var rows = await (
            from s in _db.MemberSubscriptions
            where s.MemberId == memberId
                  && s.MemberSubscriptionStatusEnum == (int)MemberSubscriptionStatusEnum.Activate
                  && s.ExpiryDate != null && s.ExpiryDate >= now
            join c in _db.Companies on s.CompanyId equals c.CompanyId
            where !c.IsDeleted
            select new
            {
                s.MemberSubscriptionId,
                c.CompanyName,
                c.TinNumber,
                s.SubscriptionTypeEnum,
                s.SubscriptionName,
                s.NoOfActiveDuration,
                s.SubscriptionDurationTypeEnum,
                s.ExpiryDate,
                s.SubscriptionActionTypeEnum,
            }
        ).ToListAsync(ct);

        return rows.Select(r => new ActiveSubscriptionDto(
            r.MemberSubscriptionId,
            r.CompanyName,
            r.TinNumber,
            r.SubscriptionTypeEnum == 1 ? "Declarant" : "Consignee",
            r.SubscriptionName,
            FormatDuration(r.NoOfActiveDuration, r.SubscriptionDurationTypeEnum),
            r.ExpiryDate!.Value,
            r.SubscriptionActionTypeEnum == AcknowledgementOnlyAction
        )).ToList();
    }

    private static string FormatDuration(int count, int? typeEnum)
    {
        return typeEnum switch
        {
            0 => "Lifetime",
            1 => $"{count} {(count == 1 ? "day" : "days")}",
            2 => $"{count} {(count == 1 ? "month" : "months")}",
            3 => $"{count} {(count == 1 ? "year" : "years")}",
            _ => $"{count} {(count == 1 ? "day" : "days")}",
        };
    }
}
