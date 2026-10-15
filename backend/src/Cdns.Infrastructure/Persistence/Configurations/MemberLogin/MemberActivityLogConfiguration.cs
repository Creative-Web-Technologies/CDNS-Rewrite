using Cdns.Domain.Entities.MemberLoginTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.MemberLogin;

public class MemberActivityLogConfiguration : IEntityTypeConfiguration<MemberActivityLog>
{
    public void Configure(EntityTypeBuilder<MemberActivityLog> builder)
    {
        builder.HasKey(x => x.MemberActivityLogId);
        builder.Property(x => x.MemberActivityLogId).UseIdentityByDefaultColumn();

        builder.Property(x => x.Activity).HasMaxLength(200);
        builder.Property(x => x.StoredProcedureName).HasMaxLength(300);
        // ActivityJson: no max length, which maps to text

        // LogDateTime stays nullable like legacy until the NullLog check returns 0.
        // If it does, make the property non-nullable and add:
        // builder.Property(x => x.LogDateTime).IsRequired().HasDefaultValueSql("now()");

        // Nullable FK. Legacy rows with MemberId = 0 are loaded as NULL.
        builder.HasOne(x => x.Member)
               .WithMany()
               .HasForeignKey(x => x.MemberId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);
    }
}