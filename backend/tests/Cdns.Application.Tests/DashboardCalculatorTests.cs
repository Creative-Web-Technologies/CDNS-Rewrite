using Cdns.Application.Feature.Dashboard;
using Cdns.Domain.Entities.Company;
using Cdns.Domain.Enums;

namespace Cdns.Application.Tests;

public class DashboardCalculatorTests
{
    private static readonly DateTime Base = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static CusDecView CusDec(DateTime? registered, params (string Cdns, DateTime At, int Seq)[] events) =>
        new(registered, events.Select(e => new StatusEvent(e.Cdns, e.At, e.Seq)).ToList());

    // ── Current status & classification ──────────────────────────────────────

    [Fact]
    public void CurrentStatus_uses_highest_sequence_not_insertion_order()
    {
        var c = CusDec(Base,
            ("Totally exited", Base.AddHours(3), 3),
            ("Registered", Base, 1),
            ("Assessed", Base.AddHours(1), 2));

        Assert.Equal("Totally exited", DashboardCalculator.CurrentStatus(c));
        Assert.True(DashboardCalculator.IsExited(c));
        Assert.False(DashboardCalculator.IsCancelled(c));
    }

    [Fact]
    public void CountCusDecs_buckets_sum_to_total_and_processing_is_remainder()
    {
        var cusDecs = new[]
        {
            CusDec(Base, ("Registered", Base, 1)),                                   // processing
            CusDec(Base, ("Registered", Base, 1), ("Totally exited", Base.AddHours(5), 2)), // exited
            CusDec(Base, ("Cancelled", Base, 1)),                                    // cancelled
            CusDec(Base, ("Assessed", Base, 1)),                                     // processing
        };

        var counts = DashboardCalculator.CountCusDecs(cusDecs);

        Assert.Equal(4, counts.Total);
        Assert.Equal(1, counts.Exited);
        Assert.Equal(1, counts.Cancelled);
        Assert.Equal(2, counts.Processing);
        Assert.Equal(counts.Total, counts.Processing + counts.Exited + counts.Cancelled);
    }

    [Fact]
    public void CurrentStatus_matching_is_case_insensitive()
    {
        var c = CusDec(Base, ("totally EXITED", Base, 1));
        Assert.True(DashboardCalculator.IsExited(c));
    }

    // ── Monthly completed ────────────────────────────────────────────────────

    [Fact]
    public void MonthlyCompleted_counts_exits_by_month_and_returns_all_twelve()
    {
        var cusDecs = new[]
        {
            CusDec(Base, ("Registered", Base, 1), ("Totally exited", new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), 2)),
            CusDec(Base, ("Registered", Base, 1), ("Totally exited", new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc), 2)),
            CusDec(Base, ("Registered", Base, 1), ("Totally exited", new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), 2)),
            CusDec(Base, ("Registered", Base, 1), ("Totally exited", new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), 2)), // prior year ignored
            CusDec(Base, ("Registered", Base, 1)),                                   // not exited, ignored
        };

        var monthly = DashboardCalculator.MonthlyCompleted(cusDecs, 2026);

        Assert.Equal(12, monthly.Count);
        Assert.Equal(2, monthly.Single(m => m.Month == 2).Count);
        Assert.Equal(1, monthly.Single(m => m.Month == 5).Count);
        Assert.Equal(0, monthly.Single(m => m.Month == 1).Count);
    }

    // ── Weekly ───────────────────────────────────────────────────────────────

    [Fact]
    public void Weekly_returns_four_rolling_windows_oldest_first()
    {
        var today = new DateOnly(2026, 11, 15);
        var cusDecs = new[]
        {
            CusDec(new DateTime(2026, 11, 14, 0, 0, 0, DateTimeKind.Utc)), // this week
            CusDec(new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc)), // this week (Nov 9-15)
            CusDec(new DateTime(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc)),  // last week (Nov 2-8)
            CusDec(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)),  // outside 4 weeks
        };

        var weekly = DashboardCalculator.Weekly(cusDecs, today);

        Assert.Equal(4, weekly.Count);
        Assert.Equal(new DateOnly(2026, 10, 19), weekly[0].WeekStart); // oldest window first
        Assert.Equal(new DateOnly(2026, 11, 9), weekly[3].WeekStart);  // newest window (today-6)
        Assert.Equal(2, weekly[3].Registered); // Nov 10 & 14
        Assert.Equal(1, weekly[2].Registered); // Nov 3 (Nov 2-8)
        Assert.Equal(0.75, DashboardCalculator.WeeklyAverage(weekly)); // (0+0+1+2)/4
    }

    [Fact]
    public void WeeklyAverage_is_mean_of_window_counts()
    {
        var weekly = new[]
        {
            new WeeklyPoint(new DateOnly(2026, 10, 26), 0),
            new WeeklyPoint(new DateOnly(2026, 11, 2), 0),
            new WeeklyPoint(new DateOnly(2026, 11, 9), 1),
            new WeeklyPoint(new DateOnly(2026, 11, 16), 2),
        };
        Assert.Equal(0.75, DashboardCalculator.WeeklyAverage(weekly));
    }

    // ── Clearances ───────────────────────────────────────────────────────────

    [Fact]
    public void Clearances_averages_first_to_exit_hours_only_for_exited()
    {
        var cusDecs = new[]
        {
            CusDec(Base, ("Registered", Base, 1), ("Totally exited", Base.AddHours(10), 2)), // 10h
            CusDec(Base, ("Registered", Base, 1), ("Totally exited", Base.AddHours(20), 2)), // 20h
            CusDec(Base, ("Registered", Base, 1)),                                           // not exited
        };

        var clearances = DashboardCalculator.Clearances(cusDecs);

        Assert.Equal(2, clearances.TotalExited);
        Assert.Equal(15.0, clearances.AvgClearanceHours);
    }

    [Fact]
    public void Clearances_avg_is_null_when_no_exited_declarations()
    {
        var cusDecs = new[] { CusDec(Base, ("Registered", Base, 1)) };

        var clearances = DashboardCalculator.Clearances(cusDecs);

        Assert.Equal(0, clearances.TotalExited);
        Assert.Null(clearances.AvgClearanceHours);
    }

    // ── Subscription counts ──────────────────────────────────────────────────

    [Fact]
    public void SubscriptionCounts_applies_pending_active_expired_rules()
    {
        var today = new DateTimeOffset(2026, 11, 15, 0, 0, 0, TimeSpan.Zero);
        var requests = new[]
        {
            (int)SubscriptionRequestStatusEnum.Pending,
            (int)SubscriptionRequestStatusEnum.Pending,
            (int)SubscriptionRequestStatusEnum.Approved,
            (int)SubscriptionRequestStatusEnum.Rejected,
        };
        var subs = new[]
        {
            new SubscriptionView((int)MemberSubscriptionStatusEnum.Activate, today.AddDays(30)),  // active
            new SubscriptionView((int)MemberSubscriptionStatusEnum.Activate, today.AddDays(-1)),  // active status but expired date -> expired
            new SubscriptionView((int)MemberSubscriptionStatusEnum.Expired, today.AddDays(-50)),  // expired
        };

        var counts = DashboardCalculator.SubscriptionCounts(requests, subs, today);

        Assert.Equal(2, counts.Pending);
        Assert.Equal(1, counts.Active);
        Assert.Equal(2, counts.Expired);
    }
}
