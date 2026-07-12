using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class EmailTemplateService(BcgHubDbContext db, CurrentUserAccessor currentUser) : IEmailTemplateService
{
    public async Task<IReadOnlyList<EmailTemplateDto>> GetAllAsync(CancellationToken cancellationToken) => await db.EmailTemplates.AsNoTracking().Where(x => x.UserAccountId == currentUser.UserId).OrderBy(x => x.Name).Select(x => Map(x)).ToListAsync(cancellationToken);

    public async Task<EmailTemplateDto> CreateAsync(SaveEmailTemplateRequest request, CancellationToken cancellationToken)
    {
        await EnsureUniqueNameAsync(request.Name, null, cancellationToken);
        var template = new EmailTemplate { UserAccountId = currentUser.UserId, Name = request.Name.Trim(), Subject = request.Subject.Trim(), BodyHtml = request.BodyHtml };
        db.EmailTemplates.Add(template);
        await db.SaveChangesAsync(cancellationToken);
        return Map(template);
    }

    public async Task<EmailTemplateDto?> UpdateAsync(Guid id, SaveEmailTemplateRequest request, CancellationToken cancellationToken)
    {
        var template = await db.EmailTemplates.SingleOrDefaultAsync(x => x.Id == id && x.UserAccountId == currentUser.UserId, cancellationToken);
        if (template is null) return null;
        await EnsureUniqueNameAsync(request.Name, id, cancellationToken);
        db.Entry(template).Property(x => x.Version).OriginalValue = request.Version;
        template.Name = request.Name.Trim(); template.Subject = request.Subject.Trim(); template.BodyHtml = request.BodyHtml;
        await db.SaveChangesAsync(cancellationToken);
        return Map(template);
    }

    public async Task<bool> DeleteAsync(Guid id, uint version, CancellationToken cancellationToken)
    {
        var template = await db.EmailTemplates.SingleOrDefaultAsync(x => x.Id == id && x.UserAccountId == currentUser.UserId, cancellationToken);
        if (template is null) return false;
        db.Entry(template).Property(x => x.Version).OriginalValue = version;
        db.EmailTemplates.Remove(template);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureUniqueNameAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim();
        if (await db.EmailTemplates.AnyAsync(x => x.UserAccountId == currentUser.UserId && x.Name == normalized && x.Id != exceptId, cancellationToken)) throw new DomainValidationException("Šablona s tímto názvem již existuje.");
    }

    private static EmailTemplateDto Map(EmailTemplate template) => new(template.Id, template.Name, template.Subject, template.BodyHtml, template.Version);
}
