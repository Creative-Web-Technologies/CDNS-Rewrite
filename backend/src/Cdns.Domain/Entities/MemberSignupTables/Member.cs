using Cdns.Domain.Entities.Enums;

namespace Cdns.Domain.Entities.MemberSignupTables;

public class Member
{
    public long MemberId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? NotificationEmail { get; set; }
    public string? Mobile { get; set; }
    public string? Nic { get; set; }
    public string? ProfileImage { get; set; }
    public string? Designation { get; set; }

    public MemberType MemberType { get; set; } = MemberType.Member;
    public SubscriptionType? DefaultSubscriptionType { get; set; } = SubscriptionType.Declarant;

    public bool IsDeleted { get; set; }
}