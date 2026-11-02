using Cdns.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cdns.Infrastructure.Notifications;

// Stub until a real SMTP/SendGrid implementation is wired.
public class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ILogger<LoggingEmailService> logger) => _logger = logger;

    public Task SendOtpAsync(string toEmail, int otp, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL STUB] OTP {Otp} → {Email}", otp, toEmail);
        return Task.CompletedTask;
    }
}
