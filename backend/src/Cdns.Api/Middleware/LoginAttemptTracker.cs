namespace Cdns.Api.Middleware;

public sealed class LoginAttemptTracker
{
    private static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(15);
    private const int MaximumFailures = 5;

    private readonly object _lock = new();
    private readonly Dictionary<string, Queue<DateTimeOffset>> _failuresByIp = new();
    private int _operations;

    public bool IsLimited(string ipAddress)
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            if (!_failuresByIp.TryGetValue(ipAddress, out var failures))
            {
                PrunePeriodically(now);
                return false;
            }

            RemoveExpired(failures, now);
            if (failures.Count == 0)
            {
                _failuresByIp.Remove(ipAddress);
                PrunePeriodically(now);
                return false;
            }

            var isLimited = failures.Count >= MaximumFailures;
            PrunePeriodically(now);
            return isLimited;
        }
    }

    public void RecordFailure(string ipAddress)
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            if (!_failuresByIp.TryGetValue(ipAddress, out var failures))
            {
                failures = new Queue<DateTimeOffset>();
                _failuresByIp.Add(ipAddress, failures);
            }

            RemoveExpired(failures, now);
            failures.Enqueue(now);
            PrunePeriodically(now);
        }
    }

    private void PrunePeriodically(DateTimeOffset now)
    {
        _operations++;
        if (_operations % 128 != 0)
        {
            return;
        }

        foreach (var (ipAddress, failures) in _failuresByIp.ToArray())
        {
            RemoveExpired(failures, now);
            if (failures.Count == 0)
            {
                _failuresByIp.Remove(ipAddress);
            }
        }
    }

    private static void RemoveExpired(Queue<DateTimeOffset> failures, DateTimeOffset now)
    {
        while (failures.TryPeek(out var failureTime) && now - failureTime >= AttemptWindow)
        {
            failures.Dequeue();
        }
    }
}
