using Cdns.Application.Feature.Subscriptions;
using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.Company;
using Cdns.Infrastructure.Persistence;

namespace Cdns.Infrastructure.Services.Subscriptions;

public sealed class MemberSubscriptionService : IMemberSubscriptionService
{
    private readonly CdnsDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IBusinessClock _clock;
    private readonly ISubscriptionAccessService _access;

    public MemberSubscriptionService(
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

    public async Task<List<ActiveSubscriptionDto>> GetActiveSubscriptionsAsync(CancellationToken ct = default)
    {
        var memberId = _currentUser.MemberId;
        var today = _clock.StartOfToday();

        // Access list: the shared usability rule (status + expiry + company active)
        // applied in SQL, then HMAC-verified in memory by the access service.
        var usable = await _access.GetUsableAsync(
            _db.MemberSubscriptions.Where(s => s.MemberId == memberId), today, ct);

        return usable.Select(s => new ActiveSubscriptionDto(
            s.MemberSubscriptionId,
            s.Company.CompanyName,
            s.Company.TinNumber,
            s.SubscriptionTypeEnum == (int)SubscriptionTypeEnum.Declarant ? "Declarant" : "Consignee",
            s.SubscriptionName,
            SubscriptionDurationFormat.Describe(s.NoOfActiveDuration, s.SubscriptionDurationTypeEnum),
            s.ExpiryDate!.Value,
            s.SubscriptionActionTypeEnum == (int)SubscriptionActionTypeEnum.OnlyForAcknowledgement
        )).ToList();
    }
}
