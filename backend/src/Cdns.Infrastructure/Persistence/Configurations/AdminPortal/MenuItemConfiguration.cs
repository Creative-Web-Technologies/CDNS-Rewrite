using Cdns.Domain.Entities.AdminPortal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.AdminPortal;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        // Primary Key (Value provided manually, not identity)
        builder.HasKey(x => x.MenuItemId);
        builder.Property(x => x.MenuItemId)
               .ValueGeneratedNever();

        // Columns & Constraints
        builder.Property(x => x.MenuItemName).HasMaxLength(200);
        builder.Property(x => x.Area).HasMaxLength(200);
        builder.Property(x => x.Controller).HasMaxLength(200);
        builder.Property(x => x.Action).HasMaxLength(200);
        builder.Property(x => x.IconName).HasMaxLength(200);
        builder.Property(x => x.WorkSpaceId).HasMaxLength(50);
        builder.Property(x => x.ReportId).HasMaxLength(50);

        builder.Property(x => x.MenuItemOrder).HasDefaultValue(0);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.IsPowerBiDashboard).HasDefaultValue(false);

        // Foreign Key Relationship: MenuItem belongs to Menu
        builder.HasOne(x => x.Menu)
               .WithMany() // Or .WithMany(m => m.MenuItems) if you add a collection to Menu
               .HasForeignKey(x => x.MenuId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}