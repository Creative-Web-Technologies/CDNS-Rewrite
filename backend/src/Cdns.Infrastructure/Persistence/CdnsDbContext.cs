using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Entities.MemberLoginTables;
using Microsoft.EntityFrameworkCore;

namespace Cdns.Infrastructure.Persistence;

public class CdnsDbContext : DbContext
{
    public CdnsDbContext(DbContextOptions<CdnsDbContext> options) : base(options) { }

    //Signup Tables
    public DbSet<Member> Members => Set<Member>(); 
    public DbSet<MemberNotification> MemberNotifications => Set<MemberNotification>();
    public DbSet<TemporaryMember> TemporaryMembers => Set<TemporaryMember>();

    //Login Tables
    public DbSet<MemberUser> MemberUsers => Set<MemberUser>();
    public DbSet<MemberLoginSession> MemberLoginSessions => Set<MemberLoginSession>();
    public DbSet<MemberUserLoginDetail> MemberUserLoginDetails => Set<MemberUserLoginDetail>();
    public DbSet<MemberActivityLog> MemberActivityLogs => Set<MemberActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CdnsDbContext).Assembly);
    }
}

