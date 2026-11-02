using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cdns.Infrastructure.Persistence;

public class CdnsDbContextFactory : IDesignTimeDbContextFactory<CdnsDbContext>
{
    public CdnsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CdnsDbContext>();
        
        // Put your REAL Postgres password here
        optionsBuilder.UseNpgsql(
        "Host=localhost;Port=5432;Database=cdns_dev;Username=postgres;Password=chirath2004", 
        x => x.MigrationsHistoryTable("__EFMigrationsHistory") // <-- Make sure this is here!
    );

        return new CdnsDbContext(optionsBuilder.Options);
    }
}