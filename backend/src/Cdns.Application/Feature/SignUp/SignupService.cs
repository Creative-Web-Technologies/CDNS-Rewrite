namespace Cdns.Application.Features.Signup;

public interface ISignupService
{
    Task<VerifyTinResponse> VerifyTinAsync(string tin);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<bool> VerifyOtpAsync(VerifyOtpRequest request);
    Task<bool> SetPasswordAsync(SetPasswordRequest request);
}