using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.MemberSignup;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("members");

        builder.HasKey(x => x.MemberId);

        builder.Property(x => x.FirstName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(200);
        builder.Property(x => x.Email).HasMaxLength(500).IsRequired();
        builder.Property(x => x.NotificationEmail).HasMaxLength(500);
        builder.Property(x => x.Mobile).HasMaxLength(20);
        builder.Property(x => x.Nic).HasMaxLength(20);
        builder.Property(x => x.Designation).HasMaxLength(200);

        builder.Property(x => x.MemberType).IsRequired();
        builder.Property(x => x.DefaultSubscriptionType);

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}