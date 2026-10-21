namespace Cdns.Domain.Entities.CusdecTables;

public class CusDecTax
{
    public long CusDecTaxId { get; set; }
    public long? CusDecId { get; set; }
    public string? TaxCode { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime LastSyncDate { get; set; }
    public long? CusDecExternalSystemNumber { get; set; }
    public int? ItemNumber { get; set; }
    public int? TaxRank { get; set; }

    public CusDec? CusDec { get; set; }
}