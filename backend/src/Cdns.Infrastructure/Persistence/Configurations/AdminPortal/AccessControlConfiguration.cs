using Cdns.Domain.Entities.AdminPortal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.AdminPortal;

public class AccessControlConfiguration : IEntityTypeConfiguration<AccessControl>
{
    public void Configure(EntityTypeBuilder<AccessControl> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.AccessControlId);
        builder.Property(x => x.AccessControlId)
               .ValueGeneratedOnAdd();

        // Columns
        builder.Property(x => x.IsDeleted)
               .HasDefaultValue(false);

        // Foreign Key: Group (Cascade Delete)
        builder.HasOne(x => x.Group)
               .WithMany() // Or .WithMany(g => g.AccessControls) if you add a collection to Group
               .HasForeignKey(x => x.GroupId)
               .OnDelete(DeleteBehavior.Cascade);

        // Foreign Key: MenuItemFunction (Restrict Delete)
        builder.HasOne(x => x.MenuItemFunction)
               .WithMany() // Or .WithMany(m => m.AccessControls) if you add a collection to MenuItemFunction
               .HasForeignKey(x => x.MenuItemFunctionId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}