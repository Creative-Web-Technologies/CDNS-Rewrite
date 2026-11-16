using Cdns.Application.Feature.Subscriptions;
using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.Company;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Services.Subscriptions;

public sealed class MemberSubscriptionService : IMemberSubscriptionService
{
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
        var today = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);

        var rows = await (
            from s in _db.MemberSubscriptions
            where s.MemberId == memberId
                  && s.MemberSubscriptionStatusEnum == (int)MemberSubscriptionStatusEnum.Activate
                  && s.ExpiryDate != null && s.ExpiryDate >= today
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
            r.SubscriptionTypeEnum == (int)SubscriptionTypeEnum.Declarant ? "Declarant" : "Consignee",
            r.SubscriptionName,
            FormatDuration(r.NoOfActiveDuration, r.SubscriptionDurationTypeEnum),
            r.ExpiryDate!.Value,
            r.SubscriptionActionTypeEnum == (int)SubscriptionActionTypeEnum.OnlyForAcknowledgement
        )).ToList();
    }

    private static string FormatDuration(int count, int? typeEnum)
    {
        return typeEnum switch
        {
            (int)SubscriptionDurationTypeEnum.Days   => $"{count} {(count == 1 ? "day" : "days")}",
            (int)SubscriptionDurationTypeEnum.Months => $"{count} {(count == 1 ? "month" : "months")}",
            (int)SubscriptionDurationTypeEnum.Years  => $"{count} {(count == 1 ? "year" : "years")}",
            _ => $"{count} {(count == 1 ? "day" : "days")}",
        };
    }
}
