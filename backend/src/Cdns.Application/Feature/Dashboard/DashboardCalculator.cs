using Cdns.Application.Interfaces;
using Cdns.Domain.Entities.Company;

namespace Cdns.Application.Feature.Dashboard;

/// <summary>
/// Pure, database-free implementation of the dashboard count rules (ported from
/// legacy). The Infrastructure services resolve EF rows into the input views in
/// <see cref="DashboardModels"/> and delegate here, so the rules can be unit
/// tested in isolation. Nothing in this class touches EF or the clock directly —
/// "today"/"year" are always passed in.
/// </summary>
public static class DashboardCalculator
{
    /// <summary>Default CDNS status that marks a declaration as fully exited.</summary>
    public const string ExitedStatus = "Totally exited";

    /// <summary>CDNS status that marks a declaration as cancelled.</summary>
    public const string CancelledStatus = "Cancelled";

    /// <summary>
    /// The most recent resolved status of a declaration: the event with the
    /// highest sequence number, falling back to the latest timestamp.
    /// </summary>
    public static string? CurrentStatus(CusDecView cusDec)
    {
        var latest = cusDec.Events
            .OrderBy(e => e.Sequence ?? int.MinValue)
            .ThenBy(e => e.At ?? DateTime.MinValue)
            .LastOrDefault();
        return latest?.CdnsStatus;
    }

    public static bool IsExited(CusDecView cusDec) =>
        string.Equals(CurrentStatus(cusDec), ExitedStatus, StringComparison.OrdinalIgnoreCase);

    public static bool IsCancelled(CusDecView cusDec) =>
        string.Equals(CurrentStatus(cusDec), CancelledStatus, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Total / processing / exited / cancelled across the visible declarations.
    /// Exited and cancelled are keyed off the current status; everything else is
    /// processing, so the three buckets always sum to the total.
    /// </summary>
    public static CusDecCounts CountCusDecs(IEnumerable<CusDecView> cusDecs)
    {
        int total = 0, exited = 0, cancelled = 0;
        foreach (var c in cusDecs)
        {
            total++;
            if (IsExited(c)) exited++;
            else if (IsCancelled(c)) cancelled++;
        }
        return new CusDecCounts(total, total - exited - cancelled, exited, cancelled);
    }

    /// <summary>
    /// Count of declarations that exited in each month (1..12) of the given year.
    /// Always returns all twelve months so the chart axis is complete.
    /// </summary>
    public static IReadOnlyList<MonthlyCompletedPoint> MonthlyCompleted(
        IEnumerable<CusDecView> cusDecs, int year)
    {
        var counts = new int[12];
        foreach (var c in cusDecs)
        {
            if (!IsExited(c)) continue;
            var exitAt = ExitMoment(c);
            if (exitAt is { } at)
            {
                var local = SriLankaTime.ToColombo(at);
                if (local.Year == year)
                    counts[local.Month - 1]++;
            }
        }
        return Enumerable.Range(1, 12)
            .Select(m => new MonthlyCompletedPoint(m, counts[m - 1]))
            .ToList();
    }

    /// <summary>
    /// Registered-declaration counts for the last <paramref name="weeks"/> rolling
    /// 7-day windows ending on <paramref name="today"/>, oldest window first.
    /// </summary>
    public static IReadOnlyList<WeeklyPoint> Weekly(
        IEnumerable<CusDecView> cusDecs, DateOnly today, int weeks = 4)
    {
        var registeredDays = cusDecs
            .Where(c => c.RegisteredAt is not null)
            .Select(c => DateOnly.FromDateTime(SriLankaTime.ToColombo(c.RegisteredAt!.Value)))
            .ToList();

        var points = new List<WeeklyPoint>(weeks);
        for (var k = weeks - 1; k >= 0; k--)
        {
            var weekStart = today.AddDays(-6 - 7 * k);
            var weekEnd = weekStart.AddDays(6);
            var count = registeredDays.Count(d => d >= weekStart && d <= weekEnd);
            points.Add(new WeeklyPoint(weekStart, count));
        }
        return points;
    }

    public static double WeeklyAverage(IEnumerable<WeeklyPoint> weekly)
    {
        var values = weekly.Select(w => w.Registered).ToList();
        return values.Count == 0 ? 0 : Math.Round(values.Average(), 2);
    }

    /// <summary>
    /// Total exited plus the mean first-status→exit-status time in hours. The
    /// average only considers exited declarations that have both a first and an
    /// exit timestamp; when none qualify it is null (not derivable).
    /// </summary>
    public static ClearanceSummary Clearances(IEnumerable<CusDecView> cusDecs)
    {
        var exited = cusDecs.Where(IsExited).ToList();
        var durations = new List<double>();
        foreach (var c in exited)
        {
            var first = FirstMoment(c);
            var exit = ExitMoment(c);
            if (first is { } f && exit is { } e && e >= f)
                durations.Add((e - f).TotalHours);
        }
        double? avg = durations.Count == 0 ? null : Math.Round(durations.Average(), 2);
        return new ClearanceSummary(exited.Count, avg);
    }

    /// <summary>
    /// Pending / active / expired subscription counts.
    /// Pending counts requests still awaiting a response; active and expired are
    /// derived from member subscriptions against <paramref name="today"/>.
    /// </summary>
    public static SubscriptionCounts SubscriptionCounts(
        IEnumerable<int> requestStatuses,
        IEnumerable<SubscriptionView> subscriptions,
        DateTimeOffset today)
    {
        var pending = requestStatuses
            .Count(s => s == (int)SubscriptionRequestStatusEnum.Pending);

        var subs = subscriptions as IReadOnlyCollection<SubscriptionView> ?? subscriptions.ToList();

        var active = subs.Count(s =>
            s.StatusEnum == (int)MemberSubscriptionStatusEnum.Activate &&
            s.ExpiryDate is not null && s.ExpiryDate >= today);

        var expired = subs.Count(s =>
            s.StatusEnum == (int)MemberSubscriptionStatusEnum.Expired ||
            (s.ExpiryDate is not null && s.ExpiryDate < today));

        return new SubscriptionCounts(pending, active, expired);
    }

    // The exit moment is the current (latest) event's timestamp, since a
    // declaration is only "exited" when its current status is the exit status.
    private static DateTime? ExitMoment(CusDecView cusDec) => cusDec.Events
        .OrderBy(e => e.Sequence ?? int.MinValue)
        .ThenBy(e => e.At ?? DateTime.MinValue)
        .LastOrDefault()?.At;

    private static DateTime? FirstMoment(CusDecView cusDec) => cusDec.Events
        .Where(e => e.At is not null)
        .Select(e => e.At)
        .DefaultIfEmpty(null)
        .Min();
}
