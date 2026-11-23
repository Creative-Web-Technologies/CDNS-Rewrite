using Cdns.Application.Interfaces;

namespace Cdns.Application.Tests;

public class SriLankaTimeTests
{
    [Fact]
    public void FromColomboToUtc_ConvertsCorrectly()
    {
        // 2026-11-20 09:00 Colombo (UTC+5:30) → 2026-11-20 03:30 UTC
        var colombo = new DateTime(2026, 11, 20, 9, 0, 0);
        var expected = new DateTime(2026, 11, 20, 3, 30, 0, DateTimeKind.Utc);

        var result = SriLankaTime.FromColomboToUtc(colombo);

        Assert.Equal(expected, result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void ToColombo_ConvertsCorrectly()
    {
        // 2026-11-19 19:00 UTC → 2026-11-20 00:30 Colombo (UTC+5:30)
        var utc = new DateTime(2026, 11, 19, 19, 0, 0, DateTimeKind.Utc);
        var expected = new DateTime(2026, 11, 20, 0, 30, 0);

        var result = SriLankaTime.ToColombo(utc);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FromColomboToUtc_NullableOverload_PassesThroughNull()
    {
        Assert.Null(SriLankaTime.FromColomboToUtc((DateTime?)null));
    }
}
