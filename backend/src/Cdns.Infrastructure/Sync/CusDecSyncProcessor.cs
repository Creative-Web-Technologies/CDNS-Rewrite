using System.Text.Json;
using Cdns.Application.DTOs.Sync;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace Cdns.Infrastructure.Sync;

public class CusDecSyncProcessor
{
    private readonly CdnsDbContext _db;
    private readonly ILogger<CusDecSyncProcessor> _logger;

    public CusDecSyncProcessor(CdnsDbContext db, ILogger<CusDecSyncProcessor> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ProcessAsync(IReadOnlyList<CusDecSyncRow> rows, CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var json = JsonSerializer.Serialize(rows);

        var rowsParam = new NpgsqlParameter("p_rows", NpgsqlDbType.Jsonb) { Value = json };
        var nowParam = new NpgsqlParameter("p_now", NpgsqlDbType.Timestamp) { Value = now };

        await _db.Database.ExecuteSqlRawAsync(
            "SELECT sync_cus_dec(@p_rows, @p_now)",
            new object[] { rowsParam, nowParam },
            cancellationToken);
    }
}