using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class EntityResourceService(BcgHubDbContext db, CurrentUserAccessor currentUser, IFileStorage storage) : IEntityResourceService
{
    public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(ResourceOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken)
    {
        await EnsureOwnerAsync(ownerType, ownerId, cancellationToken);
        return await Filter(db.Comments.AsNoTracking(), ownerType, ownerId).OrderByDescending(x => x.CreatedAtUtc).Select(x => new CommentDto(x.Id, x.AuthorName, x.Text, x.CreatedAtUtc, x.Version)).ToListAsync(cancellationToken);
    }

    public async Task<CommentDto> AddCommentAsync(ResourceOwnerType ownerType, Guid ownerId, SaveCommentRequest request, CancellationToken cancellationToken)
    {
        await EnsureOwnerAsync(ownerType, ownerId, cancellationToken);
        var comment = new Comment { AuthorName = currentUser.FullName, Text = Required(request.Text, "Komentář") };
        SetOwner(comment, ownerType, ownerId);
        db.Comments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);
        return Map(comment);
    }

    public async Task<CommentDto?> UpdateCommentAsync(Guid commentId, SaveCommentRequest request, CancellationToken cancellationToken)
    {
        var comment = await db.Comments.SingleOrDefaultAsync(x => x.Id == commentId, cancellationToken);
        if (comment is null) return null;
        await EnsureEmailOwnershipAsync(comment.EmailMessageId, cancellationToken);
        db.Entry(comment).Property(x => x.Version).OriginalValue = request.Version;
        comment.Text = Required(request.Text, "Komentář");
        await SaveAsync(cancellationToken);
        return Map(comment);
    }

    public async Task<bool> DeleteCommentAsync(Guid commentId, uint version, CancellationToken cancellationToken)
    {
        var comment = await db.Comments.SingleOrDefaultAsync(x => x.Id == commentId, cancellationToken);
        if (comment is null) return false;
        await EnsureEmailOwnershipAsync(comment.EmailMessageId, cancellationToken);
        db.Entry(comment).Property(x => x.Version).OriginalValue = version;
        db.Comments.Remove(comment);
        await SaveAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<AttachmentDto>> GetAttachmentsAsync(ResourceOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken)
    {
        await EnsureOwnerAsync(ownerType, ownerId, cancellationToken);
        return await Filter(db.Attachments.AsNoTracking(), ownerType, ownerId).OrderByDescending(x => x.CreatedAtUtc).Select(x => new AttachmentDto(x.Id, x.FileName, x.ContentType, x.Size, x.CreatedAtUtc, x.Version)).ToListAsync(cancellationToken);
    }

    public async Task<AttachmentDto> AddAttachmentAsync(ResourceOwnerType ownerType, Guid ownerId, string fileName, string contentType, long size, Stream content, CancellationToken cancellationToken)
    {
        await EnsureOwnerAsync(ownerType, ownerId, cancellationToken);
        if (size <= 0 || size > 2 * 1024 * 1024) throw new DomainValidationException("Soubor musí mít velikost od 1 B do 2 MB.");
        fileName = Path.GetFileName(fileName);
        var key = await storage.SaveAsync(fileName, content, cancellationToken);
        var attachment = new Attachment { FileName = fileName, ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType, Size = size, StorageKey = key };
        SetOwner(attachment, ownerType, ownerId);
        db.Attachments.Add(attachment);
        try { await db.SaveChangesAsync(cancellationToken); } catch { await storage.TryDeleteAsync(key, cancellationToken); throw; }
        return Map(attachment);
    }

    public async Task<StoredFile?> OpenAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await db.Attachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == attachmentId, cancellationToken);
        if (attachment is null) return null;
        await EnsureEmailOwnershipAsync(attachment.EmailMessageId, cancellationToken);
        return new StoredFile(await storage.OpenReadAsync(attachment.StorageKey, cancellationToken), attachment.FileName, attachment.ContentType);
    }

    public async Task<bool> DeleteAttachmentAsync(Guid attachmentId, uint version, CancellationToken cancellationToken)
    {
        var attachment = await db.Attachments.SingleOrDefaultAsync(x => x.Id == attachmentId, cancellationToken);
        if (attachment is null) return false;
        await EnsureEmailOwnershipAsync(attachment.EmailMessageId, cancellationToken);
        db.Entry(attachment).Property(x => x.Version).OriginalValue = version;
        db.Attachments.Remove(attachment);
        await SaveAsync(cancellationToken);
        await storage.TryDeleteAsync(attachment.StorageKey, cancellationToken);
        return true;
    }

    private async Task EnsureOwnerAsync(ResourceOwnerType type, Guid id, CancellationToken cancellationToken)
    {
        var exists = type switch { ResourceOwnerType.BusinessPartner => await db.BusinessPartners.AnyAsync(x => x.Id == id, cancellationToken), ResourceOwnerType.ContactPerson => await db.ContactPeople.AnyAsync(x => x.Id == id, cancellationToken), ResourceOwnerType.Order => await db.Orders.AnyAsync(x => x.Id == id, cancellationToken), ResourceOwnerType.WorkflowStep => await db.OrderWorkflowSteps.AnyAsync(x => x.Id == id, cancellationToken), ResourceOwnerType.TransportQuote => await db.TransportQuotes.AnyAsync(x => x.Id == id, cancellationToken), ResourceOwnerType.Communication => await db.Communications.AnyAsync(x => x.Id == id, cancellationToken), ResourceOwnerType.EmailMessage => await db.EmailMessages.AnyAsync(x => x.Id == id && x.UserAccountId == currentUser.UserId, cancellationToken), ResourceOwnerType.Complaint => await db.Complaints.AnyAsync(x => x.Id == id, cancellationToken), _ => false };
        if (!exists) throw new DomainValidationException("Cílový záznam neexistuje nebo k němu nemáte přístup.");
    }

    private async Task EnsureEmailOwnershipAsync(Guid? emailId, CancellationToken cancellationToken) { if (emailId.HasValue && !await db.EmailMessages.AnyAsync(x => x.Id == emailId && x.UserAccountId == currentUser.UserId, cancellationToken)) throw new UnauthorizedAccessException(); }
    private async Task SaveAsync(CancellationToken cancellationToken) { try { await db.SaveChangesAsync(cancellationToken); } catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("Záznam mezitím změnil jiný uživatel."); } }
    private static IQueryable<TEntity> Filter<TEntity>(IQueryable<TEntity> query, ResourceOwnerType type, Guid id) where TEntity : Entity, IEntityResource => type switch { ResourceOwnerType.BusinessPartner => query.Where(x => x.BusinessPartnerId == id), ResourceOwnerType.ContactPerson => query.Where(x => x.ContactPersonId == id), ResourceOwnerType.Order => query.Where(x => x.OrderId == id), ResourceOwnerType.WorkflowStep => query.Where(x => x.WorkflowStepId == id), ResourceOwnerType.TransportQuote => query.Where(x => x.TransportQuoteId == id), ResourceOwnerType.Communication => query.Where(x => x.CommunicationId == id), ResourceOwnerType.EmailMessage => query.Where(x => x.EmailMessageId == id), ResourceOwnerType.Complaint => query.Where(x => x.ComplaintId == id), _ => query.Where(_ => false) };
    private static void SetOwner(IEntityResource resource, ResourceOwnerType type, Guid id) { if (type == ResourceOwnerType.BusinessPartner) resource.BusinessPartnerId = id; else if (type == ResourceOwnerType.ContactPerson) resource.ContactPersonId = id; else if (type == ResourceOwnerType.Order) resource.OrderId = id; else if (type == ResourceOwnerType.WorkflowStep) resource.WorkflowStepId = id; else if (type == ResourceOwnerType.TransportQuote) resource.TransportQuoteId = id; else if (type == ResourceOwnerType.Communication) resource.CommunicationId = id; else if (type == ResourceOwnerType.EmailMessage) resource.EmailMessageId = id; else if (type == ResourceOwnerType.Complaint) resource.ComplaintId = id; else throw new DomainValidationException("Nepodporovaný typ cíle."); }
    private static CommentDto Map(Comment comment) => new(comment.Id, comment.AuthorName, comment.Text, comment.CreatedAtUtc, comment.Version);
    private static AttachmentDto Map(Attachment attachment) => new(attachment.Id, attachment.FileName, attachment.ContentType, attachment.Size, attachment.CreatedAtUtc, attachment.Version);
    private static string Required(string value, string label) => string.IsNullOrWhiteSpace(value) ? throw new DomainValidationException($"{label} je povinný.") : value.Trim();
}
