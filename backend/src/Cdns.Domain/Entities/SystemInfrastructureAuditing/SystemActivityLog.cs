using Cdns.Domain.Entities.AdminPortal; 

namespace Cdns.Domain.Entities.SystemInfrastructureAuditing;

public class SystemActivityLog
{
    public long SystemActivityLogId { get; set; }

    public string? Activity { get; set; }

    public string? ActivityJson { get; set; }

    public string? StoredProcedureName { get; set; }

    public DateTimeOffset? LogDateTime { get; set; }

    public long? EmployeeId { get; set; }

    // Navigation property
    public Employee? Employee { get; set; }
}