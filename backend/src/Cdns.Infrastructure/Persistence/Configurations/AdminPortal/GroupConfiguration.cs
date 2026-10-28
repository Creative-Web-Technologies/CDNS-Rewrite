using Cdns.Domain.Entities.AdminPortal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.AdminPortal;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.GroupId);
        builder.Property(x => x.GroupId)
               .ValueGeneratedOnAdd();

        // Columns
        builder.Property(x => x.GroupName)
               .HasMaxLength(200);

        builder.Property(x => x.IsDeleted)
               .HasDefaultValue(false);
    }
}