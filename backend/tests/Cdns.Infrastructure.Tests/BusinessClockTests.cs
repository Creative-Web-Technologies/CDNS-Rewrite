using Cdns.Infrastructure.Services;

namespace Cdns.Infrastructure.Tests;

public class BusinessClockTests
{
    private static readonly TimeZoneInfo Colombo =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");

    // ── ColomboBusinessClock (production) ────────────────────────────────────

    [Fact]
    public void ColomboBusinessClock_Now_ReturnsUtcOffset()
    {
        var clock = new ColomboBusinessClock();
        Assert.Equal(TimeSpan.Zero, clock.Now().Offset);
    }

    [Fact]
    public void ColomboBusinessClock_StartOfToday_ReturnsUtcOffset()
    {
        var clock = new ColomboBusinessClock();
        Assert.Equal(TimeSpan.Zero, clock.StartOfToday().Offset);
    }

    // ── FakeBusinessClock at boundary hours ───────────────────────────────────
    //
    // 02:00 Colombo (+05:30) = 20:30 UTC previous day — before midnight UTC.
    // 23:00 Colombo (+05:30) = 17:30 UTC — after midnight UTC but before Colombo midnight.
    // Both must return Offset == TimeSpan.Zero from every method.

    [Theory]
    [InlineData(2026, 11, 16, 2, 0)]   // 02:00 Colombo
    [InlineData(2026, 11, 16, 23, 0)]  // 23:00 Colombo
    public void FakeBusinessClock_Now_ReturnsUtcOffset(
        int year, int month, int day, int hour, int minute)
    {
        var localDt = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        var clock = new FakeBusinessClock(
            new DateTimeOffset(localDt, Colombo.GetUtcOffset(localDt)));

        Assert.Equal(TimeSpan.Zero, clock.Now().Offset);
    }

    [Theory]
    [InlineData(2026, 11, 16, 2, 0)]   // 02:00 Colombo — UTC date is Nov 15
    [InlineData(2026, 11, 16, 23, 0)]  // 23:00 Colombo — UTC date is Nov 16
    public void FakeBusinessClock_StartOfToday_ReturnsUtcOffset(
        int year, int month, int day, int hour, int minute)
    {
        var localDt = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        var clock = new FakeBusinessClock(
            new DateTimeOffset(localDt, Colombo.GetUtcOffset(localDt)));

        Assert.Equal(TimeSpan.Zero, clock.StartOfToday().Offset);
    }

    // ── StartOfToday represents the correct Colombo midnight instant ──────────

    [Theory]
    [InlineData(2026, 11, 16, 2, 0)]   // at 02:00 Colombo → today is Nov 16 Colombo
    [InlineData(2026, 11, 16, 23, 0)]  // at 23:00 Colombo → today is Nov 16 Colombo
    public void FakeBusinessClock_StartOfToday_IsColombоMidnightExpressedAsUtc(
        int year, int month, int day, int hour, int minute)
    {
        var localDt = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        var clock = new FakeBusinessClock(
            new DateTimeOffset(localDt, Colombo.GetUtcOffset(localDt)));

        var today = clock.StartOfToday();

        // Convert back to Colombo to verify it really is midnight of `day`
        var colomboToday = TimeZoneInfo.ConvertTime(today, Colombo);
        Assert.Equal(day, colomboToday.Day);
        Assert.Equal(month, colomboToday.Month);
        Assert.Equal(year, colomboToday.Year);
        Assert.Equal(0, colomboToday.Hour);
        Assert.Equal(0, colomboToday.Minute);
        Assert.Equal(0, colomboToday.Second);
    }

    [Fact]
    public void FakeBusinessClock_At0200Colombo_StartOfToday_IsNov15Utc1830()
    {
        // 2026-11-16 02:00 Colombo = 2026-11-15 20:30 UTC
        // Colombo midnight Nov 16 = 2026-11-15 18:30 UTC
        var localDt = new DateTime(2026, 11, 16, 2, 0, 0, DateTimeKind.Unspecified);
        var clock = new FakeBusinessClock(
            new DateTimeOffset(localDt, Colombo.GetUtcOffset(localDt)));

        var today = clock.StartOfToday();

        var expected = new DateTimeOffset(2026, 11, 15, 18, 30, 0, TimeSpan.Zero);
        Assert.Equal(expected, today);
    }
}
