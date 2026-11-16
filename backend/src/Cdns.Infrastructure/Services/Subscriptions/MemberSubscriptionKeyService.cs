using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cdns.Application.Feature.Subscriptions;
using Microsoft.Extensions.Configuration;

namespace Cdns.Infrastructure.Services.Subscriptions;

/// <summary>
/// HMAC-SHA512 key generator/verifier for member subscriptions.
/// Data string: "MemberId!SubscriptionId!MemberSubscriptionId!ExpiryDate(yyyy-MM-dd)!PaidAmount!ActionType!SubType"
/// HMAC key   : UTF-8(secret + expiryDateString)
/// </summary>
public sealed class MemberSubscriptionKeyService : IMemberSubscriptionKeyService
{
    private static readonly TimeZoneInfo Colombo =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");

    private readonly string _secret;

    public MemberSubscriptionKeyService(IConfiguration configuration)
    {
        _secret = configuration["Cdns:MemberSubscriptionSecret"]
            ?? throw new InvalidOperationException(
                "Configuration key 'Cdns:MemberSubscriptionSecret' is required.");
    }

    public string Generate(
        long memberId,
        int subscriptionId,
        long memberSubscriptionId,
        DateTimeOffset expiryDate,
        decimal paidAmount,
        int subscriptionActionTypeEnum,
        int subscriptionTypeEnum)
    {
        var expiryDateStr = ToColomboDayString(expiryDate);
        var data = BuildDataString(memberId, subscriptionId, memberSubscriptionId,
            expiryDateStr, paidAmount, subscriptionActionTypeEnum, subscriptionTypeEnum);
        return Hmac(data, expiryDateStr);
    }

    public bool Verify(
        long memberId,
        int subscriptionId,
        long memberSubscriptionId,
        DateTimeOffset expiryDate,
        decimal paidAmount,
        int subscriptionActionTypeEnum,
        int subscriptionTypeEnum,
        string key)
    {
        var expected = Generate(memberId, subscriptionId, memberSubscriptionId,
            expiryDate, paidAmount, subscriptionActionTypeEnum, subscriptionTypeEnum);
        return string.Equals(expected, key, StringComparison.Ordinal);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string Hmac(string data, string expiryDateStr)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_secret + expiryDateStr);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA512(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToBase64String(hash);
    }

    private static string BuildDataString(
        long memberId,
        int subscriptionId,
        long memberSubscriptionId,
        string expiryDateStr,
        decimal paidAmount,
        int actionTypeEnum,
        int typeEnum)
        => $"{memberId}!{subscriptionId}!{memberSubscriptionId}!{expiryDateStr}!" +
           $"{paidAmount.ToString(CultureInfo.InvariantCulture)}!{actionTypeEnum}!{typeEnum}";

    private static string ToColomboDayString(DateTimeOffset date)
    {
        var colombo = TimeZoneInfo.ConvertTime(date, Colombo);
        return colombo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
