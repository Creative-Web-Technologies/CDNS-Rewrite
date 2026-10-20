using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class CusDecInspectionConfiguration : IEntityTypeConfiguration<CusDecInspection>
{
    public void Configure(EntityTypeBuilder<CusDecInspection> builder)
    {
        builder.HasKey(x => x.CusDecInspectionId);
        builder.Property(x => x.CusDecInspectionId).ValueGeneratedOnAdd();

        builder.Property(x => x.IssueCode).HasMaxLength(50);
        builder.Property(x => x.Description); // NVARCHAR(MAX) -> text
        builder.Property(x => x.Value).HasPrecision(18, 2);

        builder.Property(x => x.LastSyncDateTime)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.FrdNfr).HasMaxLength(100);
        builder.Property(x => x.FrdUni).HasMaxLength(100);
        builder.Property(x => x.FrdUbr).HasMaxLength(100);

        builder.HasOne(x => x.CusDec)
            .WithMany()
            .HasForeignKey(x => x.CusDecId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}