using Cdns.Domain.Entities.Company;

namespace Cdns.Application.Feature.Subscriptions;

/// <summary>
/// Single source of truth for turning a <see cref="SubscriptionDurationTypeEnum"/>
/// value into display text. Both the member active-list formatter and the master
/// plan-picker label delegate here, so Lifetime support and the throw-on-unknown
/// rule can never drift between the two callers.
/// Unknown or null duration types throw: a silently-wrong label is not acceptable.
/// </summary>
public static class SubscriptionDurationFormat
{
    /// <summary>
    /// The bare duration-type name: "Days" / "Months" / "Years" / "Lifetime".
    /// Used by the plan picker where the count is shown separately.
    /// </summary>
    public static string Label(int? durationType) => durationType switch
    {
        (int)SubscriptionDurationTypeEnum.Days     => "Days",
        (int)SubscriptionDurationTypeEnum.Months   => "Months",
        (int)SubscriptionDurationTypeEnum.Years    => "Years",
        (int)SubscriptionDurationTypeEnum.Lifetime => "Lifetime",
        _ => throw new ArgumentOutOfRangeException(
            nameof(durationType), durationType, "Unknown subscription duration type."),
    };

    /// <summary>
    /// A full human description: "Lifetime", or a pluralised "{count} unit"
    /// (e.g. "1 month", "12 months"). Used by the member active-subscription list.
    /// </summary>
    public static string Describe(int count, int? durationType) => durationType switch
    {
        (int)SubscriptionDurationTypeEnum.Lifetime => "Lifetime",
        (int)SubscriptionDurationTypeEnum.Days     => $"{count} {(count == 1 ? "day" : "days")}",
        (int)SubscriptionDurationTypeEnum.Months   => $"{count} {(count == 1 ? "month" : "months")}",
        (int)SubscriptionDurationTypeEnum.Years    => $"{count} {(count == 1 ? "year" : "years")}",
        _ => throw new ArgumentOutOfRangeException(
            nameof(durationType), durationType, "Unknown subscription duration type."),
    };
}
