namespace Cdns.Domain.Entities.AdminPortal;

public class AccessControl
{
    public long AccessControlId { get; set; }

    public int? GroupId { get; set; }

    public int? MenuItemFunctionId { get; set; }

    public bool IsDeleted { get; set; }

    // Navigation properties
    public Group? Group { get; set; }

    public MenuItemFunction? MenuItemFunction { get; set; }
}