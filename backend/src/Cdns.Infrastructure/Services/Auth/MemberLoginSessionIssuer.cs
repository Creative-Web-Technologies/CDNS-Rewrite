using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.MemberLoginTables;
using Cdns.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cdns.Infrastructure.Services.Auth;

public class MemberLoginSessionIssuer : IAuthResultIssuer
{
    private readonly CdnsDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MemberLoginSessionIssuer> _logger;
    private readonly IHostEnvironment _env;

    private const int MaxIpLength = 50;
    private const int MaxUaLength = 512;

    public MemberLoginSessionIssuer(
        CdnsDbContext db,
        IHttpContextAccessor httpContextAccessor,
        ILogger<MemberLoginSessionIssuer> logger,
        IHostEnvironment env)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _env = env;
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
                Secure = !_env.IsDevelopment(), // true in every env except Development
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });
        }

        // Best-effort: a failure here must not block the login.
        // RemoteIpAddress reflects the real client IP when UseForwardedHeaders() is configured.
        try
        {
            var rawIp = httpContext?.Connection.RemoteIpAddress?.ToString();
            var rawUa = httpContext?.Request.Headers.UserAgent.ToString();

            _db.MemberUserLoginDetails.Add(new MemberUserLoginDetail
            {
                MemberId = memberId,
                LoginDateTime = DateTimeOffset.UtcNow,
                IpAddress = rawIp is { Length: > MaxIpLength } ? rawIp[..MaxIpLength] : rawIp,
                UserAgent = rawUa is { Length: > MaxUaLength } ? rawUa[..MaxUaLength] : rawUa
            });
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write login detail for member {MemberId} — login unaffected", memberId);
        }

        return session.SessionId.ToString();
    }
}
