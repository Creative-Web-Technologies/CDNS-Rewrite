
using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.CompanySubscriptionManagement;


public class MemberSubscriptionConfiguration : IEntityTypeConfiguration<MemberSubscription>
{
    public void Configure(EntityTypeBuilder<MemberSubscription> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.MemberSubscriptionId);
        builder.Property(x => x.MemberSubscriptionId)
               .ValueGeneratedOnAdd();

        // Columns & Constraints
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.PaidAmount).HasPrecision(18, 2).HasDefaultValue(0);
        builder.Property(x => x.SubscriptionAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.MemberSubscriptionStatusEnum).HasDefaultValue(0);
        
        builder.Property(x => x.MemberSubscriptionNumber).HasMaxLength(200);
        builder.Property(x => x.SubscriptionName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TransactionReferenceNumber).HasMaxLength(200);
        
        builder.Property(x => x.GlobalIdentity).IsRequired();

        // Foreign Key: Member (Cascade Delete)
        builder.HasOne(x => x.Member)
               .WithMany()
               .HasForeignKey(x => x.MemberId)
               .OnDelete(DeleteBehavior.Cascade);

        // Foreign Key: Subscription (Cascade Delete)
        builder.HasOne(x => x.Subscription)
               .WithMany()
               .HasForeignKey(x => x.SubscriptionId)
               .OnDelete(DeleteBehavior.Cascade);

        // Foreign Key: SubscriptionRequest
        builder.HasOne(x => x.SubscriptionRequest)
               .WithMany()
               .HasForeignKey(x => x.SubscriptionRequestId)
               .OnDelete(DeleteBehavior.Restrict);

        // Foreign Key: Company
        builder.HasOne(x => x.Company)
               .WithMany()
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}