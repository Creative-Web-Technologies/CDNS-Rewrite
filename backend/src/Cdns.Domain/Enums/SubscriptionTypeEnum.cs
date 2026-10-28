using System.ComponentModel;

namespace Cdns.Domain.Entities.Company;

public enum SubscriptionTypeEnum
{
    [Description("Declarent")]
    Declarent = 1,

    [Description("Consignee")]
    Consignee = 2
}