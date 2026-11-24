using Cdns.Application.Feature.Subscriptions;
using Cdns.Application.Feature.Dashboard;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Services.Subscriptions;
using Microsoft.Extensions.Configuration;

namespace Cdns.Application.Tests;

/// <summary>
/// D4 expiry edge cases and dashboard "expiring today = Active not Expired" rule.
/// L8/D6 HMAC generate/verify round-trip.
/// </summary>
public class SubscriptionExpiryTests
{
    // Asia/Colombo is UTC+5:30. All expiry dates returned are Colombo midnight.
    private static readonly TimeZoneInfo Colombo =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Fact]
    public void ComputeExpiry_31Jan_Plus1Month_Gives_28Feb()
    {
        // 31 Jan 2026, non-leap year → should land on 28 Feb 2026
        var start = ColombMidnight(2026, 1, 31);
        var expiry = SubscriptionExpiry.ComputeExpiryDate(
            start, noOfActiveDuration: 1,
            durationTypeEnum: (int)SubscriptionDurationTypeEnum.Months);

        var colomboExpiry = TimeZoneInfo.ConvertTime(expiry, Colombo);
        Assert.Equal(2026, colomboExpiry.Year);
        Assert.Equal(2,    colomboExpiry.Month);
        Assert.Equal(28,   colomboExpiry.Day);
        Assert.Equal(0,    colomboExpiry.Hour);
        Assert.Equal(0,    colomboExpiry.Minute);
    }

    [Fact]
    public void ComputeExpiry_29Feb2024_Plus1Year_Gives_28Feb2025()
    {
        // 2024 is a leap year; 2025 is not — lands on 28 Feb 2025
        var start = ColombMidnight(2024, 2, 29);
        var expiry = SubscriptionExpiry.ComputeExpiryDate(
            start, noOfActiveDuration: 1,
            durationTypeEnum: (int)SubscriptionDurationTypeEnum.Years);

        var colomboExpiry = TimeZoneInfo.ConvertTime(expiry, Colombo);
        Assert.Equal(2025, colomboExpiry.Year);
        Assert.Equal(2,    colomboExpiry.Month);
        Assert.Equal(28,   colomboExpiry.Day);
    }

    [Fact]
    public void ComputeExpiry_StartAt02hColombo_UsesColomboDayNotUtcDay()
    {
        // 02:00 Colombo = 20:30 UTC of the previous calendar day.
        // Colombo date is 15 Nov 2026; expiry + 30 days should land on 15 Dec 2026.
        var colomboDay = new DateTime(2026, 11, 15, 2, 0, 0, DateTimeKind.Unspecified);
        var utcOffset = Colombo.GetUtcOffset(colomboDay);
        var start = new DateTimeOffset(colomboDay, utcOffset);

        var expiry = SubscriptionExpiry.ComputeExpiryDate(
            start, noOfActiveDuration: 30,
            durationTypeEnum: (int)SubscriptionDurationTypeEnum.Days);

        var colomboExpiry = TimeZoneInfo.ConvertTime(expiry, Colombo);
        Assert.Equal(2026, colomboExpiry.Year);
        Assert.Equal(12,   colomboExpiry.Month);
        Assert.Equal(15,   colomboExpiry.Day);
        Assert.Equal(0,    colomboExpiry.Hour);
    }

    [Fact]
    public void ComputeExpiry_ReturnsColombMidnight_NotUtcMidnight()
    {
        var start = ColombMidnight(2026, 11, 1);
        var expiry = SubscriptionExpiry.ComputeExpiryDate(
            start, noOfActiveDuration: 10,
            durationTypeEnum: (int)SubscriptionDurationTypeEnum.Days);

        var colomboExpiry = TimeZoneInfo.ConvertTime(expiry, Colombo);
        Assert.Equal(2026, colomboExpiry.Year);
        Assert.Equal(11,   colomboExpiry.Month);
        Assert.Equal(11,   colomboExpiry.Day);
        Assert.Equal(0,    colomboExpiry.Hour);
        Assert.Equal(0,    colomboExpiry.Minute);
        Assert.Equal(0,    colomboExpiry.Second);
        Assert.Equal(TimeSpan.FromHours(5.5), colomboExpiry.Offset);
    }

    // ── D4: Dashboard expiring-today = Active (not Expired) ──────────────────

    [Fact]
    public void SubscriptionCounts_ExpiringToday_IsActive_NotExpired()
    {
        var today = ColombMidnight(2026, 11, 15);

        var requests = Array.Empty<int>();
        var subs = new[]
        {
            // Expiry == today → Active (ExpiryDate >= today)
            new SubscriptionView((int)MemberSubscriptionStatusEnum.Activate, today),
        };

        var counts = DashboardCalculator.SubscriptionCounts(requests, subs, today);

        Assert.Equal(1, counts.Active);
        Assert.Equal(0, counts.Expired);
    }

    [Fact]
    public void SubscriptionCounts_ExpiredYesterday_IsExpired()
    {
        var today = ColombMidnight(2026, 11, 15);

        var requests = Array.Empty<int>();
        var subs = new[]
        {
            // Expiry == yesterday → Expired (ExpiryDate < today)
            new SubscriptionView((int)MemberSubscriptionStatusEnum.Activate, today.AddDays(-1)),
        };

        var counts = DashboardCalculator.SubscriptionCounts(requests, subs, today);

        Assert.Equal(0, counts.Active);
        Assert.Equal(1, counts.Expired);
    }

    // ── L8/D6: HMAC round trip ────────────────────────────────────────────────

    [Fact]
    public void KeyService_GenerateAndVerify_RoundTrip()
    {
        var keySvc = KeyServiceFactory.Create("my-secret");
        var expiry = ColombMidnight(2026, 12, 31);

        var key = keySvc.Generate(
            memberId: 1,
            subscriptionId: 2,
            memberSubscriptionId: 3,
            expiryDate: expiry,
            paidAmount: 0m,
            subscriptionActionTypeEnum: (int)SubscriptionActionTypeEnum.General,
            subscriptionTypeEnum: (int)SubscriptionTypeEnum.Declarant);

        Assert.False(string.IsNullOrEmpty(key));

        var valid = keySvc.Verify(1, 2, 3, expiry, 0m,
            (int)SubscriptionActionTypeEnum.General,
            (int)SubscriptionTypeEnum.Declarant,
            key);

        Assert.True(valid);
    }

    [Fact]
    public void KeyService_Verify_ReturnsFalse_WhenKeyTampered()
    {
        var keySvc = KeyServiceFactory.Create("my-secret");
        var expiry = ColombMidnight(2026, 12, 31);

        var key = keySvc.Generate(1, 2, 3, expiry, 0m,
            (int)SubscriptionActionTypeEnum.General,
            (int)SubscriptionTypeEnum.Declarant);

        var valid = keySvc.Verify(1, 2, 3, expiry, 0m,
            (int)SubscriptionActionTypeEnum.General,
            (int)SubscriptionTypeEnum.Declarant,
            key + "tampered");

        Assert.False(valid);
    }

    [Fact]
    public void KeyService_IsDeteministic_SameInputsSameKey()
    {
        var keySvc = KeyServiceFactory.Create("some-secret");
        var expiry = ColombMidnight(2027, 6, 15);

        var key1 = keySvc.Generate(42, 7, 999, expiry, 0m,
            (int)SubscriptionActionTypeEnum.OnlyForAcknowledgement,
            (int)SubscriptionTypeEnum.Consignee);
        var key2 = keySvc.Generate(42, 7, 999, expiry, 0m,
            (int)SubscriptionActionTypeEnum.OnlyForAcknowledgement,
            (int)SubscriptionTypeEnum.Consignee);

        Assert.Equal(key1, key2);
    }

    // ── PaidAmount decimal-scale independence ─────────────────────────────────

    [Fact]
    public void KeyService_PaidAmount_DifferentScales_ProduceIdenticalKey()
    {
        var keySvc = KeyServiceFactory.Create("my-secret");
        var expiry = ColombMidnight(2026, 12, 31);

        string Key(decimal paid) => keySvc.Generate(
            memberId: 1, subscriptionId: 2, memberSubscriptionId: 3,
            expiryDate: expiry, paidAmount: paid,
            subscriptionActionTypeEnum: (int)SubscriptionActionTypeEnum.General,
            subscriptionTypeEnum: (int)SubscriptionTypeEnum.Declarant);

        // 25000, 25000.00 and 25000.0000 are the same value at different scales.
        var k0 = Key(25000m);
        var k2 = Key(25000.00m);
        var k4 = Key(25000.0000m);

        Assert.Equal(k0, k2);
        Assert.Equal(k0, k4);
    }

    [Fact]
    public void KeyService_PaidAmount_DifferentValue_ProducesDifferentKey()
    {
        var keySvc = KeyServiceFactory.Create("my-secret");
        var expiry = ColombMidnight(2026, 12, 31);

        string Key(decimal paid) => keySvc.Generate(1, 2, 3, expiry, paid,
            (int)SubscriptionActionTypeEnum.General, (int)SubscriptionTypeEnum.Declarant);

        // A real difference at 2 decimals must still change the key.
        Assert.NotEqual(Key(25000.00m), Key(25000.01m));
    }

    [Fact]
    public void KeyService_VerifyEntity_IgnoresScaleDrift_ButNotValueChange()
    {
        var keySvc = KeyServiceFactory.Create("my-secret");

        // Simulate approve copying a numeric(18,4) plan amount into the entity, then
        // the numeric(18,2) column returning it at scale 2 on the next read.
        var sub = new MemberSubscription
        {
            MemberId = 1, SubscriptionId = 2, MemberSubscriptionId = 3,
            ExpiryDate = ColombMidnight(2026, 12, 31),
            PaidAmount = 25000.0000m,
            SubscriptionActionTypeEnum = (int)SubscriptionActionTypeEnum.General,
            SubscriptionTypeEnum = (int)SubscriptionTypeEnum.Declarant,
            SubscriptionName = "x",
        };
        sub.Key = keySvc.Generate(sub);

        // Reloaded at the column scale → still verifies.
        sub.PaidAmount = 25000.00m;
        Assert.True(keySvc.Verify(sub));

        // A genuine value change → mismatch.
        sub.PaidAmount = 25000.01m;
        Assert.False(keySvc.Verify(sub));
    }

    // ── B13: Lifetime duration ────────────────────────────────────────────────

    [Fact]
    public void ComputeExpiry_Lifetime_Is_Start_Plus_500_Years()
    {
        var start = ColombMidnight(2026, 11, 15);
        var expiry = SubscriptionExpiry.ComputeExpiryDate(
            start, noOfActiveDuration: 1,
            durationTypeEnum: (int)SubscriptionDurationTypeEnum.Lifetime);

        var colomboExpiry = TimeZoneInfo.ConvertTime(expiry, Colombo);
        Assert.Equal(2526, colomboExpiry.Year);
        Assert.Equal(11, colomboExpiry.Month);
        Assert.Equal(15, colomboExpiry.Day);
    }

    [Fact]
    public void ComputeExpiry_UnknownDurationType_Throws()
    {
        var start = ColombMidnight(2026, 11, 15);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SubscriptionExpiry.ComputeExpiryDate(start, 1, durationTypeEnum: 99));
    }

    [Fact]
    public void ComputeExpiry_NullDurationType_Throws()
    {
        var start = ColombMidnight(2026, 11, 15);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SubscriptionExpiry.ComputeExpiryDate(start, 1, durationTypeEnum: null));
    }

    [Theory]
    [InlineData((int)SubscriptionDurationTypeEnum.Days, "Days")]
    [InlineData((int)SubscriptionDurationTypeEnum.Months, "Months")]
    [InlineData((int)SubscriptionDurationTypeEnum.Years, "Years")]
    [InlineData((int)SubscriptionDurationTypeEnum.Lifetime, "Lifetime")]
    public void DurationFormat_Label_MatchesType(int type, string expected)
        => Assert.Equal(expected, SubscriptionDurationFormat.Label(type));

    [Fact]
    public void DurationFormat_Describe_Lifetime_IgnoresCount()
        => Assert.Equal("Lifetime",
            SubscriptionDurationFormat.Describe(12, (int)SubscriptionDurationTypeEnum.Lifetime));

    [Fact]
    public void DurationFormat_Describe_PluralisesCount()
    {
        Assert.Equal("1 month", SubscriptionDurationFormat.Describe(1, (int)SubscriptionDurationTypeEnum.Months));
        Assert.Equal("12 months", SubscriptionDurationFormat.Describe(12, (int)SubscriptionDurationTypeEnum.Months));
    }

    [Fact]
    public void DurationFormat_UnknownType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SubscriptionDurationFormat.Label(99));
        Assert.Throws<ArgumentOutOfRangeException>(() => SubscriptionDurationFormat.Describe(1, null));
    }

    // ── B01: shared usable rule — expression and in-memory predicate agree ────

    [Fact]
    public void Usable_ExpressionAndInMemoryPredicate_Agree_OnFixtureSet()
    {
        var today = ColombMidnight(2026, 11, 16);
        var compiled = SubscriptionUsability.UsableExpression(today).Compile();

        foreach (var sub in UsableFixtures(today))
        {
            Assert.Equal(
                compiled(sub),
                SubscriptionUsability.IsUsable(sub, today));
        }
    }

    private static IEnumerable<MemberSubscription> UsableFixtures(DateTimeOffset today)
    {
        Company ActiveCompany() => new() { CompanyId = 1, CompanyName = "C", TinNumber = "123456789", IsDeleted = false, CompanyStatusEnum = (int)CompanyStatusEnum.Active };
        Company InactiveCompany() => new() { CompanyId = 2, CompanyName = "C", TinNumber = "123456789", IsDeleted = false, CompanyStatusEnum = (int)CompanyStatusEnum.Suspended };
        Company DeletedCompany() => new() { CompanyId = 3, CompanyName = "C", TinNumber = "123456789", IsDeleted = true, CompanyStatusEnum = (int)CompanyStatusEnum.Active };

        MemberSubscription Sub(int status, DateTimeOffset? expiry, Company? company) => new()
        {
            MemberSubscriptionId = 1, MemberId = 1, SubscriptionName = "x",
            MemberSubscriptionStatusEnum = status, ExpiryDate = expiry, Company = company!,
        };

        // fully usable
        yield return Sub((int)MemberSubscriptionStatusEnum.Activate, today, ActiveCompany());
        yield return Sub((int)MemberSubscriptionStatusEnum.Activate, today.AddDays(5), ActiveCompany());
        // expired by date
        yield return Sub((int)MemberSubscriptionStatusEnum.Activate, today.AddDays(-1), ActiveCompany());
        // null expiry
        yield return Sub((int)MemberSubscriptionStatusEnum.Activate, null, ActiveCompany());
        // inactive status
        yield return Sub((int)MemberSubscriptionStatusEnum.Deactivate, today, ActiveCompany());
        yield return Sub((int)MemberSubscriptionStatusEnum.Expired, today, ActiveCompany());
        // company suspended
        yield return Sub((int)MemberSubscriptionStatusEnum.Activate, today, InactiveCompany());
        // company deleted
        yield return Sub((int)MemberSubscriptionStatusEnum.Activate, today, DeletedCompany());
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static DateTimeOffset ColombMidnight(int year, int month, int day)
    {
        var dt = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(dt, Colombo.GetUtcOffset(dt));
    }
}

// ── Production key service for the Application.Tests layer ────────────────────
// Builds the real MemberSubscriptionKeyService (Infrastructure) with a test secret
// via in-memory configuration, so these HMAC tests exercise production code.

file static class KeyServiceFactory
{
    public static IMemberSubscriptionKeyService Create(string secret)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cdns:MemberSubscriptionSecret"] = secret,
            })
            .Build();
        return new MemberSubscriptionKeyService(config);
    }
}
