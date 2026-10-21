namespace Cdns.Domain.Entities.CompanySubscriptionManagement;


public class Subscription
{
    public int SubscriptionId { get; set; }

    public string SubscriptionName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal Amount { get; set; }

    public int NoOfActiveDuration { get; set; }

    public int? SubscriptionDurationTypeEnum { get; set; }

    public int? SubscriptionTypeEnum { get; set; }

    public bool IsDeleted { get; set; }

    public int SubscriptionActionTypeEnum { get; set; }

    public int SubscriptionStatusEnum { get; set; }

    public DateTimeOffset? CreatedDateTime { get; set; }

    public long? CreatedById { get; set; }

    public DateTimeOffset? UpdatedDateTime { get; set; }

    public long? UpdatedById { get; set; }
}