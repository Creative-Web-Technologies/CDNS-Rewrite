using System.Security.Cryptography;
using Cdns.Application.Auth;
using Cdns.Application.Interfaces;
using Cdns.Application.Security;
using Cdns.Infrastructure.Persistence;
using Cdns.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Cdns.Infrastructure.Services.Auth;

public class PasswordResetService : IPasswordResetService
{
    private readonly CdnsDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailService _email;
    private readonly IMemoryCache _cache;

    public PasswordResetService(CdnsDbContext db, IPasswordHasher hasher, IEmailService email, IMemoryCache cache)
    {
        _db = db;
        _hasher = hasher;
        _email = email;
        _cache = cache;
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

    public async Task<VerifyOtpResult> VerifyOtpAsync(string userName, string otp, CancellationToken ct = default)
    {
        if (!OtpPolicy.IsValidFormat(otp) || !int.TryParse(otp, out var otpInt))
            return new VerifyOtpResult.Invalid();

        var memberUser = await _db.MemberUsers
            .FirstOrDefaultAsync(u => u.UserName == userName, ct);

        if (memberUser is null || !memberUser.IsActiveOtp || memberUser.ActivationCode != otpInt)
            return new VerifyOtpResult.Invalid();

        memberUser.IsWrongAttempt = false; // unlock account on valid OTP
        await _db.SaveChangesAsync(ct);

        return new VerifyOtpResult.Valid();
    }

    public async Task<ResetPasswordResult> ResetPasswordAsync(
        string userName, string otp, string newPassword, CancellationToken ct = default)
    {
        if (!OtpPolicy.IsValidFormat(otp) || !int.TryParse(otp, out var otpInt))
            return new ResetPasswordResult.InvalidOtp();

        var memberUser = await _db.MemberUsers
            .FirstOrDefaultAsync(u => u.UserName == userName, ct);

        if (memberUser is null)
            return new ResetPasswordResult.UserNotFound();

        if (!memberUser.IsActiveOtp || memberUser.ActivationCode != otpInt)
            return new ResetPasswordResult.InvalidOtp();

        if (!PasswordPolicy.IsValid(newPassword))
            return new ResetPasswordResult.WeakPassword();

        var sessions = await _db.MemberLoginSessions
            .Where(s => s.MemberId == memberUser.MemberId)
            .ToListAsync(ct);
        _db.MemberLoginSessions.RemoveRange(sessions);

        var (hash, salt) = _hasher.HashNewPassword(newPassword);
        memberUser.Password = hash;
        memberUser.PasswordSalt = salt;
        memberUser.IsActiveOtp = false;
        memberUser.ActivationCode = 0;
        memberUser.IsWrongAttempt = false;

        await _db.SaveChangesAsync(ct);

        _cache.Remove(AuthCacheKeys.WrongAttempts(memberUser.UserName));

        return new ResetPasswordResult.Success();
    }
}
