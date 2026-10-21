namespace Cdns.Domain.Entities.CusdecTables;

public class CusDecInspectionSectionTab
{
    public long CusDecInspectionSectionTabId { get; set; }
    public long? CusDecId { get; set; }
    public long? CusDecExternalSystemNumber { get; set; }
    public long? InsTabExternalSystemNumber { get; set; }
    public int? KeySecNumber { get; set; }
    public string? SectionName { get; set; }
    public DateTime? SectionDate { get; set; }
    public DateTime LastSyncDateTime { get; set; }

    public CusDec? CusDec { get; set; }
}