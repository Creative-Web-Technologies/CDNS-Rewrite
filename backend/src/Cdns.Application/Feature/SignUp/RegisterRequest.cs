namespace Cdns.Application.Features.Signup;

public class RegisterRequest
{
    public string Tin { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Nic { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
}