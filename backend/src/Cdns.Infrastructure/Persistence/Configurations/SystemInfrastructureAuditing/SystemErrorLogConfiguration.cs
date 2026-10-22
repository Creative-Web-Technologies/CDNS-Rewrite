using Cdns.Domain.Entities.SystemInfrastructureAuditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.SystemInfrastructureAuditing;

public class SystemErrorLogConfiguration : IEntityTypeConfiguration<SystemErrorLog>
{
    public void Configure(EntityTypeBuilder<SystemErrorLog> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.SystemErrorLogId);
        builder.Property(x => x.SystemErrorLogId)
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