using Cdns.Domain.Entities.MemberSignupTables;

namespace Cdns.Domain.Entities.MemberLoginTables;

public class MemberUserLoginDetail
{
    public long MemberUserLoginDetailId { get; set; }

    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public DateTimeOffset LoginDateTime { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}