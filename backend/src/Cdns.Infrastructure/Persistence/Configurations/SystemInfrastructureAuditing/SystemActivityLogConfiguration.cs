using Cdns.Domain.Entities.SystemInfrastructureAuditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.SystemInfrastructureAuditing;

public class SystemActivityLogConfiguration : IEntityTypeConfiguration<SystemActivityLog>
{
    public void Configure(EntityTypeBuilder<SystemActivityLog> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.SystemActivityLogId);
        builder.Property(x => x.SystemActivityLogId)
               .ValueGeneratedOnAdd();

        // Columns & Constraints
        builder.Property(x => x.Activity)
               .HasMaxLength(200);

        builder.Property(x => x.StoredProcedureName)
               .HasMaxLength(300);

        builder.Property(x => x.LogDateTime)
               .HasDefaultValueSql("now()");

        // Foreign Key: Employee (Restrict Delete)
        builder.HasOne(x => x.Employee)
               .WithMany()
               .HasForeignKey(x => x.EmployeeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}