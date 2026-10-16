using Cdns.Domain.Entities.AdminPortal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.AdminPortal;

public class LoginUserActivityConfiguration : IEntityTypeConfiguration<LoginUserActivity>
{
    public void Configure(EntityTypeBuilder<LoginUserActivity> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.LoginUserActivityId);
        builder.Property(x => x.LoginUserActivityId)
               .ValueGeneratedOnAdd();

        // Columns & Constraints
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.GlobalIdentity).IsRequired();
        builder.Property(x => x.LoggedIpAddress).HasMaxLength(100);
        builder.Property(x => x.LoggedDevice).HasMaxLength(100);
        builder.Property(x => x.LoggedDateTime).IsRequired();

        // Foreign Key: User (UserId points to User.EmployeeId)
        builder.HasOne(x => x.User)
               .WithMany() // Or .WithMany(u => u.LoginUserActivities) if you add a collection on User
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}