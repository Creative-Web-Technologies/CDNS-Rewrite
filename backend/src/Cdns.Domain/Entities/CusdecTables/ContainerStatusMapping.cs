
namespace Cdns.Domain.Entities.CusdecTables;

public class ContainerStatusMapping
{
    public int ContainerStatusMappingId { get; set; }
    public string AsycudaStatus { get; set; } = string.Empty;
    public string AsycudaDescription { get; set; } = string.Empty;
    public string CdnsStatus { get; set; } = string.Empty;
    public string? CdnsDescription { get; set; }
    public bool IsShow { get; set; } = true;

    // Database-generated normalised keys (lower + btrim of the source columns).
    // Must never be assigned in application code.
    public string AsycudaStatusKey { get; private set; } = string.Empty;
    public string AsycudaDescriptionKey { get; private set; } = string.Empty;
}
