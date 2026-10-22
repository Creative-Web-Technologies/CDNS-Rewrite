namespace Cdns.Domain.Entities.SystemInfrastructureAuditing;

public class TaskScheduleAppDetail
{
    public int TaskScheduleAppDetailId { get; set; }

    public Guid AppId { get; set; }

    public string AppName { get; set; } = null!;

    public DateTimeOffset? LastRunDateTime { get; set; }
}