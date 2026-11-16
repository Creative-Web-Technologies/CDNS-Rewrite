namespace Cdns.Application.Interfaces;

/// <summary>
/// Abstracts the system clock for business-day logic, so services can be
/// unit-tested with a deterministic clock without touching the wall clock.
/// All instants returned have <c>Offset == TimeSpan.Zero</c> (UTC), so they
/// are safe to use as EF query parameters and entity values without triggering
/// Npgsql's "only offset 0 is supported" restriction.
/// Colombo-local date decisions (e.g. "which calendar day is today?") are
/// made internally and the result is converted back to UTC before returning.
/// </summary>
public interface IBusinessClock
{
    /// <summary>Returns the current moment in time as UTC (Offset == TimeSpan.Zero).</summary>
    DateTimeOffset Now();

    /// <summary>
    /// Returns the instant of Colombo midnight for today's calendar day,
    /// expressed as UTC (Offset == TimeSpan.Zero).
    /// The instant is Asia/Colombo 00:00:00 today, converted to UTC.
    /// </summary>
    DateTimeOffset StartOfToday();
}
