namespace Cdns.Application.Features.Signup;

public abstract record SignupVerifyOtpResult
{
    public sealed record Success : SignupVerifyOtpResult;
    public sealed record InvalidToken : SignupVerifyOtpResult;
}

public abstract record SignupSetPasswordResult
{
    public sealed record Success(string SessionToken) : SignupSetPasswordResult;
    public sealed record InvalidToken : SignupSetPasswordResult;
}

public interface ISignupService
{
    Task<VerifyTinResponse> VerifyTinAsync(string tin);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<SignupVerifyOtpResult> VerifyOtpAsync(VerifyOtpRequest request);
    Task<SignupSetPasswordResult> SetPasswordAsync(SetPasswordRequest request);
}