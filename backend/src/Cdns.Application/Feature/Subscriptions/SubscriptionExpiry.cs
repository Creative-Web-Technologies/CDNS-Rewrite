using Cdns.Domain.Entities.Company;

namespace Cdns.Application.Feature.Subscriptions;

/// <summary>
/// Pure, database-free helper for computing subscription expiry dates using
/// the Asia/Colombo timezone.  Extracted here so the logic can be unit-tested
/// without an EF context or infrastructure dependency.
/// </summary>
public static class SubscriptionExpiry
{
    private static readonly TimeZoneInfo Colombo =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");

    /// <summary>
    /// Returns Colombo midnight of the day that is
    /// <paramref name="noOfActiveDuration"/> units after the Colombo-local date
    /// of <paramref name="startDate"/>.
    /// </summary>
    public static DateTimeOffset ComputeExpiryDate(
        DateTimeOffset startDate,
        int noOfActiveDuration,
        int? durationTypeEnum)
    {
        var colomboStart = TimeZoneInfo.ConvertTime(startDate, Colombo);
        var startLocal = new DateTime(
            colomboStart.Year, colomboStart.Month, colomboStart.Day,
            0, 0, 0, DateTimeKind.Unspecified);

        // Explicit cases only — an unknown or null duration type is a data/logic
        // error and must surface, not silently fall back to AddDays.
        var expiryLocal = durationTypeEnum switch
        {
            (int)SubscriptionDurationTypeEnum.Days     => startLocal.AddDays(noOfActiveDuration),
            (int)SubscriptionDurationTypeEnum.Months   => startLocal.AddMonths(noOfActiveDuration),
            (int)SubscriptionDurationTypeEnum.Years    => startLocal.AddYears(noOfActiveDuration),
            // Lifetime matches legacy: start + 500 years.
            (int)SubscriptionDurationTypeEnum.Lifetime => startLocal.AddYears(500),
            _ => throw new ArgumentOutOfRangeException(
                nameof(durationTypeEnum), durationTypeEnum,
                "Unknown subscription duration type."),
        };

        return new DateTimeOffset(expiryLocal, Colombo.GetUtcOffset(expiryLocal));
    }
}
