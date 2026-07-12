using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class BcgHubDbContext(DbContextOptions<BcgHubDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<BusinessPartner> BusinessPartners => Set<BusinessPartner>();
    public DbSet<ContactPerson> ContactPeople => Set<ContactPerson>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderWorkflowStep> OrderWorkflowSteps => Set<OrderWorkflowStep>();
    public DbSet<TransportQuote> TransportQuotes => Set<TransportQuote>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Communication> Communications => Set<Communication>();
    public DbSet<EmailAccountSettings> EmailAccountSettings => Set<EmailAccountSettings>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BcgHubDbContext).Assembly);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(x => typeof(Entity).IsAssignableFrom(x.ClrType))) modelBuilder.Entity(entityType.ClrType).Property<uint>(nameof(Entity.Version)).IsRowVersion();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity>().Where(x => x.State == EntityState.Modified)) entry.Entity.UpdatedAtUtc = now;
        return base.SaveChangesAsync(cancellationToken);
    }
}
