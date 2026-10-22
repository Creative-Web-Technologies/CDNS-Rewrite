namespace Cdns.Domain.Entities.SystemInfrastructureAuditing;

public class SmtpMailSetting
{
    public int SmtpMailSettingId { get; set; }

    public string SmtpMailCode { get; set; } = null!;

    public string FromName { get; set; } = null!;

    public string FromAddress { get; set; } = null!;

    public string SmtpUsername { get; set; } = null!;

    public string SmtpPassword { get; set; } = null!;

    public string SmtpUri { get; set; } = null!;

    public int? SmtpPort { get; set; }
}