using Cdns.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Company;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.SubscriptionId);
        builder.Property(x => x.SubscriptionId)
               .ValueGeneratedOnAdd();

        // Columns & Constraints
        builder.Property(x => x.SubscriptionName)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(x => x.Description)
               .IsRequired();

        builder.Property(x => x.Amount)
               .HasPrecision(18, 4)
               .IsRequired();

        builder.Property(x => x.NoOfActiveDuration)
               .IsRequired();

        builder.Property(x => x.SubscriptionTypeEnum)
               .HasDefaultValue(0);

        builder.Property(x => x.IsDeleted)
               .HasDefaultValue(false);

        builder.Property(x => x.SubscriptionActionTypeEnum)
               .HasDefaultValue(0);

        builder.Property(x => x.SubscriptionStatusEnum)
               .HasDefaultValue(0);

        builder.Property(x => x.CreatedDateTime)
               .HasDefaultValueSql("now()");
    }
}