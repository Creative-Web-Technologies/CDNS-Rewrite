using global::Cdns.Domain.Entities.AdminPortal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.AdminPortal;

public class UserGroupMappingConfiguration : IEntityTypeConfiguration<UserGroupMapping>
{
    public void Configure(EntityTypeBuilder<UserGroupMapping> builder)
    {
        builder.HasKey(x => x.UserGroupMappingId);
        builder.Property(x => x.UserGroupMappingId)
               .ValueGeneratedOnAdd();

        builder.HasOne(x => x.Employee)
               .WithMany()
               .HasForeignKey(x => x.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Group)
               .WithMany()
               .HasForeignKey(x => x.GroupId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}