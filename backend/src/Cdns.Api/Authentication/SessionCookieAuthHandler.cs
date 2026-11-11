using System.Security.Claims;
using System.Text.Encodings.Web;
using Cdns.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cdns.Api.Authentication;

public class SessionCookieAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "SessionCookie";

    private readonly CdnsDbContext _db;

    public SessionCookieAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        CdnsDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue("session_id", out var rawSessionId) ||
            !Guid.TryParse(rawSessionId, out var sessionId))
        {
            return AuthenticateResult.NoResult();
        }

        var session = await _db.MemberLoginSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session is null)
            return AuthenticateResult.Fail("Session not found.");

        if (session.LoginDateTime.AddHours(8) < DateTimeOffset.UtcNow)
            return AuthenticateResult.Fail("Session expired.");

        // Look up MemberType so authorization policies can branch on it
        var memberUser = await _db.MemberUsers
            .FirstOrDefaultAsync(u => u.MemberId == session.MemberId);

        var memberTypeValue = memberUser?.MemberType.ToString() ?? "Member";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, session.MemberId.ToString()),
            new Claim(ClaimTypes.Name, session.UserName),
            new Claim("MemberType", memberTypeValue),
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
