using System.Text.RegularExpressions;

namespace Cdns.Application.Security;

public static class OtpPolicy
{
    private static readonly Regex ValidFormat = new(@"^\d{6}$", RegexOptions.Compiled);

    // True only for exactly 6 decimal digits — matches the generator range [100000, 999999].
    // Rejects empty strings, leading +/-, spaces, wrong lengths, and non-numeric characters.
    public static bool IsValidFormat(string? otp) => otp is not null && ValidFormat.IsMatch(otp);
}
