namespace Cdns.Domain.Entities.SystemInfrastructureAuditing;

public class SystemSerial
{
    public int SystemSerialId { get; set; }

    public long? AutoIncrementSerial { get; set; }

    public long? DailyIncrementSerial { get; set; }

    public long? MonthlyIncrementSerial { get; set; }

    public long? YearlyIncrementSerial { get; set; }

    public string? SerialFormatExpression { get; set; }

    public string? SerialName { get; set; }

    public int? DocumentTypeEnum { get; set; }

    public DateTimeOffset? GeneratedDateTime { get; set; }

    public bool? IsDeleted { get; set; }

    public bool? IsDefaultSerial { get; set; }
}