using Cdns.Domain.Entities.MemberLoginTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.MemberLogin;

public class MemberUserLoginDetailConfiguration : IEntityTypeConfiguration<MemberUserLoginDetail>
{
    public void Configure(EntityTypeBuilder<MemberUserLoginDetail> builder)
    {
        builder.HasKey(x => x.MemberUserLoginDetailId);
        builder.Property(x => x.MemberUserLoginDetailId).UseIdentityByDefaultColumn();

        // Legacy allowed NULL; the data has none and the only writer inserts on a successful login
        builder.Property(x => x.LoginDateTime)
               .IsRequired()
               .HasDefaultValueSql("now()");

        builder.Property(x => x.IpAddress).HasMaxLength(50);
        // UserAgent: no max length, which maps to text

        // New FK (legacy had none); confirmed safe, 0 orphans
        builder.HasOne(x => x.Member)
               .WithMany()
               .HasForeignKey(x => x.MemberId)
               .OnDelete(DeleteBehavior.Restrict);

        // Hide a soft-deleted member's login history together with the member
        builder.HasQueryFilter(x => !x.Member.IsDeleted);
    }
}