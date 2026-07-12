using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class ComplaintService(BcgHubDbContext db) : IComplaintService
{
    public async Task<IReadOnlyList<ComplaintListItem>> GetListAsync(CancellationToken cancellationToken) => await db.Complaints.AsNoTracking().OrderByDescending(x => x.ReportedOn).ThenByDescending(x => x.CreatedAtUtc).Select(x => new ComplaintListItem(x.Id, x.ReportedOn, x.Status, new PartnerReference(x.Customer.Id, x.Customer.Name), x.OrderId, x.Order.Number, x.Description)).ToListAsync(cancellationToken);
    public async Task<ComplaintDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken) => await db.Complaints.AsNoTracking().Where(x => x.Id == id).Select(x => new ComplaintDetailDto(x.Id, x.ReportedOn, x.Status, new PartnerReference(x.Customer.Id, x.Customer.Name), x.OrderId, x.Order.Number, x.Description, x.Version)).SingleOrDefaultAsync(cancellationToken);

    public async Task<ComplaintDetailDto> CreateAsync(SaveComplaintRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(request, cancellationToken);
        var complaint = new Complaint { ReportedOn = request.ReportedOn, Status = request.Status, CustomerId = request.CustomerId, OrderId = request.OrderId, Description = Clean(request.Description) };
        db.Complaints.Add(complaint);
        await SaveAsync(cancellationToken);
        return (await GetDetailAsync(complaint.Id, cancellationToken))!;
    }

    public async Task<ComplaintDetailDto?> UpdateAsync(Guid id, SaveComplaintRequest request, CancellationToken cancellationToken)
    {
        var complaint = await db.Complaints.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (complaint is null) return null;
        await ValidateAsync(request, cancellationToken);
        db.Entry(complaint).Property(x => x.Version).OriginalValue = request.Version;
        complaint.ReportedOn = request.ReportedOn; complaint.Status = request.Status; complaint.CustomerId = request.CustomerId; complaint.OrderId = request.OrderId; complaint.Description = Clean(request.Description);
        await SaveAsync(cancellationToken);
        return await GetDetailAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, uint version, CancellationToken cancellationToken)
    {
        var complaint = await db.Complaints.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (complaint is null) return false;
        db.Entry(complaint).Property(x => x.Version).OriginalValue = version;
        db.Complaints.Remove(complaint);
        await SaveAsync(cancellationToken);
        return true;
    }

    private async Task ValidateAsync(SaveComplaintRequest request, CancellationToken cancellationToken)
    {
        if (request.ReportedOn == default) throw new DomainValidationException("Datum oznámení je povinné.");
        var valid = await db.Orders.AnyAsync(x => x.Id == request.OrderId && x.CustomerId == request.CustomerId, cancellationToken);
        if (!valid) throw new DomainValidationException("Zakázka neexistuje nebo nepatří vybranému zákazníkovi.");
    }

    private async Task SaveAsync(CancellationToken cancellationToken) { try { await db.SaveChangesAsync(cancellationToken); } catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("Reklamaci mezitím změnil jiný uživatel."); } }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
