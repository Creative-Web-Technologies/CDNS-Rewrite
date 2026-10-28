using System.ComponentModel;

namespace Cdns.Domain.Entities.Company;

public enum CompanyStatusEnum
{
    [Description("Inprogress")]
    Inprogress = 0,

    [Description("Active")]
    Active = 1,

    [Description("Suspended")]
    Suspended = 2
}