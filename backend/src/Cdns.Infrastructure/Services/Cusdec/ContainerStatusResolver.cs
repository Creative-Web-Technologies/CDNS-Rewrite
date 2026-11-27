using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Services.Cusdec;

/// <summary>
/// In-memory lookup from (asycuda_status_key, asycuda_description_key) to a CDNS
/// container status name and visibility flag, loaded once from
/// <c>container_status_mappings</c>. Matching uses the same normalisation as the
/// computed columns: lower(btrim(value)). An unmapped row is treated as visible and
/// its raw status is returned unchanged.
///
/// Resolution order:
///   1. LEFT JOIN to container_status_mappings on (status_key, description_key).
///   2. If a mapping is found and IsShow=false → row is hidden.
///   3. If a mapping is found and IsShow=true → use CdnsStatus / CdnsDescription.
///   4. If no mapping (LEFT JOIN miss) → visible, raw value returned.
/// </summary>
public sealed class ContainerStatusResolver
{
    // Keyed by (asycuda_status_key, asycuda_description_key) — both already lower+btrim.
    private readonly Dictionary<(string StatusKey, string DescKey), (string CdnsStatus, string? CdnsDescription, bool IsShow)> _map = new();

    private ContainerStatusResolver() { }

    public static async Task<ContainerStatusResolver> LoadAsync(CdnsDbContext db, CancellationToken ct)
    {
        var resolver = new ContainerStatusResolver();
        var rows = await db.ContainerStatusMappings
            .Select(m => new
            {
                m.AsycudaStatusKey,
                m.AsycudaDescriptionKey,
                m.CdnsStatus,
                m.CdnsDescription,
                m.IsShow,
            })
            .ToListAsync(ct);

        foreach (var r in rows)
        {
            var key = (r.AsycudaStatusKey, r.AsycudaDescriptionKey);
            // Last-write wins if keys collide (unique index enforces this in Postgres).
            resolver._map[key] = (r.CdnsStatus, r.CdnsDescription, r.IsShow);
        }

        return resolver;
    }

    /// <summary>
    /// Returns true if this container status row should be excluded from display
    /// (mapping exists and IsShow=false). LEFT-JOIN miss → visible.
    /// </summary>
    public bool IsHidden(string? status, string? description)
    {
        var (sk, dk) = Normalize(status, description);
        return _map.TryGetValue((sk, dk), out var entry) && !entry.IsShow;
    }

    /// <summary>
    /// Resolves raw ASYCUDA (status, description) to CDNS labels. Returns the raw
    /// values if there is no mapping. Does NOT apply the IsShow gate — call
    /// <see cref="IsHidden"/> first.
    /// </summary>
    public (string? Status, string? Description) ToCdns(string? status, string? description)
    {
        var (sk, dk) = Normalize(status, description);
        if (_map.TryGetValue((sk, dk), out var entry))
            return (entry.CdnsStatus, entry.CdnsDescription);
        return (status, description);
    }

    // Normalises to match lower(btrim(value)) in Postgres.
    // btrim removes only space characters (U+0020); C# Trim(' ') mirrors that exactly.
    // ToLowerInvariant() is used for culture-neutral lowercasing (ASYCUDA codes are ASCII).
    // Non-ASCII leading/trailing whitespace (e.g. tabs) is NOT stripped by btrim or
    // Trim(' '), so a tab-padded value will not match a space-stripped key — intentional.
    private static (string StatusKey, string DescKey) Normalize(string? status, string? description) =>
        (status?.Trim(' ').ToLowerInvariant() ?? string.Empty,
         description?.Trim(' ').ToLowerInvariant() ?? string.Empty);
}
