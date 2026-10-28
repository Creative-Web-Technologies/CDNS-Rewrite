namespace Cdns.Domain.Entities.AdminPortal;

public class Module
{
    public int ModuleId { get; set; }

    public string? ModuleName { get; set; }

    public string? Area { get; set; }

    public string? Controller { get; set; }

    public string? Action { get; set; }

    public string? IconName { get; set; }

    public string? SmallIconName { get; set; }

    public string? BgColorClass { get; set; }

    public int ModuleOrder { get; set; }

    public bool IsDeleted { get; set; }
}