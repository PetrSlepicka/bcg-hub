namespace BcgHub.Api.Domain;

public enum PartnerType
{
    Customer,
    Lead,
    Supplier,
    Warehouse,
    Carrier,
    CustomsDeclarant,
    Collaborator
}

public sealed class BusinessPartner : Entity
{
    public PartnerType Type { get; set; }
    public string Name { get; set; } = "";
    public string? CompanyNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public string? Notes { get; set; }
    public string? TransportCapabilities { get; set; }
    public ICollection<ContactPerson> Contacts { get; set; } = [];
}

public sealed class ContactPerson : Entity
{
    public Guid BusinessPartnerId { get; set; }
    public BusinessPartner BusinessPartner { get; set; } = null!;
    public string FullName { get; set; } = "";
    public string? Position { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsPrimary { get; set; }
}
