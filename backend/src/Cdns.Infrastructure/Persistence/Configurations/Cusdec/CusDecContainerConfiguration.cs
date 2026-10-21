using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecContainerConfiguration : IEntityTypeConfiguration<CusDecContainer>
{
    public void Configure(EntityTypeBuilder<CusDecContainer> builder)
    {
        builder.ToTable("CusDecContainer");

        builder.HasKey(x => x.CusDecContainerId);
        builder.Property(x => x.CusDecContainerId).ValueGeneratedOnAdd();

        builder.Property(x => x.Container).HasMaxLength(100);
        builder.Property(x => x.Location).HasMaxLength(100);
        builder.Property(x => x.CurrentStatus).HasMaxLength(100);
        builder.Property(x => x.Size).HasMaxLength(50);
        builder.Property(x => x.Type).HasMaxLength(50);
        builder.Property(x => x.Voyage).HasMaxLength(50);
        builder.Property(x => x.Vessel).HasMaxLength(50);

        builder.Property(x => x.LastSyncDate)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.ContainerPassNumber)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne(x => x.CusDec)
            .WithMany()
            .HasForeignKey(x => x.CusDecId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}