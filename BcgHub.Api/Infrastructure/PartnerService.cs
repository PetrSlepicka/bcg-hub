using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class PartnerService(BcgHubDbContext db) : IPartnerService
{
    public async Task<PagedResult<PartnerListItem>> GetListAsync(PartnerType? type, string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.BusinessPartners.AsNoTracking();
        if (type is not null) query = query.Where(x => x.Type == type);
        if (!string.IsNullOrWhiteSpace(search)) { var pattern = $"%{EscapeLike(search.Trim())}%"; query = query.Where(x => EF.Functions.ILike(x.Name, pattern, "\\") || x.Email != null && EF.Functions.ILike(x.Email, pattern, "\\") || x.CompanyNumber != null && EF.Functions.ILike(x.CompanyNumber, pattern, "\\") || x.Contacts.Any(contact => EF.Functions.ILike(contact.FullName, pattern, "\\") || contact.Email != null && EF.Functions.ILike(contact.Email, pattern, "\\"))); }
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new PartnerListItem(x.Id, x.Type, x.Name, x.City, x.CountryCode, x.Email, x.Phone, x.Contacts.Count)).ToListAsync(cancellationToken);
        return new PagedResult<PartnerListItem>(items, page, pageSize, totalCount);
    }

    public async Task<PartnerDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken) => Map(await db.BusinessPartners.AsNoTracking().Include(x => x.Contacts).SingleOrDefaultAsync(x => x.Id == id, cancellationToken));

    public async Task<PartnerDetailDto> CreateAsync(SavePartnerRequest request, CancellationToken cancellationToken)
    {
        var partner = new BusinessPartner();
        Apply(partner, request);
        PartnerPrimaryContactSynchronizer.Synchronize(partner);
        db.BusinessPartners.Add(partner);
        await db.SaveChangesAsync(cancellationToken);
        return Map(partner)!;
    }

    public async Task<PartnerDetailDto?> UpdateAsync(Guid id, SavePartnerRequest request, CancellationToken cancellationToken)
    {
        var partner = await db.BusinessPartners.Include(x => x.Contacts).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (partner is null) return null;
        db.Entry(partner).Property(x => x.Version).OriginalValue = request.Version;
        Apply(partner, request);
        PartnerPrimaryContactSynchronizer.Synchronize(partner);
        await SaveAsync(cancellationToken);
        return Map(partner);
    }

    public async Task<bool> DeleteAsync(Guid id, uint version, CancellationToken cancellationToken)
    {
        var partner = await db.BusinessPartners.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (partner is null) return false;
        var isUsed = await db.Orders.AnyAsync(x => x.CustomerId == id || x.WarehouseId == id || x.CarrierId == id || x.CustomsDeclarantId == id, cancellationToken) || await db.TransportQuotes.AnyAsync(x => x.CarrierId == id, cancellationToken);
        if (isUsed) throw new DomainValidationException("Partner je použitý v zakázce nebo nabídce dopravy a nelze jej odstranit.");
        db.Entry(partner).Property(x => x.Version).OriginalValue = version;
        db.BusinessPartners.Remove(partner);
        await SaveAsync(cancellationToken);
        return true;
    }

    public async Task<ContactPersonDto?> AddContactAsync(Guid partnerId, SaveContactPersonRequest request, CancellationToken cancellationToken)
    {
        if (!await db.BusinessPartners.AnyAsync(x => x.Id == partnerId, cancellationToken)) return null;
        if (request.IsPrimary) await ClearPrimaryContactAsync(partnerId, null, cancellationToken);
        var contact = new ContactPerson { BusinessPartnerId = partnerId };
        Apply(contact, request);
        db.ContactPeople.Add(contact);
        await db.SaveChangesAsync(cancellationToken);
        return Map(contact);
    }

    public async Task<ContactPersonDto?> UpdateContactAsync(Guid partnerId, Guid contactId, SaveContactPersonRequest request, CancellationToken cancellationToken)
    {
        var contact = await db.ContactPeople.SingleOrDefaultAsync(x => x.Id == contactId && x.BusinessPartnerId == partnerId, cancellationToken);
        if (contact is null) return null;
        db.Entry(contact).Property(x => x.Version).OriginalValue = request.Version;
        if (request.IsPrimary) await ClearPrimaryContactAsync(partnerId, contactId, cancellationToken);
        Apply(contact, request);
        await SaveAsync(cancellationToken);
        return Map(contact);
    }

    public async Task<bool> DeleteContactAsync(Guid partnerId, Guid contactId, uint version, CancellationToken cancellationToken)
    {
        var contact = await db.ContactPeople.SingleOrDefaultAsync(x => x.Id == contactId && x.BusinessPartnerId == partnerId, cancellationToken);
        if (contact is null) return false;
        if (await db.Orders.AnyAsync(x => x.CustomerContactId == contactId, cancellationToken)) throw new DomainValidationException("Kontaktní osoba je použitá v zakázce a nelze ji odstranit.");
        db.Entry(contact).Property(x => x.Version).OriginalValue = version;
        db.ContactPeople.Remove(contact);
        await SaveAsync(cancellationToken);
        return true;
    }

    private async Task ClearPrimaryContactAsync(Guid partnerId, Guid? exceptId, CancellationToken cancellationToken) => await db.ContactPeople.Where(x => x.BusinessPartnerId == partnerId && x.IsPrimary && x.Id != exceptId).ExecuteUpdateAsync(update => update.SetProperty(x => x.IsPrimary, false).SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);
    private async Task SaveAsync(CancellationToken cancellationToken) { try { await db.SaveChangesAsync(cancellationToken); } catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("Partner nebo kontakt mezitím změnil jiný uživatel."); } }
    private static void Apply(BusinessPartner partner, SavePartnerRequest request) { partner.Type = request.Type; partner.Name = Required(request.Name, "Název partnera"); partner.CompanyNumber = Clean(request.CompanyNumber); partner.VatNumber = Clean(request.VatNumber); partner.Email = Clean(request.Email); partner.Phone = Clean(request.Phone); partner.Website = Clean(request.Website); partner.Street = Clean(request.Street); partner.City = Clean(request.City); partner.PostalCode = Clean(request.PostalCode); partner.CountryCode = Clean(request.CountryCode)?.ToUpperInvariant(); partner.Notes = Clean(request.Notes); partner.TransportCapabilities = Clean(request.TransportCapabilities); }
    private static void Apply(ContactPerson contact, SaveContactPersonRequest request) { contact.FullName = Required(request.FullName, "Jméno kontaktu"); contact.Position = Clean(request.Position); contact.Email = Clean(request.Email); contact.Phone = Clean(request.Phone); contact.IsPrimary = request.IsPrimary; }
    private static PartnerDetailDto? Map(BusinessPartner? partner) => partner is null ? null : new PartnerDetailDto(partner.Id, partner.Type, partner.Name, partner.CompanyNumber, partner.VatNumber, partner.Email, partner.Phone, partner.Website, partner.Street, partner.City, partner.PostalCode, partner.CountryCode, partner.Notes, partner.TransportCapabilities, partner.Contacts.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.FullName).Select(Map).ToList(), partner.Version);
    private static ContactPersonDto Map(ContactPerson contact) => new(contact.Id, contact.FullName, contact.Position, contact.Email, contact.Phone, contact.IsPrimary, contact.Version);
    private static string Required(string value, string label) => string.IsNullOrWhiteSpace(value) ? throw new DomainValidationException($"{label} je povinný.") : value.Trim();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string EscapeLike(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
