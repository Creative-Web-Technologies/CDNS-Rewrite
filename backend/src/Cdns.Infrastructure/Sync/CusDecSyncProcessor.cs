using Cdns.Application.DTOs.Sync;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Entities.CusdecTables;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

        // Build a TIN -> CompanyId lookup once per batch, matching legacy's
        // "SELECT TOP(1) CompanyId FROM Company WHERE TinNumber = LEFT(tin, 9)" behavior.
        // Legacy re-does this lookup fresh on every single sync (insert AND update), with
        // no filter on IsDeleted/CompanyStatusEnum — we match that exactly, no extra filters.
        var companies = await _db.Companies
            .AsNoTracking()
            .Select(c => new { c.TinNumber, c.CompanyId })
            .ToListAsync(cancellationToken);

        var companyByTin = new Dictionary<string, int>();
        foreach (var c in companies)
        {
            // First match wins — legacy's TOP(1) with no ORDER BY is just as ambiguous
            // if TinNumber isn't unique, so this is faithful, not a new assumption.
            companyByTin.TryAdd(c.TinNumber, c.CompanyId);
        }

        // Load any existing CusDec rows for this whole batch in one query, instead of
        // one query per row.
        var externalIds = rows.Select(r => r.CusDecExternalSystemNumber).Distinct().ToList();
        var existing = await _db.CusDecs
            .Where(c => externalIds.Contains(c.CusDecExternalSystemNumber))
            .ToDictionaryAsync(c => c.CusDecExternalSystemNumber, cancellationToken);

        var now = DateTime.UtcNow;
        
        foreach (var row in rows)
        {
            var declarantCompanyId = LookupCompanyId(companyByTin, row.DeclarantTinNumber);
            var consigneeCompanyId = LookupCompanyId(companyByTin, row.ConsigneeTinNumber);

            if (existing.TryGetValue(row.CusDecExternalSystemNumber, out var cusDec))
            {
                // WHEN MATCHED in SyncCusDec.sql only touches these six things — nothing else.
                cusDec.ReleaseDate = row.ReleaseDate;
                cusDec.DeclarantCompanyId = declarantCompanyId;
                cusDec.ConsigneeCompanyId = consigneeCompanyId;
                cusDec.DeclarantCompanyName = row.DeclarantCompanyName;
                cusDec.ConsigneeCompanyName = row.ConsigneeCompanyName;
                cusDec.SupplierCompanyName = row.SupplierCompanyName;
                cusDec.LastSyncDateTime = now;
            }
            else
            {
                // WHEN NOT MATCHED in SyncCusDec.sql — full column list on insert.
                _db.CusDecs.Add(new CusDec
                {
                    CusDecNumber = row.CusDecNumber,
                    DeclarantTinNumber = row.DeclarantTinNumber,
                    ConsigneeTinNumber = row.ConsigneeTinNumber,
                    InvoiceValue = row.InvoiceValue,
                    NoOfItems = row.NoOfItems.HasValue ? (int)row.NoOfItems.Value : null,
                    BlNumber = row.BlNumber,
                    GlobalIdentity = row.GlobalIdentity,
                    CusDecExternalSystemNumber = row.CusDecExternalSystemNumber,
                    CusDecDate = row.CusDecDate,
                    Origin = row.Origin,
                    OfficeCode = row.OfficeCode,
                    ReleaseDate = row.ReleaseDate,
                    RegistrationDate = row.RegistrationDate,
                    Exporter = row.Exporter,
                    Incoterm = row.Incoterm,
                    Destination = row.Destination,
                    Channel = row.Channel,
                    NoOfContainers = row.NoOfContainers ?? 0,
                    DeclarantCompanyId = declarantCompanyId,
                    ConsigneeCompanyId = consigneeCompanyId,
                    CusDecType = row.CusDecType,
                    DeclarantCompanyName = row.DeclarantCompanyName,
                    ConsigneeCompanyName = row.ConsigneeCompanyName,
                    SupplierCompanyName = row.SupplierCompanyName,
                    LastSyncDateTime = now,
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static int? LookupCompanyId(Dictionary<string, int> companyByTin, string? tin)
    {
        if (string.IsNullOrEmpty(tin))
        {
            return null;
        }

        var truncated = tin.Length > 9 ? tin.Substring(0, 9) : tin;
        return companyByTin.TryGetValue(truncated, out var companyId) ? companyId : null;
    }
}