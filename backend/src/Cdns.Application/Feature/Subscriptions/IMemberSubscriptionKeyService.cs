namespace Cdns.Application.Feature.Subscriptions;

/// <summary>
/// Generates and verifies the HMAC-SHA512 key stored on
/// <see cref="Cdns.Domain.Entities.CompanySubscriptionManagement.MemberSubscription.Key"/>.
/// </summary>
public interface IMemberSubscriptionKeyService
{
    /// <summary>
    /// Generates a Base64-encoded HMAC-SHA512 key for a member subscription.
    /// The HMAC key is <c>secret + expiryDateString</c> where <c>expiryDateString</c>
    /// is the ExpiryDate formatted as <c>yyyy-MM-dd</c> in Colombo local time.
    /// </summary>
    string Generate(
        long memberId,
        int subscriptionId,
        long memberSubscriptionId,
        DateTimeOffset expiryDate,
        decimal paidAmount,
        int subscriptionActionTypeEnum,
        int subscriptionTypeEnum);

    /// <summary>
    /// Returns <c>true</c> when the provided <paramref name="key"/> matches
    /// what <see cref="Generate"/> would produce for the same arguments.
    /// </summary>
    bool Verify(
        long memberId,
        int subscriptionId,
        long memberSubscriptionId,
        DateTimeOffset expiryDate,
        decimal paidAmount,
        int subscriptionActionTypeEnum,
        int subscriptionTypeEnum,
        string key);
}
