using Cdns.Application.Interfaces;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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
}

internal sealed class FakeCurrentUser(long memberId) : ICurrentUser
{
    public long MemberId => memberId;
    public string UserName => "test";
    public MemberType MemberType => MemberType.Member;
    public bool IsAuthenticated => true;
}
