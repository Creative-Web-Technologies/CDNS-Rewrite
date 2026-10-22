using Microsoft.EntityFrameworkCore;
using Cdns.Domain.Entities.AdminPortal;
using Cdns.Domain.Entities.MemberLoginTables;
using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Entities.CusdecTables;
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Entities.SystemInfrastructureAuditing;
using Cdns.Domain.Entities.CustomerSupportTickets; 


namespace Cdns.Infrastructure.Persistence;

public class CdnsDbContext : DbContext
{
    public CdnsDbContext(DbContextOptions<CdnsDbContext> options) : base(options) { }
    
    //Admin Portal Tables
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

    //Company & subscription managment Tables
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyStatusDetail> CompanyStatusDetails => Set<CompanyStatusDetail>();
    public DbSet<CompanyMemberMapping> CompanyMemberMappings => Set<CompanyMemberMapping>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionRequest> SubscriptionRequests => Set<SubscriptionRequest>();
    public DbSet<MemberSubscription> MemberSubscriptions => Set<MemberSubscription>();
    public DbSet<MemberSubscriptionStatusHistory> MemberSubscriptionStatusHistories => Set<MemberSubscriptionStatusHistory>();


    //Cusdec Tables
    public DbSet<CusDecStatusMapping> CusDecStatusMappings => Set<CusDecStatusMapping>();
    public DbSet<CusDec> CusDecs => Set<CusDec>();
    public DbSet<CusDecContainer> CusDecContainers => Set<CusDecContainer>();
    public DbSet<CusDecContainerStatus> CusDecContainerStatuses => Set<CusDecContainerStatus>(); 
    public DbSet<CusDecItem> CusDecItems => Set<CusDecItem>();
    public DbSet<CusDecStatus> CusDecStatuses => Set<CusDecStatus>();
    public DbSet<CusDecInspection> CusDecInspections => Set<CusDecInspection>();
    public DbSet<CusDecTax> CusDecTaxes => Set<CusDecTax>();
    public DbSet<CusDecReadyToProcessSetting> CusDecReadyToProcessSettings => Set<CusDecReadyToProcessSetting>();
    public DbSet<ReadyToProcessConsignee> ReadyToProcessConsignees => Set<ReadyToProcessConsignee>();
    public DbSet<CusDecInspectionMapping> CusDecInspectionMappings => Set<CusDecInspectionMapping>();
    public DbSet<CusDecInspectionSectionTabStatusMapping> CusDecInspectionSectionTabStatusMappings => Set<CusDecInspectionSectionTabStatusMapping>();
    public DbSet<CusDecPaymentSummary> CusDecPaymentSummaries => Set<CusDecPaymentSummary>();
    
    //System Infrastructure and Auditing
    public DbSet<SystemActivityLog> SystemActivityLogs => Set<SystemActivityLog>();
    public DbSet<SystemErrorLog> SystemErrorLogs => Set<SystemErrorLog>();
    public DbSet<SystemSerial> SystemSerials => Set<SystemSerial>();
    public DbSet<SmtpMailSetting> SmtpMailSettings => Set<SmtpMailSetting>();
    public DbSet<TaskScheduleAppDetail> TaskScheduleAppDetails => Set<TaskScheduleAppDetail>();
    public DbSet<OfficeCode> OfficeCodes => Set<OfficeCode>();
    public DbSet<CustomerSupportTicket> CustomerSupportTickets => Set<CustomerSupportTicket>();




 
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CdnsDbContext).Assembly);
    }
}

