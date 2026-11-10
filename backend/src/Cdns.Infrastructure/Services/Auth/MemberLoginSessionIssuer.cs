using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.MemberLoginTables;
using Cdns.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;

namespace Cdns.Infrastructure.Services.Auth;

public class MemberLoginSessionIssuer : IAuthResultIssuer
{
    private readonly CdnsDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MemberLoginSessionIssuer(CdnsDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

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

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            // MemberLoginSession has no ExpiresAt column yet — using 8-hour working-day default.
            // When an expiry column is added to the schema, replace AddHours(8) with that value.
            httpContext.Response.Cookies.Append("session_id", session.SessionId.ToString(), new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });
        }

        return session.SessionId.ToString();
    }
}
