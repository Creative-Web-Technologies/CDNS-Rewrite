using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecItemConfiguration : IEntityTypeConfiguration<CusDecItem>
{
    public void Configure(EntityTypeBuilder<CusDecItem> builder)
    {
        builder.ToTable("CusDecItem");

        builder.HasKey(x => x.CusDecItemId);
        builder.Property(x => x.CusDecItemId).ValueGeneratedOnAdd();

        builder.Property(x => x.HsCode).HasMaxLength(50);
        builder.Property(x => x.Description); // NVARCHAR(MAX) -> text
        builder.Property(x => x.Quantity).HasMaxLength(50);
        builder.Property(x => x.BlNumber).HasMaxLength(100);

        builder.Property(x => x.UnitPrice).HasPrecision(18, 4);
        builder.Property(x => x.TotalValue).HasPrecision(18, 4);

        builder.Property(x => x.LastSyncDate)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(x => x.CusDec)
            .WithMany()
            .HasForeignKey(x => x.CusDecId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}