using global::Cdns.Domain.Entities.AdminPortal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.AdminPortal;

public class UserConfigurations : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.EmployeeId);
        builder.Property(x => x.EmployeeId).ValueGeneratedNever();

        builder.Property(x => x.UserName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Password).HasMaxLength(500);
        builder.Property(x => x.PasswordSalt).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        
        builder.Property(x => x.IsActive).HasDefaultValue(false);
        builder.Property(x => x.IsSuspended).HasDefaultValue(false);
        builder.Property(x => x.OtpSequence).HasDefaultValue(0);

        builder.HasIndex(x => x.UserName).IsUnique();

        builder.HasOne(x => x.Employee)
               .WithOne()
               .HasForeignKey<User>(x => x.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}