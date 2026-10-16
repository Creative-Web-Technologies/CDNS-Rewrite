using Cdns.Domain.Entities.AdminPortal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.AdminPortal;

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        // Primary Key (Value provided manually, not identity)
        builder.HasKey(x => x.ModuleId);
        builder.Property(x => x.ModuleId)
               .ValueGeneratedNever();

        // Columns & Constraints
        builder.Property(x => x.ModuleName).HasMaxLength(200);
        builder.Property(x => x.Area).HasMaxLength(200);
        builder.Property(x => x.Controller).HasMaxLength(200);
        builder.Property(x => x.Action).HasMaxLength(200);
        builder.Property(x => x.IconName).HasMaxLength(200);
        builder.Property(x => x.SmallIconName).HasMaxLength(200);
        builder.Property(x => x.BgColorClass).HasMaxLength(50);

        builder.Property(x => x.ModuleOrder).HasDefaultValue(0);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
    }
}