using Cdns.Application.Interfaces;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

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
