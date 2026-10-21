using Cdns.Domain.Entities.CusdecTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.Cusdec;

public class ReadyToProcessConsigneeConfiguration : IEntityTypeConfiguration<ReadyToProcessConsignee>
{
    public void Configure(EntityTypeBuilder<ReadyToProcessConsignee> builder)
    {
        builder.HasKey(x => x.ReadyToProcessConsigneeId);
        builder.Property(x => x.ReadyToProcessConsigneeId).ValueGeneratedOnAdd();

        builder.Property(x => x.ConsigneeTinNumber).HasMaxLength(200);

        builder.HasOne(x => x.CusDecReadyToProcessSetting)
            .WithMany()
            .HasForeignKey(x => x.CusDecReadyToProcessSettingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}