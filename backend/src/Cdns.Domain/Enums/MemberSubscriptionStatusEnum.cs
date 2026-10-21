using System.ComponentModel;

namespace Cdns.Domain.Entities.Company;

public enum MemberSubscriptionStatusEnum
{
    [Description("Payment Pending")]
    PaymentPending = 0,

    [Description("Activate")]
    Activate = 1,

    [Description("Deactivate")]
    Deactivate = 2,

    [Description("Suspended")]
    Suspended = 3,

    [Description("Expired")]
    Expired = 4,

    [Description("Payment Inprogress")]
    PaymentInprogress = 5
}