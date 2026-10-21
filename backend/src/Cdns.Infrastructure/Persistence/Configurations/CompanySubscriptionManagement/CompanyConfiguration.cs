using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.CompanySubscriptionManagement;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.CompanyId);
        builder.Property(x => x.CompanyId)
               .ValueGeneratedOnAdd();

        // Columns & Constraints
        builder.Property(x => x.CompanyName)
               .HasMaxLength(500)
               .IsRequired();

        builder.Property(x => x.TinNumber)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(x => x.ERegSystemReferenceNumber)
               .HasMaxLength(500);

        builder.Property(x => x.IsDeleted)
               .HasDefaultValue(false);

        builder.Property(x => x.CompanyStatusEnum)
               .HasDefaultValue(0);
    }
}