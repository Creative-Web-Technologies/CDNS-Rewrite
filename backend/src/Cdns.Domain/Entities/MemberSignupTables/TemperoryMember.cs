using Cdns.Domain.Enums;

namespace Cdns.Domain.Entities.MemberSignupTables;

public class TemporaryMember
{
    public long TemporaryMemberId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public string? Nic { get; set; }
    public string? ProfileImage { get; set; }
    public string? Designation { get; set; }

    public int? OneTimePassword { get; set; }
    public string? GlobalIdentity { get; set; }

    public string? Password { get; set; }
    public string? PasswordSalt { get; set; }

    public SubscriptionType? DefaultSubscriptionType { get; set; } = SubscriptionType.Declarant;

    public DateTimeOffset CreatedDateTime { get; set; }
    public bool IsDeleted { get; set; }
}