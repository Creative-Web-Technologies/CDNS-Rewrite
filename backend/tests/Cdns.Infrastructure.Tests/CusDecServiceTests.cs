using Cdns.Application.Feature.Cusdec;
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

public class CusDecServiceTests
{
    private const int CompanyId = 1;
    private const long MemberOwner = 1;
    private const long MasterId = 10;
    private const long SubId = 100;

    private static readonly DateTimeOffset Now = new(2026, 11, 23, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateTime StatusBase = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    // ── Harness ───────────────────────────────────────────────────────────────

    private sealed class Harness
    {
        public required CdnsDbContext Db { get; init; }
        public required FakeBusinessClock Clock { get; init; }
        public required IMemberSubscriptionKeyService Keys { get; init; }
        public required CusDecService Service { get; init; }
    }

    private static Harness NewHarness(
        string name,
        ICurrentUser user,
        int subType    = (int)SubscriptionTypeEnum.Declarant,
        int actionType = (int)SubscriptionActionTypeEnum.General,
        int companyStatus = (int)CompanyStatusEnum.Active,
        long memberId  = MemberOwner,
        long masterId  = MasterId,
        DateTimeOffset? expiry = null)
    {
        var db    = TestHelpers.CreateDb(name);
        var clock = new FakeBusinessClock(Now);
        var keys  = FakeKeyServiceFactory.Create();

        db.Companies.Add(new Company
        {
            CompanyId = CompanyId, CompanyName = "Acme", TinNumber = "123456789",
            IsDeleted = false, CompanyStatusEnum = companyStatus,
        });
        db.Members.Add(new Member { MemberId = memberId, FirstName = "Mandy", LastName = "Member",
            Email = "m@t", MemberType = MemberType.Member });
        if (masterId != memberId)
            db.Members.Add(new Member { MemberId = masterId, FirstName = "Marty", LastName = "Master",
                Email = "x@t", MemberType = MemberType.CompanyMasterMember });

        var sub = new MemberSubscription
        {
            MemberSubscriptionId = SubId, MemberId = memberId, CompanyId = CompanyId,
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
            Service = new CusDecService(db, user, clock, TestHelpers.SubscriptionScope(db, user, keys, clock)),
        };
    }

    private static long _nextId = 1;

    private static CusDec AddCusDec(
        CdnsDbContext db,
        long id,
        int? declarantCompanyId    = CompanyId,
        int? consigneeCompanyId    = null,
        long? declarantClaimedBy   = null,
        long? consigneeClaimedBy   = null,
        long? declarantDisclaimedBy = null,
        long? consigneeDisclaimedBy = null,
        long? declarantCompletedBy  = null,
        long? consigneeCompletedBy  = null,
        DateTime? completedAt      = null,
        DateTime? disclaimedAt     = null,
        DateTime? cusDecDate       = null,
        string? office             = "COL",
        string? number             = null,
        string? declarantTin       = null,
        string? consigneeTin       = null)
    {
        var c = new CusDec
        {
            CusDecId = id,
            CusDecNumber = number ?? $"CD-{id}",
            DeclarantCompanyId = declarantCompanyId,
            ConsigneeCompanyId = consigneeCompanyId,
            DeclarantTinNumber = declarantTin,
            ConsigneeTinNumber = consigneeTin,
            DeclarantCompanyName = "Decl Co",
            ConsigneeCompanyName = "Cons Co",
            DeclarantClaimedById = declarantClaimedBy,
            DeclarantClaimedDateTime = declarantClaimedBy is null ? null : StatusBase,
            ConsigneeClaimedById = consigneeClaimedBy,
            ConsigneeClaimedDateTime = consigneeClaimedBy is null ? null : StatusBase,
            DeclarantDisclaimedById = declarantDisclaimedBy,
            DeclarantDisclaimedDateTime = declarantDisclaimedBy is null ? null : (disclaimedAt ?? StatusBase),
            ConsigneeDisclaimedById = consigneeDisclaimedBy,
            ConsigneeDisclaimedDateTime = consigneeDisclaimedBy is null ? null : (disclaimedAt ?? StatusBase),
            DeclarantCompletedById = declarantCompletedBy,
            DeclarantCompletedDateTime = declarantCompletedBy is null ? null : (completedAt ?? StatusBase),
            ConsigneeCompletedById = consigneeCompletedBy,
            ConsigneeCompletedDateTime = consigneeCompletedBy is null ? null : (completedAt ?? StatusBase),
            CusDecDate = cusDecDate ?? StatusBase,
            OfficeCode = office,
            GlobalIdentity = Guid.NewGuid(),
            CusDecExternalSystemNumber = id,
        };
        db.CusDecs.Add(c);
        return c;
    }

    private static void AddStatus(CdnsDbContext db, long cusDecId, string status,
        DateTime at, bool inspect = false, bool isHidden = false, string? mappedCdns = null)
    {
        // If a mapped CDNS label is requested, insert the mapping first.
        if (mappedCdns is not null)
        {
            db.CusDecStatusMappings.Add(new CusDecStatusMapping
            {
                CusDecStatusMappingId = (int)_nextId,
                AsycudaStatus = status,
                CdnsStatus = mappedCdns,
                IsShow = !isHidden,
            });
        }

        if (inspect)
            db.CusDecInspectStatuses.Add(new CusDecInspectStatus
            {
                CusDecInspectStatusId = _nextId++, CusDecId = cusDecId,
                Status = status, StatusDateTime = at, TableName = "inspect",
            });
        else
            db.CusDecStatuses.Add(new CusDecStatus
            {
                CusDecStatusId = _nextId++, CusDecId = cusDecId,
                Status = status, StatusDateTime = at, TableName = "normal",
            });
    }

    private static CusDecListQuery Query(
        string tab = "tracking", string state = "all",
        string? search = null, string? office = null,
        DateOnly? from = null, DateOnly? to = null,
        string? sort = null, int page = 1, int pageSize = 4,
        string? declarantTin = null, string? consigneeTin = null,
        string? cusDecNumber = null)
        => new(tab, state, search, office, from, to, sort, page, pageSize,
               declarantTin, consigneeTin, cusDecNumber);

    // ── Tab rules ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Tabs_ClassifyNewInProgressCompletedDisclaimed()
    {
        var h = NewHarness(nameof(Tabs_ClassifyNewInProgressCompletedDisclaimed), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        // New: unclaimed
        AddCusDec(h.Db, 1);
        // In progress: claimed, not disclaimed, not completed
        AddCusDec(h.Db, 2, declarantClaimedBy: MemberOwner);
        // Completed: claimed, not disclaimed, completed-by set (column-based, not status-based)
        AddCusDec(h.Db, 3, declarantClaimedBy: MemberOwner, declarantCompletedBy: MemberOwner);
        // Disclaimed
        AddCusDec(h.Db, 4, declarantDisclaimedBy: MemberOwner);
        await h.Db.SaveChangesAsync();

        var strip = (await h.Service.GetSubscriptionStripAsync(SubId)).Value!;
        Assert.Equal(1, strip.Counts.TrackingNew);
        Assert.Equal(1, strip.Counts.TrackingInProgress);
        Assert.Equal(1, strip.Counts.Completed);
        Assert.Equal(1, strip.Counts.Disclaimed);

        var tracking = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking"))).Value!;
        Assert.Equal(2, tracking.TotalCount); // New + In progress (completed excluded)

        var completed = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "completed"))).Value!;
        Assert.Equal(new long[] { 3 }, completed.Items.Select(i => i.CusDecId).ToArray());

        var disclaimed = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "disclaimed"))).Value!;
        Assert.Equal(new long[] { 4 }, disclaimed.Items.Select(i => i.CusDecId).ToArray());
    }

    [Fact]
    public async Task TrackingState_FiltersNewVsInProgress()
    {
        var h = NewHarness(nameof(TrackingState_FiltersNewVsInProgress), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        AddCusDec(h.Db, 1);                                   // New
        AddCusDec(h.Db, 2, declarantClaimedBy: MemberOwner);  // In progress
        await h.Db.SaveChangesAsync();

        var newOnly = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", state: "new"))).Value!;
        Assert.Equal(new long[] { 1 }, newOnly.Items.Select(i => i.CusDecId).ToArray());
        Assert.All(newOnly.Items, i => Assert.Equal("new", i.State));

        var inProgress = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", state: "inprogress"))).Value!;
        Assert.Equal(new long[] { 2 }, inProgress.Items.Select(i => i.CusDecId).ToArray());
        Assert.All(inProgress.Items, i => Assert.Equal("inprogress", i.State));
    }

    [Fact]
    public async Task UnclaimedWithExitStatus_StaysNew_NotCompleted()
    {
        // Even if status says "Totally exited", unclaimed = New (no completed-by column).
        var h = NewHarness(nameof(UnclaimedWithExitStatus_StaysNew_NotCompleted), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        AddCusDec(h.Db, 1);
        AddStatus(h.Db, 1, "Totally exited", StatusBase);
        await h.Db.SaveChangesAsync();

        var strip = (await h.Service.GetSubscriptionStripAsync(SubId)).Value!;
        Assert.Equal(1, strip.Counts.TrackingNew);
        Assert.Equal(0, strip.Counts.Completed);

        var tracking = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking"))).Value!;
        Assert.Equal("new", Assert.Single(tracking.Items).State);
    }

    // ── Status label: latest across both tables, respecting IsShow ────────────

    [Fact]
    public async Task CurrentStatus_IsLatestAcrossBothStatusTables()
    {
        var h = NewHarness(nameof(CurrentStatus_IsLatestAcrossBothStatusTables), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        // In progress row; inspect status is later → should be the current label.
        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner);
        AddStatus(h.Db, 1, "Registered",   StatusBase);
        AddStatus(h.Db, 1, "Under Review",  StatusBase.AddDays(2), inspect: true);
        await h.Db.SaveChangesAsync();

        var list = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", state: "inprogress"))).Value!;
        var item = Assert.Single(list.Items);
        Assert.Equal("Under Review", item.CurrentStatus);
    }

    [Fact]
    public async Task HiddenLatestStatus_IsSkipped_EarlierVisibleUsed()
    {
        // The latest status row's mapping has IsShow = false → it is skipped;
        // the earlier visible row becomes the current status label.
        var h = NewHarness(nameof(HiddenLatestStatus_IsSkipped_EarlierVisibleUsed), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        // Add a hidden mapping for "INTERNAL" (IsShow = false).
        h.Db.CusDecStatusMappings.Add(new CusDecStatusMapping
        {
            CusDecStatusMappingId = 1,
            AsycudaStatus = "INTERNAL", CdnsStatus = "Internal note", IsShow = false,
        });
        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner);
        // Earlier visible row
        AddStatus(h.Db, 1, "Registered", StatusBase);
        // Later hidden row
        AddStatus(h.Db, 1, "INTERNAL", StatusBase.AddDays(1));
        await h.Db.SaveChangesAsync();

        var list = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", state: "inprogress"))).Value!;
        var item = Assert.Single(list.Items);
        Assert.Equal("Registered", item.CurrentStatus); // hidden "INTERNAL" skipped
    }

    [Fact]
    public async Task ExitStatus_ResolvedThroughOfficeMapping_ShowsOnCurrentStatusLabel()
    {
        // A mapping with OfficeCode resolves "EX" → "Totally exited" for that office.
        var h = NewHarness(nameof(ExitStatus_ResolvedThroughOfficeMapping_ShowsOnCurrentStatusLabel),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        h.Db.CusDecStatusMappings.Add(new CusDecStatusMapping
        {
            CusDecStatusMappingId = 1,
            AsycudaStatus = "EX", CdnsStatus = "Totally exited", OfficeCode = "COL", IsShow = true,
        });
        // Completed via column, with the "EX" ASYCUDA status
        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner, declarantCompletedBy: MemberOwner, office: "COL");
        AddStatus(h.Db, 1, "EX", StatusBase);
        await h.Db.SaveChangesAsync();

        var completed = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "completed"))).Value!;
        var item = Assert.Single(completed.Items);
        Assert.Equal("Totally exited", item.CurrentStatus); // office-aware mapping applied
    }

    // ── IsExited flag ─────────────────────────────────────────────────────────

    [Fact]
    public async Task IsExited_TrueForInProgressWithExitStatus()
    {
        var h = NewHarness(nameof(IsExited_TrueForInProgressWithExitStatus), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner);
        AddStatus(h.Db, 1, "Totally exited", StatusBase);
        await h.Db.SaveChangesAsync();

        var list = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", state: "inprogress"))).Value!;
        var item = Assert.Single(list.Items);
        Assert.True(item.IsExited);
    }

    [Fact]
    public async Task IsExited_FalseForInProgressWithoutExitStatus()
    {
        var h = NewHarness(nameof(IsExited_FalseForInProgressWithoutExitStatus), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner);
        AddStatus(h.Db, 1, "Registered", StatusBase);
        await h.Db.SaveChangesAsync();

        var list = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", state: "inprogress"))).Value!;
        Assert.False(Assert.Single(list.Items).IsExited);
    }

    [Fact]
    public async Task IsExited_CancelledCountsAsExit()
    {
        var h = NewHarness(nameof(IsExited_CancelledCountsAsExit), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner);
        AddStatus(h.Db, 1, "Cancelled", StatusBase);
        await h.Db.SaveChangesAsync();

        var list = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", state: "inprogress"))).Value!;
        Assert.True(Assert.Single(list.Items).IsExited);
    }

    [Fact]
    public async Task IsExited_FalseForNonInProgressRows()
    {
        // IsExited is always false for Completed and Disclaimed rows.
        var h = NewHarness(nameof(IsExited_FalseForNonInProgressRows), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner, declarantCompletedBy: MemberOwner);
        AddStatus(h.Db, 1, "Totally exited", StatusBase);
        await h.Db.SaveChangesAsync();

        var completed = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "completed"))).Value!;
        Assert.False(Assert.Single(completed.Items).IsExited);
    }

    [Fact]
    public async Task IsExited_RawCodeNotInExitList_DoesNotSetIsExited()
    {
        // "EXIT_INTERNAL" maps to "Totally exited" but its raw ASYCUDA code is not
        // in ['Totally exited', 'Cancelled'], so IsExited stays false.
        // (A hidden row whose raw code IS "Totally exited" would set IsExited = true;
        //  that behaviour is tested in IsExited_HiddenRawExitStatus_SetsIsExited.)
        var h = NewHarness(nameof(IsExited_RawCodeNotInExitList_DoesNotSetIsExited), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        h.Db.CusDecStatusMappings.Add(new CusDecStatusMapping
        {
            CusDecStatusMappingId = 1,
            AsycudaStatus = "EXIT_INTERNAL", CdnsStatus = "Totally exited", IsShow = false,
        });
        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner);
        AddStatus(h.Db, 1, "EXIT_INTERNAL", StatusBase);
        AddStatus(h.Db, 1, "Registered", StatusBase.AddMinutes(-1)); // visible, not exit
        await h.Db.SaveChangesAsync();

        var list = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", state: "inprogress"))).Value!;
        var item = Assert.Single(list.Items);
        Assert.False(item.IsExited);
        Assert.Equal("Registered", item.CurrentStatus); // hidden row skipped for label
    }

    [Fact]
    public async Task IsExited_HiddenRawExitStatus_SetsIsExited()
    {
        // A status row whose raw ASYCUDA code is literally "Totally exited" sets
        // IsExited = true even when its mapping has IsShow = false. The raw code is
        // what matters, not whether the row is surfaced as the current label.
        var h = NewHarness(nameof(IsExited_HiddenRawExitStatus_SetsIsExited), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        h.Db.CusDecStatusMappings.Add(new CusDecStatusMapping
        {
            CusDecStatusMappingId = (int)_nextId++,
            AsycudaStatus = "Totally exited", CdnsStatus = "Totally exited", IsShow = false,
        });
        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner);
        AddStatus(h.Db, 1, "Totally exited", StatusBase); // hidden mapping, but raw code is in exit list
        AddStatus(h.Db, 1, "Registered", StatusBase.AddMinutes(-1));
        await h.Db.SaveChangesAsync();

        var list = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", state: "inprogress"))).Value!;
        var item = Assert.Single(list.Items);
        Assert.True(item.IsExited);
        Assert.Equal("Registered", item.CurrentStatus); // hidden "Totally exited" still skipped for label
    }

    // ── Declarant vs Consignee matching ──────────────────────────────────────

    [Fact]
    public async Task DeclarantSubscription_MatchesDeclarantColumnsOnly()
    {
        var h = NewHarness(nameof(DeclarantSubscription_MatchesDeclarantColumnsOnly),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Declarant);
        await using var _ = h.Db;

        AddCusDec(h.Db, 1, declarantCompanyId: CompanyId, consigneeCompanyId: 999); // visible
        AddCusDec(h.Db, 2, declarantCompanyId: 999, consigneeCompanyId: CompanyId); // not visible
        await h.Db.SaveChangesAsync();

        var tracking = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking"))).Value!;
        Assert.Equal(new long[] { 1 }, tracking.Items.Select(i => i.CusDecId).ToArray());
    }

    [Fact]
    public async Task ConsigneeSubscription_MatchesConsigneeColumns_AndClaims()
    {
        var h = NewHarness(nameof(ConsigneeSubscription_MatchesConsigneeColumns_AndClaims),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;

        AddCusDec(h.Db, 1, declarantCompanyId: 999, consigneeCompanyId: CompanyId);
        await h.Db.SaveChangesAsync();

        var ack = await h.Service.AcknowledgeAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.Ok, ack.Status);

        var c = await h.Db.CusDecs.FindAsync(1L);
        Assert.Equal(MemberOwner, c!.ConsigneeClaimedById);
        Assert.Null(c.DeclarantClaimedById);
    }

    [Fact]
    public async Task Complete_ConsigneeIndependentOfDeclarant()
    {
        // Completing the consignee side must not touch the declarant columns.
        var h = NewHarness(nameof(Complete_ConsigneeIndependentOfDeclarant),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;

        AddCusDec(h.Db, 1, consigneeCompanyId: CompanyId, declarantCompanyId: 999,
            consigneeClaimedBy: MemberOwner);
        await h.Db.SaveChangesAsync();

        var r = await h.Service.CompleteAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.Ok, r.Status);

        var c = await h.Db.CusDecs.FindAsync(1L);
        Assert.Equal(MemberOwner, c!.ConsigneeCompletedById);
        Assert.Null(c.DeclarantCompletedById); // declarant side untouched
    }

    // ── Ownership isolation ───────────────────────────────────────────────────

    [Fact]
    public async Task OtherMember_GetsNotFound()
    {
        var h = NewHarness(nameof(OtherMember_GetsNotFound), new FakeCurrentUser(memberId: 2));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1);
        await h.Db.SaveChangesAsync();

        Assert.Equal(SubscriptionAccessCode.NotFound, (await h.Service.GetSubscriptionStripAsync(SubId)).Code);
        Assert.Equal(SubscriptionAccessCode.NotFound, (await h.Service.GetCusDecsAsync(SubId, Query())).Code);
        Assert.Equal(SubscriptionAccessCode.NotFound, (await h.Service.AcknowledgeAsync(SubId, 1)).Access);
    }

    [Fact]
    public async Task OtherMasterCompany_GetsNotFound()
    {
        var h = NewHarness(nameof(OtherMasterCompany_GetsNotFound), new FakeMasterUser(memberId: 99));
        await using var _ = h.Db;

        Assert.Equal(SubscriptionAccessCode.NotFound, (await h.Service.GetSubscriptionStripAsync(SubId)).Code);
    }

    [Fact]
    public async Task Master_CanAccess_OwnSubscription()
    {
        // A master's own subscription has MemberId == the master's id.
        var h = NewHarness(nameof(Master_CanAccess_OwnSubscription), new FakeMasterUser(MasterId),
            memberId: MasterId, masterId: MasterId);
        await using var _ = h.Db;
        AddCusDec(h.Db, 1);
        await h.Db.SaveChangesAsync();

        var strip = await h.Service.GetSubscriptionStripAsync(SubId);
        Assert.True(strip.IsOk);
        Assert.Equal(1, strip.Value!.Counts.TrackingNew);
    }

    [Fact]
    public async Task CusDecFromAnotherCompany_IsNotFoundOnAcknowledge()
    {
        var h = NewHarness(nameof(CusDecFromAnotherCompany_IsNotFoundOnAcknowledge), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantCompanyId: 777); // different company
        await h.Db.SaveChangesAsync();

        var ack = await h.Service.AcknowledgeAsync(SubId, 1);
        Assert.Equal(SubscriptionAccessCode.Ok, ack.Access);
        Assert.Equal(CusDecActionStatus.CusDecNotFound, ack.Status);
    }

    // ── Expired / OnlyForAcknowledgement ──────────────────────────────────────

    [Fact]
    public async Task ExpiredSubscription_IsDenied()
    {
        var h = NewHarness(nameof(ExpiredSubscription_IsDenied), new FakeCurrentUser(MemberOwner),
            expiry: new FakeBusinessClock(Now).StartOfToday().AddDays(-1));
        await using var _ = h.Db;

        Assert.Equal(SubscriptionAccessCode.Expired, (await h.Service.GetSubscriptionStripAsync(SubId)).Code);
    }

    [Fact]
    public async Task OnlyForAcknowledgement_Works_AndFlagsContainersBlocked()
    {
        var h = NewHarness(nameof(OnlyForAcknowledgement_Works_AndFlagsContainersBlocked),
            new FakeCurrentUser(MemberOwner),
            actionType: (int)SubscriptionActionTypeEnum.OnlyForAcknowledgement);
        await using var _ = h.Db;
        AddCusDec(h.Db, 1);
        await h.Db.SaveChangesAsync();

        var strip = (await h.Service.GetSubscriptionStripAsync(SubId)).Value!;
        Assert.True(strip.ContainersBlocked);
        Assert.True(strip.OnlyForAcknowledgement);

        var list = (await h.Service.GetCusDecsAsync(SubId, Query())).Value!;
        Assert.True(list.ContainersBlocked);

        var ack = await h.Service.AcknowledgeAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.Ok, ack.Status);
    }

    [Fact]
    public async Task ContainersBlocked_AcknowledgementOnly_EmptyList()
    {
        // The early-return path (total == 0) must still set ContainersBlocked = true.
        var h = NewHarness(nameof(ContainersBlocked_AcknowledgementOnly_EmptyList),
            new FakeCurrentUser(MemberOwner),
            actionType: (int)SubscriptionActionTypeEnum.OnlyForAcknowledgement);
        await using var _ = h.Db;

        var list = (await h.Service.GetCusDecsAsync(SubId, Query())).Value!;
        Assert.Equal(0, list.TotalCount);
        Assert.True(list.ContainersBlocked);
    }

    [Fact]
    public async Task ContainersBlocked_AcknowledgementOnly_PagePastEnd()
    {
        // The early-return path ((page-1)*pageSize >= total) must still set ContainersBlocked = true.
        var h = NewHarness(nameof(ContainersBlocked_AcknowledgementOnly_PagePastEnd),
            new FakeCurrentUser(MemberOwner),
            actionType: (int)SubscriptionActionTypeEnum.OnlyForAcknowledgement);
        await using var _ = h.Db;
        AddCusDec(h.Db, 1);
        await h.Db.SaveChangesAsync();

        var list = (await h.Service.GetCusDecsAsync(SubId, Query(page: 2, pageSize: 4))).Value!;
        Assert.Equal(1, list.TotalCount);
        Assert.Empty(list.Items);
        Assert.True(list.ContainersBlocked);
    }

    // ── Acknowledge ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Acknowledge_IsIdempotent_AndRecordsClaimer()
    {
        var h = NewHarness(nameof(Acknowledge_IsIdempotent_AndRecordsClaimer), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1);
        await h.Db.SaveChangesAsync();

        var first = await h.Service.AcknowledgeAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.Ok, first.Status);
        Assert.Equal("inprogress", first.Item!.State);
        Assert.Equal("Mandy Member", first.Item.ClaimedByName);

        var firstClaimAt = (await h.Db.CusDecs.FindAsync(1L))!.DeclarantClaimedDateTime;

        var second = await h.Service.AcknowledgeAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.Ok, second.Status);
        Assert.Equal(firstClaimAt, (await h.Db.CusDecs.FindAsync(1L))!.DeclarantClaimedDateTime);
    }

    [Fact]
    public async Task Acknowledge_RejectedWhenAlreadyDisclaimed()
    {
        var h = NewHarness(nameof(Acknowledge_RejectedWhenAlreadyDisclaimed), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantDisclaimedBy: MemberOwner);
        await h.Db.SaveChangesAsync();

        var ack = await h.Service.AcknowledgeAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.AlreadyDisclaimed, ack.Status);
        Assert.Null((await h.Db.CusDecs.FindAsync(1L))!.DeclarantClaimedById);
    }

    // ── Disclaim ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Disclaim_AllowedWhenUnclaimed_AndIdempotent()
    {
        // Disclaim an unclaimed (New) row → OK; re-disclaim is a no-op.
        var h = NewHarness(nameof(Disclaim_AllowedWhenUnclaimed_AndIdempotent), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1);
        await h.Db.SaveChangesAsync();

        var first = await h.Service.DisclaimAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.Ok, first.Status);
        Assert.Equal("disclaimed", first.Item!.Tab);
        var disclaimAt = (await h.Db.CusDecs.FindAsync(1L))!.DeclarantDisclaimedDateTime;

        var second = await h.Service.DisclaimAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.Ok, second.Status);
        Assert.Equal(disclaimAt, (await h.Db.CusDecs.FindAsync(1L))!.DeclarantDisclaimedDateTime);
        Assert.Equal(MemberOwner, (await h.Db.CusDecs.FindAsync(1L))!.DeclarantDisclaimedById);
    }

    [Fact]
    public async Task Disclaim_AfterClaim_Returns409AlreadyClaimed()
    {
        // Acknowledge first, then try to disclaim → AlreadyClaimed (409).
        var h = NewHarness(nameof(Disclaim_AfterClaim_Returns409AlreadyClaimed), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner);
        await h.Db.SaveChangesAsync();

        var r = await h.Service.DisclaimAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.AlreadyClaimed, r.Status);
        // Disclaimer column must NOT have been written.
        Assert.Null((await h.Db.CusDecs.FindAsync(1L))!.DeclarantDisclaimedById);
    }

    // ── Complete ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Complete_SetsColumnAndReturnsCompletedTab()
    {
        var h = NewHarness(nameof(Complete_SetsColumnAndReturnsCompletedTab), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner);
        await h.Db.SaveChangesAsync();

        var r = await h.Service.CompleteAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.Ok, r.Status);
        Assert.Equal("completed", r.Item!.Tab);
        Assert.Equal("Mandy Member", r.Item.CompletedByName);
        Assert.NotNull(r.Item.CompletedAt);

        // Verify persisted
        var c = await h.Db.CusDecs.FindAsync(1L);
        Assert.Equal(MemberOwner, c!.DeclarantCompletedById);
        Assert.NotNull(c.DeclarantCompletedDateTime);
    }

    [Fact]
    public async Task Complete_IsIdempotent()
    {
        var h = NewHarness(nameof(Complete_IsIdempotent), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner);
        await h.Db.SaveChangesAsync();

        await h.Service.CompleteAsync(SubId, 1);
        var firstCompletedAt = (await h.Db.CusDecs.FindAsync(1L))!.DeclarantCompletedDateTime;

        await h.Service.CompleteAsync(SubId, 1);
        Assert.Equal(firstCompletedAt, (await h.Db.CusDecs.FindAsync(1L))!.DeclarantCompletedDateTime);
    }

    [Fact]
    public async Task Complete_RejectedWhenNotClaimed()
    {
        var h = NewHarness(nameof(Complete_RejectedWhenNotClaimed), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1); // unclaimed
        await h.Db.SaveChangesAsync();

        var r = await h.Service.CompleteAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.NotClaimed, r.Status);
        Assert.Null((await h.Db.CusDecs.FindAsync(1L))!.DeclarantCompletedById);
    }

    [Fact]
    public async Task Complete_RejectedWhenDisclaimed()
    {
        var h = NewHarness(nameof(Complete_RejectedWhenDisclaimed), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantDisclaimedBy: MemberOwner);
        await h.Db.SaveChangesAsync();

        var r = await h.Service.CompleteAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.AlreadyDisclaimed, r.Status);
    }

    // ── Reopen ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reopen_ClearsCompletedFieldsAndReturnsToInProgress()
    {
        var h = NewHarness(nameof(Reopen_ClearsCompletedFieldsAndReturnsToInProgress), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner, declarantCompletedBy: MemberOwner);
        await h.Db.SaveChangesAsync();

        var r = await h.Service.ReopenAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.Ok, r.Status);
        Assert.Equal("tracking", r.Item!.Tab);
        Assert.Equal("inprogress", r.Item.State);
        Assert.Null(r.Item.CompletedAt);

        var c = await h.Db.CusDecs.FindAsync(1L);
        Assert.Null(c!.DeclarantCompletedById);
        Assert.Null(c.DeclarantCompletedDateTime);
    }

    [Fact]
    public async Task Reopen_IsNoopWhenNotCompleted()
    {
        var h = NewHarness(nameof(Reopen_IsNoopWhenNotCompleted), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner); // in-progress, not completed
        await h.Db.SaveChangesAsync();

        var r = await h.Service.ReopenAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.Ok, r.Status);
        Assert.Equal("inprogress", r.Item!.State); // still in-progress, unchanged
    }

    [Fact]
    public async Task Reopen_IsNoopWhenNew()
    {
        // Reopening an unclaimed (New) declaration is a no-op — CompletedDateTime is
        // null, so nothing is cleared and the row stays New.
        var h = NewHarness(nameof(Reopen_IsNoopWhenNew), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1); // unclaimed, New
        await h.Db.SaveChangesAsync();

        var r = await h.Service.ReopenAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.Ok, r.Status);
        Assert.Equal("new", r.Item!.State);
        Assert.Null((await h.Db.CusDecs.FindAsync(1L))!.DeclarantCompletedById);
    }

    [Fact]
    public async Task Reopen_ConsigneeIndependentOfDeclarant()
    {
        // Reopening the consignee side must not touch the declarant columns.
        var h = NewHarness(nameof(Reopen_ConsigneeIndependentOfDeclarant),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;

        AddCusDec(h.Db, 1,
            consigneeCompanyId: CompanyId, declarantCompanyId: 999,
            consigneeClaimedBy: MemberOwner, declarantClaimedBy: MemberOwner,
            consigneeCompletedBy: MemberOwner, declarantCompletedBy: MemberOwner);
        await h.Db.SaveChangesAsync();

        var r = await h.Service.ReopenAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.Ok, r.Status);

        var c = await h.Db.CusDecs.FindAsync(1L);
        Assert.Null(c!.ConsigneeCompletedById);        // consignee side cleared
        Assert.Null(c.ConsigneeCompletedDateTime);
        Assert.Equal(MemberOwner, c.DeclarantCompletedById);  // declarant side untouched
        Assert.NotNull(c.DeclarantCompletedDateTime);
    }

    // ── Paging & sort ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Paging_DefaultsToFour_AndReturnsTotal()
    {
        var h = NewHarness(nameof(Paging_DefaultsToFour_AndReturnsTotal), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        for (long i = 1; i <= 10; i++)
            AddCusDec(h.Db, i, cusDecDate: StatusBase.AddDays(i));
        await h.Db.SaveChangesAsync();

        var page1 = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking"))).Value!;
        Assert.Equal(10, page1.TotalCount);
        Assert.Equal(4, page1.PageSize);
        Assert.Equal(4, page1.Items.Count);

        var page3 = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", page: 3))).Value!;
        Assert.Equal(2, page3.Items.Count); // 10 total, page 3 of 4 → 2 rows
    }

    [Fact]
    public async Task Paging_IntMaxValuePage_Returns200Empty()
    {
        // (int.MaxValue - 1) * MaxPageSize overflows int; must return 200 empty, not 500.
        var h = NewHarness(nameof(Paging_IntMaxValuePage_Returns200Empty), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", page: int.MaxValue, pageSize: 100))).Value!;
        Assert.Empty(result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Paging_FirstOverflowPage_Returns200Empty()
    {
        // First page where (page-1) * MaxPageSize overflows int: (21474838-1)*100 = 2147483700 > int.MaxValue.
        var h = NewHarness(nameof(Paging_FirstOverflowPage_Returns200Empty), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1);
        await h.Db.SaveChangesAsync();

        var result = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", page: 21474838, pageSize: 100))).Value!;
        Assert.Empty(result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Sort_NewestFirstByDefault_OldestWhenRequested()
    {
        var h = NewHarness(nameof(Sort_NewestFirstByDefault_OldestWhenRequested), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, cusDecDate: StatusBase.AddDays(1));
        AddCusDec(h.Db, 2, cusDecDate: StatusBase.AddDays(5));
        AddCusDec(h.Db, 3, cusDecDate: StatusBase.AddDays(3));
        await h.Db.SaveChangesAsync();

        var newest = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking"))).Value!;
        Assert.Equal(new long[] { 2, 3, 1 }, newest.Items.Select(i => i.CusDecId).ToArray());

        var oldest = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", sort: "oldest"))).Value!;
        Assert.Equal(new long[] { 1, 3, 2 }, oldest.Items.Select(i => i.CusDecId).ToArray());
    }

    [Fact]
    public async Task Sort_Completed_ByCompletedTimestamp()
    {
        var h = NewHarness(nameof(Sort_Completed_ByCompletedTimestamp), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner, declarantCompletedBy: MemberOwner,
            completedAt: StatusBase.AddDays(1));
        AddCusDec(h.Db, 2, declarantClaimedBy: MemberOwner, declarantCompletedBy: MemberOwner,
            completedAt: StatusBase.AddDays(5));
        AddCusDec(h.Db, 3, declarantClaimedBy: MemberOwner, declarantCompletedBy: MemberOwner,
            completedAt: StatusBase.AddDays(3));
        await h.Db.SaveChangesAsync();

        var newest = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "completed"))).Value!;
        Assert.Equal(new long[] { 2, 3, 1 }, newest.Items.Select(i => i.CusDecId).ToArray());
    }

    // ── Status label: outlier CusDec with many rows ──────────────────────────

    [Fact]
    public async Task CurrentStatus_OutlierCusDecWithManyStatusRows_UsesLatest()
    {
        // A production declaration was observed with 34 375 status rows.
        // The service must return the correct current status without loading all rows.
        var h = NewHarness(nameof(CurrentStatus_OutlierCusDecWithManyStatusRows_UsesLatest),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner);

        // 500 older rows (generic content that is not an exit status)
        for (var i = 0; i < 500; i++)
            AddStatus(h.Db, 1, $"Interim-{i}", StatusBase.AddSeconds(i));

        // The latest visible row — this must be the current status label
        AddStatus(h.Db, 1, "Registered", StatusBase.AddSeconds(500));
        await h.Db.SaveChangesAsync();

        var list = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", state: "inprogress"))).Value!;
        var item = Assert.Single(list.Items);
        Assert.Equal("Registered", item.CurrentStatus);
        Assert.False(item.IsExited);
    }

    [Fact]
    public async Task Search_FiltersByReferenceAndName()
    {
        var h = NewHarness(nameof(Search_FiltersByReferenceAndName), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, number: "ABC-111");
        AddCusDec(h.Db, 2, number: "XYZ-222");
        await h.Db.SaveChangesAsync();

        var hit = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", search: "xyz"))).Value!;
        Assert.Equal(new long[] { 2 }, hit.Items.Select(i => i.CusDecId).ToArray());
    }

    // ── Colombo day boundary on date filter ──────────────────────────────────

    [Fact]
    public async Task DateFilter_BucketsByColomboDay()
    {
        var h = NewHarness(nameof(DateFilter_BucketsByColomboDay), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        // 18:00Z = 23:30 Colombo (Jun 1); 20:00Z = 01:30 Colombo next day (Jun 2).
        AddCusDec(h.Db, 1, cusDecDate: new DateTime(2026, 6, 1, 18, 0, 0, DateTimeKind.Utc)); // Jun 1 Colombo
        AddCusDec(h.Db, 2, cusDecDate: new DateTime(2026, 6, 1, 20, 0, 0, DateTimeKind.Utc)); // Jun 2 Colombo
        await h.Db.SaveChangesAsync();

        var jun2 = new DateOnly(2026, 6, 2);
        var result = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", from: jun2, to: jun2))).Value!;
        Assert.Equal(new long[] { 2 }, result.Items.Select(i => i.CusDecId).ToArray());
    }

    // ── Switcher ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Switcher_ReturnsOnlyUsableSubscriptions()
    {
        var h = NewHarness(nameof(Switcher_ReturnsOnlyUsableSubscriptions), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        var subs = await h.Service.GetUsableSubscriptionsAsync();
        var dto = Assert.Single(subs);
        Assert.Equal(SubId, dto.MemberSubscriptionId);
        Assert.Equal("Declarant", dto.SubscriptionType);
        Assert.Equal("Acme", dto.CompanyName);
        Assert.Equal("123456789", dto.CompanyTin);
    }

    // ── Master CusDec scope: own subscriptions only ──────────────────────────

    private const long MasterOwnSubId   = 200;
    private const long MemberUnderSubId = 201;
    private const long MemberUnderId    = 2;

    private static (CdnsDbContext Db, CusDecService AsMaster, CusDecService AsMember)
        SeedMasterWithMemberUnder(string name)
    {
        var db    = TestHelpers.CreateDb(name);
        var clock = new FakeBusinessClock(Now);
        var keys  = FakeKeyServiceFactory.Create();

        db.Companies.Add(new Company
        {
            CompanyId = CompanyId, CompanyName = "Acme", TinNumber = "123456789",
            IsDeleted = false, CompanyStatusEnum = (int)CompanyStatusEnum.Active,
        });
        db.Members.Add(new Member { MemberId = MasterId,     FirstName = "Marty", LastName = "Master",
            Email = "x@t", MemberType = MemberType.CompanyMasterMember });
        db.Members.Add(new Member { MemberId = MemberUnderId, FirstName = "Mandy", LastName = "Member",
            Email = "m@t", MemberType = MemberType.Member });

        MemberSubscription Sub(long id, long memberId) => new()
        {
            MemberSubscriptionId = id, MemberId = memberId, CompanyId = CompanyId,
            CompanyMasterMemberId = MasterId,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = clock.StartOfToday().AddDays(30), PaidAmount = 0m,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionName = "Gold", NoOfActiveDuration = 1,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionRequestId = 0, SubscriptionId = 0,
        };

        var masterSub = Sub(MasterOwnSubId, MasterId);
        var memberSub = Sub(MemberUnderSubId, MemberUnderId);
        db.MemberSubscriptions.AddRange(masterSub, memberSub);
        db.SaveChanges();
        masterSub.Key = keys.Generate(masterSub);
        memberSub.Key = keys.Generate(memberSub);
        db.SaveChanges();

        return (
            db,
            new CusDecService(db, new FakeMasterUser(MasterId),       clock, TestHelpers.SubscriptionScope(db, new FakeMasterUser(MasterId),       keys, clock)),
            new CusDecService(db, new FakeCurrentUser(MemberUnderId), clock, TestHelpers.SubscriptionScope(db, new FakeCurrentUser(MemberUnderId), keys, clock)));
    }

    [Fact]
    public async Task Master_Switcher_ShowsOnlyOwnSubscription_NotMembersUnderThem()
    {
        var (db, asMaster, _) = SeedMasterWithMemberUnder(
            nameof(Master_Switcher_ShowsOnlyOwnSubscription_NotMembersUnderThem));
        await using var _db = db;

        var subs = await asMaster.GetUsableSubscriptionsAsync();
        var dto = Assert.Single(subs);
        Assert.Equal(MasterOwnSubId, dto.MemberSubscriptionId);
    }

    [Fact]
    public async Task Master_UsingMembersSubscriptionId_GetsNotFound()
    {
        var (db, asMaster, _) = SeedMasterWithMemberUnder(nameof(Master_UsingMembersSubscriptionId_GetsNotFound));
        await using var _db = db;

        Assert.Equal(SubscriptionAccessCode.NotFound,
            (await asMaster.GetSubscriptionStripAsync(MemberUnderSubId)).Code);
        Assert.Equal(SubscriptionAccessCode.NotFound,
            (await asMaster.GetCusDecsAsync(MemberUnderSubId, Query())).Code);
        Assert.Equal(SubscriptionAccessCode.NotFound,
            (await asMaster.AcknowledgeAsync(MemberUnderSubId, 1)).Access);
        Assert.Equal(SubscriptionAccessCode.NotFound,
            (await asMaster.DisclaimAsync(MemberUnderSubId, 1)).Access);
        Assert.Equal(SubscriptionAccessCode.NotFound,
            (await asMaster.CompleteAsync(MemberUnderSubId, 1)).Access);
        Assert.Equal(SubscriptionAccessCode.NotFound,
            (await asMaster.ReopenAsync(MemberUnderSubId, 1)).Access);

        Assert.True((await asMaster.GetSubscriptionStripAsync(MasterOwnSubId)).IsOk);
    }

    [Fact]
    public async Task Member_StillSeesOwnSubscription()
    {
        var (db, _, asMember) = SeedMasterWithMemberUnder(nameof(Member_StillSeesOwnSubscription));
        await using var _db = db;

        var subs = await asMember.GetUsableSubscriptionsAsync();
        var dto = Assert.Single(subs);
        Assert.Equal(MemberUnderSubId, dto.MemberSubscriptionId);

        Assert.True((await asMember.GetSubscriptionStripAsync(MemberUnderSubId)).IsOk);
    }

    // ── Ownership isolation: Complete / Reopen on another company's CusDec ─────

    [Fact]
    public async Task Complete_AnotherCompany_ReturnsNotFound()
    {
        var h = NewHarness(nameof(Complete_AnotherCompany_ReturnsNotFound), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantCompanyId: 777); // different company
        await h.Db.SaveChangesAsync();

        var r = await h.Service.CompleteAsync(SubId, 1);
        Assert.Equal(SubscriptionAccessCode.Ok, r.Access);
        Assert.Equal(CusDecActionStatus.CusDecNotFound, r.Status);
    }

    [Fact]
    public async Task Reopen_AnotherCompany_ReturnsNotFound()
    {
        var h = NewHarness(nameof(Reopen_AnotherCompany_ReturnsNotFound), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantCompanyId: 777); // different company
        await h.Db.SaveChangesAsync();

        var r = await h.Service.ReopenAsync(SubId, 1);
        Assert.Equal(SubscriptionAccessCode.Ok, r.Access);
        Assert.Equal(CusDecActionStatus.CusDecNotFound, r.Status);
    }

    [Fact]
    public async Task Complete_Unclaimed_ConsigneeSide_ReturnsNotClaimed()
    {
        // Complete on an unclaimed CusDec returns NotClaimed (consignee side).
        // The declarant side is covered by the existing Complete_RejectedWhenNotClaimed test.
        var h = NewHarness(nameof(Complete_Unclaimed_ConsigneeSide_ReturnsNotClaimed),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantCompanyId: 999, consigneeCompanyId: CompanyId); // visible but unclaimed
        await h.Db.SaveChangesAsync();

        var r = await h.Service.CompleteAsync(SubId, 1);
        Assert.Equal(CusDecActionStatus.NotClaimed, r.Status);
    }

    // ── Hidden by default, visible for a specific office ─────────────────────

    [Fact]
    public async Task CurrentStatus_DefaultHiddenButOfficeVisible_IsNotPreFiltered()
    {
        // "PROC" has IsShow=false in the default mapping but IsShow=true in the COL
        // office-specific mapping. It must NOT be in DefaultHiddenAsycudaCodes, so it
        // is not filtered in SQL and resolves to the office-specific CDNS label.
        var h = NewHarness(nameof(CurrentStatus_DefaultHiddenButOfficeVisible_IsNotPreFiltered),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        h.Db.CusDecStatusMappings.Add(new CusDecStatusMapping
        {
            CusDecStatusMappingId = (int)++_nextId,
            AsycudaStatus = "PROC", CdnsStatus = "Processing (default)", IsShow = false, // default: hidden
        });
        h.Db.CusDecStatusMappings.Add(new CusDecStatusMapping
        {
            CusDecStatusMappingId = (int)++_nextId,
            AsycudaStatus = "PROC", CdnsStatus = "In Processing", OfficeCode = "COL", IsShow = true, // COL: visible
        });

        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner, office: "COL");
        AddStatus(h.Db, 1, "Registered", StatusBase);           // visible, older
        AddStatus(h.Db, 1, "PROC", StatusBase.AddDays(1));      // should survive SQL filter and resolve via office mapping
        await h.Db.SaveChangesAsync();

        var list = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", state: "inprogress"))).Value!;
        var item = Assert.Single(list.Items);
        Assert.Equal("In Processing", item.CurrentStatus);  // office label, not filtered out
    }

    // ── Outlier: top-K all hidden ─────────────────────────────────────────────

    [Fact]
    public async Task CurrentStatus_OutlierTopKAllHidden_FallsBackToOlderVisible()
    {
        // A CusDec whose newest 30 status rows are all default-hidden must still return
        // the older visible row as its current status. Before the fix, those 30 hidden
        // rows would saturate Take(StatusTopK=20) and the visible row would not be loaded.
        var h = NewHarness(nameof(CurrentStatus_OutlierTopKAllHidden_FallsBackToOlderVisible),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        const string HiddenCode = "INTERNAL_PROC";
        h.Db.CusDecStatusMappings.Add(new CusDecStatusMapping
        {
            CusDecStatusMappingId = (int)++_nextId,
            AsycudaStatus = HiddenCode, CdnsStatus = "Internal processing", IsShow = false,
        });

        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner);
        // Oldest row: visible
        AddStatus(h.Db, 1, "Registered", StatusBase);
        // 30 newer hidden rows
        for (var i = 1; i <= 30; i++)
            AddStatus(h.Db, 1, HiddenCode, StatusBase.AddSeconds(i));
        await h.Db.SaveChangesAsync();

        var list = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", state: "inprogress"))).Value!;
        var item = Assert.Single(list.Items);
        Assert.Equal("Registered", item.CurrentStatus);
        Assert.False(item.IsExited);
    }

    [Fact]
    public async Task IsExited_OutlierWithHiddenTotallyExited_IsExitedTrue()
    {
        // Same outlier scenario: 30 hidden rows fill the top of the status history, one of
        // which is literally "Totally exited" (also hidden). After the fix the current status
        // label is "Registered" (the only visible row). But LoadExitedIdsAsync queries ALL
        // status rows without any IsShow filter, so IsExited must still be true.
        var h = NewHarness(nameof(IsExited_OutlierWithHiddenTotallyExited_IsExitedTrue),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        const string HiddenCode = "INTERNAL_PROC";
        h.Db.CusDecStatusMappings.Add(new CusDecStatusMapping
        {
            CusDecStatusMappingId = (int)++_nextId,
            AsycudaStatus = HiddenCode, CdnsStatus = "Internal processing", IsShow = false,
        });
        h.Db.CusDecStatusMappings.Add(new CusDecStatusMapping
        {
            CusDecStatusMappingId = (int)++_nextId,
            AsycudaStatus = "Totally exited", CdnsStatus = "Exited", IsShow = false,
        });

        AddCusDec(h.Db, 1, declarantClaimedBy: MemberOwner);
        // Oldest visible row
        AddStatus(h.Db, 1, "Registered", StatusBase);
        // Hidden "Totally exited" row (newer)
        AddStatus(h.Db, 1, "Totally exited", StatusBase.AddSeconds(1));
        // 29 more hidden rows, even newer
        for (var i = 2; i <= 30; i++)
            AddStatus(h.Db, 1, HiddenCode, StatusBase.AddSeconds(i));
        await h.Db.SaveChangesAsync();

        var list = (await h.Service.GetCusDecsAsync(SubId, Query(tab: "tracking", state: "inprogress"))).Value!;
        var item = Assert.Single(list.Items);
        Assert.Equal("Registered", item.CurrentStatus); // hidden rows not visible as label
        Assert.True(item.IsExited);                     // raw exit code still triggers IsExited
    }

    // ── DeclarantTin filter ───────────────────────────────────────────────────

    [Fact]
    public async Task DeclarantTin_ExactMatch_ReturnsRow()
    {
        var h = NewHarness(nameof(DeclarantTin_ExactMatch_ReturnsRow), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantTin: "123456789");
        AddCusDec(h.Db, 2, declarantTin: "999999999");
        await h.Db.SaveChangesAsync();

        var r = (await h.Service.GetCusDecsAsync(SubId, Query(declarantTin: "123456789"))).Value!;
        Assert.Equal(new long[] { 1 }, r.Items.Select(i => i.CusDecId).ToArray());
    }

    [Fact]
    public async Task DeclarantTin_NoMatch_ReturnsEmpty()
    {
        var h = NewHarness(nameof(DeclarantTin_NoMatch_ReturnsEmpty), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantTin: "123456789");
        await h.Db.SaveChangesAsync();

        var r = (await h.Service.GetCusDecsAsync(SubId, Query(declarantTin: "000"))).Value!;
        Assert.Equal(0, r.TotalCount);
    }

    [Fact]
    public async Task DeclarantTin_PrefixMatch_UsesStartsWith()
    {
        var h = NewHarness(nameof(DeclarantTin_PrefixMatch_UsesStartsWith), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantTin: "12345678V");
        AddCusDec(h.Db, 2, declarantTin: "12399999V"); // different from position 4
        AddCusDec(h.Db, 3, declarantTin: "99999999V");
        await h.Db.SaveChangesAsync();

        var r = (await h.Service.GetCusDecsAsync(SubId, Query(declarantTin: "123"))).Value!;
        Assert.Equal(2, r.TotalCount);
        Assert.Contains(r.Items, i => i.CusDecId == 1);
        Assert.Contains(r.Items, i => i.CusDecId == 2);
    }

    [Fact]
    public async Task DeclarantTin_CaseInsensitive()
    {
        var h = NewHarness(nameof(DeclarantTin_CaseInsensitive), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantTin: "ABC123");
        await h.Db.SaveChangesAsync();

        var upper = (await h.Service.GetCusDecsAsync(SubId, Query(declarantTin: "ABC"))).Value!;
        var lower = (await h.Service.GetCusDecsAsync(SubId, Query(declarantTin: "abc"))).Value!;
        Assert.Equal(1, upper.TotalCount);
        Assert.Equal(1, lower.TotalCount);
    }

    [Fact]
    public async Task DeclarantTin_WildcardCharactersAreLiteral()
    {
        // A filter containing % or _ must match only TINs that literally contain those chars,
        // not act as SQL wildcards.
        var h = NewHarness(nameof(DeclarantTin_WildcardCharactersAreLiteral), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantTin: "12%45");   // contains literal %
        AddCusDec(h.Db, 2, declarantTin: "12_45");    // contains literal _
        AddCusDec(h.Db, 3, declarantTin: "12345");    // no special chars
        await h.Db.SaveChangesAsync();

        // "12%" should only match the row that literally starts with "12%"
        var pctResult = (await h.Service.GetCusDecsAsync(SubId, Query(declarantTin: "12%"))).Value!;
        Assert.Equal(new long[] { 1 }, pctResult.Items.Select(i => i.CusDecId).ToArray());

        // "12_" should only match the row that literally starts with "12_"
        var underResult = (await h.Service.GetCusDecsAsync(SubId, Query(declarantTin: "12_"))).Value!;
        Assert.Equal(new long[] { 2 }, underResult.Items.Select(i => i.CusDecId).ToArray());
    }

    [Fact]
    public async Task DeclarantTin_WhitespaceOnly_IsIgnored()
    {
        var h = NewHarness(nameof(DeclarantTin_WhitespaceOnly_IsIgnored), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantTin: "12345");
        await h.Db.SaveChangesAsync();

        var r = (await h.Service.GetCusDecsAsync(SubId, Query(declarantTin: "   "))).Value!;
        Assert.Equal(1, r.TotalCount); // filter not applied
    }

    [Fact]
    public async Task DeclarantTin_InternalWhitespace_IsStripped()
    {
        // "1 2 3" → "123" after stripping whitespace → starts-with "123"
        var h = NewHarness(nameof(DeclarantTin_InternalWhitespace_IsStripped), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantTin: "123456789");
        await h.Db.SaveChangesAsync();

        var r = (await h.Service.GetCusDecsAsync(SubId, Query(declarantTin: "1 2 3"))).Value!;
        Assert.Equal(1, r.TotalCount);
    }

    [Fact]
    public async Task DeclarantTin_TooLong_IsIgnored()
    {
        // A TIN longer than 20 chars after stripping is treated as no filter.
        var h = NewHarness(nameof(DeclarantTin_TooLong_IsIgnored), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantTin: "12345");
        await h.Db.SaveChangesAsync();

        var longTin = new string('1', 21);
        var r = (await h.Service.GetCusDecsAsync(SubId, Query(declarantTin: longTin))).Value!;
        Assert.Equal(1, r.TotalCount); // filter ignored → all visible rows returned
    }

    // ── ConsigneeTin filter ───────────────────────────────────────────────────

    [Fact]
    public async Task ConsigneeTin_Match_ReturnsRow_ConsigneeSubscription()
    {
        var h = NewHarness(nameof(ConsigneeTin_Match_ReturnsRow_ConsigneeSubscription),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantCompanyId: 999, consigneeCompanyId: CompanyId, consigneeTin: "AAABBB");
        AddCusDec(h.Db, 2, declarantCompanyId: 999, consigneeCompanyId: CompanyId, consigneeTin: "CCCDD");
        await h.Db.SaveChangesAsync();

        var r = (await h.Service.GetCusDecsAsync(SubId, Query(consigneeTin: "AAA"))).Value!;
        Assert.Equal(new long[] { 1 }, r.Items.Select(i => i.CusDecId).ToArray());
    }

    [Fact]
    public async Task ConsigneeTin_WildcardCharactersAreLiteral()
    {
        var h = NewHarness(nameof(ConsigneeTin_WildcardCharactersAreLiteral),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, declarantCompanyId: 999, consigneeCompanyId: CompanyId, consigneeTin: "AA%BB");
        AddCusDec(h.Db, 2, declarantCompanyId: 999, consigneeCompanyId: CompanyId, consigneeTin: "AABB");
        await h.Db.SaveChangesAsync();

        var r = (await h.Service.GetCusDecsAsync(SubId, Query(consigneeTin: "AA%"))).Value!;
        Assert.Equal(new long[] { 1 }, r.Items.Select(i => i.CusDecId).ToArray());
    }

    // ── CusDecNumber filter ───────────────────────────────────────────────────

    [Fact]
    public async Task CusDecNumber_ContainsMatch()
    {
        var h = NewHarness(nameof(CusDecNumber_ContainsMatch), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, number: "COL-2026-00001");
        AddCusDec(h.Db, 2, number: "GAL-2026-00001");
        await h.Db.SaveChangesAsync();

        var r = (await h.Service.GetCusDecsAsync(SubId, Query(cusDecNumber: "COL"))).Value!;
        Assert.Equal(new long[] { 1 }, r.Items.Select(i => i.CusDecId).ToArray());
    }

    [Fact]
    public async Task CusDecNumber_CaseInsensitive()
    {
        var h = NewHarness(nameof(CusDecNumber_CaseInsensitive), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, number: "COL-2026-00001");
        await h.Db.SaveChangesAsync();

        var lower = (await h.Service.GetCusDecsAsync(SubId, Query(cusDecNumber: "col-2026"))).Value!;
        Assert.Equal(1, lower.TotalCount);
    }

    [Fact]
    public async Task CusDecNumber_NoMatch_ReturnsEmpty()
    {
        var h = NewHarness(nameof(CusDecNumber_NoMatch_ReturnsEmpty), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, number: "COL-2026-00001");
        await h.Db.SaveChangesAsync();

        var r = (await h.Service.GetCusDecsAsync(SubId, Query(cusDecNumber: "NOMATCH"))).Value!;
        Assert.Equal(0, r.TotalCount);
    }

    [Fact]
    public async Task CusDecNumber_WildcardCharactersAreLiteral()
    {
        var h = NewHarness(nameof(CusDecNumber_WildcardCharactersAreLiteral), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, number: "ABC%DEF");
        AddCusDec(h.Db, 2, number: "ABCXDEF");
        await h.Db.SaveChangesAsync();

        // "%" in filter must match the literal % char, not any character sequence.
        var r = (await h.Service.GetCusDecsAsync(SubId, Query(cusDecNumber: "%"))).Value!;
        Assert.Equal(new long[] { 1 }, r.Items.Select(i => i.CusDecId).ToArray());
    }

    [Fact]
    public async Task CusDecNumber_WhitespaceOnly_IsIgnored()
    {
        var h = NewHarness(nameof(CusDecNumber_WhitespaceOnly_IsIgnored), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;
        AddCusDec(h.Db, 1, number: "COL-001");
        await h.Db.SaveChangesAsync();

        var r = (await h.Service.GetCusDecsAsync(SubId, Query(cusDecNumber: "  "))).Value!;
        Assert.Equal(1, r.TotalCount);
    }

    // ── Filters combine with other params ─────────────────────────────────────

    [Fact]
    public async Task Filters_CombineWithTabAndOffice()
    {
        var h = NewHarness(nameof(Filters_CombineWithTabAndOffice), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        // Both declarantTin AND office must match
        AddCusDec(h.Db, 1, declarantTin: "12345", office: "COL");                     // matches both
        AddCusDec(h.Db, 2, declarantTin: "12345", office: "GAL");                     // wrong office
        AddCusDec(h.Db, 3, declarantTin: "99999", office: "COL");                     // wrong TIN
        // CusDec 4: completed tab — should not appear in tracking results
        AddCusDec(h.Db, 4, declarantTin: "12345", office: "COL",
            declarantClaimedBy: MemberOwner, declarantCompletedBy: MemberOwner);
        await h.Db.SaveChangesAsync();

        var r = (await h.Service.GetCusDecsAsync(SubId,
            Query(tab: "tracking", office: "COL", declarantTin: "123"))).Value!;
        Assert.Equal(new long[] { 1 }, r.Items.Select(i => i.CusDecId).ToArray());
    }

    [Fact]
    public async Task Filters_CombineWithDateRange()
    {
        var h = NewHarness(nameof(Filters_CombineWithDateRange), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        var jan = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var jun = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        AddCusDec(h.Db, 1, declarantTin: "12345", cusDecDate: jan);
        AddCusDec(h.Db, 2, declarantTin: "12345", cusDecDate: jun);
        await h.Db.SaveChangesAsync();

        var r = (await h.Service.GetCusDecsAsync(SubId, Query(
            declarantTin: "123",
            from: new DateOnly(2026, 6, 1),
            to: new DateOnly(2026, 6, 30)))).Value!;
        Assert.Equal(new long[] { 2 }, r.Items.Select(i => i.CusDecId).ToArray());
    }

    [Fact]
    public async Task AllThreeFilters_AllMustMatch()
    {
        var h = NewHarness(nameof(AllThreeFilters_AllMustMatch), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        AddCusDec(h.Db, 1, number: "ABC-001", declarantTin: "12345", consigneeTin: "ZZZZ");  // all match
        AddCusDec(h.Db, 2, number: "XYZ-001", declarantTin: "12345", consigneeTin: "ZZZZ");  // number mismatch
        AddCusDec(h.Db, 3, number: "ABC-001", declarantTin: "99999", consigneeTin: "ZZZZ");  // dTin mismatch
        AddCusDec(h.Db, 4, number: "ABC-001", declarantTin: "12345", consigneeTin: "AAAA");  // cTin mismatch
        await h.Db.SaveChangesAsync();

        var r = (await h.Service.GetCusDecsAsync(SubId,
            Query(cusDecNumber: "ABC", declarantTin: "123", consigneeTin: "ZZZ"))).Value!;
        Assert.Equal(new long[] { 1 }, r.Items.Select(i => i.CusDecId).ToArray());
    }

    // ── Filters cannot expose another company's or other side's CusDecs ──────

    [Fact]
    public async Task DeclarantTin_CannotSeeOtherCompany()
    {
        // Both CusDecs share the same declarantTin but belong to different companies.
        // The subscription scopes to company 1, so company 2's row is never visible.
        var h = NewHarness(nameof(DeclarantTin_CannotSeeOtherCompany), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        AddCusDec(h.Db, 1, declarantCompanyId: CompanyId, declarantTin: "12345");   // our company
        AddCusDec(h.Db, 2, declarantCompanyId: 777,       declarantTin: "12345");   // different company
        await h.Db.SaveChangesAsync();

        var r = (await h.Service.GetCusDecsAsync(SubId, Query(declarantTin: "123"))).Value!;
        Assert.Equal(new long[] { 1 }, r.Items.Select(i => i.CusDecId).ToArray());
    }

    [Fact]
    public async Task DeclarantTin_CannotSeeConsigneeSideRow()
    {
        // A declarant subscription must not expose rows where our company is on the
        // consignee side, even if the consigneeTin prefix matches.
        var h = NewHarness(nameof(DeclarantTin_CannotSeeConsigneeSideRow), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        // This CusDec has our company as consignee, not declarant — invisible to a declarant sub.
        AddCusDec(h.Db, 1, declarantCompanyId: 999, consigneeCompanyId: CompanyId,
            declarantTin: "12345", consigneeTin: "12345");
        await h.Db.SaveChangesAsync();

        // A declarant subscription only sees rows where declarantCompanyId == CompanyId.
        var r = (await h.Service.GetCusDecsAsync(SubId, Query(declarantTin: "123"))).Value!;
        Assert.Equal(0, r.TotalCount);
    }

    [Fact]
    public async Task ConsigneeTin_CannotSeeDeclarantSideRow()
    {
        var h = NewHarness(nameof(ConsigneeTin_CannotSeeDeclarantSideRow),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Consignee);
        await using var _ = h.Db;

        // Our company is the declarant on this row — invisible to a consignee sub.
        AddCusDec(h.Db, 1, declarantCompanyId: CompanyId, consigneeCompanyId: 999,
            declarantTin: "12345", consigneeTin: "12345");
        await h.Db.SaveChangesAsync();

        var r = (await h.Service.GetCusDecsAsync(SubId, Query(consigneeTin: "123"))).Value!;
        Assert.Equal(0, r.TotalCount);
    }

    // ── GetOfficeCodesAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task Offices_DistinctSortedCodes()
    {
        var h = NewHarness(nameof(Offices_DistinctSortedCodes), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        AddCusDec(h.Db, 1, office: "GAL");
        AddCusDec(h.Db, 2, office: "COL");
        AddCusDec(h.Db, 3, office: "COL"); // duplicate COL
        AddCusDec(h.Db, 4, office: null);  // null excluded
        await h.Db.SaveChangesAsync();

        var r = await h.Service.GetOfficeCodesAsync(SubId);
        Assert.True(r.IsOk);
        Assert.Equal(["COL", "GAL"], r.Value!);
    }

    [Fact]
    public async Task Offices_ScopedToSubscription_ExcludesOtherCompany()
    {
        var h = NewHarness(nameof(Offices_ScopedToSubscription_ExcludesOtherCompany),
            new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        AddCusDec(h.Db, 1, declarantCompanyId: CompanyId, office: "COL");  // ours
        AddCusDec(h.Db, 2, declarantCompanyId: 777,       office: "GAL");  // other company
        await h.Db.SaveChangesAsync();

        var r = await h.Service.GetOfficeCodesAsync(SubId);
        Assert.Equal(["COL"], r.Value!);
    }

    [Fact]
    public async Task Offices_ScopedToSide_DeclarantSub_ConsigneeRowsExcluded()
    {
        var h = NewHarness(nameof(Offices_ScopedToSide_DeclarantSub_ConsigneeRowsExcluded),
            new FakeCurrentUser(MemberOwner), subType: (int)SubscriptionTypeEnum.Declarant);
        await using var _ = h.Db;

        AddCusDec(h.Db, 1, declarantCompanyId: CompanyId, consigneeCompanyId: 999, office: "COL");
        AddCusDec(h.Db, 2, declarantCompanyId: 999, consigneeCompanyId: CompanyId, office: "GAL");
        await h.Db.SaveChangesAsync();

        var r = await h.Service.GetOfficeCodesAsync(SubId);
        Assert.Equal(["COL"], r.Value!); // GAL is on a consignee-side row
    }

    [Fact]
    public async Task Offices_AllTabs_AcrossTabBoundary()
    {
        // Office codes are drawn from ALL tabs (tracking + completed + disclaimed).
        var h = NewHarness(nameof(Offices_AllTabs_AcrossTabBoundary), new FakeCurrentUser(MemberOwner));
        await using var _ = h.Db;

        AddCusDec(h.Db, 1, office: "COL");                                               // tracking
        AddCusDec(h.Db, 2, office: "GAL", declarantClaimedBy: MemberOwner,
            declarantCompletedBy: MemberOwner);                                           // completed
        AddCusDec(h.Db, 3, office: "KAN", declarantDisclaimedBy: MemberOwner);           // disclaimed
        await h.Db.SaveChangesAsync();

        var r = await h.Service.GetOfficeCodesAsync(SubId);
        Assert.Equal(["COL", "GAL", "KAN"], r.Value!);
    }

    [Fact]
    public async Task Offices_GateFail_NotFound()
    {
        var h = NewHarness(nameof(Offices_GateFail_NotFound), new FakeCurrentUser(memberId: 2));
        await using var _ = h.Db;

        var r = await h.Service.GetOfficeCodesAsync(SubId);
        Assert.False(r.IsOk);
        Assert.Equal(SubscriptionAccessCode.NotFound, r.Code);
    }

    [Fact]
    public async Task Offices_GateFail_Expired()
    {
        var h = NewHarness(nameof(Offices_GateFail_Expired), new FakeCurrentUser(MemberOwner),
            expiry: new FakeBusinessClock(Now).StartOfToday().AddDays(-1));
        await using var _ = h.Db;

        var r = await h.Service.GetOfficeCodesAsync(SubId);
        Assert.False(r.IsOk);
        Assert.Equal(SubscriptionAccessCode.Expired, r.Code);
    }
}

// ── Detail endpoint tests ─────────────────────────────────────────────────────

public class CusDecDetailTests
{
    private const int CompanyId = 1;
    private const long MemberOwner = 1;
    private const long MasterId = 10;
    private const long SubId = 100;
    private static readonly DateTimeOffset Now = new(2026, 11, 23, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateTime StatusBase = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static long _nextId = 2000;

    private static (CdnsDbContext Db, CusDecService Service) NewHarness(string name,
        int subType = (int)SubscriptionTypeEnum.Declarant)
    {
        var db    = TestHelpers.CreateDb(name);
        var clock = new FakeBusinessClock(Now);
        var keys  = FakeKeyServiceFactory.Create();
        db.Companies.Add(new Company { CompanyId = CompanyId, CompanyName = "Acme", TinNumber = "TIN1",
            IsDeleted = false, CompanyStatusEnum = (int)CompanyStatusEnum.Active });
        db.Members.Add(new Member { MemberId = MemberOwner, FirstName = "A", LastName = "B",
            Email = "a@b", MemberType = MemberType.Member });
        var sub = new MemberSubscription
        {
            MemberSubscriptionId = SubId, MemberId = MemberOwner, CompanyId = CompanyId,
            CompanyMasterMemberId = MasterId,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = clock.StartOfToday().AddDays(30), PaidAmount = 0m,
            SubscriptionTypeEnum = subType,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionName = "Gold", NoOfActiveDuration = 1,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionRequestId = 0, SubscriptionId = 0,
        };
        db.MemberSubscriptions.Add(sub);
        db.SaveChanges();
        sub.Key = keys.Generate(sub);
        db.SaveChanges();
        return (db, new CusDecService(db, new FakeCurrentUser(MemberOwner), clock,
            TestHelpers.SubscriptionScope(db, new FakeCurrentUser(MemberOwner), keys, clock)));
    }

    private static CusDec AddCusDec(CdnsDbContext db, long id,
        int? declarantCompanyId = CompanyId, int? consigneeCompanyId = null,
        long? declarantClaimedBy = null, long? consigneeClaimedBy = null,
        long? declarantDisclaimedBy = null, long? consigneeDisclaimedBy = null,
        long? declarantCompletedBy = null, string? office = "COL")
    {
        var c = new CusDec
        {
            CusDecId = id, CusDecNumber = $"CD-{id}",
            DeclarantCompanyId = declarantCompanyId, ConsigneeCompanyId = consigneeCompanyId,
            DeclarantCompanyName = "Decl Co", ConsigneeCompanyName = "Cons Co",
            DeclarantTinNumber = "DEC-TIN", ConsigneeTinNumber = "CON-TIN",
            DeclarantClaimedById = declarantClaimedBy,
            DeclarantClaimedDateTime = declarantClaimedBy is null ? null : StatusBase,
            DeclarantDisclaimedById = declarantDisclaimedBy,
            DeclarantDisclaimedDateTime = declarantDisclaimedBy is null ? null : StatusBase,
            ConsigneeClaimedById = consigneeClaimedBy,
            ConsigneeClaimedDateTime = consigneeClaimedBy is null ? null : StatusBase,
            ConsigneeDisclaimedById = consigneeDisclaimedBy,
            ConsigneeDisclaimedDateTime = consigneeDisclaimedBy is null ? null : StatusBase,
            DeclarantCompletedById = declarantCompletedBy,
            DeclarantCompletedDateTime = declarantCompletedBy is null ? null : StatusBase,
            CusDecDate = StatusBase, OfficeCode = office,
            GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = id,
            InvoiceValue = 1000m, BlNumber = "BL-001", Origin = "CN",
            Destination = "LK", CusDecType = "IM4", Channel = "Green",
            Incoterm = "CIF", Exporter = "ExportCo", NoOfItems = 2, NoOfContainers = 1,
        };
        db.CusDecs.Add(c);
        return c;
    }

    // ── Access gate ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Detail_UnknownSubscription_Returns404()
    {
        var (db, svc) = NewHarness(nameof(Detail_UnknownSubscription_Returns404));
        await using var _ = db;
        var r = await svc.GetCusDecDetailAsync(999, 1);
        Assert.Equal(SubscriptionAccessCode.NotFound, r.Code);
    }

    [Fact]
    public async Task Detail_OtherCompanyCusDec_Returns404()
    {
        var (db, svc) = NewHarness(nameof(Detail_OtherCompanyCusDec_Returns404));
        await using var _ = db;
        AddCusDec(db, 1, declarantCompanyId: 777);
        await db.SaveChangesAsync();
        var r = await svc.GetCusDecDetailAsync(SubId, 1);
        Assert.Equal(SubscriptionAccessCode.NotFound, r.Code);
    }

    [Fact]
    public async Task Detail_ExpiredSubscription_Returns403()
    {
        var db    = TestHelpers.CreateDb(nameof(Detail_ExpiredSubscription_Returns403));
        var clock = new FakeBusinessClock(Now);
        var keys  = FakeKeyServiceFactory.Create();
        db.Companies.Add(new Company { CompanyId = CompanyId, CompanyName = "Acme", TinNumber = "TIN1",
            IsDeleted = false, CompanyStatusEnum = (int)CompanyStatusEnum.Active });
        db.Members.Add(new Member { MemberId = MemberOwner, FirstName = "A", LastName = "B",
            Email = "a@b", MemberType = MemberType.Member });
        var sub = new MemberSubscription
        {
            MemberSubscriptionId = SubId, MemberId = MemberOwner, CompanyId = CompanyId,
            CompanyMasterMemberId = MasterId,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = clock.StartOfToday().AddDays(-1), PaidAmount = 0m,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionName = "Gold", NoOfActiveDuration = 1,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionRequestId = 0, SubscriptionId = 0,
        };
        db.MemberSubscriptions.Add(sub);
        db.SaveChanges();
        sub.Key = keys.Generate(sub);
        db.SaveChanges();
        await using var _db = db;
        var svc = new CusDecService(db, new FakeCurrentUser(MemberOwner), clock,
            TestHelpers.SubscriptionScope(db, new FakeCurrentUser(MemberOwner), keys, clock));
        AddCusDec(db, 1);
        await db.SaveChangesAsync();
        var r = await svc.GetCusDecDetailAsync(SubId, 1);
        Assert.Equal(SubscriptionAccessCode.Expired, r.Code);
    }

    [Fact]
    public async Task Detail_AcknowledgementOnly_Succeeds()
    {
        var db    = TestHelpers.CreateDb(nameof(Detail_AcknowledgementOnly_Succeeds));
        var clock = new FakeBusinessClock(Now);
        var keys  = FakeKeyServiceFactory.Create();
        db.Companies.Add(new Company { CompanyId = CompanyId, CompanyName = "Acme", TinNumber = "TIN1",
            IsDeleted = false, CompanyStatusEnum = (int)CompanyStatusEnum.Active });
        db.Members.Add(new Member { MemberId = MemberOwner, FirstName = "A", LastName = "B",
            Email = "a@b", MemberType = MemberType.Member });
        var sub = new MemberSubscription
        {
            MemberSubscriptionId = SubId, MemberId = MemberOwner, CompanyId = CompanyId,
            CompanyMasterMemberId = MasterId,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = clock.StartOfToday().AddDays(30), PaidAmount = 0m,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.OnlyForAcknowledgement,
            SubscriptionName = "Gold", NoOfActiveDuration = 1,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionRequestId = 0, SubscriptionId = 0,
        };
        db.MemberSubscriptions.Add(sub);
        db.SaveChanges();
        sub.Key = keys.Generate(sub);
        db.SaveChanges();
        await using var _db = db;
        var svc = new CusDecService(db, new FakeCurrentUser(MemberOwner), clock,
            TestHelpers.SubscriptionScope(db, new FakeCurrentUser(MemberOwner), keys, clock));
        AddCusDec(db, 1);
        await db.SaveChangesAsync();
        var r = await svc.GetCusDecDetailAsync(SubId, 1);
        Assert.True(r.IsOk);
    }

    // ── State/tab passthrough ─────────────────────────────────────────────────

    [Fact]
    public async Task Detail_ReturnsState_ForEachTab()
    {
        var (db, svc) = NewHarness(nameof(Detail_ReturnsState_ForEachTab));
        await using var _ = db;
        AddCusDec(db, 1);                                              // new
        AddCusDec(db, 2, declarantClaimedBy: MemberOwner);             // inprogress
        AddCusDec(db, 3, declarantClaimedBy: MemberOwner, declarantCompletedBy: MemberOwner); // completed
        AddCusDec(db, 4, declarantDisclaimedBy: MemberOwner);          // disclaimed
        await db.SaveChangesAsync();

        var d1 = (await svc.GetCusDecDetailAsync(SubId, 1)).Value!;
        Assert.Equal("tracking", d1.Tab); Assert.Equal("new", d1.State);

        var d2 = (await svc.GetCusDecDetailAsync(SubId, 2)).Value!;
        Assert.Equal("tracking", d2.Tab); Assert.Equal("inprogress", d2.State);

        var d3 = (await svc.GetCusDecDetailAsync(SubId, 3)).Value!;
        Assert.Equal("completed", d3.Tab); Assert.Null(d3.State);

        var d4 = (await svc.GetCusDecDetailAsync(SubId, 4)).Value!;
        Assert.Equal("disclaimed", d4.Tab); Assert.Null(d4.State);
    }

    // ── Other side's claim state never leaks ──────────────────────────────────

    [Fact]
    public async Task Detail_DeclarantSide_DoesNotExposeConsigneeClaimState()
    {
        var (db, svc) = NewHarness(nameof(Detail_DeclarantSide_DoesNotExposeConsigneeClaimState));
        await using var _ = db;
        // Declarant is new (unclaimed), consignee is in-progress
        AddCusDec(db, 1, declarantCompanyId: CompanyId, consigneeCompanyId: 999,
            declarantClaimedBy: null, consigneeClaimedBy: MemberOwner);
        await db.SaveChangesAsync();

        var d = (await svc.GetCusDecDetailAsync(SubId, 1)).Value!;
        Assert.Equal("new", d.State);  // declarant side is new
    }

    // ── Children: empty arrays ────────────────────────────────────────────────

    [Fact]
    public async Task Detail_NoChusdren_ReturnsEmptyArrays()
    {
        var (db, svc) = NewHarness(nameof(Detail_NoChusdren_ReturnsEmptyArrays));
        await using var _ = db;
        AddCusDec(db, 1);
        await db.SaveChangesAsync();

        var d = (await svc.GetCusDecDetailAsync(SubId, 1)).Value!;
        Assert.Empty(d.Items);
        Assert.False(d.ItemsTruncated);
        Assert.Empty(d.Taxes);
        Assert.Empty(d.Inspections);
    }

    // ── Items and taxes ───────────────────────────────────────────────────────

    [Fact]
    public async Task Detail_ReturnsItemsOrderedByItemNumber_AndTaxesGrouped()
    {
        var (db, svc) = NewHarness(nameof(Detail_ReturnsItemsOrderedByItemNumber_AndTaxesGrouped));
        await using var _ = db;
        AddCusDec(db, 1);
        db.CusDecItems.Add(new CusDecItem { CusDecItemId = ++_nextId, CusDecId = 1,
            CusDecItemNumber = 2, HsCode = "HS2", CusDecExternalSystemNumber = 20 });
        db.CusDecItems.Add(new CusDecItem { CusDecItemId = ++_nextId, CusDecId = 1,
            CusDecItemNumber = 1, HsCode = "HS1", CusDecExternalSystemNumber = 10 });
        db.CusDecTaxes.Add(new CusDecTax { CusDecTaxId = ++_nextId, CusDecId = 1,
            ItemNumber = 1, TaxCode = "VAT", TaxAmount = 100m, TaxRank = 1,
            CusDecExternalSystemNumber = 1 });
        db.CusDecTaxes.Add(new CusDecTax { CusDecTaxId = ++_nextId, CusDecId = 1,
            ItemNumber = 2, TaxCode = "NBT", TaxAmount = 50m, TaxRank = 1,
            CusDecExternalSystemNumber = 2 });
        await db.SaveChangesAsync();

        var d = (await svc.GetCusDecDetailAsync(SubId, 1)).Value!;
        Assert.Equal(2, d.Items.Count);
        Assert.Equal(1, d.Items[0].ItemNumber);  // ordered by item number
        Assert.Equal(2, d.Items[1].ItemNumber);
        Assert.Equal(2, d.Taxes.Count);
        Assert.Contains(d.Taxes, g => g.ItemNumber == 1 && g.Taxes[0].TaxCode == "VAT");
        Assert.Contains(d.Taxes, g => g.ItemNumber == 2 && g.Taxes[0].TaxCode == "NBT");
    }

    [Fact]
    public async Task Detail_ItemsTruncatedAt1000()
    {
        var (db, svc) = NewHarness(nameof(Detail_ItemsTruncatedAt1000));
        await using var _ = db;
        AddCusDec(db, 1);
        for (var i = 1; i <= 1001; i++)
            db.CusDecItems.Add(new CusDecItem { CusDecItemId = ++_nextId, CusDecId = 1,
                CusDecItemNumber = i, CusDecExternalSystemNumber = i });
        await db.SaveChangesAsync();

        var d = (await svc.GetCusDecDetailAsync(SubId, 1)).Value!;
        Assert.Equal(1000, d.Items.Count);
        Assert.True(d.ItemsTruncated);
    }

    [Fact]
    public async Task Detail_OtherCusDecsItems_NotIncluded()
    {
        var (db, svc) = NewHarness(nameof(Detail_OtherCusDecsItems_NotIncluded));
        await using var _ = db;
        AddCusDec(db, 1);
        AddCusDec(db, 2);
        db.CusDecItems.Add(new CusDecItem { CusDecItemId = ++_nextId, CusDecId = 1,
            CusDecItemNumber = 1, CusDecExternalSystemNumber = 1 });
        db.CusDecItems.Add(new CusDecItem { CusDecItemId = ++_nextId, CusDecId = 2,
            CusDecItemNumber = 1, CusDecExternalSystemNumber = 2 });
        await db.SaveChangesAsync();

        var d1 = (await svc.GetCusDecDetailAsync(SubId, 1)).Value!;
        Assert.Single(d1.Items);

        var d2 = (await svc.GetCusDecDetailAsync(SubId, 2)).Value!;
        Assert.Single(d2.Items);
    }

    // ── Inspections with sections ─────────────────────────────────────────────

    [Fact]
    public async Task Detail_ReturnsInspectionsWithSections()
    {
        var (db, svc) = NewHarness(nameof(Detail_ReturnsInspectionsWithSections));
        await using var _ = db;
        AddCusDec(db, 1);
        db.CusDecInspections.Add(new CusDecInspection { CusDecInspectionId = ++_nextId, CusDecId = 1,
            InspectionInstanceId = 10, KeyFrdNumber = 1, IssueCode = "IC1",
            CusDecExternalSystemNumber = 100 });
        db.CusDecInspectionSectionTabs.Add(new CusDecInspectionSectionTab
        {
            CusDecInspectionSectionTabId = ++_nextId, CusDecId = 1,
            InsTabExternalSystemNumber = 10, KeySecNumber = 1, SectionName = "Sec A",
            CusDecExternalSystemNumber = 100,
        });
        db.CusDecInspectionSectionTabs.Add(new CusDecInspectionSectionTab
        {
            CusDecInspectionSectionTabId = ++_nextId, CusDecId = 1,
            InsTabExternalSystemNumber = 10, KeySecNumber = 2, SectionName = "Sec B",
            CusDecExternalSystemNumber = 100,
        });
        await db.SaveChangesAsync();

        var d = (await svc.GetCusDecDetailAsync(SubId, 1)).Value!;
        var insp = Assert.Single(d.Inspections);
        Assert.Equal(10, insp.InspectionInstanceId);
        Assert.Equal(2, insp.Sections.Count);
        Assert.Equal(1, insp.Sections[0].KeySecNumber);
        Assert.Equal(2, insp.Sections[1].KeySecNumber);
    }
}

// ── Timeline endpoint tests ────────────────────────────────────────────────────

public class CusDecTimelineTests
{
    private const int CompanyId = 1;
    private const long MemberOwner = 1;
    private const long MasterId = 10;
    private const long SubId = 100;
    private static readonly DateTimeOffset Now = new(2026, 11, 23, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateTime StatusBase = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static long _nextId = 3000;

    private static (CdnsDbContext Db, CusDecService Service) NewHarness(string name,
        int subType = (int)SubscriptionTypeEnum.Declarant,
        int actionType = (int)SubscriptionActionTypeEnum.General)
    {
        var db    = TestHelpers.CreateDb(name);
        var clock = new FakeBusinessClock(Now);
        var keys  = FakeKeyServiceFactory.Create();
        db.Companies.Add(new Company { CompanyId = CompanyId, CompanyName = "Acme", TinNumber = "TIN1",
            IsDeleted = false, CompanyStatusEnum = (int)CompanyStatusEnum.Active });
        db.Members.Add(new Member { MemberId = MemberOwner, FirstName = "A", LastName = "B",
            Email = "a@b", MemberType = MemberType.Member });
        var sub = new MemberSubscription
        {
            MemberSubscriptionId = SubId, MemberId = MemberOwner, CompanyId = CompanyId,
            CompanyMasterMemberId = MasterId,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = clock.StartOfToday().AddDays(30), PaidAmount = 0m,
            SubscriptionTypeEnum = subType, SubscriptionActionTypeEnum = actionType,
            SubscriptionName = "Gold", NoOfActiveDuration = 1,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionRequestId = 0, SubscriptionId = 0,
        };
        db.MemberSubscriptions.Add(sub);
        db.SaveChanges();
        sub.Key = keys.Generate(sub);
        db.SaveChanges();
        return (db, new CusDecService(db, new FakeCurrentUser(MemberOwner), clock,
            TestHelpers.SubscriptionScope(db, new FakeCurrentUser(MemberOwner), keys, clock)));
    }

    private static CusDec AddCusDec(CdnsDbContext db, long id,
        long? declarantClaimedBy = null, long? declarantDisclaimedBy = null,
        long? declarantCompletedBy = null, string? office = "COL")
    {
        var c = new CusDec
        {
            CusDecId = id, CusDecNumber = $"CD-{id}",
            DeclarantCompanyId = CompanyId, ConsigneeCompanyId = null,
            DeclarantCompanyName = "Decl Co", ConsigneeCompanyName = "Cons Co",
            DeclarantClaimedById = declarantClaimedBy,
            DeclarantClaimedDateTime = declarantClaimedBy is null ? null : StatusBase,
            DeclarantDisclaimedById = declarantDisclaimedBy,
            DeclarantDisclaimedDateTime = declarantDisclaimedBy is null ? null : StatusBase,
            DeclarantCompletedById = declarantCompletedBy,
            DeclarantCompletedDateTime = declarantCompletedBy is null ? null : StatusBase,
            CusDecDate = StatusBase, OfficeCode = office,
            GlobalIdentity = Guid.NewGuid(), CusDecExternalSystemNumber = id,
        };
        db.CusDecs.Add(c);
        return c;
    }

    private static void AddStatus(CdnsDbContext db, long cusDecId, string status,
        DateTime at, bool inspect = false, string? mappedCdns = null, bool isHidden = false)
    {
        if (mappedCdns is not null)
            db.CusDecStatusMappings.Add(new CusDecStatusMapping
            {
                CusDecStatusMappingId = (int)++_nextId,
                AsycudaStatus = status, CdnsStatus = mappedCdns, IsShow = !isHidden,
            });

        if (inspect)
            db.CusDecInspectStatuses.Add(new CusDecInspectStatus
            {
                CusDecInspectStatusId = ++_nextId, CusDecId = cusDecId,
                Status = status, StatusDateTime = at, TableName = "inspect",
            });
        else
            db.CusDecStatuses.Add(new CusDecStatus
            {
                CusDecStatusId = ++_nextId, CusDecId = cusDecId,
                Status = status, StatusDateTime = at, TableName = "normal",
            });
    }

    // ── Access gate ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Timeline_UnknownSubscription_Returns404()
    {
        var (db, svc) = NewHarness(nameof(Timeline_UnknownSubscription_Returns404));
        await using var _ = db;
        var r = await svc.GetCusDecTimelineAsync(999, 1);
        Assert.Equal(SubscriptionAccessCode.NotFound, r.Code);
    }

    [Fact]
    public async Task Timeline_OtherCompanyCusDec_Returns404()
    {
        var (db, svc) = NewHarness(nameof(Timeline_OtherCompanyCusDec_Returns404));
        await using var _ = db;
        db.CusDecs.Add(new CusDec { CusDecId = 1, DeclarantCompanyId = 777, GlobalIdentity = Guid.NewGuid(),
            CusDecExternalSystemNumber = 1, DeclarantClaimedById = MemberOwner,
            DeclarantClaimedDateTime = StatusBase });
        await db.SaveChangesAsync();
        var r = await svc.GetCusDecTimelineAsync(SubId, 1);
        Assert.Equal(SubscriptionAccessCode.NotFound, r.Code);
    }

    [Fact]
    public async Task Timeline_NewCusDec_Returns404()
    {
        var (db, svc) = NewHarness(nameof(Timeline_NewCusDec_Returns404));
        await using var _ = db;
        AddCusDec(db, 1);  // unclaimed
        await db.SaveChangesAsync();
        var r = await svc.GetCusDecTimelineAsync(SubId, 1);
        Assert.Equal(SubscriptionAccessCode.NotFound, r.Code);
    }

    [Fact]
    public async Task Timeline_DisclaimedCusDec_Returns404()
    {
        var (db, svc) = NewHarness(nameof(Timeline_DisclaimedCusDec_Returns404));
        await using var _ = db;
        AddCusDec(db, 1, declarantDisclaimedBy: MemberOwner);
        await db.SaveChangesAsync();
        var r = await svc.GetCusDecTimelineAsync(SubId, 1);
        Assert.Equal(SubscriptionAccessCode.NotFound, r.Code);
    }

    [Fact]
    public async Task Timeline_AcknowledgementOnly_Succeeds()
    {
        var (db, svc) = NewHarness(nameof(Timeline_AcknowledgementOnly_Succeeds),
            actionType: (int)SubscriptionActionTypeEnum.OnlyForAcknowledgement);
        await using var _ = db;
        AddCusDec(db, 1, declarantClaimedBy: MemberOwner);
        await db.SaveChangesAsync();
        var r = await svc.GetCusDecTimelineAsync(SubId, 1);
        Assert.True(r.IsOk);
    }

    // ── Row ordering ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Timeline_DocumentAndInspectRows_MergedInDateOrder()
    {
        var (db, svc) = NewHarness(nameof(Timeline_DocumentAndInspectRows_MergedInDateOrder));
        await using var _ = db;
        AddCusDec(db, 1, declarantClaimedBy: MemberOwner);
        AddStatus(db, 1, "DocOld",   StatusBase);
        AddStatus(db, 1, "InspNew",  StatusBase.AddDays(2), inspect: true);
        AddStatus(db, 1, "DocMid",   StatusBase.AddDays(1));
        await db.SaveChangesAsync();

        var r = (await svc.GetCusDecTimelineAsync(SubId, 1)).Value!;
        Assert.Equal(3, r.Items.Count);
        Assert.Equal("InspNew", r.Items[0].Label);  // newest first
        Assert.Equal("DocMid",  r.Items[1].Label);
        Assert.Equal("DocOld",  r.Items[2].Label);
        Assert.Equal("inspection", r.Items[0].Kind);
        Assert.Equal("document",   r.Items[2].Kind);
    }

    [Fact]
    public async Task Timeline_NullDateRows_SortLast()
    {
        var (db, svc) = NewHarness(nameof(Timeline_NullDateRows_SortLast));
        await using var _ = db;
        AddCusDec(db, 1, declarantClaimedBy: MemberOwner);
        db.CusDecStatuses.Add(new CusDecStatus { CusDecStatusId = ++_nextId, CusDecId = 1,
            Status = "NullDate", StatusDateTime = null, TableName = "normal" });
        db.CusDecStatuses.Add(new CusDecStatus { CusDecStatusId = ++_nextId, CusDecId = 1,
            Status = "HasDate", StatusDateTime = StatusBase, TableName = "normal" });
        await db.SaveChangesAsync();

        var r = (await svc.GetCusDecTimelineAsync(SubId, 1)).Value!;
        Assert.Equal("HasDate",  r.Items[0].Label);
        Assert.Equal("NullDate", r.Items[1].Label);
    }

    // ── Visibility ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Timeline_HiddenDefaultStatus_Excluded()
    {
        var (db, svc) = NewHarness(nameof(Timeline_HiddenDefaultStatus_Excluded));
        await using var _ = db;
        db.CusDecStatusMappings.Add(new CusDecStatusMapping { CusDecStatusMappingId = (int)++_nextId,
            AsycudaStatus = "INTERNAL", CdnsStatus = "Internal", IsShow = false });
        AddCusDec(db, 1, declarantClaimedBy: MemberOwner);
        AddStatus(db, 1, "Registered", StatusBase);
        AddStatus(db, 1, "INTERNAL",   StatusBase.AddDays(1));  // hidden by mapping
        await db.SaveChangesAsync();

        var r = (await svc.GetCusDecTimelineAsync(SubId, 1)).Value!;
        Assert.Single(r.Items);
        Assert.Equal("Registered", r.Items[0].Label);
    }

    [Fact]
    public async Task Timeline_OfficeMappingWins_OverDefault()
    {
        var (db, svc) = NewHarness(nameof(Timeline_OfficeMappingWins_OverDefault));
        await using var _ = db;
        db.CusDecStatusMappings.Add(new CusDecStatusMapping { CusDecStatusMappingId = (int)++_nextId,
            AsycudaStatus = "PROC", CdnsStatus = "Processing (default)", IsShow = false });
        db.CusDecStatusMappings.Add(new CusDecStatusMapping { CusDecStatusMappingId = (int)++_nextId,
            AsycudaStatus = "PROC", CdnsStatus = "In Processing", OfficeCode = "COL", IsShow = true });
        AddCusDec(db, 1, declarantClaimedBy: MemberOwner, office: "COL");
        AddStatus(db, 1, "PROC", StatusBase);
        await db.SaveChangesAsync();

        var r = (await svc.GetCusDecTimelineAsync(SubId, 1)).Value!;
        Assert.Single(r.Items);
        Assert.Equal("In Processing", r.Items[0].Label);
    }

    [Fact]
    public async Task Timeline_EmptyOfficeMapping_AppliesAllOffices()
    {
        // A mapping with no OfficeCode is the default and applies to all offices.
        var (db, svc) = NewHarness(nameof(Timeline_EmptyOfficeMapping_AppliesAllOffices));
        await using var _ = db;
        db.CusDecStatusMappings.Add(new CusDecStatusMapping { CusDecStatusMappingId = (int)++_nextId,
            AsycudaStatus = "REG", CdnsStatus = "Registered (CDNS)", IsShow = true });
        AddCusDec(db, 1, declarantClaimedBy: MemberOwner, office: "GAL");
        AddStatus(db, 1, "REG", StatusBase);
        await db.SaveChangesAsync();

        var r = (await svc.GetCusDecTimelineAsync(SubId, 1)).Value!;
        Assert.Equal("Registered (CDNS)", r.Items[0].Label);
    }

    // ── 45 rows, newest 35 all hidden — older 10 all appear ──────────────────

    [Fact]
    public async Task Timeline_Outlier_45Rows_Newest35Hidden_Older10Visible()
    {
        // Defensive check that there is no top-K: if there were a Take(35), the visible
        // 10 would never be loaded. All 10 visible rows must appear in the response.
        var (db, svc) = NewHarness(nameof(Timeline_Outlier_45Rows_Newest35Hidden_Older10Visible));
        await using var _ = db;

        const string HiddenCode = "HIDDEN";
        db.CusDecStatusMappings.Add(new CusDecStatusMapping { CusDecStatusMappingId = (int)++_nextId,
            AsycudaStatus = HiddenCode, CdnsStatus = "Hidden", IsShow = false });

        AddCusDec(db, 1, declarantClaimedBy: MemberOwner);
        for (var i = 0; i < 10; i++)
            AddStatus(db, 1, $"Visible-{i}", StatusBase.AddSeconds(i));      // older visible
        for (var i = 0; i < 35; i++)
            AddStatus(db, 1, HiddenCode, StatusBase.AddSeconds(100 + i));    // newer hidden
        await db.SaveChangesAsync();

        var r = (await svc.GetCusDecTimelineAsync(SubId, 1)).Value!;
        Assert.Equal(10, r.Items.Count);
        Assert.False(r.Truncated);
        Assert.All(r.Items, row => Assert.StartsWith("Visible-", row.Label));
    }

    // ── Defensive cap ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Timeline_MoreThan500VisibleRows_TruncatesAt500()
    {
        var (db, svc) = NewHarness(nameof(Timeline_MoreThan500VisibleRows_TruncatesAt500));
        await using var _ = db;
        AddCusDec(db, 1, declarantClaimedBy: MemberOwner);
        for (var i = 0; i < 501; i++)
            AddStatus(db, 1, $"S-{i}", StatusBase.AddSeconds(i));
        await db.SaveChangesAsync();

        var r = (await svc.GetCusDecTimelineAsync(SubId, 1)).Value!;
        Assert.Equal(500, r.Items.Count);
        Assert.True(r.Truncated);
    }

    // ── State in response ─────────────────────────────────────────────────────

    [Fact]
    public async Task Timeline_InProgressState()
    {
        var (db, svc) = NewHarness(nameof(Timeline_InProgressState));
        await using var _ = db;
        AddCusDec(db, 1, declarantClaimedBy: MemberOwner);
        AddStatus(db, 1, "Registered", StatusBase);
        await db.SaveChangesAsync();

        var r = (await svc.GetCusDecTimelineAsync(SubId, 1)).Value!;
        Assert.Equal("inprogress", r.State);
        Assert.Equal("Registered", r.CurrentStatus);
    }

    [Fact]
    public async Task Timeline_CompletedState()
    {
        var (db, svc) = NewHarness(nameof(Timeline_CompletedState));
        await using var _ = db;
        AddCusDec(db, 1, declarantClaimedBy: MemberOwner, declarantCompletedBy: MemberOwner);
        AddStatus(db, 1, "Totally exited", StatusBase);
        await db.SaveChangesAsync();

        var r = (await svc.GetCusDecTimelineAsync(SubId, 1)).Value!;
        Assert.Equal("completed", r.State);
        // IsExited is only meaningful for in-progress rows, same as the list.
        Assert.False(r.IsExited);
        Assert.Equal("Totally exited", r.CurrentStatus);
    }
}

// ── SQL translation (Npgsql provider, no live DB needed) ─────────────────────

public class CusDecSqlTranslationTests
{
    private static CdnsDbContext CreateNpgsqlContext() =>
        new(new DbContextOptionsBuilder<CdnsDbContext>()
            .UseNpgsql("Host=placeholder;Database=placeholder")
            .Options);

    [Fact]
    public void DeclarantTin_StartsWith_TranslatesToLikeSql()
    {
        using var db = CreateNpgsqlContext();
        const string tin = "12345";
        var sql = db.CusDecs
            .Where(c => c.DeclarantTinNumber != null && c.DeclarantTinNumber.ToLower().StartsWith(tin.ToLower()))
            .ToQueryString();

        Assert.Contains("LOWER", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIKE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("client", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConsigneeTin_StartsWith_TranslatesToLikeSql()
    {
        using var db = CreateNpgsqlContext();
        const string tin = "ABC";
        var sql = db.CusDecs
            .Where(c => c.ConsigneeTinNumber != null && c.ConsigneeTinNumber.ToLower().StartsWith(tin.ToLower()))
            .ToQueryString();

        Assert.Contains("LOWER", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIKE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CusDecNumber_Contains_TranslatesToLikeSql()
    {
        using var db = CreateNpgsqlContext();
        const string num = "col-2026";
        var sql = db.CusDecs
            .Where(c => c.CusDecNumber != null && c.CusDecNumber.ToLower().Contains(num))
            .ToQueryString();

        Assert.Contains("LOWER", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIKE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CusDecInspectionSectionTab_MapsToCorrectTableName()
    {
        using var db = CreateNpgsqlContext();
        var tableName = db.Model
            .FindEntityType(typeof(CusDecInspectionSectionTab))!
            .GetTableName();
        Assert.Equal("cus_dec_inspection_section_tab", tableName);
    }

    [Fact]
    public void ContainerStatusMapping_MapsToCorrectTableName()
    {
        using var db = CreateNpgsqlContext();
        var tableName = db.Model
            .FindEntityType(typeof(ContainerStatusMapping))!
            .GetTableName();
        Assert.Equal("container_status_mappings", tableName);
    }

    [Fact]
    public void CusDecContainer_MapsToCorrectTableName()
    {
        using var db = CreateNpgsqlContext();
        var tableName = db.Model
            .FindEntityType(typeof(CusDecContainer))!
            .GetTableName();
        Assert.Equal("CusDecContainers", tableName);
    }

    [Fact]
    public void CusDecContainerStatus_MapsToCorrectTableName()
    {
        using var db = CreateNpgsqlContext();
        var tableName = db.Model
            .FindEntityType(typeof(CusDecContainerStatus))!
            .GetTableName();
        Assert.Equal("CusDecContainerStatuses", tableName);
    }

    [Fact]
    public void Offices_Distinct_OrderBy_TranslatesToSql()
    {
        using var db = CreateNpgsqlContext();
        var sql = db.CusDecs
            .Where(c => c.DeclarantCompanyId == 1)
            .Where(c => c.OfficeCode != null && c.OfficeCode != "")
            .Select(c => c.OfficeCode!)
            .Distinct()
            .OrderBy(code => code)
            .ToQueryString();

        Assert.Contains("DISTINCT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
    }
}
