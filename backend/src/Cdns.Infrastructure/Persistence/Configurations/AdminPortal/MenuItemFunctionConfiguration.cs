using Cdns.Domain.Entities.AdminPortal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.AdminPortal;

public class MenuItemFunctionConfiguration : IEntityTypeConfiguration<MenuItemFunction>
{
    public void Configure(EntityTypeBuilder<MenuItemFunction> builder)
    {
        // Primary Key (Value provided manually, not identity)
        builder.HasKey(x => x.MenuItemFunctionId);
        builder.Property(x => x.MenuItemFunctionId)
               .ValueGeneratedNever();

        // Columns & Constraints
        builder.Property(x => x.MenuItemFunctionName).HasMaxLength(200);
        builder.Property(x => x.Area).HasMaxLength(200);
        builder.Property(x => x.Controller).HasMaxLength(200);
        builder.Property(x => x.Action).HasMaxLength(200);
        builder.Property(x => x.IconName).HasMaxLength(200);

        builder.Property(x => x.MenuItemFunctionOrder).HasDefaultValue(0);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // Foreign Key Relationship: MenuItemFunction belongs to MenuItem
        builder.HasOne(x => x.MenuItem)
               .WithMany() // Or .WithMany(m => m.MenuItemFunctions) if you add a collection to MenuItem
               .HasForeignKey(x => x.MenuItemId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}