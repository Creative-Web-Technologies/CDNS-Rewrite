using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecTaxConfiguration : IEntityTypeConfiguration<CusDecTax>
{
    public void Configure(EntityTypeBuilder<CusDecTax> builder)
    {
        builder.HasKey(x => x.CusDecTaxId);
        builder.Property(x => x.CusDecTaxId).ValueGeneratedOnAdd();

        builder.Property(x => x.TaxCode).HasMaxLength(50);

        builder.Property(x => x.TaxAmount)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(x => x.LastSyncDate)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(x => x.CusDec)
            .WithMany()
            .HasForeignKey(x => x.CusDecId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}