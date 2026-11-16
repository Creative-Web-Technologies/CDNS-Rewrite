using Cdns.Application.Feature.Subscriptions;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Services.Subscriptions;

namespace Cdns.Infrastructure.Tests;

public class MasterSubscriptionServiceTests
{
    // ── D1: Ownership check on subscription ───────────────────────────────────

    [Fact]
    public async Task GetSubscription_ReturnsNull_WhenMasterIsNotOwner()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetSubscription_ReturnsNull_WhenMasterIsNotOwner));
        var master = new FakeMasterUser(10);
        var clock  = new FakeBusinessClock(DateTimeOffset.UtcNow);

        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.Companies.Add(new Company { CompanyId = 5, CompanyName = "Corp", TinNumber = "111111111", IsDeleted = false });
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1, MemberId = 1, CompanyId = 5,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Approved,
            RequestedDateTime = DateTimeOffset.UtcNow, IsDeleted = false,
        });
        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 100,
            MemberId = 1,
            SubscriptionRequestId = 1,
            SubscriptionId = 1,
            CompanyId = 5,
            CompanyMasterMemberId = 99, // different master
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            SubscriptionName = "Test",
            GlobalIdentity = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionService(db, master, clock);
        var result = await svc.GetSubscriptionAsync(100);
        Assert.Null(result);
    }

    [Fact]
    public async Task Deactivate_ReturnsNotFound_WhenMasterIsNotOwner()
    {
        await using var db = TestHelpers.CreateDb(nameof(Deactivate_ReturnsNotFound_WhenMasterIsNotOwner));
        var master = new FakeMasterUser(10);
        var clock  = new FakeBusinessClock(DateTimeOffset.UtcNow);

        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.Companies.Add(new Company { CompanyId = 5, CompanyName = "Corp", TinNumber = "111111111", IsDeleted = false });
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1, MemberId = 1, CompanyId = 5,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Approved,
            RequestedDateTime = DateTimeOffset.UtcNow, IsDeleted = false,
        });
        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 100,
            MemberId = 1,
            SubscriptionRequestId = 1,
            SubscriptionId = 1,
            CompanyId = 5,
            CompanyMasterMemberId = 99, // different master
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            SubscriptionName = "Test",
            GlobalIdentity = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionService(db, master, clock);
        var result = await svc.DeactivateAsync(100, "deactivating");
        Assert.IsType<DeactivateResult.NotFound>(result);
    }

    // ── D2: State guard — deactivate only while Active ────────────────────────

    [Fact]
    public async Task Deactivate_ReturnsNotActive_WhenAlreadyDeactivated()
    {
        await using var db = TestHelpers.CreateDb(nameof(Deactivate_ReturnsNotActive_WhenAlreadyDeactivated));
        var master = new FakeMasterUser(10);
        var clock  = new FakeBusinessClock(DateTimeOffset.UtcNow);

        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.Companies.Add(new Company { CompanyId = 5, CompanyName = "Corp", TinNumber = "111111111", IsDeleted = false });
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1, MemberId = 1, CompanyId = 5,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Approved,
            RequestedDateTime = DateTimeOffset.UtcNow, IsDeleted = false,
        });
        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 100,
            MemberId = 1,
            SubscriptionRequestId = 1,
            SubscriptionId = 1,
            CompanyId = 5,
            CompanyMasterMemberId = 10,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Deactivate, // already deactivated
            SubscriptionName = "Test",
            GlobalIdentity = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionService(db, master, clock);
        var result = await svc.DeactivateAsync(100, "deactivating again");
        Assert.IsType<DeactivateResult.NotActive>(result);
    }

    // ── L11: Deactivate happy path — writes history, no notification ──────────

    [Fact]
    public async Task Deactivate_WritesHistoryEntry_AndSetsStatusDeactivate_AndSendsNoNotification()
    {
        await using var db = TestHelpers.CreateDb(nameof(Deactivate_WritesHistoryEntry_AndSetsStatusDeactivate_AndSendsNoNotification));
        var master = new FakeMasterUser(10);
        var clock  = new FakeBusinessClock(DateTimeOffset.UtcNow);

        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.Companies.Add(new Company { CompanyId = 5, CompanyName = "Corp", TinNumber = "111111111", IsDeleted = false });
        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            SubscriptionRequestId = 1, MemberId = 1, CompanyId = 5,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Approved,
            RequestedDateTime = DateTimeOffset.UtcNow, IsDeleted = false,
        });
        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 100,
            MemberId = 1,
            SubscriptionRequestId = 1,
            SubscriptionId = 1,
            CompanyId = 5,
            CompanyMasterMemberId = 10,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            SubscriptionName = "Test",
            GlobalIdentity = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow,
            ExpiryDate = DateTimeOffset.UtcNow.AddMonths(1),
        });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionService(db, master, clock);
        var result = await svc.DeactivateAsync(100, "No longer needed");

        Assert.IsType<DeactivateResult.Success>(result);

        var subscription = db.MemberSubscriptions.Single(s => s.MemberSubscriptionId == 100);
        Assert.Equal((int)MemberSubscriptionStatusEnum.Deactivate, subscription.MemberSubscriptionStatusEnum);

        var history = db.MemberSubscriptionStatusHistories.Single();
        Assert.Equal(100L, history.MemberSubscriptionId);
        Assert.Equal((int)MemberSubscriptionStatusEnum.Deactivate, history.MemberSubscriptionStatusEnum);
        Assert.Equal("No longer needed", history.Comment);
        Assert.Equal(10L, history.MasterMemberId);

        // No notifications must be sent for deactivation
        Assert.Empty(db.MemberNotifications.ToList());
    }

    // ── L9: List only master's issued subscriptions ───────────────────────────

    [Fact]
    public async Task GetSubscriptions_ReturnsOnlyMastersSubscriptions()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetSubscriptions_ReturnsOnlyMastersSubscriptions));
        var master = new FakeMasterUser(10);
        var clock  = new FakeBusinessClock(DateTimeOffset.UtcNow);

        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.Members.Add(new Member { MemberId = 2, FirstName = "Bob",   Email = "bob@test.cdns",   MemberType = MemberType.Member });
        db.Companies.Add(new Company { CompanyId = 5, CompanyName = "Corp", TinNumber = "111111111", IsDeleted = false });

        db.SubscriptionRequests.Add(new SubscriptionRequest
            { SubscriptionRequestId = 1, MemberId = 1, CompanyId = 5,
              SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Approved,
              RequestedDateTime = DateTimeOffset.UtcNow, IsDeleted = false });
        db.SubscriptionRequests.Add(new SubscriptionRequest
            { SubscriptionRequestId = 2, MemberId = 2, CompanyId = 5,
              SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Approved,
              RequestedDateTime = DateTimeOffset.UtcNow, IsDeleted = false });

        // This master's subscription
        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 100, MemberId = 1, SubscriptionRequestId = 1,
            SubscriptionId = 1, CompanyId = 5, CompanyMasterMemberId = 10,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            SubscriptionName = "Test", GlobalIdentity = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow, ExpiryDate = DateTimeOffset.UtcNow.AddMonths(1),
        });

        // Another master's subscription (master 99)
        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 200, MemberId = 2, SubscriptionRequestId = 2,
            SubscriptionId = 1, CompanyId = 5, CompanyMasterMemberId = 99,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            SubscriptionName = "Test", GlobalIdentity = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow, ExpiryDate = DateTimeOffset.UtcNow.AddMonths(1),
        });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionService(db, master, clock);
        var result = await svc.GetSubscriptionsAsync(null, null, null, null, null, null, 1, 20);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(100L, result.Items[0].MemberSubscriptionId);
    }

    // ── L9: Both-dates-inside rule ────────────────────────────────────────────

    [Fact]
    public async Task GetSubscriptions_DateRange_RequiresBothStartAndExpiryInRange()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetSubscriptions_DateRange_RequiresBothStartAndExpiryInRange));
        var master = new FakeMasterUser(10);
        var clock  = new FakeBusinessClock(DateTimeOffset.UtcNow);
        var rangeFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var rangeTo   = new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero);

        db.Members.Add(new Member { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = MemberType.Member });
        db.Members.Add(new Member { MemberId = 2, FirstName = "Bob",   Email = "bob@test.cdns",   MemberType = MemberType.Member });
        db.Companies.Add(new Company { CompanyId = 5, CompanyName = "Corp", TinNumber = "111111111", IsDeleted = false });

        db.SubscriptionRequests.Add(new SubscriptionRequest
            { SubscriptionRequestId = 1, MemberId = 1, CompanyId = 5,
              SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Approved,
              RequestedDateTime = DateTimeOffset.UtcNow, IsDeleted = false });
        db.SubscriptionRequests.Add(new SubscriptionRequest
            { SubscriptionRequestId = 2, MemberId = 2, CompanyId = 5,
              SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Approved,
              RequestedDateTime = DateTimeOffset.UtcNow, IsDeleted = false });

        // Both inside range — should match
        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 100, MemberId = 1, SubscriptionRequestId = 1,
            SubscriptionId = 1, CompanyId = 5, CompanyMasterMemberId = 10,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            SubscriptionName = "In Range", GlobalIdentity = Guid.NewGuid(),
            StartDate = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            ExpiryDate = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
        });

        // StartDate inside but ExpiryDate outside — should NOT match
        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 200, MemberId = 2, SubscriptionRequestId = 2,
            SubscriptionId = 1, CompanyId = 5, CompanyMasterMemberId = 10,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            SubscriptionName = "Straddles", GlobalIdentity = Guid.NewGuid(),
            StartDate = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            ExpiryDate = new DateTimeOffset(2027, 3, 1, 0, 0, 0, TimeSpan.Zero),
        });
        await db.SaveChangesAsync();

        var svc = new MasterSubscriptionService(db, master, clock);
        var result = await svc.GetSubscriptionsAsync(null, null, null, null, rangeFrom, rangeTo, 1, 20);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(100L, result.Items[0].MemberSubscriptionId);
    }
}
