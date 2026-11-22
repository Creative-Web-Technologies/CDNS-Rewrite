using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cdns.Application.Feature.Subscriptions;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Microsoft.Extensions.Configuration;

namespace Cdns.Infrastructure.Services.Subscriptions;

/// <summary>
/// HMAC-SHA512 key generator/verifier for member subscriptions.
/// Data string: "MemberId!SubscriptionId!MemberSubscriptionId!ExpiryDate(yyyy-MM-dd)!PaidAmount!ActionType!SubType"
/// HMAC key   : UTF-8(secret + expiryDateString)
///
/// PaidAmount is always formatted to a fixed 2 decimals ("F2", InvariantCulture)
/// to match the member_subscriptions.paid_amount column scale (numeric(18,2)).
/// This makes the key independent of the in-memory decimal's scale: 25000m,
/// 25000.00m and 25000.0000m all hash to "25000.00", so a value copied from a
/// higher-scale source (e.g. plan amount numeric(18,4)) and then round-tripped
/// through the 2-decimal column still verifies.
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
        return FixedTimeEqualsBase64(expected, key);
    }

    public string Generate(MemberSubscription subscription)
    {
        if (subscription.ExpiryDate is null)
            throw new InvalidOperationException(
                "Cannot generate a key for a subscription without an ExpiryDate.");

        return Generate(
            subscription.MemberId,
            subscription.SubscriptionId,
            subscription.MemberSubscriptionId,
            subscription.ExpiryDate.Value,
            subscription.PaidAmount,
            subscription.SubscriptionActionTypeEnum,
            subscription.SubscriptionTypeEnum);
    }

    public bool Verify(MemberSubscription subscription)
    {
        if (subscription.ExpiryDate is null || string.IsNullOrEmpty(subscription.Key))
            return false;

        return Verify(
            subscription.MemberId,
            subscription.SubscriptionId,
            subscription.MemberSubscriptionId,
            subscription.ExpiryDate.Value,
            subscription.PaidAmount,
            subscription.SubscriptionActionTypeEnum,
            subscription.SubscriptionTypeEnum,
            subscription.Key);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Constant-time comparison of two Base64-encoded HMACs. Decodes both to bytes
    /// and uses <see cref="CryptographicOperations.FixedTimeEquals"/>. Invalid
    /// Base64 (or an empty candidate) is treated as a mismatch rather than
    /// throwing, so a malformed stored key can never leak an exception.
    /// </summary>
    private static bool FixedTimeEqualsBase64(string expectedBase64, string candidateBase64)
    {
        if (string.IsNullOrEmpty(candidateBase64))
            return false;

        byte[] expectedBytes;
        byte[] candidateBytes;
        try
        {
            expectedBytes = Convert.FromBase64String(expectedBase64);
            candidateBytes = Convert.FromBase64String(candidateBase64);
        }
        catch (FormatException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, candidateBytes);
    }

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
           $"{FormatPaidAmount(paidAmount)}!{actionTypeEnum}!{typeEnum}";

    /// <summary>
    /// The single place PaidAmount is rendered into the HMAC data string. Fixed to
    /// 2 decimals (the column scale) so the key does not depend on the decimal's
    /// in-memory scale. Every Generate/Verify path reaches here via BuildDataString.
    /// </summary>
    private static string FormatPaidAmount(decimal paidAmount)
        => paidAmount.ToString("F2", CultureInfo.InvariantCulture);

    private static string ToColomboDayString(DateTimeOffset date)
    {
        var colombo = TimeZoneInfo.ConvertTime(date, Colombo);
        return colombo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
