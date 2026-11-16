using Cdns.Domain.Entities.Company;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Infrastructure.Services.Subscriptions;
using Cdns.Infrastructure.Tests;

namespace Cdns.Infrastructure.Tests;

public class MemberSubscriptionServiceTests
{
    // ── Helper: Colombo midnight as a DateTimeOffset ──────────────────────────

    private static readonly TimeZoneInfo Colombo =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");

    private static DateTimeOffset ColombMidnight(int year, int month, int day)
    {
        var dt = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(dt, Colombo.GetUtcOffset(dt));
    }

    // ── Existing tests (now pass a FakeBusinessClock) ─────────────────────────

    [Fact]
    public async Task GetActiveSubscriptions_ExcludesExpired()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_ExcludesExpired));
        var currentUser = new FakeCurrentUser(1);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);

        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Acme Ltd", TinNumber = "123456789", IsDeleted = false });

        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1,
            MemberId = 1,
            CompanyId = 1,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(-1),
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionName = "Basic",
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionRequestId = 0,
            SubscriptionId = 0,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionService(db, currentUser, clock);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveSubscriptions_ExpiryDateToday_IsIncluded()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_ExpiryDateToday_IsIncluded));
        var currentUser = new FakeCurrentUser(1);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);

        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Acme Ltd", TinNumber = "123456789", IsDeleted = false });

        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1,
            MemberId = 1,
            CompanyId = 1,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = clock.StartOfToday(),
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionName = "Basic",
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionRequestId = 0,
            SubscriptionId = 0,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionService(db, currentUser, clock);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task GetActiveSubscriptions_ExpiryDateYesterday_IsExcluded()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_ExpiryDateYesterday_IsExcluded));
        var currentUser = new FakeCurrentUser(1);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);

        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Acme Ltd", TinNumber = "123456789", IsDeleted = false });

        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1,
            MemberId = 1,
            CompanyId = 1,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = clock.StartOfToday().AddDays(-1),
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionName = "Basic",
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionRequestId = 0,
            SubscriptionId = 0,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionService(db, currentUser, clock);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveSubscriptions_ExcludesOtherMember()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_ExcludesOtherMember));
        var currentUser = new FakeCurrentUser(1);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);

        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Acme Ltd", TinNumber = "123456789", IsDeleted = false });

        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1,
            MemberId = 2,
            CompanyId = 1,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(30),
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionName = "Basic",
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionRequestId = 0,
            SubscriptionId = 0,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionService(db, currentUser, clock);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveSubscriptions_ExcludesInactiveStatus()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_ExcludesInactiveStatus));
        var currentUser = new FakeCurrentUser(1);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);

        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Acme Ltd", TinNumber = "123456789", IsDeleted = false });

        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1,
            MemberId = 1,
            CompanyId = 1,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Deactivate,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(30),
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionName = "Basic",
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionRequestId = 0,
            SubscriptionId = 0,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionService(db, currentUser, clock);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveSubscriptions_ExcludesSoftDeletedCompany()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_ExcludesSoftDeletedCompany));
        var currentUser = new FakeCurrentUser(1);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);

        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Deleted Corp", TinNumber = "987654321", IsDeleted = true });

        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1,
            MemberId = 1,
            CompanyId = 1,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(30),
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionName = "Basic",
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionRequestId = 0,
            SubscriptionId = 0,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionService(db, currentUser, clock);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveSubscriptions_ExpiredStatus_IsExcluded()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_ExpiredStatus_IsExcluded));
        var currentUser = new FakeCurrentUser(1);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);

        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Acme Ltd", TinNumber = "123456789", IsDeleted = false });

        // Status = Expired(4) — excluded even if ExpiryDate is in the future
        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1,
            MemberId = 1,
            CompanyId = 1,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Expired,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(30),
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionName = "Basic",
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionRequestId = 0,
            SubscriptionId = 0,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionService(db, currentUser, clock);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveSubscriptions_General_IsAcknowledgementOnly_False()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_General_IsAcknowledgementOnly_False));
        var currentUser = new FakeCurrentUser(1);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);

        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Acme Ltd", TinNumber = "123456789", IsDeleted = false });

        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1,
            MemberId = 1,
            CompanyId = 1,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(30),
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionName = "General Plan",
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionRequestId = 0,
            SubscriptionId = 0,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionService(db, currentUser, clock);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Single(result);
        Assert.False(result[0].IsAcknowledgementOnly);
    }

    [Fact]
    public async Task GetActiveSubscriptions_OnlyForAcknowledgement_IsAcknowledgementOnly_True()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_OnlyForAcknowledgement_IsAcknowledgementOnly_True));
        var currentUser = new FakeCurrentUser(1);
        var clock = new FakeBusinessClock(DateTimeOffset.UtcNow);

        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Acme Ltd", TinNumber = "123456789", IsDeleted = false });

        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1,
            MemberId = 1,
            CompanyId = 1,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(30),
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Consignee,
            SubscriptionName = "Acknowledgement Only Plan",
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.OnlyForAcknowledgement,
            SubscriptionRequestId = 0,
            SubscriptionId = 0,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionService(db, currentUser, clock);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Single(result);
        Assert.True(result[0].IsAcknowledgementOnly);
    }

    // ── D4 Colombo clock boundary tests ──────────────────────────────────────
    //
    // The clock is fixed at 2026-11-16 02:00 Colombo (= 2026-11-15 20:30 UTC).
    // StartOfToday() returns 2026-11-16 00:00 +05:30.
    //
    // ExpiryDate = 2026-11-15 00:00 +05:30 (= 2026-11-14 18:30 UTC) → one day
    // before today → EXCLUDED from Member active list AND counted Expired on both
    // dashboards.
    //
    // ExpiryDate = 2026-11-16 00:00 +05:30 (= 2026-11-15 18:30 UTC) → equal to
    // today → INCLUDED/Active on Member list AND Active on both dashboards.

    [Fact]
    public async Task MemberList_At0200Colombo_ExpiredYesterdayColombo_IsExcluded()
    {
        // 02:00 Colombo on 16 Nov = 20:30 UTC on 15 Nov
        var colomboTwo = new DateTime(2026, 11, 16, 2, 0, 0, DateTimeKind.Unspecified);
        var clock = new FakeBusinessClock(new DateTimeOffset(colomboTwo, Colombo.GetUtcOffset(colomboTwo)));

        await using var db = TestHelpers.CreateDb(nameof(MemberList_At0200Colombo_ExpiredYesterdayColombo_IsExcluded));
        var currentUser = new FakeCurrentUser(1);
        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Corp", TinNumber = "111111111", IsDeleted = false });

        // Expired: ExpiryDate = 15 Nov 2026 Colombo midnight (the day before)
        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1, MemberId = 1, CompanyId = 1,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = ColombMidnight(2026, 11, 15),
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionName = "Test", NoOfActiveDuration = 1,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionRequestId = 0, SubscriptionId = 0,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionService(db, currentUser, clock);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task MemberList_At0200Colombo_ExpiringTodayColombо_IsIncluded()
    {
        // 02:00 Colombo on 16 Nov = 20:30 UTC on 15 Nov
        var colomboTwo = new DateTime(2026, 11, 16, 2, 0, 0, DateTimeKind.Unspecified);
        var clock = new FakeBusinessClock(new DateTimeOffset(colomboTwo, Colombo.GetUtcOffset(colomboTwo)));

        await using var db = TestHelpers.CreateDb(nameof(MemberList_At0200Colombo_ExpiringTodayColombо_IsIncluded));
        var currentUser = new FakeCurrentUser(1);
        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Corp", TinNumber = "111111111", IsDeleted = false });

        // Active: ExpiryDate = 16 Nov 2026 Colombo midnight (== today)
        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1, MemberId = 1, CompanyId = 1,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = ColombMidnight(2026, 11, 16),
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionName = "Test", NoOfActiveDuration = 1,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionRequestId = 0, SubscriptionId = 0,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionService(db, currentUser, clock);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Single(result);
    }

    // ── D4 Master-Member agreement tests ─────────────────────────────────────
    //
    // The same ExpiryDate values must behave identically on the Master
    // subscriptions list (MasterSubscriptionService.GetSubscriptionsAsync) so
    // both portals agree on what is Active / Expired.

    [Fact]
    public async Task MasterList_At0200Colombo_ExpiredYesterdayColombо_IsExpiredStatus()
    {
        var colomboTwo = new DateTime(2026, 11, 16, 2, 0, 0, DateTimeKind.Unspecified);
        var clock = new FakeBusinessClock(new DateTimeOffset(colomboTwo, Colombo.GetUtcOffset(colomboTwo)));

        await using var db = TestHelpers.CreateDb(nameof(MasterList_At0200Colombo_ExpiredYesterdayColombо_IsExpiredStatus));
        var master = new FakeMasterUser(10);

        db.Companies.Add(new Company { CompanyId = 5, CompanyName = "Corp", TinNumber = "111111111", IsDeleted = false });
        db.Members.Add(new Cdns.Domain.Entities.MemberSignupTables.Member
            { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = Cdns.Domain.Enums.MemberType.Member });
        db.SubscriptionRequests.Add(new Cdns.Domain.Entities.CompanySubscriptionManagement.SubscriptionRequest
        {
            SubscriptionRequestId = 1, MemberId = 1, CompanyId = 5,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Approved,
            RequestedDateTime = DateTimeOffset.UtcNow, IsDeleted = false,
        });
        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 100, MemberId = 1, CompanyId = 5,
            CompanyMasterMemberId = 10, SubscriptionRequestId = 1, SubscriptionId = 1,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = ColombMidnight(2026, 11, 15),
            SubscriptionName = "Test", GlobalIdentity = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow.AddDays(-30),
        });
        await db.SaveChangesAsync();

        var svc = new Cdns.Infrastructure.Services.Subscriptions.MasterSubscriptionService(db, master, clock);
        var result = await svc.GetSubscriptionsAsync(null, null, null, null, null, null, 1, 20);

        Assert.Equal(1, result.TotalCount);
        // IsActive must be false — expiry is before StartOfToday (Colombo midnight Nov 16)
        Assert.False(result.Items[0].IsActive);
    }

    [Fact]
    public async Task MasterList_At0200Colombo_ExpiringTodayColombо_IsActiveStatus()
    {
        var colomboTwo = new DateTime(2026, 11, 16, 2, 0, 0, DateTimeKind.Unspecified);
        var clock = new FakeBusinessClock(new DateTimeOffset(colomboTwo, Colombo.GetUtcOffset(colomboTwo)));

        await using var db = TestHelpers.CreateDb(nameof(MasterList_At0200Colombo_ExpiringTodayColombо_IsActiveStatus));
        var master = new FakeMasterUser(10);

        db.Companies.Add(new Company { CompanyId = 5, CompanyName = "Corp", TinNumber = "111111111", IsDeleted = false });
        db.Members.Add(new Cdns.Domain.Entities.MemberSignupTables.Member
            { MemberId = 1, FirstName = "Alice", Email = "alice@test.cdns", MemberType = Cdns.Domain.Enums.MemberType.Member });
        db.SubscriptionRequests.Add(new Cdns.Domain.Entities.CompanySubscriptionManagement.SubscriptionRequest
        {
            SubscriptionRequestId = 1, MemberId = 1, CompanyId = 5,
            SubscriptionRequestStatusEnum = (int)SubscriptionRequestStatusEnum.Approved,
            RequestedDateTime = DateTimeOffset.UtcNow, IsDeleted = false,
        });
        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 100, MemberId = 1, CompanyId = 5,
            CompanyMasterMemberId = 10, SubscriptionRequestId = 1, SubscriptionId = 1,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = ColombMidnight(2026, 11, 16),
            SubscriptionName = "Test", GlobalIdentity = Guid.NewGuid(),
            StartDate = DateTimeOffset.UtcNow.AddDays(-30),
        });
        await db.SaveChangesAsync();

        var svc = new Cdns.Infrastructure.Services.Subscriptions.MasterSubscriptionService(db, master, clock);
        var result = await svc.GetSubscriptionsAsync(null, null, null, null, null, null, 1, 20);

        Assert.Equal(1, result.TotalCount);
        // IsActive must be true — expiry equals StartOfToday (Colombo midnight Nov 16)
        Assert.True(result.Items[0].IsActive);
    }
}
