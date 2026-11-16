namespace Cdns.Application.Feature.Subscriptions;

public interface IMemberSubscriptionService
{
    Task<List<ActiveSubscriptionDto>> GetActiveSubscriptionsAsync(CancellationToken ct = default);
}
