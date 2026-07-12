using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class CommunicationService(BcgHubDbContext db, CurrentUserAccessor currentUser) : ICommunicationService
{
    public async Task<PagedResult<CommunicationDto>> GetListAsync(Guid? partnerId, Guid? orderId, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!partnerId.HasValue && !orderId.HasValue) throw new DomainValidationException("Je nutné vybrat partnera nebo zakázku.");
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var resolvedPartnerId = partnerId ?? await db.Orders.AsNoTracking().Where(x => x.Id == orderId).Select(x => (Guid?)x.CustomerId).SingleOrDefaultAsync(cancellationToken);
        var addresses = resolvedPartnerId.HasValue ? await GetPartnerAddressesAsync(resolvedPartnerId.Value, cancellationToken) : [];
        var communications = await db.Communications.AsNoTracking().Where(x => (!partnerId.HasValue || x.BusinessPartnerId == partnerId) && (!orderId.HasValue || x.OrderId == orderId)).Select(x => new CommunicationDto(x.Id, x.Type, x.BusinessPartnerId, x.OrderId, x.Subject, x.BodyPreview, x.Sender, x.Recipients, x.OccurredAtUtc, x.Version)).ToListAsync(cancellationToken);
        var emailQuery = db.EmailMessages.AsNoTracking().Where(x => x.UserAccountId == currentUser.UserId && ((!partnerId.HasValue || x.BusinessPartnerId == partnerId) && (!orderId.HasValue || x.OrderId == orderId)));
        foreach (var address in addresses) emailQuery = emailQuery.Union(db.EmailMessages.AsNoTracking().Where(x => x.UserAccountId == currentUser.UserId && (x.FromAddress.ToLower() == address || x.ToAddress.ToLower().Contains(address) || (x.CcAddress != null && x.CcAddress.ToLower().Contains(address)))));
        var emailRows = await emailQuery.ToListAsync(cancellationToken);
        var emails = emailRows.Where(x => IsExplicitlyLinked(x, partnerId, orderId) || addresses.Contains(x.FromAddress.Trim().ToLowerInvariant()) || RecipientContains(x.ToAddress, addresses) || RecipientContains(x.CcAddress, addresses)).Select(x => new CommunicationDto(x.Id, CommunicationType.Email, x.BusinessPartnerId, x.OrderId, x.Subject, Preview(x.BodyText), x.FromAddress, x.ToAddress, x.OccurredAtUtc, x.Version)).ToList();
        var allItems = communications.Concat(emails).OrderByDescending(x => x.OccurredAtUtc).ThenBy(x => x.Id).ToList();
        return new PagedResult<CommunicationDto>(allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, allItems.Count);
    }

    public async Task<CommunicationDto> CreateAsync(SaveCommunicationRequest request, CancellationToken cancellationToken) { await ValidateAsync(request, cancellationToken); var communication = new Communication(); Apply(communication, request); db.Communications.Add(communication); await db.SaveChangesAsync(cancellationToken); return Map(communication); }
    public async Task<CommunicationDto?> UpdateAsync(Guid id, SaveCommunicationRequest request, CancellationToken cancellationToken) { var communication = await db.Communications.SingleOrDefaultAsync(x => x.Id == id, cancellationToken); if (communication is null) return null; await ValidateAsync(request, cancellationToken); db.Entry(communication).Property(x => x.Version).OriginalValue = request.Version; Apply(communication, request); await SaveAsync(cancellationToken); return Map(communication); }
    public async Task<bool> DeleteAsync(Guid id, uint version, CancellationToken cancellationToken) { var communication = await db.Communications.SingleOrDefaultAsync(x => x.Id == id, cancellationToken); if (communication is null) return false; db.Entry(communication).Property(x => x.Version).OriginalValue = version; db.Communications.Remove(communication); await SaveAsync(cancellationToken); return true; }
    private async Task ValidateAsync(SaveCommunicationRequest request, CancellationToken cancellationToken) { if (!request.BusinessPartnerId.HasValue && !request.OrderId.HasValue) throw new DomainValidationException("Komunikace musí patřit partnerovi nebo zakázce."); if (request.BusinessPartnerId.HasValue && !await db.BusinessPartners.AnyAsync(x => x.Id == request.BusinessPartnerId, cancellationToken)) throw new DomainValidationException("Partner neexistuje."); if (request.OrderId.HasValue && !await db.Orders.AnyAsync(x => x.Id == request.OrderId, cancellationToken)) throw new DomainValidationException("Zakázka neexistuje."); }
    private async Task SaveAsync(CancellationToken cancellationToken) { try { await db.SaveChangesAsync(cancellationToken); } catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("Komunikaci mezitím změnil jiný uživatel."); } }
    private static void Apply(Communication communication, SaveCommunicationRequest request) { communication.Type = request.Type; communication.BusinessPartnerId = request.BusinessPartnerId; communication.OrderId = request.OrderId; communication.Subject = string.IsNullOrWhiteSpace(request.Subject) ? throw new DomainValidationException("Předmět je povinný.") : request.Subject.Trim(); communication.BodyPreview = Clean(request.BodyPreview); communication.Sender = Clean(request.Sender); communication.Recipients = Clean(request.Recipients); communication.OccurredAtUtc = request.OccurredAtUtc == default ? DateTime.UtcNow : request.OccurredAtUtc; }
    private static CommunicationDto Map(Communication x) => new(x.Id, x.Type, x.BusinessPartnerId, x.OrderId, x.Subject, x.BodyPreview, x.Sender, x.Recipients, x.OccurredAtUtc, x.Version);
    private async Task<List<string>> GetPartnerAddressesAsync(Guid partnerId, CancellationToken cancellationToken)
    {
        var addresses = await db.BusinessPartners.AsNoTracking().Where(x => x.Id == partnerId && x.Email != null).Select(x => x.Email!).Concat(db.ContactPeople.AsNoTracking().Where(x => x.BusinessPartnerId == partnerId && x.Email != null).Select(x => x.Email!)).ToListAsync(cancellationToken);
        return addresses.Select(x => x.Trim().ToLowerInvariant()).Where(x => x.Length > 0).Distinct().ToList();
    }
    private static bool IsExplicitlyLinked(EmailMessage email, Guid? partnerId, Guid? orderId) => (!partnerId.HasValue || email.BusinessPartnerId == partnerId) && (!orderId.HasValue || email.OrderId == orderId);
    private static bool RecipientContains(string? recipients, IReadOnlyCollection<string> addresses) => !string.IsNullOrWhiteSpace(recipients) && recipients.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => x.ToLowerInvariant()).Any(addresses.Contains);
    private static string? Preview(string? value) => value is null || value.Length <= 1000 ? value : value[..1000] + "…";
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
