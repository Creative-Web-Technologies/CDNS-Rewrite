namespace Cdns.Domain.Entities.CusdecTables;

public class CusDecInspection
{
    public long CusDecInspectionId { get; set; }
    public string? IssueCode { get; set; }
    public string? Description { get; set; }
    public decimal? Value { get; set; }
    public long? CusDecId { get; set; }
    public long? CusDecExternalSystemNumber { get; set; }
    public DateTime LastSyncDateTime { get; set; }
    public long? InspectionInstanceId { get; set; }
    public int? KeyFrdNumber { get; set; }
    public string? FrdNfr { get; set; }
    public string? FrdUni { get; set; }
    public string? FrdUbr { get; set; }

    public CusDec? CusDec { get; set; }
}