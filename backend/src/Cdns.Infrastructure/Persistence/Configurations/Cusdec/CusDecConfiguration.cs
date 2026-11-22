using Cdns.Domain.Entities.CompanySubscriptionManagement;
using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecConfiguration : IEntityTypeConfiguration<CusDec>
{
    public void Configure(EntityTypeBuilder<CusDec> builder)
    {

        builder.HasKey(x => x.CusDecId);
        builder.Property(x => x.CusDecId).ValueGeneratedOnAdd();

        builder.Property(x => x.CusDecNumber).HasMaxLength(200);
        builder.Property(x => x.DeclarantTinNumber).HasMaxLength(200);
        builder.Property(x => x.ConsigneeTinNumber).HasMaxLength(200);

        builder.Property(x => x.InvoiceValue).HasPrecision(18, 2);
        builder.Property(x => x.BlNumber).HasMaxLength(200);

        builder.Property(x => x.GlobalIdentity).IsRequired();
        builder.Property(x => x.CusDecExternalSystemNumber).IsRequired();

        builder.Property(x => x.LastSyncDateTime)
            .IsRequired()
            .HasDefaultValueSql("now()");

        builder.Property(x => x.IsReadyToProcess).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CanNotProcess).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CanProcess).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.NoOfContainers).IsRequired().HasDefaultValue(0);

        builder.Property(x => x.Origin).HasMaxLength(100);
        builder.Property(x => x.OfficeCode).HasMaxLength(20);
        builder.Property(x => x.Exporter).HasMaxLength(500);
        builder.Property(x => x.Incoterm).HasMaxLength(100);
        builder.Property(x => x.Destination).HasMaxLength(200);
        builder.Property(x => x.Channel).HasMaxLength(50);
        builder.Property(x => x.CusDecType).HasMaxLength(50);

        builder.Property(x => x.CanNotProcessReason).HasColumnType("text");

        builder.Property(x => x.DeclarantCompanyName).HasMaxLength(500);
        builder.Property(x => x.ConsigneeCompanyName).HasMaxLength(500);
        builder.Property(x => x.SupplierCompanyName).HasMaxLength(500);

        // ---- Foreign keys to Company ----
        // Nullable: the ASYCUDA sync inserts the CusDec first and the company link
        // is backfilled afterwards. Restrict: deleting a Company must never erase
        // or detach declaration history.
        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.DeclarantCompanyId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.ConsigneeCompanyId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // CusDec page tabs + master auto-claim: company -> not/discarded -> not/claimed -> newest first.
        // Leading column also serves "by company" lookups and the Company FK.
        builder.HasIndex(x => new { x.DeclarantCompanyId, x.DeclarantDisclaimedById, x.DeclarantClaimedById, x.CusDecDate })
            .IsDescending(false, false, false, true)
            .HasDatabaseName("IX_CusDec_Declarant_Tabs");
        builder.HasIndex(x => new { x.ConsigneeCompanyId, x.ConsigneeDisclaimedById, x.ConsigneeClaimedById, x.CusDecDate })
            .IsDescending(false, false, false, true)
            .HasDatabaseName("IX_CusDec_Consignee_Tabs");

        builder.HasIndex(x => x.CusDecExternalSystemNumber).IsUnique();

        // The *ById columns (claim, disclaim, ready-to-process, can/can't process) are
        // plain bigint in legacy with no FK, so they stay that way for now.
    }
}