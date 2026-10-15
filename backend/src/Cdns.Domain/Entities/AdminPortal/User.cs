namespace Cdns.Domain.Entities.AdminPortal;

public class User
{
    public long EmployeeId { get; set; } // PK and FK (shared with Employee)

    public string UserName { get; set; } = null!;

    public string? Password { get; set; }

    public string? PasswordSalt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public int? ActivationCode { get; set; }

    public bool IsActive { get; set; }

    public bool IsSuspended { get; set; }

    public DateTimeOffset? SuspendedAt { get; set; }

    public Guid? UserGlobalIdentity { get; set; }

    public DateTimeOffset? LastOtpGeneratedAt { get; set; }

    public int OtpSequence { get; set; }

    // Navigation property
    public Employee Employee { get; set; } = null!;
}