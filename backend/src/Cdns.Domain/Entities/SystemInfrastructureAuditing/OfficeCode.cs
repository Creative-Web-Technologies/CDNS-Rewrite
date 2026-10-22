namespace Cdns.Domain.Entities.SystemInfrastructureAuditing;

public class OfficeCode
{
    public int OfficeCodeId { get; set; }

    public string? OfficeName { get; set; }

    public string Code { get; set; } = null!;

    public bool IsDeleted { get; set; }
}