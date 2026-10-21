using Cdns.Domain.Entities.MemberSignupTables;

namespace Cdns.Domain.Entities.CompanySubscriptionManagement;


public class CompanyMemberMapping
{
    public long CompanyMemberMappingId { get; set; }

    public int CompanyId { get; set; }

    public long MemberId { get; set; }

    public bool IsDeleted { get; set; }

    // Navigation properties
    public Company Company { get; set; } = null!;

    // Note: Ensure Member entity exists or use a stub until merged
    public Member Member { get; set; } = null!;
}