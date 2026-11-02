namespace Cdns.Application.Features.Signup;

public class SetPasswordRequest
{
    public string TempSignupToken { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}