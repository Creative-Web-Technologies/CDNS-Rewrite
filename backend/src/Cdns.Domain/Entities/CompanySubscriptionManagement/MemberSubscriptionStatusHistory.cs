namespace Cdns.Domain.Entities.Company;

public class MemberSubscriptionStatusHistory
{
    public long MemberSubscriptionStatusHistoryId { get; set; }

    public long MemberSubscriptionId { get; set; }

    public int MemberSubscriptionStatusEnum { get; set; }

    public string? Comment { get; set; }

    public long? MasterMemberId { get; set; }

    public long? EmployeeId { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }

    // Navigation Properties
    public MemberSubscription MemberSubscription { get; set; } = null!;

    public Member? MasterMember { get; set; }

    public Employee? Employee { get; set; }
}