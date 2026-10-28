using Cdns.Domain.Entities.MemberSignupTables;

namespace Cdns.Domain.Entities.MemberLoginTables;

public class MemberLoginSession
{
    public long MemberLoginSessionId { get; set; }

    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public Guid SessionId { get; set; }
    public string UserName { get; set; } = null!;
    public string? Key { get; set; }
    public DateTimeOffset LoginDateTime { get; set; }
}