using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Infrastructure.Services.Subscriptions;
using Cdns.Infrastructure.Tests;

namespace Cdns.Infrastructure.Tests;

public class MemberSubscriptionServiceTests
{
    [Fact]
    public async Task GetActiveSubscriptions_ExcludesExpired()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_ExcludesExpired));
        var currentUser = new FakeCurrentUser(1);

        // Add a company
        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Acme Ltd", TinNumber = "123456789", IsDeleted = false });

        // Add an expired subscription
        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1,
            MemberId = 1,
            CompanyId = 1,
            MemberSubscriptionStatusEnum = 1, // Activate
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(-1), // expired
            SubscriptionTypeEnum = 1,
            SubscriptionName = "Basic",
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = 2,
            SubscriptionActionTypeEnum = 1,
            SubscriptionRequestId = 0,
            SubscriptionId = 0,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionService(db, currentUser);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveSubscriptions_ExcludesOtherMember()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_ExcludesOtherMember));
        var currentUser = new FakeCurrentUser(1); // authenticated as member 1

        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Acme Ltd", TinNumber = "123456789", IsDeleted = false });

        // Subscription belongs to member 2
        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1,
            MemberId = 2, // different member
            CompanyId = 1,
            MemberSubscriptionStatusEnum = 1,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(30),
            SubscriptionTypeEnum = 1,
            SubscriptionName = "Basic",
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = 2,
            SubscriptionActionTypeEnum = 1,
            SubscriptionRequestId = 0,
            SubscriptionId = 0,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionService(db, currentUser);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveSubscriptions_ExcludesInactiveStatus()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_ExcludesInactiveStatus));
        var currentUser = new FakeCurrentUser(1);

        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Acme Ltd", TinNumber = "123456789", IsDeleted = false });

        // Subscription is deactivated (status = 2)
        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1,
            MemberId = 1,
            CompanyId = 1,
            MemberSubscriptionStatusEnum = 2, // Deactivate
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(30),
            SubscriptionTypeEnum = 1,
            SubscriptionName = "Basic",
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = 2,
            SubscriptionActionTypeEnum = 1,
            SubscriptionRequestId = 0,
            SubscriptionId = 0,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionService(db, currentUser);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveSubscriptions_ExcludesSoftDeletedCompany()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_ExcludesSoftDeletedCompany));
        var currentUser = new FakeCurrentUser(1);

        // Company is soft-deleted
        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Deleted Corp", TinNumber = "987654321", IsDeleted = true });

        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1,
            MemberId = 1,
            CompanyId = 1,
            MemberSubscriptionStatusEnum = 1,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(30),
            SubscriptionTypeEnum = 1,
            SubscriptionName = "Basic",
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = 2,
            SubscriptionActionTypeEnum = 1,
            SubscriptionRequestId = 0,
            SubscriptionId = 0,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionService(db, currentUser);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Empty(result);
    }
}
