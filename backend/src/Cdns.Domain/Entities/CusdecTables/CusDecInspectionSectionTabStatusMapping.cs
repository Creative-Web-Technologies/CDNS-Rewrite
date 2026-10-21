namespace Cdns.Domain.Entities.CusdecTables;

public class CusDecInspectionSectionTabStatusMapping
{
    public int CusDecInspectionSectionTabStatusMappingId { get; set; }
    public string? AsycudaSectionName { get; set; }
    public string? CdnsSectionName { get; set; }
    public bool IsShow { get; set; } = true;
    public string? OfficeCode { get; set; }
}