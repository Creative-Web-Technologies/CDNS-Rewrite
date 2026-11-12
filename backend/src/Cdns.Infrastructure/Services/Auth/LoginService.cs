using Cdns.Application.Auth;
using Cdns.Application.Interfaces;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Persistence;
using Cdns.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Cdns.Infrastructure.Services.Auth;

public class LoginService : ILoginService
{
    private readonly CdnsDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IEregService _ereg;
    private readonly IAuthResultIssuer _issuer;
    private readonly IMemoryCache _cache;

    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 20;

    public LoginService(CdnsDbContext db, IPasswordHasher hasher, IEregService ereg, IAuthResultIssuer issuer, IMemoryCache cache)
    {
        _db = db;
        _hasher = hasher;
        _ereg = ereg;
        _issuer = issuer;
        _cache = cache;
    }

    private static string WrongAttemptsKey(string userName) => $"wrong-attempts:{userName.Trim().ToLowerInvariant()}";

    public async Task<LoginResult> LoginAsync(string userName, string password, CancellationToken ct = default)
    {
        var memberUser = await _db.MemberUsers
            .Include(u => u.Member)
            .FirstOrDefaultAsync(u => u.UserName == userName, ct);

        if (memberUser is null)
            return new LoginResult.InvalidCredentials();

        var cacheKey = WrongAttemptsKey(memberUser.UserName);

        if (memberUser.IsWrongAttempt)
        {
            if ((memberUser.LastLoginAttemptDateTime ?? DateTimeOffset.MinValue).AddMinutes(LockoutMinutes) < DateTimeOffset.UtcNow)
            {
                // 20-minute window has passed — auto-unlock and treat this as a fresh attempt
                memberUser.IsWrongAttempt = false;
                await _db.SaveChangesAsync(ct);
                _cache.Remove(cacheKey);
            }
            else
            {
                return new LoginResult.AccountLocked();
            }
        }

        // Master activation: eReg validates credentials instead of a local password check
        if (memberUser.MemberType == MemberType.CompanyMasterMember && memberUser.RequiredEregVerification)
        {
            var eregOk = await _ereg.ValidateMasterActivation(userName, password);
            if (!eregOk)
            {
                if (!_cache.TryGetValue<(int Count, DateTimeOffset ExpiresAt)>(cacheKey, out var entry))
                    entry = (0, DateTimeOffset.UtcNow.AddMinutes(LockoutMinutes));

                var newCount = entry.Count + 1;
                _cache.Set(cacheKey, (newCount, entry.ExpiresAt), new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = entry.ExpiresAt
                });

                if (newCount >= MaxFailedAttempts)
                {
                    memberUser.IsWrongAttempt = true;
                    memberUser.LastLoginAttemptDateTime = DateTimeOffset.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    return new LoginResult.AccountLocked();
                }

                return new LoginResult.InvalidCredentials();
            }

            _cache.Remove(cacheKey);
            // GlobalIdentity is the opaque token the client carries through the activation flow
            return new LoginResult.MasterActivationRequired(memberUser.GlobalIdentity);
        }

        // Status check lives here — AFTER the eReg branch — so CompanyMasterMembers with
        // RequiredEregVerification (legitimately NotActive until activation completes) are
        // not blocked. Generic response: callers must not be able to distinguish a disabled
        // account from a wrong password (enumeration / insider-knowledge risk).
        if (memberUser.UserStatusType != UserStatusType.Active)
            return new LoginResult.InvalidCredentials();

        if (!_hasher.Verify(password, memberUser.Password, memberUser.PasswordSalt))
        {
            if (!_cache.TryGetValue<(int Count, DateTimeOffset ExpiresAt)>(cacheKey, out var entry))
                entry = (0, DateTimeOffset.UtcNow.AddMinutes(LockoutMinutes));

            var newCount = entry.Count + 1;
            _cache.Set(cacheKey, (newCount, entry.ExpiresAt), new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = entry.ExpiresAt
            });

            if (newCount >= MaxFailedAttempts)
            {
                memberUser.IsWrongAttempt = true;
                memberUser.LastLoginAttemptDateTime = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(ct);
            }

            return new LoginResult.InvalidCredentials();
        }

        _cache.Remove(cacheKey);
        memberUser.IsWrongAttempt = false;
        memberUser.LastLoginAttemptDateTime = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var sessionToken = await _issuer.IssueAsync(memberUser.MemberId, memberUser.UserName, ct);
        var portal = memberUser.MemberType == MemberType.CompanyMasterMember ? "Company" : "Member";
        return new LoginResult.Success(sessionToken, portal);
    }
}
