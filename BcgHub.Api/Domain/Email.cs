namespace BcgHub.Api.Domain;

public enum EmailDirection { Inbound, Outbound }
public enum EmailProvider { ImapSmtp, MicrosoftGraph }

public sealed class EmailAccountSettings : Entity
{
    public Guid UserAccountId { get; set; }
    public UserAccount UserAccount { get; set; } = null!;
    public EmailProvider Provider { get; set; } = EmailProvider.ImapSmtp;
    public string ImapServer { get; set; } = "";
    public int ImapPort { get; set; } = 993;
    public bool ImapUseSsl { get; set; } = true;
    public string ImapUsername { get; set; } = "";
    public string ProtectedImapPassword { get; set; } = "";
    public string SmtpServer { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public bool SmtpUseSsl { get; set; } = true;
    public string SmtpUsername { get; set; } = "";
    public string ProtectedSmtpPassword { get; set; } = "";
    public string SenderAddress { get; set; } = "";
    public string? SenderName { get; set; }
    public string? MicrosoftMailboxAddress { get; set; }
    public string? ProtectedMicrosoftRefreshToken { get; set; }
    public string? MicrosoftDeltaLink { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class EmailTemplate : Entity
{
    public Guid UserAccountId { get; set; }
    public UserAccount UserAccount { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Subject { get; set; } = "";
    public string BodyHtml { get; set; } = "";
}

public sealed class EmailMessage : Entity
{
    public Guid UserAccountId { get; set; }
    public UserAccount UserAccount { get; set; } = null!;
    public Guid? BusinessPartnerId { get; set; }
    public BusinessPartner? BusinessPartner { get; set; }
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    public EmailDirection Direction { get; set; }
    public string ExternalId { get; set; } = "";
    public uint ImapUid { get; set; }
    public string Mailbox { get; set; } = "INBOX";
    public string FromAddress { get; set; } = "";
    public string? FromName { get; set; }
    public string ToAddress { get; set; } = "";
    public string? CcAddress { get; set; }
    public string Subject { get; set; } = "";
    public string? BodyText { get; set; }
    public string? BodyHtml { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public bool IsRead { get; set; }
    public bool HasAttachments { get; set; }
}
