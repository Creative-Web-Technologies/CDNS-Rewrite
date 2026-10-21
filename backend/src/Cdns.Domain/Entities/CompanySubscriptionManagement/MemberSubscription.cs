using Cdns.Domain.Entities.MemberSignupTables;

namespace Cdns.Domain.Entities.CompanySubscriptionManagement;

public class MemberSubscription
{
    public long MemberSubscriptionId { get; set; }

    public long SubscriptionRequestId { get; set; }

    public long MemberId { get; set; }

    public int SubscriptionId { get; set; }

    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset? ExpiryDate { get; set; }

    public decimal PaidAmount { get; set; }

    public DateTimeOffset? PaidDate { get; set; }

    public int MemberSubscriptionStatusEnum { get; set; }

    public string? MemberSubscriptionNumber { get; set; }

    public decimal SubscriptionAmount { get; set; }

    public int NoOfActiveDuration { get; set; }

    public int? SubscriptionDurationTypeEnum { get; set; }

    public string SubscriptionName { get; set; } = null!;

    public string? TransactionReferenceNumber { get; set; }

    public Guid GlobalIdentity { get; set; }

    public int SubscriptionTypeEnum { get; set; }

    public int SubscriptionActionTypeEnum { get; set; }

    public int CompanyId { get; set; }

    public long CompanyMasterMemberId { get; set; }

    public string? Key { get; set; }

    // Navigation properties
    public SubscriptionRequest SubscriptionRequest { get; set; } = null!;

    public Member Member { get; set; } = null!;

    public Subscription Subscription { get; set; } = null!;

    public Company Company { get; set; } = null!;
}