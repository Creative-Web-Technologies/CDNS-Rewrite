using Cdns.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Company;

public class MemberSubscriptionStatusHistoryConfiguration : IEntityTypeConfiguration<MemberSubscriptionStatusHistory>
{
    public void Configure(EntityTypeBuilder<MemberSubscriptionStatusHistory> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.MemberSubscriptionStatusHistoryId);
        builder.Property(x => x.MemberSubscriptionStatusHistoryId)
               .ValueGeneratedOnAdd();

        // Columns & Constraints
        builder.Property(x => x.MemberSubscriptionStatusEnum)
               .IsRequired();

        builder.Property(x => x.CreatedDateTime)
               .HasDefaultValueSql("now()")
               .IsRequired();

        // Foreign Key: MemberSubscription
        builder.HasOne(x => x.MemberSubscription)
               .WithMany()
               .HasForeignKey(x => x.MemberSubscriptionId)
               .OnDelete(DeleteBehavior.Cascade);

        // Foreign Key: MasterMember (Member)
        builder.HasOne(x => x.MasterMember)
               .WithMany()
               .HasForeignKey(x => x.MasterMemberId)
               .OnDelete(DeleteBehavior.Restrict);

        // Foreign Key: Employee
        builder.HasOne(x => x.Employee)
               .WithMany()
               .HasForeignKey(x => x.EmployeeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}