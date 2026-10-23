using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecContainerStatusConfiguration : IEntityTypeConfiguration<CusDecContainerStatus>
{
    public void Configure(EntityTypeBuilder<CusDecContainerStatus> builder)
    {

        builder.HasKey(x => x.CusDecContainerStatusId);
        builder.Property(x => x.CusDecContainerStatusId).ValueGeneratedOnAdd();

        builder.Property(x => x.Status).HasMaxLength(200);
        builder.Property(x => x.Description); // NVARCHAR(MAX) -> text in Postgres
        builder.Property(x => x.ContainerNumber).HasMaxLength(100);

        builder.Property(x => x.LastSyncDate)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(x => x.CusDecContainer)
            .WithMany()
            .HasForeignKey(x => x.CusDecContainerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}