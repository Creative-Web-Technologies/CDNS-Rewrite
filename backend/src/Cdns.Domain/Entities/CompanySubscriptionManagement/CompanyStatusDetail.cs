namespace Cdns.Domain.Entities.Company;

public class CompanyStatusDetail
{
    public long CompanyStatusDetailId { get; set; }

    public int CompanyId { get; set; }

    public int CompanyStatusEnum { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }

    public long? EmployeeId { get; set; }

    // Navigation property to Company
    public Company Company { get; set; } = null!;
}