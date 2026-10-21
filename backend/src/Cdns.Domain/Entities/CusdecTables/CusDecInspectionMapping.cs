namespace Cdns.Domain.Entities.CusdecTables;

public class CusDecInspectionMapping
{
    public int CusDecInspectionMappingId { get; set; }
    public string? AsycudaFrdNfr { get; set; }
    public string? Description { get; set; }
    public bool IsShow { get; set; } = true;
}