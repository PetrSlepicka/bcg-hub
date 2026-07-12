using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BcgHub.Api.Infrastructure.Configurations;

public sealed class CollaborationConfiguration : IEntityTypeConfiguration<Comment>, IEntityTypeConfiguration<Attachment>, IEntityTypeConfiguration<Communication>
{
    public void Configure(EntityTypeBuilder<Comment> entity)
    {
        ConfigureResource(entity, "Comments");
        entity.Property(x => x.AuthorName).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Text).HasMaxLength(10000).IsRequired();
    }

    public void Configure(EntityTypeBuilder<Attachment> entity)
    {
        ConfigureResource(entity, "Attachments");
        entity.Property(x => x.FileName).HasMaxLength(500).IsRequired();
        entity.Property(x => x.ContentType).HasMaxLength(200).IsRequired();
        entity.Property(x => x.StorageKey).HasMaxLength(1000).IsRequired();
        entity.ToTable(table => table.HasCheckConstraint("CK_Attachments_NonNegativeSize", "\"Size\" >= 0"));
    }

    public void Configure(EntityTypeBuilder<Communication> entity)
    {
        entity.Property(x => x.Subject).HasMaxLength(1000).IsRequired();
        entity.Property(x => x.BodyPreview).HasMaxLength(10000);
        entity.Property(x => x.Sender).HasMaxLength(1000);
        entity.Property(x => x.Recipients).HasMaxLength(4000);
        entity.Property(x => x.ExternalProvider).HasMaxLength(100);
        entity.Property(x => x.ExternalMailboxId).HasMaxLength(500);
        entity.Property(x => x.ExternalId).HasMaxLength(1000);
        entity.HasIndex(x => new { x.BusinessPartnerId, x.OccurredAtUtc });
        entity.HasIndex(x => new { x.OrderId, x.OccurredAtUtc });
        entity.HasIndex(x => new { x.ExternalProvider, x.ExternalMailboxId, x.ExternalId }).IsUnique().HasFilter("\"ExternalId\" IS NOT NULL");
        entity.ToTable(table => table.HasCheckConstraint("CK_Communications_HasOwner", "\"BusinessPartnerId\" IS NOT NULL OR \"OrderId\" IS NOT NULL"));
        entity.HasOne(x => x.BusinessPartner).WithMany().HasForeignKey(x => x.BusinessPartnerId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureResource<TEntity>(EntityTypeBuilder<TEntity> entity, string tableName) where TEntity : Entity, IEntityResource
    {
        entity.ToTable(table => table.HasCheckConstraint($"CK_{tableName}_ExactlyOneOwner", "num_nonnulls(\"BusinessPartnerId\", \"ContactPersonId\", \"OrderId\", \"WorkflowStepId\", \"TransportQuoteId\", \"CommunicationId\", \"EmailMessageId\") = 1"));
        entity.HasIndex(x => new { x.BusinessPartnerId, x.CreatedAtUtc });
        entity.HasIndex(x => new { x.ContactPersonId, x.CreatedAtUtc });
        entity.HasIndex(x => new { x.OrderId, x.CreatedAtUtc });
        entity.HasIndex(x => new { x.WorkflowStepId, x.CreatedAtUtc });
        entity.HasIndex(x => new { x.TransportQuoteId, x.CreatedAtUtc });
        entity.HasIndex(x => new { x.CommunicationId, x.CreatedAtUtc });
        entity.HasIndex(x => new { x.EmailMessageId, x.CreatedAtUtc });
        entity.HasOne(x => x.BusinessPartner).WithMany().HasForeignKey(x => x.BusinessPartnerId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.ContactPerson).WithMany().HasForeignKey(x => x.ContactPersonId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.WorkflowStep).WithMany().HasForeignKey(x => x.WorkflowStepId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.TransportQuote).WithMany().HasForeignKey(x => x.TransportQuoteId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.Communication).WithMany().HasForeignKey(x => x.CommunicationId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.EmailMessage).WithMany().HasForeignKey(x => x.EmailMessageId).OnDelete(DeleteBehavior.Cascade);
    }
}
