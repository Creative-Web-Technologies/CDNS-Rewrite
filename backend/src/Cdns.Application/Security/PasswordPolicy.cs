using System.Text.RegularExpressions;

namespace Cdns.Application.Security;

public static class PasswordPolicy
{
    private static readonly Regex HasUpper   = new(@"[A-Z]",  RegexOptions.Compiled);
    private static readonly Regex HasLower   = new(@"[a-z]",  RegexOptions.Compiled);
    private static readonly Regex HasDigit   = new(@"[0-9]",  RegexOptions.Compiled);
    private static readonly Regex HasSpecial = new(@"[^A-Za-z0-9]", RegexOptions.Compiled);

    public static bool IsValid(string password) =>
        password.Length is >= 8 and <= 128
        && HasUpper.IsMatch(password)
        && HasLower.IsMatch(password)
        && HasDigit.IsMatch(password)
        && HasSpecial.IsMatch(password);
}
