namespace Cdns.Api.Endpoints.Auth;

public record LoginRequest(string UserName, string Password);

public record CompleteProfileHttpRequest(
    Guid ActivationToken,
    string FirstName,
    string LastName,
    string Mobile,
    string Nic,
    string Designation);

public record SetActivationPasswordHttpRequest(Guid ActivationToken, string Password);

public record ForgotPasswordHttpRequest(string UserName);

public record VerifyOtpHttpRequest(string UserName, string Otp);

public record ResetPasswordHttpRequest(string UserName, string Otp, string NewPassword);
