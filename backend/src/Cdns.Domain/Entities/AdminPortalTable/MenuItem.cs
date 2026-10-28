namespace Cdns.Domain.Entities.AdminPortal;

public class MenuItem
{
    public int MenuItemId { get; set; }

    public int? MenuId { get; set; }

    public string? MenuItemName { get; set; }

    public string? Area { get; set; }

    public string? Controller { get; set; }

    public string? Action { get; set; }

    public string? IconName { get; set; }

    public int MenuItemOrder { get; set; }

    public bool IsDeleted { get; set; }

    public string? WorkSpaceId { get; set; }

    public string? ReportId { get; set; }

    public bool IsPowerBiDashboard { get; set; }

    // Navigation property to Menu
    public Menu? Menu { get; set; }
}