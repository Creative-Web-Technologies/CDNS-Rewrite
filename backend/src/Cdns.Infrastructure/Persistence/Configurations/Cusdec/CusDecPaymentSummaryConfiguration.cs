
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Cdns.Domain.Entities.CusdecTables;

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

        // Natural key: each (Declarant, Consignee) TIN pair represents one aggregate row.
        // The legacy MERGE procs upsert on this exact pair; no FK to CusDec is correct —
        // PaymentSummary is a cross-declaration aggregate, not tied to any single CusDec row.
        builder.HasIndex(x => new { x.DeclarantTinNumber, x.ConsigneeTinNumber })
            .IsUnique();
    }
}
