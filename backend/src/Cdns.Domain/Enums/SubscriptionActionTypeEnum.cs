using System.ComponentModel;

namespace Cdns.Domain.Entities.Company;

public enum SubscriptionActionTypeEnum
{
    [Description("General")]
    General = 0,

    [Description("Only For Acknowledgement")]
    OnlyForAcknowledgement = 1
}
