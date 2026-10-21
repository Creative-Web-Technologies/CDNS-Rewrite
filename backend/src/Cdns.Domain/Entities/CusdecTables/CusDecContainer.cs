namespace Cdns.Domain.Entities.CusdecTables;

public class CusDecContainer
{
    public long CusDecContainerId { get; set; }
    public long? CusDecId { get; set; }
    public Guid? CusDecGlobalIdentity { get; set; }
    public Guid? GlobalIdentity { get; set; }
    public string? Container { get; set; }
    public string? Location { get; set; }
    public string? CurrentStatus { get; set; }
    public DateTime LastSyncDate { get; set; }
    public string? Size { get; set; }
    public string? Type { get; set; }
    public DateTime? ArrivalDate { get; set; }
    public string? Voyage { get; set; }
    public string? Vessel { get; set; }
    public long? CusDecExternalSystemNumber { get; set; }
    public int? ContainerKey { get; set; }
    public int ContainerPassNumber { get; set; }

    // Navigation
    public CusDec? CusDec { get; set; }
}