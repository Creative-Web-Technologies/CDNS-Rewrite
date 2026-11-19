namespace Cdns.Application.DTOs.Sync;

public class CusDecSyncRow
{
    public string? CusDecNumber { get; set; }
    public string? DeclarantTinNumber { get; set; }
    public string? ConsigneeTinNumber { get; set; }
    public decimal? InvoiceValue { get; set; }
    public decimal? NoOfItems { get; set; } // legacy sends decimal; CusDec entity stores int
    public string? BlNumber { get; set; }
    public Guid GlobalIdentity { get; set; }
    public long CusDecExternalSystemNumber { get; set; }
    public DateTime? CusDecDate { get; set; }
    public string? Origin { get; set; }
    public string? OfficeCode { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string? Exporter { get; set; }
    public string? Incoterm { get; set; }
    public string? Destination { get; set; }
    public string? Channel { get; set; }
    public int? NoOfContainers { get; set; }
    public string? CusDecType { get; set; }
    public string? DeclarantCompanyName { get; set; }
    public string? ConsigneeCompanyName { get; set; }
    public string? SupplierCompanyName { get; set; }
}