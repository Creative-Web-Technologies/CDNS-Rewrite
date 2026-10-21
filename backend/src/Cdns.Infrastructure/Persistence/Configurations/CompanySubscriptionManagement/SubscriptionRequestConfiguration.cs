using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.CompanySubscriptionManagement;

public class SubscriptionRequestConfiguration : IEntityTypeConfiguration<SubscriptionRequest>
{
    public void Configure(EntityTypeBuilder<SubscriptionRequest> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.SubscriptionRequestId);
        builder.Property(x => x.SubscriptionRequestId)
               .ValueGeneratedOnAdd();

        // Columns & Constraints
        builder.Property(x => x.MemberId).IsRequired();

        builder.Property(x => x.RequestedDateTime)
               .HasDefaultValueSql("now()")
               .IsRequired();

        builder.Property(x => x.SubscriptionRequestNumber)
               .HasMaxLength(200);

        builder.Property(x => x.SubscriptionRequestStatusEnum)
               .HasDefaultValue(0);

        builder.Property(x => x.SubscriptionTypeEnum)
               .HasDefaultValue(0);

        // PostgreSQL function for NEWID() / UUID generation
        builder.Property(x => x.GlobalIdentity)
               .HasDefaultValueSql("gen_random_uuid()")
               .IsRequired();

        builder.Property(x => x.IsDeleted)
               .HasDefaultValue(false);

        // Foreign Key: Member
        builder.HasOne(x => x.Member)
               .WithMany()
               .HasForeignKey(x => x.MemberId)
               .OnDelete(DeleteBehavior.Restrict);

        // Foreign Key: Company
        builder.HasOne(x => x.Company)
               .WithMany()
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}