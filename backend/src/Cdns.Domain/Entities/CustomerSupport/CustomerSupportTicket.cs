using Cdns.Domain.Entities.MemberSignupTables;

namespace Cdns.Domain.Entities.CustomerSupportTickets;

public class CustomerSupportTicket
{
    public long CustomerSupportTicketId { get; set; }

    public long MemberId { get; set; }

    public string TicketNumber { get; set; } = null!;

    public string? ContactEmail { get; set; }

    public string? ContactNumber { get; set; }

    public string? Message { get; set; }

    public DateTimeOffset CreatedDateTime { get; set; }

    public bool IsCompleted { get; set; }

    // Navigation property
    public Member Member { get; set; } = null!;
}