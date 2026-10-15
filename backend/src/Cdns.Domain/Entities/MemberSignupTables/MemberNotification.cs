using Cdns.Domain.Enums;

namespace Cdns.Domain.Entities.MemberSignupTables;

public class MemberNotification
{
    public long MemberNotificationId { get; set; }

    public long MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public string? Title { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset NotificationDateTime { get; set; }

    // Logical reference only; the FK is added once the CusDec entity exists
    public long? CusDecId { get; set; }

    public bool IsSeen { get; set; }
    public DateTimeOffset? SeenDateTime { get; set; }
    public bool ShowOnHome { get; set; }
    public bool IsDeleted { get; set; }

    public NotificationType NotificationType { get; set; }
}