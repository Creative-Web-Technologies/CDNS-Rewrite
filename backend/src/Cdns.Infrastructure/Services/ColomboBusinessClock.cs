using Cdns.Application.Interfaces;

namespace Cdns.Infrastructure.Services;

/// <summary>
/// Production clock that uses the Asia/Colombo timezone (UTC+5:30).
/// .NET 10 supports IANA timezone IDs on all platforms.
/// </summary>
public sealed class ColomboBusinessClock : IBusinessClock
{
    private static readonly TimeZoneInfo Colombo =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");

    public DateTimeOffset Now() => DateTimeOffset.UtcNow;

    public DateTimeOffset StartOfToday()
    {
        var colomboNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Colombo);
        var midnight = new DateTime(
            colomboNow.Year, colomboNow.Month, colomboNow.Day,
            0, 0, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(midnight, Colombo.GetUtcOffset(midnight));
    }
}
