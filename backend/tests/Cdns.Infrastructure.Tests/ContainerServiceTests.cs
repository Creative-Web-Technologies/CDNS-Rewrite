using Cdns.Application.Feature.Container;
using Cdns.Application.Feature.Subscriptions;
using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Entities.CusdecTables;
using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Persistence;
using Cdns.Infrastructure.Services.Cusdec;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Tests;

public class ContainerServiceTests
{
    private const int CompanyId  = 1;
    private const int Company2Id = 2;
    private const long MemberOwner  = 1;
    private const long MemberOther  = 2;   // same company, different member
    private const long MasterId     = 10;
    private const long SubId        = 100;

    private static readonly DateTimeOffset Now        = new(2026, 11, 23, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateTime       StatusBase = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    // ── Harness ───────────────────────────────────────────────────────────────

    private sealed class Harness
    {
        public required CdnsDbContext Db      { get; init; }
        public required FakeBusinessClock Clock { get; init; }
        public required IMemberSubscriptionKeyService Keys { get; init; }
        public required ContainerService Service { get; init; }
    }

    private static Harness NewHarness(
        string name,
        ICurrentUser user,
        int subType    = (int)SubscriptionTypeEnum.Declarant,
        int actionType = (int)SubscriptionActionTypeEnum.General,
        int companyStatus = (int)CompanyStatusEnum.Active,
        long memberId  = MemberOwner,
        long masterId  = MasterId,
        DateTimeOffset? expiry = null,
        int companyId = CompanyId)
    {
        var db    = TestHelpers.CreateDb(name);
        var clock = new FakeBusinessClock(Now);
        var keys  = FakeKeyServiceFactory.Create();

        db.Companies.Add(new Company
        {
            CompanyId = companyId, CompanyName = "Acme", TinNumber = "123456789",
            IsDeleted = false, CompanyStatusEnum = companyStatus,
        });
        db.Members.Add(new Member { MemberId = memberId, FirstName = "Mandy", LastName = "Member",
            Email = "m@t", MemberType = MemberType.Member });
        if (masterId != memberId)
            db.Members.Add(new Member { MemberId = masterId, FirstName = "Marty", LastName = "Master",
                Email = "x@t", MemberType = MemberType.CompanyMasterMember });

        var sub = new MemberSubscription
        {
            MemberSubscriptionId = SubId, MemberId = memberId, CompanyId = companyId,
            CompanyMasterMemberId = masterId,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = expiry ?? clock.StartOfToday().AddDays(30), PaidAmount = 0m,
            SubscriptionTypeEnum = subType, SubscriptionActionTypeEnum = actionType,
            SubscriptionName = "Gold", NoOfActiveDuration = 1,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionRequestId = 0, SubscriptionId = 0,
        };
        db.MemberSubscriptions.Add(sub);
        db.SaveChanges();
        sub.Key = keys.Generate(sub);
        db.SaveChanges();

        return new Harness
        {
            Db = db, Clock = clock, Keys = keys,
            Service = new ContainerService(db, clock, TestHelpers.SubscriptionScope(db, user, keys, clock)),
        };
    }

    // Adds a CusDec and one container linked to it; returns (cusDecId, containerId).
    private static (long CusDecId, long ContainerId) AddContainer(
        CdnsDbContext db,
        long cusDecId,
        long containerId,
        int? declarantCompanyId = CompanyId,
        int? consigneeCompanyId = null,
        string? containerNumber = null,
        DateTime? arrivalDate   = null,
        DateTime? cusDecDate    = null,
        string? location        = null,
        string? currentStatus   = null,
        int containerPassNumber = 0,
        bool declarantClaimed   = false,
        bool consigneeClaimed   = false,
        bool declarantDisclaimed = false,
        bool consigneeDisclaimed = false,
        bool declarantCompleted = false,
        bool consigneeCompleted = false)
    {
        db.CusDecs.Add(new CusDec
        {
            CusDecId = cusDecId,
            DeclarantCompanyId = declarantCompanyId,
            ConsigneeCompanyId = consigneeCompanyId,
            CusDecNumber = $"CD-{cusDecId}",
            CusDecDate = cusDecDate,
            GlobalIdentity = Guid.NewGuid(),
            CusDecExternalSystemNumber = cusDecId,
            DeclarantClaimedById    = declarantClaimed   ? (long?)1 : null,
            ConsigneeClaimedById    = consigneeClaimed   ? (long?)1 : null,
            DeclarantDisclaimedById = declarantDisclaimed ? (long?)1 : null,
            ConsigneeDisclaimedById = consigneeDisclaimed ? (long?)1 : null,
            DeclarantCompletedDateTime = declarantCompleted ? (DateTime?)StatusBase : null,
            ConsigneeCompletedDateTime = consigneeCompleted ? (DateTime?)StatusBase : null,
        });
        db.CusDecContainers.Add(new CusDecContainer
        {
            CusDecContainerId = containerId,
            CusDecId = cusDecId,
            Container = containerNumber ?? $"CONT-{containerId}",
            ArrivalDate = arrivalDate,
            Location = location,
            CurrentStatus = currentStatus,
            ContainerPassNumber = containerPassNumber,
            CusDecExternalSystemNumber = cusDecId,
            ContainerKey = (int)containerId,
        });
        return (cusDecId, containerId);
    }

    /// <summary>
    /// Adds a container status row and optionally a mapping row.
    /// <paramref name="statusKey"/> and <paramref name="descriptionKey"/> are the
    /// expected lower(btrim(value)) keys; always pass as literal strings — do not
    /// derive them with C# normalisation helpers.
    /// </summary>
    private static void AddContainerStatus(
        CdnsDbContext db,
        long containerId,
        string status,
        string description,
        DateTime at,
        bool isShow        = true,
        string? cdnsStatus = null,
        string? cdnsDesc   = null,
        string? statusKey  = null,
        string? descriptionKey = null)
    {
        if (cdnsStatus is not null || !isShow)
        {
            var mapping = new ContainerStatusMapping
            {
                ContainerStatusMappingId = db.ContainerStatusMappings.Local.Count + 1,
                AsycudaStatus = status,
                AsycudaDescription = description,
                CdnsStatus = cdnsStatus ?? status,
                CdnsDescription = cdnsDesc,
                IsShow = isShow,
            };
            db.ContainerStatusMappings.Add(mapping);
            // AsycudaStatusKey and AsycudaDescriptionKey are computed columns in Postgres.
            // InMemory does not evaluate them; callers must pass literal keys — no C# normalisation.
            db.Entry(mapping).Property("AsycudaStatusKey").CurrentValue =
                statusKey ?? throw new ArgumentNullException(nameof(statusKey),
                    "Pass a literal statusKey — do not rely on C# normalisation.");
            db.Entry(mapping).Property("AsycudaDescriptionKey").CurrentValue =
                descriptionKey ?? throw new ArgumentNullException(nameof(descriptionKey),
                    "Pass a literal descriptionKey — do not rely on C# normalisation.");
        }

        db.CusDecContainerStatuses.Add(new CusDecContainerStatus
        {
            CusDecContainerStatusId = db.CusDecContainerStatuses.Local.Count + 1,
            CusDecContainerId = containerId,
            Status = status,
            Description = description,
            StatusDateTime = at,
            ExternalSystemNumber = containerId,
            ContainerStatusNumber = db.CusDecContainerStatuses.Local.Count + 1,
        });
    }

    private static ContainerListQuery ListQuery(
        long? cusDecId = null, string? search = null,
        string? location = null, string? status = null,
        int page = 1, int pageSize = 20)
        => new(cusDecId, search, location, status, page, pageSize);

    // ── Gate tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Gate_NotOwned_ReturnsNotFound()
    {
        var h = NewHarness(nameof(Gate_NotOwned_ReturnsNotFound), new FakeCurrentUser(99));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1);
        await h.Db.SaveChangesAsync();

        var result = await h.Service.GetContainersAsync(SubId, ListQuery());
        Assert.False(result.IsOk);
        Assert.Equal(SubscriptionAccessCode.NotFound, result.Code);
    }

    [Fact]
    public async Task Gate_AcknowledgementOnly_ReturnsAcknowledgementOnly_OnAllThreeEndpoints()
    {
        var h = NewHarness(
            nameof(Gate_AcknowledgementOnly_ReturnsAcknowledgementOnly_OnAllThreeEndpoints),
            new FakeCurrentUser(MemberOwner),
            actionType: (int)SubscriptionActionTypeEnum.OnlyForAcknowledgement);
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1);
        await h.Db.SaveChangesAsync();

        var r1 = await h.Service.GetContainersAsync(SubId, ListQuery());
        var r2 = await h.Service.GetContainerDetailAsync(SubId, 1);
        var r3 = await h.Service.GetContainerHistoryAsync(SubId, 1);

        Assert.Equal(SubscriptionAccessCode.AcknowledgementOnly, r1.Code);
        Assert.Equal(SubscriptionAccessCode.AcknowledgementOnly, r2.Code);
        Assert.Equal(SubscriptionAccessCode.AcknowledgementOnly, r3.Code);
    }

    [Fact]
    public async Task Gate_InactiveSubscription_ReturnsInactive()
    {
        var h = NewHarness(nameof(Gate_InactiveSubscription_ReturnsInactive), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        var sub = await h.Db.MemberSubscriptions.FirstAsync();
        sub.MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Deactivate;
        await h.Db.SaveChangesAsync();

        var result = await h.Service.GetContainersAsync(SubId, ListQuery());
        Assert.False(result.IsOk);
        Assert.Equal(SubscriptionAccessCode.Inactive, result.Code);
    }

    [Fact]
    public async Task Gate_ExpiredSubscription_ReturnsExpired()
    {
        var h = NewHarness(nameof(Gate_ExpiredSubscription_ReturnsExpired),
            new FakeCurrentUser(MemberOwner),
            expiry: Now.AddDays(-1));
        await using var _ = h.Db;

        var result = await h.Service.GetContainersAsync(SubId, ListQuery());
        Assert.False(result.IsOk);
        Assert.Equal(SubscriptionAccessCode.Expired, result.Code);
    }

    [Fact]
    public async Task Gate_CompanySuspended_ReturnsCompanyInactive()
    {
        var h = NewHarness(nameof(Gate_CompanySuspended_ReturnsCompanyInactive),
            new FakeCurrentUser(MemberOwner),
            companyStatus: (int)CompanyStatusEnum.Suspended);
        await using var _ = h.Db;

        var result = await h.Service.GetContainersAsync(SubId, ListQuery());
        Assert.False(result.IsOk);
        Assert.Equal(SubscriptionAccessCode.CompanyInactive, result.Code);
    }

    // ── Data scope: each side sees only its own containers ────────────────────

    [Fact]
    public async Task Declarant_SeesDeclaredContainers_NotConsigneeContainers()
    {
        var h = NewHarness(nameof(Declarant_SeesDeclaredContainers_NotConsigneeContainers),
            new FakeCurrentUser(MemberOwner),
            subType: (int)SubscriptionTypeEnum.Declarant);
        await using var _ = h.Db;

        // Both claimed on their respective side so the claim rule is satisfied.
        AddContainer(h.Db, 1, 1, declarantCompanyId: CompanyId, consigneeCompanyId: null, declarantClaimed: true);
        AddContainer(h.Db, 2, 2, declarantCompanyId: null,      consigneeCompanyId: CompanyId, consigneeClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1L, Assert.Single(result.Items).CusDecContainerId);
    }

    [Fact]
    public async Task Consignee_SeesConsigneeContainers_NotDeclarantContainers()
    {
        var h = NewHarness(nameof(Consignee_SeesConsigneeContainers_NotDeclarantContainers),
            new FakeCurrentUser(MemberOwner),
            subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;

        AddContainer(h.Db, 1, 1, declarantCompanyId: CompanyId, consigneeCompanyId: null, declarantClaimed: true);
        AddContainer(h.Db, 2, 2, declarantCompanyId: null,      consigneeCompanyId: CompanyId, consigneeClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(2L, Assert.Single(result.Items).CusDecContainerId);
    }

    [Fact]
    public async Task DifferentCompany_IsExcluded()
    {
        var h = NewHarness(nameof(DifferentCompany_IsExcluded), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        // Company 999 — not Company 1
        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 99, DeclarantCompanyId = 999, ConsigneeCompanyId = 999,
            CusDecNumber = "CD-99", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 99,
        });
        h.Db.CusDecContainers.Add(new CusDecContainer
        {
            CusDecContainerId = 99, CusDecId = 99,
            Container = "OTHER", CusDecExternalSystemNumber = 99, ContainerKey = 99,
        });
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task DifferentCompany_ClaimedButWrongCompany_IsExcluded()
    {
        var h = NewHarness(nameof(DifferentCompany_ClaimedButWrongCompany_IsExcluded),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 50, DeclarantCompanyId = 999,
            CusDecNumber = "CD-50", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 50,
            DeclarantClaimedById = 5,
        });
        h.Db.CusDecContainers.Add(new CusDecContainer
        {
            CusDecContainerId = 50, CusDecId = 50,
            Container = "X", CusDecExternalSystemNumber = 50, ContainerKey = 50,
        });
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(0, result.TotalCount);
    }

    // ── Access rule: claimed + not disclaimed required ────────────────────────

    [Fact]
    public async Task Declarant_Unclaimed_ExcludedFromList()
    {
        var h = NewHarness(nameof(Declarant_Unclaimed_ExcludedFromList), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: CompanyId); // no declarantClaimed
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Declarant_Unclaimed_DetailReturns404()
    {
        var h = NewHarness(nameof(Declarant_Unclaimed_DetailReturns404), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: CompanyId);
        await h.Db.SaveChangesAsync();

        var r2 = await h.Service.GetContainerDetailAsync(SubId, 1);
        var r3 = await h.Service.GetContainerHistoryAsync(SubId, 1);
        Assert.Equal(SubscriptionAccessCode.NotFound, r2.Code);
        Assert.Equal(SubscriptionAccessCode.NotFound, r3.Code);
    }

    [Fact]
    public async Task Declarant_Unclaimed_CusDecIdFilterReturns404()
    {
        var h = NewHarness(nameof(Declarant_Unclaimed_CusDecIdFilterReturns404), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 1, DeclarantCompanyId = CompanyId,
            CusDecNumber = "CD-1", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 1,
        });
        await h.Db.SaveChangesAsync();

        var result = await h.Service.GetContainersAsync(SubId, ListQuery(cusDecId: 1));
        Assert.Equal(SubscriptionAccessCode.NotFound, result.Code);
    }

    [Fact]
    public async Task Declarant_Disclaimed_ExcludedFromList()
    {
        var h = NewHarness(nameof(Declarant_Disclaimed_ExcludedFromList), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: CompanyId, declarantClaimed: true, declarantDisclaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Declarant_Disclaimed_DetailAndStatusReturn404()
    {
        var h = NewHarness(nameof(Declarant_Disclaimed_DetailAndStatusReturn404), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: CompanyId, declarantClaimed: true, declarantDisclaimed: true);
        await h.Db.SaveChangesAsync();

        Assert.Equal(SubscriptionAccessCode.NotFound, (await h.Service.GetContainerDetailAsync(SubId, 1)).Code);
        Assert.Equal(SubscriptionAccessCode.NotFound, (await h.Service.GetContainerHistoryAsync(SubId, 1)).Code);
    }

    [Fact]
    public async Task Consignee_Unclaimed_ExcludedFromList()
    {
        var h = NewHarness(nameof(Consignee_Unclaimed_ExcludedFromList),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: null, consigneeCompanyId: CompanyId);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Consignee_Disclaimed_ExcludedFromList()
    {
        var h = NewHarness(nameof(Consignee_Disclaimed_ExcludedFromList),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: null, consigneeCompanyId: CompanyId,
            consigneeClaimed: true, consigneeDisclaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task OtherSide_Claim_DoesNotGrantAccess()
    {
        // Declarant subscription: consignee-claimed does not make the container visible
        var h = NewHarness(nameof(OtherSide_Claim_DoesNotGrantAccess), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: CompanyId, consigneeCompanyId: Company2Id,
            declarantClaimed: false, consigneeClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task OtherSide_Disclaim_DoesNotHide()
    {
        // Declarant subscription: consignee-disclaimed does not hide a declarant-claimed container
        var h = NewHarness(nameof(OtherSide_Disclaim_DoesNotHide), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: CompanyId, consigneeCompanyId: Company2Id,
            declarantClaimed: true, consigneeDisclaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task AnotherMember_SameCompany_ClaimedIsIncluded()
    {
        // MemberOther (member id 2) in the same company claimed the CusDec.
        // The subscription owner (MemberOwner) should still see the container.
        var h = NewHarness(nameof(AnotherMember_SameCompany_ClaimedIsIncluded), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        h.Db.Members.Add(new Member
        {
            MemberId = MemberOther, FirstName = "Other", LastName = "Member",
            Email = "o@t", MemberType = MemberType.Member,
        });
        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 1, DeclarantCompanyId = CompanyId,
            CusDecNumber = "CD-1", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 1,
            DeclarantClaimedById = MemberOther, // different member, same company
        });
        h.Db.CusDecContainers.Add(new CusDecContainer
        {
            CusDecContainerId = 1, CusDecId = 1, Container = "CONT-1",
            CusDecExternalSystemNumber = 1, ContainerKey = 1,
        });
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Completed_CusDec_ClaimedAndNotDisclaimed_ContainerVisible()
    {
        var h = NewHarness(nameof(Completed_CusDec_ClaimedAndNotDisclaimed_ContainerVisible),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: CompanyId, declarantClaimed: true, declarantCompleted: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(1, result.TotalCount);
    }

    // ── CusDecId filter ───────────────────────────────────────────────────────

    [Fact]
    public async Task CusDecIdFilter_InvisibleCusDec_Returns404()
    {
        var h = NewHarness(nameof(CusDecIdFilter_InvisibleCusDec_Returns404), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        // CusDec 99 belongs to company 999, not in scope
        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 99, DeclarantCompanyId = 999,
            CusDecNumber = "CD-99", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 99,
            DeclarantClaimedById = 1,
        });
        await h.Db.SaveChangesAsync();

        var result = await h.Service.GetContainersAsync(SubId, ListQuery(cusDecId: 99));
        Assert.False(result.IsOk);
        Assert.Equal(SubscriptionAccessCode.NotFound, result.Code);
    }

    [Fact]
    public async Task CusDecIdFilter_UnclaimedCusDec_Returns404()
    {
        // Unclaimed CusDec in the right company is not visible to container filter.
        var h = NewHarness(nameof(CusDecIdFilter_UnclaimedCusDec_Returns404), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 5, DeclarantCompanyId = CompanyId,
            CusDecNumber = "CD-5", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 5,
        });
        await h.Db.SaveChangesAsync();

        var result = await h.Service.GetContainersAsync(SubId, ListQuery(cusDecId: 5));
        Assert.Equal(SubscriptionAccessCode.NotFound, result.Code);
    }

    [Fact]
    public async Task CusDecIdFilter_NonexistentId_SameResultAsOutOfScope()
    {
        var h = NewHarness(nameof(CusDecIdFilter_NonexistentId_SameResultAsOutOfScope), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        var rNone  = await h.Service.GetContainersAsync(SubId, ListQuery(cusDecId: 9999));
        var rOther = await h.Service.GetContainersAsync(SubId, ListQuery(cusDecId: 9998));
        Assert.Equal(SubscriptionAccessCode.NotFound, rNone.Code);
        Assert.Equal(SubscriptionAccessCode.NotFound, rOther.Code);
    }

    [Fact]
    public async Task CusDecIdFilter_VisibleCusDec_NoContainers_Returns200Empty()
    {
        var h = NewHarness(nameof(CusDecIdFilter_VisibleCusDec_NoContainers_Returns200Empty),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 5, DeclarantCompanyId = CompanyId, DeclarantClaimedById = 1,
            CusDecNumber = "CD-5", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 5,
        });
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery(cusDecId: 5))).Value!;
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
        Assert.NotNull(result.Filter);
        Assert.Equal(5L, result.Filter!.CusDecId);
        Assert.Equal("CD-5", result.Filter.CusDecNumber);
    }

    [Fact]
    public async Task CusDecIdFilter_VisibleCusDec_WithContainers_EchoesCusDecId()
    {
        var h = NewHarness(nameof(CusDecIdFilter_VisibleCusDec_WithContainers_EchoesCusDecId),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        AddContainer(h.Db, 1, 1, declarantClaimed: true);
        AddContainer(h.Db, 2, 2, declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery(cusDecId: 1))).Value!;
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1L, Assert.Single(result.Items).CusDecContainerId);
        Assert.NotNull(result.Filter);
        Assert.Equal(1L, result.Filter!.CusDecId);
        Assert.Equal("CD-1", result.Filter.CusDecNumber);
    }

    [Fact]
    public async Task CusDecIdFilter_NoCusDecId_FilterIsNull()
    {
        var h = NewHarness(nameof(CusDecIdFilter_NoCusDecId_FilterIsNull), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Null(result.Filter);
    }

    // ── Sort: ArrivalDate DESC nulls-last ─────────────────────────────────────

    [Fact]
    public async Task Sort_Default_ArrivalDateDescNullsLast()
    {
        var h = NewHarness(nameof(Sort_Default_ArrivalDateDescNullsLast), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        AddContainer(h.Db, 1, 1, arrivalDate: null, declarantClaimed: true);
        AddContainer(h.Db, 2, 2, arrivalDate: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), declarantClaimed: true);
        AddContainer(h.Db, 3, 3, arrivalDate: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        var ids = result.Items.Select(i => i.CusDecContainerId).ToArray();

        Assert.Equal(new long[] { 3, 2, 1 }, ids);
    }

    // ── Paging ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_DefaultPageSizeIs10()
    {
        var h = NewHarness(nameof(List_DefaultPageSizeIs10), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        for (var i = 1; i <= 12; i++)
            AddContainer(h.Db, i, i, declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        // pageSize=0 triggers the default
        var result = (await h.Service.GetContainersAsync(SubId, ListQuery(pageSize: 0))).Value!;
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(12, result.TotalCount);
    }

    [Fact]
    public async Task List_Pagination_ReturnsCorrectPage()
    {
        var h = NewHarness(nameof(List_Pagination_ReturnsCorrectPage), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        for (var i = 1; i <= 5; i++)
            AddContainer(h.Db, i, i, arrivalDate: StatusBase.AddDays(i), declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var page1 = (await h.Service.GetContainersAsync(SubId, ListQuery(page: 1, pageSize: 3))).Value!;
        var page2 = (await h.Service.GetContainersAsync(SubId, ListQuery(page: 2, pageSize: 3))).Value!;

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(3, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
    }

    [Fact]
    public async Task List_PageSizeClampedToMax50()
    {
        var h = NewHarness(nameof(List_PageSizeClampedToMax50), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        for (var i = 1; i <= 60; i++)
            AddContainer(h.Db, i, i, declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery(pageSize: 999))).Value!;
        Assert.Equal(50, result.Items.Count);
    }

    [Fact]
    public async Task List_PageOverflow_IntMaxValue_Returns200Empty()
    {
        // (int.MaxValue - 1) * MaxPageSize overflows int; must return 200 empty, not 500.
        var h = NewHarness(nameof(List_PageOverflow_IntMaxValue_Returns200Empty), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery(page: int.MaxValue, pageSize: 50))).Value!;
        Assert.Empty(result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task List_PageOverflow_FirstOverflowPage_Returns200Empty()
    {
        // First page where (page-1) * MaxPageSize overflows int: (42949674-1)*50 = 2147483650 > int.MaxValue.
        var h = NewHarness(nameof(List_PageOverflow_FirstOverflowPage_Returns200Empty), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery(page: 42949674, pageSize: 50))).Value!;
        Assert.Empty(result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    // ── ContainerPassNumber ───────────────────────────────────────────────────

    [Fact]
    public async Task List_ContainerPassNumber_ZeroIsNull()
    {
        var h = NewHarness(nameof(List_ContainerPassNumber_ZeroIsNull), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, containerPassNumber: 0, declarantClaimed: true);
        AddContainer(h.Db, 2, 2, containerPassNumber: 7, declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var items = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!.Items;
        var item1 = items.Single(i => i.CusDecContainerId == 1);
        var item2 = items.Single(i => i.CusDecContainerId == 2);
        Assert.Null(item1.ContainerPassNumber);
        Assert.Equal(7, item2.ContainerPassNumber);
    }

    [Fact]
    public async Task Detail_ContainerPassNumber_NegativeIsNull()
    {
        var h = NewHarness(nameof(Detail_ContainerPassNumber_NegativeIsNull), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, containerPassNumber: -1, declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainerDetailAsync(SubId, 1)).Value!;
        Assert.Null(result.ContainerPassNumber);
    }

    // ── Search ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_Search_FiltersOnContainerNumber()
    {
        var h = NewHarness(nameof(List_Search_FiltersOnContainerNumber), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, containerNumber: "TCKU1234567", declarantClaimed: true);
        AddContainer(h.Db, 2, 2, containerNumber: "MSCU9876543", declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery(search: "TCKU"))).Value!;
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1L, Assert.Single(result.Items).CusDecContainerId);
    }

    [Fact]
    public async Task List_Search_FiltersOnLocation()
    {
        var h = NewHarness(nameof(List_Search_FiltersOnLocation), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, location: "YARD-A", declarantClaimed: true);
        AddContainer(h.Db, 2, 2, location: "SHED-B", declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery(search: "yard"))).Value!;
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1L, Assert.Single(result.Items).CusDecContainerId);
    }

    [Fact]
    public async Task List_Search_FiltersOnCurrentStatus()
    {
        var h = NewHarness(nameof(List_Search_FiltersOnCurrentStatus), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, currentStatus: "CLEARED", declarantClaimed: true);
        AddContainer(h.Db, 2, 2, currentStatus: "PENDING", declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery(search: "cleared"))).Value!;
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1L, Assert.Single(result.Items).CusDecContainerId);
    }

    [Fact]
    public async Task List_Search_FiltersOnCusDecNumber()
    {
        var h = NewHarness(nameof(List_Search_FiltersOnCusDecNumber), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        // CusDecNumber is set by AddContainer as "CD-{cusDecId}"; container 1 → "CD-1"
        AddContainer(h.Db, 1, 1, declarantClaimed: true);
        AddContainer(h.Db, 2, 2, declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery(search: "CD-1"))).Value!;
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1L, Assert.Single(result.Items).CusDecContainerId);
    }

    [Fact]
    public async Task List_Search_PercentLiteral_MatchesContainerWithPercent()
    {
        var h = NewHarness(nameof(List_Search_PercentLiteral_MatchesContainerWithPercent),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, containerNumber: "CONT-50%", declarantClaimed: true);
        AddContainer(h.Db, 2, 2, containerNumber: "CONT-50X", declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        // "%" in search should be literal, not a wildcard
        var result = (await h.Service.GetContainersAsync(SubId, ListQuery(search: "50%"))).Value!;
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1L, Assert.Single(result.Items).CusDecContainerId);
    }

    [Fact]
    public async Task List_Search_UnderscoreLiteral_MatchesContainerWithUnderscore()
    {
        var h = NewHarness(nameof(List_Search_UnderscoreLiteral_MatchesContainerWithUnderscore),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, containerNumber: "CONT_AB", declarantClaimed: true);
        AddContainer(h.Db, 2, 2, containerNumber: "CONTXAB", declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery(search: "CONT_AB"))).Value!;
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1L, Assert.Single(result.Items).CusDecContainerId);
    }

    // ── Location and Status standalone filters ────────────────────────────────

    [Fact]
    public async Task List_LocationFilter_CaseInsensitive()
    {
        var h = NewHarness(nameof(List_LocationFilter_CaseInsensitive), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, location: "NORTH YARD", declarantClaimed: true);
        AddContainer(h.Db, 2, 2, location: "SOUTH SHED", declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery(location: "north"))).Value!;
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task List_StatusFilter_CaseInsensitive()
    {
        var h = NewHarness(nameof(List_StatusFilter_CaseInsensitive), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, currentStatus: "RELEASED", declarantClaimed: true);
        AddContainer(h.Db, 2, 2, currentStatus: "PENDING", declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery(status: "RELEASED"))).Value!;
        Assert.Equal(1, result.TotalCount);
    }

    // ── List currentStatus is raw (never mapped) ──────────────────────────────

    [Fact]
    public async Task List_CurrentStatus_IsRawNotMapped()
    {
        var h = NewHarness(nameof(List_CurrentStatus_IsRawNotMapped), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, currentStatus: "RAW_STATUS", declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        // Add a mapping for this status — the list should ignore it
        var mapping = new ContainerStatusMapping
        {
            ContainerStatusMappingId = 1,
            AsycudaStatus = "RAW_STATUS", AsycudaDescription = "",
            CdnsStatus = "MappedStatus", CdnsDescription = null, IsShow = true,
        };
        h.Db.ContainerStatusMappings.Add(mapping);
        h.Db.Entry(mapping).Property("AsycudaStatusKey").CurrentValue = "raw_status";
        h.Db.Entry(mapping).Property("AsycudaDescriptionKey").CurrentValue = "";
        await h.Db.SaveChangesAsync();

        var item = Assert.Single((await h.Service.GetContainersAsync(SubId, ListQuery())).Value!.Items);
        Assert.Equal("RAW_STATUS", item.CurrentStatus);
    }

    // ── Detail ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Detail_ReturnsContainerWithCusDecNumber()
    {
        var h = NewHarness(nameof(Detail_ReturnsContainerWithCusDecNumber), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 42, 7, containerNumber: "TCKU1234567", declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainerDetailAsync(SubId, 7)).Value!;
        Assert.Equal(7, result.CusDecContainerId);
        Assert.Equal("TCKU1234567", result.ContainerNumber);
        Assert.Equal(42, result.CusDecId);
        Assert.Equal("CD-42", result.CusDecNumber);
    }

    [Fact]
    public async Task Detail_OutOfScope_ReturnsNotFound()
    {
        var h = NewHarness(nameof(Detail_OutOfScope_ReturnsNotFound), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 99, DeclarantCompanyId = 999, ConsigneeCompanyId = 999,
            CusDecNumber = "CD-99", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 99,
        });
        h.Db.CusDecContainers.Add(new CusDecContainer
        {
            CusDecContainerId = 99, CusDecId = 99,
            Container = "OTHER", CusDecExternalSystemNumber = 99, ContainerKey = 99,
        });
        await h.Db.SaveChangesAsync();

        var result = await h.Service.GetContainerDetailAsync(SubId, 99);
        Assert.False(result.IsOk);
        Assert.Equal(SubscriptionAccessCode.NotFound, result.Code);
    }

    // ── Status history: only requested container's rows ───────────────────────

    [Fact]
    public async Task History_OutOfScope_ReturnsNotFound()
    {
        var h = NewHarness(nameof(History_OutOfScope_ReturnsNotFound), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 99, DeclarantCompanyId = 999,
            CusDecNumber = "CD-99", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 99,
        });
        h.Db.CusDecContainers.Add(new CusDecContainer
        {
            CusDecContainerId = 99, CusDecId = 99,
            Container = "X", CusDecExternalSystemNumber = 99, ContainerKey = 99,
        });
        await h.Db.SaveChangesAsync();

        var result = await h.Service.GetContainerHistoryAsync(SubId, 99);
        Assert.False(result.IsOk);
        Assert.Equal(SubscriptionAccessCode.NotFound, result.Code);
    }

    [Fact]
    public async Task History_HiddenStatus_IsExcluded_EarlierVisibleUsed()
    {
        var h = NewHarness(nameof(History_HiddenStatus_IsExcluded_EarlierVisibleUsed),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantClaimed: true);
        AddContainerStatus(h.Db, 1, "INTERNAL", "Internal note", StatusBase.AddDays(2),
            isShow: false, cdnsStatus: "Internal",
            statusKey: "internal", descriptionKey: "internal note");
        AddContainerStatus(h.Db, 1, "CLEARED", "Cleared", StatusBase,
            isShow: true, cdnsStatus: "Cleared",
            statusKey: "cleared", descriptionKey: "cleared");
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainerHistoryAsync(SubId, 1)).Value!;

        Assert.Single(result.Rows);
        Assert.Equal("Cleared", result.CurrentStatus);
    }

    [Fact]
    public async Task History_UnmappedStatus_ShownAsRaw()
    {
        var h = NewHarness(nameof(History_UnmappedStatus_ShownAsRaw), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantClaimed: true);
        AddContainerStatus(h.Db, 1, "RAWSTATUS", "Raw desc", StatusBase);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainerHistoryAsync(SubId, 1)).Value!;

        var row = Assert.Single(result.Rows);
        Assert.Equal("RAWSTATUS", row.Status);
        Assert.Equal("Raw desc", row.Description);
    }

    [Fact]
    public async Task History_MappedStatus_UsesCdnsLabel()
    {
        var h = NewHarness(nameof(History_MappedStatus_UsesCdnsLabel), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantClaimed: true);
        AddContainerStatus(h.Db, 1, "DOC_CLEARED", "Document cleared", StatusBase,
            isShow: true, cdnsStatus: "Documents Cleared", cdnsDesc: "All documents cleared",
            statusKey: "doc_cleared", descriptionKey: "document cleared");
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainerHistoryAsync(SubId, 1)).Value!;

        var row = Assert.Single(result.Rows);
        Assert.Equal("Documents Cleared", row.Status);
        Assert.Equal("All documents cleared", row.Description);
    }

    [Fact]
    public async Task History_EmptyMappingsTable_ShowsRaw()
    {
        var h = NewHarness(nameof(History_EmptyMappingsTable_ShowsRaw), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantClaimed: true);
        AddContainerStatus(h.Db, 1, "STATUS_X", "Desc X", StatusBase);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainerHistoryAsync(SubId, 1)).Value!;

        var row = Assert.Single(result.Rows);
        Assert.Equal("STATUS_X", row.Status);
    }

    [Fact]
    public async Task History_NewestFirst_WithDeterministicTieBreak()
    {
        var h = NewHarness(nameof(History_NewestFirst_WithDeterministicTieBreak), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantClaimed: true);
        // Same StatusDateTime — tie broken by CusDecContainerStatusId DESC
        AddContainerStatus(h.Db, 1, "A", "first inserted", StatusBase);   // id=1
        AddContainerStatus(h.Db, 1, "B", "second inserted", StatusBase);  // id=2
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainerHistoryAsync(SubId, 1)).Value!;
        var statuses = result.Rows.Select(r => r.Status).ToArray();
        Assert.Equal(new[] { "B", "A" }, statuses); // id DESC tie-break
    }

    [Fact]
    public async Task History_NullStatusDateTime_SortedLast()
    {
        var h = NewHarness(nameof(History_NullStatusDateTime_SortedLast), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantClaimed: true);
        // Insert null-date row first (lower id) so it doesn't win the tie-break
        AddContainerStatus(h.Db, 1, "NULL_DATE", "no date", at: default); // sets at=default → non-null in EF model
        // Let's simulate null by using AddContainerStatus and then patching
        h.Db.CusDecContainerStatuses.Local.Last().StatusDateTime = null;
        AddContainerStatus(h.Db, 1, "DATED", "has date", StatusBase.AddDays(1));
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainerHistoryAsync(SubId, 1)).Value!;
        Assert.Equal("DATED", result.Rows[0].Status);
        Assert.Equal("NULL_DATE", result.Rows[1].Status);
    }

    // ── Normalisation: Trim(' ') matches Postgres btrim ──────────────────────

    [Fact]
    public async Task Normalise_OuterSpaces_Match()
    {
        // " CLEARED " should match mapping key "cleared" (btrim removes spaces)
        var h = NewHarness(nameof(Normalise_OuterSpaces_Match), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantClaimed: true);
        // Status has outer spaces; mapping key is the btrim(lower()) result
        AddContainerStatus(h.Db, 1, " CLEARED ", " Cleared ", StatusBase,
            isShow: true, cdnsStatus: "Cleared CDNS",
            statusKey: "cleared", descriptionKey: "cleared");
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainerHistoryAsync(SubId, 1)).Value!;
        Assert.Equal("Cleared CDNS", result.CurrentStatus);
    }

    [Fact]
    public async Task Normalise_OuterCasing_Match()
    {
        // "CLEARED" and "cleared" should both match the same key
        var h = NewHarness(nameof(Normalise_OuterCasing_Match), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantClaimed: true);
        AddContainerStatus(h.Db, 1, "CLEARED", "DESCRIPTION", StatusBase,
            isShow: true, cdnsStatus: "Cleared CDNS",
            statusKey: "cleared", descriptionKey: "description");
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainerHistoryAsync(SubId, 1)).Value!;
        Assert.Equal("Cleared CDNS", result.CurrentStatus);
    }

    [Fact]
    public async Task Normalise_OuterTab_DoesNotMatch()
    {
        // A tab-padded status "\tCLEARED\t" should NOT match the key "cleared"
        // because btrim/Trim(' ') does NOT strip tabs.
        var h = NewHarness(nameof(Normalise_OuterTab_DoesNotMatch), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantClaimed: true);
        // Mapping key is "cleared" (space-trimmed); status has tabs → no match → raw shown
        AddContainerStatus(h.Db, 1, "\tCLEARED\t", "desc", StatusBase,
            isShow: true, cdnsStatus: "Should Not Appear",
            statusKey: "cleared", descriptionKey: "desc");
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainerHistoryAsync(SubId, 1)).Value!;
        // No match → raw value returned
        Assert.Equal("\tCLEARED\t", result.CurrentStatus);
    }

    [Fact]
    public async Task Normalise_DistinctDbKeys_NoDuplicateKeyError()
    {
        // Two mappings with different (statusKey, descriptionKey) must coexist without collision.
        var h = NewHarness(nameof(Normalise_DistinctDbKeys_NoDuplicateKeyError), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantClaimed: true);
        AddContainerStatus(h.Db, 1, "STATUS_A", "Desc A", StatusBase.AddDays(2),
            isShow: true, cdnsStatus: "CDNS A",
            statusKey: "status_a", descriptionKey: "desc a");
        AddContainerStatus(h.Db, 1, "STATUS_B", "Desc B", StatusBase,
            isShow: true, cdnsStatus: "CDNS B",
            statusKey: "status_b", descriptionKey: "desc b");
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainerHistoryAsync(SubId, 1)).Value!;
        // Both rows present and keyed correctly
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("CDNS A", result.Rows[0].Status);
        Assert.Equal("CDNS B", result.Rows[1].Status);
    }

    // ── Status resolution: same status, different description does not match ───

    [Fact]
    public async Task History_SameStatus_DifferentDescription_NoMatch()
    {
        // A mapping keyed to (status_a, desc_x) must NOT resolve (status_a, desc_y).
        var h = NewHarness(nameof(History_SameStatus_DifferentDescription_NoMatch), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantClaimed: true);
        // Mapping covers only "cleared" + "matched desc"
        AddContainerStatus(h.Db, 1, "CLEARED", "other desc", StatusBase,
            isShow: true, cdnsStatus: "Mapped",
            statusKey: "cleared", descriptionKey: "matched desc");
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainerHistoryAsync(SubId, 1)).Value!;
        // "other desc" doesn't match the key "matched desc" → raw value returned
        Assert.Equal("CLEARED", result.CurrentStatus);
        Assert.Equal("CLEARED", result.Rows[0].Status);
        Assert.Equal("other desc", result.Rows[0].Description);
    }

    // ── Nonexistent vs out-of-scope: indistinguishable on detail and status ────

    [Fact]
    public async Task Detail_NonexistentAndOutOfScope_GiveSameResult()
    {
        var h = NewHarness(nameof(Detail_NonexistentAndOutOfScope_GiveSameResult), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        // Container 99 is in scope but unclaimed (out of scope under new rule); 1000 does not exist.
        AddContainer(h.Db, 1, 99, declarantCompanyId: CompanyId); // unclaimed
        await h.Db.SaveChangesAsync();

        var rOutOfScope  = await h.Service.GetContainerDetailAsync(SubId, 99);
        var rNonExistent = await h.Service.GetContainerDetailAsync(SubId, 1000);
        Assert.Equal(SubscriptionAccessCode.NotFound, rOutOfScope.Code);
        Assert.Equal(SubscriptionAccessCode.NotFound, rNonExistent.Code);
    }

    [Fact]
    public async Task Status_NonexistentAndOutOfScope_GiveSameResult()
    {
        var h = NewHarness(nameof(Status_NonexistentAndOutOfScope_GiveSameResult), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 99, declarantCompanyId: CompanyId); // unclaimed
        await h.Db.SaveChangesAsync();

        var rOutOfScope  = await h.Service.GetContainerHistoryAsync(SubId, 99);
        var rNonExistent = await h.Service.GetContainerHistoryAsync(SubId, 1000);
        Assert.Equal(SubscriptionAccessCode.NotFound, rOutOfScope.Code);
        Assert.Equal(SubscriptionAccessCode.NotFound, rNonExistent.Code);
    }

    // ── Gate: invalid subscription key ───────────────────────────────────────

    [Fact]
    public async Task Gate_InvalidSubscriptionKey_ReturnsKeyMismatch_OnAllThreeEndpoints()
    {
        var h = NewHarness(nameof(Gate_InvalidSubscriptionKey_ReturnsKeyMismatch_OnAllThreeEndpoints),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantClaimed: true);

        var sub = await h.Db.MemberSubscriptions.FirstAsync();
        sub.Key = "bad-key";
        await h.Db.SaveChangesAsync();

        var r1 = await h.Service.GetContainersAsync(SubId, ListQuery());
        var r2 = await h.Service.GetContainerDetailAsync(SubId, 1);
        var r3 = await h.Service.GetContainerHistoryAsync(SubId, 1);
        Assert.Equal(SubscriptionAccessCode.KeyMismatch, r1.Code);
        Assert.Equal(SubscriptionAccessCode.KeyMismatch, r2.Code);
        Assert.Equal(SubscriptionAccessCode.KeyMismatch, r3.Code);
    }

    // ── Detail: CusDec metadata exposed ─────────────────────────────────────

    [Fact]
    public async Task Detail_CusDecMetadata_ExposedForDeclarant()
    {
        var h = NewHarness(nameof(Detail_CusDecMetadata_ExposedForDeclarant), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 1, DeclarantCompanyId = CompanyId, DeclarantClaimedById = 1,
            CusDecNumber = "CD-1", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 1,
            CusDecType = "IM4", Channel = "Green", Origin = "CN", BlNumber = "BL123",
            InvoiceValue = 15000m,
            DeclarantCompanyName = "Acme Declarant", DeclarantTinNumber = "DEC001",
            ConsigneeCompanyName = "Acme Consignee", ConsigneeTinNumber = "CON001",
        });
        h.Db.CusDecContainers.Add(new CusDecContainer
        {
            CusDecContainerId = 1, CusDecId = 1, Container = "TCKU1",
            CusDecExternalSystemNumber = 1, ContainerKey = 1,
        });
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainerDetailAsync(SubId, 1)).Value!;
        Assert.Equal("IM4",  result.CusDecType);
        Assert.Equal("Green", result.Channel);
        Assert.Equal("CN",   result.Origin);
        Assert.Equal("BL123", result.BlNumber);
        Assert.Equal(15000m, result.InvoiceValue);
        Assert.Equal("Acme Declarant",  result.DeclarantName);
        Assert.Equal("DEC001",          result.DeclarantTin);
        Assert.Equal("Acme Consignee",  result.ConsigneeName);
        Assert.Equal("CON001",          result.ConsigneeTin);
    }

    [Fact]
    public async Task Detail_CusDecMetadata_ExposedForConsignee()
    {
        var h = NewHarness(nameof(Detail_CusDecMetadata_ExposedForConsignee), new FakeCurrentUser(MemberOwner),
            subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 1, ConsigneeCompanyId = CompanyId, ConsigneeClaimedById = 1,
            CusDecNumber = "CD-1", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 1,
            CusDecType = "IM4", Channel = "Red", Origin = "US", BlNumber = "BL456",
            InvoiceValue = 8500m,
            DeclarantCompanyName = "Declarant Co", DeclarantTinNumber = "D999",
            ConsigneeCompanyName = "Consignee Co", ConsigneeTinNumber = "C999",
        });
        h.Db.CusDecContainers.Add(new CusDecContainer
        {
            CusDecContainerId = 1, CusDecId = 1, Container = "MSCU1",
            CusDecExternalSystemNumber = 1, ContainerKey = 1,
        });
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainerDetailAsync(SubId, 1)).Value!;
        Assert.Equal("IM4",        result.CusDecType);
        Assert.Equal("Red",        result.Channel);
        Assert.Equal("US",         result.Origin);
        Assert.Equal("BL456",      result.BlNumber);
        Assert.Equal(8500m,        result.InvoiceValue);
        Assert.Equal("Declarant Co",  result.DeclarantName);
        Assert.Equal("D999",          result.DeclarantTin);
        Assert.Equal("Consignee Co",  result.ConsigneeName);
        Assert.Equal("C999",          result.ConsigneeTin);
    }

    // ── Detail CurrentStatus: raw column, never from history ─────────────────

    [Fact]
    public async Task Detail_CurrentStatus_MatchesList_ForDeclarant()
    {
        var h = NewHarness(nameof(Detail_CurrentStatus_MatchesList_ForDeclarant),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, currentStatus: "RELEASED", declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var list   = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!.Items.Single();
        var detail = (await h.Service.GetContainerDetailAsync(SubId, 1)).Value!;

        Assert.Equal(list.CurrentStatus, detail.CurrentStatus);
        Assert.Equal("RELEASED", detail.CurrentStatus);
    }

    [Fact]
    public async Task Detail_CurrentStatus_MatchesList_ForConsignee()
    {
        var h = NewHarness(nameof(Detail_CurrentStatus_MatchesList_ForConsignee),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: null, consigneeCompanyId: CompanyId,
            currentStatus: "PENDING", consigneeClaimed: true);
        await h.Db.SaveChangesAsync();

        var list   = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!.Items.Single();
        var detail = (await h.Service.GetContainerDetailAsync(SubId, 1)).Value!;

        Assert.Equal(list.CurrentStatus, detail.CurrentStatus);
        Assert.Equal("PENDING", detail.CurrentStatus);
    }

    [Fact]
    public async Task Detail_CurrentStatus_RawNotMapped_WhenHistoryHasDifferentMapped_ForDeclarant()
    {
        // Container column = "RAW_COL"; history row maps to "MAPPED_STATUS".
        // Detail must return "RAW_COL" (the column), not "MAPPED_STATUS".
        var h = NewHarness(nameof(Detail_CurrentStatus_RawNotMapped_WhenHistoryHasDifferentMapped_ForDeclarant),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, currentStatus: "RAW_COL", declarantClaimed: true);
        AddContainerStatus(h.Db, 1, "HISTORY_STATUS", "desc", StatusBase,
            isShow: true, cdnsStatus: "MAPPED_STATUS",
            statusKey: "history_status", descriptionKey: "desc");
        await h.Db.SaveChangesAsync();

        var detail = (await h.Service.GetContainerDetailAsync(SubId, 1)).Value!;

        Assert.Equal("RAW_COL", detail.CurrentStatus);
    }

    [Fact]
    public async Task Detail_CurrentStatus_RawNotMapped_WhenHistoryHasDifferentMapped_ForConsignee()
    {
        var h = NewHarness(nameof(Detail_CurrentStatus_RawNotMapped_WhenHistoryHasDifferentMapped_ForConsignee),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: null, consigneeCompanyId: CompanyId,
            currentStatus: "RAW_COL", consigneeClaimed: true);
        AddContainerStatus(h.Db, 1, "HISTORY_STATUS", "desc", StatusBase,
            isShow: true, cdnsStatus: "MAPPED_STATUS",
            statusKey: "history_status", descriptionKey: "desc");
        await h.Db.SaveChangesAsync();

        var detail = (await h.Service.GetContainerDetailAsync(SubId, 1)).Value!;

        Assert.Equal("RAW_COL", detail.CurrentStatus);
    }

    [Fact]
    public async Task Detail_CurrentStatus_ReturnedWithNoHistory_ForDeclarant()
    {
        var h = NewHarness(nameof(Detail_CurrentStatus_ReturnedWithNoHistory_ForDeclarant),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, currentStatus: "CLEARED", declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var detail = (await h.Service.GetContainerDetailAsync(SubId, 1)).Value!;

        Assert.Equal("CLEARED", detail.CurrentStatus);
    }

    [Fact]
    public async Task Detail_CurrentStatus_ReturnedWithNoHistory_ForConsignee()
    {
        var h = NewHarness(nameof(Detail_CurrentStatus_ReturnedWithNoHistory_ForConsignee),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: null, consigneeCompanyId: CompanyId,
            currentStatus: "CLEARED", consigneeClaimed: true);
        await h.Db.SaveChangesAsync();

        var detail = (await h.Service.GetContainerDetailAsync(SubId, 1)).Value!;

        Assert.Equal("CLEARED", detail.CurrentStatus);
    }

    [Fact]
    public async Task Detail_CurrentStatus_NullWhenColumnNull_ForDeclarant()
    {
        var h = NewHarness(nameof(Detail_CurrentStatus_NullWhenColumnNull_ForDeclarant),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, currentStatus: null, declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var detail = (await h.Service.GetContainerDetailAsync(SubId, 1)).Value!;

        Assert.Null(detail.CurrentStatus);
    }

    [Fact]
    public async Task Detail_CurrentStatus_NullWhenColumnNull_ForConsignee()
    {
        var h = NewHarness(nameof(Detail_CurrentStatus_NullWhenColumnNull_ForConsignee),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: null, consigneeCompanyId: CompanyId,
            currentStatus: null, consigneeClaimed: true);
        await h.Db.SaveChangesAsync();

        var detail = (await h.Service.GetContainerDetailAsync(SubId, 1)).Value!;

        Assert.Null(detail.CurrentStatus);
    }

    // ── Detail: cusDecDate matches list, correct UTC, null case ──────────────

    private static readonly DateTime SomeCusDecDate = new(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset ExpectedCusDecDateUtc =
        new DateTimeOffset(DateTime.SpecifyKind(SomeCusDecDate, DateTimeKind.Utc));

    [Fact]
    public async Task Detail_CusDecDate_MatchesList_AndEqualsExpectedUtc_ForDeclarant()
    {
        var h = NewHarness(nameof(Detail_CusDecDate_MatchesList_AndEqualsExpectedUtc_ForDeclarant),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, cusDecDate: SomeCusDecDate, declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var list   = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!.Items.Single();
        var detail = (await h.Service.GetContainerDetailAsync(SubId, 1)).Value!;

        Assert.Equal(list.CusDecDate, detail.CusDecDate);
        Assert.Equal(ExpectedCusDecDateUtc, detail.CusDecDate);
    }

    [Fact]
    public async Task Detail_CusDecDate_MatchesList_AndEqualsExpectedUtc_ForConsignee()
    {
        var h = NewHarness(nameof(Detail_CusDecDate_MatchesList_AndEqualsExpectedUtc_ForConsignee),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: null, consigneeCompanyId: CompanyId,
            cusDecDate: SomeCusDecDate, consigneeClaimed: true);
        await h.Db.SaveChangesAsync();

        var list   = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!.Items.Single();
        var detail = (await h.Service.GetContainerDetailAsync(SubId, 1)).Value!;

        Assert.Equal(list.CusDecDate, detail.CusDecDate);
        Assert.Equal(ExpectedCusDecDateUtc, detail.CusDecDate);
    }

    [Fact]
    public async Task Detail_CusDecDate_NullWhenCusDecDateNull_ForDeclarant()
    {
        var h = NewHarness(nameof(Detail_CusDecDate_NullWhenCusDecDateNull_ForDeclarant),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, cusDecDate: null, declarantClaimed: true);
        await h.Db.SaveChangesAsync();

        var detail = (await h.Service.GetContainerDetailAsync(SubId, 1)).Value!;

        Assert.Null(detail.CusDecDate);
    }

    [Fact]
    public async Task Detail_CusDecDate_NullWhenCusDecDateNull_ForConsignee()
    {
        var h = NewHarness(nameof(Detail_CusDecDate_NullWhenCusDecDateNull_ForConsignee),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: null, consigneeCompanyId: CompanyId,
            cusDecDate: null, consigneeClaimed: true);
        await h.Db.SaveChangesAsync();

        var detail = (await h.Service.GetContainerDetailAsync(SubId, 1)).Value!;

        Assert.Null(detail.CusDecDate);
    }

    // ── Gate: KeyMissing + Consignee AcknowledgementOnly ─────────────────────

    [Fact]
    public async Task Gate_MissingSubscriptionKey_ReturnsKeyMissing_OnAllThreeEndpoints()
    {
        var h = NewHarness(nameof(Gate_MissingSubscriptionKey_ReturnsKeyMissing_OnAllThreeEndpoints),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantClaimed: true);

        var sub = await h.Db.MemberSubscriptions.FirstAsync();
        sub.Key = null;
        await h.Db.SaveChangesAsync();

        var r1 = await h.Service.GetContainersAsync(SubId, ListQuery());
        var r2 = await h.Service.GetContainerDetailAsync(SubId, 1);
        var r3 = await h.Service.GetContainerHistoryAsync(SubId, 1);
        Assert.Equal(SubscriptionAccessCode.KeyMissing, r1.Code);
        Assert.Equal(SubscriptionAccessCode.KeyMissing, r2.Code);
        Assert.Equal(SubscriptionAccessCode.KeyMissing, r3.Code);
    }

    [Fact]
    public async Task Gate_Consignee_AcknowledgementOnly_ReturnsAcknowledgementOnly_OnAllThreeEndpoints()
    {
        var h = NewHarness(
            nameof(Gate_Consignee_AcknowledgementOnly_ReturnsAcknowledgementOnly_OnAllThreeEndpoints),
            new FakeCurrentUser(MemberOwner),
            subType:    (int)SubscriptionTypeEnum.Consignee,
            actionType: (int)SubscriptionActionTypeEnum.OnlyForAcknowledgement);
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: null, consigneeCompanyId: CompanyId,
            consigneeClaimed: true);
        await h.Db.SaveChangesAsync();

        var r1 = await h.Service.GetContainersAsync(SubId, ListQuery());
        var r2 = await h.Service.GetContainerDetailAsync(SubId, 1);
        var r3 = await h.Service.GetContainerHistoryAsync(SubId, 1);
        Assert.Equal(SubscriptionAccessCode.AcknowledgementOnly, r1.Code);
        Assert.Equal(SubscriptionAccessCode.AcknowledgementOnly, r2.Code);
        Assert.Equal(SubscriptionAccessCode.AcknowledgementOnly, r3.Code);
    }

    // ── Declarant: disclaimed cusDecId filter ─────────────────────────────────

    [Fact]
    public async Task Declarant_Disclaimed_CusDecIdFilterReturns404()
    {
        var h = NewHarness(nameof(Declarant_Disclaimed_CusDecIdFilterReturns404),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 1, DeclarantCompanyId = CompanyId, DeclarantClaimedById = 1,
            DeclarantDisclaimedById = 1,
            CusDecNumber = "CD-1", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 1,
        });
        await h.Db.SaveChangesAsync();

        var result = await h.Service.GetContainersAsync(SubId, ListQuery(cusDecId: 1));
        Assert.Equal(SubscriptionAccessCode.NotFound, result.Code);
    }

    // ── Consignee: unclaimed/disclaimed on detail, status, and cusDecId ───────

    [Fact]
    public async Task Consignee_Unclaimed_DetailAndStatusReturn404()
    {
        var h = NewHarness(nameof(Consignee_Unclaimed_DetailAndStatusReturn404),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: null, consigneeCompanyId: CompanyId);
        await h.Db.SaveChangesAsync();

        var r2 = await h.Service.GetContainerDetailAsync(SubId, 1);
        var r3 = await h.Service.GetContainerHistoryAsync(SubId, 1);
        Assert.Equal(SubscriptionAccessCode.NotFound, r2.Code);
        Assert.Equal(SubscriptionAccessCode.NotFound, r3.Code);
    }

    [Fact]
    public async Task Consignee_Unclaimed_CusDecIdFilterReturns404()
    {
        var h = NewHarness(nameof(Consignee_Unclaimed_CusDecIdFilterReturns404),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 1, ConsigneeCompanyId = CompanyId,
            CusDecNumber = "CD-1", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 1,
        });
        await h.Db.SaveChangesAsync();

        var result = await h.Service.GetContainersAsync(SubId, ListQuery(cusDecId: 1));
        Assert.Equal(SubscriptionAccessCode.NotFound, result.Code);
    }

    [Fact]
    public async Task Consignee_Disclaimed_DetailAndStatusReturn404()
    {
        var h = NewHarness(nameof(Consignee_Disclaimed_DetailAndStatusReturn404),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: null, consigneeCompanyId: CompanyId,
            consigneeClaimed: true, consigneeDisclaimed: true);
        await h.Db.SaveChangesAsync();

        Assert.Equal(SubscriptionAccessCode.NotFound,
            (await h.Service.GetContainerDetailAsync(SubId, 1)).Code);
        Assert.Equal(SubscriptionAccessCode.NotFound,
            (await h.Service.GetContainerHistoryAsync(SubId, 1)).Code);
    }

    [Fact]
    public async Task Consignee_Disclaimed_CusDecIdFilterReturns404()
    {
        var h = NewHarness(nameof(Consignee_Disclaimed_CusDecIdFilterReturns404),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 1, ConsigneeCompanyId = CompanyId, ConsigneeClaimedById = 1,
            ConsigneeDisclaimedById = 1,
            CusDecNumber = "CD-1", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 1,
        });
        await h.Db.SaveChangesAsync();

        var result = await h.Service.GetContainersAsync(SubId, ListQuery(cusDecId: 1));
        Assert.Equal(SubscriptionAccessCode.NotFound, result.Code);
    }

    // ── Consignee: other-side claim/disclaim ──────────────────────────────────

    [Fact]
    public async Task Consignee_OtherSide_Claim_DoesNotGrantAccess()
    {
        // Consignee subscription: declarant-claimed only does NOT make the container visible.
        var h = NewHarness(nameof(Consignee_OtherSide_Claim_DoesNotGrantAccess),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: Company2Id, consigneeCompanyId: CompanyId,
            declarantClaimed: true, consigneeClaimed: false);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Consignee_OtherSide_Disclaim_DoesNotHide()
    {
        // Consignee subscription: declarant-disclaimed does NOT hide a consignee-claimed container.
        var h = NewHarness(nameof(Consignee_OtherSide_Disclaim_DoesNotHide),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: Company2Id, consigneeCompanyId: CompanyId,
            consigneeClaimed: true, declarantDisclaimed: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(1, result.TotalCount);
    }

    // ── Consignee: another member, completed ──────────────────────────────────

    [Fact]
    public async Task Consignee_AnotherMember_SameCompany_ClaimedIsIncluded()
    {
        // MemberOther claimed the CusDec on the consignee side; subscription owner can still see it.
        var h = NewHarness(nameof(Consignee_AnotherMember_SameCompany_ClaimedIsIncluded),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        h.Db.Members.Add(new Member
        {
            MemberId = MemberOther, FirstName = "Other", LastName = "Member",
            Email = "o@t", MemberType = MemberType.Member,
        });
        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 1, ConsigneeCompanyId = CompanyId,
            CusDecNumber = "CD-1", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 1,
            ConsigneeClaimedById = MemberOther,
        });
        h.Db.CusDecContainers.Add(new CusDecContainer
        {
            CusDecContainerId = 1, CusDecId = 1, Container = "CONT-1",
            CusDecExternalSystemNumber = 1, ContainerKey = 1,
        });
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Consignee_Completed_CusDec_ContainerVisible()
    {
        var h = NewHarness(nameof(Consignee_Completed_CusDec_ContainerVisible),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 1, declarantCompanyId: null, consigneeCompanyId: CompanyId,
            consigneeClaimed: true, consigneeCompleted: true);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(1, result.TotalCount);
    }

    // ── Consignee: another company ────────────────────────────────────────────

    [Fact]
    public async Task Consignee_AnotherCompany_ExcludedFromList()
    {
        var h = NewHarness(nameof(Consignee_AnotherCompany_ExcludedFromList),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        // CusDec belongs to company 999 on consignee side — not CompanyId (1).
        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 99, ConsigneeCompanyId = 999, ConsigneeClaimedById = 1,
            CusDecNumber = "CD-99", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 99,
        });
        h.Db.CusDecContainers.Add(new CusDecContainer
        {
            CusDecContainerId = 99, CusDecId = 99,
            Container = "OTHER", CusDecExternalSystemNumber = 99, ContainerKey = 99,
        });
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetContainersAsync(SubId, ListQuery())).Value!;
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Consignee_AnotherCompany_DetailAndStatusAndCusDecIdReturn404()
    {
        var h = NewHarness(nameof(Consignee_AnotherCompany_DetailAndStatusAndCusDecIdReturn404),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        h.Db.CusDecs.Add(new CusDec
        {
            CusDecId = 99, ConsigneeCompanyId = 999, ConsigneeClaimedById = 1,
            CusDecNumber = "CD-99", GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = 99,
        });
        h.Db.CusDecContainers.Add(new CusDecContainer
        {
            CusDecContainerId = 99, CusDecId = 99,
            Container = "OTHER", CusDecExternalSystemNumber = 99, ContainerKey = 99,
        });
        await h.Db.SaveChangesAsync();

        Assert.Equal(SubscriptionAccessCode.NotFound,
            (await h.Service.GetContainerDetailAsync(SubId, 99)).Code);
        Assert.Equal(SubscriptionAccessCode.NotFound,
            (await h.Service.GetContainerHistoryAsync(SubId, 99)).Code);
        Assert.Equal(SubscriptionAccessCode.NotFound,
            (await h.Service.GetContainersAsync(SubId, ListQuery(cusDecId: 99))).Code);
    }

    // ── Consignee: missing id and out-of-scope give the same result ───────────

    [Fact]
    public async Task Consignee_Detail_NonexistentAndOutOfScope_GiveSameResult()
    {
        var h = NewHarness(nameof(Consignee_Detail_NonexistentAndOutOfScope_GiveSameResult),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        // Container 99 is in the right company but unclaimed (out of scope).
        AddContainer(h.Db, 1, 99, declarantCompanyId: null, consigneeCompanyId: CompanyId);
        await h.Db.SaveChangesAsync();

        var rOutOfScope  = await h.Service.GetContainerDetailAsync(SubId, 99);
        var rNonExistent = await h.Service.GetContainerDetailAsync(SubId, 1000);
        Assert.Equal(SubscriptionAccessCode.NotFound, rOutOfScope.Code);
        Assert.Equal(SubscriptionAccessCode.NotFound, rNonExistent.Code);
    }

    [Fact]
    public async Task Consignee_Status_NonexistentAndOutOfScope_GiveSameResult()
    {
        var h = NewHarness(nameof(Consignee_Status_NonexistentAndOutOfScope_GiveSameResult),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddContainer(h.Db, 1, 99, declarantCompanyId: null, consigneeCompanyId: CompanyId);
        await h.Db.SaveChangesAsync();

        var rOutOfScope  = await h.Service.GetContainerHistoryAsync(SubId, 99);
        var rNonExistent = await h.Service.GetContainerHistoryAsync(SubId, 1000);
        Assert.Equal(SubscriptionAccessCode.NotFound, rOutOfScope.Code);
        Assert.Equal(SubscriptionAccessCode.NotFound, rNonExistent.Code);
    }

    [Fact]
    public async Task Consignee_CusDecIdFilter_NonexistentId_SameResultAsOutOfScope()
    {
        var h = NewHarness(nameof(Consignee_CusDecIdFilter_NonexistentId_SameResultAsOutOfScope),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;

        var rNone  = await h.Service.GetContainersAsync(SubId, ListQuery(cusDecId: 9999));
        var rOther = await h.Service.GetContainersAsync(SubId, ListQuery(cusDecId: 9998));
        Assert.Equal(SubscriptionAccessCode.NotFound, rNone.Code);
        Assert.Equal(SubscriptionAccessCode.NotFound, rOther.Code);
    }
}
