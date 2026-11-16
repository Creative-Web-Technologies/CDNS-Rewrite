using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecInspectStatusConfiguration
    : IEntityTypeConfiguration<CusDecInspectStatus>
{
    public void Configure(EntityTypeBuilder<CusDecInspectStatus> builder)
    {
        // Primary Key
        builder.HasKey(x => x.CusDecInspectStatusId);

        builder.Property(x => x.CusDecInspectStatusId)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(x => x.Status)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Description);

        builder.Property(x => x.TableName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastSyncDateTime)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Foreign Key: CusDecId -> CusDec
        builder.HasOne(x => x.CusDec)
            .WithMany()
            .HasForeignKey(x => x.CusDecId)
            .HasPrincipalKey(x => x.CusDecId)
            .OnDelete(DeleteBehavior.Restrict);

        // Natural key, matching SyncCusDecInspectStatus's own MERGE match condition
        builder.HasIndex(x => new { x.ExternalSystemNumber, x.CusDecExternalSystemNumber, x.TableName, x.Status })
            .IsUnique();

        builder.Property(x => x.ExternalSystemNumber).IsRequired();
        builder.Property(x => x.CusDecExternalSystemNumber).IsRequired();
    }
}