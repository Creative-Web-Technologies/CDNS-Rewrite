using Cdns.Application.Feature.Subscriptions;
using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Persistence;
using Cdns.Infrastructure.Services.Cusdec;
using Cdns.Infrastructure.Services.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cdns.Infrastructure.Tests;

internal static class TestHelpers
{
    public static CdnsDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<CdnsDbContext>()
            .UseInMemoryDatabase(name)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new CdnsDbContext(options);
    }

    /// <summary>A real SubscriptionAccessService over the given context.</summary>
    public static SubscriptionAccessService AccessService(
        CdnsDbContext db, IBusinessClock clock, IMemberSubscriptionKeyService keys)
        => new(db, clock, keys, NullLogger<SubscriptionAccessService>.Instance);

    /// <summary>MemberSubscriptionService wired with its access service.</summary>
    public static MemberSubscriptionService NewMemberSubscriptionService(
        CdnsDbContext db, ICurrentUser user, IBusinessClock clock, IMemberSubscriptionKeyService keys)
        => new(db, user, clock, AccessService(db, clock, keys));

    /// <summary>Shared subscription scope backed by an InMemory db context.</summary>
    public static CusDecSubscriptionScope SubscriptionScope(
        CdnsDbContext db, ICurrentUser user, IMemberSubscriptionKeyService keys, IBusinessClock clock)
        => new(db, user, AccessService(db, clock, keys));

    /// <summary>
    /// Creates a Npgsql-wired DbContext for table-name and migration checks.
    /// Uses the same conventions as DependencyInjection (UseSnakeCaseNamingConvention).
    /// Pass a real connection string; this context is for model inspection only.
    /// </summary>
    public static CdnsDbContext CreateNpgsqlContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<CdnsDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new CdnsDbContext(options);
    }

    /// <summary>
    /// Backfills a valid HMAC key (computed from stored values) onto every tracked
    /// MemberSubscription that has an ExpiryDate and no key, so access-list fixtures
    /// aren't rejected for a missing key. Call just before SaveChanges.
    /// </summary>
    public static void AssignSubscriptionKeys(CdnsDbContext db, IMemberSubscriptionKeyService keys)
    {
        foreach (var entry in db.ChangeTracker.Entries<MemberSubscription>())
        {
            var s = entry.Entity;
            if (string.IsNullOrEmpty(s.Key) && s.ExpiryDate is not null)
                s.Key = keys.Generate(s);
        }
    }

    public static CdnsDbContext CreateDbWithInterceptor(string name, UtcDateTimeOffsetInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<CdnsDbContext>()
            .UseInMemoryDatabase(name)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(interceptor)
            .Options;
        return new CdnsDbContext(options);
    }
}

internal sealed class FakeCurrentUser(long memberId) : ICurrentUser
{
    public long MemberId => memberId;
    public string UserName => "test";
    public MemberType MemberType => MemberType.Member;
    public bool IsAuthenticated => true;
}

internal sealed class FakeMasterUser(long memberId) : ICurrentUser
{
    public long MemberId => memberId;
    public string UserName => "master-test";
    public MemberType MemberType => MemberType.CompanyMasterMember;
    public bool IsAuthenticated => true;
}

/// <summary>
/// Deterministic clock for tests. Both <see cref="Now"/> and
/// <see cref="StartOfToday"/> return UTC (Offset == TimeSpan.Zero) so the
/// values are safe as Npgsql query parameters, matching the production clock
/// contract.
/// </summary>
internal sealed class FakeBusinessClock(DateTimeOffset fixedNow) : IBusinessClock
{
    private static readonly TimeZoneInfo Colombo =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");

    public DateTimeOffset Now() => fixedNow.ToUniversalTime();

    public DateTimeOffset StartOfToday()
    {
        var colomboNow = TimeZoneInfo.ConvertTime(fixedNow, Colombo);
        var midnight = new DateTime(
            colomboNow.Year, colomboNow.Month, colomboNow.Day,
            0, 0, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(midnight, Colombo.GetUtcOffset(midnight)).ToUniversalTime();
    }
}

internal static class FakeKeyServiceFactory
{
    public static Cdns.Application.Feature.Subscriptions.IMemberSubscriptionKeyService Create(
        string secret = "test-secret")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Cdns:MemberSubscriptionSecret"] = secret,
            })
            .Build();
        return new Cdns.Infrastructure.Services.Subscriptions.MemberSubscriptionKeyService(config);
    }
}
