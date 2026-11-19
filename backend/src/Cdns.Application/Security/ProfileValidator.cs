using System.Text.RegularExpressions;

namespace Cdns.Application.Security;

public static class ProfileValidator
{
    private static readonly Regex ValidName   = new(@"^[A-Za-z\s\-'.]+$",         RegexOptions.Compiled);
    private static readonly Regex ValidEmail  = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex ValidMobile = new(@"^\+?\d{8,15}$",              RegexOptions.Compiled);
    private static readonly Regex ValidNicOld = new(@"^\d{9}[VvXx]$",             RegexOptions.Compiled);
    private static readonly Regex ValidNicNew = new(@"^\d{12}$",                  RegexOptions.Compiled);

    private static (string Code, string Field)? CheckName(string? value, string field)
    {
        var v = (value ?? "").Trim();
        return v.Length is 0 or > 50 || !ValidName.IsMatch(v) ? ("InvalidName", field) : null;
    }

    private static (string Code, string Field)? CheckEmail(string? value)
    {
        var em = (value ?? "").Trim();
        return em.Length is 0 or > 254 || !ValidEmail.IsMatch(em) ? ("InvalidEmail", "email") : null;
    }

    private static (string Code, string Field)? CheckMobile(string? value)
    {
        var mob = (value ?? "").Replace(" ", "");
        return !ValidMobile.IsMatch(mob) ? ("InvalidMobile", "mobile") : null;
    }

    private static (string Code, string Field)? CheckNic(string? value)
    {
        var n = (value ?? "").Trim();
        return !ValidNicOld.IsMatch(n) && !ValidNicNew.IsMatch(n) ? ("InvalidNic", "nic") : null;
    }

    private static (string Code, string Field)? CheckDesignation(string? value)
    {
        var v = (value ?? "").Trim();
        return v.Length is 0 or > 100 ? ("InvalidDesignation", "designation") : null;
    }

    // Used by master-activation profile fill (no email, no terms).
    public static (string Code, string Field)? ValidateProfile(
        string firstName, string lastName, string mobile, string nic, string designation) =>
        CheckName(firstName, "firstName") ??
        CheckName(lastName, "lastName")   ??
        CheckMobile(mobile)               ??
        CheckNic(nic)                     ??
        CheckDesignation(designation);

    // Used by signup registration (superset of ValidateProfile).
    public static (string Code, string Field)? ValidateSignup(
        string firstName, string lastName, string email,
        string mobile, string nic, string designation, bool agreeToTerms)
    {
        var err = CheckName(firstName, "firstName") ??
                  CheckName(lastName,  "lastName")  ??
                  CheckEmail(email)                 ??
                  CheckMobile(mobile)               ??
                  CheckNic(nic)                     ??
                  CheckDesignation(designation);

        if (err is not null) return err;
        if (!agreeToTerms) return ("TermsNotAccepted", "agreeToTerms");
        return null;
    }
}
