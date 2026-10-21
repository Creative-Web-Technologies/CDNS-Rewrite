
using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecReadyToProcessSettingConfiguration : IEntityTypeConfiguration<CusDecReadyToProcessSetting>
{
    public void Configure(EntityTypeBuilder<CusDecReadyToProcessSetting> builder)
    {
        builder.HasKey(x => x.CusDecReadyToProcessSettingId);
        builder.Property(x => x.CusDecReadyToProcessSettingId).ValueGeneratedOnAdd();

        builder.Property(x => x.Title).HasMaxLength(500);

        builder.Property(x => x.NoOfItems).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.AllItems).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.AllConsignees).IsRequired().HasDefaultValue(false);

        builder.Property(x => x.CreatedById).IsRequired();
        builder.Property(x => x.CreatedDateTime)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
    }
}