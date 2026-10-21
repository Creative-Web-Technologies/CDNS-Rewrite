
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.CompanySubscriptionManagement;


public class CompanyMemberMappingConfiguration : IEntityTypeConfiguration<CompanyMemberMapping>
{
    public void Configure(EntityTypeBuilder<CompanyMemberMapping> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.CompanyMemberMappingId);
        builder.Property(x => x.CompanyMemberMappingId)
               .ValueGeneratedOnAdd();

        // Columns
        builder.Property(x => x.IsDeleted)
               .HasDefaultValue(false);

        // Foreign Key: Company
        builder.HasOne(x => x.Company)
               .WithMany()
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Cascade);

        // Foreign Key: Member
        builder.HasOne(x => x.Member)
               .WithMany()
               .HasForeignKey(x => x.MemberId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}