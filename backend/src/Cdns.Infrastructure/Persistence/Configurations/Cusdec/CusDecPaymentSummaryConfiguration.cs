
using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.CusdecTables;

public class CusDecPaymentSummaryConfiguration
    : IEntityTypeConfiguration<CusDecPaymentSummary>
{
    public void Configure(EntityTypeBuilder<CusDecPaymentSummary> builder)
    {
        builder.HasKey(x => x.CusDecPaymentSummaryId);

        builder.Property(x => x.CusDecPaymentSummaryId)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.DeclarantTinNumber)
            .HasMaxLength(200);

        builder.Property(x => x.ConsigneeTinNumber)
            .HasMaxLength(200);

        builder.Property(x => x.TotalCif)
            .HasPrecision(28, 2);

        builder.Property(x => x.TotalTaxPaid)
            .HasPrecision(28, 2);

        builder.Property(x => x.InspectionRecoveries)
            .HasPrecision(28, 2);

        builder.Property(x => x.LastSyncDateTime)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Declarant TIN foreign key
        builder.HasOne<CusDec>()
            .WithMany()
            .HasForeignKey(x => x.DeclarantTinNumber)
            .HasPrincipalKey(x => x.DeclarantTinNumber)
            .OnDelete(DeleteBehavior.Restrict);

        // Consignee TIN foreign key
        builder.HasOne<CusDec>()
            .WithMany()
            .HasForeignKey(x => x.ConsigneeTinNumber)
            .HasPrincipalKey(x => x.ConsigneeTinNumber)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
