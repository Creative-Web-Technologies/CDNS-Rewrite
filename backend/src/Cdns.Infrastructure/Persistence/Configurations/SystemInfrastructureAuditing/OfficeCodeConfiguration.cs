using Cdns.Domain.Entities.SystemInfrastructureAuditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.SystemInfrastructureAuditing;

public class OfficeCodeConfiguration : IEntityTypeConfiguration<OfficeCode>
{
    public void Configure(EntityTypeBuilder<OfficeCode> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.OfficeCodeId);
        builder.Property(x => x.OfficeCodeId)
               .ValueGeneratedOnAdd();

        // Columns & Constraints
        builder.Property(x => x.OfficeName)
               .HasMaxLength(200);

        builder.Property(x => x.Code)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(x => x.IsDeleted)
               .HasDefaultValue(false);
    }
}