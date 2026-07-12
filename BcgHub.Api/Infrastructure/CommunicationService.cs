using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class CommunicationService(BcgHubDbContext db) : ICommunicationService
{
    public async Task<PagedResult<CommunicationDto>> GetListAsync(Guid? partnerId, Guid? orderId, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!partnerId.HasValue && !orderId.HasValue) throw new DomainValidationException("Je nutné vybrat partnera nebo zakázku.");
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Communications.AsNoTracking().Where(x => (!partnerId.HasValue || x.BusinessPartnerId == partnerId) && (!orderId.HasValue || x.OrderId == orderId));
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAtUtc).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new CommunicationDto(x.Id, x.Type, x.BusinessPartnerId, x.OrderId, x.Subject, x.BodyPreview, x.Sender, x.Recipients, x.OccurredAtUtc, x.Version)).ToListAsync(cancellationToken);
        return new PagedResult<CommunicationDto>(items, page, pageSize, totalCount);
    }

    public async Task<CommunicationDto> CreateAsync(SaveCommunicationRequest request, CancellationToken cancellationToken) { await ValidateAsync(request, cancellationToken); var communication = new Communication(); Apply(communication, request); db.Communications.Add(communication); await db.SaveChangesAsync(cancellationToken); return Map(communication); }
    public async Task<CommunicationDto?> UpdateAsync(Guid id, SaveCommunicationRequest request, CancellationToken cancellationToken) { var communication = await db.Communications.SingleOrDefaultAsync(x => x.Id == id, cancellationToken); if (communication is null) return null; await ValidateAsync(request, cancellationToken); db.Entry(communication).Property(x => x.Version).OriginalValue = request.Version; Apply(communication, request); await SaveAsync(cancellationToken); return Map(communication); }
    public async Task<bool> DeleteAsync(Guid id, uint version, CancellationToken cancellationToken) { var communication = await db.Communications.SingleOrDefaultAsync(x => x.Id == id, cancellationToken); if (communication is null) return false; db.Entry(communication).Property(x => x.Version).OriginalValue = version; db.Communications.Remove(communication); await SaveAsync(cancellationToken); return true; }
    private async Task ValidateAsync(SaveCommunicationRequest request, CancellationToken cancellationToken) { if (!request.BusinessPartnerId.HasValue && !request.OrderId.HasValue) throw new DomainValidationException("Komunikace musí patřit partnerovi nebo zakázce."); if (request.BusinessPartnerId.HasValue && !await db.BusinessPartners.AnyAsync(x => x.Id == request.BusinessPartnerId, cancellationToken)) throw new DomainValidationException("Partner neexistuje."); if (request.OrderId.HasValue && !await db.Orders.AnyAsync(x => x.Id == request.OrderId, cancellationToken)) throw new DomainValidationException("Zakázka neexistuje."); }
    private async Task SaveAsync(CancellationToken cancellationToken) { try { await db.SaveChangesAsync(cancellationToken); } catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("Komunikaci mezitím změnil jiný uživatel."); } }
    private static void Apply(Communication communication, SaveCommunicationRequest request) { communication.Type = request.Type; communication.BusinessPartnerId = request.BusinessPartnerId; communication.OrderId = request.OrderId; communication.Subject = string.IsNullOrWhiteSpace(request.Subject) ? throw new DomainValidationException("Předmět je povinný.") : request.Subject.Trim(); communication.BodyPreview = Clean(request.BodyPreview); communication.Sender = Clean(request.Sender); communication.Recipients = Clean(request.Recipients); communication.OccurredAtUtc = request.OccurredAtUtc == default ? DateTime.UtcNow : request.OccurredAtUtc; }
    private static CommunicationDto Map(Communication x) => new(x.Id, x.Type, x.BusinessPartnerId, x.OrderId, x.Subject, x.BodyPreview, x.Sender, x.Recipients, x.OccurredAtUtc, x.Version);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
