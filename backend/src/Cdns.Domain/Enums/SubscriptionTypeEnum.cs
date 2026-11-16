using System.ComponentModel;

namespace Cdns.Domain.Entities.Company;

public enum SubscriptionTypeEnum
{
    [Description("Declarant")]
    Declarant = 1,

    [Description("Consignee")]
    Consignee = 2
}