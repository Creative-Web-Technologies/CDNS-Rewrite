using Cdns.Api.Controllers;
using Cdns.Application.Interfaces;
using Cdns.Domain.Enums;
using Cdns.Infrastructure;
using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Cdns.Infrastructure.Tests;

public class DiTests
{
    [Fact]
    public void AllControllers_CanBeResolvedFromContainer()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=ditest_throwaway;Username=x;Password=x"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddLogging();
        var env = new TestHostEnvironment();
        services.AddSingleton<IHostEnvironment>(env);
        services.AddInfrastructure(config, env);

        // Replace the Npgsql DbContext with InMemory so no DB connection is attempted
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CdnsDbContext>));
        if (descriptor is not null) services.Remove(descriptor);
        services.AddDbContext<CdnsDbContext>(o => o.UseInMemoryDatabase("di_test_throwaway"));

        // ICurrentUser and IHttpContextAccessor are registered in Program.cs, not AddInfrastructure
        services.AddScoped<ICurrentUser, FakeCurrentUser>(_ => new FakeCurrentUser(0));
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        // Verify every controller's constructor arguments can be satisfied
        var _ = ActivatorUtilities.CreateInstance<MemberController>(sp);
        var __ = ActivatorUtilities.CreateInstance<CompanyController>(sp);
        var ___ = ActivatorUtilities.CreateInstance<SignupController>(sp);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
