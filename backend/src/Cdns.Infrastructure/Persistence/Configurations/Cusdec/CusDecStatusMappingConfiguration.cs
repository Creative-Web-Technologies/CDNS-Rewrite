using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecStatusMappingConfiguration : IEntityTypeConfiguration<CusDecStatusMapping>
{
    public void Configure(EntityTypeBuilder<CusDecStatusMapping> builder)
    {
        builder.ToTable("CusDecStatusMapping");

        builder.HasKey(x => x.CusDecStatusMappingId);
        builder.Property(x => x.CusDecStatusMappingId).ValueGeneratedOnAdd();

        builder.Property(x => x.AsycudaStatus).HasMaxLength(500);
        builder.Property(x => x.AsycudaDescription).HasColumnType("text");
        builder.Property(x => x.CdnsStatus).HasMaxLength(500);
        builder.Property(x => x.CdnsDescription).HasColumnType("text");

        builder.Property(x => x.IsShow).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.OfficeCode).HasMaxLength(20);
    }
}