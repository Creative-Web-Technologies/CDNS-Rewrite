using Cdns.Domain.Entities.MemberSignupTables;
using Cdns.Domain.Enums;

namespace Cdns.Domain.Entities.MemberLoginTables;

public class MemberUser
{
    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string PasswordSalt { get; set; } = null!;

    public DateTimeOffset CreatedDateTime { get; set; }
    public int ActivationCode { get; set; }
    public Guid GlobalIdentity { get; set; }
    public DateTimeOffset LastOtpGeneratedDateTime { get; set; }
    public DateTimeOffset? SuspendedDateTime { get; set; }

    public UserStatusType UserStatusType { get; set; }
    public bool IsActiveOtp { get; set; }
    public MemberType MemberType { get; set; } = MemberType.Member;
    public bool RequiredEregVerification { get; set; }
    public bool IsWrongAttempt { get; set; }
    public DateTimeOffset? LastLoginAttemptDateTime { get; set; }
}