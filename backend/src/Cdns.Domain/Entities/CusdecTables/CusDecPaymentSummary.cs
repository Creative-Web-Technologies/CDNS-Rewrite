namespace Cdns.Domain.Entities.CusdecTables;

public class CusDecPaymentSummary
{
    public long CusDecPaymentSummaryId { get; set; }
    public string? DeclarantTinNumber { get; set; }
    public string? ConsigneeTinNumber { get; set; }
    public decimal? TotalCif { get; set; }
    public decimal? TotalTaxPaid { get; set; }
    public decimal? InspectionRecoveries { get; set; }
    public DateTime LastSyncDateTime { get; set; }
}