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

        // No ExpiresAt column yet — add expiry check here once the column is added to the schema.

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, session.MemberId.ToString()),
            new Claim(ClaimTypes.Name, session.UserName),
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
