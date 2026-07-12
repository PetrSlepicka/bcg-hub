using System.ComponentModel.DataAnnotations;
using BcgHub.Api.Domain;

namespace BcgHub.Api.Application;

public sealed record ContactPersonDto(Guid Id, string FullName, string? Position, string? Email, string? Phone, bool IsPrimary, uint Version);
public sealed record PartnerDetailDto(Guid Id, PartnerType Type, string Name, string? CompanyNumber, string? VatNumber, string? Email, string? Phone, string? Website, string? Street, string? City, string? PostalCode, string? CountryCode, string? Notes, string? TransportCapabilities, IReadOnlyList<ContactPersonDto> Contacts, uint Version);

public sealed class SavePartnerRequest
{
    [EnumDataType(typeof(PartnerType))] public PartnerType Type { get; init; }
    [Required, StringLength(250)] public string Name { get; init; } = "";
    [StringLength(50)] public string? CompanyNumber { get; init; }
    [StringLength(50)] public string? VatNumber { get; init; }
    [EmailAddress, StringLength(320)] public string? Email { get; init; }
    [StringLength(100)] public string? Phone { get; init; }
    [Url, StringLength(500)] public string? Website { get; init; }
    [StringLength(300)] public string? Street { get; init; }
    [StringLength(200)] public string? City { get; init; }
    [StringLength(30)] public string? PostalCode { get; init; }
    [StringLength(2, MinimumLength = 2)] public string? CountryCode { get; init; }
    [StringLength(10000)] public string? Notes { get; init; }
    [StringLength(2000)] public string? TransportCapabilities { get; init; }
    public uint Version { get; init; }
}

public sealed class SaveContactPersonRequest
{
    [Required, StringLength(200)] public string FullName { get; init; } = "";
    [StringLength(200)] public string? Position { get; init; }
    [EmailAddress, StringLength(320)] public string? Email { get; init; }
    [StringLength(100)] public string? Phone { get; init; }
    public bool IsPrimary { get; init; }
    public uint Version { get; init; }
}
