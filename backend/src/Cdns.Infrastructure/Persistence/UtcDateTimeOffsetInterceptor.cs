using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cdns.Infrastructure.Persistence;

/// <summary>
/// Normalizes every DateTimeOffset / DateTimeOffset? property on Added or Modified
/// entries to UTC before the row is written. Guards against accidental Colombo- or
/// other-offset values reaching Npgsql, which only accepts offset 0 for
/// 'timestamp with time zone'. Do not replace with a value converter — converters
/// can break Npgsql query translation.
/// </summary>
public sealed class UtcDateTimeOffsetInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Normalize(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Normalize(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Normalize(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
        {
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTimeOffset dto && dto.Offset != TimeSpan.Zero)
                    property.CurrentValue = dto.ToUniversalTime();
            }
        }
    }
}
