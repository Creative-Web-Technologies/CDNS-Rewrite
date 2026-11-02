namespace Cdns.Application.Interfaces;

public interface IEmailService
{
    Task SendOtpAsync(string toEmail, int otp, CancellationToken ct = default);
}
