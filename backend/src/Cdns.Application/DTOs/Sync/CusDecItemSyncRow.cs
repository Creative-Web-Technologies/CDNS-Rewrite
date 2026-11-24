namespace Cdns.Application.DTOs.Sync;

public class CusDecItemSyncRow
{
    public long CusDecExternalSystemNumber { get; set; }
    public int CusDecItemNumber { get; set; }
    public string? HsCode { get; set; }
    public string? Description { get; set; }
    public string? Quantity { get; set; }
    public decimal? TotalValue { get; set; }
    public decimal? UnitPrice { get; set; } // never populated from Oracle, kept for schema completeness
    public string? BlNumber { get; set; }
    public Guid GlobalIdentity { get; set; }
}