using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cdns.Infrastructure.Persistence;

/// <summary>
/// Normalizes any DateTimeOffset / DateTimeOffset? command parameter to UTC
/// before the SQL is sent to Npgsql. Npgsql only accepts offset 0 for
/// 'timestamp with time zone'; a non-zero offset causes a runtime exception.
/// This interceptor is a safety net alongside the source fix in
/// ColomboBusinessClock and the entity-property interceptor
/// (UtcDateTimeOffsetInterceptor). Do not use an EF value converter instead —
/// converters can break Npgsql query translation.
/// </summary>
public sealed class UtcDateTimeOffsetCommandInterceptor : DbCommandInterceptor
{
    public override InterceptionResult<System.Data.Common.DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData,
        InterceptionResult<System.Data.Common.DbDataReader> result)
    {
        NormalizeParameters(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData,
        InterceptionResult<System.Data.Common.DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        NormalizeParameters(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        NormalizeParameters(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        NormalizeParameters(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        NormalizeParameters(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        NormalizeParameters(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <summary>
    /// Iterates every parameter of <paramref name="command"/> and converts any
    /// <see cref="DateTimeOffset"/> with a non-zero offset to UTC in-place.
    /// Public so that tests can exercise the normalization logic directly.
    /// </summary>
    public static void NormalizeParameters(DbCommand command)
    {
        foreach (DbParameter param in command.Parameters)
        {
            if (param.Value is DateTimeOffset dto && dto.Offset != TimeSpan.Zero)
                param.Value = dto.ToUniversalTime();
        }
    }
}
