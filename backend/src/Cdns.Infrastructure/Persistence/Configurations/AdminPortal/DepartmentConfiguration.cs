using Cdns.Domain.Entities.AdminPortal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.AdminPortal;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.DepartmentId);
        builder.Property(x => x.DepartmentId)
               .ValueGeneratedOnAdd();

        // Columns
        builder.Property(x => x.DepartmentName)
               .HasMaxLength(200);

        builder.Property(x => x.IsDeleted)
               .HasDefaultValue(false);
    }
}