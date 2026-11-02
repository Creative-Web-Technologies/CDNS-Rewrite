using Cdns.Application.Auth;
using Cdns.Application.Interfaces;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Persistence;
using Cdns.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Services.Auth;

public class LoginService : ILoginService
{
    private readonly CdnsDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IEregService _ereg;
    private readonly IAuthResultIssuer _issuer;

    public LoginService(CdnsDbContext db, IPasswordHasher hasher, IEregService ereg, IAuthResultIssuer issuer)
    {
        _db = db;
        _hasher = hasher;
        _ereg = ereg;
        _issuer = issuer;
    }

    public async Task<LoginResult> LoginAsync(string userName, string password, CancellationToken ct = default)
    {
        var memberUser = await _db.MemberUsers
            .Include(u => u.Member)
            .FirstOrDefaultAsync(u => u.UserName == userName, ct);

        if (memberUser is null)
            return new LoginResult.InvalidCredentials();

        // IsWrongAttempt is a bool flag — the schema has no integer counter, so lockout is
        // single-strike. To support an N-attempt threshold, add a WrongAttemptCount int column.
        if (memberUser.IsWrongAttempt)
            return new LoginResult.AccountLocked();

        // Master activation: eReg validates credentials instead of a local password check
        if (memberUser.MemberType == MemberType.CompanyMasterMember && memberUser.RequiredEregVerification)
        {
            var eregOk = await _ereg.ValidateMasterActivation(userName, password);
            if (!eregOk)
                return new LoginResult.InvalidCredentials();

            // GlobalIdentity is the opaque token the client carries through the activation flow
            return new LoginResult.MasterActivationRequired(memberUser.GlobalIdentity);
        }

        if (!_hasher.Verify(password, memberUser.Password, memberUser.PasswordSalt))
        {
            memberUser.IsWrongAttempt = true;
            memberUser.LastLoginAttemptDateTime = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
            return new LoginResult.InvalidCredentials();
        }

        memberUser.IsWrongAttempt = false;
        memberUser.LastLoginAttemptDateTime = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var sessionToken = await _issuer.IssueAsync(memberUser.MemberId, memberUser.UserName, ct);
        return new LoginResult.Success(sessionToken);
    }
}
