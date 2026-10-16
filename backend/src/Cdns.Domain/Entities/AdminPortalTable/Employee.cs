namespace Cdns.Domain.Entities.AdminPortal;

public class Employee
{
    public long EmployeeId { get; set; }

    public int? DepartmentId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? EmployeeCode { get; set; }

    public string? ProfileImageUrl { get; set; }

    public string? Email { get; set; }

    public bool IsDeleted { get; set; }

    // Navigation property
    public Department? Department { get; set; }
}