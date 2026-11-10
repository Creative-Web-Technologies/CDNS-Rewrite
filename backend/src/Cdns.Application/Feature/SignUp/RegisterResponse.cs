namespace Cdns.Application.Features.Signup;
public class RegisterResponse
{
    public bool Success { get; set; }
    public string? Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public string TempSignupToken { get; set; } = string.Empty;
}