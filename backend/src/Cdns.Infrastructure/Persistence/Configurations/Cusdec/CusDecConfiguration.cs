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

        // ── Partial indexes for the four CusDec page tabs ────────────────────────
        // Each partial index is company-id leading (also serves the Company FK scan),
        // then the tab-specific sort column DESC, with a WHERE clause that exactly
        // matches the tab's column-based predicate. Tenuka generates the migration.

        // Declarant — New (unclaimed, undisclaimed)
        builder.HasIndex(x => new { x.DeclarantCompanyId, x.CusDecDate }, "IX_CusDec_Declarant_New")
            .IsDescending(false, true)
            .HasFilter("declarant_claimed_by_id IS NULL AND declarant_disclaimed_by_id IS NULL");

        // Declarant — In progress (claimed, not disclaimed, not completed)
        builder.HasIndex(x => new { x.DeclarantCompanyId, x.CusDecDate }, "IX_CusDec_Declarant_InProgress")
            .IsDescending(false, true)
            .HasFilter("declarant_claimed_by_id IS NOT NULL AND declarant_disclaimed_by_id IS NULL AND declarant_completed_date_time IS NULL");

        // Declarant — Completed (claimed, not disclaimed, completed set)
        builder.HasIndex(x => new { x.DeclarantCompanyId, x.DeclarantCompletedDateTime }, "IX_CusDec_Declarant_Completed")
            .IsDescending(false, true)
            .HasFilter("declarant_claimed_by_id IS NOT NULL AND declarant_disclaimed_by_id IS NULL AND declarant_completed_date_time IS NOT NULL");

        // Declarant — Disclaimed
        builder.HasIndex(x => new { x.DeclarantCompanyId, x.DeclarantDisclaimedDateTime }, "IX_CusDec_Declarant_Disclaimed")
            .IsDescending(false, true)
            .HasFilter("declarant_disclaimed_by_id IS NOT NULL");

        // Consignee — New
        builder.HasIndex(x => new { x.ConsigneeCompanyId, x.CusDecDate }, "IX_CusDec_Consignee_New")
            .IsDescending(false, true)
            .HasFilter("consignee_claimed_by_id IS NULL AND consignee_disclaimed_by_id IS NULL");

        // Consignee — In progress
        builder.HasIndex(x => new { x.ConsigneeCompanyId, x.CusDecDate }, "IX_CusDec_Consignee_InProgress")
            .IsDescending(false, true)
            .HasFilter("consignee_claimed_by_id IS NOT NULL AND consignee_disclaimed_by_id IS NULL AND consignee_completed_date_time IS NULL");

        // Consignee — Completed
        builder.HasIndex(x => new { x.ConsigneeCompanyId, x.ConsigneeCompletedDateTime }, "IX_CusDec_Consignee_Completed")
            .IsDescending(false, true)
            .HasFilter("consignee_claimed_by_id IS NOT NULL AND consignee_disclaimed_by_id IS NULL AND consignee_completed_date_time IS NOT NULL");

        // Consignee — Disclaimed
        builder.HasIndex(x => new { x.ConsigneeCompanyId, x.ConsigneeDisclaimedDateTime }, "IX_CusDec_Consignee_Disclaimed")
            .IsDescending(false, true)
            .HasFilter("consignee_disclaimed_by_id IS NOT NULL");

        builder.HasIndex(x => x.CusDecExternalSystemNumber).IsUnique();

        // The *ById columns (claim, disclaim, complete, ready-to-process, can/can't process)
        // are plain bigint in legacy with no FK, so they stay that way for now.
    }
}