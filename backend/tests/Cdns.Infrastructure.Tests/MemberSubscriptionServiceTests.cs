using Cdns.Domain.Entities.Company;
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

        var svc = new MemberSubscriptionService(db, currentUser);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveSubscriptions_ExpiryDateToday_IsIncluded()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_ExpiryDateToday_IsIncluded));
        var currentUser = new FakeCurrentUser(1);

        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Acme Ltd", TinNumber = "123456789", IsDeleted = false });

        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1,
            MemberId = 1,
            CompanyId = 1,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero), // today 00:00:00 UTC
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionName = "Basic",
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionRequestId = 0,
            SubscriptionId = 0,
        });
        await db.SaveChangesAsync();

        var svc = new MemberSubscriptionService(db, currentUser);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task GetActiveSubscriptions_ExpiryDateYesterday_IsExcluded()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_ExpiryDateYesterday_IsExcluded));
        var currentUser = new FakeCurrentUser(1);

        db.Companies.Add(new Company { CompanyId = 1, CompanyName = "Acme Ltd", TinNumber = "123456789", IsDeleted = false });

        db.MemberSubscriptions.Add(new MemberSubscription
        {
            MemberSubscriptionId = 1,
            MemberId = 1,
            CompanyId = 1,
            MemberSubscriptionStatusEnum = (int)MemberSubscriptionStatusEnum.Activate,
            ExpiryDate = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero).AddSeconds(-1), // yesterday 23:59:59 UTC
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionName = "Basic",
            NoOfActiveDuration = 12,
            SubscriptionDurationTypeEnum = (int)SubscriptionDurationTypeEnum.Months,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
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
        var currentUser = new FakeCurrentUser(1);

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

        var svc = new MemberSubscriptionService(db, currentUser);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveSubscriptions_ExcludesSoftDeletedCompany()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_ExcludesSoftDeletedCompany));
        var currentUser = new FakeCurrentUser(1);

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

        var svc = new MemberSubscriptionService(db, currentUser);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveSubscriptions_ExpiredStatus_IsExcluded()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_ExpiredStatus_IsExcluded));
        var currentUser = new FakeCurrentUser(1);

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

        var svc = new MemberSubscriptionService(db, currentUser);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveSubscriptions_General_IsAcknowledgementOnly_False()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_General_IsAcknowledgementOnly_False));
        var currentUser = new FakeCurrentUser(1);

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

        var svc = new MemberSubscriptionService(db, currentUser);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Single(result);
        Assert.False(result[0].IsAcknowledgementOnly);
    }

    [Fact]
    public async Task GetActiveSubscriptions_OnlyForAcknowledgement_IsAcknowledgementOnly_True()
    {
        await using var db = TestHelpers.CreateDb(nameof(GetActiveSubscriptions_OnlyForAcknowledgement_IsAcknowledgementOnly_True));
        var currentUser = new FakeCurrentUser(1);

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

        var svc = new MemberSubscriptionService(db, currentUser);
        var result = await svc.GetActiveSubscriptionsAsync();

        Assert.Single(result);
        Assert.True(result[0].IsAcknowledgementOnly);
    }
}
