namespace Cdns.Application.Features.Signup;

public class VerifyTinResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TempSignupToken { get; set; }
}