using System.Security.Cryptography;
using Cdns.Application.Auth;
using Cdns.Application.Interfaces;
using Cdns.Application.Security;
using Cdns.Infrastructure.Persistence;
using Cdns.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Services.Auth;

public class PasswordResetService : IPasswordResetService
{
    private readonly CdnsDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailService _email;

    public PasswordResetService(CdnsDbContext db, IPasswordHasher hasher, IEmailService email)
    {
        _db = db;
        _hasher = hasher;
        _email = email;
    }

    public async Task<SendOtpResult> SendOtpAsync(string userName, CancellationToken ct = default)
    {
        var memberUser = await _db.MemberUsers
            .Include(u => u.Member)
            .FirstOrDefaultAsync(u => u.UserName == userName, ct);

        if (memberUser is null)
            return new SendOtpResult.UserNotFound();

        var otp = RandomNumberGenerator.GetInt32(100_000, 1_000_000); // [100000, 999999]
        memberUser.ActivationCode = otp;
        memberUser.IsActiveOtp = true;
        memberUser.LastOtpGeneratedDateTime = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Fall back to UserName (email) if Member.Email is not populated
        var toEmail = memberUser.Member.Email ?? memberUser.UserName;
        await _email.SendOtpAsync(toEmail, otp, ct);

        return new SendOtpResult.Sent();
    }

    public async Task<VerifyOtpResult> VerifyOtpAsync(string userName, int otp, CancellationToken ct = default)
    {
        var memberUser = await _db.MemberUsers
            .FirstOrDefaultAsync(u => u.UserName == userName, ct);

        if (memberUser is null || !memberUser.IsActiveOtp || memberUser.ActivationCode != otp)
            return new VerifyOtpResult.Invalid();

        memberUser.IsWrongAttempt = false; // unlock account on valid OTP
        await _db.SaveChangesAsync(ct);

        return new VerifyOtpResult.Valid();
    }

    public async Task<ResetPasswordResult> ResetPasswordAsync(
        string userName, int otp, string newPassword, CancellationToken ct = default)
    {
        var memberUser = await _db.MemberUsers
            .FirstOrDefaultAsync(u => u.UserName == userName, ct);

        if (memberUser is null)
            return new ResetPasswordResult.UserNotFound();

        if (!memberUser.IsActiveOtp || memberUser.ActivationCode != otp)
            return new ResetPasswordResult.InvalidOtp();

        if (!PasswordPolicy.IsValid(newPassword))
            return new ResetPasswordResult.WeakPassword();

        var (hash, salt) = _hasher.HashNewPassword(newPassword);
        memberUser.Password = hash;
        memberUser.PasswordSalt = salt;
        memberUser.IsActiveOtp = false;
        memberUser.ActivationCode = 0;
        memberUser.IsWrongAttempt = false;
        await _db.SaveChangesAsync(ct);

        return new ResetPasswordResult.Success();
    }
}
