namespace Cdns.Application.Features.Signup;

public class VerifyOtpRequest
{
    public string TempSignupToken { get; set; } = string.Empty;
    public int Otp { get; set; }
}