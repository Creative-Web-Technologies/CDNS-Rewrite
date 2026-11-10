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

    public async Task<VerifyTinResponse> VerifyTinAsync(string tin)
    {
        var status = await _dbContext.Database
            .SqlQueryRaw<int>("SELECT check_tin_signup_status({0}) AS \"Value\"", tin)
            .FirstOrDefaultAsync();

        var response = new VerifyTinResponse { StatusCode = status };

        switch (status)
        {
            case 1:
                response.Message = "TIN verified successfully.";
                break;
            case 2:
                response.Message = "This company's registration is pending eReg verification. Please wait for your company administrator to complete the eReg process.";
                break;
            case 3:
                response.Message = "This company's registration has not yet been activated. Please contact your company administrator.";
                break;
            case 4:
                response.Message = "Invalid TIN. Must be at least 9 characters.";
                break;
            case 5:
                response.Message = "This company's account setup is incomplete. Please contact your company administrator or CDNS support.";
                break;
            case 6:
                response.Message = "Company not found. Please check your TIN.";
                break;
            default:
                response.Message = "An unexpected error occurred. Please try again or contact support.";
                break;
        }

        return response;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        int otp = RandomNumberGenerator.GetInt32(100000, 999999);
        string newGlobalIdentity = Guid.NewGuid().ToString();

        var tempMember = new TemporaryMember
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Mobile = request.Mobile,
            Nic = request.Nic,
            Designation = request.Designation,
            OneTimePassword = otp,
            CreatedDateTime = DateTime.UtcNow,
            GlobalIdentity = newGlobalIdentity
        };

        _dbContext.TemporaryMembers.Add(tempMember);
        await _dbContext.SaveChangesAsync();

        // OTP is intentionally not logged — check the member's email or query temporary_members directly for dev testing
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

        var tempMember = await _dbContext.TemporaryMembers
            .FirstOrDefaultAsync(x => x.GlobalIdentity == globalId);

        if (tempMember == null)
        {
            _logger.LogWarning("VerifyOtp: no TemporaryMember for token {GlobalIdentity}", globalId);
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
            .FirstOrDefaultAsync(x => x.GlobalIdentity == globalId);

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
        return new SignupSetPasswordResult.Success(sessionToken);
    }
}
