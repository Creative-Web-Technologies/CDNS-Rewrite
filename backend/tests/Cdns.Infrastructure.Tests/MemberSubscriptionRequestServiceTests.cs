using Cdns.Application.Feature.Subscriptions;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Infrastructure.Services.Subscriptions;
using Cdns.Infrastructure.Tests;

namespace Cdns.Infrastructure.Tests;

public class MemberSubscriptionRequestServiceTests
{
    [Fact]
    public async Task GetRequest_ReturnsNull_ForAnotherMembersRequest()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetRequest_ReturnsNull_ForAnotherMembersRequest));
        var currentUser = new FakeCurrentUser(1); // authenticated as member 1

        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 10,
            MemberId = 2, // belongs to member 2
            CompanyId = 5,
            SubscriptionTypeEnum = 1,
            SubscriptionRequestStatusEnum = 0,
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionRequestService(db, currentUser);
        var result = await svc.GetRequestAsync(10);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateRequest_ReturnsDuplicatePending_WhenAlreadyPending()
    {
        await using var db = TestHelpers.CreateDb(nameof(CreateRequest_ReturnsDuplicatePending_WhenAlreadyPending));
        var currentUser = new FakeCurrentUser(1);

        // Add company
        db.Companies.Add(new Company { CompanyId = 10, CompanyName = "Test Corp", TinNumber = "123456789", IsDeleted = false });

        // Add an existing pending request
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1,
            MemberId = 1,
            CompanyId = 10,
            SubscriptionTypeEnum = 1,
            SubscriptionRequestStatusEnum = 0, // Pending
            RequestedDateTime = DateTimeOffset.UtcNow.AddDays(-1),
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionRequestService(db, currentUser);
        var result = await svc.CreateRequestAsync(10, 1, null);

        Assert.IsType<CreateRequestResult.DuplicatePending>(result);
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
            SubscriptionTypeEnum = 1,
            SubscriptionRequestStatusEnum = 1, // Approved
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionRequestService(db, currentUser);
        var result = await svc.CancelRequestAsync(20);

        Assert.IsType<CancelRequestResult.NotCancellable>(result);
    }

    [Fact]
    public async Task LookupCompany_UsesFirst9CharsOfTin()
    {
        await using var db = TestHelpers.CreateDb(nameof(LookupCompany_UsesFirst9CharsOfTin));
        var currentUser = new FakeCurrentUser(1);

        // Store company with 12-char TIN
        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Lookup Corp", TinNumber = "123456789ABC", IsDeleted = false });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionRequestService(db, currentUser);

        // Look up using first 9 chars only
        var result = await svc.LookupCompanyByTinAsync("123456789");

        Assert.NotNull(result);
        Assert.Equal("Lookup Corp", result!.Name);
        Assert.Equal(1, result.CompanyId);
    }

    [Fact]
    public async Task CancelRequest_ReturnsNotFound_ForAnotherMembersRequest()
    {
        await using var db = TestHelpers.CreateDb(nameof(CancelRequest_ReturnsNotFound_ForAnotherMembersRequest));
        var currentUser = new FakeCurrentUser(1); // authenticated as member 1

        // Request belongs to member 2
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 30,
            MemberId = 2, // different member
            CompanyId = 5,
            SubscriptionTypeEnum = 1,
            SubscriptionRequestStatusEnum = 0, // Pending — would be cancellable if it was owned
            RequestedDateTime = DateTimeOffset.UtcNow,
            IsDeleted = false,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionRequestService(db, currentUser);
        var result = await svc.CancelRequestAsync(30);

        Assert.IsType<CancelRequestResult.NotFound>(result);
    }
}
