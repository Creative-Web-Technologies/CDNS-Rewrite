using Cdns.Domain.Entities.AdminPortal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.AdminPortal;

public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        // Primary Key (Value provided manually, not identity)
        builder.HasKey(x => x.MenuId);
        builder.Property(x => x.MenuId)
               .ValueGeneratedNever();

        // Columns & Constraints
        builder.Property(x => x.MenuName).HasMaxLength(200);
        builder.Property(x => x.Area).HasMaxLength(200);
        builder.Property(x => x.Controller).HasMaxLength(200);
        builder.Property(x => x.Action).HasMaxLength(200);
        builder.Property(x => x.IconName).HasMaxLength(200);

        builder.Property(x => x.MenuOrder).HasDefaultValue(0);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // Foreign Key Relationship: Menu belongs to Module
        builder.HasOne(x => x.Module)
               .WithMany() // Or .WithMany(m => m.Menus) if you want a collection of Menus on Module
               .HasForeignKey(x => x.ModuleId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}