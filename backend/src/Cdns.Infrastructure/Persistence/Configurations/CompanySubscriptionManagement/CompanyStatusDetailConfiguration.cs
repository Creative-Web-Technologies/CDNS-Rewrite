using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.CompanySubscriptionManagement;


public class CompanyStatusDetailConfiguration : IEntityTypeConfiguration<CompanyStatusDetail>
{
    public void Configure(EntityTypeBuilder<CompanyStatusDetail> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.CompanyStatusDetailId);
        builder.Property(x => x.CompanyStatusDetailId)
               .ValueGeneratedOnAdd();

        // Columns & Constraints
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.CompanyStatusEnum).IsRequired();
        
        // Default timestamp using PostgreSQL function
        builder.Property(x => x.CreatedDateTime)
               .HasDefaultValueSql("now()")
               .IsRequired();

        // Foreign Key Relationship to Company
        builder.HasOne(x => x.Company)
               .WithMany() // Or .WithMany(c => c.CompanyStatusDetails) if you add a collection to Company
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}