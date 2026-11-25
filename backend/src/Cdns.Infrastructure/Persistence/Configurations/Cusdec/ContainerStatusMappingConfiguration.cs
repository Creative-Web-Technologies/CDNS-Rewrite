using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class ContainerStatusMappingConfiguration : IEntityTypeConfiguration<ContainerStatusMapping>
{
    public void Configure(EntityTypeBuilder<ContainerStatusMapping> builder)
    {
        builder.ToTable("container_status_mappings");

        builder.HasKey(x => x.ContainerStatusMappingId);
        builder.Property(x => x.ContainerStatusMappingId).ValueGeneratedOnAdd();

        builder.Property(x => x.AsycudaStatus).IsRequired().HasMaxLength(100);
        builder.Property(x => x.AsycudaDescription).IsRequired().HasMaxLength(500);
        builder.Property(x => x.CdnsStatus).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CdnsDescription).HasMaxLength(500);

        builder.Property(x => x.IsShow).IsRequired().HasDefaultValue(true);

        builder.Property(x => x.AsycudaStatusKey)
            .HasColumnName("asycuda_status_key")
            .HasComputedColumnSql("lower(btrim(asycuda_status))", stored: true);

        builder.Property(x => x.AsycudaDescriptionKey)
            .HasColumnName("asycuda_description_key")
            .HasComputedColumnSql("lower(btrim(asycuda_description))", stored: true);

        builder.HasIndex(x => new { x.AsycudaStatusKey, x.AsycudaDescriptionKey }).IsUnique();
    }
}
