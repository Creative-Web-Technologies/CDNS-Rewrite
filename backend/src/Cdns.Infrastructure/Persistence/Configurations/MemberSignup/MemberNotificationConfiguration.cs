using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.MemberSignup;

public class MemberNotificationConfiguration : IEntityTypeConfiguration<MemberNotification>
{
    public void Configure(EntityTypeBuilder<MemberNotification> builder)
    {
        builder.HasKey(x => x.MemberNotificationId);
        builder.Property(x => x.MemberNotificationId).UseIdentityByDefaultColumn();

        builder.Property(x => x.Title).HasMaxLength(500);
        // Message has no max length, which maps to text

        builder.Property(x => x.NotificationDateTime).IsRequired();

        builder.Property(x => x.IsSeen).HasDefaultValue(false);
        builder.Property(x => x.ShowOnHome).HasDefaultValue(false);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.NotificationType).HasDefaultValue(NotificationType.Info);

        builder.HasOne(x => x.Member)
               .WithMany()
               .HasForeignKey(x => x.MemberId)
               .OnDelete(DeleteBehavior.Restrict);

        // Proposed index for the bell/home query; confirm against the legacy Get procs
        builder.HasIndex(x => new { x.MemberId, x.IsDeleted, x.IsSeen, x.NotificationDateTime });
    }
}