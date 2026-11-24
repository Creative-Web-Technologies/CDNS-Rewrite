using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Services;

/// <summary>
/// In-memory lookup from (ASYCUDA status, office) to a CDNS status name and its
/// visibility flag (<c>IsShow</c>), loaded once from
/// <c>cus_dec_status_mappings</c>. Office-specific mappings win over the
/// office-agnostic default; an unmapped code falls back to itself (visible) so it
/// never silently disappears. Shared by the dashboard count rules and the CusDec
/// page so both resolve statuses identically.
/// </summary>
public sealed class AsycudaStatusResolver
{
    private readonly Dictionary<(string Asycuda, string Office), (string CdnsStatus, bool IsShow)> _byOffice = new();
    private readonly Dictionary<string, (string CdnsStatus, bool IsShow)> _default = new();
    private readonly HashSet<string> _defaultHidden = new();

    private AsycudaStatusResolver() { }

    public static async Task<AsycudaStatusResolver> LoadAsync(CdnsDbContext db, CancellationToken ct)
    {
        var resolver = new AsycudaStatusResolver();
        var mappings = await db.CusDecStatusMappings
            .Where(m => m.AsycudaStatus != null && m.CdnsStatus != null)
            .Select(m => new { m.AsycudaStatus, m.CdnsStatus, m.OfficeCode, m.IsShow })
            .ToListAsync(ct);

        foreach (var m in mappings)
        {
            if (string.IsNullOrEmpty(m.OfficeCode))
                resolver._default[m.AsycudaStatus!] = (m.CdnsStatus!, m.IsShow);
            else
                resolver._byOffice[(m.AsycudaStatus!, m.OfficeCode)] = (m.CdnsStatus!, m.IsShow);
        }

        // Build the SQL pre-filter set after both dictionaries are complete.
        // A code belongs in the set only if the default mapping hides it AND no office
        // mapping makes it visible. If any office has IsShow=true for the code, pre-filtering
        // in SQL would wrongly drop that code for CusDecs from that office.
        var officeVisible = new HashSet<string>(
            resolver._byOffice
                .Where(kvp => kvp.Value.IsShow)
                .Select(kvp => kvp.Key.Asycuda));

        foreach (var kvp in resolver._default)
        {
            if (!kvp.Value.IsShow && !officeVisible.Contains(kvp.Key))
                resolver._defaultHidden.Add(kvp.Key);
        }

        return resolver;
    }

    /// <summary>
    /// ASYCUDA status codes that are safe to exclude in SQL before Take(TopK):
    /// hidden by the office-agnostic default mapping (IsShow=false, no OfficeCode)
    /// AND not overridden to visible by any office-specific mapping.
    /// A code with any office-specific IsShow=true entry is excluded from this set
    /// so it is never pre-filtered for CusDecs from that office.
    /// </summary>
    public IReadOnlySet<string> DefaultHiddenAsycudaCodes => _defaultHidden;

    /// <summary>
    /// Resolves an ASYCUDA status to its CDNS label. Returns the original code if
    /// there is no mapping (unmapped = show as-is). Does NOT apply the IsShow gate —
    /// call <see cref="IsHidden"/> first when computing the visible current status.
    /// </summary>
    public string? ToCdns(string? asycudaStatus, string? officeCode)
    {
        if (string.IsNullOrEmpty(asycudaStatus)) return asycudaStatus;
        if (!string.IsNullOrEmpty(officeCode) &&
            _byOffice.TryGetValue((asycudaStatus, officeCode), out var officeEntry))
            return officeEntry.CdnsStatus;
        return _default.TryGetValue(asycudaStatus, out var entry) ? entry.CdnsStatus : asycudaStatus;
    }

    /// <summary>
    /// Returns true if this ASYCUDA status row should be excluded when computing
    /// the visible current status (i.e. its mapping has <c>IsShow = false</c>).
    /// Unmapped statuses are treated as visible (IsHidden = false).
    /// </summary>
    public bool IsHidden(string? asycudaStatus, string? officeCode)
    {
        if (string.IsNullOrEmpty(asycudaStatus)) return false;
        if (!string.IsNullOrEmpty(officeCode) &&
            _byOffice.TryGetValue((asycudaStatus, officeCode), out var officeEntry))
            return !officeEntry.IsShow;
        return _default.TryGetValue(asycudaStatus, out var entry) && !entry.IsShow;
    }
}
