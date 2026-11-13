namespace Cdns.Infrastructure.Services.Auth;

internal static class AuthCacheKeys
{
    internal static string WrongAttempts(string userName) =>
        $"wrong-attempts:{userName.Trim().ToLowerInvariant()}";
}
