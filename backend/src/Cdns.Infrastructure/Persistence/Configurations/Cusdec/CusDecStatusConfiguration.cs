using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecStatusConfiguration
    : IEntityTypeConfiguration<CusDecStatus>
{
    public void Configure(EntityTypeBuilder<CusDecStatus> builder)
    {

        // Primary Key
        builder.HasKey(x => x.CusDecStatusId);

        builder.Property(x => x.CusDecStatusId)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(x => x.Status)
            .HasMaxLength(500);

        builder.Property(x => x.Description);

        builder.Property(x => x.TableName)
            .HasMaxLength(100);

        builder.Property(x => x.LastSyncDateTime)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Foreign Key: CusDecId -> CusDec
        builder.HasOne(x => x.CusDec)
            .WithMany()
            .HasForeignKey(x => x.CusDecId)
            .HasPrincipalKey(x => x.CusDecId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
