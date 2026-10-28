namespace Cdns.Domain.Entities.CusdecTables;

public class CusDecContainerStatus
{
    public long CusDecContainerStatusId { get; set; }
    public long? CusDecContainerId { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public DateTime? StatusDateTime { get; set; }
    public DateTime LastSyncDate { get; set; }
    public long? CusDecExternalSystemNumber { get; set; }
    public string? ContainerNumber { get; set; }
    public long? ExternalSystemNumber { get; set; }
    public int? ContainerStatusNumber { get; set; }

    // Navigation
    public CusDecContainer? CusDecContainer { get; set; }
}