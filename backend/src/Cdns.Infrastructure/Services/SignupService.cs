using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Cdns.Application.Features.Signup;
using Cdns.Application.Interfaces;
using Cdns.Infrastructure.Persistence;
using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Entities.MemberLoginTables;
using Cdns.Domain.Enums;
using System.Security.Cryptography;
using Cdns.Infrastructure.Security;

namespace Cdns.Infrastructure.Services;

public class SignupService : ISignupService
{
    private readonly CdnsDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthResultIssuer _issuer;
    private readonly ILogger<SignupService> _logger;

    public SignupService(
        CdnsDbContext dbContext,
        IPasswordHasher passwordHasher,
        IAuthResultIssuer issuer,
        ILogger<SignupService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _issuer = issuer;
        _logger = logger;
    }

    private async Task<int> GetTinStatusAsync(string tin)
    {
        return await _dbContext.Database
            .SqlQueryRaw<int>("SELECT check_tin_signup_status({0}) AS \"Value\"", tin)
            .FirstOrDefaultAsync();
    }

    private static (string Code, string Message) TinStatusToError(int status) => status switch
    {
        2 => ("CompanyPendingEregVerification", "This company's registration is pending eReg verification."),
        3 => ("CompanyNotActivated", "This company's registration has not yet been activated."),
        4 => ("InvalidTin", "Invalid TIN. Must be exactly 9 digits."),
        5 => ("CompanySetupIncomplete", "This company's account setup is incomplete."),
        6 => ("OrganizationNotRegistered", "Company not found. Please check your TIN."),
        _ => ("UnexpectedError", "An unexpected error occurred. Please try again."),
    };

    public async Task<VerifyTinResponse> VerifyTinAsync(string tin)
    {
        var status = await GetTinStatusAsync(tin);

        if (status == 1)
            return new VerifyTinResponse { StatusCode = status, Message = "TIN verified successfully." };

        var (_, message) = TinStatusToError(status);
        return new VerifyTinResponse { StatusCode = status, Message = message };
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        // 1. Normalize
        var tin = new string(request.Tin.Trim().Where(char.IsDigit).ToArray());
        var email = request.Email.Trim().ToLowerInvariant();

        if (tin.Length != 9)
            return new RegisterResponse { Success = false, Code = "InvalidTin", Message = "TIN must be exactly 9 digits." };

        // 2. TIN gate — same logic as VerifyTinAsync so they can't drift
        var tinStatus = await GetTinStatusAsync(tin);
        if (tinStatus != 1)
        {
            var (code, message) = TinStatusToError(tinStatus);
            return new RegisterResponse { Success = false, Code = code, Message = message };
        }

        // 3a. Email == Master's UserName for this company
        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(c => c.TinNumber == tin);

        if (company != null)
        {
            var masterUserName = await (
                from mapping in _dbContext.CompanyMemberMappings
                join member in _dbContext.Members on mapping.MemberId equals member.MemberId
                join mu in _dbContext.MemberUsers on member.MemberId equals mu.MemberId
                where mapping.CompanyId == company.CompanyId
                    && !mapping.IsDeleted
                    && member.MemberType == MemberType.CompanyMasterMember
                select mu.UserName
            ).FirstOrDefaultAsync();

            if (masterUserName != null && masterUserName.ToLower() == email)
                return new RegisterResponse
                {
                    Success = false,
                    Code = "EmailBelongsToMaster",
                    Message = "This email is registered as the company administrator account."
                };
        }

        // 3b. Email in member_users (any existing user)
        if (await _dbContext.MemberUsers.AnyAsync(mu => mu.UserName.ToLower() == email))
            return new RegisterResponse { Success = false, Code = "AccountExists", Message = "An account with this email already exists." };

        // 3c. Email in members (not deleted)
        if (await _dbContext.Members.AnyAsync(m => !m.IsDeleted && m.Email != null && m.Email.ToLower() == email))
            return new RegisterResponse { Success = false, Code = "AccountExists", Message = "An account with this email already exists." };

        // 4. Soft-delete stale TemporaryMember rows so only the newest OTP/GlobalIdentity is live
        var stale = await _dbContext.TemporaryMembers
            .Where(t => !t.IsDeleted && t.Email != null && t.Email.ToLower() == email)
            .ToListAsync();

        if (stale.Count > 0)
        {
            foreach (var row in stale) row.IsDeleted = true;
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Soft-deleted {Count} stale TemporaryMember row(s) for {Email}", stale.Count, email);
        }

        // 5. Create TemporaryMember
        int otp = RandomNumberGenerator.GetInt32(100000, 999999);
        string newGlobalIdentity = Guid.NewGuid().ToString();

        var tempMember = new TemporaryMember
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email.Trim(),
            Mobile = request.Mobile,
            Nic = request.Nic,
            Designation = request.Designation,
            OneTimePassword = otp,
            CreatedDateTime = DateTime.UtcNow,
            GlobalIdentity = newGlobalIdentity
        };

        _dbContext.TemporaryMembers.Add(tempMember);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Signup OTP generated for session {GlobalIdentity}", newGlobalIdentity);

        return new RegisterResponse
        {
            Success = true,
            Message = "Registration successful. Please check your email for the OTP.",
            TempSignupToken = newGlobalIdentity
        };
    }

    public async Task<SignupVerifyOtpResult> VerifyOtpAsync(VerifyOtpRequest request)
    {
        string globalId = request.TempSignupToken;
        if (string.IsNullOrEmpty(globalId))
        {
            _logger.LogWarning("VerifyOtp called with empty token");
            return new SignupVerifyOtpResult.InvalidToken();
        }

        // Filter IsDeleted so stale rows from previous registrations are ignored
        var tempMember = await _dbContext.TemporaryMembers
            .FirstOrDefaultAsync(x => x.GlobalIdentity == globalId && !x.IsDeleted);

        if (tempMember == null)
        {
            _logger.LogWarning("VerifyOtp: no active TemporaryMember for token {GlobalIdentity}", globalId);
            return new SignupVerifyOtpResult.InvalidToken();
        }

        if (tempMember.OneTimePassword != request.Otp)
        {
            _logger.LogWarning("VerifyOtp: OTP mismatch for token {GlobalIdentity}", globalId);
            return new SignupVerifyOtpResult.InvalidToken();
        }

        tempMember.OneTimePassword = 0;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("OTP verified for signup session {GlobalIdentity}", globalId);
        return new SignupVerifyOtpResult.Success();
    }

    public async Task<SignupSetPasswordResult> SetPasswordAsync(SetPasswordRequest request)
    {
        string globalId = request.TempSignupToken;
        if (string.IsNullOrEmpty(globalId))
        {
            _logger.LogWarning("SetPassword called with empty token");
            return new SignupSetPasswordResult.InvalidToken();
        }

        var tempMember = await _dbContext.TemporaryMembers
            .FirstOrDefaultAsync(x => x.GlobalIdentity == globalId && !x.IsDeleted);

        if (tempMember == null || tempMember.OneTimePassword != 0)
        {
            _logger.LogWarning("SetPassword: temp member not found or OTP not verified for token {GlobalIdentity}", globalId);
            return new SignupSetPasswordResult.InvalidToken();
        }

        var (hash, salt) = _passwordHasher.HashNewPassword(request.Password);

        var newMember = new Member
        {
            FirstName = tempMember.FirstName,
            LastName = tempMember.LastName,
            Email = tempMember.Email,
            Mobile = tempMember.Mobile,
            Nic = tempMember.Nic,
            Designation = tempMember.Designation,
            MemberType = MemberType.Member
        };

        _dbContext.Members.Add(newMember);
        await _dbContext.SaveChangesAsync();

        var memberUser = new MemberUser
        {
            MemberId = newMember.MemberId,
            Member = newMember,
            UserName = tempMember.Email ?? string.Empty,
            Password = hash,
            PasswordSalt = salt,
            CreatedDateTime = DateTimeOffset.UtcNow,
            GlobalIdentity = Guid.NewGuid(),
            LastOtpGeneratedDateTime = DateTimeOffset.UtcNow,
            IsActiveOtp = true,
            MemberType = MemberType.Member,
            UserStatusType = UserStatusType.Active
        };

        _dbContext.MemberUsers.Add(memberUser);
        _dbContext.TemporaryMembers.Remove(tempMember);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Member {MemberId} created via signup for {Email}", newMember.MemberId, tempMember.Email);

        var sessionToken = await _issuer.IssueAsync(newMember.MemberId, memberUser.UserName);
        return new SignupSetPasswordResult.Success(sessionToken, "Member");
    }
}
