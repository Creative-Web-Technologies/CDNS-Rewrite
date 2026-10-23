using Cdns.Domain.Entities.MemberSignupTables;

namespace Cdns.Domain.Entities.CompanySubscriptionManagement;


public class SubscriptionRequest
{
    public long SubscriptionRequestId { get; set; }

    public long MemberId { get; set; }

    public int? CompanyId { get; set; }

    public DateTimeOffset RequestedDateTime { get; set; }

    public DateTimeOffset? RespondDateTime { get; set; }

    public long? ResponderId { get; set; }

    public string? Comment { get; set; }

    public string? ResponderComment { get; set; }

    public string? SubscriptionRequestNumber { get; set; }

    public int? SubscriptionRequestStatusEnum { get; set; }

    public int? SubscriptionTypeEnum { get; set; }

    public Guid GlobalIdentity { get; set; }

    public bool? IsDeleted { get; set; }

    // Navigation properties
    public Member Member { get; set; } = null!;

    public Company? Company { get; set; }
}