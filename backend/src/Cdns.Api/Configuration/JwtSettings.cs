using System.Globalization;
using System.Text;

namespace Cdns.Api.Configuration;

public sealed class JwtSettings
{
    private JwtSettings(string secret, string issuer, string audience, int expiryMinutes)
    {
        Secret = secret;
        Issuer = issuer;
        Audience = audience;
        ExpiryMinutes = expiryMinutes;
    }

    public string Secret { get; }
    public string Issuer { get; }
    public string Audience { get; }
    public int ExpiryMinutes { get; }

    public static JwtSettings FromConfiguration(IConfiguration configuration)
    {
        var secret = ReadRequired(configuration, "Jwt:Secret", "JWT_SECRET");
        var issuer = ReadRequired(configuration, "Jwt:Issuer", "JWT_ISSUER");
        var audience = ReadRequired(configuration, "Jwt:Audience", "JWT_AUDIENCE");
        var expiryValue = ReadRequired(configuration, "Jwt:ExpiryMinutes", "JWT_EXPIRY_MINUTES");

        if (!int.TryParse(expiryValue, NumberStyles.None, CultureInfo.InvariantCulture, out var expiryMinutes)
            || expiryMinutes < 1)
        {
            throw new InvalidOperationException("Jwt:ExpiryMinutes must be a positive integer.");
        }

        if (Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException("Jwt:Secret must be at least 32 bytes for HMAC-SHA256.");
        }

        return new JwtSettings(secret, issuer, audience, expiryMinutes);
    }

    private static string ReadRequired(IConfiguration configuration, string configurationKey, string environmentKey)
    {
        var value = configuration[configurationKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            value = configuration[environmentKey];
        }

        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException(
                $"Configuration value '{configurationKey}' or environment variable '{environmentKey}' is required.");
    }
}
