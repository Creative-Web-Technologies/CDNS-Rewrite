using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecInspectionMappingConfiguration : IEntityTypeConfiguration<CusDecInspectionMapping>
{
    public void Configure(EntityTypeBuilder<CusDecInspectionMapping> builder)
    {
        builder.HasKey(x => x.CusDecInspectionMappingId);
        builder.Property(x => x.CusDecInspectionMappingId).ValueGeneratedOnAdd();

        builder.Property(x => x.AsycudaFrdNfr).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.Property(x => x.IsShow)
            .IsRequired()
            .HasDefaultValue(true);
    }
}