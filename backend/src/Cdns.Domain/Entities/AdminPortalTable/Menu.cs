namespace Cdns.Domain.Entities.AdminPortal;

public class Menu
{
    public int MenuId { get; set; }

    public int? ModuleId { get; set; }

    public string? MenuName { get; set; }

    public string? Area { get; set; }

    public string? Controller { get; set; }

    public string? Action { get; set; }

    public string? IconName { get; set; }

    public int MenuOrder { get; set; }

    public bool IsDeleted { get; set; }

    // Navigation property to Module
    public Module? Module { get; set; }
}