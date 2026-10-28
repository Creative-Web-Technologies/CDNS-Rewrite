using Cdns.Domain.Entities.SystemInfrastructureAuditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.SystemInfrastructureAuditing;

public class TaskScheduleAppDetailConfiguration : IEntityTypeConfiguration<TaskScheduleAppDetail>
{
    public void Configure(EntityTypeBuilder<TaskScheduleAppDetail> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.TaskScheduleAppDetailId);
        builder.Property(x => x.TaskScheduleAppDetailId)
               .ValueGeneratedOnAdd();

        // Columns & Constraints
        builder.Property(x => x.AppId)
               .IsRequired();

        builder.Property(x => x.AppName)
               .HasMaxLength(500)
               .IsRequired();
    }
}