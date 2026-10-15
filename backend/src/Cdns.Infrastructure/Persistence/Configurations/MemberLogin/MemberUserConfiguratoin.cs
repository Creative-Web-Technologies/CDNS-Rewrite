using Cdns.Domain.Entities.MemberLoginTables;
using Cdns.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.MemberLogin;

public class MemberUserConfiguration : IEntityTypeConfiguration<MemberUser>
{
    public void Configure(EntityTypeBuilder<MemberUser> builder)
    {
        // PK is also the FK to Member (1:1). MemberId is unique in the data.
        builder.HasKey(x => x.MemberId);
        builder.Property(x => x.MemberId).ValueGeneratedNever();

        // citext keeps the legacy case-insensitive behaviour. The 200-char limit is app validation.
        builder.Property(x => x.UserName)
               .IsRequired()
               .HasColumnType("citext");
        builder.HasIndex(x => x.UserName).IsUnique();   // legacy UQ_MemberUser_UserName

        // Legacy PBKDF2 hash and salt, kept as-is so migrated members can still log in
        builder.Property(x => x.Password).IsRequired();
        builder.Property(x => x.PasswordSalt).IsRequired();

        builder.Property(x => x.CreatedDateTime).IsRequired();
        builder.Property(x => x.ActivationCode).IsRequired();
        builder.Property(x => x.GlobalIdentity).IsRequired();   // never used for lookups, so no index
        builder.Property(x => x.LastOtpGeneratedDateTime).IsRequired();

        builder.Property(x => x.UserStatusType).HasDefaultValue(UserStatusType.NotActive);
        builder.Property(x => x.MemberType).HasDefaultValue(MemberType.Member).HasSentinel(MemberType.Member);
        builder.Property(x => x.IsActiveOtp).HasDefaultValue(false);
        builder.Property(x => x.RequiredEregVerification).HasDefaultValue(false);
        builder.Property(x => x.IsWrongAttempt).HasDefaultValue(false);
        builder.Property(x => x.LastLoginAttemptDateTime).HasDefaultValueSql("now()");

        builder.HasOne(x => x.Member)
               .WithOne()
               .HasForeignKey<MemberUser>(x => x.MemberId)
               .OnDelete(DeleteBehavior.Restrict);

           // Hide a soft-deleted member's login history together with the member
        builder.HasQueryFilter(x => !x.Member.IsDeleted);
    }
}