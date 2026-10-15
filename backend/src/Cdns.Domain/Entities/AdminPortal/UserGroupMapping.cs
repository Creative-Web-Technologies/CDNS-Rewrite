namespace Cdns.Domain.Entities.AdminPortal;

public class UserGroupMapping
{
    public int UserGroupMappingId { get; set; }

    public long EmployeeId { get; set; }

    public int GroupId { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;

    public Group Group { get; set; } = null!;
}