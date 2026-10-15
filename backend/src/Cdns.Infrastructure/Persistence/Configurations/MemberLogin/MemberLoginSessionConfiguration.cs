using Cdns.Domain.Entities.MemberLoginTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.MemberLogin;

public class MemberLoginSessionConfiguration : IEntityTypeConfiguration<MemberLoginSession>
{
    public void Configure(EntityTypeBuilder<MemberLoginSession> builder)
    {
        builder.HasKey(x => x.MemberLoginSessionId);
        builder.Property(x => x.MemberLoginSessionId).UseIdentityByDefaultColumn();

        builder.Property(x => x.SessionId).IsRequired();
        builder.HasIndex(x => x.SessionId).IsUnique();   // one row per session; login merges on it

        builder.Property(x => x.UserName)
               .IsRequired()
               .HasColumnType("citext");

        // Key: no max length, which maps to text

        builder.Property(x => x.LoginDateTime)
               .IsRequired()
               .HasDefaultValueSql("now()");

        // New FK (legacy had none); confirmed safe, 0 orphans
        builder.HasOne(x => x.Member)
               .WithMany()
               .HasForeignKey(x => x.MemberId)
               .OnDelete(DeleteBehavior.Restrict);

           // Hide a soft-deleted member's login history together with the member
        builder.HasQueryFilter(x => !x.Member.IsDeleted);
    }
}