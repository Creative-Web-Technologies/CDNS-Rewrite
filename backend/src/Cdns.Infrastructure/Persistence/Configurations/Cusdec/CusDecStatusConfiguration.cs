using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecStatusConfiguration : IEntityTypeConfiguration<CusDecStatus>
{
    public void Configure(EntityTypeBuilder<CusDecStatus> builder)
    {
        builder.ToTable("CusDecStatus");

        builder.HasKey(x => x.CusDecStatusId);
        builder.Property(x => x.CusDecStatusId).ValueGeneratedOnAdd();

        builder.Property(x => x.Status).HasMaxLength(500);
        builder.Property(x => x.Description); // NVARCHAR(MAX) -> text
        builder.Property(x => x.TableName).HasMaxLength(100);

        builder.Property(x => x.LastSyncDateTime)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(x => x.CusDec)
            .WithMany()
            .HasForeignKey(x => x.CusDecId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}