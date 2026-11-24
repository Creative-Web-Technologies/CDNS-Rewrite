using Cdns.Application.Feature.Subscriptions;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Api.Endpoints.Dev;

/// <summary>
/// Development-only maintenance endpoints. Registered ONLY when the environment is
/// Development (see Program.cs) — not mapped at all otherwise.
/// </summary>
public static class DevSubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapDevSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var dev = app.MapGroup("/api/dev").RequireAuthorization();

        // Backfills the HMAC key for member_subscriptions rows that have none
        // (e.g. seeded rows). Computes each key from the row's own stored values
        // and never overwrites a key that is already present.
        dev.MapPost("/subscriptions/rekey", async (
            CdnsDbContext db, IMemberSubscriptionKeyService keyService, CancellationToken ct) =>
        {
            var rows = await db.MemberSubscriptions
                .Where(s => s.Key == null || s.Key == "")
                .ToListAsync(ct);

            var ids = new List<long>();
            foreach (var s in rows)
            {
                // A key over a null ExpiryDate is meaningless; skip such rows.
                if (s.ExpiryDate is null) continue;
                s.Key = keyService.Generate(s);
                ids.Add(s.MemberSubscriptionId);
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(new { updated = ids.Count, ids });
        });

        return app;
    }
}
