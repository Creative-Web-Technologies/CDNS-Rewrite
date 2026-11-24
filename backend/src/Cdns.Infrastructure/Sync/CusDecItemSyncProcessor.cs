using System.Text.Json;
using Cdns.Application.DTOs.Sync;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace Cdns.Infrastructure.Sync;

public class CusDecItemSyncProcessor
{
    private readonly CdnsDbContext _db;
    private readonly ILogger<CusDecItemSyncProcessor> _logger;

    public CusDecItemSyncProcessor(CdnsDbContext db, ILogger<CusDecItemSyncProcessor> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ProcessAsync(IReadOnlyList<CusDecItemSyncRow> rows, CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow; // last_sync_date is timestamptz — plain UTC Kind is correct here
        var json = JsonSerializer.Serialize(rows);

        var rowsParam = new NpgsqlParameter("p_rows", NpgsqlDbType.Jsonb) { Value = json };
        var nowParam = new NpgsqlParameter("p_now", NpgsqlDbType.TimestampTz) { Value = now };

        await _db.Database.ExecuteSqlRawAsync(
            "SELECT sync_cus_dec_item(@p_rows, @p_now)",
            new object[] { rowsParam, nowParam },
            cancellationToken);
    }
}