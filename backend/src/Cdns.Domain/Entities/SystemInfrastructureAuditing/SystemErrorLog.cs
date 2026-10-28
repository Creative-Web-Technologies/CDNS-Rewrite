using Cdns.Domain.Entities.AdminPortal;

namespace Cdns.Domain.Entities.SystemInfrastructureAuditing;

public class SystemErrorLog
{
    public long SystemErrorLogId { get; set; }

    public string? Activity { get; set; }

    public string? Error { get; set; }

    public string? Description { get; set; }

    public string? StoredProcedureName { get; set; }

    public DateTimeOffset? LogDateTime { get; set; }

    public long? EmployeeId { get; set; }

    // Navigation property
    public Employee? Employee { get; set; }
}