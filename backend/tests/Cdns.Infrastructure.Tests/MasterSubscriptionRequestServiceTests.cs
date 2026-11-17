using Cdns.Application.Feature.Subscriptions;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Services.Subscriptions;

namespace Cdns.Infrastructure.Tests;

public class MasterSubscriptionRequestServiceTests
{
    // ── D1: Ownership checks ──────────────────────────────────────────────────

    [Fact]
    public async Task GetRequest_ReturnsNull_WhenMasterNotMappedToCompany()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetRequest_ReturnsNull_WhenMasterNotMappedToCompany));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keySvc = FakeKeyServiceFactory.Create();

        // Company 5 — master 10 is NOT mapped
        db.Companies.Add(new Company { CompanyId = 5, CompanyName = "Other Corp", TinNumber = "111111111", IsDeleted = false });
        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1,
            MemberId = 1,
            CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionRequestService(db, master, clock, keySvc);
        var result = await svc.GetRequestAsync(1);
        Assert.Null(result);
    }

    [Fact]
    public async Task RejectRequest_ReturnsNotFound_WhenMasterNotMappedToCompany()
    {
        await using var db = TestHelpers.CreateDb(nameof(RejectRequest_ReturnsNotFound_WhenMasterNotMappedToCompany));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keySvc = FakeKeyServiceFactory.Create();

        db.Companies.Add(new Company { CompanyId = 5, CompanyName = "Other Corp", TinNumber = "111111111", IsDeleted = false });
        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1,
            MemberId = 1,
            CompanyId = 5,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionRequestService(db, master, clock, keySvc);
        var result = await svc.RejectRequestAsync(1, "rejected");
        Assert.IsType<RejectRequestResult.NotFound>(result);
    }

    [Fact]
    public async Task GetPlansForRequest_ReturnsNull_WhenMasterNotMappedToCompany()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetPlansForRequest_ReturnsNull_WhenMasterNotMappedToCompany));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keySvc = FakeKeyServiceFactory.Create();

        db.Companies.Add(new Company { CompanyId = 5, CompanyName = "Other Corp", TinNumber = "111111111", IsDeleted = false });
        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1,
            MemberId = 1,
            CompanyId = 5,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionRequestService(db, master, clock, keySvc);
        var result = await svc.GetPlansForRequestAsync(1);
        Assert.Null(result);
    }

    [Fact]
    public async Task ApproveRequest_ReturnsNotFound_WhenMasterNotMappedToCompany()
    {
        await using var db = TestHelpers.CreateDb(nameof(ApproveRequest_ReturnsNotFound_WhenMasterNotMappedToCompany));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keySvc = FakeKeyServiceFactory.Create();

        db.Companies.Add(new Company { CompanyId = 5, CompanyName = "Other Corp", TinNumber = "111111111", IsDeleted = false });
        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1,
            MemberId = 1,
            CompanyId = 5,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionRequestService(db, master, clock, keySvc);
        var result = await svc.ApproveRequestAsync(1, 1);
        Assert.IsType<ApproveRequestResult.NotFound>(result);
    }

    // ── D2: State guards ──────────────────────────────────────────────────────

    [Fact]
    public async Task RejectRequest_ReturnsNotPending_WhenAlreadyApproved()
    {
        await using var db = TestHelpers.CreateDb(nameof(RejectRequest_ReturnsNotPending_WhenAlreadyApproved));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keySvc = FakeKeyServiceFactory.Create();

        SeedMasterMapping(db, masterId: 10, companyId: 5);
        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1,
            MemberId = 1,
            CompanyId = 5,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Approved,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionRequestService(db, master, clock, keySvc);
        var result = await svc.RejectRequestAsync(1, "too late");
        Assert.IsType<RejectRequestResult.NotPending>(result);
    }

    [Fact]
    public async Task ApproveRequest_ReturnsNotPending_WhenAlreadyApproved()
    {
        await using var db = TestHelpers.CreateDb(nameof(ApproveRequest_ReturnsNotPending_WhenAlreadyApproved));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keySvc = FakeKeyServiceFactory.Create();

        SeedMasterMapping(db, masterId: 10, companyId: 5);
        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1,
            MemberId = 1,
            CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Approved,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionRequestService(db, master, clock, keySvc);
        var result = await svc.ApproveRequestAsync(1, 1);
        Assert.IsType<ApproveRequestResult.NotPending>(result);
    }

    // ── D3: Approve is atomic ─────────────────────────────────────────────────

    [Fact]
    public async Task ApproveRequest_ReturnsNotFound_WhenPlanIdDoesNotExist_AndLeavesRequestUnchanged()
    {
        await using var db = TestHelpers.CreateDb(nameof(ApproveRequest_ReturnsNotFound_WhenPlanIdDoesNotExist_AndLeavesRequestUnchanged));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keySvc = FakeKeyServiceFactory.Create();

        SeedMasterMapping(db, masterId: 10, companyId: 5);
        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1,
            MemberId = 1,
            CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionRequestService(db, master, clock, keySvc);
        var result = await svc.ApproveRequestAsync(1, 999); // plan 999 doesn't exist
        Assert.IsType<ApproveRequestResult.PlanNotFound>(result);

        // Request must remain Pending, no subscription created, no notification
        var req = db.SubscriptionRequests.Single(r => r.SubscriptionRequestId == 1);
        Assert.Equal((int)SubscriptionRequestStatusEnum.Pending, req.SubscriptionRequestStatusEnum);
        Assert.Empty(db.MemberSubscriptions.ToList());
        Assert.Empty(db.MemberNotifications.ToList());
    }

    // ── L5: Plan filtering ────────────────────────────────────────────────────

    [Fact]
    public async Task GetPlansForRequest_ExcludesWrongType_InactivePlan_DeletedPlan()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetPlansForRequest_ExcludesWrongType_InactivePlan_DeletedPlan));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keySvc = FakeKeyServiceFactory.Create();

        SeedMasterMapping(db, masterId: 10, companyId: 5);
        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1,
            MemberId = 1,
            CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });

        // Should be returned: Active, Declarant
        db.Subscriptions.Add(new Subscription { SubscriptionId = 1, SubscriptionName = "Good Plan", Description = "ok",
            Amount = 100, NoOfActiveDuration = 1, SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionStatusEnum = (int)SubscriptionStatusEnum.Active, IsDeleted = false,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General });

        // Excluded: wrong type (Consignee)
        db.Subscriptions.Add(new Subscription { SubscriptionId = 2, SubscriptionName = "Consignee Plan", Description = "ok",
            Amount = 100, NoOfActiveDuration = 1, SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Consignee,
            SubscriptionStatusEnum = (int)SubscriptionStatusEnum.Active, IsDeleted = false,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General });

        // Excluded: Inactive
        db.Subscriptions.Add(new Subscription { SubscriptionId = 3, SubscriptionName = "Inactive Plan", Description = "ok",
            Amount = 100, NoOfActiveDuration = 1, SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionStatusEnum = (int)SubscriptionStatusEnum.Inactive, IsDeleted = false,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General });

        // Excluded: Deleted
        db.Subscriptions.Add(new Subscription { SubscriptionId = 4, SubscriptionName = "Deleted Plan", Description = "ok",
            Amount = 100, NoOfActiveDuration = 1, SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionStatusEnum = (int)SubscriptionStatusEnum.Active, IsDeleted = true,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General });

        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionRequestService(db, master, clock, keySvc);
        var plans = await svc.GetPlansForRequestAsync(1);
        Assert.NotNull(plans);
        Assert.Single(plans!);
        Assert.Equal(1, plans![0].SubscriptionId);
    }

    // ── L1: List excludes master's own requests ───────────────────────────────

    [Fact]
    public async Task GetRequests_ExcludesMastersOwnRequests()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetRequests_ExcludesMastersOwnRequests));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keySvc = FakeKeyServiceFactory.Create();

        SeedMasterMapping(db, masterId: 10, companyId: 5);
        db.Members.Add(new Member { MemberId = 1,  FirstName = "Alice",  Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.Members.Add(new Member { MemberId = 10, FirstName = "Master", Email = "master@test.cdns", MemberType = MemberType.CompanyMasterMember });

        // Member request — should be included
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1,
            MemberId = 1,
            CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });

        // Master's own request — must be excluded
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 2,
            MemberId = 10,
            CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Consignee,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionRequestService(db, master, clock, keySvc);
        var result = await svc.GetRequestsAsync(null, null, null, null, null, null, 1, 20);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Items[0].SubscriptionRequestId);
    }

    // ── L6: Full approve happy path ───────────────────────────────────────────

    [Fact]
    public async Task ApproveRequest_CreatesSubscriptionAndUpdatesRequest_AndSendsNotification()
    {
        await using var db = TestHelpers.CreateDb(nameof(ApproveRequest_CreatesSubscriptionAndUpdatesRequest_AndSendsNotification));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keySvc = FakeKeyServiceFactory.Create();

        SeedMasterMapping(db, masterId: 10, companyId: 5);
        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1,
            MemberId = 1,
            CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        db.Subscriptions.Add(new Subscription
        {
            SubscriptionId = 1,
            SubscriptionName = "Basic Plan",
            Description = "ok",
            Amount = 500m,
            NoOfActiveDuration = 1,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionStatusEnum = (int)SubscriptionStatusEnum.Active,
            IsDeleted = false,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
        });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionRequestService(db, master, clock, keySvc);
        var result = await svc.ApproveRequestAsync(1, 1);

        var success = Assert.IsType<ApproveRequestResult.Success>(result);
        Assert.Matches(@"^MS-\d{6}$", success.Number);

        var subscription = db.MemberSubscriptions.Single();
        Assert.Equal((int)MemberSubscriptionStatusEnum.Activate, subscription.MemberSubscriptionStatusEnum);
        Assert.Equal(500m, subscription.SubscriptionAmount);
        Assert.Equal(0m, subscription.PaidAmount);
        Assert.Null(subscription.PaidDate);
        Assert.Equal("", subscription.TransactionReferenceNumber);
        Assert.NotNull(subscription.Key);

        var req = db.SubscriptionRequests.Single(r => r.SubscriptionRequestId == 1);
        Assert.Equal((int)SubscriptionRequestStatusEnum.Approved, req.SubscriptionRequestStatusEnum);

        var notification = db.MemberNotifications.Single();
        Assert.Equal(1, notification.MemberId);
        Assert.Equal("Subscription Approved.", notification.Title);
        Assert.Equal((int)NotificationType.Success, (int)notification.NotificationType);
    }

    // ── L6a: Approve stores UTC offsets ──────────────────────────────────────

    [Fact]
    public async Task ApproveRequest_StoresUtcOffsets_ForAllDateFields()
    {
        // SubscriptionExpiry.ComputeExpiryDate always returns a +05:30 Colombo
        // offset. The service must store UTC (offset 0) for every date field so
        // Npgsql's 'timestamp with time zone' column is satisfied.
        await using var db = TestHelpers.CreateDb(nameof(ApproveRequest_StoresUtcOffsets_ForAllDateFields));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keySvc = FakeKeyServiceFactory.Create();

        SeedMasterMapping(db, masterId: 10, companyId: 5);
        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1,
            MemberId = 1,
            CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        db.Subscriptions.Add(new Subscription
        {
            SubscriptionId = 1,
            SubscriptionName = "Basic Plan",
            Description = "ok",
            Amount = 500m,
            NoOfActiveDuration = 1,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionStatusEnum = (int)SubscriptionStatusEnum.Active,
            IsDeleted = false,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
        });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionRequestService(db, master, clock, keySvc);
        await svc.ApproveRequestAsync(1, 1);

        var subscription = db.MemberSubscriptions.Single();
        Assert.Equal(TimeSpan.Zero, subscription.StartDate.Offset);
        Assert.Equal(TimeSpan.Zero, subscription.ExpiryDate!.Value.Offset);

        var req = db.SubscriptionRequests.Single(r => r.SubscriptionRequestId == 1);
        Assert.Equal(TimeSpan.Zero, req.RespondDateTime!.Value.Offset);

        var notification = db.MemberNotifications.Single();
        Assert.Equal(TimeSpan.Zero, notification.NotificationDateTime.Offset);
    }

    // ── L7: Self-create ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSelfRequest_CreatesPendingConsigneeRequest_UsingMastersOwnCompany()
    {
        await using var db = TestHelpers.CreateDb(nameof(CreateSelfRequest_CreatesPendingConsigneeRequest_UsingMastersOwnCompany));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keySvc = FakeKeyServiceFactory.Create();

        db.Companies.Add(new Company { CompanyId = 5, CompanyName = "My Corp", TinNumber = "123456789", IsDeleted = false });
        db.Members.Add(new Member { MemberId = 10, FirstName = "Master", Email = "master@test.cdns", MemberType = MemberType.CompanyMasterMember });
        db.CompanyMemberMappings.Add(new CompanyMemberMapping { CompanyId = 5, MemberId = 10, IsDeleted = false });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionRequestService(db, master, clock, keySvc);
        var result = await svc.CreateSelfRequestAsync();

        var success = Assert.IsType<CreateMasterRequestResult.Success>(result);
        Assert.Matches(@"^SR-\d{6}$", success.RequestNumber);

        var req = db.SubscriptionRequests.Single();
        Assert.Equal(10L, req.MemberId);    // Master's own MemberId
        Assert.Equal(5, req.CompanyId);     // Master's own company
        Assert.Equal((int)SubscriptionTypeEnum.Consignee, req.SubscriptionTypeEnum);
        Assert.Equal((int)SubscriptionRequestStatusEnum.Pending, req.SubscriptionRequestStatusEnum);
    }

    [Fact]
    public async Task CreateSelfRequest_ReturnsCompanyNotMapped_WhenMasterHasNoCompanies()
    {
        await using var db = TestHelpers.CreateDb(nameof(CreateSelfRequest_ReturnsCompanyNotMapped_WhenMasterHasNoCompanies));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keySvc = FakeKeyServiceFactory.Create();

        // No company mapping for master 10
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionRequestService(db, master, clock, keySvc);
        var result = await svc.CreateSelfRequestAsync();
        Assert.IsType<CreateMasterRequestResult.CompanyNotMapped>(result);
    }

    // ── L6: Type-mismatch and own-request rules ────────────────────────────────

    [Fact]
    public async Task ApproveRequest_ReturnsTypeMismatch_WhenMemberRequestAndPlanTypeDiffers()
    {
        await using var db = TestHelpers.CreateDb(nameof(ApproveRequest_ReturnsTypeMismatch_WhenMemberRequestAndPlanTypeDiffers));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keySvc = FakeKeyServiceFactory.Create();

        SeedMasterMapping(db, masterId: 10, companyId: 5);
        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        // Member requests Consignee
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1,
            MemberId = 1,
            CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Consignee,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        // Plan is Declarant — mismatched
        db.Subscriptions.Add(new Subscription { SubscriptionId = 1, SubscriptionName = "Declarant Plan", Description = "ok",
            Amount = 100, NoOfActiveDuration = 1, SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionStatusEnum = (int)SubscriptionStatusEnum.Active, IsDeleted = false,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionRequestService(db, master, clock, keySvc);
        var result = await svc.ApproveRequestAsync(1, 1);
        Assert.IsType<ApproveRequestResult.TypeMismatch>(result);
        // Request must remain Pending
        var req = db.SubscriptionRequests.Single(r => r.SubscriptionRequestId == 1);
        Assert.Equal((int)SubscriptionRequestStatusEnum.Pending, req.SubscriptionRequestStatusEnum);
    }

    [Fact]
    public async Task ApproveRequest_AllowsEitherType_ForOwnRequest_AndUpdatesRequestType()
    {
        await using var db = TestHelpers.CreateDb(nameof(ApproveRequest_AllowsEitherType_ForOwnRequest_AndUpdatesRequestType));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keySvc = FakeKeyServiceFactory.Create();

        SeedMasterMapping(db, masterId: 10, companyId: 5);
        db.Members.Add(new Member { MemberId = 10, FirstName = "Master", Email = "master@test.cdns", MemberType = MemberType.CompanyMasterMember });
        // Own request defaults to Consignee
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1,
            MemberId = 10,  // same as masterId — own request
            CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Consignee,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        // Plan is Declarant — different from request's Consignee default
        db.Subscriptions.Add(new Subscription { SubscriptionId = 1, SubscriptionName = "Declarant Plan", Description = "ok",
            Amount = 500, NoOfActiveDuration = 1, SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionStatusEnum = (int)SubscriptionStatusEnum.Active, IsDeleted = false,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionRequestService(db, master, clock, keySvc);
        var result = await svc.ApproveRequestAsync(1, 1);

        Assert.IsType<ApproveRequestResult.Success>(result);
        var req = db.SubscriptionRequests.Single(r => r.SubscriptionRequestId == 1);
        // Request type must be updated to match the plan
        Assert.Equal((int)SubscriptionTypeEnum.Declarant, req.SubscriptionTypeEnum);
        var sub = db.MemberSubscriptions.Single();
        Assert.Equal((int)SubscriptionTypeEnum.Declarant, sub.SubscriptionTypeEnum);
    }

    // ── L5: Plan list for own request returns all types ───────────────────────

    [Fact]
    public async Task GetPlansForRequest_ReturnsAllTypes_ForOwnRequest_ExcludesInactiveAndDeleted()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetPlansForRequest_ReturnsAllTypes_ForOwnRequest_ExcludesInactiveAndDeleted));
        var master = new FakeMasterUser(10);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var keySvc = FakeKeyServiceFactory.Create();

        SeedMasterMapping(db, masterId: 10, companyId: 5);
        db.Members.Add(new Member { MemberId = 10, FirstName = "Master", Email = "master@test.cdns", MemberType = MemberType.CompanyMasterMember });
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1,
            MemberId = 10,  // own request
            CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Consignee,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });

        // Both types returned
        db.Subscriptions.Add(new Subscription { SubscriptionId = 1, SubscriptionName = "Consignee Plan", Description = "ok",
            Amount = 100, NoOfActiveDuration = 1, SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Consignee,
            SubscriptionStatusEnum = (int)SubscriptionStatusEnum.Active, IsDeleted = false,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General });
        db.Subscriptions.Add(new Subscription { SubscriptionId = 2, SubscriptionName = "Declarant Plan", Description = "ok",
            Amount = 200, NoOfActiveDuration = 1, SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionStatusEnum = (int)SubscriptionStatusEnum.Active, IsDeleted = false,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General });

        // Excluded: Inactive
        db.Subscriptions.Add(new Subscription { SubscriptionId = 3, SubscriptionName = "Inactive Plan", Description = "ok",
            Amount = 100, NoOfActiveDuration = 1, SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Consignee,
            SubscriptionStatusEnum = (int)SubscriptionStatusEnum.Inactive, IsDeleted = false,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General });

        // Excluded: Deleted
        db.Subscriptions.Add(new Subscription { SubscriptionId = 4, SubscriptionName = "Deleted Plan", Description = "ok",
            Amount = 100, NoOfActiveDuration = 1, SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Consignee,
            SubscriptionStatusEnum = (int)SubscriptionStatusEnum.Active, IsDeleted = true,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionRequestService(db, master, clock, keySvc);
        var plans = await svc.GetPlansForRequestAsync(1);
        Assert.NotNull(plans);
        Assert.Equal(2, plans!.Count);
        Assert.Contains(plans, p => p.SubscriptionType == "Consignee");
        Assert.Contains(plans, p => p.SubscriptionType == "Declarant");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SeedMasterMapping(
        Cdns.Infrastructure.Persistence.CdnsDbContext db, long masterId, int companyId)
    {
        db.Companies.Add(new Company
            { CompanyId = companyId, CompanyName = "Corp", TinNumber = "123456789", IsDeleted = false });
        db.CompanyMemberMappings.Add(new CompanyMemberMapping
            { CompanyId = companyId, MemberId = masterId, IsDeleted = false });
    }
}
