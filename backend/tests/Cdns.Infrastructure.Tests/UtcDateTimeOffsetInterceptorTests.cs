using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Infrastructure.Persistence;
using Npgsql;

namespace Cdns.Infrastructure.Tests;

public class UtcDateTimeOffsetInterceptorTests
{
    private static readonly TimeZoneInfo Colombo =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");

    // ── (b) Interceptor normalizes +05:30 to UTC ──────────────────────────────

    [Fact]
    public async Task Interceptor_NormalizesColomboOffset_ToUtc_OnAdd()
    {
        var interceptor = new UtcDateTimeOffsetInterceptor();
        await using var db = TestHelpers.CreateDbWithInterceptor(
            nameof(Interceptor_NormalizesColomboOffset_ToUtc_OnAdd), interceptor);

        var localDt = new DateTime(2026, 11, 16, 10, 0, 0, DateTimeKind.Unspecified);
        var colomboNow = new DateTimeOffset(localDt, Colombo.GetUtcOffset(localDt)); // +05:30

        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            MemberId = 1,
            CompanyId = null,
            RequestedDateTime = colomboNow,
            IsDeleted = false,
            GlobalIdentity = Guid.NewGuid(),
        });
        await db.SaveChangesAsync();

        var stored = db.SubscriptionRequests.Single();
        Assert.Equal(TimeSpan.Zero, stored.RequestedDateTime.Offset);
        Assert.Equal(colomboNow.ToUniversalTime(), stored.RequestedDateTime);
    }

    [Fact]
    public async Task Interceptor_LeavesUtcOffsetUnchanged()
    {
        var interceptor = new UtcDateTimeOffsetInterceptor();
        await using var db = TestHelpers.CreateDbWithInterceptor(
            nameof(Interceptor_LeavesUtcOffsetUnchanged), interceptor);

        var utcNow = DateTimeOffset.UtcNow;

        db.SubscriptionRequests.Add(new SubscriptionRequest
        {
            MemberId = 1,
            CompanyId = null,
            RequestedDateTime = utcNow,
            IsDeleted = false,
            GlobalIdentity = Guid.NewGuid(),
        });
        await db.SaveChangesAsync();

        var stored = db.SubscriptionRequests.Single();
        Assert.Equal(TimeSpan.Zero, stored.RequestedDateTime.Offset);
        Assert.Equal(utcNow, stored.RequestedDateTime);
    }

    // ── (c) HMAC round-trip: sign with +05:30, verify with UTC ───────────────

    [Fact]
    public void HmacKey_VerifyPassesAfterConversionToUtc()
    {
        // SubscriptionExpiry returns a Colombo-offset value, which we store as
        // UTC. Verify() receives the UTC value read back from DB and must still
        // match the key that was generated with the original Colombo-offset value.
        // Both paths convert to Asia/Colombo before formatting, so they agree.
        var keySvc = FakeKeyServiceFactory.Create();

        // Expiry at 02:00 Colombo on 16 Nov 2026 (+05:30)
        var localDt = new DateTime(2026, 11, 16, 2, 0, 0, DateTimeKind.Unspecified);
        var expiryColombо = new DateTimeOffset(localDt, Colombo.GetUtcOffset(localDt));

        // Sign with the original Colombo-offset value (what the service produces)
        var key = keySvc.Generate(1L, 2, 3L, expiryColombо, 100m, 0, 0);

        // Simulate what the DB returns: UTC (2026-11-15 20:30 +00:00)
        var expiryUtc = expiryColombо.ToUniversalTime();
        Assert.Equal(TimeSpan.Zero, expiryUtc.Offset);

        // Verify with the UTC value must pass — same Colombo date "2026-11-16"
        Assert.True(keySvc.Verify(1L, 2, 3L, expiryUtc, 100m, 0, 0, key));
    }

    [Fact]
    public void HmacKey_VerifyFailsOnTamperedField()
    {
        var keySvc = FakeKeyServiceFactory.Create();

        var localDt = new DateTime(2026, 11, 16, 2, 0, 0, DateTimeKind.Unspecified);
        var expiryColombо = new DateTimeOffset(localDt, Colombo.GetUtcOffset(localDt));

        var key = keySvc.Generate(1L, 2, 3L, expiryColombо, 100m, 0, 0);
        var expiryUtc = expiryColombо.ToUniversalTime();

        // Tampered: paidAmount changed from 100 to 999
        Assert.False(keySvc.Verify(1L, 2, 3L, expiryUtc, 999m, 0, 0, key));
    }

    // ── Command interceptor: normalizes DateTimeOffset query parameters ───────

    [Fact]
    public void CommandInterceptor_NormalizesColomboParam_ToUtc()
    {
        // Simulate a LINQ query that passes StartOfToday() (now fixed to UTC, but
        // this tests the safety-net interceptor directly).
        var localDt = new DateTime(2026, 11, 16, 0, 0, 0, DateTimeKind.Unspecified);
        var colomboParam = new DateTimeOffset(localDt, Colombo.GetUtcOffset(localDt)); // +05:30

        using var cmd = new NpgsqlCommand("SELECT 1 WHERE @today <= expiry_date");
        cmd.Parameters.AddWithValue("today", colomboParam);

        UtcDateTimeOffsetCommandInterceptor.NormalizeParameters(cmd);

        var normalized = (DateTimeOffset)cmd.Parameters[0].Value!;
        Assert.Equal(TimeSpan.Zero, normalized.Offset);
        Assert.Equal(colomboParam.ToUniversalTime(), normalized);
    }

    [Fact]
    public void CommandInterceptor_LeavesUtcParamUnchanged()
    {
        var utcParam = new DateTimeOffset(2026, 11, 15, 18, 30, 0, TimeSpan.Zero);

        using var cmd = new NpgsqlCommand("SELECT 1 WHERE @today <= expiry_date");
        cmd.Parameters.AddWithValue("today", utcParam);

        UtcDateTimeOffsetCommandInterceptor.NormalizeParameters(cmd);

        var value = (DateTimeOffset)cmd.Parameters[0].Value!;
        Assert.Equal(TimeSpan.Zero, value.Offset);
        Assert.Equal(utcParam, value);
    }
}
