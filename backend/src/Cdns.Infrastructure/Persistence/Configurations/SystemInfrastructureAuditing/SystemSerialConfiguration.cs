using Cdns.Domain.Entities.SystemInfrastructureAuditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.SystemInfrastructureAuditing;

public class SystemSerialConfiguration : IEntityTypeConfiguration<SystemSerial>
{
    public void Configure(EntityTypeBuilder<SystemSerial> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.SystemSerialId);
        builder.Property(x => x.SystemSerialId)
               .ValueGeneratedOnAdd();

        // Columns, Limits & Defaults
       builder.Property(x => x.AutoIncrementSerial)
              .HasDefaultValue(0L);

       builder.Property(x => x.DailyIncrementSerial)
              .HasDefaultValue(0L);

       builder.Property(x => x.MonthlyIncrementSerial)
              .HasDefaultValue(0L);

       builder.Property(x => x.YearlyIncrementSerial)
              .HasDefaultValue(0L);

        builder.Property(x => x.SerialFormatExpression)
               .HasMaxLength(400);

        builder.Property(x => x.SerialName)
               .HasMaxLength(250);

        builder.Property(x => x.DocumentTypeEnum)
               .HasDefaultValue(0);

        builder.Property(x => x.IsDeleted)
               .HasDefaultValue(false);

        builder.Property(x => x.IsDefaultSerial)
               .HasDefaultValue(false);
    }
}