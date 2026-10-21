namespace Cdns.Domain.Entities.CusdecTables;

public class CusDecReadyToProcessSetting
{
    public int CusDecReadyToProcessSettingId { get; set; }
    public string? Title { get; set; }
    public int? OfficeCodeId { get; set; }   // FK to Office added once that table exists
    public int NoOfItems { get; set; }
    public bool AllItems { get; set; }
    public bool AllConsignees { get; set; }
    public long CreatedById { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public bool IsDeleted { get; set; }
}