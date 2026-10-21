
using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecInspectionSectionTabConfiguration : IEntityTypeConfiguration<CusDecInspectionSectionTab>
{
    public void Configure(EntityTypeBuilder<CusDecInspectionSectionTab> builder)
    {
        builder.HasKey(x => x.CusDecInspectionSectionTabId);
        builder.Property(x => x.CusDecInspectionSectionTabId).ValueGeneratedOnAdd();

        builder.Property(x => x.SectionName).HasMaxLength(500);

        builder.Property(x => x.LastSyncDateTime)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(x => x.CusDec)
            .WithMany()
            .HasForeignKey(x => x.CusDecId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}