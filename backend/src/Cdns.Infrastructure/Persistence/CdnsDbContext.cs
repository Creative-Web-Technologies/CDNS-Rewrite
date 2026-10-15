using Microsoft.EntityFrameworkCore;
using Cdns.Domain.Entities.AdminPortal;

namespace Cdns.Infrastructure.Persistence;

public class CdnsDbContext : DbContext
{
    public CdnsDbContext(DbContextOptions<CdnsDbContext> options) : base(options) { }
    
    //Admin Portal
    public DbSet<User> User => Set<User>();
    public DbSet<UserGroupMapping> UserGroupMappings => Set<UserGroupMapping>();
    public DbSet<Employee> Employees => Set<Employee>();
    public Dbset<Department> Department => set<Department>();
    public DbSet<Group> Group => set<Group>();
    public Dbset<Module> Module => set<Module>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public Dbset<MenuItemFunction> MenuItemFunction => set<MenuItemFunction>();
    public Dbset<AccessControl> AccessControl => set<AccessControl>();
    public DbSet<LoginUserActivity> LoginUserActivities => Set<LoginUserActivity>();

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
        modelBuilder.HasPostgresExtension("citext")
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CdnsDbContext).Assembly);
    }
}

