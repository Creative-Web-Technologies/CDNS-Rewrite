using Cdns.Domain.Entities.AdminPortal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.AdminPortal;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.EmployeeId);
        builder.Property(x => x.EmployeeId)
               .ValueGeneratedOnAdd();

        // Columns & Constraints
        builder.Property(x => x.FirstName).HasMaxLength(100);
        builder.Property(x => x.LastName).HasMaxLength(100);
        builder.Property(x => x.EmployeeCode).HasMaxLength(100);
        builder.Property(x => x.Email).HasMaxLength(100);
        
        builder.Property(x => x.IsDeleted)
               .HasDefaultValue(false);

        // Foreign Key: Department (Restrict Delete)
        builder.HasOne(x => x.Department)
               .WithMany() // Or .WithMany(d => d.Employees) if you add a collection to Department
               .HasForeignKey(x => x.DepartmentId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}