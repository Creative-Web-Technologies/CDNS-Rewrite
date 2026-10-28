using Cdns.Domain.Entities.SystemInfrastructureAuditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.SystemInfrastructureAuditing;

public class SmtpMailSettingConfiguration : IEntityTypeConfiguration<SmtpMailSetting>
{
    public void Configure(EntityTypeBuilder<SmtpMailSetting> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.SmtpMailSettingId);
        builder.Property(x => x.SmtpMailSettingId)
               .ValueGeneratedOnAdd();

        // Columns & Constraints
        builder.Property(x => x.SmtpMailCode)
               .HasMaxLength(10)
               .IsRequired();

        builder.Property(x => x.FromName)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(x => x.FromAddress)
               .HasMaxLength(500)
               .IsRequired();

        builder.Property(x => x.SmtpUsername)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(x => x.SmtpPassword)
               .IsRequired();

        builder.Property(x => x.SmtpUri)
               .IsRequired();
    }
}