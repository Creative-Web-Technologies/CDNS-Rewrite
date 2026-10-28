using Cdns.Domain.Entities.MemberSignupTables;

namespace Cdns.Domain.Entities.MemberLoginTables;

public class MemberActivityLog
{
    public long MemberActivityLogId { get; set; }

    public string? Activity { get; set; }
    public string? ActivityJson { get; set; }
    public string? StoredProcedureName { get; set; }
    public DateTimeOffset? LogDateTime { get; set; }

    // NULL means "no member". Legacy stored 0, which the data migration converts to NULL.
    public long? MemberId { get; set; }
    public Member? Member { get; set; }
}