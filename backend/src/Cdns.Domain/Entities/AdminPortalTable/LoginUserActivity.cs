namespace Cdns.Domain.Entities.AdminPortal;

public class LoginUserActivity
{
    public long LoginUserActivityId { get; set; }

    public long UserId { get; set; }

    public Guid GlobalIdentity { get; set; }

    public string? LoggedIpAddress { get; set; }

    public string? LoggedDevice { get; set; }

    public DateTimeOffset LoggedDateTime { get; set; }

    public bool IsCurrentSession { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}