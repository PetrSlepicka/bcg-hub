using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BcgHub.Api.Infrastructure.Configurations;

public sealed class EmailConfiguration : IEntityTypeConfiguration<EmailAccountSettings>, IEntityTypeConfiguration<EmailMessage>, IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailAccountSettings> entity)
    {
        entity.Property(x => x.ImapServer).HasMaxLength(300).IsRequired();
        entity.Property(x => x.ImapUsername).HasMaxLength(320).IsRequired();
        entity.Property(x => x.ProtectedImapPassword).HasMaxLength(4000).IsRequired();
        entity.Property(x => x.SmtpServer).HasMaxLength(300).IsRequired();
        entity.Property(x => x.SmtpUsername).HasMaxLength(320).IsRequired();
        entity.Property(x => x.ProtectedSmtpPassword).HasMaxLength(4000).IsRequired();
        entity.Property(x => x.SenderAddress).HasMaxLength(320).IsRequired();
        entity.Property(x => x.SenderName).HasMaxLength(300);
        entity.Property(x => x.MicrosoftMailboxAddress).HasMaxLength(320);
        entity.Property(x => x.ProtectedMicrosoftRefreshToken).HasMaxLength(8000);
        entity.Property(x => x.MicrosoftDeltaLink).HasMaxLength(8000);
        entity.HasIndex(x => x.UserAccountId).IsUnique();
        entity.HasOne(x => x.UserAccount).WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<EmailMessage> entity)
    {
        entity.Property(x => x.ExternalId).HasMaxLength(1000).IsRequired();
        entity.Property(x => x.Mailbox).HasMaxLength(300).IsRequired();
        entity.Property(x => x.FromAddress).HasMaxLength(1000).IsRequired();
        entity.Property(x => x.FromName).HasMaxLength(500);
        entity.Property(x => x.ToAddress).HasMaxLength(4000).IsRequired();
        entity.Property(x => x.CcAddress).HasMaxLength(4000);
        entity.Property(x => x.Subject).HasMaxLength(1000).IsRequired();
        entity.HasIndex(x => new { x.UserAccountId, x.ExternalId }).IsUnique();
        entity.HasIndex(x => new { x.UserAccountId, x.OccurredAtUtc });
        entity.HasOne(x => x.UserAccount).WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.BusinessPartner).WithMany().HasForeignKey(x => x.BusinessPartnerId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<EmailTemplate> entity)
    {
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Subject).HasMaxLength(1000).IsRequired();
        entity.Property(x => x.BodyHtml).IsRequired();
        entity.HasIndex(x => new { x.UserAccountId, x.Name }).IsUnique();
        entity.HasOne(x => x.UserAccount).WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
    }
}
