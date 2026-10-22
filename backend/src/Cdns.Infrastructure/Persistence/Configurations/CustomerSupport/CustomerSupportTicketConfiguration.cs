using Cdns.Domain.Entities.CustomerSupportTickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cdns.Infrastructure.Persistence.Configurations.CustomerSupport;

public class CustomerSupportTicketConfiguration : IEntityTypeConfiguration<CustomerSupportTicket>
{
    public void Configure(EntityTypeBuilder<CustomerSupportTicket> builder)
    {
        // Primary Key (Identity)
        builder.HasKey(x => x.CustomerSupportTicketId);
        builder.Property(x => x.CustomerSupportTicketId)
               .ValueGeneratedOnAdd();

        // Columns & Constraints
        builder.Property(x => x.TicketNumber)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(x => x.ContactEmail)
               .HasMaxLength(500);

        builder.Property(x => x.ContactNumber)
               .HasMaxLength(20);

        builder.Property(x => x.CreatedDateTime)
               .HasDefaultValueSql("now()")
               .IsRequired();

        builder.Property(x => x.IsCompleted)
               .HasDefaultValue(false);

        // Foreign Key: Member (Restrict Delete)
        builder.HasOne(x => x.Member)
               .WithMany()
               .HasForeignKey(x => x.MemberId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}