using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.MemberLoginTables;
using Cdns.Infrastructure.Persistence;

namespace Cdns.Infrastructure.Services.Auth;

public class MemberLoginSessionIssuer : IAuthResultIssuer
{
    private readonly CdnsDbContext _db;

    public MemberLoginSessionIssuer(CdnsDbContext db) => _db = db;

    public async Task<string> IssueAsync(long memberId, string userName, CancellationToken ct = default)
    {
        var session = new MemberLoginSession
        {
            MemberId = memberId,
            UserName = userName,
            SessionId = Guid.NewGuid(),
            LoginDateTime = DateTimeOffset.UtcNow
        };
        _db.MemberLoginSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session.SessionId.ToString();
    }
}
