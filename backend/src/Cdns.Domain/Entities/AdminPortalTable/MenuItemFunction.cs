namespace Cdns.Domain.Entities.AdminPortal;

public class MenuItemFunction
{
    public int MenuItemFunctionId { get; set; }

    public int? MenuItemId { get; set; }

    public string? MenuItemFunctionName { get; set; }

    public string? Area { get; set; }

    public string? Controller { get; set; }

    public string? Action { get; set; }

    public string? IconName { get; set; }

    public int MenuItemFunctionOrder { get; set; }

    public bool IsDeleted { get; set; }

    // Navigation property to MenuItem
    public MenuItem? MenuItem { get; set; }
}