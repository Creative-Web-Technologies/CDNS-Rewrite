namespace Cdns.Application.Interfaces;

/// <summary>
/// Abstracts the system clock for business-day logic, so services can be
/// unit-tested with a deterministic clock without touching the wall clock.
/// All times are returned as <see cref="DateTimeOffset"/> with the correct
/// UTC offset for the business timezone (Asia/Colombo).
/// </summary>
public interface IBusinessClock
{
    /// <summary>Returns the current moment in time (wall clock, any offset).</summary>
    DateTimeOffset Now();

    /// <summary>
    /// Returns Colombo midnight of the current calendar day as a
    /// <see cref="DateTimeOffset"/> carrying the Sri Lanka UTC+5:30 offset.
    /// </summary>
    DateTimeOffset StartOfToday();
}
