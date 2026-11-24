using Cdns.Application.DTOs.Sync;
using Cdns.Infrastructure;
using Cdns.Infrastructure.Persistence;
using Cdns.Infrastructure.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Cdns.Infrastructure.Tests.Sync;

public class CusDecItemSyncProcessorTests
{
    // Different from CusDecSyncProcessorTests' TestExternalId (777777777) — xUnit can
    // run different test classes in parallel, so sharing one ID risks a race.
    private const long TestExternalId = 888888888L;
    private const int TestItemNumber = 1;

    private static readonly string TestConnectionString =
        Environment.GetEnvironmentVariable("CDNS_TEST_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=cdns_test;Username=postgres;Password=postgres";

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Cdns.Infrastructure.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static ServiceProvider BuildServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = TestConnectionString,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration, new TestHostEnvironment());
        return services.BuildServiceProvider();
    }

    private static async Task CleanupAsync(CdnsDbContext db)
    {
        var item = await db.CusDecItems.FirstOrDefaultAsync(
            i => i.CusDecExternalSystemNumber == TestExternalId && i.CusDecItemNumber == TestItemNumber);
        if (item is not null)
        {
            db.CusDecItems.Remove(item);
        }

        var parent = await db.CusDecs.FirstOrDefaultAsync(c => c.CusDecExternalSystemNumber == TestExternalId);
        if (parent is not null)
        {
            db.CusDecs.Remove(parent);
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ProcessAsync_InsertsNewCusDecItem_WhenNoneExists()
    {
        using var provider = BuildServices();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CdnsDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<CusDecItemSyncProcessor>();

        await CleanupAsync(db);

        var row = new CusDecItemSyncRow
        {
            CusDecExternalSystemNumber = TestExternalId,
            CusDecItemNumber = TestItemNumber,
            HsCode = "1234.56",
            Description = "Test Item",
            Quantity = "10 KG",
            TotalValue = 100.50m,
            BlNumber = "BL-0001",
            GlobalIdentity = Guid.NewGuid(),
        };

        await processor.ProcessAsync(new[] { row }, CancellationToken.None);

        var inserted = await db.CusDecItems.FirstOrDefaultAsync(
            i => i.CusDecExternalSystemNumber == TestExternalId && i.CusDecItemNumber == TestItemNumber);

        Assert.NotNull(inserted);
        Assert.Equal("1234.56", inserted!.HsCode);
        Assert.Equal("Test Item", inserted.Description);
        Assert.Equal("BL-0001", inserted.BlNumber);
        Assert.Equal(100.50m, inserted.TotalValue);

        // No parent exists yet — both FK-resolution columns must stay null.
        Assert.Null(inserted.CusDecId);
        Assert.Null(inserted.CusDecGlobalIdentity);

        await CleanupAsync(db);
    }

    [Fact]
    public async Task ProcessAsync_UpdatesOnlyUnitPriceAndBlNumber_WhenItemAlreadyExists()
    {
        using var provider = BuildServices();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CdnsDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<CusDecItemSyncProcessor>();

        await CleanupAsync(db);

        var originalHsCode = "0000.00";
        var originalDescription = "ORIGINAL DESCRIPTION";
        var originalQuantity = "5 KG";
        var originalTotalValue = 50m;

        db.CusDecItems.Add(new Cdns.Domain.Entities.CusdecTables.CusDecItem
        {
            CusDecExternalSystemNumber = TestExternalId,
            CusDecItemNumber = TestItemNumber,
            HsCode = originalHsCode,
            Description = originalDescription,
            Quantity = originalQuantity,
            TotalValue = originalTotalValue,
            BlNumber = "ORIGINAL-BL",
            GlobalIdentity = Guid.NewGuid(),
            LastSyncDate = DateTime.UtcNow.AddDays(-1),
        });
        await db.SaveChangesAsync();

        foreach (var entry in db.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var row = new CusDecItemSyncRow
        {
            CusDecExternalSystemNumber = TestExternalId,
            CusDecItemNumber = TestItemNumber,
            HsCode = "SHOULD-NOT-OVERWRITE",
            Description = "SHOULD-NOT-OVERWRITE",
            Quantity = "SHOULD-NOT-OVERWRITE",
            TotalValue = 999999m,
            BlNumber = "UPDATED-BL",
            UnitPrice = 25.5m,
            GlobalIdentity = Guid.NewGuid(),
        };

        await processor.ProcessAsync(new[] { row }, CancellationToken.None);

        var updated = await db.CusDecItems.FirstOrDefaultAsync(
            i => i.CusDecExternalSystemNumber == TestExternalId && i.CusDecItemNumber == TestItemNumber);

        Assert.NotNull(updated);

        // These should have changed:
        Assert.Equal("UPDATED-BL", updated!.BlNumber);
        Assert.Equal(25.5m, updated.UnitPrice);

        // Everything else should be untouched:
        Assert.Equal(originalHsCode, updated.HsCode);
        Assert.Equal(originalDescription, updated.Description);
        Assert.Equal(originalQuantity, updated.Quantity);
        Assert.Equal(originalTotalValue, updated.TotalValue);

        await CleanupAsync(db);
    }

    [Fact]
    public async Task ProcessAsync_ResolvesCusDecId_WhenParentAlreadyExists()
    {
        using var provider = BuildServices();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CdnsDbContext>();
        var itemProcessor = scope.ServiceProvider.GetRequiredService<CusDecItemSyncProcessor>();

        await CleanupAsync(db);

        var parentGlobalIdentity = Guid.NewGuid();
        db.CusDecs.Add(new Cdns.Domain.Entities.CusdecTables.CusDec
        {
            CusDecExternalSystemNumber = TestExternalId,
            GlobalIdentity = parentGlobalIdentity,
            LastSyncDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
        });
        await db.SaveChangesAsync();

        var parentId = await db.CusDecs
            .Where(c => c.CusDecExternalSystemNumber == TestExternalId)
            .Select(c => c.CusDecId)
            .FirstAsync();

        foreach (var entry in db.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var row = new CusDecItemSyncRow
        {
            CusDecExternalSystemNumber = TestExternalId,
            CusDecItemNumber = TestItemNumber,
            HsCode = "1234.56",
            GlobalIdentity = Guid.NewGuid(),
        };

        await itemProcessor.ProcessAsync(new[] { row }, CancellationToken.None);

        var inserted = await db.CusDecItems.FirstOrDefaultAsync(
            i => i.CusDecExternalSystemNumber == TestExternalId && i.CusDecItemNumber == TestItemNumber);

        Assert.NotNull(inserted);
        Assert.Equal(parentId, inserted!.CusDecId);
        Assert.Equal(parentGlobalIdentity, inserted.CusDecGlobalIdentity);

        await CleanupAsync(db);
    }

    [Fact]
    public async Task ProcessAsync_ReconcilesOrphanedItem_WhenParentSyncsLater()
    {
        using var provider = BuildServices();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CdnsDbContext>();
        var itemProcessor = scope.ServiceProvider.GetRequiredService<CusDecItemSyncProcessor>();
        var cusDecProcessor = scope.ServiceProvider.GetRequiredService<CusDecSyncProcessor>();

        await CleanupAsync(db);

        // Sync the item FIRST, with no parent present — must insert with CusDecId = null.
        var itemRow = new CusDecItemSyncRow
        {
            CusDecExternalSystemNumber = TestExternalId,
            CusDecItemNumber = TestItemNumber,
            HsCode = "1234.56",
            GlobalIdentity = Guid.NewGuid(),
        };
        await itemProcessor.ProcessAsync(new[] { itemRow }, CancellationToken.None);

        var orphaned = await db.CusDecItems.FirstOrDefaultAsync(
            i => i.CusDecExternalSystemNumber == TestExternalId && i.CusDecItemNumber == TestItemNumber);
        Assert.NotNull(orphaned);
        Assert.Null(orphaned!.CusDecId);

        foreach (var entry in db.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        // Now sync the parent — its own sync's reconciliation step should link the orphan.
        var parentRow = new CusDecSyncRow
        {
            CusDecExternalSystemNumber = TestExternalId,
            GlobalIdentity = Guid.NewGuid(),
        };
        await cusDecProcessor.ProcessAsync(new[] { parentRow }, CancellationToken.None);

        var parent = await db.CusDecs.FirstAsync(c => c.CusDecExternalSystemNumber == TestExternalId);

        var reconciled = await db.CusDecItems.FirstOrDefaultAsync(
            i => i.CusDecExternalSystemNumber == TestExternalId && i.CusDecItemNumber == TestItemNumber);

        Assert.NotNull(reconciled);
        Assert.Equal(parent.CusDecId, reconciled!.CusDecId);
        Assert.Equal(parent.GlobalIdentity, reconciled.CusDecGlobalIdentity);

        await CleanupAsync(db);
    }
}