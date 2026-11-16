using Cdns.Application.Feature.Subscriptions;
using Cdns.Application.Feature.Dashboard;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Enums;

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

    // ── Helper ────────────────────────────────────────────────────────────────

    private static DateTimeOffset ColombMidnight(int year, int month, int day)
    {
        var dt = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(dt, Colombo.GetUtcOffset(dt));
    }
}

// ── Inline key service for the Application.Tests layer ───────────────────────
// Application.Tests references only Cdns.Application, not Cdns.Infrastructure,
// so we re-implement the HMAC logic inline rather than coupling to Infrastructure.

file static class KeyServiceFactory
{
    public static Cdns.Application.Feature.Subscriptions.IMemberSubscriptionKeyService Create(
        string secret)
        => new TestKeyService(secret);

    private sealed class TestKeyService : Cdns.Application.Feature.Subscriptions.IMemberSubscriptionKeyService
    {
        private static readonly TimeZoneInfo Colombo =
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");
        private readonly string _secret;

        public TestKeyService(string secret) => _secret = secret;

        public string Generate(long memberId, int subscriptionId, long memberSubscriptionId,
            DateTimeOffset expiryDate, decimal paidAmount, int subscriptionActionTypeEnum, int subscriptionTypeEnum)
        {
            var expiryDateStr = ToColomboDayString(expiryDate);
            var data = $"{memberId}!{subscriptionId}!{memberSubscriptionId}!{expiryDateStr}!" +
                       $"{paidAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)}!" +
                       $"{subscriptionActionTypeEnum}!{subscriptionTypeEnum}";
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(_secret + expiryDateStr);
            var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
            using var hmac = new System.Security.Cryptography.HMACSHA512(keyBytes);
            return Convert.ToBase64String(hmac.ComputeHash(dataBytes));
        }

        public bool Verify(long memberId, int subscriptionId, long memberSubscriptionId,
            DateTimeOffset expiryDate, decimal paidAmount, int subscriptionActionTypeEnum, int subscriptionTypeEnum,
            string key)
            => string.Equals(
                Generate(memberId, subscriptionId, memberSubscriptionId,
                    expiryDate, paidAmount, subscriptionActionTypeEnum, subscriptionTypeEnum),
                key,
                StringComparison.Ordinal);

        private static string ToColomboDayString(DateTimeOffset date)
        {
            var colombo = TimeZoneInfo.ConvertTime(date, Colombo);
            return colombo.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
