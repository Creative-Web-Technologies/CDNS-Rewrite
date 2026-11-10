using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Cdns.Infrastructure.Persistence;

public class CdnsDbContextFactory : IDesignTimeDbContextFactory<CdnsDbContext>
{
    public CdnsDbContext CreateDbContext(string[] args)
    {
        // AppContext.BaseDirectory = the startup project's bin output dir, where the
        // .NET SDK copies appsettings*.json automatically (CopyToOutputDirectory: PreserveNewest).
        // appsettings.Development.json is gitignored — copy appsettings.Development.example.json
        // to appsettings.Development.json and fill in your local password before running migrations.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default not found. Copy appsettings.Development.example.json " +
                "to appsettings.Development.json in src/Cdns.Api and set your local password.");

        var optionsBuilder = new DbContextOptionsBuilder<CdnsDbContext>();
        optionsBuilder
            .UseNpgsql(connectionString,
                x => x.MigrationsHistoryTable("__EFMigrationsHistory"))
            .UseSnakeCaseNamingConvention();

        return new CdnsDbContext(optionsBuilder.Options);
    }
}
