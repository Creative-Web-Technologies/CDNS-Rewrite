using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.MemberSignup;

public class TemporaryMemberConfiguration : IEntityTypeConfiguration<TemporaryMember>
{
    public void Configure(EntityTypeBuilder<TemporaryMember> builder)
    {
        builder.HasKey(x => x.TemporaryMemberId);
        builder.Property(x => x.TemporaryMemberId).UseIdentityByDefaultColumn();

        builder.Property(x => x.FirstName).HasMaxLength(200);
        builder.Property(x => x.LastName).HasMaxLength(200);
        builder.Property(x => x.Email).HasMaxLength(500);
        builder.Property(x => x.Mobile).HasMaxLength(20);
        builder.Property(x => x.Nic).HasMaxLength(20);
        builder.Property(x => x.Designation).HasMaxLength(200);
        builder.Property(x => x.GlobalIdentity).HasMaxLength(50);
        // ProfileImage, Password, PasswordSalt: no max length, which maps to text

        builder.Property(x => x.CreatedDateTime)
               .IsRequired()
               .HasDefaultValueSql("now()");

        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.Property(x => x.DefaultSubscriptionType)
               .HasDefaultValue(SubscriptionType.Declarant);
    }
}