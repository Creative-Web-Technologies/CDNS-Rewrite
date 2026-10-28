namespace Cdns.Domain.Entities.AdminPortal;

public class Department
{
    public int DepartmentId { get; set; }

    public string? DepartmentName { get; set; }

    public bool IsDeleted { get; set; }
}