namespace Cdns.Domain.Entities.CusdecTables;

public class CusDecStatus
{
    public long CusDecStatusId { get; set; }
    public long? CusDecId { get; set; }
    public Guid? CusDecGlobalIdentity { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public DateTime? StatusDateTime { get; set; }
    public DateTime LastSyncDateTime { get; set; }
    public long? ExternalSystemNumber { get; set; }
    public int? CusDecStatusNumber { get; set; }
    public long? CusDecExternalSystemNumber { get; set; }
    public string? TableName { get; set; }

    // Navigation
    public CusDec? CusDec { get; set; }
}