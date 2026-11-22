using Cdns.Application.Feature.Subscriptions;
using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cdns.Infrastructure.Services.Subscriptions;

/// <summary>
/// B01 + B02 access gate. Applies the shared <see cref="SubscriptionUsability"/>
/// rule in SQL, then verifies the HMAC key in memory. Failures are logged with
/// the id, caller and code only — never the key, data string or secret.
/// </summary>
public sealed class SubscriptionAccessService : ISubscriptionAccessService
{
    private readonly CdnsDbContext _db;
    private readonly IBusinessClock _clock;
    private readonly IMemberSubscriptionKeyService _keyService;
    private readonly ILogger<SubscriptionAccessService> _logger;

    public SubscriptionAccessService(
        CdnsDbContext db,
        IBusinessClock clock,
        IMemberSubscriptionKeyService keyService,
        ILogger<SubscriptionAccessService> logger)
    {
        _db = db;
        _clock = clock;
        _keyService = keyService;
        _logger = logger;
    }

    public async Task<SubscriptionAccessResult> CheckAsync(
        long memberSubscriptionId, long callerMemberId, CancellationToken ct = default)
    {
        var sub = await _db.MemberSubscriptions
            .Include(s => s.Company)
            .FirstOrDefaultAsync(s => s.MemberSubscriptionId == memberSubscriptionId, ct);

        if (sub is null)
            return Fail(SubscriptionAccessCode.NotFound, memberSubscriptionId, callerMemberId);

        if (sub.MemberId != callerMemberId)
            return Fail(SubscriptionAccessCode.NotOwner, memberSubscriptionId, callerMemberId);

        if (sub.MemberSubscriptionStatusEnum != (int)MemberSubscriptionStatusEnum.Activate)
            return Fail(SubscriptionAccessCode.Inactive, memberSubscriptionId, callerMemberId);

        var today = _clock.StartOfToday();
        if (!SubscriptionUsability.ExpiryIsCurrent(sub.ExpiryDate, today))
            return Fail(SubscriptionAccessCode.Expired, memberSubscriptionId, callerMemberId);

        if (sub.Company is null
            || sub.Company.CompanyStatusEnum != (int)CompanyStatusEnum.Active
            || sub.Company.IsDeleted)
            return Fail(SubscriptionAccessCode.CompanyInactive, memberSubscriptionId, callerMemberId);

        if (string.IsNullOrEmpty(sub.Key))
            return Fail(SubscriptionAccessCode.KeyMissing, memberSubscriptionId, callerMemberId);

        if (!_keyService.Verify(sub))
            return Fail(SubscriptionAccessCode.KeyMismatch, memberSubscriptionId, callerMemberId);

        return SubscriptionAccessResult.Ok(sub);
    }

    public async Task<List<MemberSubscription>> GetUsableAsync(
        IQueryable<MemberSubscription> scopedSource, DateTimeOffset today, CancellationToken ct = default)
    {
        var rows = await scopedSource
            .Where(SubscriptionUsability.UsableExpression(today))
            .Include(s => s.Company)
            .ToListAsync(ct);

        var result = new List<MemberSubscription>(rows.Count);
        foreach (var s in rows)
        {
            if (string.IsNullOrEmpty(s.Key))
            {
                LogFailure(SubscriptionAccessCode.KeyMissing, s.MemberSubscriptionId, s.MemberId);
                continue;
            }

            if (!_keyService.Verify(s))
            {
                LogFailure(SubscriptionAccessCode.KeyMismatch, s.MemberSubscriptionId, s.MemberId);
                continue;
            }

            result.Add(s);
        }

        return result;
    }

    private SubscriptionAccessResult Fail(
        SubscriptionAccessCode code, long memberSubscriptionId, long callerMemberId)
    {
        LogFailure(code, memberSubscriptionId, callerMemberId);
        return SubscriptionAccessResult.Fail(code);
    }

    private void LogFailure(SubscriptionAccessCode code, long memberSubscriptionId, long callerMemberId) =>
        _logger.LogWarning(
            "Subscription access denied. MemberSubscriptionId={MemberSubscriptionId} CallerMemberId={CallerMemberId} Code={Code}",
            memberSubscriptionId, callerMemberId, code);
}
