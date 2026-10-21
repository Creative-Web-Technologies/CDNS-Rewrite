using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecInspectionSectionTabStatusMappingConfiguration
    : IEntityTypeConfiguration<CusDecInspectionSectionTabStatusMapping>
{
    public void Configure(EntityTypeBuilder<CusDecInspectionSectionTabStatusMapping> builder)
    {
        builder.HasKey(x => x.CusDecInspectionSectionTabStatusMappingId);
        builder.Property(x => x.CusDecInspectionSectionTabStatusMappingId).ValueGeneratedOnAdd();

        builder.Property(x => x.AsycudaSectionName).HasMaxLength(500);
        builder.Property(x => x.CdnsSectionName).HasMaxLength(500);

        builder.Property(x => x.IsShow)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.OfficeCode).HasMaxLength(20);
    }
}
