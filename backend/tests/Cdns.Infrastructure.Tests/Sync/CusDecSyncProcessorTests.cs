using Cdns.Application.DTOs.Sync;
using Cdns.Infrastructure;
using Cdns.Infrastructure.Persistence;
using Cdns.Infrastructure.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Cdns.Application.Interfaces;
using WorkerService = Cdns.Worker.Worker;
using Microsoft.Extensions.Logging;

namespace Cdns.Infrastructure.Tests.Sync;

public class CusDecSyncProcessorTests
{
    private static readonly Guid AsycudaSyncAppId = new("3f2a9c1e-6b7d-4e3a-9f0a-5d8c4b1e2a77");
    private const string AsycudaSyncAppName = "AsycudaCusDecSync";
    // Same connection string as appsettings.Development.json's "Default" entry —
    // this test writes real rows into your local dev Postgres.
    private const string TestConnectionString = "Host=localhost;Port=5432;Database=cdns_dev;Username=postgres;Password=chirath2004";

    // An external system number that's extremely unlikely to collide with any real
    // ASYCUDA instance ID, so this test never touches real data even accidentally.
    private const long TestExternalId = 900000001L;

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
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

        private static ServiceProvider BuildServicesWithFakeAsycudaSync(bool shouldThrow)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = TestConnectionString,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        // Overrides the real Oracle-backed sync service — this test only exercises
        // the Worker's watermark logic, never real Oracle.
        services.AddScoped<IAsycudaSyncService>(_ => new FakeAsycudaSyncService(shouldThrow));

        return services.BuildServiceProvider();
    }

    private sealed class FakeAsycudaSyncService : IAsycudaSyncService
    {
        private readonly bool _shouldThrow;

        public FakeAsycudaSyncService(bool shouldThrow)
        {
            _shouldThrow = shouldThrow;
        }

        public Task<IReadOnlyList<CusDecSyncRow>> GetChangedCusDecsAsync(DateTime since, CancellationToken cancellationToken)
        {
            if (_shouldThrow)
            {
                throw new InvalidOperationException("Simulated Oracle failure for watermark test.");
            }

            return Task.FromResult<IReadOnlyList<CusDecSyncRow>>(Array.Empty<CusDecSyncRow>());
        }
    }

        [Fact]
    public async Task Worker_AdvancesWatermark_OnSuccessfulSync()
    {
        using var provider = BuildServicesWithFakeAsycudaSync(shouldThrow: false);

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CdnsDbContext>();
            var leftover = await db.TaskScheduleAppDetails.FirstOrDefaultAsync(a => a.AppId == AsycudaSyncAppId);
            if (leftover is not null)
            {
                db.TaskScheduleAppDetails.Remove(leftover);
                await db.SaveChangesAsync();
            }
        }

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var logger = provider.GetRequiredService<ILogger<WorkerService>>();
        var worker = new WorkerService(scopeFactory, logger);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(1000); // let the first sync cycle run
        await worker.StopAsync(CancellationToken.None);

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CdnsDbContext>();
            var appDetail = await db.TaskScheduleAppDetails.FirstOrDefaultAsync(a => a.AppId == AsycudaSyncAppId);

            Assert.NotNull(appDetail);
            Assert.True(appDetail!.IsSuccess);
            Assert.NotNull(appDetail.LastRunDateTime);
            Assert.Null(appDetail.Errors);

            db.TaskScheduleAppDetails.Remove(appDetail);
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Worker_DoesNotAdvanceWatermark_OnFailedSync()
    {
        using var provider = BuildServicesWithFakeAsycudaSync(shouldThrow: true);

        var previousRunTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CdnsDbContext>();
            var leftover = await db.TaskScheduleAppDetails.FirstOrDefaultAsync(a => a.AppId == AsycudaSyncAppId);
            if (leftover is not null)
            {
                db.TaskScheduleAppDetails.Remove(leftover);
                await db.SaveChangesAsync();
            }

            db.TaskScheduleAppDetails.Add(new Cdns.Domain.Entities.SystemInfrastructureAuditing.TaskScheduleAppDetail
            {
                AppId = AsycudaSyncAppId,
                AppName = AsycudaSyncAppName,
                LastRunDateTime = previousRunTime,
                IsSuccess = true,
                Errors = null,
            });
            await db.SaveChangesAsync();
        }

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var logger = provider.GetRequiredService<ILogger<WorkerService>>();
        var worker = new WorkerService(scopeFactory, logger);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(1000);
        await worker.StopAsync(CancellationToken.None);

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CdnsDbContext>();
            var appDetail = await db.TaskScheduleAppDetails.FirstOrDefaultAsync(a => a.AppId == AsycudaSyncAppId);

            Assert.NotNull(appDetail);
            Assert.False(appDetail!.IsSuccess);
            Assert.Equal(previousRunTime, appDetail.LastRunDateTime); // watermark NOT advanced
            Assert.NotNull(appDetail.Errors);

            db.TaskScheduleAppDetails.Remove(appDetail);
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task ProcessAsync_InsertsNewCusDec_WhenNoneExists()
    {
        using var provider = BuildServices();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CdnsDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<CusDecSyncProcessor>();

        var leftover = await db.CusDecs.FirstOrDefaultAsync(c => c.CusDecExternalSystemNumber == TestExternalId);
        if (leftover is not null)
        {
            db.CusDecs.Remove(leftover);
            await db.SaveChangesAsync();
        }

        var row = new CusDecSyncRow
        {
            CusDecExternalSystemNumber = TestExternalId,
            CusDecNumber = "TEST-0001",
            DeclarantTinNumber = "123456789",
            ConsigneeTinNumber = "987654321",
            InvoiceValue = 1000m,
            CusDecType = "IM",
            ConsigneeCompanyName = "Test Consignee",
            SupplierCompanyName = "Test Supplier",
            GlobalIdentity = Guid.NewGuid(),
            RegistrationDate = new DateTime(2026, 1, 1),
        };

        await processor.ProcessAsync(new[] { row }, CancellationToken.None);

        var inserted = await db.CusDecs
        .FirstOrDefaultAsync(c => c.CusDecExternalSystemNumber == TestExternalId);

        Assert.NotNull(inserted);
        Assert.Equal("TEST-0001", inserted!.CusDecNumber);
        Assert.Equal("Test Consignee", inserted.ConsigneeCompanyName);
        Assert.Equal("Test Supplier", inserted.SupplierCompanyName);

        Assert.Null(inserted.BlNumber);
        Assert.Null(inserted.NoOfItems);
        Assert.Null(inserted.Exporter);
        Assert.Null(inserted.DeclarantCompanyName);

        db.CusDecs.Remove(inserted);
        await db.SaveChangesAsync();

    }
        [Fact]
    public async Task ProcessAsync_UpdatesOnlyLegacySpecifiedFields_WhenCusDecAlreadyExists()
    {
        using var provider = BuildServices();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CdnsDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<CusDecSyncProcessor>();

        var leftover = await db.CusDecs.FirstOrDefaultAsync(c => c.CusDecExternalSystemNumber == TestExternalId);
        if (leftover is not null)
        {
            db.CusDecs.Remove(leftover);
            await db.SaveChangesAsync();
        }

        // Seed an existing CusDec directly (bypassing the processor), with values that
        // should NOT change on update — only the six legacy-specified fields should.
        var originalCusDecNumber = "ORIGINAL-0001";
        var originalInvoiceValue = 500m;
        var originalOrigin = "LK";

        db.CusDecs.Add(new Cdns.Domain.Entities.CusdecTables.CusDec
        {
            CusDecExternalSystemNumber = TestExternalId,
            CusDecNumber = originalCusDecNumber,
            DeclarantTinNumber = "123456789",
            ConsigneeTinNumber = "987654321",
            InvoiceValue = originalInvoiceValue,
            Origin = originalOrigin,
            GlobalIdentity = Guid.NewGuid(),
            NoOfContainers = 0,
            LastSyncDateTime = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-1), DateTimeKind.Unspecified),
        });
        await db.SaveChangesAsync();

        // Detach everything so the processor starts with a clean change tracker,
        // same as it would in a real sync run.
        foreach (var entry in db.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var newReleaseDate = new DateTime(2026, 2, 1);

        var row = new CusDecSyncRow
        {
            CusDecExternalSystemNumber = TestExternalId,
            CusDecNumber = "SHOULD-NOT-OVERWRITE",
            DeclarantTinNumber = "123456789",
            ConsigneeTinNumber = "987654321",
            InvoiceValue = 999999m,
            Origin = "SHOULD-NOT-OVERWRITE",
            ReleaseDate = newReleaseDate,
            ConsigneeCompanyName = "Updated Consignee",
            SupplierCompanyName = "Updated Supplier",
            GlobalIdentity = Guid.NewGuid(),
        };

        await processor.ProcessAsync(new[] { row }, CancellationToken.None);

        var updated = await db.CusDecs
            .FirstOrDefaultAsync(c => c.CusDecExternalSystemNumber == TestExternalId);

        Assert.NotNull(updated);

        // These should have changed:
        Assert.Equal(newReleaseDate, updated!.ReleaseDate);
        Assert.Equal("Updated Consignee", updated.ConsigneeCompanyName);
        Assert.Equal("Updated Supplier", updated.SupplierCompanyName);

        // Everything else should be untouched by the update path:
        Assert.Equal(originalCusDecNumber, updated.CusDecNumber);
        Assert.Equal(originalInvoiceValue, updated.InvoiceValue);
        Assert.Equal(originalOrigin, updated.Origin);

        db.CusDecs.Remove(updated);
        await db.SaveChangesAsync();
    }
        [Fact]
    public async Task ProcessAsync_SetsCompanyIds_WhenTinMatchesExistingCompany()
    {
        using var provider = BuildServices();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CdnsDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<CusDecSyncProcessor>();

        var leftover = await db.CusDecs.FirstOrDefaultAsync(c => c.CusDecExternalSystemNumber == TestExternalId);
        if (leftover is not null)
        {
            db.CusDecs.Remove(leftover);
            await db.SaveChangesAsync();
        }

        // Use a TIN longer than 9 chars to prove the truncate-to-9 matching logic works,
        // same as legacy's LEFT(TinNumber, 9) behavior.
        var declarantTinFull = "111222333999"; // truncates to "111222333"
        var consigneeTinFull = "444555666888"; // truncates to "444555666"

        var declarantCompanyName = "Test Declarant Co " + Guid.NewGuid();
        var consigneeCompanyName = "Test Consignee Co " + Guid.NewGuid();

        var existingDeclarant = await db.Companies.FirstOrDefaultAsync(c => c.TinNumber == "111222333");
        if (existingDeclarant is not null)
        {
            db.Companies.Remove(existingDeclarant);
        }
        var existingConsignee = await db.Companies.FirstOrDefaultAsync(c => c.TinNumber == "444555666");
        if (existingConsignee is not null)
        {
            db.Companies.Remove(existingConsignee);
        }
        await db.SaveChangesAsync();

        db.Companies.Add(new Cdns.Domain.Entities.CompanySubscriptionManagement.Company
        {
            CompanyName = declarantCompanyName,
            TinNumber = "111222333",
        });
        db.Companies.Add(new Cdns.Domain.Entities.CompanySubscriptionManagement.Company
        {
            CompanyName = consigneeCompanyName,
            TinNumber = "444555666",
        });
        await db.SaveChangesAsync();

        var declarantCompanyId = await db.Companies
            .Where(c => c.TinNumber == "111222333")
            .Select(c => c.CompanyId)
            .FirstAsync();
        var consigneeCompanyId = await db.Companies
            .Where(c => c.TinNumber == "444555666")
            .Select(c => c.CompanyId)
            .FirstAsync();

        foreach (var entry in db.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var row = new CusDecSyncRow
        {
            CusDecExternalSystemNumber = TestExternalId,
            CusDecNumber = "TEST-TIN-0001",
            DeclarantTinNumber = declarantTinFull,
            ConsigneeTinNumber = consigneeTinFull,
            GlobalIdentity = Guid.NewGuid(),
        };

        await processor.ProcessAsync(new[] { row }, CancellationToken.None);

        var inserted = await db.CusDecs
            .FirstOrDefaultAsync(c => c.CusDecExternalSystemNumber == TestExternalId);

        Assert.NotNull(inserted);
        Assert.Equal(declarantCompanyId, inserted!.DeclarantCompanyId);
        Assert.Equal(consigneeCompanyId, inserted.ConsigneeCompanyId);

        db.CusDecs.Remove(inserted);
        await db.SaveChangesAsync();

        var declarantCompany = await db.Companies.FirstAsync(c => c.CompanyId == declarantCompanyId);
        var consigneeCompany = await db.Companies.FirstAsync(c => c.CompanyId == consigneeCompanyId);
        db.Companies.Remove(declarantCompany);
        db.Companies.Remove(consigneeCompany);
        await db.SaveChangesAsync();
    }
}