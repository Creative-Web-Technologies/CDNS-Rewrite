namespace Cdns.Domain.Entities.CusdecTables;

public class CusDec
{
    public long CusDecId { get; set; }
    public string? CusDecNumber { get; set; }
    public string? DeclarantTinNumber { get; set; }
    public string? ConsigneeTinNumber { get; set; }

        // FKs to Company (nullable, Restrict) are configured in CusDecConfiguration
    public int? DeclarantCompanyId { get; set; }
    public int? ConsigneeCompanyId { get; set; }

    public decimal? InvoiceValue { get; set; }
    public int? NoOfItems { get; set; }
    public string? BlNumber { get; set; }
    public Guid GlobalIdentity { get; set; }
    public long CusDecExternalSystemNumber { get; set; }
    public DateTime LastSyncDateTime { get; set; }

    public long? DeclarantClaimedById { get; set; }
    public long? ConsigneeClaimedById { get; set; }
    public long? DeclarantDisclaimedById { get; set; }
    public long? ConsigneeDisclaimedById { get; set; }
    public DateTime? DeclarantClaimedDateTime { get; set; }
    public DateTime? ConsigneeClaimedDateTime { get; set; }
    public DateTime? DeclarantDisclaimedDateTime { get; set; }
    public DateTime? ConsigneeDisclaimedDateTime { get; set; }

    public bool IsReadyToProcess { get; set; }
    public long? ReadyToProcessMarkById { get; set; }
    public DateTime? ReadyToProcessMarkDateTime { get; set; }

    public DateTime? CusDecDate { get; set; }
    public string? Origin { get; set; }
    public string? OfficeCode { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string? Exporter { get; set; }
    public string? Incoterm { get; set; }
    public string? Destination { get; set; }
    public string? Channel { get; set; }
    public int NoOfContainers { get; set; }
    public string? CusDecType { get; set; }

    public bool CanNotProcess { get; set; }
    public long? CanNotProcessById { get; set; }
    public DateTime? CanNotProcessMarkDateTime { get; set; }
    public string? CanNotProcessReason { get; set; }

    public bool CanProcess { get; set; }
    public long? CanProcessById { get; set; }
    public DateTime? CanProcessMarkDateTime { get; set; }

    public string? DeclarantCompanyName { get; set; }
    public string? ConsigneeCompanyName { get; set; }
    public string? SupplierCompanyName { get; set; }
}