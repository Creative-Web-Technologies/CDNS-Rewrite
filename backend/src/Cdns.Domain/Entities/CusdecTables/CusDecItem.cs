namespace Cdns.Domain.Entities.CusdecTables;

public class CusDecItem
{
    public long CusDecItemId { get; set; }
    public long? CusDecId { get; set; }
    public Guid? CusDecGlobalIdentity { get; set; }
    public Guid? GlobalIdentity { get; set; }
    public string? HsCode { get; set; }
    public string? Description { get; set; }
    public DateTime LastSyncDate { get; set; }
    public string? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TotalValue { get; set; }
    public long? CusDecExternalSystemNumber { get; set; }
    public int? CusDecItemNumber { get; set; }
    public string? BlNumber { get; set; }

    // Navigation
    public CusDec? CusDec { get; set; }
}