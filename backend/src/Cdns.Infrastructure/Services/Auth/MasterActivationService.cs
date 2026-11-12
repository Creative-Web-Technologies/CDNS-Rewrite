using Cdns.Application.Auth;
using Cdns.Application.Interfaces;
using Cdns.Application.Security;
using Cdns.Domain.Enums;
using Cdns.Infrastructure.Persistence;
using Cdns.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cdns.Infrastructure.Services.Auth;

public class MasterActivationService : IMasterActivationService
{
    private readonly CdnsDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IAuthResultIssuer _issuer;
    private readonly ILogger<MasterActivationService> _logger;

    public MasterActivationService(
        CdnsDbContext db,
        IPasswordHasher hasher,
        IAuthResultIssuer issuer,
        ILogger<MasterActivationService> logger)
    {
        _db = db;
        _hasher = hasher;
        _issuer = issuer;
        _logger = logger;
    }

    public async Task<CompleteProfileResult> CompleteProfileAsync(
        Guid activationToken,
        CompleteProfileRequest request,
        CancellationToken ct = default)
    {
        var memberUser = await _db.MemberUsers
            .Include(u => u.Member)
            .FirstOrDefaultAsync(u => u.GlobalIdentity == activationToken, ct);

        if (memberUser is null
            || memberUser.MemberType != MemberType.CompanyMasterMember
            || !memberUser.RequiredEregVerification)
            return new CompleteProfileResult.InvalidToken();

        var member = memberUser.Member;
        member.FirstName = request.FirstName;
        member.LastName = request.LastName;
        member.Mobile = request.Mobile;
        member.Nic = request.Nic;
        member.Designation = request.Designation;

        await _db.SaveChangesAsync(ct);
        return new CompleteProfileResult.Success();
    }

    public async Task<SetActivationPasswordResult> SetPasswordAsync(
        Guid activationToken,
        string newPassword,
        CancellationToken ct = default)
    {
        var memberUser = await _db.MemberUsers
            .Include(u => u.Member)
            .FirstOrDefaultAsync(u => u.GlobalIdentity == activationToken, ct);

        if (memberUser is null
            || memberUser.MemberType != MemberType.CompanyMasterMember
            || !memberUser.RequiredEregVerification)
            return new SetActivationPasswordResult.InvalidToken();

        if (!PasswordPolicy.IsValid(newPassword))
            return new SetActivationPasswordResult.WeakPassword();

        // All DB mutations run inside a single transaction so that a failure in
        // ClaimCusDecsForMaster (or any later step) rolls back the password change
        // and RequiredEregVerification flip, leaving the master still unactivated
        // and retryable.
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var (hash, salt) = _hasher.HashNewPassword(newPassword);
            memberUser.Password = hash;
            memberUser.PasswordSalt = salt;
            memberUser.RequiredEregVerification = false;
            memberUser.UserStatusType = UserStatusType.Active;

            await _db.SaveChangesAsync(ct);

            var mapping = await _db.CompanyMemberMappings
                .FirstOrDefaultAsync(m => m.MemberId == memberUser.MemberId && !m.IsDeleted, ct);

            if (mapping is not null)
            {
                await SetupDefaultSubscription(memberUser.MemberId, mapping.CompanyId, ct);
                await ClaimCusDecsForMaster(memberUser.MemberId, mapping.CompanyId, ct);
            }

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        // Session issued AFTER commit: cookie writes are not transactional.
        // If IssueAsync fails, the master is fully activated and can log in normally.
        var sessionToken = await _issuer.IssueAsync(memberUser.MemberId, memberUser.UserName, ct);
        return new SetActivationPasswordResult.Success(sessionToken, "Company");
    }

    private async Task SetupDefaultSubscription(long memberId, int companyId, CancellationToken ct)
    {
        // Stub: MemberSubscription requires a SubscriptionId FK to a real Subscriptions row,
        // and no subscription seed data exists yet. Implement once subscription records are seeded:
        //   1. Look up Subscription by SubscriptionTypeEnum matching Member.DefaultSubscriptionType
        //   2. Create SubscriptionRequest (status = Approved) + MemberSubscription (status = Active)
        _logger.LogWarning(
            "SetupDefaultSubscription not yet implemented — member {MemberId} / company {CompanyId} has no subscription",
            memberId, companyId);
        await Task.CompletedTask;
    }

    private async Task ClaimCusDecsForMaster(long memberId, int companyId, CancellationToken ct)
    {
        // timestamp without time zone columns require Unspecified Kind — UTC Kind throws in Npgsql
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        var declarantCount = await _db.CusDecs
            .Where(c => c.DeclarantCompanyId == companyId
                     && c.DeclarantClaimedById == null
                     && c.DeclarantDisclaimedById == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.DeclarantClaimedById, memberId)
                .SetProperty(c => c.DeclarantClaimedDateTime, now),
            ct);

        var consigneeCount = await _db.CusDecs
            .Where(c => c.ConsigneeCompanyId == companyId
                     && c.ConsigneeClaimedById == null
                     && c.ConsigneeDisclaimedById == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.ConsigneeClaimedById, memberId)
                .SetProperty(c => c.ConsigneeClaimedDateTime, now),
            ct);

        _logger.LogInformation(
            "ClaimCusDecsForMaster: member {MemberId} / company {CompanyId} — {D} declarant, {C} consignee CusDecs claimed",
            memberId, companyId, declarantCount, consigneeCount);
    }
}
