namespace Cdns.Application.Feature.Dashboard;

// ── Output DTOs (serialised to the dashboard endpoints) ──────────────────────
// System.Text.Json camel-cases these by default, matching the documented shape.

public sealed record SubscriptionCounts(int Pending, int Active, int Expired);

public sealed record CusDecCounts(int Total, int Processing, int Exited, int Cancelled);

public sealed record MonthlyCompletedPoint(int Month, int Count);

public sealed record WeeklyPoint(DateOnly WeekStart, int Registered);

public sealed record ClearanceSummary(int TotalExited, double? AvgClearanceHours);

public sealed record NearestExpiry(
    string CompanyName,
    string SubscriptionType,
    DateTimeOffset StartDate,
    DateTimeOffset ExpiryDate,
    int DaysTotal,
    int DaysUsed,
    int DaysRemaining);

public sealed record DashboardNotification(
    long Id,
    string? Title,
    string? Message,
    DateTimeOffset CreatedAt,
    bool IsSeen);

public sealed record PaymentSummary(
    decimal TotalCif,
    decimal TotalTaxPaid,
    decimal InspectionRecoveries);

public sealed record MemberDashboardSummary(
    SubscriptionCounts Subscriptions,
    CusDecCounts CusDecs,
    IReadOnlyList<MonthlyCompletedPoint> MonthlyCompleted,
    IReadOnlyList<WeeklyPoint> Weekly,
    double WeeklyAverage,
    ClearanceSummary Clearances,
    NearestExpiry? NearestExpiry,
    IReadOnlyList<DashboardNotification> RecentNotifications);

public sealed record CompanyDashboardSummary(
    SubscriptionCounts Subscriptions,
    CusDecCounts CusDecs,
    IReadOnlyList<MonthlyCompletedPoint> MonthlyCompleted,
    IReadOnlyList<WeeklyPoint> Weekly,
    double WeeklyAverage,
    ClearanceSummary Clearances,
    IReadOnlyList<DashboardNotification> RecentNotifications,
    PaymentSummary PaymentSummary);

// ── Calculator input views ───────────────────────────────────────────────────
// The Infrastructure services load EF rows into these plain records (resolving
// the ASYCUDA→CDNS status mapping first) and hand them to DashboardCalculator,
// so every count rule is pure and unit-testable without a database.

/// <summary>A single resolved status event on a declaration, in CDNS terms.</summary>
public sealed record StatusEvent(string? CdnsStatus, DateTime? At, int? Sequence);

/// <summary>A declaration visible to the dashboard, with its resolved status history.</summary>
public sealed record CusDecView(DateTime? RegisteredAt, IReadOnlyList<StatusEvent> Events);

/// <summary>A member subscription, reduced to the fields the count rules need.</summary>
public sealed record SubscriptionView(int StatusEnum, DateTimeOffset? ExpiryDate);
