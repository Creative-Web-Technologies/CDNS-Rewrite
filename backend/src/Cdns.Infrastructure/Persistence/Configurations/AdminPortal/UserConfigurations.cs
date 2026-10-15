using Cdns.Domain.Entities.AdminPortal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Staff;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table name (PostgreSQL convention usually handles snake_case, but can be explicit if needed)
        // builder.ToTable("user");

        // Primary key (value comes from Employee, not auto-generated)
        builder.HasKey(x => x.EmployeeId);
        builder.Property(x => x.EmployeeId).ValueGeneratedNever();

        // Columns & constraints
        builder.Property(x => x.UserName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Password).HasMaxLength(500);
        builder.Property(x => x.PasswordSalt).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        
        builder.Property(x => x.IsActive).HasDefaultValue(false);
        builder.Property(x => x.IsSuspended).HasDefaultValue(false);
        builder.Property(x => x.OtpSequence).HasDefaultValue(0);

        // Indexes
        builder.HasIndex(x => x.UserName).IsUnique();

        // Relationship: one Employee has one User
        builder.HasOne(x => x.Employee)
               .WithOne() // Or .WithOne(emp => emp.User) if Employee has a navigation property back
               .HasForeignKey<User>(x => x.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}