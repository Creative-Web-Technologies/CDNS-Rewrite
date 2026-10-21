using System.ComponentModel;

namespace Cdns.Domain.Entities.Company;

public enum SubscriptionRequestStatusEnum
{
    [Description("Pending")]
    Pending = 0,

    [Description("Approved")]
    Approved = 1,

    [Description("Rejected")]
    Rejected = 2,

    [Description("Cancelled")]
    Cancelled = 3
}