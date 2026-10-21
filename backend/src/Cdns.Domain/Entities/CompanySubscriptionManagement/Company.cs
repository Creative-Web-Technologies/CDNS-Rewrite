namespace Cdns.Domain.Entities.Company;

public class Company
{
    public int CompanyId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string TinNumber { get; set; } = null!;

    public string? ERegSystemReferenceNumber { get; set; }

    public bool IsDeleted { get; set; }

    public int CompanyStatusEnum { get; set; }
}