namespace Cdns.Application.Auth;

public abstract record SendOtpResult
{
    public sealed record Sent : SendOtpResult;
    public sealed record UserNotFound : SendOtpResult;
}

public abstract record VerifyOtpResult
{
    public sealed record Valid : VerifyOtpResult;
    public sealed record Invalid : VerifyOtpResult;
}

public abstract record ResetPasswordResult
{
    public sealed record Success : ResetPasswordResult;
    public sealed record InvalidOtp : ResetPasswordResult;
    public sealed record UserNotFound : ResetPasswordResult;
    public sealed record WeakPassword : ResetPasswordResult;
}

public interface IPasswordResetService
{
    Task<SendOtpResult> SendOtpAsync(string userName, CancellationToken ct = default);
    Task<VerifyOtpResult> VerifyOtpAsync(string userName, int otp, CancellationToken ct = default);
    Task<ResetPasswordResult> ResetPasswordAsync(string userName, int otp, string newPassword, CancellationToken ct = default);
}
