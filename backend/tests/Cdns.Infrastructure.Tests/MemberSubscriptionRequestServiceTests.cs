using Cdns.Application.Feature.Subscriptions;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Services.Subscriptions;
using Cdns.Infrastructure.Tests;

namespace Cdns.Infrastructure.Tests;

public class MemberSubscriptionRequestServiceTests
{
    [Fact]
    public async Task GetRequest_ReturnsNull_ForAnotherMembersRequest()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetRequest_ReturnsNull_ForAnotherMembersRequest));
        var currentUser = new FakeCurrentUser(1);

        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 10,
            MemberId = 2,
            CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionRequestService(db, currentUser);
        var result = await svc.GetRequestAsync(10);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRequests_ExcludesSoftDeletedRequests()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetRequests_ExcludesSoftDeletedRequests));
        var currentUser = new FakeCurrentUser(1);

        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            MemberId = 1,
            CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = true,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionRequestService(db, currentUser);
        var result = await svc.GetRequestsAsync(null, null, 1, 20);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task CreateRequest_ReturnsDuplicatePending_WhenAlreadyPending()
    {
        await using var db = TestHelpers.CreateDb(nameof(CreateRequest_ReturnsDuplicatePending_WhenAlreadyPending));
        var currentUser = new FakeCurrentUser(1);

        db.Companies.Add(new Company { CompanyId = 10, CompanyName = "Test Corp", TinNumber = "123456789", IsDeleted = false });

        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1,
            MemberId = 1,
            CompanyId = 10,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow.AddDays(-1),
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionRequestService(db, currentUser);
        var result = await svc.CreateRequestAsync(10, (int)SubscriptionTypeEnum.Declarant, null);

        Assert.IsType<CreateRequestResult.DuplicatePending>(result);
    }

    [Fact]
    public async Task CreateRequest_AllowsDifferentTypeWhenOnePending()
    {
        await using var db = TestHelpers.CreateDb(nameof(CreateRequest_AllowsDifferentTypeWhenOnePending));
        var currentUser = new FakeCurrentUser(1);

        db.Companies.Add(new Company { CompanyId = 10, CompanyName = "Test Corp", TinNumber = "123456789", IsDeleted = false });

        // Existing pending for Declarant
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            MemberId = 1,
            CompanyId = 10,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow.AddDays(-1),
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionRequestService(db, currentUser);
        // Different type (Consignee) must be allowed
        var result = await svc.CreateRequestAsync(10, (int)SubscriptionTypeEnum.Consignee, null);

        Assert.IsType<CreateRequestResult.Success>(result);
    }

    [Fact]
    public async Task CreateRequest_FansOutNotifications_ToAllCompanyMembers()
    {
        await using var db = TestHelpers.CreateDb(nameof(CreateRequest_FansOutNotifications_ToAllCompanyMembers));
        var currentUser = new FakeCurrentUser(99);

        db.Companies.Add(new Company { CompanyId = 10, CompanyName = "Test Corp", TinNumber = "123456789", IsDeleted = false });

        // Members must exist so the MemberNotification FK is satisfiable
        db.Members.AddRange(
            new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member },
            new Member { MemberId = 2, FirstName = "Bob",   Email = "bob@test.cdns",   MemberType = MemberType.Member });

        // Two members mapped to the company
        db.CompanyMemberMappings.AddRange(
            new CompanyMemberMapping { CompanyId = 10, MemberId = 1, IsDeleted = false },
            new CompanyMemberMapping { CompanyId = 10, MemberId = 2, IsDeleted = false });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionRequestService(db, currentUser);
        var result = await svc.CreateRequestAsync(10, (int)SubscriptionTypeEnum.Declarant, null);

        Assert.IsType<CreateRequestResult.Success>(result);

        var notifications = db.MemberNotifications.ToList();
        Assert.Equal(2, notifications.Count);
        Assert.Contains(notifications, n => n.MemberId == 1);
        Assert.Contains(notifications, n => n.MemberId == 2);
    }

    [Fact]
    public async Task CreateRequest_RequestNumber_IsSrFormat()
    {
        await using var db = TestHelpers.CreateDb(nameof(CreateRequest_RequestNumber_IsSrFormat));
        var currentUser = new FakeCurrentUser(1);

        db.Companies.Add(new Company { CompanyId = 10, CompanyName = "Test Corp", TinNumber = "123456789", IsDeleted = false });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionRequestService(db, currentUser);
        var result = await svc.CreateRequestAsync(10, (int)SubscriptionTypeEnum.Declarant, null);

        var success = Assert.IsType<CreateRequestResult.Success>(result);
        Assert.Matches(@"^SR-\d{6}$", success.RequestNumber);
        Assert.Equal($"SR-{success.RequestId:D6}", success.RequestNumber);
    }

    [Fact]
    public async Task CancelRequest_ReturnsNotCancellable_WhenApproved()
    {
        await using var db = TestHelpers.CreateDb(nameof(CancelRequest_ReturnsNotCancellable_WhenApproved));
        var currentUser = new FakeCurrentUser(1);

        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 20,
            MemberId = 1,
            CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Approved,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionRequestService(db, currentUser);
        var result = await svc.CancelRequestAsync(20);

        Assert.IsType<CancelRequestResult.NotCancellable>(result);
    }

    [Fact]
    public async Task CancelRequest_ReturnsNotFound_ForAnotherMembersRequest()
    {
        await using var db = TestHelpers.CreateDb(nameof(CancelRequest_ReturnsNotFound_ForAnotherMembersRequest));
        var currentUser = new FakeCurrentUser(1);

        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 30,
            MemberId = 2,
            CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionRequestService(db, currentUser);
        var result = await svc.CancelRequestAsync(30);

        Assert.IsType<CancelRequestResult.NotFound>(result);
    }

    [Fact]
    public async Task CancelRequest_Succeeds_WhenOwnerAndPending()
    {
        await using var db = TestHelpers.CreateDb(nameof(CancelRequest_Succeeds_WhenOwnerAndPending));
        var currentUser = new FakeCurrentUser(1);

        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 40,
            MemberId = 1,
            CompanyId = 5,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Pending,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionRequestService(db, currentUser);
        var result = await svc.CancelRequestAsync(40);

        Assert.IsType<CancelRequestResult.Success>(result);

        var req = db.SubscriptionRequests.Single(r => r.SubscriptionRequestId == 40);
        Assert.Equal((int)SubscriptionRequestStatusEnum.Cancelled, req.SubscriptionRequestStatusEnum);
    }

    [Fact]
    public async Task LookupCompany_UsesFirst9CharsOfTin()
    {
        await using var db = TestHelpers.CreateDb(nameof(LookupCompany_UsesFirst9CharsOfTin));
        var currentUser = new FakeCurrentUser(1);

        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Lookup Corp", TinNumber = "123456789ABC", IsDeleted = false });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionRequestService(db, currentUser);
        var result = await svc.LookupCompanyByTinAsync("123456789");

        Assert.NotNull(result);
        Assert.Equal("Lookup Corp", result!.Name);
        Assert.Equal(1, result.CompanyId);
    }
}
