using System.ComponentModel;

namespace Cdns.Domain.Entities.Company;

public enum SubscriptionDurationTypeEnum
{
    [Description("Days")]
    Days = 0,

    [Description("Months")]
    Months = 1,

    [Description("Years")]
    Years = 2
}
