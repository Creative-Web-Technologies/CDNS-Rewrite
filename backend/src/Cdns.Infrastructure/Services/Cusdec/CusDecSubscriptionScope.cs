using Cdns.Application.Feature.Cusdec;
using Cdns.Application.Feature.Subscriptions;
using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Entities.CusdecTables;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Services.Cusdec;

/// <summary>
/// Shared subscription resolution and company-side data scoping used by both
/// <see cref="CusDecService"/> and <see cref="ContainerService"/>. Encapsulates
/// the ownership check (OwnedSubscriptions) and the full gate (ISubscriptionAccessService).
/// </summary>
public sealed class CusDecSubscriptionScope
{
    private readonly CdnsDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ISubscriptionAccessService _access;

    public CusDecSubscriptionScope(
        CdnsDbContext db, ICurrentUser currentUser, ISubscriptionAccessService access)
    {
        _db = db;
        _currentUser = currentUser;
        _access = access;
    }

    public IQueryable<MemberSubscription> OwnedSubscriptions() =>
        _db.MemberSubscriptions.Where(s => s.MemberId == _currentUser.MemberId);

    /// <summary>Returns owned subscriptions that pass usability and HMAC checks.</summary>
    public Task<List<MemberSubscription>> GetUsableOwnedSubscriptionsAsync(
        DateTimeOffset today, CancellationToken ct) =>
        _access.GetUsableAsync(OwnedSubscriptions(), today, ct);

    /// <summary>
    /// Resolves one subscription: checks ownership (NotFound if not owned), then
    /// runs the full access gate. Returns Ok with the loaded subscription on success.
    /// </summary>
    public async Task<CusDecAccess<MemberSubscription>> ResolveAsync(
        long memberSubscriptionId, CancellationToken ct)
    {
        var ownerMemberId = await OwnedSubscriptions()
            .Where(s => s.MemberSubscriptionId == memberSubscriptionId)
            .Select(s => (long?)s.MemberId)
            .FirstOrDefaultAsync(ct);

        if (ownerMemberId is null)
            return CusDecAccess<MemberSubscription>.Fail(SubscriptionAccessCode.NotFound);

        var check = await _access.CheckAsync(memberSubscriptionId, ownerMemberId.Value, ct);
        return check.IsOk
            ? CusDecAccess<MemberSubscription>.Ok(check.Subscription!)
            : CusDecAccess<MemberSubscription>.Fail(check.Code);
    }

    /// <summary>
    /// Scopes the CusDec table to declarations visible to the subscription's company
    /// on the subscription's side (Declarant or Consignee). Used by CusDec endpoints only.
    /// Does NOT apply claimed/disclaimed filtering.
    /// </summary>
    public IQueryable<CusDec> CusDecsScopedTo(MemberSubscription sub) =>
        IsDeclarant(sub)
            ? _db.CusDecs.Where(c => c.DeclarantCompanyId == sub.CompanyId)
            : _db.CusDecs.Where(c => c.ConsigneeCompanyId == sub.CompanyId);

    /// <summary>
    /// Scopes the CusDec table to declarations that are visible in the Container page:
    /// company match on the subscription's side AND claimed AND not disclaimed.
    /// Completed CusDecs (claimed + not disclaimed + completedDateTime != null) are included.
    /// </summary>
    public IQueryable<CusDec> ContainerVisibleCusDecsScopedTo(MemberSubscription sub) =>
        IsDeclarant(sub)
            ? _db.CusDecs.Where(c =>
                c.DeclarantCompanyId == sub.CompanyId &&
                c.DeclarantClaimedById != null &&
                c.DeclarantDisclaimedById == null)
            : _db.CusDecs.Where(c =>
                c.ConsigneeCompanyId == sub.CompanyId &&
                c.ConsigneeClaimedById != null &&
                c.ConsigneeDisclaimedById == null);

    /// <summary>
    /// Scopes the CusDecContainer table to containers associated with CusDeclarations
    /// visible in the Container page (claimed, not disclaimed, correct company side).
    /// The CusDec navigation property is included so callers can project CusDec fields.
    /// </summary>
    public IQueryable<CusDecContainer> ContainersScopedTo(MemberSubscription sub)
    {
        var companyId = sub.CompanyId;
        return IsDeclarant(sub)
            ? _db.CusDecContainers
                .Include(c => c.CusDec)
                .Where(c =>
                    c.CusDec != null &&
                    c.CusDec.DeclarantCompanyId == companyId &&
                    c.CusDec.DeclarantClaimedById != null &&
                    c.CusDec.DeclarantDisclaimedById == null)
            : _db.CusDecContainers
                .Include(c => c.CusDec)
                .Where(c =>
                    c.CusDec != null &&
                    c.CusDec.ConsigneeCompanyId == companyId &&
                    c.CusDec.ConsigneeClaimedById != null &&
                    c.CusDec.ConsigneeDisclaimedById == null);
    }

    public static bool IsDeclarant(MemberSubscription sub) =>
        sub.SubscriptionTypeEnum == (int)SubscriptionTypeEnum.Declarant;
}
