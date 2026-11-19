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

        var expiryLocal = durationTypeEnum switch
        {
            (int)SubscriptionDurationTypeEnum.Months => startLocal.AddMonths(noOfActiveDuration),
            (int)SubscriptionDurationTypeEnum.Years  => startLocal.AddYears(noOfActiveDuration),
            _  /* Days (0) or null */                => startLocal.AddDays(noOfActiveDuration),
        };

        return new DateTimeOffset(expiryLocal, Colombo.GetUtcOffset(expiryLocal));
    }
}
