
namespace Cdns.Domain.Entities.CusdecTables;

public class CusDecStatusMapping
{
    public int CusDecStatusMappingId { get; set; }
    public string? AsycudaStatus { get; set; }
    public string? AsycudaDescription { get; set; }
    public string? CdnsStatus { get; set; }
    public string? CdnsDescription { get; set; }
    public bool IsShow { get; set; } = true;
    public string? OfficeCode { get; set; }
}