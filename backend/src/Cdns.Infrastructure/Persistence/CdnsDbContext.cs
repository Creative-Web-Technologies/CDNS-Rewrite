using Microsoft.EntityFrameworkCore;
using Cdns.Domain.Entities.AdminPortal;
using Cdns.Domain.Entities.MemberLoginTables;
using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Entities.CusdecTables;


namespace Cdns.Infrastructure.Persistence;

public class CdnsDbContext : DbContext
{
    public CdnsDbContext(DbContextOptions<CdnsDbContext> options) : base(options) { }
    
    //Admin Portal
    public DbSet<User> User => Set<User>();
    public DbSet<UserGroupMapping> UserGroupMappings => Set<UserGroupMapping>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Department => Set<Department>();
    public DbSet<Group> Group => Set<Group>();
    public DbSet<Module> Module => Set<Module>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemFunction> MenuItemFunction => Set<MenuItemFunction>();
    public DbSet<AccessControl> AccessControl => Set<AccessControl>();
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

    //Cusdec Tables
    public DbSet<CusDecStatusMapping> CusDecStatusMappings => Set<CusDecStatusMapping>();
    public DbSet<CusDec> CusDecs => Set<CusDec>();
    public DbSet<CusDecContainer> CusDecContainers => Set<CusDecContainer>();
    public DbSet<CusDecContainerStatus>CusDecContainerStatuses => Set<CusDecContainerStatus>(); 
    public DbSet<CusDecItem> CusDecItems => Set<CusDecItem>();
    public DbSet<CusDecStatus> CusDecStatuses => Set<CusDecStatus>();
    
 
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CdnsDbContext).Assembly);
    }
}

