using System.ComponentModel;

namespace Cdns.Domain.Entities.Company;

public enum SubscriptionStatusEnum
{
    [Description("Draft")]
    Draft = 0,

    [Description("Active")]
    Active = 1,

    [Description("Inactive")]
    Inactive = 2
}
