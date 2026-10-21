namespace Cdns.Domain.Entities.CusdecTables;

public class ReadyToProcessConsignee
{
    public int ReadyToProcessConsigneeId { get; set; }
    public int? CusDecReadyToProcessSettingId { get; set; }
    public string? ConsigneeTinNumber { get; set; }

    public CusDecReadyToProcessSetting? CusDecReadyToProcessSetting { get; set; }
}